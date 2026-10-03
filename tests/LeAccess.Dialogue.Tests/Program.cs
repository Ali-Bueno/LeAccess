using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        try
        {
            ReadingAndLifecycle();
            ConsecutiveIdenticalNativeSubtitles();
            VisibilityAndPriority();
            ResponseOwnership();
            NativeUiDiscovery();
            NativeTypewriter();
            InspectionReminder();
            SubtitleReminderTransitions();
            ReminderTypewriter();
            RejectedOutputRetriesCurrentTurn();
            Console.WriteLine($"Dialogue tests passed: {_checks} checks.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void ReadingAndLifecycle()
    {
        var ui = Create();
        var subtitle = Show(ui, "¡Bienvenido, viajero!");
        Check(DialogueAnnouncer.HasPendingContent, "Showing a subtitle reserves its pending readout.");
        DialogueAnnouncer.Update();
        Check(ScreenReader.Messages.Count == 0, "The opening hook does not race same-frame layout.");
        Tick();
        Check(ScreenReader.Messages.Count == 0, "One observation waits for coherent text.");
        Tick();
        Check(ScreenReader.Messages.Single() == ("Erza: ¡Bienvenido, viajero!", true),
            "The actual localized speaker and full line are read together.");
        Tick();
        DialogueAnnouncer.ShowSubtitle(ui, subtitle);
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Repeated Show and unchanged display do not repeat the line.");

        var next = Show(ui, "Encuentra los fragmentos.", "En");
        Tick();
        ui.currentSubtitleControls.line.text = "Encuentra";
        Tick();
        Check(ScreenReader.Messages.Last().Text == "Erza: Encuentra los fragmentos.",
            "The current subtitle supplies the complete line while the typewriter reveals prefixes.");
        ui.currentSubtitleControls.line.text = "Encuentra los fragmentos.";
        Tick();
        Check(ScreenReader.Messages.Count == 2, "Revealing the remainder does not restart a spoken subtitle.");
        DialogueAnnouncer.HideSubtitle(ui, subtitle);
        Check(DialogueAnnouncer.HasActiveContent, "A stale hide for the previous subtitle cannot remove the new one.");
        DialogueAnnouncer.HideSubtitle(ui, next);
        Check(!DialogueAnnouncer.HasActiveContent, "Hiding the current subtitle releases its content.");
        Show(ui, "Encuentra los fragmentos."); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 3, "A genuinely reopened subtitle may be read again.");
        DialogueAnnouncer.Reset();
        Tick();
        Check(!DialogueAnnouncer.HasActiveContent && !DialogueAnnouncer.HasPendingContent,
            "Ending a conversation clears both ownership and pending content.");
    }

    private static void VisibilityAndPriority()
    {
        var ui = Create();
        Show(ui, "Una conversación.");
        ScreenContentAnnouncer.HasActiveContent = true;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0 && DialogueAnnouncer.HasPendingContent,
            "An active tutorial or modal holds dialogue until the foreground content is dismissed.");
        ScreenContentAnnouncer.HasActiveContent = false;
        Tick();
        Check(ScreenReader.Messages.Count == 1, "Current visible dialogue resumes when the popup leaves.");
        Show(ui, "No leas esta línea oculta.");
        ui.currentSubtitleControls.line.alpha = 0;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1 && !DialogueAnnouncer.HasActiveContent,
            "Transparent subtitle text cannot be read or own focus.");
        ui.currentSubtitleControls.line.alpha = 1;
        ui.gameObject.Visible = false;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Hidden ancestors suppress the subtitle.");
        ui.gameObject.Visible = true;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2, "A subtitle becoming visible is announced once.");
        LE.UI.LoadingScreen.IsVisible = true;
        Tick();
        LE.UI.LoadingScreen.IsVisible = false;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && !DialogueAnnouncer.HasActiveContent,
            "Loading discards old dialogue instead of replaying it in the next scene.");
        Show(ui, "No accedas al objeto recogido.");
        ui.WasCollected = true;
        Tick();
        Check(!DialogueAnnouncer.HasActiveContent, "Collected UI wrappers are rejected before native null operators.");
    }

    private static void ConsecutiveIdenticalNativeSubtitles()
    {
        var ui = Create();
        ui.dialogue.npcSubtitle = ui.currentSubtitleControls;
        var first = new Subtitle { formattedText = new FormattedText { text = "Sí." } };
        var manager = new GameObject().Add(new DialogueSystemController { currentDialogueUI = ui,
            conversationController = new ConversationController { state = new ConversationState { subtitle = first } } });
        PixelCrushers.DialogueSystem.DialogueManager.instance = manager;
        ui.currentSubtitleControls.line.text = "Sí.";
        DialogueAnnouncer.ShowSubtitle(ui, first); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "The first native occurrence speaks once.");
        var next = new Subtitle { formattedText = new FormattedText { text = "Sí." } };
        manager.conversationController.state.subtitle = next;
        DialogueAnnouncer.ShowSubtitle(ui, next); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Erza: Sí.",
            "A new native subtitle identity can repeat identical speaker/text without an intervening hide.");
        DialogueAnnouncer.ShowSubtitle(ui, next); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2, "Duplicate hooks for the same native occurrence remain quiet.");
        ScreenContentAnnouncer.HasActiveContent = true;
        var deferred = new Subtitle { formattedText = new FormattedText { text = "Sí." } };
        manager.conversationController.state.subtitle = deferred;
        DialogueAnnouncer.ShowSubtitle(ui, deferred); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && DialogueAnnouncer.HasPendingContent,
            "An identical new occurrence stays pending behind a visible modal.");
        manager.conversationController.state.subtitle = new Subtitle { formattedText = new FormattedText { text = "Ahora." } };
        ui.currentSubtitleControls.line.text = "Ahora.";
        DialogueAnnouncer.ShowSubtitle(ui, manager.conversationController.state.subtitle);
        Tick(); Tick(); ScreenContentAnnouncer.HasActiveContent = false; Tick();
        Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text == "Erza: Ahora.",
            "Only the latest native subtitle survives a deferred repeated occurrence.");
        PixelCrushers.DialogueSystem.DialogueManager.instance = null; DialogueAnnouncer.Reset();
    }

    private static void ResponseOwnership()
    {
        var ui = Create();
        var responseRoot = ui.gameObject.Child();
        ui.dialogue.responseMenu = new TextMeshProResponseMenuControls { panel = responseRoot.Add(new Graphic()) };
        var first = responseRoot.Child(); first.Add(new TMP_Text { text = "Aceptar" });
        var second = responseRoot.Child(); second.Add(new TMP_Text { text = "Rechazar" });
        DialogueAnnouncer.ShowResponses(ui);
        Tick(); Tick();
        Check(DialogueAnnouncer.HasActiveContent && !DialogueAnnouncer.HasPendingContent,
            "The response menu owns dialogue focus without reserving another body announcement.");
        Check(DialogueAnnouncer.OwnsFocus(first) && DialogueAnnouncer.OwnsFocus(second),
            "The existing focus reader can read either selected response.");
        Check(!DialogueAnnouncer.OwnsFocus(new GameObject()), "Background controls do not belong to the conversation.");
        Check(ScreenReader.Messages.Count == 0, "The dialogue reader never enumerates response choices.");
        DialogueAnnouncer.HideResponses(ui);
        Check(!DialogueAnnouncer.HasActiveContent, "Closing responses releases dialogue ownership.");
    }

    private static TextMeshProDialogueUI Create()
    {
        PixelCrushers.DialogueSystem.DialogueManager.instance = null;
        DialogueAnnouncer.Reset();
        ScreenReader.Messages.Clear();
        ScreenContentAnnouncer.HasActiveContent = false;
        LE.UI.LoadingScreen.IsVisible = false;
        Application.isFocused = true;
        var root = new GameObject();
        var ui = root.Add(new TextMeshProDialogueUI());
        ui.dialogue = new TextMeshProDialogueControls { panel = root.Add(new Graphic()) };
        ui.currentSubtitleControls = new TextMeshProSubtitleControls
        {
            panel = root.Add(new Graphic()),
            portraitName = root.Child().Add(new TMP_Text { text = "Erza" }),
            line = root.Child().Add(new TMP_Text())
        };
        return ui;
    }

    private static void NativeUiDiscovery()
    {
        var ui=Create();ui.currentSubtitleControls.line.text="Sigue el camino ancestral.";
        ui.currentSubtitleControls.panel=null;
        ui.dialogue.npcSubtitle=ui.currentSubtitleControls;
        var manager=new GameObject().Add(new DialogueSystemController{
            currentDialogueUI=ui,conversationController=new ConversationController()});
        PixelCrushers.DialogueSystem.DialogueManager.instance=manager;
        Tick();Check(ScreenReader.Messages.Count==0&&DialogueAnnouncer.HasPendingContent,"Native displayed UI discovered without subtitle hook.");
        Tick();Check(ScreenReader.Messages.Single().Text=="Erza: Sigue el camino ancestral.","Owned installed-style null subtitle panel read.");
        Tick();Check(ScreenReader.Messages.Count==1,"Native polling deduplicates line.");
        var retained=new Subtitle{formattedText=new FormattedText{text="Sigue el camino ancestral."}};
        DialogueAnnouncer.ShowSubtitle(ui,retained);Tick();
        DialogueAnnouncer.HideSubtitle(ui,retained);Tick();Tick();
        Check(ScreenReader.Messages.Count==1&&DialogueAnnouncer.HasActiveContent,"Native HideSubtitle preserves still-rendered NPC statement without rereading.");
        var responseRoot=ui.gameObject.Child();
        ui.dialogue.responseMenu=new TextMeshProResponseMenuControls{panel=responseRoot.Add(new Graphic())};
        var choice=responseRoot.Child();Tick();
        Check(DialogueAnnouncer.OwnsFocus(choice),"Discovered response menu owns current selectable before generic focus.");
        ui.currentSubtitleControls=null;Tick();Tick();
        Check(ScreenReader.Messages.Count==1,"Visible NPC text with responses persists without current-control pointer.");
        ui.dialogue.npcSubtitle.line.text="Encuentra a Grae.";Tick();Tick();
        Check(ScreenReader.Messages.Last().Text=="Erza: Encuentra a Grae.","New visible native NPC text read.");
        ui.dialogue.panel.gameObject.Visible=false;Tick();Tick();
        responseRoot.Visible=false;
        Check(!DialogueAnnouncer.HasActiveContent&&!DialogueAnnouncer.HasPendingContent,"Hidden owning conversation panel never supplies stale label.");
        ui.dialogue.panel.gameObject.Visible=true;manager.conversationController.IsActive=false;Tick();
        Check(!DialogueAnnouncer.HasActiveContent,"Native end clears visible old text.");
        manager.conversationController.IsActive=true;ui.dialogue.npcSubtitle.line.text="Otra conversación.";Tick();Tick();
        Check(ScreenReader.Messages.Last().Text=="Erza: Otra conversación.","New native conversation rearmed.");
        manager.currentDialogueUI=new GameObject().Add(new NativeDialogueUI());Tick();
        Check(!DialogueAnnouncer.HasActiveContent,"Different native UI never borrows previous text.");
        manager.currentDialogueUI=ui;manager.conversationController.WasCollected=true;Tick();
        Check(!DialogueAnnouncer.HasActiveContent,"Collected native conversation rejected.");
        manager.conversationController.WasCollected=false;ui.dialogue.npcSubtitle.line.text="Now";Tick();
        manager.currentDialogueUI=null;Tick();Tick();
        Check(ScreenReader.Messages.Last().Text!="Erza: Now"&&!DialogueAnnouncer.HasPendingContent,"Deferred old UI discarded on native switch.");
        PixelCrushers.DialogueSystem.DialogueManager.instance=null;DialogueAnnouncer.Reset();
    }

    private static void NativeTypewriter()
    {
        var ui=Create();ui.dialogue.npcSubtitle=ui.currentSubtitleControls;
        var nativeSubtitle=new Subtitle{formattedText=new FormattedText{text="Ve hacia el bosque."}};
        var manager=new GameObject().Add(new DialogueSystemController{currentDialogueUI=ui,
            conversationController=new ConversationController{state=new ConversationState{subtitle=nativeSubtitle}}});
        PixelCrushers.DialogueSystem.DialogueManager.instance=manager;
        ui.currentSubtitleControls.line.text="Ve";Tick();Tick();
        Check(ScreenReader.Messages.Single().Text=="Erza: Ve hacia el bosque.","Native discovered typewriter reads localized full line once.");
        ui.currentSubtitleControls.line.text="Ve hacia";Tick();Tick();
        ui.currentSubtitleControls.line.text="Ve hacia el bosque.";Tick();Tick();
        Check(ScreenReader.Messages.Count==1,"Stable partial prefixes never interrupt repeatedly.");
        manager.conversationController.state.subtitle=new Subtitle{formattedText=new FormattedText{text="Ve hacia el puente."}};
        ui.currentSubtitleControls.line.text="Ve";Tick();Tick();
        Check(ScreenReader.Messages.Count==2&&ScreenReader.Messages.Last().Text=="Erza: Ve hacia el puente.","Consecutive same-speaker shared prefixes use current native subtitle identity.");
        manager.conversationController.state.subtitle=new Subtitle{formattedText=new FormattedText{text="Go toward the road."}};
        ui.currentSubtitleControls.line.text="Sigue el camino.";Tick();Tick();
        Check(ScreenReader.Messages.Last().Text=="Erza: Sigue el camino.","Raw untranslated model text never replaces rendered language.");
        ui.currentSubtitleControls=null;manager.conversationController.state.subtitle=new Subtitle{formattedText=new FormattedText{text="Sigue el camino. Player response"}};
        Tick();Tick();
        Check(ScreenReader.Messages.Last().Text=="Erza: Sigue el camino.","Retained NPC label not paired with a later response state.");
        PixelCrushers.DialogueSystem.DialogueManager.instance=null;DialogueAnnouncer.Reset();
    }

    private static Subtitle Show(TextMeshProDialogueUI ui, string fullLine, string displayed = null)
    {
        ui.currentSubtitleControls.line.text = displayed ?? fullLine;
        var subtitle = new Subtitle { formattedText = new FormattedText { text = fullLine } };
        DialogueAnnouncer.ShowSubtitle(ui, subtitle);
        return subtitle;
    }

    private static void InspectionReminder()
    {
        var ui = Create();
        ui.dialogue.npcSubtitle = ui.currentSubtitleControls;
        ui.currentSubtitleControls.line.gameObject.Active = false;
        ui.currentSubtitleControls.portraitName.text = "Cadáveres ardientes";
        var reminderRoot = ui.gameObject.Child();
        var reminder = new TextMeshProSubtitleControls
        {
            line = reminderRoot.Add(new TMP_Text { text = "Los cuerpos están quemados. Estaban huyendo." }),
            portraitName = ui.currentSubtitleControls.portraitName
        };
        var responses = new TextMeshProResponseMenuControls { subtitleReminder = reminder };
        ui.dialogue.responseMenu = responses;
        var choice = ui.gameObject.Child();
        choice.Add(new TMP_Text { text = "..." });
        var manager = new GameObject().Add(new DialogueSystemController
        {
            currentDialogueUI = ui, conversationController = new ConversationController()
        });
        PixelCrushers.DialogueSystem.DialogueManager.instance = manager;
        Tick();
        Check(DialogueAnnouncer.HasPendingContent && ScreenReader.Messages.Count == 0,
            "Installed null-panel reminder reserves its inspection body before focus is read");
        Check(DialogueAnnouncer.HasActiveContent && DialogueAnnouncer.OwnsFocus(choice),
            "Null response panel uses its owned outer dialogue panel for response focus");
        Tick();
        Check(ScreenReader.Messages.Single() == ("Cadáveres ardientes: Los cuerpos están quemados. Estaban huyendo.", true),
            "Burning corpses-style examination reads the displayed reminder, never its ellipsis response");
        DialogueAnnouncer.ShowResponses(ui); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Repeated response callbacks and native polling deduplicate the reminder");

        reminder.line.text = "Una descripción nueva.";
        reminder.line.alpha = 0; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1 && !DialogueAnnouncer.HasPendingContent,
            "Transparent reminder cannot supply inspection speech");
        reminder.line.alpha = 1; reminder.line.canvasRenderer.cull = true; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Culled reminder remains silent");
        reminder.line.canvasRenderer.cull = false; reminderRoot.Visible = false; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Hidden reminder hierarchy remains silent");
        reminderRoot.Visible = true; ui.dialogue.panel.gameObject.Visible = false; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1 && !DialogueAnnouncer.HasActiveContent,
            "Hidden outer panel suppresses the installed null-panel reminder and response ownership");
        ui.dialogue.panel.gameObject.Visible = true; responses.isVisible = false; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1 && !DialogueAnnouncer.HasActiveContent,
            "A leftover active label cannot speak after its native response menu closes");
        responses.isVisible = true; Tick(); Tick();
        Check(ScreenReader.Messages.Last().Text == "Cadáveres ardientes: Una descripción nueva.",
            "Reopening the visible inspection reads its current description");
        reminder.line.text = "No debe quedar en cola."; Tick();
        Check(DialogueAnnouncer.HasPendingContent, "Changed inspection text can wait for its settled observation");
        responses.isVisible = false; DialogueAnnouncer.HideResponses(ui); Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && !DialogueAnnouncer.HasPendingContent,
            "Closing responses discards pending reminder speech");
        responses.isVisible = true; reminder.WasCollected = true; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2, "Collected reminder controls are not read");
        reminder.WasCollected = false; reminder.line.text = "No leas una conversación finalizada."; Tick();
        manager.conversationController.IsActive = false; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && !DialogueAnnouncer.HasActiveContent,
            "Conversation end discards a still-visible reminder and pending body");
        PixelCrushers.DialogueSystem.DialogueManager.instance = null;

        ui = Create(); ui.currentSubtitleControls.line.gameObject.Active = false;
        ui.dialogue.responseMenu = new TextMeshProResponseMenuControls
        {
            subtitleReminder = new TextMeshProSubtitleControls { line = ui.gameObject.Child().Add(new TMP_Text { text = "Examinar." }) }
        };
        DialogueAnnouncer.ShowResponses(ui); Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text == "Examinar.", "Response hook can also discover the owned reminder without a subtitle hook");
    }

    private static void SubtitleReminderTransitions()
    {
        var ui = Create(); ui.dialogue.npcSubtitle = ui.currentSubtitleControls;
        const string body = "La descripción visible.";
        ui.currentSubtitleControls.line.text = body;
        var subtitle = new Subtitle { formattedText = new FormattedText { text = body } };
        var manager = new GameObject().Add(new DialogueSystemController
        {
            currentDialogueUI = ui,
            conversationController = new ConversationController { state = new ConversationState { subtitle = subtitle } }
        });
        PixelCrushers.DialogueSystem.DialogueManager.instance = manager;
        Tick(); Tick();
        var reminder = new TextMeshProSubtitleControls
        {
            line = ui.gameObject.Child().Add(new TMP_Text { text = "La" }),
            portraitName = ui.currentSubtitleControls.portraitName
        };
        ui.dialogue.responseMenu = new TextMeshProResponseMenuControls { subtitleReminder = reminder };
        ui.currentSubtitleControls.line.gameObject.Active = false;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Transferring the same NPC body to a typewritten response reminder does not repeat it");
        reminder.line.text = body; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Completing the reminder's typewriter cannot replay the NPC statement");
        ui.dialogue.responseMenu.isVisible = false;
        ui.currentSubtitleControls.line.gameObject.Active = true;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Returning the same native subtitle to ordinary controls does not repeat it");
        PixelCrushers.DialogueSystem.DialogueManager.instance = null;
        DialogueAnnouncer.Reset();
    }

    private static void ReminderTypewriter()
    {
        var ui = Create();
        ui.currentSubtitleControls.line.gameObject.Active = false;
        var reminder = new TextMeshProSubtitleControls
        {
            line = ui.gameObject.Child().Add(new TMP_Text { text = "Los" })
        };
        ui.dialogue.responseMenu = new TextMeshProResponseMenuControls { subtitleReminder = reminder };
        const string full = "Los cuerpos están quemados. Estaban huyendo.";
        var manager = new GameObject().Add(new DialogueSystemController
        {
            currentDialogueUI = ui,
            conversationController = new ConversationController
            { state = new ConversationState { subtitle = new Subtitle { formattedText = new FormattedText { text = full } } } }
        });
        PixelCrushers.DialogueSystem.DialogueManager.instance = manager;
        Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text == full, "Reminder-only discovery reads the full localized native typewriter line");
        reminder.line.text = "Los cuerpos"; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Stable reminder prefixes never interrupt the completed description");
        manager.conversationController.state.subtitle = new Subtitle { formattedText = new FormattedText { text = "Respuesta del jugador." } };
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "A retained typewritten reminder cannot borrow or reset for an unrelated response state");
        reminder.line.text = full; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Finishing retained text remains deduplicated after model state changes");
        PixelCrushers.DialogueSystem.DialogueManager.instance = null;
        DialogueAnnouncer.Reset();
    }

    private static void RejectedOutputRetriesCurrentTurn()
    {
        var ui = Create(); Show(ui, "Actual."); ScreenReader.OutputAccepted = false;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0 && DialogueAnnouncer.HasPendingContent, "Rejected dialogue output remains pending");
        ScreenReader.OutputAccepted = true; Tick();
        Check(ScreenReader.Messages.Single().Text == "Erza: Actual.", "Unchanged owned dialogue retries after acceptance recovers");
        Tick(); Check(ScreenReader.Messages.Count == 1, "Accepted dialogue is acknowledged once");
        ui = Create(); Show(ui, "Anterior."); ScreenReader.OutputAccepted = false; Tick(); Tick();
        Show(ui, "Actual."); ScreenReader.OutputAccepted = true; Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text == "Erza: Actual.", "Rejected dialogue replaced before recovery does not replay");
        ui = Create(); Show(ui, "Oculto."); ScreenReader.OutputAccepted = false; Tick(); Tick();
        ui.gameObject.Active = false; ScreenReader.OutputAccepted = true; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0 && !DialogueAnnouncer.HasPendingContent, "Hidden rejected dialogue expires before recovery");
        DialogueAnnouncer.Reset();
    }

    private static void Tick() { Time.frameCount++; DialogueAnnouncer.Update(); }
    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
