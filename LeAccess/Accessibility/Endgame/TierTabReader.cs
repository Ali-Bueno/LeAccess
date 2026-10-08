using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Reads the tier tabs of the dungeon and arena entrance panels, which carry no text in their prefabs.</summary>
/// <remarks>See reference/endgame-panels.md#entrance-panels.</remarks>
internal static class TierTabReader
{
    internal static bool Owns(Panel panel, GameObject element) => TryFind(panel, element, out _, out _);

    internal static bool TryRead(Panel panel, GameObject element, out EndgameStop stop)
    {
        stop = default;
        if (!TryFind(panel, element, out var tab, out var tabs)) return false;
        var elements = tabs.tabElements;
        int index = -1;
        for (int i = 0; i < elements.Count; i++)
            if (IsAlive(elements[i]) && elements[i].Pointer == tab.Pointer) index = i;
        if (index < 0) return false;
        bool selected = tabs.ActiveTabId == tab.elementID;
        stop = new EndgameStop($"{tab.GetInstanceID()}:{selected}:{tab.isDisabled}",
            EndgameText.Tier(index + 1, selected, tab.isDisabled, index + 1, elements.Count));
        return true;
    }

    private static bool TryFind(Panel panel, GameObject element, out TabUIElement tab, out TabUIController tabs)
    {
        tab = IsAlive(element) ? element.GetComponentInParent<TabUIElement>() : null;
        tabs = TierTabs(panel);
        return IsAlive(tab) && IsAlive(tabs) && IsAlive(tab.controller) && tab.controller.Pointer == tabs.Pointer &&
            tabs.tabElements is { WasCollected: false };
    }

    private static TabUIController TierTabs(Panel panel)
    {
        var temporal = panel.TryCast<TemporalSanctumEntrancePanel>();
        if (IsAlive(temporal)) return Tabs(temporal._dungeonEnterPanelUI);
        var soulfire = panel.TryCast<SoulfireBastionEntrancePanel>();
        if (IsAlive(soulfire)) return Tabs(soulfire._dungeonEnterPanelUI);
        var lightless = panel.TryCast<LightlessArborEntrancePanel>();
        if (IsAlive(lightless)) return Tabs(lightless._dungeonEnterPanelUI);
        var arena = panel.TryCast<ArenaEntrancePanel>();
        return IsAlive(arena) && IsAlive(arena._arenaEnterPanelUI) ? arena._arenaEnterPanelUI.tierTabs : null;
    }

    private static TabUIController Tabs(DungeonEnterPanelUI ui) => IsAlive(ui) ? ui.tierTabs : null;
}
