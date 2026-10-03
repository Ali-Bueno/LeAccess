using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class RejectedOutputTests
{
    private static void Reset()
    {
        VisibleSubtitleAnnouncer.Reset(); ScreenReader.Messages.Clear(); ScreenReader.OutputAccepted = true;
        CharacterSelectionVideoManager.instance = null; CinematicSubtitlePanel.Instance = null;
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
        DialogueAnnouncer.HasActiveContent = ScreenContentAnnouncer.HasActiveContent = false;
        PlayerFinder.player = new GameObject(); CameraManager.Main = new GameObject().Add(new Camera());
        Screen.width = 1920; Screen.height = 1080;
    }

    private static (TMP_Text Label, Action Hide) Create(string kind)
    {
        Reset();
        var root = new GameObject();
        var label = root.Child().Add(new TMP_Text { text = "Anterior" });
        if (kind == "video")
            CharacterSelectionVideoManager.instance = new GameObject().Add(new CharacterSelectionVideoManager
            { holder = root, cv = root.Add(new CanvasGroup()), subtitleText = label });
        else if (kind == "cinematic")
            CinematicSubtitlePanel.Instance = root.Add(new CinematicSubtitlePanel
            { _canvasGroup = root.Add(new CanvasGroup()), _subtitleText = label });
        else
        {
            root.Add(new Canvas());
            var ui = root.Add(new FloatingBarkUI { textComponent = label });
            var bark = new GameObject().Add(new FloatingBark { FloatingUI = ui });
            ui.bark = bark; VisibleSubtitleAnnouncer.BarkShown(bark);
        }
        return (label, () => root.Active = false);
    }

    internal static void Run(Action<bool, string> check)
    {
        void Tick() => VisibleSubtitleAnnouncer.Update();
        foreach (string kind in new[] { "bark", "cinematic", "video" })
        {
            var source = Create(kind);
            ScreenReader.OutputAccepted = false; Tick(); Tick();
            check(ScreenReader.Messages.Count == 0 && VisibleSubtitleAnnouncer.HasPendingContent,
                $"Rejected {kind} output retains pending current rendered content");
            ScreenReader.OutputAccepted = true; Tick();
            check(ScreenReader.Messages.Single().Text == "Anterior" && !VisibleSubtitleAnnouncer.HasPendingContent,
                $"Unchanged owned {kind} retries after recovery and acknowledges accepted output");
            Tick(); check(ScreenReader.Messages.Count == 1, $"Accepted {kind} content is not repeated");
            source = Create(kind); ScreenReader.OutputAccepted = false; Tick(); Tick();
            source.Label.text = "Actual"; ScreenReader.OutputAccepted = true; Tick(); Tick();
            check(ScreenReader.Messages.Single().Text == "Actual", $"Rejected {kind} history is replaced by fresh rendered content");
            source = Create(kind); ScreenReader.OutputAccepted = false; Tick(); Tick();
            source.Hide(); ScreenReader.OutputAccepted = true; Tick(); Tick();
            check(ScreenReader.Messages.Count == 0 && !VisibleSubtitleAnnouncer.HasPendingContent,
                $"Hidden rejected {kind} content expires before recovery");
        }
        Reset(); ScreenReader.OutputAccepted = false;
        VisibleSubtitleAnnouncer.NpcMessageSubmitted("Escucha.", "Erza"); Tick();
        check(ScreenReader.Messages.Count == 0 && VisibleSubtitleAnnouncer.HasPendingContent,
            "Rejected NPC submission keeps its existing unexpired pending occurrence");
        ScreenReader.OutputAccepted = true; Tick();
        check(ScreenReader.Messages.Single().Text == "Erza: Escucha." && !VisibleSubtitleAnnouncer.HasPendingContent,
            "Unexpired submitted content is acknowledged after accepted output");
        Tick(); check(ScreenReader.Messages.Count == 1, "Accepted NPC submission is consumed once");
        var tracker = new SubmittedSubtitleTracker();
        tracker.Submit("Anterior", "Erza", 0);
        check(tracker.TryTake(true, 1, out var pending, acknowledge: false) && pending == "Erza: Anterior",
            "A submitted candidate remains pending until explicit acceptance");
        check(!tracker.TryTake(true, SubmittedSubtitleTracker.MaximumDelaySeconds, out pending, acknowledge: false) && pending == null,
            "A rejected submission retains its original expiry deadline");
        check(!tracker.Acknowledge("Erza: Anterior", SubmittedSubtitleTracker.MaximumDelaySeconds),
            "An expired submission cannot be acknowledged or resurrected");
        tracker.Submit("Actual", "Erza", 20);
        tracker.TryTake(true, 21, out pending, acknowledge: false);
        check(!tracker.Acknowledge("Erza: Anterior", 21) && tracker.Acknowledge(pending, 21),
            "Submitted acknowledgement matches the current exact occurrence");
        tracker.Reset(); tracker.Submit("Actual", "Erza", 0);
        tracker.TryTake(true, 1, out pending, out long identity, acknowledge: false);
        tracker.Reset(); tracker.Submit("Actual", "Erza", 2);
        check(!tracker.Acknowledge(identity, pending, 3) && tracker.TryTake(false, 3, out _),
            "An accepted old occurrence cannot acknowledge a newer identical submission after lifecycle reset");
        Reset();
    }
}
