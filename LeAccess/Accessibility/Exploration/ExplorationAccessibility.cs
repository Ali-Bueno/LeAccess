using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Exploration;

// Create + right-stick scanner of what the player has discovered (D2R exploration policy) and the
// Create+Cross beacon. See reference/exploration-mode.md.
internal static class ExplorationAccessibility
{
    private const string ConfigSection = "Exploration";
    private const double RefreshSeconds = 0.5;
    private const double StatusSeconds = 10;
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _seenRange;
    private static readonly ExplorationCatalog Catalog = new();
    private static readonly List<ExplorationEntry> Entries = new();
    private static readonly List<System.Numerics.Vector2> UnexploredPositions = new();
    private static MapSectionWorldSnapshot _world;
    private static bool _hasEntries;
    private static long _session;
    private static double _nextRefresh, _nextStatus;
    private static string _lastFailure, _sceneName, _cameFrom;
    private static ExplorationTarget? _lastTarget;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Scanner of discovered places, quest markers and nearby enemies (hold Create, move the right stick), and the beacon (hold Create, press Cross).");
        _seenRange = config.Bind(ConfigSection, "SeenRange", 20f, new ConfigDescription(
            "A place is listed once it has been this close (world units) and in walkable line of sight.",
            new AcceptableValueRange<float>(5f, 60f)));
        ExplorationInput.Initialize(config);
        ExplorationBeacon.Initialize(config);
        ExplorationFeedback.Initialize(config);
    }

    // Runs before VitalsAccessibility.Update, which reads this frame's Create+Square command.
    internal static void Update(bool worldContentActive)
    {
        if (_enabled == null) return;
        bool gameplay = false;
        try
        {
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            gameplay = !worldContentActive && !NavigationAudioState.IsSuspended && GameplayInputState.OwnsInput() &&
                !(IsAlive(input) && input.isInputFieldActive) && !(IsAlive(keyboard) && keyboard._enabled);
        }
        catch (Exception ex) { ReportFailure(ex.Message); }
        ExplorationInput.Update(gameplay);
        if (!_enabled.Value)
        {
            ExplorationBeacon.Update(false, null);
            ExplorationFeedback.Silence();
            return;
        }
        try
        {
            double now = Time.unscaledTimeAsDouble;
            var command = ExplorationInput.Command;
            if (ExplorationInput.MenuOpenedThisFrame) ExplorationBeacon.PlayMenuClick();
            bool scannerCommand = command is ExplorationCommand.CategoryPrevious or ExplorationCommand.CategoryNext or
                ExplorationCommand.EntryPrevious or ExplorationCommand.EntryNext or ExplorationCommand.Lock;
            if (gameplay && (now >= _nextRefresh || scannerCommand))
            {
                _nextRefresh = now + RefreshSeconds;
                Refresh(now);
            }
            if (gameplay && scannerCommand) Handle(command);
            ExplorationFeedback.Update(gameplay, now);
            string beaconText = ExplorationBeacon.Update(gameplay, LockedTarget());
            if (beaconText != null) Speak(beaconText, interrupt: false);
        }
        catch (Exception ex) { ReportFailure(ex.Message); }
    }

    private static void Refresh(double now)
    {
        _hasEntries = false;
        if (!MapSectionWorld.TryCapturePlayer(out var world)) return;
        if (world.Session != _session)
        {
            _session = world.Session;
            Catalog.Reset();
            _nextStatus = 0; // the status at arrival shows what the scanner starts with
        }
        _world = world;
        if (world.SceneName != _sceneName)
        {
            _cameFrom = _sceneName;
            _sceneName = world.SceneName;
        }
        ExplorationPersistence.Update(now, world);
        ExplorationBeacon.TryGetWalkability(out var nav);
        _hasEntries = ExplorationProbe.TryRefresh(world, nav, ExplorationBeacon.CurrentMap(world.Session),
            _seenRange.Value, now, _cameFrom, Entries);
        PublishUnexplored(now);
        ExplorationFeedback.Refreshed();
        if (ExplorationProbe.LastFailure != null) ReportFailure(ExplorationProbe.LastFailure);
        if (now < _nextStatus) return;
        _nextStatus = now + StatusSeconds;
        Plugin.Log.LogInfo($"[Exploration] scene={world.SceneName} {ExplorationProbe.LastStatus}");
    }

    // The radar marks openings toward listed unexplored areas within twice the explored radius.
    private static void PublishUnexplored(double now)
    {
        UnexploredPositions.Clear();
        foreach (var entry in Entries)
            if (entry.Category == ExplorationCategory.UnexploredRoom)
                UnexploredPositions.Add(new System.Numerics.Vector2(entry.Position.X, entry.Position.Z));
        RadarUnexploredMarks.Publish(UnexploredPositions, ExplorationBeacon.ExploreRadius * 2, now);
    }

    private static void Handle(ExplorationCommand command)
    {
        var camera = Camera.main;
        if (!_hasEntries || !IsAlive(camera) || !camera.isActiveAndEnabled)
        {
            Speak(ModText.Get("exploration.scanner.unavailable"), interrupt: true);
            return;
        }
        var player = new System.Numerics.Vector3((float)_world.X, (float)_world.Y, (float)_world.Z);
        var forward = GuidanceRouteProbe.Convert(camera.transform.forward);
        var right = GuidanceRouteProbe.Convert(camera.transform.right);
        // One step is the character's body width (D2R counts steps in its own footprint).
        float step = _world.BodyRadius > 0 ? (float)_world.BodyRadius * 2 : 1f;
        if (ExplorationBeacon.TryGetWalkability(out _)) Catalog.LevelHeight = ExplorationBeacon.LevelHeight;
        ExplorationText text = ModText.Format;
        string sentence = command switch
        {
            ExplorationCommand.CategoryPrevious => Catalog.StepCategory(-1, Entries, player, forward, right, step, text),
            ExplorationCommand.CategoryNext => Catalog.StepCategory(1, Entries, player, forward, right, step, text),
            ExplorationCommand.EntryPrevious => Catalog.StepEntry(-1, Entries, player, forward, right, step, text),
            ExplorationCommand.EntryNext => Catalog.StepEntry(1, Entries, player, forward, right, step, text),
            _ => Lock()
        };
        Speak(sentence, interrupt: true);
    }

    private static string Lock()
    {
        if (Catalog.Selected(Entries) is not { } entry)
            return ModText.Get(ExplorationInput.LeftStickIsModifier ? "exploration.scanner.no_selection_l3" : "exploration.scanner.no_selection");
        _lastTarget = new ExplorationTarget(entry.Key, entry.Name, entry.Position, entry.ArrivalRadius);
        return ExplorationBeacon.Lock(_lastTarget.Value);
    }

    // Latest position of the locked entry. A failed refresh keeps the last one; a place that briefly
    // cannot be interacted with keeps its position; a dead enemy or a completed objective is gone.
    private static ExplorationTarget? LockedTarget()
    {
        string key = ExplorationBeacon.LockedKey;
        var last = _lastTarget is { } previous && previous.Key == key ? _lastTarget : null;
        if (key == null) { _lastTarget = null; return null; }
        if (!_hasEntries) return last;
        foreach (var entry in Entries)
            if (entry.Key == key)
                return _lastTarget = new ExplorationTarget(entry.Key, entry.Name, entry.Position, entry.ArrivalRadius);
        return key.StartsWith("listener:", StringComparison.Ordinal) ? last : null;
    }

    private static void Speak(string text, bool interrupt)
    {
        if (string.IsNullOrWhiteSpace(text) || !ScreenReader.TrySpeak(text, interrupt)) return;
        Plugin.Log.LogInfo($"[Exploration] {text}");
    }

    private static void ReportFailure(string message)
    {
        if (message == null || message == _lastFailure) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Exploration unavailable: {message}");
    }

    internal static void Cleanup()
    {
        ExplorationInput.Cleanup();
        ExplorationPersistence.Cleanup();
        ExplorationFeedback.Cleanup();
        ExplorationBeacon.Cleanup();
        ExplorationProbe.Reset();
        Catalog.Reset();
        Entries.Clear();
        UnexploredPositions.Clear();
        RadarUnexploredMarks.Clear();
        _hasEntries = false;
        _lastTarget = null;
        _session = 0;
        _nextRefresh = _nextStatus = 0;
        _lastFailure = _sceneName = _cameFrom = null;
        _enabled = null;
        _seenRange = null;
    }
}
