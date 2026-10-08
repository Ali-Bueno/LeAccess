using LeAccess.Accessibility;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;
using LE.UI.PanelSystem;

int checks = 0;
var failures = new List<string>();
void Check(bool condition, string message) { checks++; if (!condition) failures.Add(message); }
TMP_Text Label(GameObject root, string text) => root.Child().Add(new TMP_Text { text = text });
CharacterSelect Reset()
{
    CharacterNameAccessibility.Reset();
    CharacterSelect.instance = null; LocalCharacterSlots.instance = null;
    ScreenKeyboardManager.Instance = null;
    ScreenReader.Messages.Clear(); ScreenReader.Available = true; ScreenReader.SpokeThisFrame = false;
    Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
    ScreenContentAnnouncer.HasActiveContent = DialogueAnnouncer.HasActiveContent = ControllerMenuAccessibility.HasActiveContent = false;
    LeAccess.Plugin.Log.Warnings.Clear();
    var owner = new GameObject().Add(new CharacterSelect());
    owner.newCharacterNameInputField = owner.gameObject.Child().Add(new TMP_InputField());
    owner.newCharacterError = Label(owner.gameObject, "Nombre inválido");
    CharacterSelect.instance = owner;
    NavigationManager.currentlySelected = owner.gameObject.Child().Add(new PrioritizedNavigable());
    return owner;
}
void Tick(bool blocked = false)
{ ScreenReader.SpokeThisFrame = blocked; CharacterNameAccessibility.Update(); }
void Settle() { Tick(); Tick(); }

var form = Reset(); Tick();
Check(ScreenReader.Messages.Count == 0 && CharacterNameAccessibility.HasPendingContent, "Error waits for stable text");
Tick();
Check(ScreenReader.Messages.Single().Text == "Nombre inválido" && ScreenReader.Messages[0].Interrupt,
    "Visible owned direct error speaks localized text with interruption");
Settle(); Check(ScreenReader.Messages.Count == 1 && !CharacterNameAccessibility.HasPendingContent, "Unchanged error never repeats");
form.newCharacterError.text = ""; Settle();
form.newCharacterError.text = "Nombre inválido"; Settle();
Check(ScreenReader.Messages.Count == 2, "Correcting then reproducing the same error reannounces it");

form = Reset();
var validator = form.gameObject.Child().Add(new CharacterNameInputValidator
{ input = form.newCharacterNameInputField, errorUI = Label(form.gameObject, "<b>Nombre ocupado</b>") });
Settle();
Check(ScreenReader.Messages.Single().Text == "Nombre inválido. Nombre ocupado", "Matching input validator adds cleaned visible error");
form = Reset();
form.gameObject.Child().Add(new CharacterNameInputValidator
{ input = form.newCharacterNameInputField, errorUI = Label(form.gameObject, "Nombre inválido") });
Settle(); Check(ScreenReader.Messages.Single().Text == "Nombre inválido", "Direct and validator duplicate error is spoken once");
form = Reset(); form.newCharacterError.text = "";
form.gameObject.Child().Add(new CharacterNameInputValidator
{ input = form.gameObject.Child().Add(new TMP_InputField()), errorUI = Label(form.gameObject, "Foreign sibling error") });
Settle(); Check(ScreenReader.Messages.Count == 0, "Validator for another input cannot contribute sibling error");

foreach (string mode in new[] { "hidden", "alpha", "cull", "disabled", "collected", "group", "canvas" })
{
    form = Reset(); var error = form.newCharacterError;
    if (mode == "hidden") error.gameObject.Active = false;
    if (mode == "alpha") error.alpha = 0;
    if (mode == "cull") error.canvasRenderer.cull = true;
    if (mode == "disabled") error.Enabled = false;
    if (mode == "collected") error.WasCollected = true;
    if (mode == "group") error.gameObject.Add(new CanvasGroup { alpha = 0 });
    if (mode == "canvas") error.gameObject.Add(new Canvas { Enabled = false });
    Settle(); Check(ScreenReader.Messages.Count == 0, $"{mode} error label is not spoken");
}
foreach (string mode in new[] { "owner disabled", "owner collected", "input hidden", "input collected", "validator disabled", "validator collected" })
{
    form = Reset();
    if (mode.StartsWith("validator"))
    {
        form.newCharacterError.text = "";
        validator = form.gameObject.Child().Add(new CharacterNameInputValidator
        { input = form.newCharacterNameInputField, errorUI = Label(form.gameObject, "Validator-only error") });
        if (mode == "validator disabled") validator.Enabled = false;
        if (mode == "validator collected") validator.WasCollected = true;
    }
    if (mode == "owner disabled") form.Enabled = false;
    if (mode == "owner collected") form.WasCollected = true;
    if (mode == "input hidden") form.newCharacterNameInputField.gameObject.Active = false;
    if (mode == "input collected") form.newCharacterNameInputField.WasCollected = true;
    Settle(); Check(ScreenReader.Messages.Count == 0, $"{mode} cannot own error speech");
}
foreach (string gate in new[] { "focus", "loading", "popup", "dialogue", "radial", "foreign navigation", "pause", "pause selectable", "foreign keyboard" })
{
    form = Reset(); Tick();
    if (gate == "focus") Application.isFocused = false;
    if (gate == "loading") LE.UI.LoadingScreen.IsVisible = true;
    if (gate == "popup") ScreenContentAnnouncer.HasActiveContent = true;
    if (gate == "dialogue") DialogueAnnouncer.HasActiveContent = true;
    if (gate == "radial") ControllerMenuAccessibility.HasActiveContent = true;
    if (gate == "foreign navigation") NavigationManager.currentlySelected = new GameObject().Add(new PrioritizedNavigable());
    if (gate == "pause") NavigationManager.currentlySelected = form.gameObject.Child().Add(new MainMenuPanel());
    if (gate == "pause selectable")
    {
        var pause = form.gameObject.Child().Add(new MainMenuPanel());
        ((PrioritizedNavigable)NavigationManager.currentlySelected).ActiveSelectable = pause.gameObject.Child().Add(new UnityEngine.UI.Selectable());
    }
    if (gate == "foreign keyboard") ScreenKeyboardManager.Instance = new GameObject().Add(new ScreenKeyboardManager
    { _enabled = true, _selectedGameObject = new GameObject() });
    Settle(); Check(ScreenReader.Messages.Count == 0 && !CharacterNameAccessibility.HasPendingContent,
        $"{gate} discards pending form error speech");
}
form = Reset();
ScreenKeyboardManager.Instance = new GameObject().Add(new ScreenKeyboardManager
{ _enabled = true, _selectedGameObject = form.newCharacterNameInputField.gameObject.Child() });
NavigationManager.currentlySelected = null; Settle();
Check(ScreenReader.Messages.Count == 1, "Native keyboard target belonging to the input retains name form ownership");
form = Reset();
var slots = new GameObject().Add(new LocalCharacterSlots());
slots.inputTextTMP = slots.gameObject.Child().Add(new TMP_InputField());
slots.errorTextTMP = Label(slots.gameObject, "Error de personaje local");
LocalCharacterSlots.instance = slots; NavigationManager.currentlySelected = slots;
Settle(); Check(ScreenReader.Messages.Single().Text == "Error de personaje local", "Local slot form owns its actual error and input");
form = Reset(); Tick(true); Tick(true);
Check(ScreenReader.Messages.Count == 0 && CharacterNameAccessibility.HasPendingContent, "Other speech preserves pending error");
Tick(); Check(ScreenReader.Messages.Count == 1, "Pending error is delivered after other speech");
foreach (string close in new[] { "focus loss", "scene close", "reset" })
{
    form = Reset(); Settle();
    if (close == "focus loss") { Application.isFocused = false; Tick(); Application.isFocused = true; }
    if (close == "scene close") { form.gameObject.Active = false; Tick(); form.gameObject.Active = true; }
    if (close == "reset") CharacterNameAccessibility.Reset();
    Settle(); Check(ScreenReader.Messages.Count == 2, $"{close} clears retained errors so reopening reannounces");
}
form = Reset(); form.newCharacterError.text = ""; Settle();
form.gameObject.Active = false; Tick();
validator = form.gameObject.Child().Add(new CharacterNameInputValidator
{ input = form.newCharacterNameInputField, errorUI = Label(form.gameObject, "Validator refreshed on reopen") });
form.gameObject.Active = true; Settle();
Check(ScreenReader.Messages.Single().Text == "Validator refreshed on reopen", "Reopening rebuilds the bounded validator cache");
form = Reset(); Tick(); form.newCharacterError.text = "Segundo error"; Tick(); Tick();
Check(ScreenReader.Messages.Single().Text == "Segundo error", "Rapid error changes discard the unsettled previous error");
if (failures.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
checks += KeyboardTests.Run();
checks += TypingTests.Run();
Console.WriteLine($"Character name tests passed: {checks} checks.");
