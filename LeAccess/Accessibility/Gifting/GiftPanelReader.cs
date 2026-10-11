using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.Factions;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Gifting;

internal readonly record struct GiftPanel(ItemGiftPopupNavigable Navigable, ItemGiftingPanelUI Ui, ItemGiftingPanel Panel);

internal readonly record struct GiftStop(string Key, string Text);

/// <summary>Reads the item gifting panel the game opens from a party member's "Regalar objeto".</summary>
/// <remarks>See reference/social-reading.md#item-gifting-2026-10-07.</remarks>
internal static class GiftPanelReader
{
    private static IntPtr _slottedField;

    /// <summary>The gifting panel while its own navigable owns controller focus.</summary>
    internal static bool TryGet(out GiftPanel panel)
    {
        panel = default;
        var navigation = NavigationManager.currentlySelected;
        var popup = navigation is null || navigation.WasCollected ? null : navigation.TryCast<ItemGiftPopupNavigable>();
        if (!IsAlive(popup) || !popup.isActiveAndEnabled || !UiVisibility.IsVisible(popup.gameObject)) return false;
        var owner = popup.GetComponent<ItemGiftingPanel>();
        var ui = IsAlive(owner) ? owner._itemGiftingPanelUI : null;
        if (!IsAlive(ui) || !Same(owner._navigable, popup)) return false;
        panel = new GiftPanel(popup, ui, owner);
        return true;
    }

    /// <summary>The stop the game selected (`_selectables[_selectedIndex]`), with its state and place.</summary>
    internal static bool TryReadStop(GiftPanel panel, out GiftStop stop)
    {
        stop = default;
        var stops = panel.Navigable._selectables;
        int index = panel.Navigable._selectedIndex;
        if (stops is null || stops.WasCollected || index < 0 || index >= stops.Count) return false;
        var root = stops[index];
        if (!IsAlive(root) || !root.transform.IsChildOf(panel.Navigable.transform)) return false;
        string text = Describe(panel, root);
        if (string.IsNullOrWhiteSpace(text)) return false;
        stop = new GiftStop($"{root.GetInstanceID()}",
            ScreenContentTracker.Combine(new[] { text, ListPosition.Text(index + 1, stops.Count) }));
        return true;
    }

    /// <summary>The panel's own text: title, recipient and the gifting rule, in reading order.</summary>
    internal static string Intro(GiftPanel panel)
    {
        var ui = panel.Ui;
        var stops = panel.Navigable._selectables;
        return ScreenContentReader.Read(ui.gameObject, null, null, include: label =>
            !Inside(label, ui.resonancesPopup) && !Inside(label, ui.resonanceRequiredBox) && !InsideStop(label, stops));
    }

    /// <summary>A row of the resonance pop-up: its kind, the player it belongs to and how many there are.</summary>
    internal static bool TryReadResonanceRow(GameObject element, out string text)
    {
        text = null;
        try
        {
            var row = element.GetComponent<GiftResonanceUI>();
            var ui = IsAlive(row) ? row.GetComponentInParent<ItemGiftingPanelUI>() : null;
            if (!IsAlive(ui) || !UiVisibility.IsVisible(element) ||
                !Same(row, ui.goldResonanceUI) && !Same(row, ui.obsidianResonanceUI)) return false;
            bool obsidian = row.resonanceType == ResonanceType.OBSIDIAN;
            text = GiftText.ResonanceRow(obsidian ? GiftResonance.Obsidian : GiftResonance.Gold, Label(row.playerText, element),
                Label(obsidian ? row.obsidianAmountText : row.goldAmountText, element));
            if (IsAlive(row.button) && !row.button.IsInteractable()) text = ModText.Format("control.disabled", text);
            return true;
        }
        catch (Exception) { text = null; return false; }
    }

    /// <summary>The name of the item waiting in the gift slot, or null.</summary>
    internal static string SlotItem(ItemGiftingPanel panel)
    {
        var slot = IsAlive(panel) ? panel._giftItemContainer : null;
        var items = IsAlive(slot) ? slot.Container : null;
        var content = items is null || items.WasCollected ? null : items.GetContent();
        if (content is null || content.WasCollected || content.Count == 0) return null;
        var entry = content[0];
        return entry is null || entry.WasCollected ? null : Forge.ForgeCellReader.NameOf(entry.data);
    }

    private static string Describe(GiftPanel panel, GameObject root)
    {
        var ui = panel.Ui;
        if (IsStop(root, ui.confirmGiftItemButton))
        {
            var box = ui.resonanceRequiredBox;
            string requirement = IsAlive(box) && UiVisibility.IsVisible(box) ? TextUtils.GetTextFromHierarchy(box) : null;
            return GiftText.Confirm(TextUtils.GetTextFromHierarchy(root), ui.confirmGiftItemButton.IsInteractable(),
                SlotItem(panel.Panel), requirement);
        }
        if (IsStop(root, ui.showResonancesPopup)) return GiftText.Resonance(Slotted(ui));
        if (IsStop(root, ui.closeButtton)) return GiftText.Close();
        var slot = panel.Panel._giftItemContainer;
        if (IsAlive(slot) && root.Pointer == slot.gameObject.Pointer) return GiftText.Slot(SlotItem(panel.Panel));
        return TextUtils.GetTextFromHierarchy(root);
    }

    // The game keeps the slotted resonance only in a native Nullable<ResonanceType>, which the interop wrapper cannot
    // read while empty: hasValue is its first byte, the byte-sized value the second (get_SlottedResonance 0x17A59B0).
    private static GiftResonance Slotted(ItemGiftingPanelUI ui)
    {
        if (_slottedField == IntPtr.Zero)
        {
            _slottedField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ItemGiftingPanelUI>.NativeClassPtr,
                "<SlottedResonance>k__BackingField");
            if (_slottedField == IntPtr.Zero) throw new MissingFieldException("ItemGiftingPanelUI.SlottedResonance");
        }
        int offset = (int)IL2CPP.il2cpp_field_get_offset(_slottedField);
        if (Marshal.ReadByte(ui.Pointer, offset) == 0) return GiftResonance.None;
        return (ResonanceType)Marshal.ReadByte(ui.Pointer, offset + 1) == ResonanceType.OBSIDIAN
            ? GiftResonance.Obsidian : GiftResonance.Gold;
    }

    private static bool IsStop(GameObject root, Button button) => IsAlive(button) && root.Pointer == button.gameObject.Pointer;

    private static bool InsideStop(Component label, Il2CppSystem.Collections.Generic.List<GameObject> stops)
    {
        if (stops is null || stops.WasCollected) return false;
        for (int i = 0; i < stops.Count; i++)
            if (Inside(label, stops[i])) return true;
        return false;
    }

    private static bool Inside(Component label, GameObject root) =>
        IsAlive(root) && label.transform.IsChildOf(root.transform);

    private static string Label(TMP_Text label, GameObject owner)
    {
        string value = TextUtils.IsVisibleLabel(label) && label.transform.IsChildOf(owner.transform) ? TextUtils.CleanText(label.text) : null;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool Same(Component first, Component second) => IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;
}
