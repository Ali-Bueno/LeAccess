using LeAccess.Accessibility;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class PopupVisibilityTests
{
    internal static void Run(Action<bool, string> check)
    {
        void Frames() { for (int i = 0; i < 3; i++) { Time.frameCount++; ScreenContentAnnouncer.Update(); } }
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var root = new GameObject();
        var label = root.Child().Add(new TMP_Text { text = "Faded confirmation" });
        label.canvasRenderer.Alpha = 0;
        ScreenContentAnnouncer.Track(root);
        Frames();
        check(!ScreenContentAnnouncer.HasActiveContent && !ScreenContentAnnouncer.OwnsFocus(label.gameObject) &&
            ScreenReader.Messages.Count == 0, "A popup with no rendered graphics cannot retain modal ownership behind other menus");
        label.canvasRenderer.Alpha = 1;
        Frames();
        check(ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Single().Text == "Faded confirmation",
            "A registered active popup becomes readable once its presentation fades in");
        label.canvasRenderer.cull = true;
        Frames();
        check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 1,
            "Clipped popup presentation releases modal input/speech ownership");
        label.canvasRenderer.cull = false;
        Frames();
        check(ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 2,
            "Returning a popup to visible rendering settles and rereads its content");
        var top = new GameObject();
        var topLabel = top.Child().Add(new TMP_Text { text = "Top confirmation", alpha = 0 });
        ScreenContentAnnouncer.Track(top);
        Frames();
        check(ScreenContentAnnouncer.OwnsFocus(label.gameObject) && !ScreenContentAnnouncer.OwnsFocus(topLabel.gameObject) &&
            ScreenReader.Messages.Count == 2, "An invisible newer popup cannot steal the visible underlying modal's focus");
        topLabel.alpha = 1;
        Frames();
        check(ScreenContentAnnouncer.OwnsFocus(topLabel.gameObject) && ScreenReader.Messages.Last().Text == "Top confirmation",
            "The newer popup takes ownership when its actual presentation becomes visible");
        topLabel.isActiveAndEnabled = false;
        Frames();
        check(ScreenContentAnnouncer.OwnsFocus(label.gameObject) && ScreenReader.Messages.Count == 3,
            "Disabling all top-popup graphics releases ownership without replaying unchanged underlying content");
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
    }
}
