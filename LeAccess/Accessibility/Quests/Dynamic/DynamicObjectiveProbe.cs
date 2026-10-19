using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Morditas;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal readonly record struct DynamicObjectiveContent(int Id, string Text);

internal static class DynamicObjectiveProbe
{
    internal static void Read(Scene scene, List<DynamicObjectiveContent> content)
    {
        content.Clear();
        ReadTracker(scene, content);
        // The Morditas reader speaks these instructions itself while it follows the encounter.
        if (!MorditasSpeechOwner.Active) ReadMorditas(scene, content);
    }

    private static void ReadTracker(Scene scene, List<DynamicObjectiveContent> content)
    {
        var tracker = QuestTrackerUI.instance;
        if (!Visible(tracker) || !tracker.initialised || tracker.currentScene != scene.name ||
            !Owned(tracker.questHolder, tracker.transform)) return;
        var rows = tracker.questsDisplayed;
        if (rows is null || rows.WasCollected) return;
        foreach (var row in rows)
        {
            if (!Visible(row) || !Owned(row.gameObject, tracker.questHolder.transform) ||
                row.quest is not null || !row.TracksMonolithObjective && !row.TracksMonolithModifiers) continue;
            var fragments = new List<UiReadingOrder.Fragment>();
            Add(row.questName, row.transform, fragments);
            Add(row.chapterText, row.transform, fragments);
            var objectives = row.objectivesDisplayed;
            if (objectives is null || objectives.WasCollected) continue;
            int headings = fragments.Count;
            foreach (var objective in objectives)
            {
                if (!Visible(objective) || !Owned(objective.gameObject, row.transform)) continue;
                Add(objective.text, objective.transform, fragments);
            }
            if (fragments.Count == headings) continue;
            content.Add(new(row.GetInstanceID(), ScreenContentTracker.Combine(UiReadingOrder.Read(fragments))));
        }
    }

    private static void ReadMorditas(Scene scene, List<DynamicObjectiveContent> content)
    {
        var tracker = RageOfMorditasQuestTrackerUI.Instance;
        if (!Visible(tracker) || !tracker._encounterScene.isLoaded || tracker._encounterScene.handle != scene.handle) return;
        var current = tracker._currentObjective;
        var active = tracker._activeObjectives;
        if (!Visible(current) || !Owned(current.gameObject, tracker.transform) || active is null || active.WasCollected) return;
        bool listed = false;
        foreach (var objective in active)
            if (IsAlive(objective) && objective.Pointer == current.Pointer) { listed = true; break; }
        if (!listed || !TextUtils.IsVisibleLabel(current.text) || !Owned(current.text.gameObject, current.transform) ||
            string.IsNullOrWhiteSpace(TextUtils.CleanText(current.text.text))) return;
        var fragments = new List<UiReadingOrder.Fragment>();
        if (Visible(tracker.questHeader) && Owned(tracker.questHeader.gameObject, tracker.transform))
            Add(tracker.questHeader.questName, tracker.questHeader.transform, fragments);
        Add(current.text, current.transform, fragments);
        content.Add(new(tracker.GetInstanceID(), ScreenContentTracker.Combine(UiReadingOrder.Read(fragments))));
    }

    private static void Add(TMP_Text text, Transform owner, List<UiReadingOrder.Fragment> fragments)
    {
        if (TextUtils.IsVisibleLabel(text) && Owned(text.gameObject, owner))
        {
            string clean = TextUtils.CleanText(text.text);
            if (!string.IsNullOrWhiteSpace(clean)) fragments.Add(new(text, clean));
        }
    }

    private static bool Owned(GameObject element, Transform owner) =>
        IsAlive(element) && IsAlive(element.transform) && IsAlive(owner) &&
        element.transform.IsChildOf(owner) && UiVisibility.IsVisible(element);

    private static bool Visible(Behaviour component) =>
        IsAlive(component) && component.isActiveAndEnabled && UiVisibility.HasVisiblePresentation(component.gameObject);
}
