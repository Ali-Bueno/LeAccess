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
        ScreenReader.OutputAccepted = true;
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
        LeAccess.Plugin.Log.Information.Clear();
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
            "Selected name settles before reading");
        Tick();
        check(ScreenReader.Messages.Single() == ("Bola de fuego. Equipped: A. Learned", true),
            "Navigation reads only name and assignment, without description or tooltip");
        check(SkillAccessibility.TryHandleFocus(fixture.Control.gameObject.Child()) &&
            !SkillAccessibility.TryHandleFocus(new GameObject()), "Focus ownership stays with the selected control");
        fixture.Tooltip.descriptionText.text = "Inflige 36 de daño de fuego.";
        fixture.Tooltip.moreInfoObj.Active = false;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 1, "Tooltip changes cannot restart automatic navigation speech");
        BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "RB"); Tick(0.21); Tick();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Bola de fuego. Equipped: RB. Learned",
            "Live remapping updates the brief assigned button");
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("36") && ScreenReader.Messages.Last().Text.Contains("Equipped: RB"),
            "Right stick requests current full details and assignment");
        int spoken = ScreenReader.Messages.Count;
        Tick(); Tick();
        fixture.Tooltip.moreInfoObj.Active = true; Tick(); Tick();
        check(ScreenReader.Messages.Count == spoken, "Automatic focus and native expansion cannot repeat manually spoken details");
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("inteligencia"), "Manual request includes currently expanded details");
        fixture.Tooltip.content.Active = false;
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("Lanza") && !ScreenReader.Messages.Last().Text.Contains("36"),
            "Manual request rejects retained hidden tooltip stats");

        fixture = Reset(); Tick();
        var second = fixture.Panel.gameObject.Child().Add(new Selectable());
        second.gameObject.Add(new AbilitySelectionIcon { ability = new Ability { abilityName = "Escudo de hielo" } });
        fixture.Cells[1, 2] = second; Tick();
        check(ScreenReader.Messages.Count == 0, "Rapid navigation discards the unsettled previous skill");
        Tick();
        check(ScreenReader.Messages.Single().Text == "Escudo de hielo. Not equipped", "Only latest stable skill and assignment are announced");
        foreach (string gate in new[] { "popup", "dialogue", "radial menu", "loading", "focus" })
        {
            fixture = Reset(); Tick();
            if (gate == "popup") ScreenContentAnnouncer.HasActiveContent = true;
            if (gate == "dialogue") DialogueAnnouncer.HasActiveContent = true;
            if (gate == "radial menu") ControllerMenuAccessibility.HasActiveContent = true;
            if (gate == "loading") LE.UI.LoadingScreen.IsVisible = true;
            if (gate == "focus") Application.isFocused = false;
            Tick(); SkillAccessibility.Update(readDetails: true);
            check(ScreenReader.Messages.Count == 0 && !SkillAccessibility.HasActiveContent && !SkillAccessibility.HasPendingContent,
                $"{gate} blocks background automatic and manual speech");
        }
        fixture = Reset();
        BindingTests.Icon(fixture.Ability, "cast"); BindingTests.Bind("cast", EpochInputManager.InputActions.Ability1, "A");
        PromptsInfoProvider.Instance.Throw = true;
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Bola de fuego. Equipped. Learned",
            "Unavailable binding preserves known equipped status");
        NavigationManager.currentlySelected = null; Tick();
        check(!SkillAccessibility.HasActiveContent && !SkillAccessibility.HasPendingContent, "Closing resets ownership");
        NavigationManager.currentlySelected = fixture.Panel; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2, "Reopening reads the brief selection again");

        fixture = Reset(); Tick(); Tick();
        Tooltip(fixture);
        fixture.Tooltip.statFields = new();
        var liveRoot = fixture.Tooltip.content.Child();
        var liveValue = Text(liveRoot, "Daño por segundo: 80");
        fixture.Tooltip.statFields.Add(new UITooltipAbility.BigStatField { obj = liveRoot, textField = liveValue });
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 1, "Late tooltip never adds unsolicited fragments to initial speech");
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("80"), "Manual inspection includes current labeled combat stats");
        liveValue.text = "Daño por segundo: 82"; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2, "Fluctuating DPS remains silent automatically");
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("82"), "A second manual inspection refreshes the actual stat");
        fixture.Tooltip.descriptionText.text = "Nueva mejora"; liveRoot.Active = false;
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("Nueva mejora") && !ScreenReader.Messages.Last().Text.Contains("82"),
            "Hidden stats never persist across manual requests");
        fixture = Reset();
        SkillAccessibility.Update(readDetails: true); Tick(); Tick();
        check(ScreenReader.Messages.Count == 1, "Manual read before settled focus suppresses a following automatic duplicate");
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
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("Descripción local") &&
            !ScreenReader.Messages.Last().Text.Contains("Detalle moderno"), "Manual read also rejects stale node details");
        int beforeModal = ScreenReader.Messages.Count;
        ScreenContentAnnouncer.HasActiveContent = true;
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Count == beforeModal, "Manual skill gesture cannot read behind a modal");
        ScreenContentAnnouncer.HasActiveContent = false;
        modernSystem._nodeTooltipRequiresLayout = false;
        SkillAccessibility.Update(readDetails: true);
        int afterNodeRead = ScreenReader.Messages.Count;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == afterNodeRead,
            "Differently worded tooltip title cannot cause a brief node reread after manual inspection");
        node.pointsAllocated = 1; Tick(); Tick();
        check(ScreenReader.Messages.Last().Text == "Nombre local. 1 of 5 points allocated",
            "Allocating a node updates points without its description or the parent ability binding");

        fixture = Reset();
        var hotbar = new GameObject().Add(new PrioritizedNavigable());
        var slotControl = hotbar.gameObject.Child().Add(new Selectable());
        var barIcon = slotControl.gameObject.Add(new AbilityBarIcon { abilityNumber = 1, player = PlayerFinder.player,
            ability = new Ability { abilityName = "Golpe de combo" } });
        hotbar.ActiveSelectable = slotControl; NavigationManager.currentlySelected = hotbar;
        BindingTests.Equip(1, fixture.Ability);
        BindingTests.Bind("bar-one", EpochInputManager.InputActions.Ability1, "Square");
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Bola de fuego. Equipped: Square. Learned. Action bar: Square",
            "Assignment bar reads the stable skill and actual destination button rather than its current combo hit");
        BindingTests.Equip(1, null); Tick(); Tick();
        check(ScreenReader.Messages.Last().Text == "Empty skill slot. Action bar: Square",
            "Empty action bar slot retains its button and does not announce old skill text");
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("open the list"), "Manual slot details explain how to choose a replacement");
        var choices = new GameObject().Add(new UnityDrivenGridNavigable());
        var choice = choices.gameObject.Child().Add(new Selectable());
        choice.gameObject.Add(new AbilitySelectionIcon { ability = fixture.Ability });
        choices.ActiveSelectable = choice;
        AbilityBarSelectorManager.instance = new GameObject().Add(new AbilityBarSelectorManager { IsMenuOpened = true,
            abilityClicked = 1, _playerObject = PlayerFinder.player, _gridNavigable = choices });
        NavigationManager.currentlySelected = choices; Tick(); Tick();
        check(ScreenReader.Messages.Last().Text == "Bola de fuego. Not equipped. Learned. Assign to: Square",
            "Available skill menu distinguishes the destination button from current assignment");
        SkillAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last().Text.Contains("confirm to assign"), "Manual candidate details explain confirmation");
        fixture = Reset();
        fixture.Icon.UnlockReqTMP = Text(fixture.Control.gameObject, "8");
        fixture.Icon.UnlockReqTMP.gameObject.Active = false;
        var variant = new Ability { abilityName = "Bow variant", overridePrimaryAbility = true,
            customPrimaryAbility = new AbilityRef { target = fixture.Ability } };
        BindingTests.Equip(2, variant);
        BindingTests.Bind("equipped-variant", EpochInputManager.InputActions.Ability2, "Square");
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Bola de fuego. Equipped: Square. Learned. Required level: 8",
            "Navigation combines declared-primary assignment, learned state and hidden owned required level without tooltip body");
        fixture.Icon.locked = true; BindingTests.Equip(2, null); Tick(0.21); Tick();
        check(ScreenReader.Messages.Last().Text == "Bola de fuego. Not equipped. Not learned. Required level: 8",
            "Changed native lock and assignment state update automatically without a manual description request");
        SkillAccessibility.Reset();
        foreach (string gate in new[] { "popup", "dialogue", "radial menu", "loading", "focus", "selection" })
        {
            fixture = Reset();
            Localization.Abilities.OnDescription = () =>
            {
                ScreenContentAnnouncer.HasActiveContent = gate == "popup";
                DialogueAnnouncer.HasActiveContent = gate == "dialogue";
                ControllerMenuAccessibility.HasActiveContent = gate == "radial menu";
                LE.UI.LoadingScreen.IsVisible = gate == "loading";
                Application.isFocused = gate != "focus";
                if (gate == "selection") fixture.Icon.ability = new Ability { abilityName = "New selection" };
            };
            SkillAccessibility.Update(readDetails: true);
            check(ScreenReader.Messages.Count == 0 && !SkillAccessibility.HasPendingContent,
                "Manual skill inspection cancels output when ownership changes during detail formatting: " + gate);
        }
        Reset();
        CheckFirstOpening(check);
    }

    private static void CheckFirstOpening(Action<bool, string> check)
    {
        var fixture = Reset();
        var pending = new Ability { overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef() };
        BindingTests.Equip(1, pending);
        BindingTests.Equip(2, fixture.Ability);
        BindingTests.Bind("cast-two", EpochInputManager.InputActions.Ability2, "Triangle");
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Bola de fuego. Equipped: Triangle. Learned",
            "First class-panel opening announces its assigned button while another primary is pending");
        pending.customPrimaryAbility.target = new Ability();
        Tick(0.21); Tick();
        check(ScreenReader.Messages.Count == 1, "Unrelated resolution does not duplicate the same spoken binding");

        fixture = Reset();
        pending = new Ability { overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef() };
        BindingTests.Equip(1, pending);
        BindingTests.Bind("cast-one", EpochInputManager.InputActions.Ability1, "Square");
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Bola de fuego. Learned",
            "An unresolved selected variant stays unknown instead of falsely announcing not equipped");
        for (int index = 0; index < 4; index++) { Tick(0.21); Tick(); }
        check(ScreenReader.Messages.Count == 1, "Pending first-open identity does not repeat the name");
        check(LeAccess.Plugin.Log.Information.Count(line => line == "[Skill binding] primary identity pending") == 1,
            "Persistent first-open readiness failure has one diagnostic");
        pending.customPrimaryAbility.target = fixture.Ability;
        Tick(0.21); Tick();
        check(ScreenReader.Messages.Last().Text == "Bola de fuego. Equipped: Square. Learned" && ScreenReader.Messages.Count == 2,
            "Primary becomes readable during the first opening and assignment is announced without closing");
        check(LeAccess.Plugin.Log.Information.Count(line => line == "[Skill binding] ready") == 1,
            "Recovery is logged once after a pending identity");
        Tick(0.21); Tick();
        check(ScreenReader.Messages.Count == 2, "Recovered binding does not repeat every refresh");
        NavigationManager.currentlySelected = null; Tick();
        NavigationManager.currentlySelected = fixture.Panel; Tick(); Tick();
        check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text.Contains("Equipped: Square"),
            "Second opening remains correct after first-opening recovery");

        fixture = Reset();
        BindingTests.Equip(1, fixture.Ability);
        BindingTests.Bind("cast-one", EpochInputManager.InputActions.Ability1, "Square");
        var combo = (ComboAbilityList)PlayerFinder.getPlayerActor().abilities;
        combo.HasInitialised = false;
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Bola de fuego. Learned", "Pre-initialization menu never guesses equipped state");
        combo.HasInitialised = true;
        Tick(0.21); Tick();
        check(ScreenReader.Messages.Last().Text == "Bola de fuego. Equipped: Square. Learned" && ScreenReader.Messages.Count == 2,
            "Combo initialization completes while first menu remains open and speech recovers");
        check(LeAccess.Plugin.Log.Information.Contains("[Skill binding] combo initialization pending"),
            "Initialization delay is distinguished from unresolved primary metadata");

        fixture = Reset();
        pending = new Ability { overridePrimaryAbility = true, customPrimaryAbility = new AbilityRef() };
        BindingTests.Equip(1, pending);
        Tick(); Tick();
        NavigationManager.currentlySelected = null; Tick();
        pending.customPrimaryAbility.target = fixture.Ability;
        Tick(0.21); Tick();
        check(ScreenReader.Messages.Count == 1, "Resolution after the menu closes does not announce a stale selection");
        check(!SkillAccessibility.HasActiveContent, "Closed first-open ownership stays released during initialization");
        fixture = Reset();
        ScreenReader.OutputAccepted = false;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 0 && SkillAccessibility.HasPendingContent,
            "Rejected skill brief remains pending until transport accepts output");
        ScreenReader.OutputAccepted = true;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 1 && !SkillAccessibility.HasPendingContent,
            "Recovered skill selection is acknowledged exactly once");

        fixture = Reset();
        ScreenReader.OutputAccepted = false;
        SkillAccessibility.Update(readDetails: true);
        check(SkillAccessibility.HasPendingContent, "Rejected manual skill details preserve current request intent");
        Localization.Abilities.Descriptions[fixture.Ability] = "Fresh skill details after recovery.";
        ScreenReader.OutputAccepted = true;
        Tick(otherSpeech: true);
        check(ScreenReader.Messages.Count == 0 && SkillAccessibility.HasPendingContent,
            "Automatic skill detail retry yields to accepted output in the same frame");
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text.Contains("Fresh skill details after recovery"),
            "Recovered skill details are freshly read instead of replaying cached text");
        ScreenReader.Available = false;
        SkillAccessibility.Update(readDetails: true);
        check(SkillAccessibility.HasPendingContent, "Unavailable manual skill intent stays pending after an accepted brief");
        ScreenReader.Available = true; Tick(); Tick();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("Fresh skill details after recovery"),
            "Late skill reader accepts current manual intent exactly once");

        foreach (var gate in new[] { "changed", "hidden", "collected", "unready", "unavailable" })
        {
            fixture = Reset(); ScreenReader.OutputAccepted = false;
            ScreenReader.Available = gate != "unavailable";
            SkillAccessibility.Update(readDetails: true);
            if (gate == "changed")
            {
                fixture.Ability = new Ability { abilityName = "Next ability" };
                fixture.Icon.ability = fixture.Ability;
                Localization.Abilities.Names[fixture.Ability] = "Next selected skill";
            }
            else if (gate == "collected") fixture.Panel.WasCollected = true;
            else if (gate == "unready") fixture.Panel.selectables = null;
            else fixture.Panel.gameObject.Active = false;
            ScreenReader.OutputAccepted = ScreenReader.Available = true;
            Tick(); Tick();
            check(ScreenReader.Messages.All(message => !message.Text.Contains("Lanza una bola ardiente")) &&
                (gate == "changed" ? ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text.Contains("Next selected skill") : ScreenReader.Messages.Count == 0),
                "Failed manual skill details cannot replay after ownership loss: " + gate);
        }
        SkillAccessibility.Reset();
    }
}
