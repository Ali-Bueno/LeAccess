using System;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Names the item slots of the item-service panels and the dungeon keys trader.</summary>
/// <remarks>See reference/faction-vendor-panels.md#item-service-panels.</remarks>
internal static class ServiceSlotReader
{
    /// <summary>The slot's own caption ("Sacrifice", "Reroll") or its place among the trader's keys; null when unnamed.</summary>
    internal static string Read(ItemContainerUI container)
    {
        try
        {
            if (!IsAlive(container)) return null;
            return PanelIntro.KindOf(container.gameObject) switch
            {
                PanelKind.Crafting => Caption(container),
                PanelKind.KeysTrader => KeyPlace(container),
                _ => null
            };
        }
        catch (Exception) { return null; }
    }

    // Each captioned slot sits in a holder beside its own label; a slot without one is named by the panel's intro.
    private static string Caption(ItemContainerUI container)
    {
        var holder = container.transform.parent;
        if (!IsAlive(holder) || holder.GetComponentInParent<Panel>() is not { } panel || holder.Pointer == panel.transform.Pointer)
            return null;
        foreach (var label in holder.GetComponentsInChildren<TMP_Text>(false))
        {
            if (!TextUtils.IsVisibleLabel(label) || label.transform.IsChildOf(container.transform)) continue;
            string text = TextUtils.CleanText(label.text);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return null;
    }

    private static string KeyPlace(ItemContainerUI container)
    {
        var panel = container.GetComponentInParent<DungeonKeysTraderPanel>();
        var keys = IsAlive(panel) ? panel._keyContainers : null;
        if (keys is null || keys.WasCollected) return null;
        for (int i = 0; i < keys.Count; i++)
            if (IsAlive(keys[i]) && keys[i].Pointer == container.Pointer) return PanelText.KeySlot(i + 1, keys.Count);
        return null;
    }
}
