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
    internal static string Read(GameObject root, TMP_Text header, TMP_Text body)
    {
        var fragments = new List<string>();
        Add(header, fragments);
        Add(body, fragments);
        // Limit discovery to the popup that the game's display lifecycle identified.
        var labels = root.GetComponentsInChildren<TMP_Text>(false);
        if (labels != null && !labels.WasCollected)
            foreach (var label in labels) Add(label, fragments);
        var legacyLabels = root.GetComponentsInChildren<Text>(false);
        if (legacyLabels != null && !legacyLabels.WasCollected)
            foreach (var label in legacyLabels)
                if (IsAlive(label) && label.isActiveAndEnabled && label.color.a > 0 &&
                    UiVisibility.IsVisible(label.gameObject) && !IsInputValue(label))
                    fragments.Add(TextUtils.CleanText(label.text));
        return ScreenContentTracker.Combine(fragments);
    }

    private static void Add(TMP_Text label, List<string> fragments)
    {
        if (!IsAlive(label) || !label.isActiveAndEnabled || label.alpha <= 0 ||
            !UiVisibility.IsVisible(label.gameObject) || IsInputValue(label)) return;
        var renderer = label.canvasRenderer;
        if (IsAlive(renderer) && renderer.cull) return;
        fragments.Add(TextUtils.CleanText(label.text));
    }

    private static bool IsInputValue(Component label) =>
        IsAlive(label.GetComponentInParent<TMP_InputField>()) || IsAlive(label.GetComponentInParent<InputField>());
}
