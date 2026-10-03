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
    private static ConfigEntry<bool> _potionChanges;
    private static double _nextPoll;
    private static int _potions = -1;
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
        _potionChanges = config.Bind("Vitals", "AnnouncePotionCharges", true,
            "Announce health potion charges when one is used or picked up. The report always includes them.");
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
            { Announcements.Reset(); _session = 0; _potions = -1; return; }
            double now = Time.unscaledTimeAsDouble;
            if (!report && now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            if (!VitalsProbe.TryRead(out var snapshot))
            {
                Announcements.Reset();
                _session = 0;
                _potions = -1;
                _previousGameplayInput = false;
                if (report) Speak(ModText.Get("vitals.unavailable"), true);
                return;
            }
            if (_session != snapshot.Session)
            {
                Announcements.Reset();
                _potions = -1;
                _session = snapshot.Session;
            }
            int potions = snapshot.Potions;
            // The first readable charge count, or any count while announcements are off, is a silent baseline.
            if (_potions < 0 || !_potionChanges.Value) _potions = potions;
            string potionText = PotionText(snapshot);
            int health = VitalsAnnouncementState.Percent(snapshot.Health, snapshot.MaxHealth);
            int mana = VitalsAnnouncementState.Percent(snapshot.Mana, snapshot.MaxMana);
            var change = Announcements.Observe(health, mana, acknowledge: false);
            if (report)
            {
                string healthText = health < 0 ? ModText.Get("vitals.health_unavailable") : HealthText(health);
                string manaText = snapshot.ManaReplaced ? ModText.Get("vitals.mana_replaced")
                    : mana < 0 ? ModText.Get("vitals.mana_unavailable") : ManaText(mana);
                if (Speak(Join(Join(healthText, manaText), potionText), true))
                {
                    Announcements.Acknowledge(health, mana);
                    _potions = potions;
                }
            }
            else if (_automatic.Value && (change.HasContent || potions >= 0 && potions != _potions))
            {
                bool potionChanged = potions >= 0 && potions != _potions;
                // Keep critical damage audible even with a non-pausing menu open.
                // Ordinary crossings and potion counts queue through the existing speech sink.
                string vitals = Join(change.HealthPercent < 0 ? null : HealthText(change.HealthPercent),
                    change.ManaPercent < 0 ? null : ManaText(change.ManaPercent));
                if (Speak(Join(vitals, potionChanged ? potionText : null), change.Interrupt))
                {
                    Announcements.Acknowledge(change.HealthPercent, change.ManaPercent);
                    if (potionChanged) _potions = potions;
                }
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
    private static string PotionText(VitalsSnapshot snapshot) => snapshot.Potions < 0 ? null
        : snapshot.MaxPotions > 0 ? ModText.Format("vitals.potions", snapshot.Potions, snapshot.MaxPotions)
        : ModText.Format("vitals.potions_count", snapshot.Potions);
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
        _potions = -1;
        _nextPoll = 0;
        _previousGameplayInput = false;
    }

    internal static void Cleanup()
    {
        Reset();
        _automatic = null;
        _reportKey = null;
        _controllerReport = null;
        _potionChanges = null;
        _lastFailure = null;
        VitalsInput.Reset();
    }
}
