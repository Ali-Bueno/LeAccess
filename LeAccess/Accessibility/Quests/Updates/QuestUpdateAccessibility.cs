using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class QuestUpdateAccessibility
{
    // Match the existing vitals reader's responsive 100 ms sampling budget.
    private const double PollSeconds = 0.1;
    private static readonly QuestUpdateState Campaign = new(), Monolith = new(), Stash = new();
    private static readonly QuestUpdateState[] UpdateSources = { Campaign, Monolith, Stash };
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<double> _speechInterval;
    private static double _nextPoll;
    private static double _nextSpeech;
    private static IntPtr _character;
    private static string _lastFailure;
    private static int _nextSource;
    private static bool _canOwnAnnouncement, _campaignReady, _monolithReady, _stashReady;
    private static bool _preferDynamic;
    internal static bool AutomaticUpdatesEnabled => _enabled is not null && _enabled.Value;
    internal static double MinimumUpdateIntervalSeconds
    {
        get
        {
            double interval = _speechInterval?.Value ?? NearestEnemyAnnouncementState.DefaultSpeechIntervalSeconds;
            return double.IsFinite(interval) && interval > 0 ? interval : NearestEnemyAnnouncementState.DefaultSpeechIntervalSeconds;
        }
    }
    internal static bool HasPendingContent => Campaign.HasPendingContent || Monolith.HasPendingContent || Stash.HasPendingContent;
    internal static bool HasPendingAnnouncement => _canOwnAnnouncement &&
        (Campaign.HasReadyContent && _campaignReady || Monolith.HasReadyContent && _monolithReady || Stash.HasReadyContent && _stashReady);
    private static bool ShouldYieldToDynamic => _preferDynamic && DynamicObjectiveAccessibility.HasPendingAnnouncement;
    internal static bool HasPriorityOverDynamic => HasPendingAnnouncement && !ShouldYieldToDynamic;
    internal static bool CanAnnounceAt(double now) => now >= _nextSpeech;
    internal static void RecordAnnouncement(double now, bool dynamic = false)
    { _nextSpeech = now + MinimumUpdateIntervalSeconds; _preferDynamic = !dynamic; }

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Quests", "AutomaticUpdates", true,
            "Announce new quests, changed current objectives and quest completion during gameplay.");
        _speechInterval = config.Bind("Quests", "MinimumUpdateIntervalSeconds",
            NearestEnemyAnnouncementState.DefaultSpeechIntervalSeconds,
            "Minimum time between automatic quest updates; intervening changes coalesce to the latest current objective.");
        Reset();
    }

    internal static void Update(bool otherContentActive = false, bool higherPriorityPending = false)
    {
        _canOwnAnnouncement = false;
        if (_enabled == null || !_enabled.Value) { Reset(); return; }
        try
        {
            bool gameplayAccess = QuestAnnouncementEligibility.HasGameplayAccess(otherContentActive, _character);
            _canOwnAnnouncement = gameplayAccess;
            if (LE.UI.LoadingScreen.IsVisible) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;
            if (!SceneList.TryGetNonDefaultSceneDetails(scene.name, out _)) { Reset(); return; }
            var provider = PlayerFinder.getLocalStatefulQuestListProvider();
            if (provider is null || provider.WasCollected) { _canOwnAnnouncement = false; return; }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            var player = Players.LocalPlayerSource.Player;
            if (!IsAlive(player) || !player.activeInHierarchy || player.scene.handle != scene.handle) return;
            var character = QuestAnnouncementEligibility.Character();
            if (character == IntPtr.Zero) return;
            if (_character != character)
            {
                Reset(); _character = character; _nextPoll = now + PollSeconds;
                gameplayAccess = QuestAnnouncementEligibility.HasGameplayAccess(otherContentActive, _character);
            }
            _canOwnAnnouncement = gameplayAccess;
            _campaignReady = Observe(provider.getPlayerQuestListHolder(), Campaign);
            _monolithReady = Observe(provider.getPlayerMonolithQuestListHolder(), Monolith);
            _stashReady = Observe(provider.getPlayerStashQuestListHolder(), Stash);
            bool canSpeak = _canOwnAnnouncement && !higherPriorityPending && !ScreenReader.SpokeThisFrame &&
                ScreenReader.IsAvailable() && CanAnnounceAt(now) && !ShouldYieldToDynamic;
            if (TryTake(canSpeak, stackalloc bool[] { _campaignReady, _monolithReady, _stashReady }, out var change, out int source))
            {
                string text = Format(change);
                if (!ScreenReader.TrySpeak(text, interrupt: false)) return;
                UpdateSources[source].Acknowledge(change);
                _nextSource = (source + 1) % UpdateSources.Length;
                RecordAnnouncement(now);
                Plugin.Log.LogInfo($"[Quest update] {text}");
            }
        }
        catch (Exception ex)
        {
            _canOwnAnnouncement = false;
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Quest updates unavailable: {ex.Message}");
        }
    }

    private static bool TryTake(bool canSpeak, ReadOnlySpan<bool> ready, out QuestProgressChange change, out int source)
    {
        change = default;
        source = -1;
        if (!canSpeak) return false;
        for (int offset = 0; offset < UpdateSources.Length; offset++)
        {
            int index = (_nextSource + offset) % UpdateSources.Length;
            if (!UpdateSources[index].TryTake(ready[index], out change, acknowledge: false)) continue;
            source = index;
            return true;
        }
        return false;
    }

    private static bool Observe(StatefulQuestListHolder holder, QuestUpdateState state)
    {
        if (!QuestUpdateProbe.TryRead(holder, out var snapshots)) return false;
        state.Observe(snapshots, holder.statefulQuestList.Pointer.ToInt64());
        return true;
    }

    internal static string Format(QuestProgressChange change)
    {
        string key = change.Kind switch
        {
            QuestUpdateKind.Started => "quest.update_started",
            QuestUpdateKind.Completed => "quest.update_completed",
            QuestUpdateKind.Failed => "quest.update_failed",
            _ => "quest.update_changed"
        };
        string text = ModText.Format(key, change.Quest.Title);
        return change.Quest.Status == QuestProgressStatus.Active && !string.IsNullOrWhiteSpace(change.Quest.Objectives)
            ? text + ". " + ModText.Format("quest.current_objectives", change.Quest.Objectives) : text;
    }

    internal static void Reset()
    {
        Campaign.Reset(); Monolith.Reset(); Stash.Reset();
        _canOwnAnnouncement = _campaignReady = _monolithReady = _stashReady = false;
        _preferDynamic = false;
        _character = IntPtr.Zero; _nextPoll = 0; _nextSpeech = 0; _nextSource = 0;
    }

    internal static void Cleanup() { Reset(); _enabled = null; _speechInterval = null; _lastFailure = null; }
}
