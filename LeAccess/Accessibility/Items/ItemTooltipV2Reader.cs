using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.Tooltips;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

internal static class ItemTooltipV2Reader
{
    internal static string Read(ItemTooltip tooltip)
    {
        var fragments = new List<string>();
        var template = tooltip._tooltipTemplate;
        var scroll = IsAlive(template) ? template._scrollRect : null;
        var scrollContent = IsAlive(scroll) ? scroll.content : null;
        // Primary sections exclude the comparison delta section.
        Component[] sections =
        {
            tooltip._name, tooltip._topTag, tooltip._secondaryNameSection, tooltip._unidentifiedText,
            tooltip._primaryType, tooltip._secondaryType, tooltip._potential, tooltip._description,
            tooltip._implicitsSection, tooltip._weaponRange, tooltip._weaponSpeed, tooltip._prefixesSection,
            tooltip._suffixesSection, tooltip._sealedSection, tooltip._uniqueModifiersSection,
            tooltip._setItemsSection, tooltip._setBonusesSection, tooltip._addsAffixesSection,
            tooltip._canRollOnSection, tooltip._loreText, tooltip._priceList,
            tooltip._factionRankRequirement, tooltip._requirements
        };
        foreach (var section in sections)
        {
            if (!IsAlive(section) || !UiVisibility.IsVisible(section.gameObject)) continue;
            var labels = section.gameObject.GetComponentsInChildren<TMP_Text>(false);
            if (labels is null || labels.WasCollected) continue;
            foreach (var label in labels)
            {
                if (!IsAlive(label) || !label.isActiveAndEnabled || label.alpha <= 0 ||
                    !UiVisibility.IsVisible(label.gameObject)) continue;
                bool inScrollContent = IsAlive(scrollContent) && IsAlive(label.transform) &&
                    label.transform.IsChildOf(scrollContent);
                // A tooltip's scroll mask must not hide the remaining expanded stats from speech.
                if (!inScrollContent && IsAlive(label.canvasRenderer) && label.canvasRenderer.cull) continue;
                fragments.Add(TextUtils.CleanText(label.text));
            }
        }
        fragments.AddRange(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(tooltip));
        return ScreenContentTracker.Combine(fragments);
    }
}
