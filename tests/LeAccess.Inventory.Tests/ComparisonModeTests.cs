using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Vitals;
using LeAccess.I18n;
using LE.UI.Tooltips;
using TMPro;
using UnityEngine;

/// <summary>The older item tooltip's held modes, the panel notice that leads an item brief, and the forge slot exclusion.</summary>
internal static class ComparisonModeTests
{
    private static string T(string key) => ModText.Get(key);
    private static string F(string key, params object[] args) => ModText.Format(key, args);
    private static string Join(params string[] parts) => ScreenContentTracker.Combine(parts);

    private static TMP_Text Text(string text, Transform parent)
    {
        var label = new TMP_Text { text = text };
        label.transform.parent = parent;
        return label;
    }

    private static UITooltipItemAffix Row(string text)
    {
        var row = new UITooltipItemAffix();
        row._text = Text(text, row.transform);
        return row;
    }

    private static Il2CppSystem.Collections.Generic.List<UITooltipItemAffix> Rows(params UITooltipItemAffix[] rows)
    {
        var list = new Il2CppSystem.Collections.Generic.List<UITooltipItemAffix>();
        foreach (var row in rows) list.Add(row);
        return list;
    }

    private static GameObject Header(string text)
    {
        var header = new GameObject();
        Text(text, header.transform);
        return header;
    }

    internal static void Run(Action<bool, string> check)
    {
        var item = new ItemDataUnpacked { Id = "legacy", FullName = "Hacha" };
        var tooltip = new UITooltipItem();
        var parameters = new TooltipItemManager.ItemTooltipParameters
        { _Item_k__BackingField = item, _TooltipMode_k__BackingField = TooltipMode.CompareDefault };
        var manager = new TooltipItemManager { activeParameters = parameters, itemTooltip = tooltip };
        var system = new TooltipSystem { _tooltipItemManager = manager };
        var savedUi = UIBase.instance;
        UIBase.instance = new UIBase { TooltipSystem = system };
        bool Read(out string key, out string text) => ItemComparisonReader.TryRead(item, out key, out text);
        string Ranges(params string[] lines) => lines.Length == 0 ? T("item.ranges_none") : F("item.ranges", string.Join("; ", lines));
        string Explanations(params string[] lines) => lines.Length == 0 ? T("item.explanations_none") : F("item.explanations", string.Join("; ", lines));
        void Mode(TooltipMode mode) => parameters._TooltipMode_k__BackingField = mode;

        // Comparison: the equipped column is the game's compareContent.
        tooltip.compareContent.Visible = false;
        check(Read(out var key, out var text) && text == T("item.compare_nothing"), "Legacy compare with nothing equipped says so: " + text);
        check(ItemComparisonReader.LastState == "v2 hidden; legacy mode=CompareDefault autoCompare=False nothing equipped",
            "The state names both tooltips, the mode and that nothing is equipped: " + ItemComparisonReader.LastState);
        tooltip.compareContent.Visible = true;
        tooltip.compareNameText = Text("Hacha vieja", tooltip.transform);
        tooltip.compareTypeText = Text("Hacha de una mano", tooltip.transform);
        tooltip.comparisonText = Text("Mejor ataque", tooltip.transform);
        string equipped = Join("Hacha vieja", "Hacha de una mano");
        check(Read(out key, out text) && text == Join(F("item.comparison_with", equipped), "Mejor ataque") &&
            ItemComparisonReader.LastState.EndsWith("compared"), "Legacy compare reads the equipped item, then the game's summary: " + text);
        check(key.StartsWith("legacy:CompareDefault:"), "The legacy key names the mode");
        tooltip.comparisonGainedHeader = Header("Ganas"); tooltip.comparisonGainedText = Text("+5 daño", tooltip.transform);
        tooltip.comparisonLostHeader = Header("Pierdes"); tooltip.comparisonLostText = Text("-3 vida", tooltip.transform);
        check(Read(out _, out text) && text == Join(F("item.comparison_with", equipped), "Mejor ataque",
            Join("Ganas", "+5 daño"), Join("Pierdes", "-3 vida")), "Gained and lost lines follow their headers: " + text);
        tooltip.comparisonLostHeader.Visible = false;
        check(Read(out _, out text) && text.EndsWith("-3 vida") && !text.Contains("Pierdes"), "A hidden header is dropped, its lines stay");
        tooltip.comparisonLostHeader = null; tooltip.comparisonText = null;
        check(Read(out _, out text) && text == Join(F("item.comparison_with", equipped), Join("Ganas", "+5 daño"), "-3 vida"),
            "A header-less body still reads: " + text);

        // Without the game's summary the equipped item's own rows stand in, in tooltip order.
        tooltip.comparisonGainedHeader = null; tooltip.comparisonGainedText = tooltip.comparisonLostText = null;
        tooltip.compareImplicitText = Text("Implícito +1", tooltip.transform);
        tooltip.compareUniqueAffixesText = Rows(Row("Único uno"));
        var hidden = Row("Oculto"); hidden.gameObject.Visible = false;
        tooltip.comparePrefixes = Rows(Row("+10 vida"), hidden, Row("+5 fuerza"));
        tooltip.compareSuffixes = Rows(Row("+2 suerte"));
        tooltip.compareSealedAffix = Row("Sellado");
        tooltip.compareSealedCorruptedAffix = Row("Corrupto");
        check(Read(out _, out text) && text == Join(F("item.comparison_with", equipped),
            "Implícito +1; Único uno; +10 vida; +5 fuerza; +2 suerte; Sellado; Corrupto"),
            "With no summary the equipped item's rows are read, hidden rows skipped: " + text);
        tooltip.compareImplicitText = null; tooltip.compareUniqueAffixesText = tooltip.comparePrefixes = tooltip.compareSuffixes = null;
        tooltip.compareSealedAffix = tooltip.compareSealedCorruptedAffix = null;
        check(Read(out _, out text) && text == F("item.comparison_with", equipped), "An equipped item with no rows reads just its name: " + text);
        tooltip.compareNameText = tooltip.compareTypeText = null;
        check(Read(out _, out text) && text == T("item.comparison"), "No equipped name falls back to the plain comparison label: " + text);

        // Explanation and range modes read the main rows.
        Mode(TooltipMode.Explanation);
        check(Read(out key, out text) && text == Explanations() && key.StartsWith("legacy:Explanation:"), "Explanation mode with no rows says none: " + text);
        check(ItemComparisonReader.LastState == "v2 hidden; legacy mode=Explanation autoCompare=False", "Non-compare modes add no equipped suffix");
        tooltip.implicitText = Text("Implícito +1 (explicado)", tooltip.transform);
        tooltip.uniqueAffixesText = Rows(Row("Único"));
        tooltip.prefixesText = Rows(Row("Prefijo A"), Row("Prefijo B"));
        tooltip.suffixesText = Rows(Row("Sufijo"));
        tooltip.sealedAffix = Row("Sellado A"); tooltip.sealedPrimordialAffix = Row("Primordial"); tooltip.sealedCorruptedAffix = Row("Corrupto A");
        tooltip.forgingPotentialAltText = Text("Potencial 3", tooltip.transform);
        tooltip.legendaryPotentialAltText = Text("Legendario 2", tooltip.transform);
        tooltip.weaversWillAltText = Text("Voluntad 1", tooltip.transform);
        tooltip.weaversTouchAltText = Text("Toque 4", tooltip.transform);
        string[] all = { "Implícito +1 (explicado)", "Único", "Prefijo A", "Prefijo B", "Sufijo", "Sellado A", "Primordial", "Corrupto A",
            "Potencial 3", "Legendario 2", "Voluntad 1", "Toque 4" };
        check(Read(out _, out text) && text == Explanations(all), "Explanation mode reads every main row in order: " + text);
        Mode(TooltipMode.DefaultModRanges);
        check(Read(out key, out text) && text == Ranges(all) && key.StartsWith("legacy:DefaultModRanges:"), "Range mode reads the same rows as ranges: " + text);
        tooltip.compareContent.Visible = false;
        Mode(TooltipMode.CompareModRanges);
        check(Read(out _, out text) && text == Join(T("item.compare_nothing"), Ranges(all)), "Comparison with ranges and nothing equipped: " + text);
        tooltip.compareContent.Visible = true;
        tooltip.compareNameText = Text("Hacha vieja", tooltip.transform);
        tooltip.comparisonText = Text("Mejor ataque", tooltip.transform);
        check(Read(out _, out text) && text == Join(F("item.comparison_with", "Hacha vieja"), "Mejor ataque", Ranges(all)),
            "Comparison with ranges reads the comparison, then the ranges: " + text);

        // What is not read: loading, other items, plain info, hidden or inactive tooltips.
        Mode(TooltipMode.Explanation);
        Read(out _, out _);
        string seen = ItemComparisonReader.LastState;
        manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.SetContent;
        Mode(TooltipMode.DefaultModRanges);
        check(!Read(out key, out text) && key is null && text is null, "A tooltip still loading reads nothing");
        check(ItemComparisonReader.LastState == seen, "A tooltip still loading leaves the logged state alone: " + ItemComparisonReader.LastState);
        manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.Completed;
        Mode(TooltipMode.DefaultInfo);
        check(!Read(out _, out _) && ItemComparisonReader.LastState == "v2 hidden; legacy mode=DefaultInfo autoCompare=False",
            "The plain info mode is left to the ordinary reader: " + ItemComparisonReader.LastState);
        Mode(TooltipMode.CompareDefault);
        manager.autoCompareEnabled = true;
        Read(out _, out _);
        check(ItemComparisonReader.LastState.Contains("autoCompare=True"), "The state records the game's automatic compare setting");
        manager.autoCompareEnabled = false;
        check(!ItemComparisonReader.TryRead(new ItemDataUnpacked { Id = "legacy" }, out _, out _) &&
            ItemComparisonReader.LastState == "v2 hidden; legacy other item", "Another item with the same id is never read: " + ItemComparisonReader.LastState);
        tooltip.tooltipActive = false;
        check(!Read(out _, out _) && ItemComparisonReader.LastState == "v2 hidden; legacy hidden", "A hidden tooltip is never read");
        tooltip.tooltipActive = true;
        manager.active = false;
        check(!Read(out _, out _) && ItemComparisonReader.LastState == "v2 hidden; legacy inactive", "An inactive tooltip manager reads nothing");
        manager.active = true;

        // The newer tooltip wins when it shows this item.
        var modern = new ItemTooltip();
        var modernInfo = new ItemTooltipInfo { _Item_k__BackingField = item, _TooltipMode_k__BackingField = TooltipMode.Explanation };
        modern._info = modernInfo;
        var modifier = new ItemTooltipModifier { Info = new() };
        modifier.transform.parent = modern.transform;
        modifier._modifierText = Text("+8 fuerza", modifier.transform);
        modifier._explanationText = Text("Aumenta la fuerza", modifier.transform);
        system._itemTooltip = modern; system._activeItemTooltipInfo = modernInfo;
        Mode(TooltipMode.Explanation);
        check(Read(out key, out text) && text == F("item.explanations", F("item.explained_line", "+8 fuerza", "Aumenta la fuerza")) &&
            !key.StartsWith("legacy") && ItemComparisonReader.LastState == "v2 mode=Explanation", "The newer tooltip wins over the older one: " + text);
        modernInfo._TooltipMode_k__BackingField = TooltipMode.CompareDefault;
        check(Read(out _, out text) && text == T("item.compare_nothing") && ItemComparisonReader.LastState == "v2 mode=CompareDefault",
            "The newer compare mode with its comparison tooltip missing says nothing is equipped: " + text);
        system._activeItemTooltipInfo = new ItemTooltipInfo();
        Mode(TooltipMode.Explanation);
        check(Read(out _, out text) && text == Explanations(all) && ItemComparisonReader.LastState.StartsWith("v2 stale; legacy "),
            "A stale newer tooltip falls back to the older one: " + ItemComparisonReader.LastState);
        system._itemTooltip = null; system._activeItemTooltipInfo = null;

        UIBase.instance = savedUi;
        RunLeadingNotice(check);
        RunForgeSlot(check);
    }

    // A panel beside the inventory leads the next brief, before the gold, once; it ends when the selection moves.
    private static void RunLeadingNotice(Action<bool, string> check)
    {
        var ui = new UIBase();
        UIBase.instance = ui; GoldReport.Amount = 800;
        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Update();   // a closed inventory ends any earlier gold notice
        ui.InventoryOpen = true;
        var first = new ItemDataUnpacked { Id = "lead-first", FullName = "Casco" };
        var container = new ItemContainerUI { currentlySelectedItem = new() { data = first } };
        NavigationManager.currentlySelected = container;
        InventoryAccessibility.Reset(); InventoryAccessibility.Lead(null); ScreenReader.Messages.Clear();
        void Pump() { InventoryAccessibility.Update(); InventoryAccessibility.Update(); }
        InventoryAccessibility.Lead("Forja abierta");
        Pump();
        check(ScreenReader.Messages.Single() == Join("Forja abierta", F("gold.amount", 800), "Casco"),
            "A panel notice leads the next brief, before the gold: " + string.Join(" | ", ScreenReader.Messages));
        Pump();
        check(ScreenReader.Messages.Count == 1, "The notice is spoken once");
        container.currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "lead-second", FullName = "Guantes" } };
        Pump();
        check(ScreenReader.Messages.Last() == "Guantes", "The notice is dropped once the selection moves: " + ScreenReader.Messages.Last());
        InventoryAccessibility.Lead("Primero");
        InventoryAccessibility.Lead("Segundo");
        container.currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "lead-third", FullName = "Botas" } };
        Pump();
        check(ScreenReader.Messages.Last() == Join("Segundo", "Botas"), "A newer notice replaces an older unspoken one");
        InventoryAccessibility.Lead("Nunca");
        InventoryAccessibility.Lead(null);
        container.currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "lead-fourth", FullName = "Anillo" } };
        Pump();
        check(ScreenReader.Messages.Last() == "Anillo", "A null notice clears it before it is spoken");
        ui.InventoryOpen = false; UIBase.instance = null; GoldReport.Amount = null; NavigationManager.currentlySelected = null;
        InventoryAccessibility.Reset(); InventoryAccessibility.Lead(null); ScreenReader.Messages.Clear();
    }

    // The forge's item slot is an ItemContainerUI, but the forge reader speaks it.
    private static void RunForgeSlot(Action<bool, string> check)
    {
        var slot = new CraftingMainUI { currentlySelectedItem = new() { data = new ItemDataUnpacked { Id = "forge-item", FullName = "Espada" } } };
        NavigationManager.currentlySelected = slot;
        check(InventorySelectionReader.GetFocusedContainer() is null, "The inventory reader leaves the forge's item slot alone");
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Count == 0, "Nothing is read for the forge's item slot");
        var ordinary = new ItemContainerUI();
        NavigationManager.currentlySelected = ordinary;
        check(InventorySelectionReader.GetFocusedContainer() is { } found && found.Pointer == ordinary.Pointer, "An ordinary container is still read");
        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Reset();
    }
}
