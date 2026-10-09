using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Map;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal readonly record struct QuestSelection(string Key, string Summary, string Objectives, string Details)
{
    internal string Text => ScreenContentTracker.Combine(new[] { Summary, Objectives, Details });
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
            !IsAlive(button.buttonGenerator) || button.buttonGenerator.Pointer != generator.Pointer) return false;
        string title = ReadText(button.tmp, button.transform);
        if (string.IsNullOrWhiteSpace(title)) return false;
        // The first entry, Knowledge of Orobyss, has no quest: selecting it shows the epoch mastery panel instead.
        if (button.isKnowledgeOfOrobyssQuest)
        {
            EpochMasteryReader.Describe(button.epochMasteryUI, out string guidance, out string checklist);
            selection = new QuestSelection($"{generator.Pointer}:{button.Pointer}:orobyss", title, guidance, checklist);
            return true;
        }
        if (!IsAlive(button.quest)) return false;
        string key = $"{generator.Pointer}:{button.Pointer}:{button.quest.Pointer}";
        var info = generator.questInformation;
        bool hasModel = QuestModelReader.TryRead(button.quest, info, out string state,
            out string currentObjectives, out string modelDetails);
        var summary = new List<string> { title, hasModel ? state : button.completed ? ModText.Get("quest.completed") : null,
            ReadText(button.chapterText, button.transform), ModText.Get(button.pinned ? "quest.pinned" : "quest.unpinned") };
        var fragments = new List<string> { modelDetails };
        var objectives = new List<string> { currentObjectives };
        var history = new List<UiReadingOrder.Fragment>();
        var objectiveRows = new List<UiReadingOrder.Fragment>();
        var rewardRows = new List<UiReadingOrder.Fragment>();
        string fallbackState = null;
        if (IsAlive(info) && info.isActiveAndEnabled && UiVisibility.IsVisible(info.gameObject) &&
            IsAlive(info.allContent) && UiVisibility.IsVisible(info.allContent) &&
            IsAlive(info.quest) && info.quest.Pointer == button.quest.Pointer &&
            IsAlive(info.previousQuest) && info.previousQuest.Pointer == button.quest.Pointer &&
            ReadText(info.questName, info.transform) == title)
        {
            if (!hasModel)
            {
                if (!button.completed) fallbackState = ReadText(info.status, info.transform);
                fragments.Add(ReadText(info.details, info.transform));
            }
            if (!hasModel && IsAlive(info.objectivesHolder) && info.objectives is not null && !info.objectives.WasCollected)
                foreach (var objective in info.objectives)
                {
                    if (!IsAlive(objective) || !objective.isActiveAndEnabled ||
                        !IsOwned(objective.gameObject, info.objectivesHolder)) continue;
                    string objectiveText = ReadText(objective.text, objective.transform);
                    if (string.IsNullOrWhiteSpace(objectiveText)) continue;
                    if (IsAlive(objective.check) && UiVisibility.IsVisible(objective.check))
                        history.Add(new(objective.text, ModText.Format("quest.objective_completed", objectiveText)));
                    else if (!button.completed) objectiveRows.Add(new(objective.text, objectiveText));
                }
            objectives.AddRange(UiReadingOrder.Read(objectiveRows));
            if (history.Count != 0)
                fragments.Add(ModText.Format("quest.objective_history", ScreenContentTracker.Combine(UiReadingOrder.Read(history))));
            if (IsAlive(info.rewardsHolder) && info.rewards is not null && !info.rewards.WasCollected)
                foreach (var reward in info.rewards)
                    if (IsAlive(reward) && reward.isActiveAndEnabled && IsOwned(reward.gameObject, info.rewardsHolder.transform))
                    {
                        string rewardText = ReadText(reward.text, reward.transform);
                        if (!string.IsNullOrWhiteSpace(rewardText)) rewardRows.Add(new(reward.text, rewardText));
                    }
            fragments.AddRange(UiReadingOrder.Read(rewardRows));
        }
        string current = ScreenContentTracker.Combine(objectives);
        string currentLabel = string.IsNullOrWhiteSpace(current) ? null : ModText.Format("quest.current_objectives", current);
        selection = new QuestSelection(key, ScreenContentTracker.Combine(summary),
            ScreenContentTracker.Combine(new[] { fallbackState, currentLabel }), ScreenContentTracker.Combine(fragments));
        return true;
    }

    internal static bool IsOwned(GameObject element, Transform owner) =>
        IsAlive(element) && IsAlive(element.transform) && IsAlive(owner) &&
        element.transform.IsChildOf(owner) && UiVisibility.IsVisible(element);

    private static string ReadText(TextMeshProUGUI label, Transform owner) =>
        TextUtils.IsVisibleLabel(label) && IsOwned(label.gameObject, owner)
            ? TextUtils.CleanText(label.text) : null;
}
