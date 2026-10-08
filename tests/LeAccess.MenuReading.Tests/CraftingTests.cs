using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class CraftingTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        TextMeshProUGUI Label(GameObject parent, string value) => parent.Child().Add(new TextMeshProUGUI { text = value });
        reset(); LeAccess.I18n.ModText.SetLanguage("en");
        var root = new GameObject();
        var row = root.Child("ModSlot").Add(new AffixSlotForge());
        row.nameTMP = Label(row.gameObject.Child("affixDescHolder"), "Increased health");
        row.tierTMP = Label(row.nameTMP.gameObject.Parent, "T3");
        row.countTMP = Label(row.gameObject.Child("AvailableShardsofSlottedType"), "12");
        row.addAffixButton = row.gameObject.Child("addShardButton").Add(new Button());
        var upgrade = row.gameObject.Child("upgradeAvailable").Add(new CraftingUpgradeButton());
        upgrade.affixSlotForge = row; upgrade._upgradeButton = upgrade.gameObject.Add(new Button()); row.upgradeButton = upgrade;
        var tooltip = upgrade.gameObject.Add(new InfoForTooltip { tooltipInfo = new TooltipInfo { nameText = "Affix", mainText = "Improves the current affix." } });
        ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join(Speech.T("crafting.upgrade_affix"), "Increased health", "T3", Speech.F("crafting.shards", 12)),
            "An icon-only upgrade reads its owned affix, tier and quantity despite a second row button");
        Check(tooltip.SetTextCalls == 0, "Brief crafting labels do not construct a tooltip model");
        Check(ElementAnnouncer.TryReadDetails(upgrade.gameObject, out var upgradeDetails) && upgradeDetails.EndsWith("Improves the current affix."),
            "Manual crafting inspection retains the existing associated tooltip description");
        Check(tooltip.SetTextCalls == 1, "Manual inspection retains the existing native tooltip population path");
        reset(); ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join(Speech.T("crafting.add_affix"), "Increased health", "T3", Speech.F("crafting.shards", 12)), "The add button retains its distinct action");
        reset(); upgrade._upgradeButton.Interactable = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text.EndsWith(Speech.DisabledMark()), "Disabled crafting actions retain unavailable state");
        reset(); row.gameObject.name = "Disabled ModSlot"; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text.Split(Speech.DisabledMark()).Length == 2, "Native and ancestor disabled states wrap only once");
        row.gameObject.name = "ModSlot";
        upgrade._upgradeButton.Interactable = true;
        reset(); row.nameTMP.text = "Added fire damage"; row.tierTMP.text = "T4"; row.countTMP.text = "11";
        ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text.Contains(Speech.Join("Added fire damage", "T4", Speech.F("crafting.shards", 11))), "Reused slots read current content");
        reset(); row.nameTMP.gameObject.Active = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.T("crafting.upgrade_affix"), "A hidden affix does not leak tier or old quantity");
        row.nameTMP.gameObject.Active = true;
        foreach (string empty in new[] { "", "   ", "<b></b>" })
        {
            reset(); row.nameTMP.text = empty; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
            Check(ScreenReader.Messages.Single().Text == Speech.T("crafting.upgrade_affix"), "Empty or markup-only affixes do not leak tier or quantity");
        }
        row.nameTMP.text = "Added fire damage";
        reset(); row.countTMP.gameObject.Active = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains(Speech.F("crafting.shards", "").Trim()), "Hidden quantities stay silent");
        row.countTMP.gameObject.Active = true;
        reset(); row.tierTMP = Label(new GameObject(), "Foreign tier"); ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains("Foreign"), "Foreign serialized labels are rejected");
        reset(); upgrade.affixSlotForge = root.Child().Add(new AffixSlotForge()); ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(!ScreenReader.Messages.Single().Text.StartsWith(Speech.T("crafting.upgrade_affix")), "Replaced upgrade ownership is rejected");
        upgrade.affixSlotForge = row;
        reset(); var other = row.gameObject.Child("OtherButton").Add(new Button()); ElementAnnouncer.AnnounceElement(other.gameObject);
        Check(ScreenReader.Messages.All(message => !message.Text.StartsWith(Speech.T("crafting.add_affix")) && !message.Text.StartsWith(Speech.T("crafting.upgrade_affix"))),
            "Unrelated row controls are not assigned an action");
        reset(); root.Active = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Count == 0, "Closed forge rejects retained focus"); root.Active = true;
        reset(); ScreenContentAnnouncer.HasActiveContent = true; ScreenContentAnnouncer.Root = new GameObject();
        ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Count == 0, "Foreground popup suppresses background forge");
        reset(); LeAccess.I18n.ModText.SetLanguage("es"); ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text.StartsWith(Speech.Join(Speech.T("crafting.add_affix"), "Added fire damage")) && ScreenReader.Messages.Single().Text.EndsWith(Speech.F("crafting.shards", 11)),
            "Action labels localize without translating native content");
        reset(); LeAccess.I18n.ModText.SetLanguage("en");
        var shard = root.Child().Add(new ShardAffixListElement());
        shard.button = shard.gameObject.Child("Button"); shard.button.Add(new Button());
        shard.tmpName = Label(shard.gameObject, "Increased health"); shard.tmpAffixType = Label(shard.gameObject, "Prefix");
        shard.tmpQuantity = Label(shard.gameObject, "9"); shard.improvementTMP = Label(shard.gameObject, "Adds 12 health.");
        ElementAnnouncer.AnnounceElement(shard.button);
        Check(ScreenReader.Messages.Single().Text == Speech.Join("Increased health", "Prefix", Speech.F("crafting.shards", 9)), "Pooled shard buttons read exact owned fields");
        Check(ElementAnnouncer.TryReadDetails(shard.button, out var details) && details.EndsWith("Adds 12 health."), "Manual shard details include rendered improvement");
        reset(); shard.tmpName.text = "Fire resistance"; shard.tmpQuantity.text = "2"; ElementAnnouncer.AnnounceElement(shard.button);
        Check(ScreenReader.Messages.Single().Text == Speech.Join("Fire resistance", "Prefix", Speech.F("crafting.shards", 2)), "Reused shard row reads current labels");
        reset(); shard.tmpQuantity.text = "[14]"; ElementAnnouncer.AnnounceElement(shard.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join("Fire resistance", "Prefix", Speech.F("crafting.shards", 14)),
            "The highlighted row itself reads its shard, with the game's bracketed count unwrapped");
        LeAccess.Accessibility.Items.ItemTooltipReader.Opener = shard.gameObject;
        LeAccess.Accessibility.Items.ItemTooltipReader.Text = "Fire resistance. Tier 1: +5%";
        reset();
        Check(ElementAnnouncer.TryReadDetails(shard.gameObject, out details) && details.EndsWith("Adds 12 health. Fire resistance. Tier 1: +5%"),
            "Manual row details add the affix tooltip the game opened for that row");
        reset(); ElementAnnouncer.AnnounceElement(shard.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains("Tier 1"), "The brief row reading omits the affix tooltip");
        LeAccess.Accessibility.Items.ItemTooltipReader.Opener = null;
        reset(); shard.button.GetComponent<Button>().Interactable = false; ElementAnnouncer.AnnounceElement(shard.gameObject);
        Check(ScreenReader.Messages.Single().Text.EndsWith(Speech.DisabledMark()), "A row whose button the game disabled says so");
        shard.button.GetComponent<Button>().Interactable = true;
        reset(); var nested = shard.button.Child().Add(new Button());
        Check(!CraftingControlReader.TryRead(nested.gameObject, false, out _, out _), "Nested unrelated buttons cannot borrow a shard action");
        reset(); var stray = shard.gameObject.Child("main");
        Check(!CraftingControlReader.TryRead(stray, false, out _, out _), "Other parts of the row are not the highlighted row");

        reset(); var panelRoot = new GameObject(); var panel = panelRoot.Add(new CraftingMaterialsPanelUI());
        CraftingMaterialsFilterTab Tab(string caption)
        { var tab = panelRoot.Child("CraftingMaterialsFilterTab").Add(new CraftingMaterialsFilterTab()); Label(tab.gameObject, caption); return tab; }
        panel.allAffixes = Tab("any affix"); panel.prefixesOnly = Tab("prefixes"); panel.suffixesOnly = Tab("suffixes");
        panel.currentlySelected = panel.allAffixes;
        ElementAnnouncer.AnnounceElement(panel.allAffixes.gameObject);
        Check(ScreenReader.Messages.Single().Text == Speech.Join(Speech.F("crafting.filter_selected", "any affix"), Speech.T("crafting.materials_directions")),
            "The selected shard filter says it is selected and where the runes, glyphs and shards are");
        reset(); ElementAnnouncer.AnnounceElement(panel.prefixesOnly.gameObject);
        Check(ScreenReader.Messages.Single().Text.StartsWith(Speech.F("crafting.filter", "prefixes")), "Other filters are not called selected");
        panel.noShardNotice = Label(panelRoot.Child("No Shard Notice"), "You have no affix shards.");
        reset(); ElementAnnouncer.AnnounceElement(panel.allAffixes.gameObject);
        Check(ScreenReader.Messages.Single().Text.Contains("You have no affix shards."), "The game's no-shard notice joins the filter reading");
        panel.noShardNotice.gameObject.Active = false;
        reset(); ElementAnnouncer.AnnounceElement(panel.allAffixes.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains("no affix shards"), "A hidden notice stays silent");
        reset(); var foreignTab = new GameObject().Add(new CraftingMaterialsFilterTab()); Label(foreignTab.gameObject, "elsewhere");
        Check(!CraftingMaterialsPanelReader.TryRead(foreignTab.gameObject, out _), "Tabs outside the materials panel are not its filters");

        var content = panelRoot.Child("Content");
        panel.unusedAffixesHeader = Label(content.Child("Available Affix Header"), "available to be applied").transform;
        panel._availableAffixesGroup = content.Child("Available Affixes Holder").Add(new PooledVerticalLayoutGroup());
        panel._incompatibleAffixesGroup = content.Child("Incompatible Affixes Holder").Add(new PooledVerticalLayoutGroup());
        reset(); ElementAnnouncer.AnnounceElement(panel._availableAffixesGroup.gameObject);
        Check(ScreenReader.Messages.Single().Text == "available to be applied", "A section stop reads its own heading, not its object name");
        reset(); ElementAnnouncer.AnnounceElement(panel._incompatibleAffixesGroup.gameObject);
        Check(ScreenReader.Messages.Count == 0, "A section without a readable heading stays silent instead of speaking its object name");
        foreach (string lifetime in new[] { "collected", "disabled" })
        {
            reset(); row.WasCollected = lifetime == "collected"; row.Enabled = lifetime != "disabled";
            Check(!CraftingControlReader.TryRead(upgrade.gameObject, false, out _, out _), "Crafting rejects " + lifetime + " rows");
            row.WasCollected = false; row.Enabled = true;
        }
        reset(); upgrade._upgradeButton.Enabled = false;
        Check(!CraftingControlReader.TryRead(upgrade.gameObject, false, out _, out _), "Disabled Button components cannot supply specialized content");
        upgrade._upgradeButton.Enabled = true;
        reset(); return checks;
    }
}

public class AffixSlotForge : UnityEngine.Behaviour
{
    public TextMeshProUGUI nameTMP, tierTMP, countTMP;
    public Button addAffixButton;
    public CraftingUpgradeButton upgradeButton;
}
public class CraftingUpgradeButton : UnityEngine.Behaviour
{
    public Button _upgradeButton;
    public AffixSlotForge affixSlotForge;
}
public class ShardAffixListElement : UnityEngine.Behaviour
{
    public GameObject button;
    public TextMeshProUGUI tmpName, tmpQuantity, tmpAffixType, improvementTMP;
}
public class TabUIElement : UnityEngine.UI.Selectable { }
public class CraftingMaterialsFilterTab : TabUIElement { }
public class PooledVerticalLayoutGroup : UnityEngine.UI.Selectable { }
public class CraftingMaterialsPanelUI : UnityEngine.Behaviour
{
    public TabUIElement allAffixes, prefixesOnly, suffixesOnly, currentlySelected;
    public TMP_Text noShardNotice;
    public PooledVerticalLayoutGroup _availableAffixesGroup, _appliedAffixesGroup, _incompatibleAffixesGroup;
    public Transform unusedAffixesHeader, appliedAffixesHeader, incompatibleAffixesHeader;
}
