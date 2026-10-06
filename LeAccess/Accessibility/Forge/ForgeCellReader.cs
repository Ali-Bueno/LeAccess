using System.Text.RegularExpressions;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Forge;

internal readonly record struct ForgeCell(string Key, GameObject Root, string Brief);

/// <summary>Reads the forge grid cell the game has selected: what it is, what it holds and what Cross does there.</summary>
/// <remarks>See reference/crafting-reading.md#controller-forge-2026-10-05.</remarks>
internal static class ForgeCellReader
{
    private static readonly Regex Tier = new(@"^T(\d+)$", RegexOptions.CultureInvariant);

    /// <summary>The forge when its grid owns navigation, or its item slot does (L2 with an item on the cursor).</summary>
    internal static bool TryGetForge(out CraftingSlotManager forge)
    {
        var navigation = NavigationManager.currentlySelected;
        forge = navigation is null || navigation.WasCollected ? null : navigation.TryCast<CraftingSlotManager>();
        var slot = IsAlive(forge) || navigation is null || navigation.WasCollected ? null : navigation.TryCast<CraftingMainUI>();
        if (IsAlive(slot)) forge = slot.GetComponentInParent<CraftingSlotManager>();
        return IsAlive(forge) && forge.isActiveAndEnabled && UiVisibility.IsVisible(forge.gameObject);
    }

    internal static bool TryRead(CraftingSlotManager forge, out ForgeCell cell)
    {
        cell = default;
        var navigation = NavigationManager.currentlySelected;
        var slot = navigation is null || navigation.WasCollected ? null : navigation.TryCast<CraftingMainUI>();
        if (IsAlive(slot) && slot.transform.IsChildOf(forge.transform))
        {
            cell = new ForgeCell($"{slot.gameObject.GetInstanceID()}", slot.gameObject,
                ScreenContentTracker.Combine(new[] { MainItem(forge), ModText.Get("forge.cursor_hint") }));
            return true;
        }
        var index = forge.currentlySelectedIdx;
        // The grid the game focuses: RectTransforms of the active cells ([column, row]).
        var cells = forge.activeNavigationElements?.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2 || index.x < 0 || index.y < 0 ||
            index.x >= cells.GetLength(0) || index.y >= cells.GetLength(1)) return false;
        var value = cells.GetValue(index.x, index.y);
        var rect = value is null || value.WasCollected ? null : value.TryCast<RectTransform>();
        if (!IsAlive(rect) || !rect.gameObject.activeInHierarchy || !rect.transform.IsChildOf(forge.transform)) return false;
        string brief = Describe(forge, rect.gameObject);
        if (string.IsNullOrWhiteSpace(brief)) return false;
        cell = new ForgeCell($"{rect.gameObject.GetInstanceID()}", rect.gameObject, brief);
        return true;
    }

    private static string Describe(CraftingSlotManager forge, GameObject root)
    {
        var row = root.GetComponent<AffixSlotForge>();
        if (IsAlive(row)) return AffixRow(row);
        var upgrade = root.GetComponent<CraftingUpgradeButton>();
        if (IsAlive(upgrade)) return Upgrade(upgrade);
        if (IsAlive(root.GetComponent<CraftingMainUI>())) return MainItem(forge);
        if (IsAlive(root.GetComponent<CraftingSupportUI>()))
            return ItemName(forge.support) is { } glyph ? ModText.Format("forge.glyph", glyph) : ModText.Get("forge.glyph_empty");
        if (IsAlive(root.GetComponent<CraftingModifierUI>()))
            return ItemName(forge.modifier) is { } material ? ModText.Format("forge.modifier", material) : ModText.Get("forge.modifier_empty");
        if (IsAlive(root.GetComponent<CraftingRuneButton>())) return ModText.Get("forge.runes");
        if (IsAlive(root.GetComponent<CraftingMaterialsToggleButton>())) return ButtonLabel(root) ?? ModText.Get("forge.materials");
        if (IsAlive(forge.forgeButton) && forge.forgeButton.gameObject.GetInstanceID() == root.GetInstanceID()) return ForgeButton(forge);
        return ButtonLabel(root) ?? ModText.Get("forge.help");
    }

    // A row: its slot, the affix it holds with tier and shards, and what Cross or the outer column does.
    private static string AffixRow(AffixSlotForge row)
    {
        string name = Label(row.nameTMP);
        bool add = IsAlive(row.addAffixButton) && row.addAffixButton.gameObject.activeInHierarchy;
        bool upgrade = row.availableUpgrade && IsAlive(row.upgradeButton) && row.upgradeButton.gameObject.activeInHierarchy;
        bool prefix = row.slotID is AffixSlotForge.AffixSlotID.PREFIX_ONE or AffixSlotForge.AffixSlotID.PREFIX_TWO;
        return ScreenContentTracker.Combine(new[] {
            SlotLabel(row.slotID),
            name == null ? ModText.Get(add ? "forge.slot_empty" : "forge.slot_unavailable") : ScreenContentTracker.Combine(new[] { name, TierText(row.tierTMP) }),
            name == null ? null : Shards(row.countTMP),
            add ? ModText.Get("forge.add_hint") : null,
            upgrade ? ModText.Get(prefix ? "forge.upgrade_left" : "forge.upgrade_right") : null });
    }

    private static string Upgrade(CraftingUpgradeButton upgrade)
    {
        var row = upgrade.affixSlotForge;
        string name = IsAlive(row) ? Label(row.nameTMP) : null;
        var button = upgrade._upgradeButton;
        bool usable = IsAlive(button) && button.interactable;
        return ScreenContentTracker.Combine(new[] {
            ModText.Format("forge.upgrade", name ?? (IsAlive(row) ? SlotLabel(row.slotID) : "")),
            IsAlive(row) ? TierText(row.tierTMP) : null, IsAlive(row) ? Shards(row.countTMP) : null,
            ModText.Get(usable ? "forge.upgrade_hint" : "forge.slot_unavailable") });
    }

    private static string MainItem(CraftingSlotManager forge)
    {
        string name = ItemName(forge.main);
        if (name == null) return ModText.Get("forge.item_empty");
        string potential = Label(forge.forgingPotentialText);
        return ScreenContentTracker.Combine(new[] { ModText.Format("forge.item", name),
            potential == null ? null : ModText.Format("forge.potential", potential), ModText.Get("forge.item_hint") });
    }

    // The game writes its own wording on the button; when forging is impossible it explains why.
    private static string ForgeButton(CraftingSlotManager forge)
    {
        string label = Label(forge.forgeButtonText) ?? ModText.Get("forge.forge");
        if (!forge.canForge) return ScreenContentTracker.Combine(new[] { label, Label(forge.cannotForgeText) });
        string min = Label(forge.minForgingPotentialText), max = Label(forge.maxForgingPotentialText);
        return ScreenContentTracker.Combine(new[] { label,
            min == null || max == null ? null : min == max ? ModText.Format("forge.cost_single", min) : ModText.Format("forge.cost", min, max) });
    }

    /// <summary>The result labels the game fills after a craft (outcome, fracture, potential lost, criticals).</summary>
    internal static string ReadResult(CraftingSlotManager forge)
    {
        string result = ScreenContentTracker.Combine(new[] {
            Label(forge.singlelineFractureChanceTMP), Label(forge.minorFractureChanceTMP), Label(forge.criticalChanceOutcomeTMP),
            Label(forge.lostForgingPotentialTMP), Label(forge.typeOfForgeTMP) });
        return string.IsNullOrWhiteSpace(result) ? ModText.Get("forge.result_done") : ModText.Format("forge.result", result);
    }

    /// <summary>The forge's item, for the opening announcement.</summary>
    internal static string ItemSummary(CraftingSlotManager forge) =>
        ItemName(forge.main) is { } name ? ModText.Format("forge.item", name) : ModText.Get("forge.item_empty");

    private static string SlotLabel(AffixSlotForge.AffixSlotID slot) => slot switch
    {
        AffixSlotForge.AffixSlotID.PREFIX_ONE => ModText.Format("forge.prefix", 1),
        AffixSlotForge.AffixSlotID.PREFIX_TWO => ModText.Format("forge.prefix", 2),
        AffixSlotForge.AffixSlotID.SUFFIX_ONE => ModText.Format("forge.suffix", 1),
        _ => ModText.Format("forge.suffix", 2)
    };

    // The game shows tiers as "T3".
    private static string TierText(TMP_Text label)
    {
        string text = Label(label);
        var match = text == null ? null : Tier.Match(text);
        return match is { Success: true } ? ModText.Format("forge.tier", match.Groups[1].Value) : text;
    }

    private static string Shards(TMP_Text label) => Label(label) is { } count ? ModText.Format("forge.shards_available", count) : null;

    private static string ItemName(OneItemContainer container) => NameOf(Content(container));

    /// <summary>The item a forge slot holds, or null.</summary>
    internal static ItemData Content(OneItemContainer container)
    {
        if (container is null || container.WasCollected) return null;
        var content = container.GetContent();
        if (content is null || content.WasCollected || content.Count == 0) return null;
        var data = content[0]?.data;
        return data is null || data.WasCollected ? null : data;
    }

    internal static string NameOf(ItemData data)
    {
        var unpacked = data is null || data.WasCollected ? null : data.getAsUnpacked();
        string name = unpacked is null || unpacked.WasCollected ? null : TextUtils.CleanText(unpacked.FullName);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static string ButtonLabel(GameObject root) => Label(root.GetComponentInChildren<TMP_Text>());

    // Position and clipping are ignored: the forge's labels are read wherever they sit.
    private static string Label(TMP_Text label)
    {
        if (!IsAlive(label) || !label.isActiveAndEnabled) return null;
        string text = TextUtils.CleanText(label.text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
