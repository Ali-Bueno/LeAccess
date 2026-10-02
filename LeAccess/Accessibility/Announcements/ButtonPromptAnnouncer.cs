using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using UnityEngine;

namespace LeAccess.Accessibility.Announcements;

internal static class ButtonPromptAnnouncer
{
    private static readonly Dictionary<int, MultiButtonsActionPrompt> Prompts = new();
    private static readonly Dictionary<int, string> Visible = new();
    private static readonly Dictionary<int, string> Actions = new();
    private static readonly List<int> Expired = new();
    private static readonly PromptAnnouncementTracker Tracker = new();
    private static string _lastFailure;

    internal static void Track(MultiButtonsActionPrompt prompt)
    {
        try
        {
            if (prompt == null) return;
            Prompts[prompt.GetInstanceID()] = prompt;
        }
        catch (Exception) { }
    }

    internal static void Remove(MultiButtonsActionPrompt prompt)
    {
        try
        {
            if (prompt != null) Prompts.Remove(prompt.GetInstanceID());
        }
        catch (Exception) { }
    }

    internal static void Update(bool otherContentPending = false)
    {
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible)
            {
                Tracker.Reset();
                return;
            }

            var selected = NavigationManager.currentlySelected;
            if (selected?.WasCollected == true) selected = null;
            var selectedComponent = selected?.TryCast<Component>();
            var player = PlayerFinder.getPlayer();
            bool gameplay = player is not null && !player.WasCollected && player != null &&
                player.activeInHierarchy && GameplayInputState.OwnsInput();
            // Ground targeting and device changes must not narrate the persistent HUD hint bar.
            if (gameplay) { Tracker.Reset(); return; }
            long context = selected?.Pointer.ToInt64() ?? 0;
            Visible.Clear();
            Actions.Clear();
            Expired.Clear();
            foreach (var entry in Prompts)
            {
                try
                {
                    var prompt = entry.Value;
                    if (prompt.WasCollected || prompt == null)
                    {
                        Expired.Add(entry.Key);
                        continue;
                    }
                    var label = prompt._promptDescription;
                    if (!prompt.isActiveAndEnabled || !prompt.IsPromptActive() || label == null ||
                        !label.isActiveAndEnabled || label.alpha <= 0 || label.canvasRenderer.cull ||
                        !UiVisibility.IsVisible(label.gameObject)) continue;

                    bool belongsToSelection = selectedComponent != null &&
                        prompt.transform.IsChildOf(selectedComponent.transform);
                    // Global HUD hints can be initialized while the login menu is on screen.
                    if (!belongsToSelection)
                    {
                        var allowed = prompt._activePromptNavigables;
                        bool explicitlyOwned = selected is not null && allowed is not null && !allowed.WasCollected &&
                            !prompt._exceptSelectedNavigables && allowed.Contains(selected.GetNavigableID());
                        if (!explicitlyOwned) continue;
                    }

                    string text = InputBindingText.ReadPrompt(prompt);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Visible[entry.Key] = text;
                        Actions[entry.Key] = TextUtils.CleanText(label.text);
                    }
                }
                catch (Exception)
                {
                    Expired.Add(entry.Key);
                }
            }
            foreach (int id in Expired) Prompts.Remove(id);

            // Keep hints here until speech is idle; never leave obsolete hints queued in the reader.
            string announcement = Tracker.Observe(context, Visible,
                !ScreenContentAnnouncer.HasActiveContent && !DialogueAnnouncer.HasActiveContent && !otherContentPending &&
                !ScreenReader.SpokeThisFrame && !ScreenReader.IsSpeaking(), Actions);
            if (string.IsNullOrEmpty(announcement)) return;
            Plugin.Log.LogInfo($"[ButtonPrompt] {announcement}");
            ScreenReader.Speak(announcement, false);
        }
        catch (Exception ex)
        {
            Tracker.Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Button prompt feedback unavailable: {ex.Message}");
        }
    }

    internal static void Reset()
    {
        Prompts.Clear();
        Visible.Clear();
        Actions.Clear();
        Expired.Clear();
        Tracker.Reset();
        _lastFailure = null;
    }
}
