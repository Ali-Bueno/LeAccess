using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Social;

/// <summary>Social panel feedback outside focus: pop-up introductions, social chat results, the unknown-username
/// error, typing echo in the username fields and the party-invite prompt.</summary>
/// <remarks>See reference/social-reading.md.</remarks>
internal static class SocialAccessibility
{
    private static readonly Dictionary<int, GameObject> Introduced = new();
    private static readonly ConcurrentQueue<string> Submitted = new();
    private static readonly Dictionary<int, string> FieldValues = new();
    private static readonly List<int> Expired = new();
    private static readonly ScreenContentTracker PromptTracker = new();
    private static FriendPostRequestsUI _social;
    private static string _result;
    private static int _resultFrame;
    private static GameObject _offeredWindow;
    private static bool _offeredResult;
    private static int _offeredElement;
    private static string _lastFailure;

    /// <summary>The Social panel the reader last saw; its windows and fields are polled from here.</summary>
    internal static void Observe(FriendPostRequestsUI social) => _social = social;

    /// <summary>The game posted a system chat line (SystemMessageService.SubmitSystemMessage).</summary>
    internal static void SystemMessageSubmitted(string content)
    {
        if (!string.IsNullOrWhiteSpace(content)) Submitted.Enqueue(content);
    }

    /// <summary>Text to lead this element's announcement: a pending social result and the pop-up it opens, if new.</summary>
    internal static string LeadFor(GameObject element)
    {
        _offeredWindow = null;
        _offeredResult = false;
        try
        {
            string intro = null;
            if (SocialWindows.TryGet(element, out var window, out var kind) &&
                !Introduced.ContainsKey(window.GetInstanceID()))
            {
                intro = SocialWindows.Intro(window, kind);
                _offeredWindow = window;
            }
            _offeredResult = _result != null &&
                (IsAlive(window) || IsAlive(_social) && element.transform.IsChildOf(_social.transform));
            if (intro == null && !_offeredResult) return null;
            _offeredElement = element.GetInstanceID();
            return ScreenContentTracker.Combine(new[] { _offeredResult ? _result : null, intro });
        }
        catch (Exception ex) { ReportFailure(ex); return null; }
    }

    /// <summary>The announcement carrying the lead was spoken.</summary>
    internal static void LeadSpoken(GameObject element)
    {
        if (!IsAlive(element) || element.GetInstanceID() != _offeredElement) return;
        if (IsAlive(_offeredWindow)) Introduced[_offeredWindow.GetInstanceID()] = _offeredWindow;
        if (_offeredResult) _result = null;
        _offeredWindow = null;
        _offeredResult = false;
    }

    internal static void Update()
    {
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) { PromptTracker.Reset(); return; }
            ForgetClosedWindows();
            UpdateResults();
            EchoFields();
            UpdatePrompt();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static void ForgetClosedWindows()
    {
        Expired.Clear();
        foreach (var entry in Introduced)
            if (!UiVisibility.IsVisible(entry.Value)) Expired.Add(entry.Key);
        foreach (int id in Expired) Introduced.Remove(id);
    }

    // A result waits one frame so the focus the game moves after it can carry it; otherwise it speaks alone.
    private static void UpdateResults()
    {
        if (_result != null && Time.frameCount > _resultFrame && ScreenReader.TrySpeak(_result, interrupt: false))
            _result = null;
        while (Submitted.TryDequeue(out string line))
            if (SocialChatLines.IsSocial(line, Localize)) Raise(TextUtils.CleanText(line));
        // The invite window logs a user-service error and stays open, silent, when the name is not found.
        if (SocialLog.TakeUserError() && IsAlive(_social) && UiVisibility.IsVisible(_social.partyInviteMenu))
            Raise(ModText.Get("social.user_not_found"));
    }

    private static void Raise(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _result = ScreenContentTracker.Combine(new[] { _result, text });
        _resultFrame = Time.frameCount;
        Plugin.Log.LogInfo($"[Social] {text}");
    }

    // Typed or erased characters, while a username window is open; opening it sets the baseline silently.
    private static void EchoFields()
    {
        if (!IsAlive(_social)) { FieldValues.Clear(); return; }
        Echo(_social.partyInviteMenu, SocialField.PartyInvite);
        Echo(_social.addFriendMenu, SocialField.AddFriend);
        Echo(_social.blockUserMenu, SocialField.Block);
    }

    private static void Echo(GameObject window, SocialField field)
    {
        if (!IsAlive(window)) return;
        int id = window.GetInstanceID();
        var input = window.activeInHierarchy ? SocialWindows.FieldOf(window, field) : null;
        if (!IsAlive(input) || !UiVisibility.IsVisible(input.gameObject)) { FieldValues.Remove(id); return; }
        string value = input.text ?? "";
        if (!FieldValues.TryGetValue(id, out string before)) { FieldValues[id] = value; return; }
        if (before == value) return;
        FieldValues[id] = value;
        string edit = SocialText.Edit(before, value);
        if (edit != null) ScreenReader.Speak(edit, interrupt: true);
    }

    // The game shows the prompt without focusing it; it speaks once per opening unless focus already carried it.
    private static void UpdatePrompt()
    {
        var prompt = SocialWindows.FindPrompt();
        if (!IsAlive(prompt)) return;
        var window = prompt.gameObject;
        int id = window.GetInstanceID();
        if (!window.activeInHierarchy || !UiVisibility.IsVisible(window)) { PromptTracker.Hide(id); return; }
        if (Introduced.ContainsKey(id)) return;
        string text = SocialWindows.PromptText(prompt);
        bool canSpeak = ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame;
        if (!PromptTracker.Observe(id, text, canSpeak, out string announcement, acknowledge: false) ||
            announcement == null) return;
        if (!ScreenReader.TrySpeak(announcement, interrupt: false)) return;
        PromptTracker.Acknowledge(id, announcement);
        Introduced[id] = window;
        Plugin.Log.LogInfo($"[Social] Party invite prompt: {announcement}; focus: {DescribeFocus()}");
    }

    // How a controller reaches the prompt is unverified; the log records who holds focus when it appears.
    private static string DescribeFocus()
    {
        var navigation = NavigationManager.currentlySelected;
        bool live = navigation is not null && !navigation.WasCollected;
        var owner = live ? navigation.TryCast<Component>() : null;
        var standard = live ? navigation.TryCast<Navigable>() : null;
        var selected = IsAlive(standard) ? standard.ActiveSelectable : null;
        return $"navigable={(IsAlive(owner) ? owner.gameObject.name : "none")}, " +
            $"selected={(IsAlive(selected) ? selected.gameObject.name : "none")}";
    }

    private static string Localize(string key) => Localization.TryGetText(key, out string text) ? text : null;

    internal static void Reset()
    {
        Introduced.Clear();
        while (Submitted.TryDequeue(out _)) { }
        FieldValues.Clear();
        PromptTracker.Reset();
        SocialWindows.Reset();
        _social = null;
        _result = null;
        _offeredWindow = null;
        _offeredResult = false;
        _lastFailure = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Social feedback unavailable: {ex}");
    }
}
