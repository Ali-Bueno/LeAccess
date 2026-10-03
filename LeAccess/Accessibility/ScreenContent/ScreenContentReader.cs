using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ScreenContent;

internal static class ScreenContentReader
{
    internal static string Read(GameObject root, TMP_Text header, TMP_Text body,
        Func<TMP_Text, string> readLabel = null, Func<Component, bool> include = null)
    {
        if (!UiVisibility.IsVisible(root)) return null;
        var fragments = new List<UiReadingOrder.Fragment>();
        var seen = new HashSet<int>();
        Add(root, header, fragments, seen, readLabel);
        Add(root, body, fragments, seen, readLabel);
        // Limit discovery to the popup that the game's display lifecycle identified.
        var labels = root.GetComponentsInChildren<TMP_Text>(false);
        if (labels != null && !labels.WasCollected)
            foreach (var label in labels)
                if (include == null || include(label)) Add(root, label, fragments, seen, readLabel);
        var legacyLabels = root.GetComponentsInChildren<Text>(false);
        if (legacyLabels != null && !legacyLabels.WasCollected)
            foreach (var label in legacyLabels)
                if (UiVisibility.IsVisibleGraphic(label) && !IsInputValue(label) && (include == null || include(label)) &&
                    seen.Add(label.GetInstanceID()))
                    fragments.Add(new(label, TextUtils.CleanText(label.text)));
        return ScreenContentTracker.Combine(UiReadingOrder.Read(fragments));
    }

    private static void Add(GameObject root, TMP_Text label, List<UiReadingOrder.Fragment> fragments,
        HashSet<int> seen, Func<TMP_Text, string> readLabel)
    {
        if (!TextUtils.IsVisibleLabel(label) || !label.transform.IsChildOf(root.transform) ||
            IsInputValue(label) || !seen.Add(label.GetInstanceID())) return;
        fragments.Add(new(label, TextUtils.CleanText(readLabel != null ? readLabel(label) : label.text)));
    }

    private static bool IsInputValue(Component label) =>
        IsAlive(label.GetComponentInParent<TMP_InputField>()) || IsAlive(label.GetComponentInParent<InputField>());
}
