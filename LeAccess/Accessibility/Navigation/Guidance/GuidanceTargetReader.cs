using System;
using System.Collections.Generic;
using DMM;
using LE.Data;
using LE.UI.Minimap;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal readonly record struct GuidanceTarget(int Id, string Name, Vector3 Position, float ArrivalRadius, bool IsQuest)
{
    internal int Priority { get; init; }
}

internal static class GuidanceTargetReader
{
    internal static string LastFailure { get; private set; }

    internal static bool TryRead(MapSectionWorldSnapshot world, List<GuidanceTarget> targets, bool includeExits = false)
    {
        targets.Clear();
        LastFailure = null;
        try
        {
            var manager = MinimapIconManager.Instance;
            if (!IsAlive(manager) || !manager.isActiveAndEnabled || !IsAlive(manager.minimap) ||
                !manager.minimap.isActiveAndEnabled) return false;
            var icons = manager.icons;
            if (icons is null || icons.WasCollected) return false;
            var seen = new HashSet<int>();
            for (int i = 0; i < icons.Count; i++)
            {
                try
                {
                    var entry = icons[i];
                    if (entry is null || entry.WasCollected) continue;
                    var icon = entry.icon;
                    if (!Available(icon, world) || !Finite(icon.transform.position) ||
                        !GuidanceMarkerVisibility.IsVisible(entry.ui, manager)) continue;
                    if (TryQuest(icon, world, out var questTarget))
                    {
                        if (seen.Add(questTarget.Id)) targets.Add(questTarget);
                    }
                    else if (TryExit(icon, world, out var exitTarget) && seen.Add(exitTarget.Id))
                        targets.Add(exitTarget);
                }
                catch (Exception ex) { LastFailure ??= ex.GetType().Name + ": " + ex.Message; }
            }
            if (!includeExits && targets.Exists(target => target.IsQuest)) targets.RemoveAll(target => !target.IsQuest);
            return targets.Count > 0 || LastFailure is null;
        }
        catch (Exception ex)
        {
            LastFailure = ex.GetType().Name + ": " + ex.Message;
            targets.Clear();
            return false;
        }
    }

    private static bool TryQuest(DMMapIcon icon, MapSectionWorldSnapshot world, out GuidanceTarget target)
    {
        target = default;
        var owners = icon.GetComponentsInParent<SetMapIconColourFromObjectiveCondition>(true);
        foreach (var owner in owners)
        {
            if (!IsAlive(owner) || !Owned(owner, world) || !SameIcon(owner.icon, icon) && !SameIcon(owner.arrowIcon, icon))
                continue;
            var condition = owner.objectiveCondition;
            if (!IsAlive(condition) || !Owned(condition, world)) continue;
            var objective = condition.objective;
            if (!IsAlive(objective)) continue;
            var quest = objective.quest;
            var step = objective.questStep;
            if (!IsAlive(quest) || !quest.playerVisible || !IsAlive(step) || !IsAlive(step.quest) ||
                step.quest.Pointer != quest.Pointer) continue;
            var list = quest.getLocalStatefulQuestList();
            if (list is null || list.WasCollected || !list.UIIsQuestTracked(quest) ||
                list.getObjectiveState(objective) != QuestState.Active) continue;
            var state = list.getStatefulQuest(quest);
            if (state is null || state.WasCollected || !IsAlive(state.quest) || state.quest.Pointer != quest.Pointer ||
                state.getState() != QuestState.Active || state.trackStatus == SavedQuest.TrackStatus.FORCE_UNTRACKED)
                continue;
            var localization = global::Localization.Quests;
            string title = quest.displayName, text = objective.activeText;
            if (localization is not null && !localization.WasCollected)
            {
                title = localization.GetQuestName(quest.id, title);
                text = localization.GetObjectiveActive(quest.id, step.id, objective.id, text);
            }
            title = TextUtils.CleanText(title);
            text = TextUtils.CleanText(text);
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(text)) continue;
            // The native circle and direction arrow share one owner. Its center is a marker, not a completion trigger.
            target = new GuidanceTarget(owner.GetInstanceID(), ModText.Format("guidance.quest_marker", title, text),
                icon.transform.position, 0, true)
            {
                Priority = state.trackStatus == SavedQuest.TrackStatus.FORCE_TRACKED ? 0 : quest.mainLineQuest ? 1 : 2
            };
            return true;
        }
        return false;
    }

    private static bool TryExit(DMMapIcon icon, MapSectionWorldSnapshot world, out GuidanceTarget target)
    {
        target = default;
        var mapOwner = icon.GetComponentInParent<DMMapWorldIcon>(true);
        if (!Available(mapOwner, world) || !SameIcon(mapOwner.MmIcon, icon) || mapOwner.awaitingDestruction) return false;
        var listener = mapOwner.GetComponentInParent<WorldObjectClickListener>(true);
        if (!Available(listener, world)) return false;
        var tag = listener.hoverTagOnMouseOver;
        if (!IsAlive(tag)) tag = listener.GetComponent<HoverTagOnMouseOver>();
        if (!IsAlive(tag) || !Owned(tag, world) || tag.gameObject.Pointer != listener.gameObject.Pointer) return false;
        TransitionOrGateInteraction interaction;
        if (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION)
            interaction = tag.sceneInteraction;
        else if (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.DUNGEON_TRANSITION)
            interaction = tag.dungeonTransitionInteraction;
        else return false;
        if (!IsAlive(interaction) || !Owned(interaction, world) || string.IsNullOrWhiteSpace(interaction.sceneName) ||
            !SceneList.TryGetNonDefaultSceneDetails(interaction.sceneName, out var details) ||
            details is null || details.WasCollected) return false;
        string name = TextUtils.CleanText(details.LocalizedName);
        float range = listener.interactionRange;
        Vector3 position = listener.transform.position;
        if (string.IsNullOrWhiteSpace(name) || !float.IsFinite(range) || range <= 0 || !Finite(position) ||
            !listener.CanInteract()) return false;
        target = new GuidanceTarget(listener.GetInstanceID(), ModText.Format("guidance.exit_to", name), position, range, false)
        { Priority = 3 };
        return true;
    }

    private static bool SameIcon(BaseDMMapIcon owned, DMMapIcon actual) => IsAlive(owned) && owned.Pointer == actual.Pointer;
    private static bool Available(Behaviour component, MapSectionWorldSnapshot world) =>
        IsAlive(component) && component.isActiveAndEnabled && Owned(component, world);
    private static bool Owned(Component component, MapSectionWorldSnapshot world) =>
        IsAlive(component.gameObject) && (int)component.gameObject.scene.handle == world.SceneHandle;
    private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
}
