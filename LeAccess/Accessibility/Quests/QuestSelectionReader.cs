using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal readonly record struct QuestSelection(string Key, string Summary, string Details)
{
    internal string Text => ScreenContentTracker.Combine(new[] { Summary, Details });
}

internal static class QuestSelectionReader
{
    internal static bool TryRead(QuestButtonGenerator generator, out QuestSelection selection)
    {
        selection = default;
        if (!IsAlive(generator) || !generator.isActiveAndEnabled || !UiVisibility.IsVisible(generator.gameObject)) return false;
        var panel = generator.questPanel;
        if (!IsAlive(panel) || !panel.isActiveAndEnabled || panel.questsHidden || !UiVisibility.IsVisible(panel.gameObject) ||
            !IsAlive(panel.buttonGenerator) || panel.buttonGenerator.Pointer != generator.Pointer) return false;
        var button = generator._selectedButton_k__BackingField;
        if (!IsAlive(button) || !button.isActiveAndEnabled || !UiVisibility.IsVisible(button.gameObject) ||
            !IsAlive(button.quest) || !IsAlive(button.buttonGenerator) || button.buttonGenerator.Pointer != generator.Pointer) return false;
        string title = ReadText(button.tmp, button.transform);
        if (string.IsNullOrWhiteSpace(title)) return false;
        var fragments = new List<string> { title, ReadText(button.chapterText, button.transform) };
        if (button.completed) fragments.Add(ModText.Get("quest.completed"));
        fragments.Add(ModText.Get(button.pinned ? "quest.pinned" : "quest.unpinned"));
        string key = $"{generator.Pointer}:{button.Pointer}:{button.quest.Pointer}";
        string summary = ScreenContentTracker.Combine(fragments);
        fragments.Clear();

        var info = generator.questInformation;
        if (IsAlive(info) && info.isActiveAndEnabled && UiVisibility.IsVisible(info.gameObject) &&
            IsAlive(info.allContent) && UiVisibility.IsVisible(info.allContent) &&
            IsAlive(info.quest) && info.quest.Pointer == button.quest.Pointer &&
            IsAlive(info.previousQuest) && info.previousQuest.Pointer == button.quest.Pointer &&
            ReadText(info.questName, info.transform) == title)
        {
            fragments.Add(ReadText(info.status, info.transform));
            fragments.Add(ReadText(info.details, info.transform));
            if (IsAlive(info.objectivesHolder) && info.objectives is not null && !info.objectives.WasCollected)
                foreach (var objective in info.objectives)
                {
                    if (!IsAlive(objective) || !objective.isActiveAndEnabled ||
                        !IsOwned(objective.gameObject, info.objectivesHolder)) continue;
                    string objectiveText = ReadText(objective.text, objective.transform);
                    if (string.IsNullOrWhiteSpace(objectiveText)) continue;
                    if (IsAlive(objective.check) && UiVisibility.IsVisible(objective.check))
                        objectiveText = ModText.Format("quest.objective_completed", objectiveText);
                    fragments.Add(objectiveText);
                }
            if (IsAlive(info.rewardsHolder) && info.rewards is not null && !info.rewards.WasCollected)
                foreach (var reward in info.rewards)
                    if (IsAlive(reward) && reward.isActiveAndEnabled && IsOwned(reward.gameObject, info.rewardsHolder.transform))
                        fragments.Add(ReadText(reward.text, reward.transform));
        }
        selection = new QuestSelection(key, summary, ScreenContentTracker.Combine(fragments));
        return true;
    }

    internal static bool IsOwned(GameObject element, Transform owner) =>
        IsAlive(element) && IsAlive(element.transform) && IsAlive(owner) &&
        element.transform.IsChildOf(owner) && UiVisibility.IsVisible(element);

    private static string ReadText(TextMeshProUGUI label, Transform owner) =>
        IsAlive(label) && label.isActiveAndEnabled && label.alpha > 0 &&
        (!IsAlive(label.canvasRenderer) || !label.canvasRenderer.cull) && IsOwned(label.gameObject, owner)
            ? TextUtils.CleanText(label.text) : null;
}
