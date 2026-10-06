using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class ForgeSummaryTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        TextMeshProUGUI Label(GameObject parent, string text) => parent.Child().Add(new TextMeshProUGUI { text = text });
        reset(); LeAccess.I18n.ModText.SetLanguage("en");
        var forge = new GameObject().Add(new CraftingSlotManager { canForge = true });
        var row = forge.gameObject.Child().Add(new AffixSlotForge());
        row.nameTMP = Label(row.gameObject, "Added fire damage");
        row.addAffixButton = row.gameObject.Child().Add(new Button());
        row.addAffixButton.gameObject.Add(new Graphic());
        var rect = row.addAffixButton.gameObject.Add(new RectTransform());
        forge.activeNavigationElements = new Il2CppSystem.Array(new NativeObject[,] { { rect } });
        forge.singleSuccessChanceString = Label(forge.gameObject, "Will add <color=#FFFFFF>3 random affixes</color>");
        forge.modifierDetails = Label(forge.gameObject, "Rerolls the values of all affixes.");
        forge.glyphsPreviewText = Label(forge.gameObject, "Glyph of Hope may prevent forging potential loss.");
        forge.typeOfForgeText = Label(forge.gameObject, "Forging Potential");
        forge.currentForgingPotentialText = Label(forge.gameObject, "19");
        forge.cannotForgeText = Label(forge.gameObject, "Retained unavailable reason");
        forge.forgeButton = forge.gameObject.Child().Add(new Button());
        forge.forgeButton.gameObject.Add(new Graphic());
        Label(forge.forgeButton.gameObject, "Forge");
        NavigationManager.currentlySelected = forge;
        ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join(Speech.T("crafting.add_affix"), "Added fire damage"), "Forge navigation keeps brief selected-control content");
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out var details) && details.Contains("Will add 3 random affixes"), "Manual forge inspection recovers the visible native preview");
        Check(details.Contains("Rerolls the values of all affixes.") && details.Contains("Glyph of Hope may prevent"), "Manual forge details retain readable modifier and glyph preview text");
        Check(details.Contains("Forging Potential: 19") && !details.Contains("Retained unavailable"), "Ready forging uses its visible authored potential heading and excludes stale failure reasons");
        forge.canForge = false; forge.forgeButton.gameObject.Active = false;
        forge.cannotForgeText.text = "This item has no forging potential.";
        reset(); NavigationManager.currentlySelected = forge;
        ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join(Speech.T("crafting.add_affix"), "Added fire damage"), "Blocked forging does not replace the selected affix's brief speech");
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && details.EndsWith("This item has no forging potential."), "Exact unavailable reason remains readable from a live control when native forge action is hidden");
        Check(!details.Contains("Will add") && !details.Contains("Rerolls") && !details.Contains("Forging Potential:"), "Blocked forging excludes retained ready preview fields");
        forge.cannotForgeText.text = "Este objeto ya tiene cuatro afijos.";
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && details.Contains("Este objeto ya tiene cuatro afijos."), "Unavailable reasons are read afresh in the game's language");
        forge.cannotForgeText.gameObject.Active = false;
        Check(!ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out _), "Hidden unavailable reasons do not replay old failure content"); forge.cannotForgeText.gameObject.Active = true;
        var matrix = (Il2CppSystem.Array)forge.activeNavigationElements;
        var actionRect = forge.forgeButton.gameObject.Add(new RectTransform());
        matrix.Cells[0, 0] = actionRect;
        Check(!ElementAnnouncer.TryReadDetails(forge.forgeButton.gameObject, out _), "The native hidden forge action cannot be inspected through retained focus"); matrix.Cells[0, 0] = rect;
        forge.canForge = true; forge.forgeButton.gameObject.Active = true;
        forge.singleSuccessChanceString.text = "Will upgrade the selected affix.";
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && details.Contains("Will upgrade") && !details.Contains("Este objeto"), "A resumed forge drops old failure speech immediately");
        matrix.Cells[0, 0] = actionRect;
        Check(ElementAnnouncer.TryReadDetails(forge.forgeButton.gameObject, out details) && details.Contains("Will upgrade"), "The live native forge action shares manual preview reading");
        Check(!ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out _), "Switching to the forge action immediately rejects the old affix selection"); matrix.Cells[0, 0] = rect;
        forge.glyphsPreviewText.text = "<size=25><sprite=1></size> and maybe";
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && !details.Contains("and maybe"), "Unresolved glyph icons cannot become misleading partial sentences");
        forge.singleSuccessChanceString.text = "<SPRITE name=unresolved> will be added";
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && !details.Contains("will be added"), "Unresolved symbols in a native preview exclude that entire fragment");
        forge.singleSuccessChanceString.text = "Will upgrade the selected affix.";
        forge.modifierDetails.gameObject.Active = false;
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && !details.Contains("Rerolls"), "Hidden native modifier details stay silent"); forge.modifierDetails.gameObject.Active = true;
        forge.currentForgingPotentialText = Label(new GameObject(), "999");
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && !details.Contains("999") && !details.Contains("Forging Potential:"), "Foreign quantities cannot borrow the forge's native heading");
        forge.singleSuccessChanceString = Label(new GameObject(), "Foreign preview");
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && !details.Contains("Foreign preview"), "Foreign serialized preview labels are excluded");
        forge.singleSuccessChanceString = Label(forge.gameObject, "<b></b>"); forge.modifierDetails.text = "   ";
        Check(!ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out _), "Blank, markup-only and unresolved preview fields do not invent manual details");
        var tooltip = row.addAffixButton.gameObject.Add(new InfoForTooltip { tooltipInfo = new TooltipInfo { nameText = "Affix", mainText = "Owned affix description." } });
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && details.Contains("Owned affix description."), "Missing forge summary preserves the existing owned tooltip details");
        forge.singleSuccessChanceString.text = "Will upgrade the selected affix.";
        Check(ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out details) && details.Contains("Owned affix description.") && details.Contains("Will upgrade"), "Forge summary adds to existing selected tooltip descriptions");
        reset(); NavigationManager.currentlySelected = forge; ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join(Speech.T("crafting.add_affix"), "Added fire damage"), "Manual summary never leaks into later brief navigation");
        Check(tooltip.SetTextCalls == 2, "Brief forge navigation never invokes the manual tooltip population path");
        forge.currentlySelectedIdx = new(1, 0);
        Check(!ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out _), "Unready native forge selection cannot inspect retained controls"); forge.currentlySelectedIdx = new(0, 0);
        foreach (string gate in new[] { "closed", "disabled", "collected", "loading", "background", "popup" })
        {
            reset(); NavigationManager.currentlySelected = forge;
            forge.gameObject.Active = gate != "closed"; forge.Enabled = gate != "disabled"; forge.WasCollected = gate == "collected";
            LE.UI.LoadingScreen.IsVisible = gate == "loading"; Application.isFocused = gate != "background";
            ScreenContentAnnouncer.HasActiveContent = gate == "popup"; ScreenContentAnnouncer.Root = new GameObject();
            Check(!ElementAnnouncer.TryReadDetails(row.addAffixButton.gameObject, out _), "Forge manual summary rejects " + gate);
            forge.gameObject.Active = forge.Enabled = true; forge.WasCollected = false;
        }
        reset(); return checks;
    }
}
