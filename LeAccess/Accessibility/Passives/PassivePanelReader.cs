using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

/// <summary>Reads the controller passive panel: its mastery trees, banner diamonds and tree points.</summary>
internal static class PassivePanelReader
{
    /// <summary>The controller panel while its class tree is shown.</summary>
    internal static bool TryGetOpen(out PassivePanelManager manager)
    {
        manager = PassivePanelManager.instance;
        var tree = IsAlive(manager) ? manager.activeTree : null;
        if (IsAlive(tree) && UiVisibility.IsVisible(tree.gameObject)) return true;
        manager = null;
        return false;
    }

    internal static bool Owns(Component navigable) =>
        IsAlive(navigable) && TryGetOpen(out var manager) && navigable.transform.IsChildOf(manager.activeTree.transform);

    /// <summary>The mastery page holding this navigable, else the displayed mastery.</summary>
    internal static int TreeIndex(PassivePanelManager manager, Component navigable)
    {
        var pages = manager.activeTree.masteries;
        if (IsAlive(navigable) && pages is not null && !pages.WasCollected)
            for (int i = 0; i < pages.Length; i++)
                if (IsAlive(pages[i]) && navigable.transform.IsChildOf(pages[i].transform)) return i;
        return manager.activeMastery;
    }

    /// <summary>True while a newly clicked mastery's page has not replaced the shown one yet.</summary>
    internal static bool IsSwitching(PassivePanelManager manager)
    {
        var pages = manager.activeTree.masteries;
        int index = manager.activeMastery;
        if (pages is null || pages.WasCollected || index >= pages.Length) return false;
        var page = pages[index];
        return IsAlive(page) && !page.activeInHierarchy;
    }

    /// <summary>A tree's name, allocated points and role, then the character's unspent points.</summary>
    internal static string ReadTree(PassivePanelManager manager, int index) => ScreenContentTracker.Combine(new[] {
        Describe(manager, index), ModText.Format("passive.unspent", manager.activeTree.unspentPoints) });

    /// <summary>Every tree's points with the chosen mastery marked.</summary>
    internal static string ReadOverview(PassivePanelManager manager)
    {
        var trees = new List<string>();
        for (int i = 0; i < ButtonCount(manager); i++)
        {
            string tree = Describe(manager, i);
            if (!string.IsNullOrWhiteSpace(tree)) trees.Add(tree);
        }
        if (trees.Count == 0) return null;
        var parts = new List<string>();
        if (LocalTree() is { chosenMastery: 0 }) parts.Add(ModText.Get("passive.no_mastery"));
        parts.Add(ModText.Format("passive.overview", string.Join("; ", trees)));
        return ScreenContentTracker.Combine(parts);
    }

    /// <summary>The tree's allocated points, from its visible diamond label or the saved levels.</summary>
    internal static int? Points(PassivePanelManager manager, int index)
    {
        var button = Button(manager, index);
        var label = IsAlive(button) ? button.levelTMP : null;
        if (TextUtils.IsVisibleLabel(label) && int.TryParse(TextUtils.CleanText(label.text), out int shown)) return shown;
        var levels = LocalTree()?.masteryLevels;
        return levels is not null && !levels.WasCollected && index >= 0 && index < levels.Length ? levels[index] : null;
    }

    /// <summary>The tree's maximum points, when its diamond's bar has been filled.</summary>
    internal static int? MaxPoints(PassivePanelManager manager, int index)
    {
        var button = Button(manager, index);
        return IsAlive(button) && PassiveTreeReader.TryReadBar(button.masteryBarDisplay, out _, out int max) ? max : null;
    }

    /// <summary>The mastery index of the banner diamond containing this element, or -1.</summary>
    internal static int DiamondIndex(PassivePanelManager manager, GameObject element)
    {
        if (!IsAlive(element)) return -1;
        for (int i = 0; i < ButtonCount(manager); i++)
            if (Button(manager, i) is { } button && IsAlive(button) && element.transform.IsChildOf(button.transform)) return i;
        return -1;
    }

    /// <summary>The focused diamond: its tree's name, points and role, with the mastery's description as details.</summary>
    internal static bool TryReadBanner(PassivePanelManager manager, PassiveTreeBannerNavigable banner, out PassiveSelection selection)
    {
        selection = default;
        var focused = IsAlive(banner) && !banner.isDescriptionSelected ? banner._currentButton : null;
        int index = IsAlive(focused) ? DiamondIndex(manager, focused.gameObject) : -1;
        if (index < 0) return false;
        string tree = Describe(manager, index);
        if (string.IsNullOrWhiteSpace(tree)) return false;
        var button = Button(manager, index);
        string summary = ScreenContentTracker.Combine(new[] { tree,
            index == manager.activeMastery ? ModText.Get("passive.tree_shown") : null });
        selection = new PassiveSelection($"banner:{button.Pointer}:{manager.activeMastery}", button.gameObject, summary,
            MasteryReader.ReadDefinition(button));
        return true;
    }

    private static string Describe(PassivePanelManager manager, int index)
    {
        var button = Button(manager, index);
        string name = IsAlive(button) ? MasteryReader.ReadName(button) : null;
        if (string.IsNullOrWhiteSpace(name)) return null;
        int? points = Points(manager, index);
        return ScreenContentTracker.Combine(new[] {
            points is null ? name : ModText.Format("passive.tree", name, points.Value), Role(index, button) });
    }

    private static string Role(int index, MasteryButton button)
    {
        if (!button.isMasteryClass) return ModText.Get("passive.tree_class");
        if (LocalTree() is { } tree && tree.chosenMastery == index) return ModText.Get("passive.tree_chosen");
        return IsAlive(button.lockIcon) && UiVisibility.IsVisible(button.lockIcon) ? ModText.Get("passive.tree_locked") : null;
    }

    private static int ButtonCount(PassivePanelManager manager)
    {
        var banner = manager.openedBanner;
        var buttons = IsAlive(banner) ? banner.masteryButtons : null;
        return buttons is null || buttons.WasCollected ? 0 : buttons.Count;
    }

    private static MasteryButton Button(PassivePanelManager manager, int index)
    {
        var banner = manager.openedBanner;
        var buttons = IsAlive(banner) ? banner.masteryButtons : null;
        return buttons is null || buttons.WasCollected || index < 0 || index >= buttons.Count ? null : buttons[index];
    }

    private static LocalTreeData LocalTree()
    {
        var actor = PlayerFinder.getPlayerActor();
        var tree = IsAlive(actor) ? actor.localTreeData : null;
        return IsAlive(tree) ? tree : null;
    }
}
