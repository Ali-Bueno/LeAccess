using System;
using BepInEx.Configuration;
using Rewired;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Exploration;

// One exploration command per frame from the right stick, the keyboard and the Create modifier.
// While the modifier is held in gameplay the game bindings of every button are switched off, so
// Square and Cross do not also cast or interact; each is restored once its button is up.
internal static class ExplorationInput
{
    private readonly struct Suppressed
    {
        internal readonly ActionElementMap Element;
        internal readonly Joystick Joystick;
        internal readonly int ButtonId;
        internal Suppressed(ActionElementMap element, Joystick joystick, int buttonId)
        { Element = element; Joystick = joystick; ButtonId = buttonId; }
    }

    private struct Reading
    {
        internal bool Modifier, Vitals, Lock, Gold;
        internal ExplorationCommand Flick;
        internal string Signature;
    }

    private static ConfigEntry<bool> _scanner;
    private static ConfigEntry<bool> _modifierMenu;
    private static readonly BindingSuppression<Suppressed> Ledger = new();
    private static bool _previousModifier, _active;
    private static string _suppressionSignature;
    private static string _controllerFailure, _suppressionFailure, _keyboardFailure;

    internal static ExplorationCommand Command { get; private set; }
    internal static bool MenuHeld { get; private set; }
    internal static bool MenuOpenedThisFrame { get; private set; }

    internal static void Initialize(ConfigFile config)
    {
        _scanner = config.Bind("Exploration", "ControllerScanner", true,
            "Drive the exploration scanner with the controller right stick during gameplay: up/down change category, left/right change entry.");
        _modifierMenu = config.Bind("Exploration", "ModifierMenu", true,
            "Hold the controller Create/Share button for the exploration menu (Square = vitals, Cross = lock, Triangle = gold). The game button bindings are paused while it is held.");
        _previousModifier = _active = false;
        _controllerFailure = _suppressionFailure = _keyboardFailure = null;
    }

    internal static void Update(bool gameplay)
    {
        Command = ExplorationCommand.None;
        MenuHeld = false;
        MenuOpenedThisFrame = false;
        if (_scanner is null) return;
        try
        {
            var reading = ReadControllers(_scanner.Value, _modifierMenu.Value);
            bool wentDown = reading.Modifier && !_previousModifier;
            _previousModifier = reading.Modifier;
            if (!gameplay)
            {
                RestoreAll("left gameplay");
                _active = false;
                return;
            }
            if (!reading.Modifier) _active = false;
            else if (wentDown)
            {
                _active = true;
                MenuOpenedThisFrame = true;
                Suppress(reading.Signature);
            }
            if (_active && reading.Signature != _suppressionSignature)
            {
                // The joystick set changed under us: give everything back; the next press starts afresh.
                RestoreAll("controllers changed");
                _active = false;
            }
            if (!_active && Ledger.Count > 0) ReleaseWhenButtonsUp();
            MenuHeld = _active;

            var command = KeyboardCommand();
            if (command == ExplorationCommand.None && _active)
                command = reading.Vitals ? ExplorationCommand.Vitals : reading.Lock ? ExplorationCommand.Lock
                    : reading.Gold ? ExplorationCommand.Gold : command;
            Command = command != ExplorationCommand.None ? command : reading.Flick;
        }
        catch (Exception ex)
        {
            Command = ExplorationCommand.None;
            MenuHeld = false;
            MenuOpenedThisFrame = false;
            _active = false;
            ReportFailure("Exploration input", ex, ref _controllerFailure);
            RestoreAll("input error");
        }
    }

    internal static void Cleanup()
    {
        Command = ExplorationCommand.None;
        MenuHeld = false;
        MenuOpenedThisFrame = false;
        _active = false;
        RestoreAll("cleanup");
    }

    private static Reading ReadControllers(bool stick, bool modifier)
    {
        var reading = new Reading { Signature = "" };
        if (!stick && !modifier) return reading;
        var player = CurrentPlayer();
        var joysticks = player?.controllers?.Joysticks;
        if (joysticks is null || joysticks.WasCollected) return reading;
        var collection = joysticks.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
        if (collection is null || collection.WasCollected) return reading;
        for (int index = 0; index < collection.Count; index++)
        {
            var joystick = joysticks[index];
            if (joystick is null || joystick.WasCollected) continue;
            reading.Signature += $"{joystick.id};";
            var template = joystick.GetTemplate<IGamepadTemplate>();
            if (template is null || template.WasCollected) continue;
            if (modifier)
            {
                var back = template.back;
                if (back is not null && !back.WasCollected && back.value) reading.Modifier = true;
                var square = template.actionTopRow1;
                if (square is not null && !square.WasCollected && square.justPressed) reading.Vitals = true;
                var cross = template.actionBottomRow1;
                if (cross is not null && !cross.WasCollected && cross.justPressed) reading.Lock = true;
                var triangle = template.actionTopRow2;
                if (triangle is not null && !triangle.WasCollected && triangle.justPressed) reading.Gold = true;
            }
            if (stick && reading.Flick == ExplorationCommand.None)
            {
                var rightStick = template.rightStick;
                if (rightStick is null || rightStick.WasCollected) continue;
                var value = rightStick.value;
                var previous = rightStick.valuePrev;
                reading.Flick = StickFlick.Detect(value.x, value.y, previous.x, previous.y);
            }
        }
        return reading;
    }

    private static Player CurrentPlayer()
    {
        var manager = EpochInputManager.instance;
        var player = IsAlive(manager) ? manager.rewiredPlayer : null;
        return player is null || player.WasCollected ? null : player;
    }

    private static void Suppress(string signature)
    {
        try
        {
            var player = CurrentPlayer();
            var joysticks = player?.controllers?.Joysticks;
            var collection = joysticks?.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
            if (collection is null) return;
            _suppressionSignature = signature;
            int before = Ledger.Count;
            for (int index = 0; index < collection.Count; index++)
            {
                var joystick = joysticks[index];
                if (joystick is null || joystick.WasCollected) continue;
                var maps = player.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id);
                int mapCount = maps.TryCast<Il2CppSystem.Collections.Generic.ICollection<ControllerMap>>()?.Count ?? 0;
                for (int m = 0; m < mapCount; m++)
                {
                    var map = maps[m];
                    if (map is null || map.WasCollected) continue;
                    var elements = map.AllMaps;
                    int elementCount = elements.TryCast<Il2CppSystem.Collections.Generic.ICollection<ActionElementMap>>()?.Count ?? 0;
                    for (int e = 0; e < elementCount; e++)
                    {
                        var element = elements[e];
                        if (element is null || element.WasCollected || !element.enabled ||
                            element.elementType != ControllerElementType.Button) continue;
                        element.enabled = false;
                        Ledger.Record(new Suppressed(element, joystick, element.elementIdentifierId));
                    }
                }
            }
            Plugin.Log.LogInfo($"[Exploration input] game button bindings paused: {Ledger.Count - before}");
        }
        catch (Exception ex)
        {
            ReportFailure("Exploration suppression", ex, ref _suppressionFailure);
            RestoreAll("suppression error");
            _active = false;
        }
    }

    private static void ReleaseWhenButtonsUp()
    {
        Ledger.ReleaseWhenUp(item => !item.Joystick.WasCollected && item.Joystick.GetButtonById(item.ButtonId),
            item => item.Element.enabled = true);
        if (Ledger.Count == 0) Plugin.Log.LogInfo("[Exploration input] game button bindings restored");
    }

    private static void RestoreAll(string reason)
    {
        if (Ledger.Count == 0) return;
        int count = Ledger.RestoreAll(item => item.Element.enabled = true);
        Plugin.Log.LogInfo($"[Exploration input] game button bindings restored ({reason}): {count}");
    }

    private static ExplorationCommand KeyboardCommand()
    {
        try
        {
            if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ||
                Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) ||
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return ExplorationCommand.None;
            if (Input.GetKeyDown(KeyCode.Keypad8)) return ExplorationCommand.CategoryPrevious;
            if (Input.GetKeyDown(KeyCode.Keypad2)) return ExplorationCommand.CategoryNext;
            if (Input.GetKeyDown(KeyCode.Keypad4)) return ExplorationCommand.EntryPrevious;
            if (Input.GetKeyDown(KeyCode.Keypad6)) return ExplorationCommand.EntryNext;
            if (Input.GetKeyDown(KeyCode.B)) return ExplorationCommand.Lock;
            if (Input.GetKeyDown(KeyCode.G)) return ExplorationCommand.Gold;
        }
        catch (Exception ex) { ReportFailure("Exploration keyboard", ex, ref _keyboardFailure); }
        return ExplorationCommand.None;
    }

    private static void ReportFailure(string source, Exception exception, ref string previous)
    {
        string failure = $"{exception.GetType().Name}: {exception.Message}";
        if (previous == failure) return;
        previous = failure;
        Plugin.Log.LogWarning($"{source} unavailable: {failure}");
    }
}
