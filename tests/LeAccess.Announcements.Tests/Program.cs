using LeAccess.Accessibility.Announcements;

var tracker = new PromptAnnouncementTracker();
var empty = new Dictionary<int, string>();
var forge = new Dictionary<int, string> { [1] = "La forja contiene un objeto" };
var menu = new Dictionary<int, string> { [2] = "Detalles", [3] = "Comparar", [4] = "Detalles" };
int checks = 0;

void Check(string expected, string actual, string description)
{
    checks++;
    if (expected != actual) throw new InvalidOperationException(description + ": " + actual);
}

Check("", tracker.Observe(1, forge, true), "Initialization should wait for settled visibility");
Check("", tracker.Observe(1, empty, true), "A transient forge prompt must never speak");
Check("", tracker.Observe(1, menu, true), "New visible hints settle first");
Check("Detalles. Comparar", tracker.Observe(1, menu, true), "Group distinct hints in display registration order");
Check("", tracker.Observe(1, menu, true), "Persistent hints do not repeat");

tracker.Reset();
Check("", tracker.Observe(1, forge, false), "Focus speech has priority");
Check("", tracker.Observe(1, forge, false), "Do not queue hints behind long speech");
Check("", tracker.Observe(2, menu, false), "Changing menu drops old pending hints");
Check("Detalles. Comparar", tracker.Observe(2, menu, true), "Only current hints speak after focus speech");

Check("", tracker.Observe(2, empty, true), "Hidden hints are forgotten");
Check("", tracker.Observe(2, menu, true), "Reopened hints settle");
Check("", tracker.Observe(2, menu, true), "Recreated hints in the same menu are not new information");
var translated = new Dictionary<int, string> { [2] = "Más información" };
Check("", tracker.Observe(2, translated, true), "Localization change settles");
Check("Más información", tracker.Observe(2, translated, true), "Changed text is spoken even without a visibility toggle");
Check("", tracker.Observe(3, translated, true), "New context restarts stability");
Check("", tracker.Observe(3, translated, true), "Unchanged hints do not repeat when child navigables change");
var recreated = new Dictionary<int, string> { [20] = "Más información", [21] = "Más información" };
Check("", tracker.Observe(3, recreated, true), "Replacement widgets settle without losing prior speech");
Check("", tracker.Observe(3, recreated, true), "Identical hints do not repeat under new widget identities");
Check("", tracker.Observe(3, empty, true), "Leaving a focused item hides its hint");
Check("", tracker.Observe(3, recreated, true), "Another focused item brings the same hint back");
Check("", tracker.Observe(3, recreated, true), "Navigation cannot repeatedly announce More information");
Check("", tracker.Observe(4, empty, true), "A different menu discards hints absent from that context");
Check("", tracker.Observe(3, recreated, true), "Returning to a menu settles its instructions");
Check("Más información", tracker.Observe(3, recreated, true), "Instructions are available on a real menu revisit");

tracker.Reset();
Check("", tracker.Observe(3, translated, true), "Loading reset cannot replay a stale hint");
Check("", tracker.Observe(3, empty, false), "Hidden while busy");
Check("", tracker.Observe(3, empty, true), "A hidden pending hint is never replayed");
Console.WriteLine($"Announcement tests passed: {checks} checks.");
