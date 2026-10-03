using LeAccess.Accessibility;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
TMP_Text Label(GameObject root, string text) => root.Child().Add(new TMP_Text { text = text });
void Frame() { Time.frameCount++; ScreenContentAnnouncer.Update(); }

var tracker = new ScreenContentTracker();
Check(tracker.Observe(1, "Title. Body. Confirm. Cancel", true, out var text) && text == null,
    "Opening content waits for a settled second observation");
Check(tracker.Observe(1, "Título. Cuerpo. Confirmar. Cancelar", true, out text) && text == null,
    "Localization change restarts settling");
Check(tracker.Observe(1, "Título. Cuerpo. Confirmar. Cancelar", true, out text) && text.Contains("Cuerpo"),
    "Stable complete localized content is announced");
Check(!tracker.Observe(1, text, true, out _), "Unchanged modal content does not repeat");
tracker.Hide(1);
Check(tracker.Observe(1, "Same popup", true, out text) && text == null, "Reopened popup settles again");
Check(tracker.Observe(1, "Same popup", true, out text) && text == "Same popup", "Explicit reopen permits identical content");
Check(tracker.Observe(1, "New text", false, out text) && text == null, "Unavailable output leaves content pending");
Check(tracker.Observe(1, "Latest text", true, out text) && text == null, "Unspoken stale content is replaced");
Check(tracker.Observe(1, "Latest text", true, out text) && text == "Latest text", "Only latest stable content speaks");
Check(!tracker.Observe(1, null, true, out _), "Missing visible content clears the previous state");
Check(ScreenContentTracker.Combine(new[] { "Title", "Body.", "Confirm", "Title", " ", null }) == "Title. Body. Confirm",
    "Header, body and choices preserve order, deduplicate and keep punctuation");

var popup = new GameObject();
var header = Label(popup, "Tutorial");
string fullBody = string.Concat(Enumerable.Repeat("This full instruction must remain available. ", 20)).Trim();
var body = Label(popup, fullBody);
Label(popup, "Confirm");
Label(popup, "Cancel");
Label(popup, "Hidden alpha").alpha = 0;
Label(popup, "Disabled label").isActiveAndEnabled = false;
Label(popup, "Culled label").canvasRenderer.cull = true;
Label(popup, "Inactive root").gameObject.Active = false;
Label(popup, "Hidden ancestor").gameObject.Visible = false;
Label(popup, "Collected label").WasCollected = true;
var input = popup.Child();
input.Add(new TMP_InputField());
Label(input, "Typed character name");
var legacy = popup.Child().Add(new UnityEngine.UI.Text { text = "Legacy caption" });
string captured = ScreenContentReader.Read(popup, header, body);
Check(captured.StartsWith("Tutorial. " + fullBody), "Full tutorial body is retained without length truncation");
Check(captured.EndsWith("Confirm. Cancel. Legacy caption"), "Visible choices and legacy text follow tutorial content");
Check(!captured.Contains("Hidden") && !captured.Contains("Disabled") && !captured.Contains("Culled") &&
    !captured.Contains("Inactive") && !captured.Contains("Collected"), "Invisible and collected labels are excluded");
Check(!captured.Contains("Typed character"), "Input values do not cause whole-popup rereading while typing");
Check(captured.IndexOf(fullBody) == captured.LastIndexOf(fullBody), "Explicit body and descendant capture are deduplicated");

ScreenContentAnnouncer.Reset();
ScreenReader.Messages.Clear();
popup.Visible = false;
ScreenContentAnnouncer.Track(popup, header, body);
Frame(); Frame();
Check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 0,
    "Hidden startup template never owns focus or announces");
popup.Visible = true;
Frame();
Check(ScreenContentAnnouncer.HasActiveContent && ScreenContentAnnouncer.HasPendingContent && ScreenReader.Messages.Count == 0,
    "Visible tutorial owns the surface while its content settles");
Frame();
Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text == captured && ScreenReader.Messages[0].Interrupt,
    "Settled tutorial interrupts once with complete visible text");
Check(!ScreenContentAnnouncer.HasPendingContent, "Initial body is no longer pending after delivery");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "Unchanged visible modal does not repeat each frame");
Check(ScreenContentAnnouncer.OwnsFocus(body.gameObject), "Controls inside current root belong to its speech surface");
Check(!ScreenContentAnnouncer.OwnsFocus(new GameObject()), "Background focus does not belong to the modal");
ScreenContentAnnouncer.Track(body.gameObject);
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "Nested hooks do not duplicate the owning tutorial body");

var top = new GameObject();
var topTitle = Label(top, "Important confirmation");
Label(top, "Yes"); Label(top, "No");
ScreenContentAnnouncer.Track(top, topTitle);
ScreenContentAnnouncer.Update();
Check(ScreenReader.Messages.Count == 1 && ScreenContentAnnouncer.HasPendingContent,
    "Opening frame of newer modal cannot reannounce underlying content");
Check(ScreenContentAnnouncer.OwnsFocus(topTitle.gameObject) && !ScreenContentAnnouncer.OwnsFocus(body.gameObject),
    "Only newest visible modal owns focus");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages[1].Text == "Important confirmation. Yes. No",
    "Newest modal reads title before available choices");
ScreenContentAnnouncer.Remove(top);
top.Active = false;
Frame();
Check(ScreenReader.Messages.Count == 2 && ScreenContentAnnouncer.OwnsFocus(body.gameObject),
    "Closing top modal restores ownership without repeating the previous body");
body.text = "Updated tutorial step";
Frame();
Check(ScreenReader.Messages.Count == 2, "Updated step waits for stable content");
Frame();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages[2].Text.Contains("Updated tutorial step"),
    "Updated visible tutorial step announces once");
popup.WasCollected = true;
Frame();
Check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 3,
    "Collected popup owner is forgotten without Unity null access or speech");
ScreenContentAnnouncer.Reset();
var confirmationRoot = new GameObject();
var confirmation = confirmationRoot.Add(new LE.UI.Generic.UnityUI.ConfirmationPanel
{
    HeaderText = Label(confirmationRoot, "Confirmation"),
    BodyText = Label(confirmationRoot, "Full question")
});
var nestedNavigation = confirmationRoot.Child().Add(new Component());
ScreenContentAnnouncer.TrackPopup(nestedNavigation);
Frame(); Frame();
Check(ScreenReader.Messages.Last().Text == "Confirmation. Full question",
    "Nested confirmation navigation reads its owning panel's header and body");
Check(ScreenContentAnnouncer.OwnsFocus(confirmation.HeaderText.gameObject),
    "Normalized owning panel includes header outside the navigation child");
ScreenContentAnnouncer.RemovePopup(nestedNavigation);
Check(!ScreenContentAnnouncer.HasActiveContent, "Nested close hook removes the same normalized panel as its open hook");
ScreenContentAnnouncer.Reset();
ScreenReader.Messages.Clear();
var idol = new GameObject { Active = false };
var idolText = Label(idol, "Hidden idol template");
ScreenContentAnnouncer.Watch(idol);
Frame(); Frame(); Frame();
Check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Count == 0,
    "Awake registration retains an inactive idol tooltip without reading its hidden template");
var priorModal = new GameObject();
var priorAction = Label(priorModal, "Existing modal");
ScreenContentAnnouncer.Track(priorModal);
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "Existing visible modal is announced normally beside hidden watcher");
idolText.text = "Idols. Place an idol in an unlocked slot. Close";
idol.Active = true;
Check(ScreenContentAnnouncer.HasActiveContent && ScreenContentAnnouncer.OwnsFocus(idolText.gameObject) &&
    !ScreenContentAnnouncer.OwnsFocus(priorAction.gameObject) && ScreenContentAnnouncer.HasPendingContent,
    "First visible idol tooltip takes foreground ownership even though Awake ran before the current modal");
Frame();
Check(ScreenReader.Messages.Count == 1, "Visible idol text waits for stable content");
Frame();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == idolText.text,
    "Awake-registered tooltip reads its full actual visible instruction");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 2, "Watched visible tooltip does not repeat");
idol.Active = false;
ScreenContentAnnouncer.Remove(idol);
Frame();
Check(ScreenReader.Messages.Count == 2 && ScreenContentAnnouncer.OwnsFocus(priorAction.gameObject),
    "Explicit close releases watched tooltip ownership without repeating the older modal");
idol.Active = true;
Frame(); Frame(); Frame();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text == idolText.text,
    "Same tooltip reopens and reads once without another Awake or unsafe AttemptTooltip hook");
idol.Visible = false;
Frame();
Check(ScreenContentAnnouncer.OwnsFocus(priorAction.gameObject), "Canvas-hidden watched tooltip releases foreground ownership");
idol.Visible = true;
var laterModal = new GameObject();
var laterAction = Label(laterModal, "Later confirmation");
ScreenContentAnnouncer.Track(laterModal);
Frame(); Frame();
Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Text == "Later confirmation" &&
    ScreenContentAnnouncer.OwnsFocus(laterAction.gameObject),
    "A newer modal remains foreground when watched tooltip becomes visible before its opening hook");
laterModal.Active = false;
ScreenContentAnnouncer.Remove(laterModal);
Frame(); Frame();
Check(ScreenReader.Messages.Count == 5 && ScreenReader.Messages.Last().Text == idolText.text,
    "Canvas-hidden tooltip can reopen and is read when the newer modal closes");
idol.WasCollected = true;
Frame();
Check(ScreenReader.Messages.Count == 5 && ScreenContentAnnouncer.OwnsFocus(priorAction.gameObject),
    "Collected watched tooltip is removed safely and previous modal remains available");
ScreenContentAnnouncer.Reset();
Check(!ScreenContentAnnouncer.HasActiveContent, "Reset clears retained tooltip watches too");
ScreenReader.Messages.Clear();
var termsRoot = new GameObject { Visible = false };
string wholeDocument = string.Join(" ", Enumerable.Range(1, 200).Select(i => $"Apartado {i}. Texto completo de las condiciones."));
var terms = termsRoot.Add(new LE.Tos.TosPanel
{
    Title = "Condiciones del servicio",
    _displayedContent = new LE.Tos.TosContent { Content = wholeDocument }
});
Label(termsRoot,"Solo la porción del documento que cabe en la ventana");
ScreenContentAnnouncer.TrackTerms(terms);
Frame(); Frame();
Check(ScreenReader.Messages.Count == 0 && !ScreenContentAnnouncer.HasActiveContent,
    "Initializing terms data does not announce a hidden panel");
termsRoot.Visible = true;
Frame();
Check(ScreenReader.Messages.Count == 0 && ScreenContentAnnouncer.HasPendingContent,
    "Visible terms wait for the settled displayed document");
Frame();
Check(ScreenReader.Messages.Single() == ("Condiciones del servicio. " + wholeDocument,true),
    "Terms read the full selected localized document, including sections beyond the scroll viewport");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "Scrolling unchanged terms cannot repeat the document");
terms._displayedContent = new LE.Tos.TosContent { Content = "Nuevas condiciones localizadas." };
ScreenContentAnnouncer.TrackTerms(terms);
Frame(); Frame();
Check(ScreenReader.Messages.Last().Text == "Condiciones del servicio. Nuevas condiciones localizadas.",
    "An actual displayed document update refreshes the text");
terms._displayedContent.WasCollected = true;
Frame();
Check(ScreenReader.Messages.Count == 2, "Collected terms content is not accessed or announced");
ScreenContentAnnouncer.Remove(termsRoot);
Check(!ScreenContentAnnouncer.HasActiveContent, "Closing terms releases modal focus ownership");

ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
var hintRoot = new GameObject();
var tutorial = new GameObject().Add(new TutorialManager
{
    tutorialTooltip = hintRoot.transform,
    titleTMP = Label(hintRoot, "Moverse"),
    bodyTMP = Label(hintRoot, "Usa el joystick para moverte.")
});
ScreenContentAnnouncer.TrackTutorial(tutorial);
Frame(); Frame();
Check(ScreenReader.Messages.Single().Text == "Moverse. Usa el joystick para moverte.",
    "A passive gameplay tutorial still reads its full localized instruction");
Check(!ScreenContentAnnouncer.HasActiveContent && !ScreenContentAnnouncer.OwnsFocus(hintRoot),
    "A tutorial callout cannot claim modal ownership and silence controller/combat/world feedback");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "An unchanged passive callout does not repeat");
var blocking = new GameObject(); var blockingTitle = Label(blocking, "Confirmar salida");
ScreenContentAnnouncer.Track(blocking, blockingTitle); Frame(); Frame();
ScreenContentAnnouncer.RemoveTutorial(tutorial);
tutorial.bodyTMP.text = "Instrucción nueva.";
ScreenContentAnnouncer.TrackTutorial(tutorial); Frame(); Frame();
Check(ScreenContentAnnouncer.HasActiveContent && ScreenContentAnnouncer.OwnsFocus(blockingTitle.gameObject),
    "A newer passive tutorial cannot take ownership from a blocking confirmation");
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Confirmar salida",
    "A tutorial arriving behind a modal waits instead of interrupting its choices");
ScreenContentAnnouncer.Remove(blocking); blocking.Active = false; Frame(); Frame();
Check(!ScreenContentAnnouncer.HasActiveContent && ScreenReader.Messages.Last().Text == "Moverse. Instrucción nueva.",
    "Pending tutorial is delivered after the modal closes without blocking gameplay");
ScreenContentAnnouncer.RemoveTutorial(tutorial);
Frame(); Frame();
Check(!ScreenContentAnnouncer.HasPendingContent && ScreenReader.Messages.Count == 3,
    "Closing a tutorial clears its pending state without duplicating speech");
ScreenContentAnnouncer.Reset(); ScreenReader.Messages.Clear();
LeAccess.I18n.ModText.SetLanguage("es");
tutorial.titleTMP.text = "Pociones de vida";
tutorial.bodyTMP.text = "Usa pociones para recuperar vida.";
tutorial._tutorialNotice = new TutorialNotice { textLocalizationKey = "Tutorials_HealthPotions_Text" };
Rewired.ReInput.mapping.Actions[8] = new Rewired.InputAction { name = "UsePotion" };
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["UsePotion"] = "LB";
ScreenContentAnnouncer.TrackTutorial(tutorial); Frame(); Frame();
Check(ScreenReader.Messages.Single().Text.EndsWith("Botón: LB"),
    "Installed potion notice receives its actual action binding even when body omits a button");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "Stable tutorial binding is not repeated");
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["UsePotion"] = "RT";
Frame(); Frame();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.EndsWith("Botón: RT"),
    "Remapping while a tutorial remains visible refreshes its current instruction");
tutorial._tutorialNotice.textLocalizationKey = "Tutorials_Evade_Text";
tutorial.titleTMP.text = "Evasión";
Rewired.ReInput.mapping.Actions[192] = new Rewired.InputAction { name = "Evade" };
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["Evade"] = "B";
Frame(); Frame();
Check(ScreenReader.Messages.Last().Text.EndsWith("Botón: B"), "Evade uses its own action rather than last tutorial binding");
tutorial._tutorialNotice.textLocalizationKey = "Tutorials_NewPassivePoint_Text";
EpochInputManager.IsControllerActive = true;
Rewired.ReInput.mapping.Actions[61] = new Rewired.InputAction { name = "Radial" };
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["Radial"] = "Mantener Back";
Frame(); Frame();
Check(ScreenReader.Messages.Last().Text.EndsWith("Abrir menú del mando: Mantener Back"),
    "A menu tutorial without a direct controller binding supplies actual radial opening control");
EpochInputManager.IsControllerActive = false;
Frame(); Frame();
Check(!ScreenReader.Messages.Last().Text.Contains("Back"), "Keyboard input never receives controller radial fallback");
tutorial._tutorialNotice.textLocalizationKey = "Unknown_Tutorial";
var closePrompt = hintRoot.Child().Add(new MultiButtonsActionPrompt { Text = "Continuar: A" });
Frame(); Frame();
Check(ScreenReader.Messages.Last().Text.EndsWith("Continuar: A"), "Visible native tutorial prompts supply their own action and control");
closePrompt.Active = false; Frame(); Frame();
Check(!ScreenReader.Messages.Last().Text.Contains("Continuar"), "Inactive tutorial prompt cannot leave stale button instructions");
closePrompt.Active = true; closePrompt.gameObject.Visible = false; Frame(); Frame();
Check(!ScreenReader.Messages.Last().Text.Contains("Continuar"), "Hidden tutorial prompts are not read");
int prior = ScreenReader.Messages.Count;
hintRoot.Visible = false; Frame(); Frame();
Check(ScreenReader.Messages.Count == prior && TutorialTextReader.Read(tutorial) is null,
    "Hidden tutorial cannot emit controls or obsolete content");
ReadingOrderTests.Run(Check);
Console.WriteLine($"Screen content tests passed: {checks} checks.");
