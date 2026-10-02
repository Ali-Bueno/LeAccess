using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class VideoSubtitleTests
{
    private static CharacterSelectionVideoManager Video(string text)
    {
        VisibleSubtitleAnnouncer.Reset();
        ScreenReader.Messages.Clear();
        CinematicSubtitlePanel.Instance = null;
        Application.isFocused = true;
        LE.UI.LoadingScreen.IsVisible = false;
        DialogueAnnouncer.HasActiveContent = ScreenContentAnnouncer.HasActiveContent = false;
        var holder = new GameObject();
        var video = new GameObject().Add(new CharacterSelectionVideoManager
        {
            holder = holder,
            cv = holder.Add(new CanvasGroup()),
            subtitleText = holder.Child().Add(new TMP_Text { text = text })
        });
        CharacterSelectionVideoManager.instance = video;
        return video;
    }

    internal static void Run(Action<bool, string> check)
    {
        void Tick(bool blocked = false) => VisibleSubtitleAnnouncer.Update(blocked);
        var video = Video("El mundo no siempre fue así.");
        Tick();
        check(ScreenReader.Messages.Count == 0 && VisibleSubtitleAnnouncer.HasPendingContent,
            "Movie's owned subtitle settles independently of CinematicSubtitlePanel");
        Tick();
        check(ScreenReader.Messages.Single().Text == "El mundo no siempre fue así.",
            "Intro movie reads actual localized subtitle label");
        Tick();
        check(ScreenReader.Messages.Count == 1 && !VisibleSubtitleAnnouncer.HasPendingContent,
            "Continuous movie caption is not repeated");
        video.currentSubtitleIndex++;
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 2, "Identical text in a later authored caption reads again");
        video.currentCinematic = new Cinematic();
        Tick(); Tick();
        check(ScreenReader.Messages.Count == 3, "A new movie does not inherit previous caption deduplication");

        video = Video("Introducción durante la transición.");
        LE.UI.LoadingScreen.IsVisible = true;
        VisibleSubtitleAnnouncer.NpcMessageSubmitted("Stale world speech", "Grael");
        Tick(); Tick(); Tick();
        check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text == "Introducción durante la transición.",
            "Visible active movie survives gameplay loading reset while world speech is discarded");
        video.IsPlaying = false;
        Tick();
        check(!VisibleSubtitleAnnouncer.HasPendingContent, "Stopped movie clears pending caption even during loading");
        LE.UI.LoadingScreen.IsVisible = false;
        Tick();
        check(ScreenReader.Messages.Count == 1, "Ended movie and discarded world speech never replay after loading");

        foreach (string gate in new[] { "owner", "stopped", "cinematic", "index", "holder", "canvas", "label", "alpha", "cull", "collected", "focus" })
        {
            video = Video("Hidden retained movie text");
            switch (gate)
            {
                case "owner": video.isActiveAndEnabled = false; break;
                case "stopped": video.IsPlaying = false; break;
                case "cinematic": video.currentCinematic = null; break;
                case "index": video.currentSubtitleIndex = 0; break;
                case "holder": video.holder.Visible = false; break;
                case "canvas": video.cv.alpha = 0; break;
                case "label": video.subtitleText.isActiveAndEnabled = false; break;
                case "alpha": video.subtitleText.alpha = 0; break;
                case "cull": video.subtitleText.canvasRenderer.cull = true; break;
                case "collected": video.WasCollected = true; break;
                case "focus": Application.isFocused = false; break;
            }
            Tick(); Tick();
            check(ScreenReader.Messages.Count == 0 && !VisibleSubtitleAnnouncer.HasPendingContent,
                "Movie subtitle gate " + gate);
        }

        video = Video("Old pending caption");
        Tick(true); Tick(true);
        video.currentSubtitleIndex++;
        video.subtitleText.text = "Current caption";
        Tick(); Tick();
        check(ScreenReader.Messages.Single().Text == "Current caption", "Deferred old movie caption is replaced before delivery");
        video = Video("Skipped caption");
        Tick(true); video.IsPlaying = false; Tick(); Tick();
        check(ScreenReader.Messages.Count == 0 && !VisibleSubtitleAnnouncer.HasPendingContent,
            "Skipping a movie drops its pending subtitle");
        foreach (string gate in new[] { "dialogue", "modal", "priority" })
        {
            video = Video("Movie waits for priority");
            DialogueAnnouncer.HasActiveContent = gate == "dialogue";
            ScreenContentAnnouncer.HasActiveContent = gate == "modal";
            Tick(gate == "priority"); Tick(gate == "priority");
            check(ScreenReader.Messages.Count == 0, "Movie respects priority " + gate);
        }
        video = Video("Reset pending caption");
        Tick(true); VisibleSubtitleAnnouncer.Reset(); video.IsPlaying = false; Tick();
        check(ScreenReader.Messages.Count == 0, "Explicit lifecycle reset drops movie pending content");
        CharacterSelectionVideoManager.instance = null;
    }
}
