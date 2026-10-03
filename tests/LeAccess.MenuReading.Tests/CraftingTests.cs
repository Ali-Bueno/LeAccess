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
        Check(ScreenReader.Messages.Single().Text == "Upgrade affix. Increased health. T3. Shards: 12",
            "An icon-only upgrade reads its owned affix, tier and quantity despite a second row button");
        Check(tooltip.SetTextCalls == 0, "Brief crafting labels do not construct a tooltip model");
        Check(ElementAnnouncer.TryReadDetails(upgrade.gameObject, out var upgradeDetails) && upgradeDetails.EndsWith("Improves the current affix."),
            "Manual crafting inspection retains the existing associated tooltip description");
        Check(tooltip.SetTextCalls == 1, "Manual inspection retains the existing native tooltip population path");
        reset(); ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Add affix. Increased health. T3. Shards: 12", "The add button retains its distinct action");
        reset(); upgrade._upgradeButton.Interactable = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text.EndsWith("(Disabled)"), "Disabled crafting actions retain unavailable state");
        reset(); row.gameObject.name = "Disabled ModSlot"; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text.Split("(Disabled)").Length == 2, "Native and ancestor disabled states wrap only once");
        row.gameObject.name = "ModSlot";
        upgrade._upgradeButton.Interactable = true;
        reset(); row.nameTMP.text = "Added fire damage"; row.tierTMP.text = "T4"; row.countTMP.text = "11";
        ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text.Contains("Added fire damage. T4. Shards: 11"), "Reused slots read current content");
        reset(); row.nameTMP.gameObject.Active = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Upgrade affix", "A hidden affix does not leak tier or old quantity");
        row.nameTMP.gameObject.Active = true;
        foreach (string empty in new[] { "", "   ", "<b></b>" })
        {
            reset(); row.nameTMP.text = empty; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
            Check(ScreenReader.Messages.Single().Text == "Upgrade affix", "Empty or markup-only affixes do not leak tier or quantity");
        }
        row.nameTMP.text = "Added fire damage";
        reset(); row.countTMP.gameObject.Active = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains("Shards"), "Hidden quantities stay silent");
        row.countTMP.gameObject.Active = true;
        reset(); row.tierTMP = Label(new GameObject(), "Foreign tier"); ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains("Foreign"), "Foreign serialized labels are rejected");
        reset(); upgrade.affixSlotForge = root.Child().Add(new AffixSlotForge()); ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(!ScreenReader.Messages.Single().Text.StartsWith("Upgrade affix"), "Replaced upgrade ownership is rejected");
        upgrade.affixSlotForge = row;
        reset(); var other = row.gameObject.Child("OtherButton").Add(new Button()); ElementAnnouncer.AnnounceElement(other.gameObject);
        Check(ScreenReader.Messages.All(message => !message.Text.StartsWith("Add affix") && !message.Text.StartsWith("Upgrade affix")),
            "Unrelated row controls are not assigned an action");
        reset(); root.Active = false; ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Count == 0, "Closed forge rejects retained focus"); root.Active = true;
        reset(); ScreenContentAnnouncer.HasActiveContent = true; ScreenContentAnnouncer.Root = new GameObject();
        ElementAnnouncer.AnnounceElement(upgrade.gameObject);
        Check(ScreenReader.Messages.Count == 0, "Foreground popup suppresses background forge");
        reset(); LeAccess.I18n.ModText.SetLanguage("es"); ElementAnnouncer.AnnounceElement(row.addAffixButton.gameObject);
        Check(ScreenReader.Messages.Single().Text.StartsWith("Añadir afijo. Added fire damage") && ScreenReader.Messages.Single().Text.EndsWith("Fragmentos: 11"),
            "Action labels localize without translating native content");
        reset(); LeAccess.I18n.ModText.SetLanguage("en");
        var shard = root.Child().Add(new ShardAffixListElement());
        shard.button = shard.gameObject.Child("Button"); shard.button.Add(new Button());
        shard.tmpName = Label(shard.gameObject, "Increased health"); shard.tmpAffixType = Label(shard.gameObject, "Prefix");
        shard.tmpQuantity = Label(shard.gameObject, "9"); shard.improvementTMP = Label(shard.gameObject, "Adds 12 health.");
        ElementAnnouncer.AnnounceElement(shard.button);
        Check(ScreenReader.Messages.Single().Text == "Increased health. Prefix. Shards: 9", "Pooled shard buttons read exact owned fields");
        Check(ElementAnnouncer.TryReadDetails(shard.button, out var details) && details.EndsWith("Adds 12 health."), "Manual shard details include rendered improvement");
        reset(); shard.tmpName.text = "Fire resistance"; shard.tmpQuantity.text = "2"; ElementAnnouncer.AnnounceElement(shard.button);
        Check(ScreenReader.Messages.Single().Text == "Fire resistance. Prefix. Shards: 2", "Reused shard row reads current labels");
        reset(); var nested = shard.button.Child().Add(new Button());
        Check(!CraftingControlReader.TryRead(nested.gameObject, false, out _, out _), "Nested unrelated buttons cannot borrow a shard action");
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
