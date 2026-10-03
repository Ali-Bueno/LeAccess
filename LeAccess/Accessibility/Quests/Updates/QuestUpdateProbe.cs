using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class QuestUpdateProbe
{
    internal static bool TryRead(StatefulQuestListHolder holder, out List<QuestProgressSnapshot> snapshots)
    {
        snapshots = new();
        if (!IsAlive(holder) || !holder.initialised || holder.hasBeenNullified) return false;
        var list = holder.statefulQuestList;
        if (list is null || list.WasCollected || list.trackedQuests is null || list.trackedQuests.WasCollected)
            return false;
        var localization = global::Localization.Quests;
        if (localization is null || localization.WasCollected) return false;
        var keys = new HashSet<string>();
        foreach (var tracked in list.trackedQuests)
        {
            if (tracked is null || tracked.WasCollected) continue;
            var quest = tracked.quest;
            if (!IsAlive(quest)) continue;
            if (!quest.playerVisible) continue;
            string key = quest.id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!keys.Add(key)) return false;
            if (TryReadQuest(tracked, list, key, out var snapshot)) snapshots.Add(snapshot);
            else snapshots.Add(new(key, null, QuestProgressStatus.Hidden, null, null, Available: false));
        }
        return true;
    }

    private static bool TryReadQuest(StatefulQuest tracked, StatefulQuestList list, string key,
        out QuestProgressSnapshot snapshot)
    {
        snapshot = default;
        var quest = tracked.quest;
        var localization = global::Localization.Quests;
        var owner = quest.getLocalStatefulQuestList();
        if (owner is null || owner.WasCollected || owner.Pointer != list.Pointer) return false;
        var step = tracked.currentStep;
        if (!IsAlive(step) || !IsAlive(step.quest) || step.quest.Pointer != quest.Pointer) return false;
        string title = TextUtils.CleanText(localization.GetQuestName(quest.id, quest.displayName));
        if (string.IsNullOrWhiteSpace(title)) return false;
        var status = tracked.getState() switch
        {
            QuestState.Active => QuestProgressStatus.Active,
            QuestState.Completed => QuestProgressStatus.Completed,
            QuestState.Failed => QuestProgressStatus.Failed,
            _ => QuestProgressStatus.Hidden
        };
        var revisions = new List<string>();
        var texts = new List<string>();
        if (status == QuestProgressStatus.Active)
        {
            var objectives = tracked.objectives;
            if (objectives is null || objectives.WasCollected) return false;
            foreach (var objective in objectives)
            {
                if (objective is null || objective.WasCollected) return false;
                var asset = objective.objective;
                if (!IsAlive(asset) || !IsAlive(asset.quest) || asset.quest.Pointer != quest.Pointer ||
                    !IsAlive(asset.questStep) || !IsAlive(asset.questStep.quest) ||
                    asset.questStep.quest.Pointer != quest.Pointer || objective.statefulQuest is null ||
                    objective.statefulQuest.WasCollected || objective.statefulQuest.Pointer != tracked.Pointer) return false;
                if (objective.state != QuestState.Active) continue;
                // This is the HUD's native localized formatter, including incremental counters.
                string text = TextUtils.CleanText(objective.GetObjectiveActiveText());
                if (string.IsNullOrWhiteSpace(text)) continue;
                texts.Add(text);
                revisions.Add($"{asset.id}:{(asset.isIncremental ? objective.progress : 0)}");
            }
        }
        revisions.Sort(System.StringComparer.Ordinal);
        string revision = status == QuestProgressStatus.Active
            ? $"{status}:{step.id}|{string.Join(",", revisions)}" : status.ToString();
        snapshot = new(key, title, status, revision, ScreenContentTracker.Combine(texts));
        return true;
    }
}
