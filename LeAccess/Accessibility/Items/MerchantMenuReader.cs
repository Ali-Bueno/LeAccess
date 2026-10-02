using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

internal static class MerchantMenuReader
{
    internal static bool TryReadTab(GameObject element, out string text)
    {
        text = null;
        try
        {
            if (!IsAlive(element) || !UiVisibility.IsVisible(element) || !IsAlive(UIBase.instance)) return false;
            var panel = UIBase.GetPanelIfOpen<ShopPanel>();
            if (!IsAlive(panel) || !panel.isActiveAndEnabled || !UiVisibility.IsVisible(panel.gameObject) ||
                !IsAlive(panel._shop)) return false;
            var owner = panel._shop.TryCast<BaseShopContainerUI>();
            if (!IsAlive(owner)) return false;
            if (Matches(element, owner.shopBtn))
                text = ReadLabel(owner.shopBtn, "Shop_Panel_Title", "merchant.shop");
            else if (Matches(element, owner.buyBackBtn))
                text = ReadLabel(owner.buyBackBtn, "Shop_Panel_BuyBack_Button", "merchant.buyback");
            return !string.IsNullOrWhiteSpace(text);
        }
        catch (Exception) { text = null; return false; }
    }

    private static bool Matches(GameObject element, Selectable button) => IsAlive(button) &&
        button.isActiveAndEnabled && IsAlive(button.gameObject) && button.gameObject.Pointer == element.Pointer;

    private static string ReadLabel(Selectable button, string key, string fallbackKey)
    {
        var label = button.GetComponentInChildren<TMP_Text>();
        string fallback = IsAlive(label) && label.isActiveAndEnabled && label.alpha > 0 &&
            UiVisibility.IsVisible(label.gameObject) && (!IsAlive(label.canvasRenderer) || !label.canvasRenderer.cull)
            ? TextUtils.CleanText(label.text) : null;
        if (string.IsNullOrWhiteSpace(fallback)) fallback = ModText.Get(fallbackKey);
        return TextUtils.CleanText(Localization.GetText(key, fallback));
    }
}
