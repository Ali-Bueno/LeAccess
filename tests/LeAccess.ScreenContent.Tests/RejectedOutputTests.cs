using LeAccess.Accessibility;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class RejectedOutputTests
{
    internal static void Run(Action<bool, string> check)
    {
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        ScreenReader.OutputAccepted = false;
        var root = new GameObject();
        var body = root.Child().Add(new TMP_Text { text = "Aviso actual" });
        ScreenContentAnnouncer.Track(root, body);
        void Frame() { Time.frameCount++; ScreenContentAnnouncer.Update(); }
        Frame(); Frame();
        check(ScreenReader.Messages.Count == 0 && ScreenContentAnnouncer.HasPendingContent,
            "A rejected modal output stays pending while its current source remains visible");
        ScreenReader.OutputAccepted = true; Frame();
        check(ScreenReader.Messages.Single().Text == "Aviso actual", "An unchanged visible modal retries after transport recovery");
        Frame(); check(ScreenReader.Messages.Count == 1, "Only accepted modal output is deduplicated");
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear(); ScreenReader.OutputAccepted = false;
        ScreenContentAnnouncer.Track(root, body); Frame(); Frame();
        body.text = "Aviso reemplazado";
        ScreenReader.OutputAccepted = true; Frame(); Frame();
        check(ScreenReader.Messages.Single().Text == "Aviso reemplazado", "Recovery rereads current modal content instead of rejected history");
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear(); ScreenReader.OutputAccepted = false;
        ScreenContentAnnouncer.Track(root, body); Frame(); Frame(); root.Active = false;
        ScreenReader.OutputAccepted = true; Frame(); Frame();
        check(ScreenReader.Messages.Count == 0 && !ScreenContentAnnouncer.HasPendingContent, "A hidden rejected modal never replays after recovery");
        ScreenContentAnnouncer.Reset();
        var tracker = new ScreenContentTracker();
        tracker.Observe(1, "Actual", true, out _, acknowledge: false);
        check(!tracker.Acknowledge(1, "Actual"), "Unsettled content cannot be acknowledged");
        tracker.Observe(1, "Actual", true, out var candidate, acknowledge: false);
        check(candidate == "Actual" && tracker.HasStableContent(1), "Pending observation preserves candidate until explicit acceptance");
        check(!tracker.Acknowledge(1, "Anterior") && tracker.HasStableContent(1), "A stale output cannot acknowledge a different current candidate");
        check(tracker.Acknowledge(1, "Actual") && !tracker.HasStableContent(1), "Explicit acceptance acknowledges the current settled candidate");
        tracker.Observe(1, "Nuevo", false, out _, acknowledge: false);
        check(!tracker.Acknowledge(1, "Actual"), "A replaced candidate cannot acknowledge old output");
        tracker.Hide(1);
        check(!tracker.Acknowledge(1, "Nuevo"), "A removed source cannot acknowledge pending content");
    }
}
