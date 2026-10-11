using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

internal enum SkillDetailsSource { Description, Tooltip, ExpandedTooltip }

internal readonly record struct SkillSelection(string Key, GameObject Root, Ability Ability,
    AbilityBarIcon BarIcon, string Name, string Details,
    SkillDetailsSource DetailsSource = SkillDetailsSource.Description, string LiveDetails = null,
    string Status = null, bool IsNode = false, string Context = null, string Position = null, string MoreInfo = null);

/// <summary>Reads the game's current selection; never opens a tooltip or changes navigation.</summary>
internal static class SkillSelectionReader
{
    /// <summary>The Skills panel when it owns navigation and nothing else is speaking over it.</summary>
    internal static bool TryGetPanel(out SkillsPanelManager panel)
    {
        panel = null;
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
            DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent) return false;
        var navigable = NavigationManager.currentlySelected;
        panel = navigable is null || navigable.WasCollected ? null : navigable.TryCast<SkillsPanelManager>();
        return IsAlive(panel) && panel.isActiveAndEnabled && UiVisibility.IsVisible(panel.gameObject);
    }

    internal static bool TryRead(out SkillSelection selection, bool includeDetails = true)
    {
        selection = default;
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
            DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent) return false;
        var navigable = NavigationManager.currentlySelected;
        if (navigable is null || navigable.WasCollected) return false;

        var panel = navigable.TryCast<SkillsPanelManager>();
        if (IsAlive(panel))
        {
            if (!panel.isActiveAndEnabled || !UiVisibility.IsVisible(panel.gameObject)) return false;
            if (panel.IsSkillTreeOpen && !panel.inTopArea) return TryReadNode(panel, out selection, includeDetails);

            // Interop erases this rectangular array's element type.
            // The game's FocusSelectable indexes it as [x,y].
            var raw = panel.selectables;
            if (raw is null || raw.WasCollected) return false;
            var cells = raw.TryCast<Il2CppSystem.Array>();
            if (cells is null || cells.WasCollected || cells.Rank != 2) return false;
            var index = panel.currentlySelectedIdx;
            if (index.x < 0 || index.y < 0 || index.x >= cells.GetLength(0) || index.y >= cells.GetLength(1))
                return false;
            var cell = cells.GetValue(index.x, index.y);
            if (cell is null || cell.WasCollected) return false;
            var component = cell.TryCast<Component>();
            if (!IsAlive(component)) return false;
            if (!TryReadControl(component.gameObject, out selection, includeDetails, cells))
                return panel.IsSkillTreeOpen && TryReadTreeButton(panel, component.gameObject, out selection);
            if (SkillPanelGrid.TryDescribe(panel, out string row, out int position, out int count))
                selection = selection with
                {
                    Context = row,
                    Position = ListPosition.Text(position, count)
                };
            return true;
        }

        // The ability assignment grid/hotbar uses the ordinary active Selectable.
        var prioritized = navigable.TryCast<PrioritizedNavigable>();
        if (!IsAlive(prioritized) || !prioritized.isActiveAndEnabled ||
            !UiVisibility.IsVisible(prioritized.gameObject)) return false;
        var selected = prioritized.ActiveSelectable;
        return IsAlive(selected) && selected.isActiveAndEnabled &&
            TryReadControl(selected.gameObject, out selection, includeDetails);
    }

    // After A on a free slot the game marks the skills it can specialise there (AbilityPanelIcon.SetSpecializationSelectReadyStatus).
    private static bool ChoosingSpecialisation(Il2CppSystem.Array cells)
    {
        for (int y = 1; y < cells.GetLength(1); y++)
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                var icon = cells.GetValue(x, y)?.TryCast<Component>()?.GetComponent<AbilityPanelIcon>();
                if (IsAlive(icon) && icon.specMode && icon.slotToSpec >= 0) return true;
            }
        return false;
    }

    private static bool TryReadControl(GameObject root, out SkillSelection selection, bool includeDetails,
        Il2CppSystem.Array panelCells = null)
    {
        selection = default;
        if (!IsAlive(root) || !UiVisibility.IsVisible(root)) return false;
        Ability ability = null;
        AbilityTooltip owner = null;
        AbilityBarIcon bar = null;
        string name = null;
        string learningStatus = null;
        string definition = null;
        bool hasItemIdentity = false;
        SkillAssignmentReader.TryReadContext(root, out var assignment);
        string assignmentStatus = assignment == null ? null : ModText.Format(
            assignment.Stage == SkillAssignmentStage.AvailableSkills ? "skill.assign_to" : "skill.bar_slot",
            assignment.Binding ?? ModText.Format("skill.slot_number", assignment.SlotNumber));
        if (assignment?.Stage == SkillAssignmentStage.Hotbar && SkillSwapCarry.PendingName is string carried)
            assignmentStatus = ScreenContentTracker.Combine(new[] { assignmentStatus, ModText.Format("skill.swap_carry", carried) });

        var panelIcon = Find<AbilityPanelIcon>(root);
        if (IsAlive(panelIcon))
        {
            hasItemIdentity = true;
            ability = panelIcon.ability;
            owner = panelIcon.tooltipAbility;
            name = ReadLabel(panelIcon.skillName);
            learningStatus = SkillLearningReader.ReadPanel(panelIcon);
            // In that mode A specialises an eligible skill at once; already specialised ones are marked with no slot.
            if (panelIcon.specMode)
                learningStatus = ScreenContentTracker.Combine(new[] { learningStatus, panelIcon.slotToSpec < 0
                    ? ModText.Get("skill.already_specialised")
                    : panelIcon.locked ? null : ModText.Get("skill.confirm_to_specialise") });
        }
        else
        {
            var selectionIcon = Find<AbilitySelectionIcon>(root);
            if (IsAlive(selectionIcon))
            {
                hasItemIdentity = true;
                ability = selectionIcon.ability;
                owner = selectionIcon.tooltipAbility;
            }
            else
            {
                bar = Find<AbilityBarIcon>(root);
                if (IsAlive(bar))
                {
                    hasItemIdentity = true;
                    if (!SkillBindingReader.TryReadSlot(bar, out ability)) return false;
                    if (!IsAlive(ability))
                    {
                        selection = new SkillSelection($"slot:{root.GetInstanceID()}:{bar.abilityNumber}", root, null, bar,
                            ModText.Get("skill.empty_slot"), ModText.Get("skill.hotbar_help"), Status: assignmentStatus);
                        return true;
                    }
                    owner = bar.tooltipAbility;
                }
                else
                {
                    var specialised = Find<SpecialisedAbilityIcon>(root);
                    if (IsAlive(specialised))
                    {
                        hasItemIdentity = true;
                        ability = specialised.ability;
                        if (!IsAlive(ability))
                        {
                            selection = new SkillSelection($"specialization:{root.GetInstanceID()}", root, null, null,
                                ModText.Get(specialised.locked ? "skill.specialization_locked" : "skill.specialization_empty"),
                                null, Status: specialised.locked ? ReadLabel(specialised.levelLockedText)
                                    : ModText.Get(panelCells != null && ChoosingSpecialisation(panelCells)
                                        ? "skill.choose_to_specialise" : "skill.slot_help"));
                            return true;
                        }
                    }
                }
            }
        }

        var adapter = Find<AbilityTooltipSelectableAdapter>(root);
        if (!IsAlive(owner) && IsAlive(adapter)) owner = adapter._abilityTooltip;
        // Empty slots can retain an old tooltip component. Their explicit null item wins.
        if (!hasItemIdentity && !IsAlive(ability) && IsAlive(owner)) ability = owner.currentAbility;
        if (!IsAlive(ability)) return false;
        learningStatus ??= SkillLearningReader.ReadKnown(ability, provenLearned: assignment != null);
        learningStatus ??= assignment != null ? ModText.Get("skill.learned") : null;

        var localized = Localization.Abilities;
        if (string.IsNullOrWhiteSpace(name) && localized is not null && !localized.WasCollected)
            name = TextUtils.CleanText(localized.GetName(ability));
        string description = includeDetails && localized is not null && !localized.WasCollected
            ? TextUtils.CleanText(localized.GetDescription(ability)) : null;
        string moreInfo = null;
        string liveDetails = null;
        var detailsSource = SkillDetailsSource.Description;

        // The global tooltip can still describe the previously highlighted skill.
        // Both its source component and displayed ability must match this selection.
        var tooltip = IsAlive(owner) ? owner.toolTip : null;
        if (includeDetails && IsAlive(owner) && owner.activeAbilityTooltip && SameAbility(owner.currentAbility, ability) &&
            IsAlive(tooltip) && tooltip.tooltipActive && SameAbility(tooltip.currentAbility, ability) &&
            tooltip.activeInitializationStage == UITooltipAbility.TooltipInitializationStage.Completed &&
            UiVisibility.IsVisible(tooltip.gameObject) && IsAlive(tooltip.content) &&
            UiVisibility.IsVisible(tooltip.content))
        {
            detailsSource = SkillDetailsSource.Tooltip;
            string visibleName = ReadLabel(tooltip.nameText);
            if (!string.IsNullOrWhiteSpace(visibleName)) name = visibleName;
            string visibleDescription = ReadLabel(tooltip.descriptionText);
            if (!string.IsNullOrWhiteSpace(visibleDescription)) description = visibleDescription;
            definition = ReadLabel(tooltip.skillDefinition);
            liveDetails = SkillTooltipDetailsReader.ReadAbility(tooltip);
            if (IsAlive(tooltip.moreInfoObj) && UiVisibility.IsVisible(tooltip.moreInfoObj))
            {
                moreInfo = ReadLabel(tooltip.moreInfoDetailsText);
                if (!string.IsNullOrWhiteSpace(moreInfo)) detailsSource = SkillDetailsSource.ExpandedTooltip;
            }
        }

        if (string.IsNullOrWhiteSpace(name)) return false;
        // Held Right Stick already shows the explanation; otherwise read the ability's own alt text.
        string keywords = includeDetails && moreInfo == null ? KeywordExplanation.Describe($"skill {name}", ability.altText) : null;
        string help = assignment == null ? null : ModText.Get(assignment.Stage == SkillAssignmentStage.AvailableSkills
            ? "skill.assignment_help" : "skill.hotbar_help");
        selection = new SkillSelection($"ability:{root.GetInstanceID()}:{ability.Pointer}", root, ability, bar,
            name, ScreenContentTracker.Combine(new[] { definition, description, moreInfo, keywords, help }), detailsSource,
            liveDetails, ScreenContentTracker.Combine(new[] { learningStatus, assignmentStatus }), MoreInfo: moreInfo);
        return true;
    }

    // The row above an open tree: Back, the specialization slots, then respec, specialise and stop respeccing.
    private static bool TryReadTreeButton(SkillsPanelManager panel, GameObject root, out SkillSelection selection)
    {
        selection = default;
        if (!IsAlive(root) || !UiVisibility.IsVisible(root)) return false;
        var specialisation = panel.SkillsSpecialisationUIManager;
        string fallback = null, status = null;
        if (Same(root, panel.backButton)) fallback = "skill.tree_back";
        else if (Same(root, panel.specButton) || IsAlive(specialisation) && Same(root, specialisation.specialiseButton))
            fallback = "skill.specialise";
        else if (Same(root, panel.stopRespecButton) || IsAlive(specialisation) && Same(root, specialisation.stopRespeccingButton))
            fallback = "skill.stop_respec";
        else if (IsAlive(specialisation) && Same(root, specialisation.respecButton))
        {
            fallback = "skill.respec";
            status = ReadLabel(specialisation.respecMinLevelText);
        }
        if (fallback == null) return false;
        string name = TextUtils.GetTextFromHierarchy(root);
        if (string.IsNullOrWhiteSpace(name)) name = ModText.Get(fallback);
        string key = $"tree-button:{root.GetInstanceID()}";
        var notice = SkillTreeState.TryRead(panel, out var state) ? SkillTreeState.Describe(key, state) : default;
        selection = new SkillSelection(key, root, null, null, name, state.Level,
            Status: ScreenContentTracker.Combine(new[] { status, notice.After }), Context: notice.Before);
        return true;
    }

    private static bool Same(GameObject root, Component component) =>
        IsAlive(component) && component.gameObject.GetInstanceID() == root.GetInstanceID();

    private static bool Same(GameObject root, GameObject other) => IsAlive(other) && other.GetInstanceID() == root.GetInstanceID();

    private static bool TryReadNode(SkillsPanelManager panel, out SkillSelection selection, bool includeDetails)
    {
        selection = default;
        var node = SkillTreeNavigation.ResolveNode(panel);
        var tree = panel.CurrentlyOpenTree;
        if (!IsAlive(node) || !IsAlive(tree) || !IsAlive(node.tree) || node.tree.Pointer != tree.Pointer ||
            !UiVisibility.IsVisible(node.gameObject)) return false;
        var localized = Localization.Skills;
        if (localized is null || localized.WasCollected) return false;
        string NodeName(SkillTreeNode other) => TextUtils.CleanText(localized.GetNodeName(tree.treeID, other.id, other.nodeName));
        string name = NodeName(node);
        string description = TextUtils.CleanText(localized.GetNodeDescription(tree.treeID, node.id, node.description));
        string points = null;
        string key = $"node:{tree.GetInstanceID()}:{node.GetInstanceID()}";
        bool placed = SkillTreeNavigation.TryDescribe(panel, node, out string branch, out int place, out int count);
        SkillTreeNavigation.TryReadLinks(panel, node, NodeName, out string requires, out string links);
        var notice = SkillTreeState.TryRead(panel, out var state) ? SkillTreeState.Describe(key, state) : default;
        // The root is the skill itself and takes no points; in respec mode A refunds only where the game allows it.
        string status = ScreenContentTracker.Combine(new[] {
            node.maxPoints > 0 ? ModText.Format("node.points", node.pointsAllocated, node.maxPoints) : null,
            state.Respeccing && node.pointsAllocated > 0 && !node.getCouldBeRespecced() ? ModText.Get("skill.cannot_refund") : null,
            notice.After, requires });
        string position = placed ? ListPosition.Text(place, count) : null;
        string keywords = includeDetails ? KeywordExplanation.Describe($"skill node {name}", node.altText) : null;
        if (notice.Before != null) branch = ScreenContentTracker.Combine(new[] { notice.Before, branch });
        var detailsSource = SkillDetailsSource.Description;
        bool modernVisible = false;
        if (includeDetails && SkillTooltipDetailsReader.TryReadModernNode(node, out string modernName, out string modernDetails,
            out modernVisible))
        {
            if (!string.IsNullOrWhiteSpace(modernName)) name = modernName;
            selection = new SkillSelection(key, node.gameObject, tree.ability, null, name,
                ScreenContentTracker.Combine(new[] { state.Level, modernDetails, keywords, links }), SkillDetailsSource.Tooltip,
                Status: status, IsNode: true, Context: branch, Position: position);
            return !string.IsNullOrWhiteSpace(name);
        }
        var tooltip = NodeTooltipManager.instance;
        if (includeDetails && !modernVisible && IsAlive(tooltip) && tooltip.tooltipActive && !tooltip.requireLayoutUpdate &&
            IsAlive(tooltip.previousNode) && tooltip.previousNode.Pointer == node.Pointer &&
            UiVisibility.IsVisible(tooltip.gameObject) && IsAlive(tooltip.content) &&
            UiVisibility.IsVisible(tooltip.content))
        {
            detailsSource = SkillDetailsSource.Tooltip;
            string visibleName = ReadLabel(tooltip.title);
            if (!string.IsNullOrWhiteSpace(visibleName)) name = visibleName;
            string visibleDescription = ReadLabel(tooltip.description);
            if (!string.IsNullOrWhiteSpace(visibleDescription)) description = visibleDescription;
            points = SkillTooltipDetailsReader.ReadLegacyNode(tooltip);
        }
        if (string.IsNullOrWhiteSpace(name)) return false;
        selection = new SkillSelection(key, node.gameObject,
            tree.ability, null, name, ScreenContentTracker.Combine(new[] { state.Level, description, points, keywords, links }),
            detailsSource, Status: status, IsNode: true, Context: branch, Position: position);
        return true;
    }

    private static T Find<T>(GameObject root) where T : Component
    {
        var component = root.GetComponentInParent<T>();
        return IsAlive(component) ? component : root.GetComponentInChildren<T>();
    }

    private static bool SameAbility(Ability left, Ability right) =>
        IsAlive(left) && IsAlive(right) && left.Pointer == right.Pointer;

    private static string ReadLabel(TMP_Text text) =>
        TextUtils.IsVisibleLabel(text) ? TextUtils.CleanText(text.text) : null;
}
