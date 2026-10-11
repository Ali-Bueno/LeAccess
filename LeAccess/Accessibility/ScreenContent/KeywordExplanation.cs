using System.Collections.Generic;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;

namespace LeAccess.Accessibility.ScreenContent;

/// <summary>The status-effect keyword text a tooltip shows only while Right Stick is held.</summary>
/// <remarks>See reference/tooltip-reading-audit.md (2026-10-10, user report).</remarks>
internal static class KeywordExplanation
{
    private static readonly HashSet<string> Logged = new();

    internal static bool Enabled { get; set; } = true;

    /// <summary>The lead-in and cleaned text, or null when disabled or empty; what the game returned is logged once per key.</summary>
    internal static string Describe(string key, string raw)
    {
        if (!Enabled) return null;
        string text = TextUtils.CleanText(raw);
        bool empty = string.IsNullOrWhiteSpace(text);
        if (Logged.Add(key)) Plugin.Log.LogInfo($"[Tooltips] keyword text: {(empty ? "empty" : text)} ({key})");
        return empty ? null : ScreenContentTracker.Combine(new[] { ModText.Get("node.more_info"), text });
    }

    internal static void Reset() => Logged.Clear();
}
