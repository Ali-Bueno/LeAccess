using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class GenericModalTests
{
    private sealed class Window
    {
        internal GameObject Root, Static;
        internal Panel Panel;
        internal TMP_Text Title, Message, Info, Caption, Placeholder;
        internal readonly List<TMP_Text> Items = new();
    }

    internal static void Run(Action<bool, string> check)
    {
        void Frames() { for (int i = 0; i < 3; i++) { Time.frameCount++; ScreenContentAnnouncer.Update(); } }
        TMP_Text Label(GameObject parent, string text) => parent.Child().Add(new TMP_Text { text = text });
        Window Build(Panel panel, int template)
        {
            var window = new Window { Root = new GameObject(), Panel = panel };
            window.Root.Add(panel);
            var templateRoot = window.Root.Child();
            window.Title = Label(templateRoot, "Edit rule");
            window.Message = Label(templateRoot, "Choose what this rule shows.");
            if (template == 1) templateRoot.Add(new LE.UI.PanelSystem.ModalTemplate { _title = window.Title, _message = window.Message });
            if (template == 2) templateRoot.Add(new LE.UI.Components.ModalTemplate { _title = window.Title, _message = window.Message });
            var confirm = templateRoot.Child(); confirm.Add(new Button()); Label(confirm, "Confirm");
            window.Info = Label(window.Root, "Costs 100 gold");
            var scroll = window.Root.Child(); scroll.Add(new ScrollRect());
            var content = scroll.Child().Child();
            window.Items.Add(Label(content, "Sword")); window.Items.Add(Label(content, "Axe"));
            window.Placeholder = Label(content, "Options are loading... please wait...");
            var toggle = window.Root.Child(); toggle.Add(new Selectable());
            window.Caption = Label(toggle, "Show uniques");
            window.Static = window.Info.gameObject;
            return window;
        }
        NavigableMediator Focus(Panel panel)
        {
            var mediator = new GameObject().Add(new NavigableMediator { FocusedPanel = panel });
            NavigationManager.currentlySelected = mediator;
            return mediator;
        }
        const string Opening = "Edit rule. Choose what this rule shows. Costs 100 gold";

        foreach (int template in new[] { 1, 2 })
        {
            ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
            var modal = Build(new GenericNativeModal(), template);
            var mediator = Focus(modal.Panel);
            ScreenReader.OutputAccepted = false; Frames();
            check(ScreenReader.Messages.Count == 0 && ScreenContentAnnouncer.BlocksNotifications,
                "An undelivered generic modal opening keeps notification priority");
            ScreenReader.OutputAccepted = true; Frames();
            check(ScreenReader.Messages.SingleOrDefault() == (Opening, true),
                $"Generic modal (template {template}) reads its template header/body and static text once on open");
            check(ScreenContentAnnouncer.HasActiveContent && ScreenContentAnnouncer.OwnsFocus(modal.Items[0].gameObject) &&
                !ScreenContentAnnouncer.BlocksNotifications,
                "A delivered generic modal keeps its controls but releases passive notifications");
            modal.Items[0].text = "Mace"; modal.Items[1].gameObject.Active = false;
            modal.Placeholder.text = "Two-handed swords"; Label(modal.Items[0].gameObject.Parent, "Bow");
            Frames();
            check(ScreenReader.Messages.Count == 1, "Scrolling or loading a list inside a generic modal stays silent");
            modal.Caption.text = "Hide uniques"; Frames();
            check(ScreenReader.Messages.Count == 1, "Control caption changes inside a generic modal stay silent");
            modal.Info.text = "Costs 200 gold"; Frames();
            check(ScreenReader.Messages.Count == 1, "Static text outside the header/body never forces a generic modal reread");
            modal.Message.text = "Choose what this rule hides."; Frames();
            check(ScreenReader.Messages.Count == 2 &&
                ScreenReader.Messages.Last() == ("Edit rule. Choose what this rule hides.", false),
                "A changed header/body rereads only the header/body without interrupting");
            Frames();
            check(ScreenReader.Messages.Count == 2, "An unchanged header/body stays silent");

            var child = Build(new GenericNativeModal(), template);
            child.Title.text = "Pick affixes";
            mediator.FocusedPanel = child.Panel; Frames();
            check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text.StartsWith("Pick affixes"),
                "A child generic modal reads its own opening");
            mediator.FocusedPanel = modal.Panel; Frames();
            check(ScreenReader.Messages.Count == 3 && ScreenContentAnnouncer.OwnsFocus(modal.Items[0].gameObject),
                "Returning from a child modal resumes the parent without repeating it");
            modal.Title.text = "Edit filter rule"; Frames();
            check(ScreenReader.Messages.Count == 4 && !ScreenReader.Messages.Last().Interrupt,
                "Header changes after returning are still reported without interrupting");
            Application.isFocused = false; Frames();
            Application.isFocused = true; Frames();
            check(ScreenReader.Messages.Count == 4, "Regaining application focus does not repeat an open generic modal");
            modal.Root.Visible = false; Frames();
            modal.Root.Visible = true; Frames();
            check(ScreenReader.Messages.Count == 5 && ScreenReader.Messages.Last().Interrupt &&
                ScreenReader.Messages.Last().Text.EndsWith("Costs 200 gold"),
                "Closing and reopening a generic modal reads its opening again");
        }

        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var bare = Build(new GenericNativeModal(), 0);
        Focus(bare.Panel); Frames();
        check(ScreenReader.Messages.SingleOrDefault() == (Opening, true),
            "A generic modal without a template reads its static text once");
        bare.Message.text = "Changed static body."; bare.Items[0].text = "Mace"; Frames();
        check(ScreenReader.Messages.Count == 1, "A generic modal without typed header/body never rereads");

        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var late = Build(new GenericNativeModal(), 0);
        Focus(late.Panel); Frames();
        int lateBase = ScreenReader.Messages.Count;
        late.Message.text = "Before template."; Frames();
        late.Title.gameObject.Parent.Add(new LE.UI.PanelSystem.ModalTemplate { _title = late.Title, _message = late.Message });
        late.Message.text = "After template."; Frames();
        check(ScreenReader.Messages.Count == lateBase + 1 && ScreenReader.Messages.Last().Text.EndsWith("After template."),
            "A template that appears after the first frame is still resolved for header changes");
        var replacement = Label(late.Root, "New title");
        var newMessage = Label(late.Root, "New body.");
        late.Title.gameObject.Parent.Active = false;
        late.Root.Add(new LE.UI.PanelSystem.ModalTemplate { _title = replacement, _message = newMessage });
        late.Root.Visible = false; Frames();
        late.Root.Visible = true; Frames();
        newMessage.text = "Newer body."; Frames();
        check(ScreenReader.Messages.Last().Text.Contains("Newer body."),
            "A re-instantiated template is re-resolved on the next opening");

        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var warning = Build(new WarningConfirmationModal(), 1);
        Focus(warning.Panel); Frames();
        check(ScreenReader.Messages.SingleOrDefault().Text ==
            "Edit rule. Choose what this rule shows. Confirm. Costs 100 gold. Sword. Axe. Options are loading... please wait... Show uniques",
            "Explicit warning modals keep reading their whole visible window");
        warning.Items[0].text = "Mace"; Frames();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Interrupt &&
            ScreenContentAnnouncer.BlocksNotifications, "Explicit warning changes keep rereading with notification priority");

        int before = UiVisibility.PresentationChecks;
        for (int i = 0; i < 20; i++) _ = ScreenContentAnnouncer.HasActiveContent && ScreenContentAnnouncer.OwnsFocus(warning.Static);
        check(UiVisibility.PresentationChecks - before <= 1, "Repeated foreground queries reuse one presentation walk per frame");
        warning.Root.Active = false;
        check(!ScreenContentAnnouncer.HasActiveContent, "A window deactivated mid-frame releases ownership immediately");
        warning.Root.Active = true;
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        NavigationManager.currentlySelected = null;
    }
}
