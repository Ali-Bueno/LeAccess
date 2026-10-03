using LeAccess.Accessibility;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;

internal static class NativeWindowTests
{
    internal static void Run(Action<bool, string> check)
    {
        void Frames() { for (int i = 0; i < 3; i++) { Time.frameCount++; ScreenContentAnnouncer.Update(); } }
        foreach (var panel in new Panel[] { new LightlessArborEntrancePanel(), new SoulfireBastionEntrancePanel(),
            new TemporalSanctumEntrancePanel(), new DungeonFloorExitPanel(), new BossConfirmationPanel(),
            new WarningConfirmationModal(), new AlertModal(), new GenericNativeModal() })
        {
            ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
            var root = new GameObject(); root.Add(panel);
            var title = root.Child().Add(new TMP_Text { text = "Dungeon warning" });
            var body = root.Child().Add(new TMP_Text { text = "Enemies deal increased damage. Choose an exit." });
            root.Child().Add(new TMP_Text { text = "Confirm" });
            var hidden = root.Child(); hidden.Active = false;
            hidden.Add(new TMP_Text { text = "Retained old modifier" });
            var mediator = new GameObject().Add(new NavigableMediator { FocusedPanel = panel });
            NavigationManager.currentlySelected = mediator;
            Frames();
            check(ScreenReader.Messages.SingleOrDefault().Text == "Dungeon warning. Enemies deal increased damage. Choose an exit. Confirm",
                $"Focused native {panel.GetType().Name} reads visible title, instruction and choices");
            check(ScreenContentAnnouncer.HasActiveContent && ScreenContentAnnouncer.OwnsFocus(body.gameObject),
                "A focused native window claims its own controls");
            Frames();
            check(ScreenReader.Messages.Count == 1, "An unchanged native window stays silent");
            body.text = "Enemies deal increased cold damage. Choose an exit."; Frames();
            check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("cold"),
                "Native modifier/instruction changes replace stale content");
            root.Visible = false; Frames();
            check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 2,
                "A hidden native window releases focus and cannot read retained content");
            root.Visible = true; Frames();
            check(ScreenReader.Messages.Count == 3, "Reopening the same native window rereads its current instruction");
            mediator.FocusedPanel = new GameObject().Add(new Panel());
            check(!ScreenContentAnnouncer.OwnsFocus(body.gameObject),
                "Changing the authoritative focused panel invalidates ownership before the next update");
            Frames();
            check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 3,
                "A visible retained native window cannot own a different focused panel");
            mediator.FocusedPanel = panel; root.Active = false; Frames();
            check(!ScreenContentAnnouncer.HasActiveContent, "Inactive native windows never claim focus");
            root.Active = true; panel.isActiveAndEnabled = false; Frames();
            check(!ScreenContentAnnouncer.HasActiveContent, "Disabled native windows never claim focus");
            panel.isActiveAndEnabled = true; Frames();
            title.canvasRenderer.Alpha = body.canvasRenderer.Alpha = 0;
            root.Children[2].GetComponentsInChildren<TMP_Text>(false).Single().alpha = 0;
            Frames();
            check(!ScreenContentAnnouncer.HasActiveContent, "Fully faded native windows release modal ownership");
        }
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        var legacyRoot = new GameObject(); legacyRoot.Add(new DungeonFloorExitPanelUI());
        var legacyLabel = legacyRoot.Child().Add(new TMP_Text { text = "Next floor modifier" });
        NavigationManager.currentlySelected = legacyRoot.Child().Add(new Navigable());
        Frames();
        check(ScreenReader.Messages.Single().Text == "Next floor modifier" &&
            ScreenContentAnnouncer.OwnsFocus(legacyLabel.gameObject),
            "Native dungeon UI ancestry supports legacy navigation without panel mediation");
        var popup = new GameObject(); popup.Add(new TMP_Text { text = "Abandon dungeon?" });
        ScreenContentAnnouncer.Track(popup, separateWindow: true); Frames();
        check(ScreenReader.Messages.Last().Text == "Abandon dungeon?" && !ScreenContentAnnouncer.OwnsFocus(legacyLabel.gameObject),
            "A newer independent popup retains foreground priority over a discovered dungeon window");
        popup.Active = false; Frames();
        check(ScreenReader.Messages.Count == 2 && ScreenContentAnnouncer.OwnsFocus(legacyLabel.gameObject),
            "Closing an independent popup restores dungeon ownership without repeating its unchanged instruction");
        var selectedRoot = new GameObject(); selectedRoot.Add(new DungeonEnterPanelUI());
        selectedRoot.Child().Add(new TMP_Text { text = "Select dungeon tier" });
        var selectable = selectedRoot.Child().Add(new Behaviour());
        NavigationManager.currentlySelected = new GameObject().Add(new Navigable { ActiveSelectable = selectable });
        Frames();
        check(ScreenReader.Messages.Last().Text == "Select dungeon tier",
            "A native selected control identifies its exact dungeon owner even when navigation is elsewhere");
        selectable.WasCollected = true; Frames();
        check(!ScreenContentAnnouncer.HasActiveContent,
            "Collected selected-control wrappers invalidate discovered ownership without access");
        ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
        NavigationManager.currentlySelected = null;
    }
}
