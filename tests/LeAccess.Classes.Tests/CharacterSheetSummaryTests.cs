using LeAccess.Accessibility.CharacterSheet;

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
        return checks;
    }
}
