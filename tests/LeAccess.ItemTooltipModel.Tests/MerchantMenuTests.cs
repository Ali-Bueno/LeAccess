using LeAccess.Accessibility.Items;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class MerchantMenuTests
{
    internal static void Run(Action<bool, string> check)
    {
        var shopButton = new Selectable { Text = new TMP_Text { text = "SHOP" } };
        var buybackButton = new Selectable { Text = new TMP_Text { text = "Recompra" } };
        var owner = new BaseShopContainerUI { shopBtn = shopButton, buyBackBtn = buybackButton };
        var panel = new ShopPanel { _shop = owner };
        UIBase.instance = new UIBase();
        UIBase.Shop = panel;
        Localization.Texts["Shop_Panel_Title"] = "Tienda";
        check(MerchantMenuReader.TryReadTab(shopButton.gameObject, out var text) && text == "Tienda",
            "Exact merchant tab uses native localized key over serialized English TMP");
        check(MerchantMenuReader.TryReadTab(buybackButton.gameObject, out text) && text == "Recompra",
            "Exact buyback tab retains current visible localized label");
        check(!MerchantMenuReader.TryReadTab(new GameObject(), out _), "Unrelated controls cannot inherit shop tab labels");
        owner.isActiveAndEnabled = false;
        check(MerchantMenuReader.TryReadTab(buybackButton.gameObject, out _), "Inactive shop grid does not hide the separately visible buyback tab");
        owner.isActiveAndEnabled = true;
        var activeShopButton = new Selectable { Text = new TMP_Text { text = "SHOP" } };
        var activeBuybackButton = new Selectable { Text = new TMP_Text { text = "Recompra activa" } };
        var buybackOwner = new BaseShopContainerUI { shopBtn = activeShopButton, buyBackBtn = activeBuybackButton };
        panel._buyback = buybackOwner;
        check(MerchantMenuReader.TryReadTab(activeShopButton.gameObject, out text) && text == "Tienda",
            "Buyback container's separately owned shop button uses the native tab label");
        check(MerchantMenuReader.TryReadTab(activeBuybackButton.gameObject, out text) && text == "Recompra activa",
            "Buyback container's visible buyback button does not depend on the inactive shop grid's button identities");
        foreach (string missingShop in new[] { "missing", "collected", "different type" })
        {
            panel._shop = missingShop == "missing" ? null : missingShop == "different type" ? new ItemContainerUI() : owner;
            owner.WasCollected = missingShop == "collected";
            check(MerchantMenuReader.TryReadTab(activeBuybackButton.gameObject, out _),
                "Live buyback headers remain readable when shop reference is " + missingShop);
        }
        owner.WasCollected = false;
        panel._shop = owner;
        buybackOwner.WasCollected = true;
        check(!MerchantMenuReader.TryReadTab(activeBuybackButton.gameObject, out _),
            "Collected buyback owner cannot supply retained buttons");
        buybackOwner.WasCollected = false;
        activeBuybackButton.gameObject.activeInHierarchy = false;
        check(!MerchantMenuReader.TryReadTab(activeBuybackButton.gameObject, out _),
            "Hidden buyback header cannot inherit another container's visible label");
        activeBuybackButton.gameObject.activeInHierarchy = true;
        ModText.SetLanguage("es");
        buybackButton.Text.alpha = 0;
        check(MerchantMenuReader.TryReadTab(buybackButton.gameObject, out text) && text == ModText.Get("merchant.buyback"),
            "Missing visible label and native localization use mod language fallback");
        buybackButton.Text.alpha = 1;
        buybackButton.Text.canvasRenderer.cull = true;
        check(MerchantMenuReader.TryReadTab(buybackButton.gameObject, out text) && text == ModText.Get("merchant.buyback"),
            "Culled old label does not override merchant fallback");
        buybackButton.Text.canvasRenderer.cull = false;
        foreach (string gate in new[] { "closed", "hidden panel", "disabled panel", "hidden button", "disabled button", "collected owner", "collected ui" })
        {
            UIBase.Shop = gate == "closed" ? null : panel;
            panel.gameObject.activeInHierarchy = gate != "hidden panel";
            panel.isActiveAndEnabled = gate != "disabled panel";
            shopButton.gameObject.activeInHierarchy = gate != "hidden button";
            shopButton.isActiveAndEnabled = gate != "disabled button";
            owner.WasCollected = gate == "collected owner";
            UIBase.instance.WasCollected = gate == "collected ui";
            check(!MerchantMenuReader.TryReadTab(shopButton.gameObject, out _), "Merchant tab gate: " + gate);
        }
        UIBase.instance.WasCollected = false;
        UIBase.Shop = null;
        ModText.SetLanguage("en");
    }
}
