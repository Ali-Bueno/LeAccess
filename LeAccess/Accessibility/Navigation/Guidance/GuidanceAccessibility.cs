using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.MapSections;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal static class GuidanceAccessibility
{
    private static readonly List<GuidanceTarget> Targets = new();
    private static readonly GuidanceProgress Progress = new();
    private static ConfigEntry<bool> _enabled, _controller;
    private static ConfigEntry<KeyCode> _reportKey;
    private static long _session;
    private static int? _selected;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Orientation", "Enabled", true,
            "Report a tracked quest marker or a visible named exit on request, without moving the character.");
        _reportKey = config.Bind("Orientation", "ReportKey", KeyCode.F6,
            "Keyboard key for objective direction and progress during gameplay.");
        _controller = config.Bind("Orientation", "RightStickDown", true,
            "Push the right stick down from neutral to request objective direction during gameplay.");
        Reset();
    }

    internal static void Update(bool otherContentActive = false)
    {
        if (_enabled?.Value != true) { Reset(); return; }
        bool requested = false;
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            if (otherContentActive || NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput() ||
                IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled) return;
            requested = KeyPressed() || _controller.Value && MenuDetailInput.ReadGuidanceRequested();
            if (!requested) return;
            if (!ScreenReader.IsAvailable()) return;
            if (!MapSectionWorld.TryCapturePlayer(out var world)) { Reset(); Speak("guidance.unavailable"); return; }
            if (_session != world.Session) { Reset(); _session = world.Session; }
            var camera = Camera.main;
            if (!IsAlive(camera) || !camera.isActiveAndEnabled) { Speak("guidance.unavailable"); return; }
            Targets.Clear();
            if (!GuidanceTargetReader.TryRead(world, Targets))
            {
                _selected = null;
                Progress.Reset();
                if (GuidanceTargetReader.LastFailure != null) ReportFailure(GuidanceTargetReader.LastFailure);
                Speak("guidance.unavailable");
                return;
            }
            var position = new Vector3((float)world.X, (float)world.Y, (float)world.Z);
            if (!TrySelect(position, out var target))
            { _selected = null; Progress.Reset(); Speak("guidance.no_target"); return; }
            var route = GuidanceRouteProbe.Read(world, target.Position);
            string direction = GuidanceGeometry.Direction(GuidanceRouteProbe.Convert(route.Next - position),
                GuidanceRouteProbe.Convert(camera.transform.forward), GuidanceRouteProbe.Convert(camera.transform.right));
            bool nearMarker = float.IsFinite(route.BodyWidth) && route.BodyWidth > 0 &&
                route.Distance <= route.BodyWidth;
            string text;
            if (nearMarker) text = ModText.Format("guidance.near_marker", target.Name);
            else if (direction == null) text = ModText.Format("guidance.direction_unavailable", target.Name);
            else
            {
                text = ModText.Format(route.Complete ? "guidance.route" : "guidance.bearing",
                    target.Name, ModText.Get(direction), Math.Ceiling(route.Distance));
                if (!route.Complete)
                    text += ". " + ModText.Get(route.DirectOpen == false ? "guidance.direct_blocked" : "guidance.route_unverified");
            }
            string change = Progress.Observe(world.Session, target.Id, route.Complete, route.Distance,
                GuidanceRouteProbe.Convert(target.Position), route.BodyWidth, acknowledge: false);
            if (change != null && !nearMarker) text += ". " + ModText.Get(change);
            if (target.Kind == GuidanceTargetKind.Exit) text += ". " + ModText.Get("guidance.exit_unconfirmed");
            if (!ScreenReader.TrySpeak(text, interrupt: true)) return;
            Progress.Observe(world.Session, target.Id, route.Complete, route.Distance,
                GuidanceRouteProbe.Convert(target.Position), route.BodyWidth);
            Plugin.Log.LogInfo($"[Orientation] route={route.Complete} target={target.Id}: {text}");
        }
        catch (Exception ex)
        {
            Reset();
            ReportFailure(ex.Message);
            if (requested) Speak("guidance.unavailable");
        }
    }

    private static bool TrySelect(Vector3 player, out GuidanceTarget target)
    {
        target = default;
        bool found = false;
        float nearest = float.PositiveInfinity;
        foreach (var candidate in Targets)
        {
            float distance = (candidate.Position - player).sqrMagnitude;
            if (!float.IsFinite(distance) || string.IsNullOrWhiteSpace(candidate.Name)) continue;
            if (found && candidate.Priority > target.Priority) continue;
            if (found && candidate.Priority == target.Priority)
            {
                if (target.Id == _selected) continue;
                if (candidate.Id != _selected && (distance > nearest || distance == nearest && candidate.Id >= target.Id)) continue;
            }
            target = candidate;
            nearest = distance;
            found = true;
        }
        if (found) _selected = target.Id;
        return found;
    }

    private static bool KeyPressed() => _reportKey.Value != KeyCode.None &&
        !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
        !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) &&
        !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(_reportKey.Value);

    private static void Speak(string key) => ScreenReader.Speak(ModText.Get(key), interrupt: true);

    private static void ReportFailure(string message)
    {
        if (_lastFailure == message) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Objective orientation unavailable: {message}");
    }

    private static void Reset()
    {
        _session = 0;
        _selected = null;
        Targets.Clear();
        Progress.Reset();
        GuidanceRouteProbe.Reset();
    }

    internal static void Cleanup() { Reset(); _enabled = null; _reportKey = null; _controller = null; _lastFailure = null; }
}
