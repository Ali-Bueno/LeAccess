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
    internal static bool HasPendingContent => Campaign.HasPendingContent || Monolith.HasPendingContent || Stash.HasPendingContent;

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
        if (_enabled == null || !_enabled.Value) { Reset(); return; }
        try
        {
            if (LE.UI.LoadingScreen.IsVisible) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;
            if (!SceneList.TryGetNonDefaultSceneDetails(scene.name, out _)) { Reset(); return; }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            var player = PlayerFinder.player;
            if (!IsAlive(player) || !player.activeInHierarchy || player.scene.handle != scene.handle) return;
            var character = PlayerFinder.getPlayerData();
            if (character is null || character.WasCollected) return;
            if (_character != character.Pointer)
            { Reset(); _character = character.Pointer; _nextPoll = now + PollSeconds; }
            var provider = PlayerFinder.getLocalStatefulQuestListProvider();
            if (provider is null || provider.WasCollected) return;
            bool campaignReady = Observe(provider.getPlayerQuestListHolder(), Campaign);
            bool monolithReady = Observe(provider.getPlayerMonolithQuestListHolder(), Monolith);
            bool stashReady = Observe(provider.getPlayerStashQuestListHolder(), Stash);
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            bool typing = IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled;
            bool canSpeak = Application.isFocused && !NavigationAudioState.IsSuspended &&
                !typing && !otherContentActive && !higherPriorityPending && !ScreenReader.SpokeThisFrame &&
                ScreenReader.IsAvailable() && now >= _nextSpeech && GameplayInputState.OwnsInput();
            if (TryTake(canSpeak, stackalloc bool[] { campaignReady, monolithReady, stashReady }, out var change))
            {
                string text = Format(change);
                ScreenReader.Speak(text, interrupt: false);
                double interval = _speechInterval.Value;
                if (!double.IsFinite(interval) || interval <= 0) interval = NearestEnemyAnnouncementState.DefaultSpeechIntervalSeconds;
                _nextSpeech = now + interval;
                Plugin.Log.LogInfo($"[Quest update] {text}");
            }
        }
        catch (Exception ex)
        {
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Quest updates unavailable: {ex.Message}");
        }
    }

    private static bool TryTake(bool canSpeak, ReadOnlySpan<bool> ready, out QuestProgressChange change)
    {
        change = default;
        if (!canSpeak) return false;
        for (int offset = 0; offset < UpdateSources.Length; offset++)
        {
            int index = (_nextSource + offset) % UpdateSources.Length;
            if (!UpdateSources[index].TryTake(ready[index], out change)) continue;
            _nextSource = (index + 1) % UpdateSources.Length;
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
        _character = IntPtr.Zero; _nextPoll = 0; _nextSpeech = 0; _nextSource = 0;
    }

    internal static void Cleanup() { Reset(); _enabled = null; _speechInterval = null; _lastFailure = null; }
}
