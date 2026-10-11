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

/// <summary>Turns off every Rewired controller map of every player and the UI event system's navigation while the menu
/// is open (Player.isPlaying silences nothing), and turns back on only what it turned off once nothing is held.</summary>
internal static class InputBlock
{
    private static readonly List<ControllerMap> Disabled = new();
    private static readonly List<UnityEngine.EventSystems.EventSystem> Navigation = new();
    private static bool _open;
    private static int _atOpen;
    private static string _failure;

    internal static void Begin()
    {
        _open = true;
        Hold();
        _atOpen = Disabled.Count;
        Plugin.Log.LogInfo($"[Mod menu] game input paused: {Disabled.Count} controller maps off, UI navigation off: {Navigation.Count}");
    }

    /// <summary>Each frame while open: a map the game turns on meanwhile goes off too.</summary>
    internal static void Hold()
    {
        try
        {
            var players = ReInput.players;
            if (players is null) return;
            for (int index = 0; index < players.playerCount; index++) HoldPlayer(players.GetPlayer(index));
            HoldPlayer(players.GetSystemPlayer());
        }
        catch (Exception ex) { Report(ex); }
        HoldNavigation();
    }

    internal static void Close()
    {
        _open = false;
        // More maps than at opening means the game turned some back on while the menu was open.
        if (Disabled.Count != _atOpen) Plugin.Log.LogInfo($"[Mod menu] maps turned back on by the game while open: {Disabled.Count - _atOpen}");
    }

    /// <summary>Waiting for release keeps the closing press from reaching the game as a new press.</summary>
    internal static void EndWhenIdle()
    {
        if (_open || Disabled.Count == 0 && Navigation.Count == 0) return;
        if (AnythingHeld()) { Hold(); return; }
        End();
    }

    internal static void End()
    {
        _open = false;
        foreach (var events in Navigation)
        {
            try { if (!events.WasCollected) events.sendNavigationEvents = true; }
            catch (Exception ex) { Report(ex); }
        }
        Navigation.Clear();
        if (Disabled.Count == 0) return;
        int restored = 0;
        foreach (var map in Disabled)
        {
            try { if (!map.WasCollected) { map.enabled = true; restored++; } }
            catch (Exception ex) { Report(ex); }
        }
        Disabled.Clear();
        Plugin.Log.LogInfo($"[Mod menu] game input resumed: {restored} controller maps on");
    }

    // Unity UI modules (the login screen reads the pad through Unity's own input) send no move, submit or cancel then.
    private static void HoldNavigation()
    {
        try
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events is null || events.WasCollected || !events.sendNavigationEvents) return;
            events.sendNavigationEvents = false;
            Navigation.Add(events);
        }
        catch (Exception ex) { Report(ex); }
    }

    private static void HoldPlayer(Player player)
    {
        if (player is null || player.WasCollected) return;
        var maps = player.controllers?.maps;
        if (maps is null) return;
        HoldMaps(maps.GetMaps(ControllerType.Keyboard, 0));
        HoldMaps(maps.GetMaps(ControllerType.Mouse, 0));
        var joysticks = player.controllers.Joysticks;
        var collection = joysticks?.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
        for (int index = 0; collection is not null && index < collection.Count; index++)
        {
            var joystick = joysticks[index];
            if (joystick is not null && !joystick.WasCollected) HoldMaps(maps.GetMaps(ControllerType.Joystick, joystick.id));
        }
    }

    private static void HoldMaps(Il2CppSystem.Collections.Generic.IList<ControllerMap> maps)
    {
        int count = maps?.TryCast<Il2CppSystem.Collections.Generic.ICollection<ControllerMap>>()?.Count ?? 0;
        for (int index = 0; index < count; index++)
        {
            var map = maps[index];
            if (map is null || map.WasCollected || !map.enabled) continue;
            map.enabled = false;
            Disabled.Add(map);
        }
    }

    private static void Report(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_failure == failure) return;
        _failure = failure;
        Plugin.Log.LogWarning($"Mod menu input block unavailable: {failure}");
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
