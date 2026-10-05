using System;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>The controller panel's mastery banner as a list (base class, then masteries 1–3) instead of the game's cross.</summary>
/// <remarks>See reference/passive-reading.md#mastery-banner-as-a-list-2026-10-05.</remarks>
internal static class PassiveBannerNavigation
{
    private static string _lastFailure;

    /// <summary>Up/Down step through the diamonds and stop at the ends, Right returns to the tree, Left stays;
    /// false leaves the move to the game (description scrolling, the mastery skill, the fullscreen panel).</summary>
    internal static bool TryNavigate(PassiveTreeBannerNavigable banner, Direction direction)
    {
        try
        {
            if (!IsAlive(banner) || banner.isDescriptionSelected || !PassivePanelReader.TryGetOpen(out var manager)) return false;
            int current = IndexOf(manager, banner._currentButton);
            if (current < 0) return false;
            switch (direction)
            {
                case Direction.UP when Usable(manager, current - 1):
                    Focus(banner, manager, current - 1);
                    return true;
                case Direction.DOWN when Usable(manager, current + 1):
                    Focus(banner, manager, current + 1);
                    return true;
                case Direction.RIGHT:
                    // The game's own way back from its right diamond: the tree's top-left cell, searching down.
                    var tree = banner.GetPassiveTreeNavigable();
                    if (!IsAlive(tree)) return false;
                    tree.selectSkillTreeWithIndexAndDirection(new Vector2Int(0, 4), PassiveTreeNavigable.PassiveSkillTreeNodeSearchDirection.Down);
                    return true;
                case Direction.UP:
                case Direction.DOWN:
                case Direction.LEFT:
                    PassiveAccessibility.RepeatCurrent();
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>Entering from the tree (the game lands on its right diamond) starts on the shown tree's diamond.</summary>
    internal static void AfterEntered(PassiveTreeBannerNavigable banner, Vector2Int index)
    {
        try
        {
            // (0,1) is the tree's own exit to the banner (PassiveTreeNavigable.IsOutBorder).
            if (index.x != 0 || index.y != 1 || !EpochInputManager.IsControllerActive || !IsAlive(banner) ||
                !PassivePanelReader.TryGetOpen(out var manager)) return;
            int shown = manager.activeMastery;
            if (Usable(manager, shown) && IndexOf(manager, banner._currentButton) != shown) Focus(banner, manager, shown);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    /// <summary>The diamond's place in the banner, by the panel's own mastery index (0 = base class).</summary>
    internal static int IndexOf(PassivePanelManager manager, UnityEngine.UI.Button button)
    {
        if (!IsAlive(button)) return -1;
        for (int i = 0; i < PassivePanelReader.ButtonCount(manager); i++)
        {
            var diamond = PassivePanelReader.Button(manager, i);
            if (IsAlive(diamond) && IsAlive(diamond.button) && diamond.button.Pointer == button.Pointer) return i;
        }
        return -1;
    }

    internal static bool Usable(PassivePanelManager manager, int index)
    {
        var diamond = PassivePanelReader.Button(manager, index);
        return IsAlive(diamond) && IsAlive(diamond.button) && diamond.gameObject.activeInHierarchy;
    }

    // The game's own focus change: tooltip exit/enter and the cursor, without selecting through the EventSystem.
    private static void Focus(PassiveTreeBannerNavigable banner, PassivePanelManager manager, int index) =>
        banner.CurrentButton = PassivePanelReader.Button(manager, index).button;

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Passive banner navigation fell back to the game: {ex}");
    }
}
