using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.Bazaar;
using LE.UI.MultiPicker.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the Bazaar's filter controls: dropdowns, min/max and text fields and the multi-selection openers, each with its title.</summary>
/// <remarks>Titles are the sibling labels of the prefab rows. See reference/faction-vendor-panels.md#bazaar-market.</remarks>
internal static class BazaarControlReader
{
    internal static bool TryRead(GameObject element, BazaarUI ui, out string text)
    {
        text = null;
        // Options of an expanded list are read by the dropdown reader before the panel readers.
        if (IsAlive(element.GetComponentInParent<Toggle>()) && IsAlive(element.GetComponentInParent<TMP_Dropdown>())) return false;
        var dropdown = element.GetComponent<TMP_Dropdown>() ?? element.GetComponentInParent<TMP_Dropdown>();
        if (IsAlive(dropdown)) return Say(DropdownText(ui, dropdown), dropdown, out text);
        var picker = element.GetComponentInParent<MultiPickerOpener>();
        if (IsAlive(picker)) return Say(DropdownReader.Control(DropdownReader.Title(picker), Visible(picker.label)), null, out text);
        var selection = element.GetComponentInParent<MultiSelectionOpener>();
        if (IsAlive(selection)) return Say(DropdownReader.Control(DropdownReader.Title(selection), Visible(selection.label)), null, out text);
        var field = element.GetComponent<TMP_InputField>() ?? element.GetComponentInParent<TMP_InputField>();
        return IsAlive(field) && Say(Field(field), null, out text);
    }

    private static bool Say(string spoken, TMP_Dropdown dropdown, out string text)
    {
        text = spoken;
        if (dropdown != null) DropdownReader.NoteSpoken(dropdown);
        return !string.IsNullOrWhiteSpace(text);
    }

    // The stall type and sort dropdowns hide their title in the prefab, so they are named from the panel's own references.
    private static string DropdownText(BazaarUI ui, TMP_Dropdown dropdown)
    {
        string title = DropdownReader.Title(dropdown);
        if (title == null && IsAlive(ui.stallTypeDropdown) && Is(ui.stallTypeDropdown.dropdown, dropdown)) title = ModText.Get("market.stall_type");
        var sorting = IsAlive(ui.filterUI) && IsAlive(ui.filterUI.sortingSelection) ? ui.filterUI.sortingSelection.dropdown : null;
        if (title == null && IsAlive(sorting) && Is(sorting, dropdown)) title = ModText.Get("market.sorting");
        return DropdownReader.Control(title, DropdownReader.Caption(dropdown));
    }

    private static string Field(TMP_InputField field)
    {
        string title = DropdownReader.Title(field);
        string value = field.text;
        var number = field.GetComponentInParent<NumberInput>();
        string suffix = IsAlive(number) ? Visible(number._suffix) : null;
        int index = RangeIndex(field);
        return index >= 0 ? MarketText.RangeField(title, index > 0, value) : MarketText.InputField(title, value, suffix);
    }

    // Min / max pairs are two input fields under one titled row; the first is the minimum.
    private static int RangeIndex(TMP_InputField field)
    {
        var parent = field.transform.parent;
        if (!IsAlive(parent)) return -1;
        int seen = 0, mine = -1;
        for (int i = 0; i < parent.childCount; i++)
        {
            var other = parent.GetChild(i).GetComponentInChildren<TMP_InputField>();
            if (!IsAlive(other) || !other.gameObject.activeInHierarchy) continue;
            if (other.Pointer == field.Pointer) mine = seen;
            seen++;
        }
        return seen == 2 ? mine : -1;
    }

    private static bool Is(Component a, Component b) => IsAlive(a) && IsAlive(b) && a.Pointer == b.Pointer;

    private static string Visible(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text) ? TextUtils.CleanText(label.text) : null;
}
