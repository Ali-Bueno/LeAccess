using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EHG.Multiplayer;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

// On-demand scan of nearby hostiles; see reference/enemy-scan.md.
internal static class EnemyScanAccessibility
{
    private const string ConfigSection = "Enemy scan";
    private static readonly List<EnemyContact> Contacts = new();
    private static ConfigEntry<bool> _enabled, _controller;
    private static ConfigEntry<KeyCode> _reportKey;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Report the count, direction and distance of nearby hostile enemies on request during gameplay.");
        _reportKey = config.Bind(ConfigSection, "ReportKey", KeyCode.F7,
            "Keyboard key for the enemy scan during gameplay.");
        _controller = config.Bind(ConfigSection, "RightStickUp", true,
            "Push the right stick up from neutral to request the enemy scan during gameplay.");
    }

    // stickUp is the right-stick-up gesture, read once per frame by the caller.
    internal static void Update(bool otherContentActive, bool stickUp)
    {
        if (_enabled?.Value != true) return;
        bool requested = false;
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) return;
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            if (otherContentActive || NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput() ||
                IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled) return;
            requested = MenuDetailInput.KeyPressed(_reportKey.Value) || _controller.Value && stickUp;
            if (!requested || !ScreenReader.IsAvailable()) return;
            var camera = Camera.main;
            float range = ControllerTargetingParadigm._SkillTargetMaxDistance;
            Contacts.Clear();
            if (!IsAlive(camera) || !camera.isActiveAndEnabled || !float.IsFinite(range) || range <= 0 ||
                !EnemyScanProbe.TryRead(GameplayEnvironment.IsOnlineClient, range, Contacts, out var origin))
            {
                Speak("enemy_scan.unavailable");
                return;
            }
            if (EnemyScanProbe.LastFailure != null) ReportFailure(EnemyScanProbe.LastFailure);
            string text = EnemyScanReport.Build(Contacts, origin, GuidanceRouteProbe.Convert(camera.transform.forward),
                GuidanceRouteProbe.Convert(camera.transform.right), ModText.Format);
            if (!ScreenReader.TrySpeak(text, interrupt: true)) return;
            Plugin.Log.LogInfo($"[Enemy scan] {text}");
        }
        catch (Exception ex)
        {
            ReportFailure(ex.Message);
            if (requested) Speak("enemy_scan.unavailable");
        }
        finally { Contacts.Clear(); }
    }

    private static void Speak(string key) => ScreenReader.Speak(ModText.Get(key), interrupt: true);

    private static void ReportFailure(string message)
    {
        if (_lastFailure == message) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Enemy scan unavailable: {message}");
    }

    internal static void Cleanup() { Contacts.Clear(); _enabled = null; _reportKey = null; _controller = null; _lastFailure = null; }
}
