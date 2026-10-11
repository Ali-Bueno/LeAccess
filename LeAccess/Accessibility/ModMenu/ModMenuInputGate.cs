using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Exploration;
using Rewired;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>Keeps the game from hearing L1 while a chord modifier is held (potion in gameplay, era on the map).</summary>
internal static class ShoulderGuard
{
    private readonly struct Suppressed
    {
        internal readonly ActionElementMap Element;
        internal readonly Joystick Joystick;
        internal readonly int ButtonId;
        internal Suppressed(ActionElementMap element, Joystick joystick, int buttonId)
        { Element = element; Joystick = joystick; ButtonId = buttonId; }
    }

    private static readonly BindingSuppression<Suppressed> Ledger = new();
    private static readonly HashSet<int> HeldJoysticks = new();
    private static readonly HashSet<int> Shoulder = new();
    private static string _failure, _logged;

    internal static void Hold(Player player, Joystick joystick, IGamepadTemplate template)
    {
        if (!HeldJoysticks.Add(joystick.id)) return;
        try
        {
            ShoulderElements(template);
            if (Shoulder.Count == 0) return;
            var maps = player.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id);
            int mapCount = maps.TryCast<Il2CppSystem.Collections.Generic.ICollection<ControllerMap>>()?.Count ?? 0;
            int before = Ledger.Count;
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
                        element.elementType != ControllerElementType.Button || !Shoulder.Contains(element.elementIdentifierId)) continue;
                    element.enabled = false;
                    Ledger.Record(new Suppressed(element, joystick, element.elementIdentifierId));
                }
            }
            string message = $"[Mod menu] L1 bindings held back while a chord modifier is down: {Ledger.Count - before}";
            if (message != _logged) Plugin.Log.LogInfo(_logged = message);
        }
        catch (Exception ex) { Report(ex); }
    }

    /// <summary>Gives L1 back once it is up; the modifier is already up.</summary>
    internal static void ReleaseWhenUp()
    {
        HeldJoysticks.Clear();
        if (Ledger.Count == 0) return;
        Ledger.ReleaseWhenUp(item => !item.Joystick.WasCollected && item.Joystick.GetButtonById(item.ButtonId),
            item => { if (!item.Element.WasCollected) item.Element.enabled = true; });
    }

    internal static void Release()
    {
        HeldJoysticks.Clear();
        Ledger.RestoreAll(item => { if (!item.Element.WasCollected) item.Element.enabled = true; });
    }

    // The template's L1 names the pad's own button through its source target.
    private static void ShoulderElements(IGamepadTemplate template)
    {
        Shoulder.Clear();
        var shoulder = template.leftShoulder1;
        if (shoulder is null || shoulder.WasCollected) return;
        var source = shoulder.source?.TryCast<IControllerTemplateButtonSource>();
        var target = source?.target;
        if (target is not null && target.hasTarget) Shoulder.Add(target.elementIdentifierId);
    }

    private static void Report(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_failure == failure) return;
        _failure = failure;
        Plugin.Log.LogWarning($"Mod menu L1 guard unavailable: {failure}");
    }
}

/// <summary>Stops the game's Rewired player while the menu is open; it plays again once nothing is held.</summary>
internal static class InputBlock
{
    private static Player _player;

    internal static void Begin(Player player)
    {
        if (_player is not null || player is null || !player.isPlaying) return;
        player.isPlaying = false;
        _player = player;
        Plugin.Log.LogInfo("[Mod menu] game input paused");
    }

    /// <summary>Waiting for release keeps the closing press from reaching the game as a new press.</summary>
    internal static void EndWhenIdle()
    {
        if (_player is null || AnythingHeld()) return;
        End();
    }

    internal static void End()
    {
        if (_player is null) return;
        try { if (!_player.WasCollected) _player.isPlaying = true; }
        catch (Exception ex) { Plugin.Log.LogWarning($"Mod menu could not resume game input: {ex.Message}"); }
        _player = null;
        Plugin.Log.LogInfo("[Mod menu] game input resumed");
    }

    private static bool AnythingHeld()
    {
        if (UnityEngine.Input.anyKey) return true;
        var controllers = ReInput.controllers;
        if (controllers is null) return false;
        var mouse = controllers.Mouse;
        if (mouse is not null && !mouse.WasCollected && mouse.GetAnyButton()) return true;
        var joysticks = controllers.Joysticks;
        if (joysticks is null || joysticks.WasCollected) return false;
        for (int i = 0; i < controllers.joystickCount; i++)
        {
            var joystick = joysticks[i];
            if (joystick is not null && !joystick.WasCollected && joystick.GetAnyButton()) return true;
        }
        return false;
    }
}
