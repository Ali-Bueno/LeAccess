using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>The bazaar purchase confirmation: its gold, favor and token amounts, which the game shows as bare numbers.</summary>
/// <remarks>See reference/faction-vendor-panels.md#bazaar-purchase-confirmation.</remarks>
internal static class CostModalReader
{
    /// <summary>For the bazaar confirmation: the amount labels the generic reader must skip and the named costs to add.</summary>
    internal static bool TryCreate(Panel panel, out Func<Component, bool> skip, out Func<string> extra)
    {
        skip = null;
        extra = null;
        var modal = panel.TryCast<BazaarPurchaseConfirmationModal>();
        if (!IsAlive(modal)) return false;
        skip = label => Inside(label, modal._goldCostWrapper) || Inside(label, modal._favorCostWrapper) ||
            Inside(label, modal._merchantTokenCostWrapper);
        extra = () => Read(modal);
        return true;
    }

    private static string Read(BazaarPurchaseConfirmationModal modal)
    {
        try
        {
            var costs = new List<string>();
            Add(costs, modal._goldCostWrapper, modal._goldCost, ItemVendorCurrencyType.Gold);
            Add(costs, modal._favorCostWrapper, modal._favorCost, ItemVendorCurrencyType.MerchantsGuildFavor);
            var tokens = IsAlive(modal._merchantTokenCostWrapper) && modal._merchantTokenCostWrapper.activeInHierarchy
                ? modal._merchantTokenCostWrapper.GetComponentInChildren<TMP_Text>() : null;
            if (IsAlive(tokens) && Amount(tokens) is { } count) costs.Add(PanelText.MerchantTokens(count.ToString()));
            return costs.Count == 0 ? null : PanelText.Cost(costs);
        }
        catch (Exception) { return null; }
    }

    private static void Add(List<string> costs, GameObject wrapper, TMP_Text label, ItemVendorCurrencyType type)
    {
        if (!IsAlive(wrapper) || !wrapper.activeInHierarchy || !IsAlive(label) || Amount(label) is not { } amount) return;
        string text = TextUtils.CleanText(new ItemTooltipPriceInfo(type, amount, true).ToSearchableString());
        if (!string.IsNullOrWhiteSpace(text)) costs.Add(text);
    }

    private static int? Amount(TMP_Text label)
    {
        var digits = new System.Text.StringBuilder();
        foreach (char c in label.text ?? "")
            if (char.IsDigit(c)) digits.Append(c);
        return int.TryParse(digits.ToString(), out int value) ? value : null;
    }

    private static bool Inside(Component label, GameObject root) =>
        IsAlive(root) && label.transform.IsChildOf(root.transform);
}
