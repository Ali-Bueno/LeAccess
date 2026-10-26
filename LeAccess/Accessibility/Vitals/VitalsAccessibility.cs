using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Progression;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

internal static class VitalsAccessibility
{
    // D2R vitals_service.cpp uses a 100 ms sample interval.
    private const double PollSeconds = 0.1;
    private const int MaximumStep = 50;
    private static readonly VitalsAnnouncementState Announcements = new();
    private static ConfigEntry<bool> _automatic;
    private static ConfigEntry<KeyCode> _reportKey;
    private static ConfigEntry<bool> _controllerReport;
    private static ConfigEntry<bool> _potionChanges;
    private static ConfigEntry<bool> _lifeChanges;
    private static ConfigEntry<bool> _manaChanges;
    private static ConfigEntry<int> _lifeStep;
    private static ConfigEntry<int> _manaStep;
    private static ConfigEntry<bool> _lifeFirst;
    // The latest life reading for the low-life sound; -1 when unknown.
    private static int _lifePercent = -1;
    private static Steps _lifeSteps, _manaSteps;
    private static double _nextPoll;
    private static int _potions = -1;
    private static long _session;
    private static bool _previousGameplayInput;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _automatic = config.Bind("Vitals", "AutomaticAnnouncements", true,
            "Master switch for automatic life and mana announcements (percentage bands on loss and recovery, as in the unreleased Diablo 2 mod). AnnounceLifeChanges and AnnounceManaChanges choose which meters speak.");
        _lifeChanges = config.Bind("Vitals", "AnnounceLifeChanges", true,
            "Announce life when it crosses a band (needs AutomaticAnnouncements). The manual report always includes life.");
        _manaChanges = config.Bind("Vitals", "AnnounceManaChanges", true,
            "Announce mana when it crosses a band (needs AutomaticAnnouncements). The manual report always includes mana.");
        _lifeStep = config.Bind("Vitals", "LifeMinimumChange", 0,
            "Smallest drop or rise in life percent (0 to 50), since the last announced reading, that earns a new band announcement. 0 announces every band; critical life (10 percent and below) always speaks.");
        _manaStep = config.Bind("Vitals", "ManaMinimumChange", 0,
            "Smallest drop or rise in mana percent (0 to 50), since the last announced reading, that earns a new band announcement. 0 announces every band; a larger value means fewer mana announcements; empty mana always speaks.");
        _lifeFirst = config.Bind("Vitals", "LifeFirst", true,
            "Automatic life announcements cut any other speech, and for a moment other speech waits behind them instead of cutting them.");
        _lifeSteps = new Steps(config, "Life", VitalsSteps.Life);
        _manaSteps = new Steps(config, "Mana", VitalsSteps.Mana);
        LowLifeCue.Initialize(config);
        _reportKey = config.Bind("Vitals", "ReportKey", KeyCode.R,
            "Read life, mana, level and experience, potions and effects during gameplay. Use None to disable the keyboard shortcut.");
        _controllerReport = config.Bind("Vitals", "ReportWithControllerView", true,
            "Read life, mana, level and experience, potions and effects with the controller Create button held plus Square (the exploration menu). Disable to leave the controller unread.");
        _potionChanges = config.Bind("Vitals", "AnnouncePotionCharges", true,
            "Announce health potion charges when one is used or picked up. The report always includes them.");
        Reset();
    }

    internal static void Update(bool otherContentActive = false)
    {
        if (_automatic == null) return;
        Poll(otherContentActive);
        LowLifeCue.Update(_lifePercent);
    }

    private static void Poll(bool otherContentActive)
    {
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
            if (!typing && (gameplayInput || _previousGameplayInput) && VitalsInput.GoldPressed())
                Speak(GoldAndPointsText(), true);
            if (!typing && (gameplayInput || _previousGameplayInput) && VitalsInput.MinionsPressed())
                Speak(MinionHealthReport.Next(), true);
            _previousGameplayInput = gameplayInput;

            // The low-life sound needs readings even with spoken announcements off.
            if (!_automatic.Value && !report && !LowLifeCue.Wanted)
            { Announcements.Reset(); _session = 0; _potions = -1; _lifePercent = -1; return; }
            double now = Time.unscaledTimeAsDouble;
            if (!report && now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            if (!VitalsProbe.TryRead(out var snapshot))
            {
                Announcements.Reset();
                _session = 0;
                _potions = -1;
                _lifePercent = -1;
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
            _lifePercent = health;
            if (!_automatic.Value && !report)
            { Announcements.Reset(); _session = 0; _potions = -1; return; }
            var change = Announcements.Observe(health, mana, acknowledge: false, Policy());
            if (report)
            {
                string healthText = health < 0 ? ModText.Get("vitals.health_unavailable") : HealthText(health);
                string manaText = snapshot.ManaReplaced ? ModText.Get("vitals.mana_replaced")
                    : mana < 0 ? ModText.Get("vitals.mana_unavailable") : ManaText(mana);
                // During a Rage of Morditas encounter its standing follows life and mana.
                string encounter = Morditas.MorditasSpeechOwner.Status;
                if (Speak(Join(Join(Join(Join(Join(healthText, manaText), encounter), LevelText()), potionText), EffectsText()), true))
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
                // A potion count alone is a pickup line; a life or mana crossing goes before pickups.
                bool lifeFirst = _lifeFirst.Value && change.HealthPercent >= 0;
                if (Speak(Join(vitals, potionChanged ? potionText : null), change.Interrupt, pickup: vitals is null, lifeFirst))
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

    private static VitalsPolicy Policy() => new(_lifeChanges.Value, _manaChanges.Value,
        Math.Clamp(_lifeStep.Value, 0, MaximumStep), Math.Clamp(_manaStep.Value, 0, MaximumStep),
        _lifeSteps.Read(), _manaSteps.Read());

    // One meter's steps: every N percent, then every 10, 5 and 1 below the chosen percents (0 = never).
    private sealed class Steps
    {
        private readonly ConfigEntry<int> _every, _tens, _fives, _ones;

        internal Steps(ConfigFile config, string meter, VitalsSteps defaults)
        {
            string name = meter.ToLowerInvariant();
            _every = config.Bind("Vitals", meter + "Step", defaults.Every, new ConfigDescription(
                $"Announce {name} every this many percent (1, 5, 10 or 25), above the finer ranges below.",
                new AcceptableValueList<int>(Choices)));
            _tens = Below(config, meter + "Every10Below", defaults.TensBelow, $"Below this {name} percent, announce every 10 percent; 0 never.");
            _fives = Below(config, meter + "Every5Below", defaults.FivesBelow, $"Below this {name} percent, announce every 5 percent; 0 never.");
            _ones = Below(config, meter + "Every1Below", defaults.OnesBelow, $"Below this {name} percent, announce every 1 percent; 0 never.");
        }

        internal VitalsSteps Read() => new(_every.Value, _tens.Value, _fives.Value, _ones.Value);

        private static ConfigEntry<int> Below(ConfigFile config, string key, int value, string description) =>
            config.Bind("Vitals", key, value, new ConfigDescription(description, new AcceptableValueRange<int>(0, 100)));
    }

    /// <summary>The steps a meter can announce at (ModMenuCatalog.VitalSteps).</summary>
    internal static readonly int[] Choices = { 1, 5, 10, 25 };

    // The Triangle readout: gold, then unspent passive points, skills that can take a point and the zone.
    private static string GoldAndPointsText()
    {
        string gold = GoldReport.TryRead(out int amount) ? ModText.Format("gold.amount", amount) : ModText.Get("gold.unavailable");
        return Join(gold, ProgressionProbe.TryRead(out var snapshot) ? ProgressionState.PointsText(snapshot) : null);
    }

    private static string LevelText() => ProgressionProbe.TryRead(out var snapshot) ? ProgressionState.LevelText(snapshot) : null;

    private static string HealthText(int percent) => ModText.Format("vitals.health", percent);
    private static string ManaText(int percent) => ModText.Format("vitals.mana", percent);
    // Harmful effects are read only on request; they change too often in combat to announce.
    private static string EffectsText()
    {
        var effects = VitalsEffectsProbe.Read();
        if (VitalsEffectsProbe.LastFailure != null) Plugin.Log.LogWarning($"Vitals effects unavailable: {VitalsEffectsProbe.LastFailure}");
        return effects.Count == 0 ? null : ModText.Format("vitals.effects", string.Join(", ", effects));
    }

    private static string PotionText(VitalsSnapshot snapshot) => snapshot.Potions < 0 ? null
        : snapshot.MaxPotions > 0 ? ModText.Format("vitals.potions", snapshot.Potions, snapshot.MaxPotions)
        : ModText.Format("vitals.potions_count", snapshot.Potions);
    private static string Join(string health, string mana) => string.IsNullOrEmpty(health) ? mana
        : string.IsNullOrEmpty(mana) ? health : health + ". " + mana;

    private static bool Speak(string text, bool interrupt, bool pickup = false, bool lifeFirst = false)
    {
        bool spoken = lifeFirst ? ScreenReader.TrySpeakLife(text)
            : pickup ? ScreenReader.TrySpeakPickup(text) : ScreenReader.TrySpeakOverPickup(text, interrupt);
        if (!spoken) return false;
        Plugin.Log.LogInfo($"[Vitals] {text}");
        return true;
    }

    private static void Reset()
    {
        _lifePercent = -1;
        Announcements.Reset();
        _session = 0;
        _potions = -1;
        _nextPoll = 0;
        _previousGameplayInput = false;
    }

    internal static void Cleanup()
    {
        Reset();
        LowLifeCue.Cleanup();
        _automatic = null;
        _reportKey = null;
        _controllerReport = null;
        _potionChanges = null;
        _lifeChanges = _manaChanges = null;
        _lifeStep = _manaStep = null;
        _lastFailure = null;
        VitalsInput.Reset();
    }
}
