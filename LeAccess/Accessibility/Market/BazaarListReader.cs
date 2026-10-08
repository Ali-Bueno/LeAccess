using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.Bazaar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the Bazaar's list-an-item form: the item chosen, its suggested price, the asking price field, fees and the confirm button.</summary>
/// <remarks>Gold is the only currency; fees are the game's own summaries. See reference/faction-vendor-panels.md#bazaar-market.</remarks>
internal static class BazaarListReader
{
    private static string _item;
    private static string _suggestion;

    internal static bool TryRead(GameObject element, BazaarUI ui, out string text)
    {
        text = null;
        var form = ui.listItemUI;
        if (!IsAlive(form) || !element.transform.IsChildOf(form.transform)) return false;
        if (Inside(form._listButton, element)) text = ConfirmButton(form);
        else if (Inside(form._stashButton, element)) text = MarketText.SourceButton(true, Visible(form._stashButton.GetComponentInChildren<TMP_Text>()));
        else if (Inside(form._inventoryButton, element)) text = MarketText.SourceButton(false, Visible(form._inventoryButton.GetComponentInChildren<TMP_Text>()));
        else if (IsAlive(form._askingPriceInput) && element.transform.IsChildOf(form._askingPriceInput.transform))
            text = AskingPrice(form);
        else if (IsAlive(form._priceLoweringEnabledCheckbox) && element.transform.IsChildOf(form._priceLoweringEnabledCheckbox.transform))
            text = MarketText.Checkbox(DropdownReader.Title(form._priceLoweringEnabledCheckbox), form._priceLoweringEnabledCheckbox.Value);
        return text != null;
    }

    private static string ConfirmButton(ListItemUI form)
    {
        string button = MarketText.ListButton(Visible(form._buttonText), Visible(form._listFavorCost), Visible(form._listPriceTaxSummary),
            Visible(form._listPriceLoweringSummary));
        return form._listButton.IsInteractable() ? button : ModText.Format("control.disabled", button);
    }

    private static string AskingPrice(ListItemUI form)
    {
        var field = form._askingPriceInput._input;
        if (!IsAlive(field)) return null;
        string title = DropdownReader.Title(form._askingPriceInput) ?? DropdownReader.Title(field) ?? ModText.Get("market.list_price_label");
        return ScreenContentTracker.Combine(new[]
            { MarketText.InputField(title, field.text, gold: true), Visible(form._listPriceTaxSummary) });
    }

    /// <summary>Says the item placed in the form once (and its suggested price once it arrives); the empty slot as well.</summary>
    internal static string Notice(BazaarUI ui)
    {
        var form = ui.listItemUI;
        var details = IsAlive(form) && form.IsActive ? form._itemDetails : null;
        if (!IsAlive(details)) { _item = _suggestion = null; return null; }
        string name = Visible(details._itemName);
        if (name == null)
        {
            string empty = IsAlive(form._noItemPlaced) && form._noItemPlaced.activeInHierarchy
                ? MarketText.ListEmpty(TextUtils.GetTextFromHierarchy(form._noItemPlaced)) : null;
            _suggestion = null;
            return Once(ref _item, "-", empty);
        }
        if (name != _item)
        {
            _item = name;
            _suggestion = null;
            return MarketText.ListItem(name, Visible(details._itemDetails), null);
        }
        var price = IsAlive(details._priceSuggestion) ? details._priceSuggestion : null;
        string suggested = IsAlive(price) && IsAlive(price._priceSuggestionValueWrapper) && price._priceSuggestionValueWrapper.activeInHierarchy
            ? Visible(price._priceSuggestionValue) : null;
        return Once(ref _suggestion, suggested, suggested == null ? null : MarketText.ListItem(null, null, suggested));
    }

    internal static void Reset() { _item = _suggestion = null; }

    private static string Once(ref string seen, string key, string text)
    {
        if (text == null || key == seen) return null;
        seen = key;
        return text;
    }

    private static bool Inside(Component root, GameObject element) => IsAlive(root) && element.transform.IsChildOf(root.transform);

    private static string Visible(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text) ? TextUtils.CleanText(label.text) : null;
}
