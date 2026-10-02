using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LE.Factions;
using LE.UI.Generic.UnityUI;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ScreenContent;

internal static class ScreenContentAnnouncer
{
    private static readonly Dictionary<int, Source> Sources = new();
    private static readonly List<int> Expired = new();
    private static readonly ScreenContentTracker Tracker = new();
    private static long _sequence;
    private static string _lastFailure;
    internal static bool HasPendingContent { get; private set; }
    // Tutorial callouts remain readable while gameplay and controller menus keep their own input.
    internal static bool HasActiveContent => FindForeground() != null;

    internal static bool OwnsFocus(GameObject element)
    {
        try
        {
            var foreground = FindForeground();
            return foreground != null && IsAlive(element) && IsAlive(element.transform) &&
                IsAlive(foreground.Root.transform) && element.transform.IsChildOf(foreground.Root.transform);
        }
        catch (Exception) { return false; }
    }

    internal static void TrackTutorial(TutorialManager tutorial)
    {
        try
        {
            if (IsAlive(tutorial) && IsAlive(tutorial.tutorialTooltip))
                Track(tutorial.tutorialTooltip.gameObject, tutorial.titleTMP, tutorial.bodyTMP,
                    readText: () => TutorialTextReader.Read(tutorial), blocksInput: false);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void RemoveTutorial(TutorialManager tutorial)
    {
        try
        {
            if (IsAlive(tutorial) && IsAlive(tutorial.tutorialTooltip)) Remove(tutorial.tutorialTooltip.gameObject);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void TrackPopup(Component popup)
    {
        try
        {
            if (!IsAlive(popup)) return;
            var confirmation = popup.GetComponentInParent<ConfirmationPanel>();
            if (IsAlive(confirmation))
            {
                Track(confirmation.gameObject, confirmation.HeaderText, confirmation.BodyText);
                return;
            }
            var choice = popup.GetComponentInParent<BinaryChoiceModalWindow>();
            if (IsAlive(choice))
            {
                Track(choice.gameObject, choice.headerText, choice.contentText);
                return;
            }
            Track(popup.gameObject);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Watch(GameObject root) => Track(root, retainWhenHidden: true);

    internal static void TrackTerms(LE.Tos.TosPanel panel)
    {
        try
        {
            if (!IsAlive(panel)) return;
            Track(panel.gameObject, readText: () =>
            {
                if (!IsAlive(panel)) return null;
                var content = panel._displayedContent;
                if (content is null || content.WasCollected) return null;
                string body = Utils.TextUtils.CleanText(content.Content);
                if (string.IsNullOrWhiteSpace(body)) return null;
                return ScreenContentTracker.Combine(new[] { Utils.TextUtils.CleanText(panel.Title), body });
            });
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Track(GameObject root, TMP_Text header = null, TMP_Text body = null,
        bool retainWhenHidden = false, Func<string> readText = null, bool blocksInput = true)
    {
        try
        {
            if (!IsAlive(root)) return;
            int id = root.GetInstanceID();
            if (Sources.TryGetValue(id, out var existing))
            {
                if (IsAlive(header)) existing.Header = header;
                if (IsAlive(body)) existing.Body = body;
                existing.RetainWhenHidden |= retainWhenHidden;
                existing.BlocksInput |= blocksInput;
                if (readText != null) existing.ReadText = readText;
                return;
            }
            var descendants = new List<int>();
            foreach (var other in Sources.Values)
            {
                if (!IsAlive(other.Root) || !IsAlive(other.Root.transform) || !IsAlive(root.transform)) continue;
                IsSourceVisible(other);
                if (root.transform.IsChildOf(other.Root.transform)) return;
                if (other.Root.transform.IsChildOf(root.transform)) descendants.Add(other.Id);
            }
            foreach (int descendant in descendants) { Sources.Remove(descendant); Tracker.Hide(descendant); }
            Sources[id] = new Source
            {
                Id = id, Root = root, Header = header, Body = body,
                ReadText = readText,
                OpenedFrame = Time.frameCount, Order = ++_sequence, RetainWhenHidden = retainWhenHidden,
                BlocksInput = blocksInput
            };
            HasPendingContent = true;
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void RemovePopup(Component popup)
    {
        try
        {
            if (!IsAlive(popup)) return;
            var confirmation = popup.GetComponentInParent<ConfirmationPanel>();
            if (IsAlive(confirmation)) { Remove(confirmation.gameObject); return; }
            var choice = popup.GetComponentInParent<BinaryChoiceModalWindow>();
            Remove(IsAlive(choice) ? choice.gameObject : popup.gameObject);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Remove(GameObject root)
    {
        try
        {
            if (!IsAlive(root)) return;
            int id = root.GetInstanceID();
            if (Sources.TryGetValue(id, out var source) && source.RetainWhenHidden)
                source.WasVisible = false;
            else Sources.Remove(id);
            Tracker.Hide(id);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Update()
    {
        HasPendingContent = false;
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible)
        {
            Tracker.Reset();
            return;
        }
        Expired.Clear();
        Source foreground = null;
        foreach (var pair in Sources)
        {
            try
            {
                var source = pair.Value;
                if (!IsAlive(source.Root))
                {
                    Expired.Add(pair.Key);
                    Tracker.Hide(pair.Key);
                    continue;
                }
                if (!IsSourceVisible(source))
                {
                    if (!source.RetainWhenHidden && !source.Root.activeInHierarchy &&
                        Time.frameCount > source.OpenedFrame) Expired.Add(pair.Key);
                    Tracker.Hide(pair.Key);
                    continue;
                }
                if (foreground == null || source.BlocksInput && !foreground.BlocksInput ||
                    source.BlocksInput == foreground.BlocksInput && source.Order > foreground.Order) foreground = source;
            }
            catch (Exception ex)
            {
                Expired.Add(pair.Key);
                ReportFailure(ex);
            }
        }
        foreach (int id in Expired) { Sources.Remove(id); Tracker.Hide(id); }
        if (foreground == null) return;
        if (Time.frameCount <= foreground.OpenedFrame)
        {
            HasPendingContent = true;
            return;
        }
        try
        {
            string text = foreground.ReadText != null ? foreground.ReadText() :
                ScreenContentReader.Read(foreground.Root, foreground.Header, foreground.Body);
            HasPendingContent = Tracker.Observe(foreground.Id, text,
                ScreenReader.IsAvailable(), out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[ScreenContent] {announcement}");
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static Source FindForeground()
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) return null;
        Source foreground = null;
        foreach (var source in Sources.Values)
        {
            try
            {
                if (!source.BlocksInput || !IsSourceVisible(source)) continue;
                if (foreground == null || source.Order > foreground.Order) foreground = source;
            }
            catch (Exception) { }
        }
        return foreground;
    }

    private static bool IsSourceVisible(Source source)
    {
        bool visible = IsAlive(source.Root) && UiVisibility.IsVisible(source.Root);
        if (source.RetainWhenHidden)
        {
            // Awake registers the exact tooltip once; visibility, not registration, determines opening order.
            if (visible && !source.WasVisible)
            {
                source.OpenedFrame = Time.frameCount;
                source.Order = ++_sequence;
                HasPendingContent = true;
            }
            if (!visible) Tracker.Hide(source.Id);
            source.WasVisible = visible;
        }
        return visible;
    }

    internal static void Reset()
    {
        Sources.Clear();
        Expired.Clear();
        Tracker.Reset();
        HasPendingContent = false;
        _sequence = 0;
        _lastFailure = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Screen content feedback unavailable: {ex.Message}");
    }

    private sealed class Source
    {
        internal int Id;
        internal GameObject Root;
        internal TMP_Text Header;
        internal TMP_Text Body;
        internal Func<string> ReadText;
        internal int OpenedFrame;
        internal long Order;
        internal bool RetainWhenHidden;
        internal bool WasVisible;
        internal bool BlocksInput;
    }
}
