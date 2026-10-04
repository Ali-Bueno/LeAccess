using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using LeAccess.Accessibility.Navigation;
using Rewired;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Diagnostics;

// Read-only research log for choosing exploration-mode buttons (reference/exploration-mode.md):
// the joystick's Rewired maps, and the game actions behind each controller press.
internal static class ControllerBindingLog
{
    private const string ConfigSection = "Diagnostics";
    private const float AxisPress = 0.5f;
    private const double RecheckSeconds = 1;
    private static ConfigEntry<bool> _enabled;
    private static readonly List<string> Mapped = new();
    private static readonly List<string> Fired = new();
    private static string _dumped, _enabledMaps, _lastFailure;
    private static double _nextCheck;

    internal static void Initialize(ConfigFile config)
    {
        // Renamed from ControllerBindingLog (default true) so the finished research stops logging every press.
        _enabled = config.Bind(ConfigSection, "ControllerInputLog", false,
            "Log the controller's game bindings and the game actions behind each controller press. Read-only research aid.");
    }

    internal static void Update()
    {
        if (_enabled?.Value != true || !ReInput.isReady) return;
        try
        {
            var manager = EpochInputManager.instance;
            var player = IsAlive(manager) ? manager.rewiredPlayer : null;
            if (player is null || player.WasCollected) return;
            var joysticks = player.controllers?.Joysticks;
            int count = Count(joysticks);
            double now = Time.unscaledTimeAsDouble;
            bool recheck = now >= _nextCheck;
            if (recheck) _nextCheck = now + RecheckSeconds;
            for (int index = 0; index < count; index++)
            {
                var joystick = joysticks[index];
                if (joystick is null || joystick.WasCollected) continue;
                if (recheck) Describe(joystick, Maps(player, joystick));
                LogPresses(player, joystick);
            }
            _lastFailure = null;
        }
        catch (Exception ex)
        {
            string failure = $"{ex.GetType().Name}: {ex.Message}";
            if (failure == _lastFailure) return;
            _lastFailure = failure;
            Plugin.Log.LogWarning($"[Controller bindings] unavailable: {failure}");
        }
    }

    private static void Describe(Joystick joystick, Il2CppSystem.Collections.Generic.IList<ControllerMap> maps)
    {
        var mapping = ReInput.mapping;
        int mapCount = Count(maps);
        string identity = $"{joystick.id}:{joystick.hardwareName}:{mapCount}";
        var enabled = new StringBuilder();
        for (int i = 0; i < mapCount; i++)
            if (maps[i] is { WasCollected: false } map && map.enabled)
                enabled.Append(enabled.Length == 0 ? "" : ", ").Append(Category(mapping, map));
        string enabledMaps = $"{identity}:{enabled}";
        if (identity != _dumped)
        {
            _dumped = identity;
            Plugin.Log.LogInfo($"[Controller bindings] joystick '{joystick.name}' hardware '{joystick.hardwareName}' maps={mapCount}");
            for (int i = 0; i < mapCount; i++)
            {
                var map = maps[i];
                if (map is null || map.WasCollected) continue;
                var text = new StringBuilder($"[Controller bindings] map category={Category(mapping, map)} layout={Layout(mapping, map)} enabled={map.enabled}");
                var elements = map.AllMaps;
                int elementCount = Count(elements);
                for (int e = 0; e < elementCount; e++)
                {
                    var element = elements[e];
                    if (element is null || element.WasCollected) continue;
                    text.Append("\n    ").Append(ElementName(element)).Append(" -> ").Append(ActionName(mapping, element.actionId));
                    if (!element.enabled) text.Append(" [disabled]");
                }
                Plugin.Log.LogInfo(text.ToString());
            }
        }
        if (enabledMaps == _enabledMaps) return;
        _enabledMaps = enabledMaps;
        string context = GameplayInputState.OwnsInput() ? "gameplay" : "menu";
        Plugin.Log.LogInfo($"[Controller bindings] enabled maps ({context}): {enabled}");
    }

    private static Il2CppSystem.Collections.Generic.IList<ControllerMap> Maps(Player player, Joystick joystick) =>
        player.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id);

    // Maps are read only when a press happens; per-frame work stays one pass over the elements.
    private static void LogPresses(Player player, Joystick joystick)
    {
        var buttons = joystick.Buttons;
        int buttonCount = Count(buttons);
        for (int i = 0; i < buttonCount; i++)
        {
            var button = buttons[i];
            if (button is null || button.WasCollected || !button.justPressed) continue;
            var identifier = button.elementIdentifier;
            LogPress(player, joystick, identifier?.name ?? $"button {i}", identifier?.id ?? -1, 0);
        }
        var axes = joystick.Axes;
        int axisCount = Count(axes);
        for (int i = 0; i < axisCount; i++)
        {
            var axis = axes[i];
            if (axis is null || axis.WasCollected) continue;
            float value = axis.value, previous = axis.valuePrev;
            if (!float.IsFinite(value) || !float.IsFinite(previous) ||
                Math.Abs(previous) >= AxisPress || Math.Abs(value) < AxisPress) continue;
            var identifier = axis.elementIdentifier;
            int sign = value > 0 ? 1 : -1;
            LogPress(player, joystick, $"{identifier?.name ?? $"axis {i}"} {(sign > 0 ? "+" : "-")}", identifier?.id ?? -1, sign);
        }
    }

    private static void LogPress(Player player, Joystick joystick, string name, int elementId, int sign)
    {
        var mapping = ReInput.mapping;
        var maps = Maps(player, joystick);
        Mapped.Clear();
        Fired.Clear();
        int mapCount = Count(maps);
        for (int i = 0; i < mapCount; i++)
        {
            var map = maps[i];
            if (map is null || map.WasCollected || !map.enabled) continue;
            var elements = map.AllMaps;
            int elementCount = Count(elements);
            for (int e = 0; e < elementCount; e++)
            {
                var element = elements[e];
                if (element is null || element.WasCollected || !element.enabled || element.elementIdentifierId != elementId) continue;
                if (sign != 0 && element.axisRange != AxisRange.Full &&
                    (element.axisContribution == Pole.Positive ? 1 : -1) != sign) continue;
                Mapped.Add($"{ActionName(mapping, element.actionId)} [{Category(mapping, map)}]");
            }
        }
        var actions = mapping.Actions;
        int actionCount = Count(actions);
        for (int i = 0; i < actionCount; i++)
        {
            var action = actions[i];
            if (action is not null && !action.WasCollected && player.GetButtonDown(action.id)) Fired.Add(action.name);
        }
        string context = GameplayInputState.OwnsInput() ? "gameplay" : "menu";
        Plugin.Log.LogInfo($"[Controller press] {name} ({context}): mapped=[{string.Join(", ", Mapped)}] fired=[{string.Join(", ", Fired)}]");
    }

    private static string ElementName(ActionElementMap element)
    {
        string name = element.elementIdentifierName;
        if (element.elementType != ControllerElementType.Axis || element.axisRange == AxisRange.Full) return name;
        return name + (element.axisContribution == Pole.Positive ? " +" : " -");
    }

    private static string ActionName(ReInput.MappingHelper mapping, int actionId)
    {
        var action = mapping.GetAction(actionId);
        return action is null || action.WasCollected ? $"action {actionId}" : $"{action.name} ({action.descriptiveName})";
    }

    private static string Category(ReInput.MappingHelper mapping, ControllerMap map)
    {
        var category = mapping.GetMapCategory(map.categoryId);
        return category is null || category.WasCollected ? $"category {map.categoryId}" : category.name;
    }

    private static string Layout(ReInput.MappingHelper mapping, ControllerMap map)
    {
        var layout = mapping.GetJoystickLayout(map.layoutId);
        return layout is null || layout.WasCollected ? $"layout {map.layoutId}" : layout.name;
    }

    private static int Count<T>(Il2CppSystem.Collections.Generic.IList<T> list) where T : Il2CppSystem.Object
    {
        if (list is null || list.WasCollected) return 0;
        var collection = list.TryCast<Il2CppSystem.Collections.Generic.ICollection<T>>();
        return collection is null || collection.WasCollected ? 0 : collection.Count;
    }

    internal static void Cleanup()
    {
        _dumped = _enabledMaps = _lastFailure = null;
        _nextCheck = 0;
        _enabled = null;
    }
}
