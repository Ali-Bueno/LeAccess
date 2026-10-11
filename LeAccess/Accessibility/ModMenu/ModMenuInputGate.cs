using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Exploration;
using Rewired;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

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

/// <summary>While the menu is open the game hears nothing: every Rewired controller map is off, the game's controller map
/// stops reading the pad and the UI event system stops navigating; each comes back once nothing is held (reference/mod-menu.md).</summary>
internal static class InputBlock
{
    private readonly record struct MapKey(int Player, ControllerType Type, int Controller, int Category, int Layout);

    private static readonly HashSet<MapKey> Disabled = new();
    private static readonly List<UnityEngine.EventSystems.EventSystem> Navigation = new();
    private static readonly List<ButtonInputListener> Listeners = new();
    private static int _holdsCancelled, _turnedBackOn;
    private static UnityEngine.GameObject _eventSelection;
    private static bool _open, _controllerMapHeld;
    private static string _failure;

    /// <summary>True while open and until the closing press is released.</summary>
    internal static bool Blocking => _open || Disabled.Count > 0 || Navigation.Count > 0 || Listeners.Count > 0 ||
        _controllerMapHeld;

    internal static void Begin()
    {
        _open = true;
        _holdsCancelled = _turnedBackOn = 0;
        Hold();
        Plugin.Log.LogInfo($"[Mod menu] game input paused: {Disabled.Count} controller maps off, UI navigation off: {Navigation.Count}, " +
            $"game controller map held: {_controllerMapHeld}, button listeners off: {Listeners.Count}");
    }

    /// <summary>Each frame while open: a map the game turns on meanwhile goes off too.</summary>
    internal static void Hold()
    {
        try { ForEachMap(HoldMap); }
        catch (Exception ex) { Report("maps", ex); }
        HoldNavigation();
        HoldControllerMap();
        HoldListeners();
        CancelButtonHold();
    }

    internal static void Close()
    {
        _open = false;
        if (_turnedBackOn > 0) Plugin.Log.LogInfo($"[Mod menu] maps turned back on by the game while open: {_turnedBackOn}");
        if (_holdsCancelled > 0) Plugin.Log.LogInfo($"[Mod menu] game button holds cancelled while open: {_holdsCancelled}");
    }

    /// <summary>Waiting for release keeps the closing press from reaching the game as a new press.</summary>
    internal static void EndWhenIdle()
    {
        if (_open || !Blocking) return;
        if (AnythingHeld()) { Hold(); return; }
        End();
    }

    internal static void End()
    {
        _open = false;
        foreach (var listener in Listeners)
        {
            try { if (!listener.WasCollected) listener.enabled = true; }
            catch (Exception ex) { Report("listener restore", ex); }
        }
        Listeners.Clear();
        foreach (var events in Navigation)
        {
            try { if (!events.WasCollected) events.sendNavigationEvents = true; }
            catch (Exception ex) { Report("navigation restore", ex); }
        }
        Navigation.Clear();
        RestoreEventSelection();
        RestoreMaps();
        RestoreControllerMap();
    }

    private static void HoldMap(Player player, ControllerMap map)
    {
        if (!map.enabled) return;
        map.enabled = false;
        if (!Disabled.Add(Key(player, map))) _turnedBackOn++;
    }

    // A change of controller mode or active pad makes Rewired replace that pad's maps, each copying the off state of the
    // one it replaces; the live map with the same player, controller, category and layout is the one turned back on.
    private static void RestoreMaps()
    {
        if (Disabled.Count == 0) return;
        int restored = 0;
        try
        {
            ForEachMap((player, map) =>
            {
                if (map.enabled || !Disabled.Contains(Key(player, map))) return;
                map.enabled = true;
                restored++;
            });
        }
        catch (Exception ex) { Report("map restore", ex); }
        Plugin.Log.LogInfo($"[Mod menu] game input resumed: {restored} of {Disabled.Count} controller maps on");
        Disabled.Clear();
    }

    // The game's controller maps read the pad through its template in a coroutine that ends, before reading, once
    // ControllerManager's current map is not itself. DefaultMap reads no buttons, so it stands in while open.
    private static void HoldControllerMap()
    {
        try
        {
            var manager = ControllerManager.instance;
            var standIn = ControllerMapsManager.defaultMap?.TryCast<IControllerMap>();
            if (!IsAlive(manager) || standIn is null || standIn.WasCollected) return;
            var current = manager._CurrentMap_k__BackingField;
            if (current is not null && !current.WasCollected && current.Pointer == standIn.Pointer) return;
            manager._CurrentMap_k__BackingField = standIn;
            _controllerMapHeld = true;
        }
        catch (Exception ex) { Report("controller map", ex); }
    }

    // The game chooses the map for whatever screen is open now; SetMap starts that map's coroutine again.
    private static void RestoreControllerMap()
    {
        if (!_controllerMapHeld) return;
        _controllerMapHeld = false;
        try
        {
            ControllerMapsManager.UpdateMap();
            Plugin.Log.LogInfo("[Mod menu] game controller map chosen again");
        }
        catch (Exception ex) { Report("map update", ex); }
    }

    // Buttons such as the landing screen's Play answer a key of their own (ButtonInputListener.key, read through Unity's
    // input, pad buttons included); disabled, they leave ButtonInputListenerManager until enabled again.
    private static void HoldListeners()
    {
        try
        {
            var registered = ButtonInputListenerManager._inputListeners;
            if (registered is null || registered.WasCollected) return;
            var held = new List<ButtonInputListener>();
            for (int index = 0; index < registered.Count; index++)
            {
                var listener = registered[index]?.TryCast<ButtonInputListener>();
                if (listener is not null && !listener.WasCollected && listener.enabled) held.Add(listener);
            }
            foreach (var listener in held)
            {
                listener.enabled = false;
                Listeners.Add(listener);
            }
        }
        catch (Exception ex) { Report("listeners", ex); }
    }

    // A menu panel with a hold action starts ControllerButtonHoldManager's hold on Cross before the selection is asked;
    // its update then vibrates the pad and plays the hold feedback. Resetting stops both and clears the hold.
    private static void CancelButtonHold()
    {
        try
        {
            var holds = ControllerButtonHoldManager.instance;
            if (holds is null || holds.WasCollected || holds._currentlyHeldTemplateButton is null) return;
            holds.ResetHoldingButton();
            _holdsCancelled++;
        }
        catch (Exception ex) { Report("button hold", ex); }
    }

    // Unity UI modules (the login screen reads the pad through Unity's own input) send no move, submit or cancel then.
    private static void HoldNavigation()
    {
        try
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events is null || events.WasCollected || !events.sendNavigationEvents) return;
            if (Navigation.Count == 0) _eventSelection = events.currentSelectedGameObject;
            events.sendNavigationEvents = false;
            Navigation.Add(events);
        }
        catch (Exception ex) { Report("navigation", ex); }
    }

    // The UI focus can still move while open (the landing screen went to Play offline); it goes back to where it was.
    private static void RestoreEventSelection()
    {
        var saved = _eventSelection;
        _eventSelection = null;
        try
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (saved is null || saved.WasCollected || !saved.activeInHierarchy || events is null || events.WasCollected) return;
            if (events.currentSelectedGameObject?.Pointer != saved.Pointer) events.SetSelectedGameObject(saved);
        }
        catch (Exception ex) { Report("focus restore", ex); }
    }

    private static MapKey Key(Player player, ControllerMap map) =>
        new(player.id, map.controllerType, map.controllerId, map.categoryId, map.layoutId);

    // Every player's keyboard, mouse and joystick maps, the system player included.
    private static void ForEachMap(Action<Player, ControllerMap> visit)
    {
        var players = ReInput.players;
        if (players is null) return;
        for (int index = 0; index < players.playerCount; index++) VisitPlayer(players.GetPlayer(index), visit);
        VisitPlayer(players.GetSystemPlayer(), visit);
    }

    private static void VisitPlayer(Player player, Action<Player, ControllerMap> visit)
    {
        if (player is null || player.WasCollected) return;
        var maps = player.controllers?.maps;
        if (maps is null) return;
        VisitMaps(player, maps.GetMaps(ControllerType.Keyboard, 0), visit);
        VisitMaps(player, maps.GetMaps(ControllerType.Mouse, 0), visit);
        var joysticks = player.controllers.Joysticks;
        var collection = joysticks?.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
        for (int index = 0; collection is not null && index < collection.Count; index++)
        {
            var joystick = joysticks[index];
            if (joystick is not null && !joystick.WasCollected) VisitMaps(player, maps.GetMaps(ControllerType.Joystick, joystick.id), visit);
        }
    }

    private static void VisitMaps(Player player, Il2CppSystem.Collections.Generic.IList<ControllerMap> maps,
        Action<Player, ControllerMap> visit)
    {
        int count = maps?.TryCast<Il2CppSystem.Collections.Generic.ICollection<ControllerMap>>()?.Count ?? 0;
        for (int index = 0; index < count; index++)
        {
            var map = maps[index];
            if (map is not null && !map.WasCollected) visit(player, map);
        }
    }

    private static void Report(string stage, Exception ex)
    {
        string failure = $"{stage}: {ex.GetType().Name}: {ex.Message}";
        if (_failure == failure) return;
        _failure = failure;
        Plugin.Log.LogWarning($"Mod menu input block unavailable ({failure})");
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
