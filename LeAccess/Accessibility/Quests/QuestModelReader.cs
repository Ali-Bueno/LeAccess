using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class QuestModelReader
{
    internal static bool TryRead(Quest quest, QuestInformation formatter, out string state,
        out string currentObjectives, out string details)
    {
        state = currentObjectives = details = null;
        if (!IsAlive(quest) || !IsAlive(formatter)) return false;
        var list = quest.getLocalStatefulQuestList();
        if (list is null || list.WasCollected) return false;
        var tracked = list.getStatefulQuest(quest);
        if (tracked is null || tracked.WasCollected || !IsAlive(tracked.quest) || tracked.quest.Pointer != quest.Pointer)
            return false;
        var questState = tracked.getState();
        state = TextUtils.CleanText(formatter.getNameForState(questState));
        var full = new List<string>();
        var active = new List<string>();
        var history = new List<string>();
        var step = tracked.currentStep;
        var localization = global::Localization.Quests;
        if (IsAlive(step) && IsAlive(step.quest) && step.quest.Pointer == quest.Pointer &&
            localization is not null && !localization.WasCollected)
            full.Add(TextUtils.CleanText(localization.GetStepDescription(quest.id, step.id, step.displayText)));

        // Use the same objective projection and formatter as native GenerateObjectives.
        var objectives = list.getObjectiveStatesForQuest(quest);
        if (objectives is not null && !objectives.WasCollected)
            foreach (var entry in objectives)
            {
                if (entry is null || entry.WasCollected) continue;
                if (entry.state is not (QuestState.Active or QuestState.Completed or QuestState.Failed)) continue;
                var objective = entry.objective;
                if (!IsAlive(objective) || !IsAlive(objective.quest) || objective.quest.Pointer != quest.Pointer ||
                    !IsAlive(objective.questStep) || !IsAlive(objective.questStep.quest) ||
                    objective.questStep.quest.Pointer != quest.Pointer) continue;
                string text = TextUtils.CleanText(formatter.getObjectiveStateText(objective, entry.state));
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (entry.state == QuestState.Active)
                {
                    if (questState == QuestState.Active) active.Add(text);
                    continue;
                }
                history.Add(ModText.Format(entry.state == QuestState.Completed ? "quest.objective_completed" : "quest.objective_failed", text));
            }
        currentObjectives = ScreenContentTracker.Combine(active);
        if (history.Count != 0) full.Add(ModText.Format("quest.objective_history", ScreenContentTracker.Combine(history)));
        details = ScreenContentTracker.Combine(full);
        return true;
    }
}
