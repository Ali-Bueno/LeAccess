using LE.Tos;
using LE.UI.Login.UnityUI;
using LeAccess.Accessibility;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First-login terms gate and account forms.
internal static class LoginTermsTests
{
    internal static void Run(Action<bool, string> check)
    {
        ModText.SetLanguage("en");
        void Frames(int count = 3) { for (int i = 0; i < count; i++) { Time.frameCount++; ScreenContentAnnouncer.Update(); } }
        string Hint(string accept, string jump) => ScreenContentTracker.Combine(new[]
            { ModText.Format("terms.locked", accept), jump is null ? null : ModText.Format("terms.scroll_button", jump) });
        string Opening(params string[] parts) => ScreenContentTracker.Combine(parts);

        (TosPanel Panel, GameObject Root) Terms(bool require, bool atEnd, bool interactable)
        {
            ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
            var root = new GameObject();
            var accept = root.Child().Add(new Button { interactable = interactable });
            accept.gameObject.Child().Add(new TMP_Text { text = "Accept" });
            var jump = root.Child().Add(new Button());
            jump.gameObject.Child().Add(new TMP_Text { text = "Scroll to end" });
            var panel = root.Add(new TosPanel
            {
                Title = "Terms", _displayedContent = new TosContent { Content = "Body text." },
                _requireAcceptance = require, _hasScrolledToEnd = atEnd, _acceptButton = accept, _scrollToEndButton = jump
            });
            ScreenContentAnnouncer.TrackTerms(panel);
            Frames();
            return (panel, root);
        }

        var (locked, _) = Terms(true, false, false);
        check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Interrupt &&
            ScreenReader.Messages[0].Text == Opening("Terms", Hint("Accept", "Scroll to end"), "Body text."),
            "Terms that gate acceptance read title, hint, then body");
        locked._hasScrolledToEnd = true; Frames();
        check(ScreenReader.Messages.Count == 1, "Reaching the end with Accept still disabled stays silent");
        locked._acceptButton.interactable = true; Frames();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages[1] == (ModText.Format("terms.unlocked", "Accept"), false),
            "Accept becoming available is announced without interrupting");
        Frames(5);
        check(ScreenReader.Messages.Count == 2, "The unlock is never repeated");

        var (end, _) = Terms(true, true, true);
        check(ScreenReader.Messages.Single().Text == Opening("Terms", "Body text.") && ScreenReader.Messages[0].Interrupt,
            "Terms already scrolled to the end have no hint");
        Frames(5);
        check(ScreenReader.Messages.Count == 1, "An already open Accept is not announced as new");

        var (optional, _) = Terms(false, false, true);
        check(ScreenReader.Messages.Single().Text == Opening("Terms", "Body text."), "Terms without a gate have no hint");
        optional._hasScrolledToEnd = true; Frames();
        check(ScreenReader.Messages.Count == 1, "Terms without a gate never announce the unlock");

        var (noJump, _) = Terms(true, false, false);
        noJump._scrollToEndButton = null;
        check(TermsGate.ReadHint(noJump) == ModText.Format("terms.locked", "Accept"), "Missing scroll button omits that sentence");
        noJump._acceptButton.gameObject.Active = false;
        check(TermsGate.ReadHint(noJump) is null, "A hidden Accept yields no hint");

        // Account forms are windows: title and help on opening, the error label only through the update path.
        foreach (bool email in new[] { false, true })
        {
            ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
            var root = new GameObject(); root.name = email ? "EmailPrompt" : "DisplayNamePrompt";
            root.Child().Add(new TMP_Text { text = "Choose your name" });
            root.Child().Add(new TMP_Text { text = "Others will see it." });
            var input = root.Child().Add(new TMP_InputField());
            var submit = root.Child().Add(new Button());
            submit.gameObject.Child().Add(new TMP_Text { text = "Submit" });
            var header = root.Child().Add(new LoginHeader());
            var error = header.label = header.gameObject.Child().Add(new TMP_Text { text = "" });
            Behaviour panel = email
                ? root.Add(new EmailPromptPanel { emailText = input, submitButton = submit, errorHeader = header })
                : root.Add(new DisplayNamePromptPanel { displayNameText = input, submitButton = submit, errorHeader = header });
            NavigationManager.currentlySelected = new GameObject().Add(new Navigable { ActiveSelectable = input.gameObject.Add(new UnityEngine.UI.Selectable()) });
            Frames();
            check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0] == ("Choose your name. Others will see it.", true),
                $"{root.name} window reads title and help only");
            error.text = "Name taken"; error.gameObject.Visible = false; Frames();
            check(ScreenReader.Messages.Count == 1, $"{root.name} hidden error stays silent");
            error.gameObject.Visible = true; Frames();
            check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages[1] == ("Name taken", false),
                $"{root.name} later visible error is spoken without interrupting");
            Frames(5);
            check(ScreenReader.Messages.Count == 2, $"{root.name} unchanged error is not repeated");
            NavigationManager.currentlySelected = null;
        }
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
    }
}
