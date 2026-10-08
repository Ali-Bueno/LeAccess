using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Progression;

/// <summary>Speaks level-ups and new passive/skill points, and the level report on Create+Circle or Ctrl+L.</summary>
internal static class ProgressionAccessibility
{
    // Level-ups and HUD buttons last seconds; a quarter second is prompt and cheap.
    private const double PollSeconds = 0.25;
    private static readonly ProgressionState State = new();
    private static ConfigEntry<bool> _automatic;
    private static double _nextPoll;
    private static bool _previousGameplayInput;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _automatic = config.Bind("Progression", "AnnounceChanges", true,
            "Say the new level, new passive points and skills that can level up. The report (Create+Circle, Ctrl+L) always works.");
        Reset();
    }

    internal static void Update(bool otherContentActive)
    {
        if (_automatic == null) return;
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            bool typing = IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled;
            bool gameplayInput = !typing && !otherContentActive && !NavigationAudioState.IsSuspended &&
                GameplayInputState.OwnsInput();
            bool report = !typing && (gameplayInput || _previousGameplayInput) &&
                ExplorationInput.Command == ExplorationCommand.Status;
            _previousGameplayInput = gameplayInput;
            if (report) Report();
            if (!_automatic.Value) { State.Reset(); return; }

            double now = Time.unscaledTimeAsDouble;
            if (now >= _nextPoll)
            {
                _nextPoll = now + PollSeconds;
                if (ProgressionProbe.TryRead(out var snapshot)) State.Observe(snapshot);
                else State.Reset();
            }
            // Queued speech never interrupts; held back while a menu or dialogue owns the player's attention.
            if (gameplayInput && State.HasPending && ScreenReader.TrySpeak(State.Next, false))
            {
                Plugin.Log.LogInfo($"[Progression] {State.Next}");
                State.Acknowledge();
            }
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Progression feedback unavailable: {ex}");
        }
    }

    private static void Report()
    {
        if (!ProgressionProbe.TryRead(out var snapshot))
        {
            if (ProgressionProbe.LastFailure != null) Plugin.Log.LogWarning($"Progression unavailable: {ProgressionProbe.LastFailure}");
            ScreenReader.TrySpeak(ModText.Get("progression.unavailable"), true);
            return;
        }
        string text = ProgressionState.StatusText(snapshot);
        if (ScreenReader.TrySpeak(text, true))
            Plugin.Log.LogInfo($"[Progression] {text} (xp {snapshot.Experience}/{snapshot.NextExperience})");
    }

    private static void Reset()
    {
        State.Reset();
        _nextPoll = 0;
        _previousGameplayInput = false;
    }

    internal static void Cleanup()
    {
        Reset();
        _automatic = null;
        _lastFailure = null;
    }
}
