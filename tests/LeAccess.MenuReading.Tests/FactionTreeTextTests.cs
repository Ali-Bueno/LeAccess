using LeAccess.Accessibility.Panels;
using LeAccess.I18n;

internal static class FactionTreeTextTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        foreach (string language in new[] { "es", "en" })
        {
            ModText.SetLanguage(language);
            string Get(string key) => ModText.Get(key);
            string Points(int have, int max) => ModText.Format("node.points", have, max);

            Check(FactionTreeText.Node("Huevos", 1, 3, true, "Más huevos") == $"Huevos. {Points(1, 3)}. Más huevos", language + ": open node");
            Check(FactionTreeText.Node("Huevos", 0, 3, false, "Más huevos") == $"Huevos. {Points(0, 3)}. {Get("node.locked")}. Más huevos",
                language + ": a locked node says so before its effect");
            Check(FactionTreeText.Node("Huevos", 0, 3, true, null) == $"Huevos. {Points(0, 3)}", language + ": no effect text, no filler");

            var before = new FactionTreeText.NodeState(7, 0, 3, 2, true, null);
            Check(FactionTreeText.Changes(before, before with { Points = 1, Unspent = 1 }) == $"{Points(1, 3)}. {FactionTreeText.PointsLeft(1)}",
                language + ": a spent point says the node and what is left");
            Check(FactionTreeText.Changes(before, before with { Unspent = 3 }) == FactionTreeText.PointsLeft(3),
                language + ": only the tree changed");
            Check(FactionTreeText.Changes(before, before with { Pending = "2 sin confirmar" }) == "2 sin confirmar", language + ": pending text is the game's");
            Check(FactionTreeText.Changes(before, before with { RequirementsMet = false }) == Get("node.locked"), language + ": a node that locks again");
            Check(FactionTreeText.Changes(before, before) == null, language + ": no change, no speech");

            Check(FactionTreeText.TreeIntro(2, "1 sin confirmar") == $"{Get("weaver.tree")}. {ModText.Format("weaver.points_left", 2)}. 1 sin confirmar",
                language + ": tree introduction");
            Check(FactionTreeText.TreeIntro(0, null) == $"{Get("weaver.tree")}. {ModText.Format("weaver.points_left", 0)}", language + ": no points left is said");

            string up = Get("guidance.direction.up"), left = Get("guidance.direction.left");
            string details = FactionTreeText.Details(new[] { "Efecto", "Bono" }, new[] { "Req" },
                new[] { new FactionTreeText.Neighbour("Alfa", "guidance.direction.up"), new("Beta", "guidance.direction.left") });
            Check(details == $"Efecto. Bono. Req. {ModText.Format("weaver.connected", $"Alfa, {up}; Beta, {left}")}", language + ": details list effect, requirements, neighbours");
            Check(FactionTreeText.Details(new string[0], new string[0], new FactionTreeText.Neighbour[0]) == null, language + ": nothing to detail");

            Check(FactionTreeText.Timeline("El Sol Negro", "Presagiador de la Guerra", true, 3, 10) ==
                $"El Sol Negro. Presagiador de la Guerra. {Get("weaver.timeline_defeated")}. {ModText.Format("list.position", 3, 10)}", language + ": defeated timeline");
            Check(FactionTreeText.Timeline("La Última Ruina", null, false, 1, 10) ==
                $"La Última Ruina. {Get("weaver.timeline_pending")}. {ModText.Format("list.position", 1, 10)}", language + ": pending timeline without a name");
            Check(FactionTreeText.Timeline("Sola", "Uno", false, 1, 1) == $"Sola. Uno. {Get("weaver.timeline_pending")}", language + ": a lone timeline has no place");
            Check(FactionTreeText.TimelinesIntro(new[] { "Corrupción requerida: 300", "Muertes: 9" }, 4, 10) ==
                $"Corrupción requerida: 300. Muertes: 9. {ModText.Format("weaver.timelines_done", 4, 10)}", language + ": timelines introduction");
            Check(FactionTreeText.TimelinesIntro(new string[0], 0, 0) == null, language + ": nothing to introduce");
        }
        ModText.SetLanguage("en");
        return checks;
    }
}
