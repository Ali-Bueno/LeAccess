using LeAccess.Accessibility.ScreenContent;
using LE.Factions;
using LE.UI.PanelSystem;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>A panel's visible text that no control, slot, list, tooltip or currency counter owns.</summary>
internal static class PanelStaticText
{
    internal static string Read(Panel panel) =>
        ScreenContentReader.Read(panel.gameObject, null, null, include: label => IsStatic(label, panel));

    // Bare counters (favor, bones, rank numbers) and per-item text belong to the readers of those controls.
    private static bool IsStatic(Component label, Panel panel) =>
        !Inside<Selectable>(label, panel) && !Inside<ItemContainerUI>(label, panel) && !Inside<ScrollRect>(label, panel) &&
        !Inside<InputDependantPrompt>(label, panel) && !Inside<NavigableDependantPrompt>(label, panel) &&
        !Inside<GeneralTooltop>(label, panel) && !Inside<FactionReward>(label, panel) &&
        !Inside<UIFavorText>(label, panel) && !Inside<UIGoldText>(label, panel) && !Inside<UIAncientBonesText>(label, panel) &&
        !Inside<SoulEmberCounterUI>(label, panel) && !Inside<UIKeyText>(label, panel) &&
        !Inside<TimeBeastEvolutionTile>(label, panel) && !Inside<SkillTreeNode>(label, panel) && !Inside<HarbingerDefeatedView>(label, panel) && !InsideSubTile(label, panel);

    // A faction overview lists its tiles itself; a rank panel is one tile on the panel's own object.
    private static bool InsideSubTile(Component label, Panel panel)
    {
        var tile = label.GetComponentInParent<FactionTile>();
        return IsAlive(tile) && tile.gameObject.Pointer != panel.gameObject.Pointer;
    }

    private static bool Inside<T>(Component label, Panel panel) where T : Component
    {
        var owner = label.GetComponentInParent<T>();
        return IsAlive(owner) && owner.transform.IsChildOf(panel.transform);
    }
}
