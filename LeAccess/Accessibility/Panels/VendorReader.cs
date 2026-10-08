using System;
using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.Utils;
using LE.Factions;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Panels;

/// <summary>Vendor and gambler panels: tabs, the refresh cost, the currencies they trade in and their stock prices.</summary>
/// <remarks>See reference/faction-vendor-panels.md#vendors-and-gamblers.</remarks>
internal static class VendorReader
{
    private const float PollSeconds = 0.25f;
    private static readonly CurrencyChanges Changes = new();
    private static readonly List<(ItemVendorCurrencyType Type, TMP_Text Label)> Currencies = new();
    private static IntPtr _resolvedFor;
    private static float _next;

    /// <summary>"You have ..." for the currencies the open vendor trades in, or null.</summary>
    internal static string IntroLine(Panel panel)
    {
        Resolve(panel);
        var lines = Read();
        return PanelText.YouHave(string.Join(", ", lines.ConvertAll(line => line.Value)));
    }

    /// <summary>Every frame: after a purchase, sale or refresh, speaks the currency that changed.</summary>
    internal static void Update()
    {
        try
        {
            if (!PanelIntro.TryGetCurrent(out var panel, out var kind) || kind is not (PanelKind.Vendor or PanelKind.Gambling))
            { Forget(); return; }
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + PollSeconds;
            if (_resolvedFor != panel.Pointer) Resolve(panel);
            string changed = Changes.Observe(Read());
            if (changed != null && ScreenReader.IsAvailable()) ScreenReader.Speak(changed, interrupt: false);
        }
        catch (Exception ex) { Forget(); Plugin.Log.LogWarning($"Vendor currency unavailable: {ex.Message}"); }
    }

    internal static void Reset() => Forget();

    internal static bool TryReadControl(GameObject element, PanelKind kind, out string text)
    {
        text = null;
        if (kind is not (PanelKind.Vendor or PanelKind.Gambling)) return false;
        var tab = element.GetComponent<TabbedUITab>();
        if (IsAlive(tab))
        {
            string name = TextUtils.GetTextFromHierarchy(element);
            if (string.IsNullOrWhiteSpace(name)) return false;
            TabPlace(element.transform, out int place, out int count);
            text = PanelText.Tab(name, tab.IsActive, place, count);
            return true;
        }
        var refresh = element.GetComponent<TabbedShopItemContainerRefreshButton>();
        if (!IsAlive(refresh)) return false;
        string label = Labels(element);
        if (string.IsNullOrWhiteSpace(label)) return false;
        text = PanelText.Refresh(label, IsAlive(refresh.button) && refresh.button.IsInteractable());
        return true;
    }

    /// <summary>The slot type and price wording the tooltip system uses for an item of this vendor's stock or buyback.</summary>
    internal static bool TryPrice(ItemContainerUI container, out TooltipItemManager.SlotType slot, out string priceKey)
    {
        slot = TooltipItemManager.SlotType.UNDEFINED;
        priceKey = null;
        if (!IsAlive(container) || PanelIntro.KindOf(container.gameObject) is not (PanelKind.Vendor or PanelKind.Gambling)) return false;
        var panel = container.GetComponentInParent<Panel>();
        if (!PanelFamily.TryVendor(panel, out var stock, out var buyback, out slot)) return false;
        if (IsAlive(stock) && stock.Pointer == container.Pointer) priceKey = "merchant.buy_price";
        else if (IsAlive(buyback) && buyback.Pointer == container.Pointer)
        {
            slot = TooltipItemManager.SlotType.SHOP;
            priceKey = "merchant.buyback_price";
        }
        return priceKey != null;
    }

    private static void Resolve(Panel panel)
    {
        Currencies.Clear();
        Changes.Reset();
        _resolvedFor = panel.Pointer;
        foreach (var bones in panel.GetComponentsInChildren<UIAncientBonesText>(false))
            if (IsAlive(bones) && IsAlive(bones.ancientBonesText)) Currencies.Add((ItemVendorCurrencyType.AncientBones, bones.ancientBonesText));
        foreach (var favor in panel.GetComponentsInChildren<UIFavorText>(false))
        {
            if (!IsAlive(favor) || !IsAlive(favor.favorText)) continue;
            var type = favor.factionID switch
            {
                FactionID.CircleOfFortune => ItemVendorCurrencyType.CircleOfFortuneFavor,
                FactionID.MerchantsGuild => ItemVendorCurrencyType.MerchantsGuildFavor,
                FactionID.TheWeaver => ItemVendorCurrencyType.MemoryAmber,
                _ => (ItemVendorCurrencyType?)null
            };
            if (type is { } known) Currencies.Add((known, favor.favorText));
        }
        foreach (var embers in panel.GetComponentsInChildren<SoulEmberCounterUI>(false))
            if (IsAlive(embers) && IsAlive(embers.text)) Currencies.Add((ItemVendorCurrencyType.SoulEmbers, embers.text));
        if (panel.TryCast<TimeglassVendorPanel>() is not null)
            foreach (var fragments in panel.GetComponentsInChildren<UIKeyText>(false))
                if (IsAlive(fragments) && IsAlive(fragments.amountText))
                { Currencies.Add((ItemVendorCurrencyType.TimeglassFragments, fragments.amountText)); break; }
    }

    private static List<KeyValuePair<string, string>> Read()
    {
        var lines = new List<KeyValuePair<string, string>>();
        foreach (var (type, label) in Currencies)
        {
            if (!IsAlive(label) || !int.TryParse(Digits(label.text), out int amount)) continue;
            string text = TextUtils.CleanText(new ItemTooltipPriceInfo(type, amount, true).ToSearchableString());
            if (!string.IsNullOrWhiteSpace(text)) lines.Add(new KeyValuePair<string, string>(type.ToString(), text));
        }
        return lines;
    }

    private static string Digits(string text)
    {
        var digits = new System.Text.StringBuilder();
        foreach (char c in text ?? "")
            if (char.IsDigit(c)) digits.Append(c);
        return digits.ToString();
    }

    private static void TabPlace(Transform tab, out int place, out int count)
    {
        place = count = 0;
        var bar = tab.parent;
        if (!IsAlive(bar)) return;
        for (int i = 0; i < bar.childCount; i++)
        {
            var sibling = bar.GetChild(i);
            if (!sibling.gameObject.activeInHierarchy || !IsAlive(sibling.GetComponent<TabbedUITab>())) continue;
            count++;
            if (sibling.Pointer == tab.Pointer) place = count;
        }
    }

    // The button's own words and cost, without the "|" divider the game draws between them.
    private static string Labels(GameObject button)
    {
        var parts = new List<string>();
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(false))
        {
            if (!TextUtils.IsVisibleLabel(label)) continue;
            string text = TextUtils.CleanText(label.text);
            if (!string.IsNullOrWhiteSpace(text) && text.Any(char.IsLetterOrDigit)) parts.Add(text);
        }
        return LeAccess.Accessibility.ScreenContent.ScreenContentTracker.Combine(parts);
    }

    private static void Forget()
    {
        Currencies.Clear();
        Changes.Reset();
        _resolvedFor = IntPtr.Zero;
        _next = 0;
    }
}
