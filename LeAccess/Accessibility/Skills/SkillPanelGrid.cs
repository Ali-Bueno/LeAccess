using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Step = LeAccess.Accessibility.Navigation.GroupedGridOrder.Step;

namespace LeAccess.Accessibility.Skills;

/// <summary>Moves the Skills overview cursor through one row per class instead of the native wrapping column walk.</summary>
/// <remarks>See reference/skill-tree-navigation.md#skills-overview-grid-2026-10-04.</remarks>
internal static class SkillPanelGrid
{
    private readonly record struct Cell(int X, int Y, IntPtr Item);

    // Rows as heard: the specialization slots, the base class, the panel's three masteries, then anything else
    // (the tree buttons beside the slots are active only while a tree is open, when this grid is not used).
    private const int SlotGroup = 0, BaseGroup = 1, MasteryCount = 3, OtherGroup = BaseGroup + MasteryCount + 1;
    private static readonly List<Cell>[] Groups = CreateGroups();
    private static readonly int[] Sizes = new int[OtherGroup + 1];
    private static readonly Dictionary<IntPtr, int> Members = new();
    private static readonly GroupEntryTracker Entry = new();
    private static IntPtr _panel;
    private static string _lastFailure;

    /// <summary>Handles a d-pad step on the overview; false lets the game's own step run.</summary>
    internal static bool TryNavigate(SkillsPanelManager panel, Direction direction)
    {
        try
        {
            if (!IsAlive(panel) || panel.IsSkillTreeOpen || !TryReadGroups(panel)) return false;
            if (!TryFind(panel.currentlySelectedIdx, out int group, out int index))
            {
                // A cursor left on a hidden or unknown cell restarts on the first row with skills.
                for (int g = BaseGroup; g < Groups.Length; g++)
                    if (Sizes[g] > 0) { Focus(panel, Groups[g][0]); return true; }
                return false;
            }
            Step? step = direction switch
            {
                Direction.RIGHT => Step.NextInGroup,
                Direction.LEFT => Step.PreviousInGroup,
                Direction.DOWN => Step.NextGroup,
                Direction.UP => Step.PreviousGroup,
                _ => null
            };
            if (step is null) return false;
            var move = GroupedGridOrder.Plan(Sizes, group, index, step.Value);
            if (move.Kind == GroupedGridOrder.MoveKind.Select)
            {
                Focus(panel, Groups[move.Group][move.Index]);
                return true;
            }
            // Up from the specialization slots reaches the search and buttons above, as in the game.
            if (group == SlotGroup && direction == Direction.UP) return false;
            SkillAccessibility.RepeatCurrent();
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>The selected cell's row name (only when the selection entered that row) and its place in the row.</summary>
    internal static bool TryDescribe(SkillsPanelManager panel, out string label, out int position, out int count)
    {
        label = null;
        position = count = 0;
        try
        {
            if (!IsAlive(panel) || panel.IsSkillTreeOpen) return false;
            var index = panel.currentlySelectedIdx;
            // The grid is rebuilt on every move; between moves the cached rows hold unless the cell's item changed.
            if (_panel != panel.Pointer || !TryFind(index, out int group, out int place) ||
                Groups[group][place].Item != ItemAt(panel, index.x, index.y))
            {
                if (!TryReadGroups(panel) || !TryFind(index, out group, out place)) return false;
            }
            // A skill outside the panel's class lists has no known row to place it in.
            if (group == OtherGroup) return false;
            position = place + 1;
            count = Groups[group].Count;
            if (Entry.Entered(Groups[group][place].Item, group)) label = Label(panel, group);
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>Forgets the rows and the spoken row once the overview no longer holds the selection.</summary>
    internal static void Forget()
    {
        Entry.Reset();
        _panel = IntPtr.Zero;
    }

    private static bool TryReadGroups(SkillsPanelManager panel)
    {
        _panel = IntPtr.Zero;
        foreach (var group in Groups) group.Clear();
        Array.Clear(Sizes, 0, Sizes.Length);
        // Interop erases this rectangular array's element type; the game indexes it as [x,y] with y=0 on top.
        var raw = panel.selectables;
        var cells = raw is null || raw.WasCollected ? null : raw.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2) return false;

        Members.Clear();
        AddMembers(panel.baseClassPanelIcons, BaseGroup);
        AddMembers(panel.baseClassTreePanelIcons, BaseGroup);
        AddMembers(panel.masteryClass1PanelIcons, BaseGroup + 1);
        AddMembers(panel.masteryClass2PanelIcons, BaseGroup + 2);
        AddMembers(panel.masteryClass3PanelIcons, BaseGroup + 3);
        AddMember(panel.masteryAbilityPanelIcon1, BaseGroup + 1);
        AddMember(panel.masteryAbilityPanelIcon2, BaseGroup + 2);
        AddMember(panel.masteryAbilityPanelIcon3, BaseGroup + 3);

        for (int y = 0; y < cells.GetLength(1); y++)
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                var component = cells.GetValue(x, y)?.TryCast<Component>();
                // Like the game's own empty-slot test, hidden cells are skipped.
                if (!IsAlive(component) || !component.gameObject.activeInHierarchy) continue;
                var icon = component.GetComponent<AbilityPanelIcon>();
                int group = IsAlive(component.GetComponent<SpecialisedAbilityIcon>()) ? SlotGroup
                    : IsAlive(icon) && Members.TryGetValue(icon.Pointer, out int member) ? member : OtherGroup;
                Groups[group].Add(new Cell(x, y, component.Pointer));
            }
        for (int g = 0; g < Groups.Length; g++) Sizes[g] = Groups[g].Count;
        _panel = panel.Pointer;
        return true;
    }

    private static void AddMembers(Il2CppSystem.Collections.Generic.List<AbilityPanelIcon> icons, int group)
    {
        if (icons is null || icons.WasCollected) return;
        foreach (var icon in icons) AddMember(icon, group);
    }

    private static void AddMember(AbilityPanelIcon icon, int group)
    {
        if (IsAlive(icon)) Members[icon.Pointer] = group;
    }

    private static bool TryFind(Vector2Int index, out int group, out int place)
    {
        for (group = 0; group < Groups.Length; group++)
            for (place = 0; place < Groups[group].Count; place++)
                if (Groups[group][place].X == index.x && Groups[group][place].Y == index.y) return true;
        group = place = -1;
        return false;
    }

    private static IntPtr ItemAt(SkillsPanelManager panel, int x, int y)
    {
        var cells = panel.selectables?.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || x >= cells.GetLength(0) || y >= cells.GetLength(1)) return IntPtr.Zero;
        var component = cells.GetValue(x, y)?.TryCast<Component>();
        return IsAlive(component) ? component.Pointer : IntPtr.Zero;
    }

    private static string Label(SkillsPanelManager panel, int group) => group switch
    {
        SlotGroup => ModText.Get("skill.specialization_slots"),
        BaseGroup => Read(panel.baseClassName),
        BaseGroup + 1 => Read(panel.mastery1ClassName),
        BaseGroup + 2 => Read(panel.mastery2ClassName),
        BaseGroup + 3 => Read(panel.mastery3ClassName),
        _ => null
    };

    private static string Read(TMP_Text text) => IsAlive(text) ? TextUtils.CleanText(text.text) : null;

    // The same steps as the native move: clear UI selection and the previous tooltip, then focus the cell.
    private static void Focus(SkillsPanelManager panel, Cell cell)
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (IsAlive(events)) events.SetSelectedGameObject(null);
        panel.HideIconTooltip();
        panel.currentlySelectedIdx = new Vector2Int { x = cell.X, y = cell.Y };
        panel.FocusSelectable();
    }

    private static List<Cell>[] CreateGroups()
    {
        var groups = new List<Cell>[OtherGroup + 1];
        for (int g = 0; g < groups.Length; g++) groups[g] = new List<Cell>();
        return groups;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Skills overview navigation fell back to the game: {ex}");
    }
}
