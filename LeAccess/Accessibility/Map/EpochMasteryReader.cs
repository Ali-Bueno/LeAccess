using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Reads the world map's epoch mastery panel (Knowledge of Orobyss); see reference/waypoint-map.md.</summary>
internal static class EpochMasteryReader
{
    /// <summary>Guidance is the description or, once completed, the completion text with how to travel; the checklist lists each timeline point.</summary>
    internal static void Describe(EpochMasteryUI panel, out string guidance, out string checklist)
    {
        guidance = checklist = null;
        if (!IsAlive(panel) || !UiVisibility.IsVisible(panel.gameObject)) return;
        if (IsShown(panel.completionPanel))
        {
            var parts = PanelTexts(panel.completionPanel);
            parts.Add(ModText.Get("epoch_mastery.travel_hint"));
            guidance = ScreenContentTracker.Combine(parts);
            return;
        }
        if (!IsShown(panel.progressionPanel)) return;
        guidance = WaypointReader.ReadLabel(panel.masteryDescription);
        var points = new List<string>();
        var boxes = panel.progressionPointsCheckBoxes;
        for (int i = 0; boxes is not null && i < boxes.Length; i++)
        {
            var box = boxes[i];
            if (!IsAlive(box) || !UiVisibility.IsVisible(box.gameObject)) continue;
            string label = IsAlive(box.label) ? TextUtils.CleanText(box.label.text) : null;
            if (!string.IsNullOrWhiteSpace(label))
                points.Add(ModText.Format(box.IsComplete ? "epoch_mastery.point_done" : "epoch_mastery.point_missing", label));
        }
        checklist = ScreenContentTracker.Combine(points);
    }

    /// <summary>The completed panel focuses its acknowledge button; it reads the whole completion text.</summary>
    internal static bool TryRead(GameObject go, out string text)
    {
        text = null;
        var panel = go.GetComponentInParent<EpochMasteryUI>();
        if (!IsAlive(panel) || !IsShown(panel.completionPanel) || !IsAlive(panel.acknowledgeButton) ||
            !go.transform.IsChildOf(panel.acknowledgeButton.transform)) return false;
        text = ScreenContentTracker.Combine(PanelTexts(panel.completionPanel));
        return !string.IsNullOrWhiteSpace(text);
    }

    /// <summary>Cross on the focused completion panel travels at once, without a prompt.</summary>
    internal static void AnnounceSubmit(EpochMasteryUI panel, SubmitType type)
    {
        if (type == SubmitType.A_EX && IsAlive(panel) && IsShown(panel.completionPanel))
            ScreenReader.Speak(ModText.Get("waypoint.traveling"), interrupt: true);
    }

    private static bool IsShown(GameObject panel) => IsAlive(panel) && UiVisibility.IsVisible(panel);

    private static List<string> PanelTexts(GameObject panel)
    {
        var fragments = new List<UiReadingOrder.Fragment>();
        foreach (var label in panel.GetComponentsInChildren<TMP_Text>(false))
            if (TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text))
                fragments.Add(new UiReadingOrder.Fragment(label, TextUtils.CleanText(label.text)));
        return UiReadingOrder.Read(fragments);
    }
}
