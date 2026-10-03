using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class DialoguePriorityTests
{
    internal static TextMeshProDialogueUI Open()
    {
        var root = new GameObject();
        var ui = root.Add(new TextMeshProDialogueUI());
        ui.dialogue = new TextMeshProDialogueControls { panel = root.Add(new Graphic()) };
        ui.currentSubtitleControls = new TextMeshProSubtitleControls
        {
            panel = root.Add(new Graphic()),
            portraitName = root.Child().Add(new TMP_Text { text = "Erza" }),
            line = root.Child().Add(new TMP_Text { text = "Escucha." })
        };
        DialogueAnnouncer.ShowSubtitle(ui, new Subtitle { formattedText = new FormattedText { text = "Escucha." } });
        return ui;
    }

    internal static void Run(Action<bool, string> check)
    {
        void Reset()
        {
            ScreenContentAnnouncer.Reset(); DialogueAnnouncer.Reset();
            ScreenReader.Messages.Clear(); ScreenReader.SpokeThisFrame = false;
            Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
            DialogueManager.instance = null; NavigationManager.currentlySelected = null;
        }
        void Frame()
        {
            Time.frameCount++;
            ScreenReader.SpokeThisFrame = false;
            // Same service order as MenuAccessibility.LateUpdate.
            ScreenContentAnnouncer.Update(); DialogueAnnouncer.Update();
        }
        Reset();
        var ui = Open();
        var tooltip = new GameObject();
        var tutorial = new GameObject().Add(new TutorialManager
        {
            tutorialTooltip = tooltip.transform,
            titleTMP = tooltip.Child().Add(new TMP_Text { text = "Moverse" }),
            bodyTMP = tooltip.Child().Add(new TMP_Text { text = "Primera instrucción." })
        });
        ScreenContentAnnouncer.TrackTutorial(tutorial);
        check(!ScreenContentAnnouncer.HasActiveContent && DialogueAnnouncer.HasActiveContent,
            "Actual foreground selection excludes passive tutorials while the actual dialogue owns its rendered surface");
        Frame(); Frame(); Frame();
        check(ScreenReader.Messages.Single() == ("Erza: Escucha.", true),
            "Actual dialogue progresses while the passive tutorial defers; the services cannot circularly block each other");
        check(ScreenContentAnnouncer.HasPendingContent && !DialogueAnnouncer.HasPendingContent,
            "Only the tutorial remains pending after the dialogue body is delivered");
        tutorial.bodyTMP.text = "Instrucción actual.";
        Frame(); Frame();
        check(ScreenReader.Messages.Count == 1, "Deferred tutorial changes cannot interrupt the actual active dialogue");
        ui.gameObject.Active = false;
        Frame(); Frame();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Moverse. Instrucción actual.",
            "The latest tutorial is delivered after the actual dialogue releases its rendered surface");

        Reset(); Open();
        var modal = new GameObject();
        var title = modal.Child().Add(new TMP_Text { text = "Confirmar salida" });
        ScreenContentAnnouncer.Track(modal, title);
        Frame(); Frame(); Frame();
        check(ScreenReader.Messages.Single().Text == "Confirmar salida" && DialogueAnnouncer.HasPendingContent &&
            ScreenContentAnnouncer.HasActiveContent,
            "A real blocking modal keeps priority and defers the actual dialogue service");
        modal.Active = false;
        Frame(); Frame();
        check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Erza: Escucha.",
            "Dialogue progresses when the actual blocking modal releases ownership");
        Reset();
    }
}
