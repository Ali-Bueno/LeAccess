using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class ReadingOrderTests
{
    internal static void Run(Action<bool, string> check)
    {
        TMP_Text Label(GameObject owner, string text, float x, float y, float height = 20)
        {
            var child = owner.Child();
            child.transform = new RectTransform { gameObject = child, Bounds = new(x, y, 100, height) };
            return child.Add(new TMP_Text { text = text });
        }
        void Frames() { for (int i = 0; i < 3; i++) { Time.frameCount++; ScreenContentAnnouncer.Update(); } }
        var popup = new GameObject();
        var right = Label(popup, "Right button", 300, 10);
        var body = Label(popup, "Body", 0, 100);
        var left = Label(popup, "Left button", 0, 10);
        var title = Label(popup, "Title", 0, 150);
        var legacyRoot = popup.Child();
        legacyRoot.transform = new RectTransform { gameObject = legacyRoot, Bounds = new(0, 60, 100, 20) };
        legacyRoot.Add(new UnityEngine.UI.Text { text = "Legacy middle" });
        check(ScreenContentReader.Read(popup, title, body) == "Title. Body. Legacy middle. Left button. Right button",
            "Actual screen position sorts mixed TMP/legacy labels and reversed serialized buttons");
        ((RectTransform)left.transform).Bounds = new(0, 8, 100, 24);
        check(ScreenContentReader.Read(popup, title, body).EndsWith("Left button. Right button"),
            "Different font heights on the same row retain left-to-right order");
        var foreign = Label(new GameObject(), "Foreign header", 0, 200);
        check(!ScreenContentReader.Read(popup, foreign, foreign).Contains("Foreign"), "Foreign header/body references cannot leak another panel");
        body.canvasRenderer.Alpha = 0;
        check(!ScreenContentReader.Read(popup, title, body).Contains("Body"), "Renderer-faded text is omitted from a visible popup");
        body.canvasRenderer.Alpha = 1;
        popup.Visible = false;
        check(ScreenContentReader.Read(popup, title, body) == null, "Hidden root cannot be read through still-live label references");
        popup.Visible = true;
        var unknown = new GameObject().Add(new TMP_Text { text = "Semantic anchor" });
        var fragments = new List<UiReadingOrder.Fragment> { new(unknown, unknown.text), new(right, right.text), new(left, left.text) };
        check(string.Join("|", UiReadingOrder.Read(fragments)) == "Semantic anchor|Left button|Right button",
            "Unavailable geometry retains its semantic position while known positions sort");
        unknown.WasCollected = true;
        check(!UiReadingOrder.Read(fragments).Contains("Semantic anchor"), "Collected label is discarded before geometry access");

        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var canvasRoot = new GameObject(); canvasRoot.Add(new Canvas());
        var back = canvasRoot.Child(); var backLabel = Label(back, "Back window", 0, 0);
        var front = canvasRoot.Child(); var frontLabel = Label(front, "Front window", 0, 0);
        ScreenContentAnnouncer.Track(front); ScreenContentAnnouncer.Track(back); Frames();
        check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text == "Front window",
            "Later hook for a visually lower sibling cannot steal popup speech");
        check(ScreenContentAnnouncer.OwnsFocus(frontLabel.gameObject) && !ScreenContentAnnouncer.OwnsFocus(backLabel.gameObject),
            "Foreground focus follows drawing hierarchy as well as announcement choice");
        front.Visible = false; Frames();
        check(ScreenReader.Messages[^1].Text == "Back window", "Hiding the front window releases pending background content");

        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var lowerCanvas = new GameObject(); var low = lowerCanvas.Add(new Canvas { renderOrder = 1, sortingOrder = 1, sortingLayerID = 1 });
        var higherCanvas = new GameObject(); higherCanvas.Add(new Canvas { renderOrder = 2, sortingOrder = 2, sortingLayerID = 1 });
        Label(lowerCanvas, "Lower canvas", 0, 0); Label(higherCanvas, "Higher canvas", 0, 0);
        ScreenContentAnnouncer.Track(higherCanvas); ScreenContentAnnouncer.Track(lowerCanvas); Frames();
        check(ScreenReader.Messages.Single().Text == "Higher canvas", "Engine overlay draw order overrides notification chronology");
        low.renderMode = RenderMode.WorldSpace;
        check(!UiWindowOrder.TryCompare(lowerCanvas, higherCanvas, out _), "Uncomparable camera/world render order is not guessed");
        check(UiWindowOrder.TryCompare(front, front, out int same) && same == 0, "Same surface has equal drawing order");

        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var host = new GameObject(); host.Add(new Canvas()); Label(host, "Outer popup", 0, 0);
        ScreenContentAnnouncer.Track(host); Frames();
        var inner = host.Child(); var innerTitle = Label(inner, "Nested confirmation", 0, 0);
        inner.Add(new Canvas { overrideSorting = true, renderOrder = 3 });
        var confirmation = inner.Add(new LE.UI.Generic.UnityUI.ConfirmationPanel { HeaderText = innerTitle });
        ScreenContentAnnouncer.TrackPopup(confirmation); Frames();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages[^1].Text == "Nested confirmation",
            "Native nested popup remains independent of an already tracked ancestor");
        check(ScreenContentAnnouncer.OwnsFocus(innerTitle.gameObject), "Nested confirmation owns its own focus");
        ScreenContentAnnouncer.RemovePopup(confirmation); inner.Active = false; Frames();
        check(ScreenReader.Messages.Count == 2, "Returning to an unchanged ancestor does not reread it");
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
    }
}
