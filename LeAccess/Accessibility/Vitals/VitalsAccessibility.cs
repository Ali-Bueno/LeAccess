using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

internal static class VitalsAccessibility
{
    // D2R vitals_service.cpp uses a 100 ms sample interval.
    private const double PollSeconds = 0.1;
    private static readonly VitalsAnnouncementState Announcements = new();
    private static ConfigEntry<bool> _automatic;
    private static ConfigEntry<KeyCode> _reportKey;
    private static ConfigEntry<bool> _controllerReport;
    private static double _nextPoll;
    private static long _session;
    private static bool _previousGameplayInput;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _automatic = config.Bind("Vitals", "AutomaticAnnouncements", true,
            "Announce health and mana percentage bands on loss and recovery, as in D2R Access.");
        _reportKey = config.Bind("Vitals", "ReportKey", KeyCode.R,
            "Read health and mana percentages during gameplay. Use None to disable the keyboard shortcut.");
        _controllerReport = config.Bind("Vitals", "ReportWithControllerView", true,
            "Read health and mana with the controller Back/View button. Native game bindings remain active.");
        Reset();
    }

    internal static void Update(bool otherContentActive = false)
    {
        if (_automatic == null) return;
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible)
            { Reset(); return; }

            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            bool typing = IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled;
            bool gameplayInput = !typing && !otherContentActive && !NavigationAudioState.IsSuspended &&
                GameplayInputState.OwnsInput();
            // A native View binding may open a menu before this LateUpdate. Accept
            // that same edge if gameplay owned the preceding frame, never defer it.
            bool report = !typing && (gameplayInput || _previousGameplayInput) &&
                VitalsInput.ReportPressed(gameplayInput ? _reportKey.Value : KeyCode.None, _controllerReport.Value);
            _previousGameplayInput = gameplayInput;

            if (!_automatic.Value && !report)
            { Announcements.Reset(); _session = 0; return; }
            double now = Time.unscaledTimeAsDouble;
            if (!report && now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            if (!VitalsProbe.TryRead(out var snapshot))
            {
                Announcements.Reset();
                _session = 0;
                _previousGameplayInput = false;
                if (report) Speak(ModText.Get("vitals.unavailable"), true);
                return;
            }
            if (_session != snapshot.Session)
            {
                Announcements.Reset();
                _session = snapshot.Session;
            }
            int health = VitalsAnnouncementState.Percent(snapshot.Health, snapshot.MaxHealth);
            int mana = VitalsAnnouncementState.Percent(snapshot.Mana, snapshot.MaxMana);
            var change = Announcements.Observe(health, mana, acknowledge: false);
            if (report)
            {
                string healthText = health < 0 ? ModText.Get("vitals.health_unavailable") : HealthText(health);
                string manaText = snapshot.ManaReplaced ? ModText.Get("vitals.mana_replaced")
                    : mana < 0 ? ModText.Get("vitals.mana_unavailable") : ManaText(mana);
                if (Speak(Join(healthText, manaText), true)) Announcements.Acknowledge(health, mana);
            }
            else if (_automatic.Value && change.HasContent)
            {
                // Keep critical damage audible even with a non-pausing menu open.
                // Ordinary crossings queue through the existing speech sink.
                if (Speak(Join(change.HealthPercent < 0 ? null : HealthText(change.HealthPercent),
                    change.ManaPercent < 0 ? null : ManaText(change.ManaPercent)), change.Interrupt))
                    Announcements.Acknowledge(change.HealthPercent, change.ManaPercent);
            }
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Vitals feedback unavailable: {ex}");
        }
    }

    private static string HealthText(int percent) => ModText.Format("vitals.health", percent);
    private static string ManaText(int percent) => ModText.Format("vitals.mana", percent);
    private static string Join(string health, string mana) => string.IsNullOrEmpty(health) ? mana
        : string.IsNullOrEmpty(mana) ? health : health + ". " + mana;

    private static bool Speak(string text, bool interrupt)
    {
        if (!ScreenReader.TrySpeak(text, interrupt)) return false;
        Plugin.Log.LogInfo($"[Vitals] {text}");
        return true;
    }

    private static void Reset()
    {
        Announcements.Reset();
        _session = 0;
        _nextPoll = 0;
        _previousGameplayInput = false;
    }

    internal static void Cleanup()
    {
        Reset();
        _automatic = null;
        _reportKey = null;
        _controllerReport = null;
        _lastFailure = null;
        VitalsInput.Reset();
    }
}
