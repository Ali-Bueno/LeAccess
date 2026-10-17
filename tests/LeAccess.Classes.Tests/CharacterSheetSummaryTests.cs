using LeAccess.Accessibility.CharacterSheet;
using LeAccess.I18n;

internal static class CharacterSheetSummaryTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        // The game's labels are whatever its localization shows; the test supplies stand-ins, not translated literals.
        const string label = "label-a", other = "label-b";
        Check(CharacterSheetSummaryText.Entry(label, " 75% ") == label + ": 75%", "an entry pairs the game's label with its cleaned value");
        Check(CharacterSheetSummaryText.Entry(null, "75%") == null && CharacterSheetSummaryText.Entry(label, " ") == null,
            "an entry without a label or a value is never spoken");
        Check(CharacterSheetSummaryText.Attribute(new[] { "", "14", label }) == label + ": 14", "an attribute names itself by its text without digits");
        Check(CharacterSheetSummaryText.Attribute(new[] { "14" }) == null && CharacterSheetSummaryText.Attribute(new[] { label }) == null,
            "an attribute needs both a name and a value");
        string summary = CharacterSheetSummaryText.Compose(new[] { label + ": 1", null, other + ": 2", label + ": 1" });
        Check(summary == label + ": 1. " + other + ": 2", "the summary lists each distinct entry once, in order: " + summary);

        // A row's values share its label; a value with its own words needs none.
        Check(CharacterSheetSummaryText.Row(label, new[] { "92", " 17% " }) == label + ": 92, 17%", "a row reads all its values under one label");
        Check(CharacterSheetSummaryText.Row(null, new[] { "Level 95" }) == "Level 95" && CharacterSheetSummaryText.Row(null, new[] { "95" }) == null,
            "a row without a label is read only when its value has words");
        Check(CharacterSheetSummaryText.Row(label, new[] { " " }) == null, "a row without values is never spoken");

        // Sections: the first push reads the first; the ends stay put.
        Check(CharacterSheetSummaryText.Step(-1, 4, 1) == 0 && CharacterSheetSummaryText.Step(-1, 4, -1) == 0, "the first push reads the first section");
        Check(CharacterSheetSummaryText.Step(1, 4, 1) == 2 && CharacterSheetSummaryText.Step(2, 4, -1) == 1, "up moves on, down moves back");
        Check(CharacterSheetSummaryText.Step(3, 4, 1) == 3 && CharacterSheetSummaryText.Step(0, 4, -1) == 0, "no wrap at either end");
        Check(CharacterSheetSummaryText.Step(9, 3, -1) == 2 && CharacterSheetSummaryText.Step(0, 0, 1) == -1, "a shorter list keeps the step in range");
        ModText.Initialize(null, _ => { });
        ModText.SetLanguage("en");
        string section = CharacterSheetSummaryText.Section(other, 2, 5, new[] { label + ": 1" });
        Check(section == other + ". " + ModText.Format("list.position", 2, 5) + ". " + label + ": 1", "a section says its title, place and entries: " + section);
        Check(CharacterSheetSummaryText.Section(null, 3, 5, new[] { label + ": 1" }).StartsWith(ModText.Format("list.position", 3, 5)),
            "a section without a title starts with its place");

        // Detail groups (Damage tab, first column): speeds with the one cast-speed line, criticals, then after a spacer
        // two short runs, and a single line alone after another spacer.
        const int speed = 1, cast = 2, crit = 3, multiplier = 4, area = 5, stun = 6, single = 7;
        var lines = new (int, int)[]
        {
            (0, speed), (0, speed), (0, cast), (0, crit), (0, crit), (0, multiplier), (0, multiplier),
            (1, area), (1, area), (1, stun), (1, stun), (2, single)
        };
        Check(string.Join(",", CharacterSheetSummaryText.Groups(lines)) == "0,0,0,1,1,2,2,3,3,4,4,5",
            "groups follow stat kinds and the game's spacers");
        // Defence column: singles stay together until a longer run starts its own group; damage lines with their
        // penetrations stay one group.
        var mixed = new (int, int)[] { (0, 8), (0, 9), (0, 8), (0, 10), (0, 10), (0, 11), (1, 1), (1, 1), (1, 2), (1, 1), (1, 2) };
        Check(string.Join(",", CharacterSheetSummaryText.Groups(mixed)) == "0,0,0,1,1,1,2,2,2,2,2", "singles join the lines before them");
        Check(CharacterSheetSummaryText.Groups(Array.Empty<(int, int)>()).Length == 0, "no lines, no groups");
        return checks;
    }
}
