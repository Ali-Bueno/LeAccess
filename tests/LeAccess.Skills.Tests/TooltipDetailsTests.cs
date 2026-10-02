using LE.UI.Tooltips;
using LeAccess.Accessibility.Skills;
using TMPro;
using UnityEngine;

internal static class TooltipDetailsTests
{
    private static TMP_Text Text(GameObject root, string value) => root.Child().Add(new TMP_Text { text = value });

    internal static void Run(Action<bool, string> check)
    {
        var node = new GameObject().Add(new SkillTreeNode());
        var system = new GameObject().Add(new TooltipSystem());
        UIBase.instance = new GameObject().Add(new UIBase { TooltipSystem = system });
        var tooltip = new GameObject().Add(new NodeTooltip());
        system._nodeTooltip = tooltip;
        system.ActiveNodeTooltipNode = node;
        tooltip._info = system._activeNodeTooltipInfo = new NodeTooltipInfo { Node = node };
        tooltip._name = Text(tooltip.gameObject, "Fuerza");
        tooltip._description = Text(tooltip.gameObject, "Mejora el golpe");
        tooltip._modifiersSection = tooltip.gameObject.Child().Add(new Component());
        Text(tooltip._modifiersSection.gameObject, "+20% daño");
        tooltip._requirementsSection = tooltip.gameObject.Child().Add(new Component());
        Text(tooltip._requirementsSection.gameObject, "Requiere 3 puntos");
        tooltip._pointBonusContainer = tooltip.gameObject.Child();
        Text(tooltip._pointBonusContainer, "Con 5 puntos: golpe doble");
        tooltip._refundCostRoot = tooltip.gameObject.Child();
        Text(tooltip._refundCostRoot, "Reembolso: 100 oro");
        Text(tooltip.gameObject, "Unrelated action prompt");
        check(SkillTooltipDetailsReader.TryReadModernNode(node, out var name, out var details, out var visible) &&
            visible && name == "Fuerza" && details.Contains("+20%") && details.Contains("Requiere 3") &&
            details.Contains("golpe doble") && details.Contains("100 oro") && !details.Contains("Unrelated"),
            "Modern node sections include owned modifiers, requirements, bonus and refund without action prompts");
        tooltip._requirementsSection.gameObject.Active = false;
        check(SkillTooltipDetailsReader.TryReadModernNode(node, out _, out details, out _) && !details.Contains("Requiere"),
            "Hidden node requirements are excluded");
        tooltip._requirementsSection.gameObject.Active = true;
        tooltip._info.PointsAllocated = 2;
        tooltip._info.Requirements = new();
        var metRequirement = new NodeTooltipRequirementInfo { Text = "<color=green>Requiere 3 puntos</color>", IsMet = true };
        var unmetRequirement = new NodeTooltipRequirementInfo { Text = "Completa la misión", IsMet = false };
        tooltip._info.Requirements.Add(metRequirement);
        tooltip._info.Requirements.Add(unmetRequirement);
        LeAccess.I18n.ModText.SetLanguage("en");
        check(SkillTooltipDetailsReader.TryReadModernNode(node, out _, out details, out _) &&
            details.Contains(LeAccess.I18n.ModText.Format("node.points", 2, 5)) &&
            details.Contains(LeAccess.I18n.ModText.Format("node.requirement_met", "Requiere 3 puntos")) &&
            details.Contains(LeAccess.I18n.ModText.Format("node.requirement_unmet", "Completa la misión")) &&
            !details.Contains("<color") && details.Split("Requiere 3 puntos").Length == 2,
            "Allocated/max points and met/unmet status use localized model entries without duplicate rendered requirements");
        tooltip._info.PointsAllocated = 3;
        unmetRequirement.IsMet = true;
        check(SkillTooltipDetailsReader.TryReadModernNode(node, out _, out details, out _) &&
            details.Contains(LeAccess.I18n.ModText.Format("node.points", 3, 5)) &&
            details.Contains(LeAccess.I18n.ModText.Format("node.requirement_met", "Completa la misión")),
            "Point allocation and changed requirement status refresh current modern detail");
        LeAccess.I18n.ModText.SetLanguage("es");
        check(SkillTooltipDetailsReader.TryReadModernNode(node, out name, out details, out _) && name == "Fuerza" &&
            details.Contains(LeAccess.I18n.ModText.Format("node.points", 3, 5)) &&
            details.Contains("Requiere 3 puntos") && !details.Contains("Requirement met"),
            "Spanish wrappers preserve game-provided node and requirement text");
        LeAccess.I18n.ModText.SetLanguage("en");
        tooltip._info.Requirements.WasCollected = true;
        check(SkillTooltipDetailsReader.TryReadModernNode(node, out _, out details, out _) &&
            details.Contains("Requiere 3 puntos") && !details.Contains("Completa la misión"),
            "Unavailable model list falls back to rendered requirement section");
        foreach (var stale in new[] { "layout", "info identity", "info node", "active node", "collected", "hidden" })
        {
            var info = new NodeTooltipInfo { Node = node };
            tooltip._info = system._activeNodeTooltipInfo = info;
            system.ActiveNodeTooltipNode = node;
            system._nodeTooltipRequiresLayout = false;
            tooltip.gameObject.Active = true;
            if (stale == "layout") system._nodeTooltipRequiresLayout = true;
            if (stale == "info identity") system._activeNodeTooltipInfo = new NodeTooltipInfo { Node = node };
            if (stale == "info node") info.Node = new GameObject().Add(new SkillTreeNode());
            if (stale == "active node") system.ActiveNodeTooltipNode = new GameObject().Add(new SkillTreeNode());
            if (stale == "collected") info.WasCollected = true;
            if (stale == "hidden") tooltip.gameObject.Active = false;
            check(!SkillTooltipDetailsReader.TryReadModernNode(node, out _, out _, out _),
                $"Modern {stale} cannot leak the previous node's tooltip");
        }
        UIBase.instance = null;
        var ability = new GameObject().Add(new UITooltipAbility());
        var statsRoot = ability.gameObject.Child();
        var mana = Text(statsRoot, "Coste de maná: 12");
        ability.statFields = new();
        ability.statFields.Add(new UITooltipAbility.BigStatField { obj = statsRoot, textField = mana });
        ability.scalingTagsText = Text(ability.gameObject, "Fuego, hechizo");
        ability.minionTagsText = Text(ability.gameObject, "Esbirro");
        check(SkillTooltipDetailsReader.ReadAbility(ability).Contains("Coste de maná: 12") &&
            SkillTooltipDetailsReader.ReadAbility(ability).Contains("Esbirro"),
            "Ability detail reads actual localized stat labels and tags instead of raw numeric guesses");
        statsRoot.Active = false;
        ability.minionTagsText.alpha = 0;
        check(SkillTooltipDetailsReader.ReadAbility(ability) == "Fuego, hechizo",
            "Hidden stat groups and transparent minion tags are omitted");
        var labeledAbility = new GameObject().Add(new UITooltipAbility { statFields = new() });
        var statLabels = new List<TMP_Text>();
        var statValues = new List<TMP_Text>();
        foreach (var pair in new[] { ("DPS (estimado):", "39,2"), ("Coste de maná:", "12"),
            ("Recarga:", "10s"), ("Coste de canalización:", "20") })
        {
            var root = labeledAbility.gameObject.Child();
            statLabels.Add(Text(root.Child(), pair.Item1));
            var value = Text(root, pair.Item2);
            statValues.Add(value);
            labeledAbility.statFields.Add(new UITooltipAbility.BigStatField { obj = root, textField = value });
        }
        Text(labeledAbility.gameObject, "Unrelated ability action");
        details = SkillTooltipDetailsReader.ReadAbility(labeledAbility);
        check(details == "DPS (estimado): 39,2. Coste de maná: 12. Recarga: 10s. Coste de canalización: 20",
            "Installed stat layout reads localized sibling labels before values and retains native units");
        check(!details.Contains("Unrelated"), "Ability stat scope excludes unrelated tooltip sections");
        statValues[0].alpha = 0;
        details = SkillTooltipDetailsReader.ReadAbility(labeledAbility);
        check(!details.Contains("DPS") && !details.Contains("39,2") && details.Contains("Recarga: 10s"),
            "Invisible stat value does not announce a detached label or affect adjacent stats");
        statValues[0].alpha = 1;
        statLabels[0].text = "Estimated damage per second:";
        check(SkillTooltipDetailsReader.ReadAbility(labeledAbility).StartsWith("Estimated damage per second: 39,2"),
            "Changed game localization is read directly without a stat index or mod translation table");
        statValues[2].canvasRenderer.cull = true;
        check(!SkillTooltipDetailsReader.ReadAbility(labeledAbility).Contains("Recarga"),
            "Culled cooldown value does not leak through visible stat siblings");
        var legacy = new GameObject().Add(new NodeTooltipManager());
        legacy.requirementQuestText = Text(legacy.gameObject, "Completa la misión");
        legacy.altText = Text(legacy.gameObject, "Detalle alternativo");
        legacy.scalingStats = new();
        var stat = legacy.gameObject.Child().Add(new NodeTooltipStatDisplay());
        stat.text = Text(stat.gameObject, "+8 daño");
        legacy.scalingStats.Add(stat);
        check(SkillTooltipDetailsReader.ReadLegacyNode(legacy).Contains("Completa la misión") &&
            SkillTooltipDetailsReader.ReadLegacyNode(legacy).Contains("+8 daño"),
            "Legacy node includes displayed quest requirements and scaling stat labels");
    }
}
