using LeAccess.Accessibility;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class SelectionTests
{
    private sealed class Fixture
    {
        internal SkillsPanelManager Panel;
        internal Component[,] Cells;
        internal Ability Ability;
        internal Selectable Control;
        internal AbilityPanelIcon Icon;
        internal UITooltipAbility Tooltip;
        internal AbilityTooltip Owner;
    }

    private static TMP_Text Text(GameObject root, string text) => root.Child().Add(new TMP_Text { text = text });
    private static Fixture Reset()
    {
        SkillAccessibility.Reset();
        BindingTests.Reset();
        ScreenReader.Messages.Clear();
        ScreenReader.Available = true;
        ScreenReader.SpokeThisFrame = false;
        ScreenContentAnnouncer.HasActiveContent = DialogueAnnouncer.HasActiveContent = false;
        ControllerMenuAccessibility.HasActiveContent = LE.UI.LoadingScreen.IsVisible = false;
        Application.isFocused = true;
        Time.unscaledTimeAsDouble = 0;
        Time.frameCount = 0;
        Localization.Abilities = new AbilityLocalization();
        Localization.Skills = new SkillLocalization();
        NodeTooltipManager.instance = null;
        UIBase.instance = null;
        LeAccess.Plugin.Log.Warnings.Clear();
        var panel = new GameObject().Add(new SkillsPanelManager());
        var control = panel.gameObject.Child().Add(new Selectable());
        var ability = new Ability { abilityName = "Internal Fireball" };
        Localization.Abilities.Names[ability] = "Bola de fuego";
        Localization.Abilities.Descriptions[ability] = "Lanza una bola ardiente.";
        var icon = control.gameObject.Add(new AbilityPanelIcon { ability = ability });
        var cells = new Component[2, 3];
        cells[1, 2] = control;
        panel.selectables = new Il2CppSystem.Array(cells);
        panel.currentlySelectedIdx = new Vector2Int { x = 1, y = 2 };
        NavigationManager.currentlySelected = panel;
        Text(panel.gameObject, "Más información");
        return new Fixture { Panel = panel, Cells = cells, Ability = ability, Control = control, Icon = icon };
    }
    private static void Tooltip(Fixture fixture)
    {
        var tooltip = new GameObject().Add(new UITooltipAbility { currentAbility = fixture.Ability });
        tooltip.content = tooltip.gameObject.Child();
        tooltip.nameText = Text(tooltip.content, "Bola de fuego");
        tooltip.descriptionText = Text(tooltip.content, "Inflige 24 de daño de fuego.");
        tooltip.skillDefinition = Text(tooltip.content, "Hechizo");
        tooltip.moreInfoObj = tooltip.content.Child();
        tooltip.moreInfoDetailsText = Text(tooltip.moreInfoObj, "Escala con inteligencia");
        fixture.Owner = fixture.Control.gameObject.Add(new AbilityTooltip
        { currentAbility = fixture.Ability, toolTip = tooltip });
        fixture.Icon.tooltipAbility = fixture.Owner;
        fixture.Tooltip = tooltip;
    }
    private static void Tick(double elapsed = 0.05, bool otherSpeech = false)
    {
        Time.frameCount++;
        Time.unscaledTimeAsDouble += elapsed;
        ScreenReader.SpokeThisFrame = otherSpeech;
        SkillAccessibility.Update();
    }

    internal static void Run(Action<bool, string> check)
    {
        var fixture = Reset();
        check(SkillSelectionReader.TryRead(out var selection) && selection.Name == "Bola de fuego" &&
            selection.Root == fixture.Control.gameObject && !selection.Details.Contains("Más información"),
            "Actual non-square panel cell [x,y] supplies the skill independently of generic More information prompts");
        foreach (string invalid in new[] { "bounds", "rank", "collected array", "empty cell" })
        {
            fixture = Reset(); Tooltip(fixture);
            if (invalid == "bounds") fixture.Panel.currentlySelectedIdx = new Vector2Int { x = 2, y = 1 };
            if (invalid == "rank") fixture.Panel.selectables = new Il2CppSystem.Array(new Component[3]);
            if (invalid == "collected array") fixture.Panel.selectables.WasCollected = true;
            if (invalid == "empty cell") fixture.Cells[1, 2] = null;
            check(!SkillSelectionReader.TryRead(out _), $"Invalid {invalid} never falls back to the previously displayed skill tooltip");
        }
        fixture = Reset(); Tooltip(fixture);
        check(SkillSelectionReader.TryRead(out selection) && selection.Details ==
            "Hechizo. Inflige 24 de daño de fuego. Escala con inteligencia",
            "Only owned, completed visible skill body and expanded details supplement the localized name");
        fixture.Tooltip.currentAbility = new Ability { abilityName = "Wrong" };
        check(SkillSelectionReader.TryRead(out selection) && selection.Details == "Lanza una bola ardiente.",
            "Reused tooltip displaying another ability contributes no stale description or requirements");
        fixture.Tooltip.currentAbility = fixture.Ability;
        fixture.Owner.currentAbility = new Ability();
        check(SkillSelectionReader.TryRead(out selection) && selection.Details == "Lanza una bola ardiente.",
            "A mismatched tooltip source is rejected even if the displayed ability happens to match");
        foreach (string hidden in new[] { "content", "initialization", "opacity" })
        {
            fixture = Reset(); Tooltip(fixture);
            if (hidden == "content") fixture.Tooltip.content.Active = false;
            if (hidden == "initialization") fixture.Tooltip.activeInitializationStage = UITooltipAbility.TooltipInitializationStage.SetContent;
            if (hidden == "opacity") fixture.Tooltip.gameObject.Add(new CanvasGroup { alpha = 0 });
            check(SkillSelectionReader.TryRead(out selection) && selection.Details == "Lanza una bola ardiente.",
                $"Hidden or unsettled {hidden} cannot expose stale tooltip body");
        }
        fixture = Reset(); Tooltip(fixture); fixture.Icon.ability = null;
        check(!SkillSelectionReader.TryRead(out _), "Empty selected skill slot cannot inherit its retained tooltip ability");
        fixture = Reset(); fixture.Ability.abilityName = null; Localization.Abilities.Names.Clear();
        check(!SkillSelectionReader.TryRead(out _), "Missing skill name never becomes a generic More information-only selection");
        fixture = Reset();
        var grid = new GameObject().Add(new PrioritizedNavigable());
        var gridControl = grid.gameObject.Child().Add(new Selectable());
        gridControl.gameObject.Add(new AbilitySelectionIcon { ability = fixture.Ability });
        grid.ActiveSelectable = gridControl; NavigationManager.currentlySelected = grid;
        check(SkillSelectionReader.TryRead(out selection) && selection.Root == gridControl.gameObject && selection.Ability == fixture.Ability,
            "Assignment grid follows its actual ActiveSelectable independently of the skills panel");
        fixture = Reset(); Tooltip(fixture);
        fixture.Panel.IsSkillTreeOpen = true; fixture.Panel.inTopArea = true;
        var tree = new GameObject().Add(new SkillTree { treeID = 3, ability = fixture.Ability });
        var node = tree.gameObject.Child().Add(new SkillTreeNode
        { tree = tree, id = 4, nodeName = "Nodo retenido", description = "Descripción del nodo" });
        fixture.Panel.CurrentlyOpenTree = tree; fixture.Panel.currentlySelectedNode = node;
        check(SkillSelectionReader.TryRead(out selection) && selection.Name == "Bola de fuego",
            "Top specialization area keeps its selected skill while an old tree node remains stored");
        fixture.Panel.inTopArea = false;
        check(SkillSelectionReader.TryRead(out selection) && selection.Name == "Nodo retenido" && selection.Root == node.gameObject,
            "Entering the actual tree reads its selected node instead of the top skill control");
        node.tree = new GameObject().Add(new SkillTree());
        check(!SkillSelectionReader.TryRead(out _), "A node from a previously opened tree is rejected");

        fixture = Reset(); Tooltip(fixture);
        BindingTests.Icon(fixture.Ability, "cast"); BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "A");
        Tick();
        check(ScreenReader.Messages.Count == 0 && SkillAccessibility.HasPendingContent,
            "Selected skill body settles before reading and owns pending priority");
        Tick();
        check(ScreenReader.Messages.Single().Text.StartsWith("Bola de fuego. Equipped: A.") && ScreenReader.Messages[0].Interrupt,
            "Focused skill announces its name then actual assigned button then details");
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 1 && !SkillAccessibility.HasPendingContent,
            "Unchanged selected skill does not repeat with focus callbacks or frame updates");
        check(SkillAccessibility.TryHandleFocus(fixture.Control.gameObject.Child()) &&
            !SkillAccessibility.TryHandleFocus(new GameObject()),
            "Skill focus ownership covers only the selected control and its descendants");
        BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "RB"); Tick(0.21); Tick();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains(". Equipped: RB."),
            "Remapping while focus stays on one skill is refreshed after the binding cache interval");
        fixture.Tooltip.descriptionText.text = "Inflige 30 de daño de fuego."; Tick(); Tick();
        check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text.Contains("30"),
            "Actual changed skill stats are read without moving selection");
        fixture.Tooltip.moreInfoObj.Active = false; Tick(); Tick();
        check(ScreenReader.Messages.Count == 3, "Closing expanded information does not replay the reduced basic tooltip");
        fixture.Tooltip.descriptionText.text = "Inflige 36 de daño de fuego."; Tick(); Tick();
        check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Text.Contains("36"),
            "Real description changes remain audible while the tooltip stays in basic mode");
        int spoken = ScreenReader.Messages.Count;
        fixture.Tooltip.content.Active = false; Tick(); Tick();
        check(ScreenReader.Messages.Count == spoken,
            "Transient tooltip disappearance must not interrupt the same selected skill with reduced fallback text");
        fixture.Tooltip.content.Active = true; Tick(); Tick();
        check(ScreenReader.Messages.Count == spoken, "Restoring the same tooltip does not replay already-spoken detail");
        fixture.Tooltip.content.Active = false;
        BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "LB"); Tick(0.21); Tick();
        check(ScreenReader.Messages.Count == spoken + 1 && ScreenReader.Messages.Last().Text.Contains(". Equipped: LB.") &&
            !ScreenReader.Messages.Last().Text.Contains("36"),
            "Changed binding while tooltip is hidden cannot inherit retained old visible stats");

        fixture = Reset(); Tick();
        var second = fixture.Panel.gameObject.Child().Add(new Selectable());
        second.gameObject.Add(new AbilitySelectionIcon { ability = new Ability { abilityName = "Escudo de hielo" } });
        fixture.Cells[1, 2] = second; Tick();
        check(ScreenReader.Messages.Count == 0, "Rapid navigation discards the unsettled previous skill");
        Tick();
        check(ScreenReader.Messages.Single().Text == "Escudo de hielo", "Only the latest stable selected skill is announced");
        foreach (string gate in new[] { "popup", "dialogue", "radial menu", "loading", "focus" })
        {
            fixture = Reset(); Tick();
            if (gate == "popup") ScreenContentAnnouncer.HasActiveContent = true;
            if (gate == "dialogue") DialogueAnnouncer.HasActiveContent = true;
            if (gate == "radial menu") ControllerMenuAccessibility.HasActiveContent = true;
            if (gate == "loading") LE.UI.LoadingScreen.IsVisible = true;
            if (gate == "focus") Application.isFocused = false;
            Tick(); Tick();
            check(ScreenReader.Messages.Count == 0 && !SkillAccessibility.HasActiveContent && !SkillAccessibility.HasPendingContent,
                $"{gate} discards background skill speech and focus ownership");
        }
        fixture = Reset(); PromptsInfoProvider.Instance.Throw = true;
        BindingTests.Icon(fixture.Ability, "cast"); BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "A");
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text.StartsWith("Bola de fuego") &&
            !ScreenReader.Messages[0].Text.Contains(". A."),
            "Unavailable binding never suppresses the selected skill's real name or invents an unassigned claim");
        NavigationManager.currentlySelected = null; Tick();
        check(!SkillAccessibility.HasActiveContent && !SkillAccessibility.HasPendingContent,
            "Closing the panel clears selected skill ownership and pending content");
        NavigationManager.currentlySelected = fixture.Panel; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2, "Reopening the same skill announces it again after close reset");
        fixture = Reset();
        BindingTests.Icon(fixture.Ability, "cast"); BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "A");
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Interrupt,
            "The initial owned name, equipped button and description remain immediate");
        for (int i = 0; i < 5; i++) Tick();
        Tooltip(fixture);
        fixture.Tooltip.moreInfoObj.Active = false;
        fixture.Tooltip.descriptionText.text = "Lanza una bola ardiente.";
        fixture.Tooltip.statFields = new();
        var delayedRoot = fixture.Tooltip.content.Child();
        var delayedDps = Text(delayedRoot, "Daño por segundo: 80");
        fixture.Tooltip.statFields.Add(new UITooltipAbility.BigStatField { obj = delayedRoot, textField = delayedDps });
        fixture.Tooltip.activeInitializationStage = UITooltipAbility.TooltipInitializationStage.SetContent;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 1, "Native incomplete tooltip stage cannot emit partially rendered additions");
        fixture.Tooltip.activeInitializationStage = UITooltipAbility.TooltipInitializationStage.Completed;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 2 && !ScreenReader.Messages.Last().Interrupt &&
            ScreenReader.Messages.Last().Text.Contains("Hechizo") && ScreenReader.Messages.Last().Text.Contains("80"),
            "Late completed tooltip appends all new definition and stats without interrupting initial speech");
        check(!ScreenReader.Messages.Last().Text.Contains("Bola de fuego") && !ScreenReader.Messages.Last().Text.Contains("Equipped") &&
            !ScreenReader.Messages.Last().Text.Contains("Lanza"), "Late additions do not replay name, binding or existing description");
        delayedDps.text = "Daño por segundo: 81"; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2, "Late-tooltip DPS retains the existing fluctuation freeze");
        fixture.Tooltip.content.Active = false; Tick(); Tick();
        fixture.Tooltip.content.Active = true; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2, "Hiding and restoring enriched tooltip does not repeat either fragment set");
        fixture.Tooltip.moreInfoObj.Active = true; Tick(); Tick();
        check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Interrupt &&
            ScreenReader.Messages.Last().Text.Contains("inteligencia"), "Deliberate expanded details remain a full interrupting requested read");
        NavigationManager.currentlySelected = null; Tick();
        NavigationManager.currentlySelected = fixture.Panel; Tick(); Tick();
        check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Interrupt &&
            ScreenReader.Messages.Last().Text.Contains("Bola de fuego"), "Reselection resets enrichment identity and reads the full current skill");
        check(SkillAnnouncementDelta.NewFragments("Skill. Description.", "Skill. Description. DPS: 3.5. Cost: 8") == "DPS: 3.5. Cost: 8",
            "Delta preserves decimal values while eliminating already delivered sentence fragments");
        check(SkillAnnouncementDelta.NewFragments("Skill. Description!", "Skill. Definition. Description! Cost: 8") == "Definition. Cost: 8",
            "Non-period description punctuation does not cause duplicate base speech");

        fixture = Reset(); Tooltip(fixture);
        fixture.Tooltip.statFields = new();
        var liveRoot = fixture.Tooltip.content.Child();
        var liveValue = Text(liveRoot, "Daño por segundo: 80");
        fixture.Tooltip.statFields.Add(new UITooltipAbility.BigStatField { obj = liveRoot, textField = liveValue });
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text.Contains("Daño por segundo: 80"),
            "Settled ability speech includes current localized visible combat stats");
        liveValue.text = "Daño por segundo: 81"; Tick(); Tick();
        liveValue.text = "Daño por segundo: 82"; Tick(); Tick();
        check(ScreenReader.Messages.Count == 1, "Continuously fluctuating DPS cannot replay an unchanged focused ability");
        fixture.Tooltip.descriptionText.text = "Nueva mejora de fuego"; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("82"),
            "Real stable detail change refreshes the visible combat-stat snapshot");
        NavigationManager.currentlySelected = null; Tick();
        liveValue.text = "Daño por segundo: 100";
        NavigationManager.currentlySelected = fixture.Panel; Tick(); Tick();
        check(ScreenReader.Messages.Last().Text.Contains("100"), "Reopening selection refreshes combat stats after snapshot reset");
        liveRoot.Active = false;
        fixture.Tooltip.descriptionText.text = "Detalle nuevo sin estadísticas visibles"; Tick(); Tick();
        check(ScreenReader.Messages.Last().Text.Contains("Detalle nuevo") && !ScreenReader.Messages.Last().Text.Contains("100"),
            "Changed stable detail with hidden combat fields cannot inherit an old stat snapshot");
        fixture = Reset();
        fixture.Panel.IsSkillTreeOpen = true;
        tree = new GameObject().Add(new SkillTree { treeID = 3, ability = fixture.Ability });
        node = tree.gameObject.Child().Add(new SkillTreeNode
        { tree = tree, id = 4, nodeName = "Nombre local", description = "Descripción local" });
        fixture.Panel.CurrentlyOpenTree = tree;
        fixture.Panel.currentlySelectedNode = node;
        var modernSystem = new GameObject().Add(new LE.UI.Tooltips.TooltipSystem());
        UIBase.instance = new GameObject().Add(new UIBase { TooltipSystem = modernSystem });
        var modern = new GameObject().Add(new LE.UI.Tooltips.NodeTooltip());
        modernSystem._nodeTooltip = modern;
        modernSystem.ActiveNodeTooltipNode = node;
        modern._info = modernSystem._activeNodeTooltipInfo = new LE.UI.Tooltips.NodeTooltipInfo { Node = node };
        modern._name = Text(modern.gameObject, "Nombre moderno");
        modern._description = Text(modern.gameObject, "Detalle moderno");
        var legacyTooltip = new GameObject().Add(new NodeTooltipManager { previousNode = node });
        legacyTooltip.content = legacyTooltip.gameObject.Child();
        legacyTooltip.title = Text(legacyTooltip.content, "Título legado retenido");
        NodeTooltipManager.instance = legacyTooltip;
        check(SkillSelectionReader.TryRead(out selection) && selection.Name == "Nombre moderno" &&
            selection.Details.Contains("Detalle moderno") && selection.Details.Contains("0 of 5"),
            "Modern owned node supersedes a retained legacy tooltip and includes allocated points");
        modernSystem._nodeTooltipRequiresLayout = true;
        check(SkillSelectionReader.TryRead(out selection) && selection.Name == "Nombre local" &&
            selection.Details == "Descripción local", "Unsettled modern node falls back to current node data, never retained legacy text");
        SkillAccessibility.Reset();
    }
}
