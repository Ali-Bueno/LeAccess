using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using ButtonList = Il2CppSystem.Collections.Generic.List<ButtonInfo>;

int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
void Equal(string expected, string actual, string message) => Check(expected == actual, message + $": expected '{expected}', got '{actual}'");
ButtonInfo Button(string name, bool hold = false) => new() { Name = name, IsHoldAction = hold };
ModText.Initialize(null, _ => { }); ModText.SetLanguage("en");
var provider = new GameObject().Add(new PromptsInfoProvider()); PromptsInfoProvider.Instance = provider;
provider.Mappings[("Interact", false)] = new(Button("A"));
Equal("A", InputBindingText.ReadAction("Interact"), "native mapped controller button name");
provider.Mappings[("Interact", false)] = new(Button("Right Shoulder"));
Equal("Right Shoulder", InputBindingText.ReadAction("Interact"), "same action uses current native remapping");
provider.Mappings[("Interact", true)] = new(Button("F"));
Equal("F", InputBindingText.ReadAction("Interact", true), "explicit keyboard binding uses provider keyboard route");
Check(provider.Requests.SequenceEqual(new[] { ("Interact", false), ("Interact", false), ("Interact", true) }), "provider receives exact native action and keyboard preference");
provider.Mappings[("ControllerOnlyAction", false)] = new(Button("DualSense Cross"));
Equal("DualSense Cross", InputBindingText.ReadAction("ControllerOnlyAction"), "device-specific native names preserved without guessed universal buttons");
Equal(null, InputBindingText.ReadAction("Unknown"), "missing action remains unbound");
int requests = provider.Requests.Count;
Equal(null, InputBindingText.ReadAction(" "), "empty action ignored");
Rewired.ReInput.isReady = false; Equal(null, InputBindingText.ReadAction("Interact"), "uninitialized input cannot supply a binding"); Rewired.ReInput.isReady = true;
Check(provider.Requests.Count == requests, "invalid action and uninitialized input do not invoke provider");
provider.WasCollected = true; Equal(null, InputBindingText.ReadAction("Interact"), "collected provider wrapper rejected before native equality"); provider.WasCollected = false;
provider.Destroyed = true; Equal(null, InputBindingText.ReadAction("Interact"), "destroyed provider rejected"); provider.Destroyed = false;
PromptsInfoProvider.Instance = null; Equal(null, InputBindingText.ReadAction("Interact"), "absent provider rejected"); PromptsInfoProvider.Instance = provider;
provider.Throw = true; Equal(null, InputBindingText.ReadAction("Interact"), "provider native exception contained"); provider.Throw = false;
Equal("Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl"), Button("A"))), "native plus-split combo stays a single chord");
Equal("Hold Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl", true), Button("A", true))), "hold applied once to complete chord");
Equal("Hold Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl"), Button("A", true))), "one native hold flag applies to complete chord");
Equal("Hold Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl"), Button("A")), true), "panel action can explicitly require hold");
Equal("Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl", true), Button("A", true)), false), "panel action false overrides stale provider hold flags");
provider.Mappings[("Interact", false)] = new(Button("Left Shoulder", true), Button("A", true));
Equal("Left Shoulder + A", InputBindingText.ReadAction("Interact", false, false), "native panel override passed through action resolution");
Equal("Hold Left Shoulder + A", InputBindingText.ReadAction("Interact", false, true), "explicit hold propagated through provider action");
Equal("Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl"), Button("Ctrl"), Button("A"))), "duplicate native chord labels do not repeat");
Equal("A", InputBindingText.Read(Button(" <b>A</b> \n")), "production text cleaning preserves plain native name");
Equal("Hold A", InputBindingText.Read(Button("A", true)), "single button reader preserves native hold");
Equal(null, InputBindingText.Read(null), "missing button rejected");
Equal(null, InputBindingText.ReadButtons(null), "missing native list rejected");
Equal(null, InputBindingText.ReadButtons(new()), "empty native combo rejected");
var collectedList = new ButtonList(Button("A")) { WasCollected = true };
Equal(null, InputBindingText.ReadButtons(collectedList), "collected native list rejected");
var collected = Button("Ctrl"); collected.WasCollected = true;
Equal(null, InputBindingText.ReadButtons(new(collected, Button("A"))), "collected modifier cannot become misleading unmodified A");
Equal(null, InputBindingText.ReadButtons(new(null, Button("A"))), "missing modifier invalidates whole binding");
Equal(null, InputBindingText.ReadButtons(new(Button(" "), Button("A"))), "nameless modifier invalidates whole binding");
ModText.SetLanguage("es");
Equal("Mantener Ctrl + A", InputBindingText.ReadButtons(new(Button("Ctrl", true), Button("A", true))), "Spanish hold wraps native chord once");
Equal("Mantener Right Shoulder", InputBindingText.Read(Button("Right Shoulder", true)), "native device label preserved while mod grammar localizes");
ModText.SetLanguage("en");

MultiButtonsActionPrompt Prompt(GameObject parent, string action, ButtonList buttons = null)
{
    var root = parent.Child("ActionPrompt");
    return root.Add(new MultiButtonsActionPrompt { _promptDescription = root.Child("Label").Add(new TMP_Text { text = action }), Buttons = buttons });
}
var panel = new GameObject { name = "Inventory" };
var prompt = Prompt(panel, "Equip", new(Button("X")));
provider.Mappings[("Equip", false)] = new(Button("A"));
Equal("Equip: X", InputBindingText.ReadPrompt(prompt), "prompt uses its own native GetButtonsInfo override rather than generic action binding");
Check(prompt.ButtonReads == 1, "native prompt binding getter actually invoked");
prompt.Buttons = null; Equal("Equip", InputBindingText.ReadPrompt(prompt), "visible action retained when binding is missing");
prompt.Buttons = new(null, Button("A")); Equal("Equip", InputBindingText.ReadPrompt(prompt), "incomplete combo retains action without false controls");
prompt.ThrowButtons = true; Equal("Equip", InputBindingText.ReadPrompt(prompt), "native prompt binding failure retains visible action"); prompt.ThrowButtons = false;
prompt.Buttons = new(new ButtonInfo { ThrowName = true }); Equal("Equip", InputBindingText.ReadPrompt(prompt), "button property failure retains visible action");
prompt.Buttons = new(Button("X"));
prompt._promptDescription.alpha = 0; Equal(null, InputBindingText.ReadPrompt(prompt), "hidden label cannot supply an action"); prompt._promptDescription.alpha = 1;
prompt._promptDescription.canvasRenderer.cull = true; Equal(null, InputBindingText.ReadPrompt(prompt), "culled label ignored"); prompt._promptDescription.canvasRenderer.cull = false;
prompt._promptDescription.WasCollected = true; Equal(null, InputBindingText.ReadPrompt(prompt), "collected label ignored safely"); prompt._promptDescription.WasCollected = false;
prompt.WasCollected = true; Equal(null, InputBindingText.ReadPrompt(prompt), "collected prompt ignored safely"); prompt.WasCollected = false;
var canvasGroup = panel.Add(new CanvasGroup { alpha = 0 }); Equal(null, InputBindingText.ReadPrompt(prompt), "hidden owning canvas group excludes prompt"); canvasGroup.alpha = 1;
prompt._promptDescription.text = "<sprite name=\"A\">"; Equal(null, InputBindingText.ReadPrompt(prompt), "icon without action label does not invent action text"); prompt._promptDescription.text = "Equip";

void ResetPrompts()
{
    ButtonPromptAnnouncer.Reset(); ScreenReader.Messages.Clear(); ScreenReader.SpokeThisFrame = ScreenReader.Speaking = false;
    Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
    ScreenContentAnnouncer.HasActiveContent = DialogueAnnouncer.HasActiveContent = false;
    PlayerFinder.Player = null; GameplayInputState.Owns = false; NavigationManager.currentlySelected = null;
}
void Tick(bool pending = false) { ScreenReader.SpokeThisFrame = false; ButtonPromptAnnouncer.Update(pending); }
void Settle(bool pending = false) { Tick(pending); Tick(pending); }
ResetPrompts();
var selection = panel.Add(new Navigable { Id = 10 }); NavigationManager.currentlySelected = selection;
ButtonPromptAnnouncer.Track(prompt);
Tick(); Check(ScreenReader.Messages.Count == 0, "menu prompt waits for settled visible text");
Tick(); Equal("Equip: X", ScreenReader.Messages.Single().Text, "owned selected menu action announces actual mapped button");
Check(!ScreenReader.Messages.Single().Interrupt, "action prompt does not interrupt current reader content");
Settle(); Check(ScreenReader.Messages.Count == 1, "unchanged action and binding deduplicated");
prompt.Buttons = new(Button("Y")); Tick(); Check(ScreenReader.Messages.Count == 1, "remap settles before speech"); Tick();
Check(ScreenReader.Messages.Count == 1, "device/remapping change alone cannot replay an already explained action");
var unrelated = Prompt(new GameObject { name = "HUD" }, "Talk", new(Button("A"))); ButtonPromptAnnouncer.Track(unrelated);
Settle(); Check(ScreenReader.Messages.Count == 1, "unrelated HUD prompt excluded while menu owns input");
unrelated._activePromptNavigables = new(new[] { 10 }); Settle();
Equal("Talk: A", ScreenReader.Messages.Last().Text, "explicit native navigable ownership permits non-descendant menu action");
ResetPrompts(); NavigationManager.currentlySelected = selection; unrelated._exceptSelectedNavigables = true; ButtonPromptAnnouncer.Track(unrelated); Settle();
Check(ScreenReader.Messages.Count == 0, "except-list does not authorize selected menu ownership"); unrelated._exceptSelectedNavigables = false;
unrelated._activePromptNavigables = new(new[] { 11 }); Settle(); Check(ScreenReader.Messages.Count == 0, "another navigable cannot authorize prompt");
unrelated._activePromptNavigables = new(new[] { 10 }) { WasCollected = true }; Settle(); Check(ScreenReader.Messages.Count == 0, "collected native ownership list cannot authorize prompt");
ResetPrompts(); NavigationManager.currentlySelected = selection; prompt.PromptActive = false; ButtonPromptAnnouncer.Track(prompt); Settle();
Check(ScreenReader.Messages.Count == 0, "native prompt inactive for current input/context remains silent"); prompt.PromptActive = true;
prompt.Enabled = false; Settle(); Check(ScreenReader.Messages.Count == 0, "disabled action prompt remains silent"); prompt.Enabled = true;
prompt._promptDescription.canvasRenderer.cull = true; Settle(); Check(ScreenReader.Messages.Count == 0, "culled menu prompt remains silent"); prompt._promptDescription.canvasRenderer.cull = false;
canvasGroup.alpha = 0; Settle(); Check(ScreenReader.Messages.Count == 0, "hidden owning menu group remains silent"); canvasGroup.alpha = 1;
Application.isFocused = false; Settle(); Check(ScreenReader.Messages.Count == 0, "unfocused game cannot speak actions"); Application.isFocused = true;
LE.UI.LoadingScreen.IsVisible = true; Settle(); Check(ScreenReader.Messages.Count == 0, "loading screen suppresses action hints"); LE.UI.LoadingScreen.IsVisible = false;
ScreenContentAnnouncer.HasActiveContent = true; Settle(); Check(ScreenReader.Messages.Count == 0, "screen content keeps priority over hints"); ScreenContentAnnouncer.HasActiveContent = false;
DialogueAnnouncer.HasActiveContent = true; Settle(); Check(ScreenReader.Messages.Count == 0, "dialogue keeps priority over hints"); DialogueAnnouncer.HasActiveContent = false;
Settle(true); Check(ScreenReader.Messages.Count == 0, "other pending content keeps priority");
ScreenReader.Speaking = true; Settle(); Check(ScreenReader.Messages.Count == 0, "active speech keeps priority"); ScreenReader.Speaking = false;
Tick(); Equal("Equip: Y", ScreenReader.Messages.Single().Text, "current owned action delivered when speech becomes available");
ResetPrompts(); PlayerFinder.Player = new GameObject(); GameplayInputState.Owns = true;
unrelated._activePromptNavigables = null; ButtonPromptAnnouncer.Track(unrelated); Settle();
Check(ScreenReader.Messages.Count == 0, "persistent HUD hints stay silent during exploration");
int readBeforeGameplay = unrelated.ButtonReads;
foreach (string control in new[] { "Left Alt", "R3", "Left Control", "L3" })
{
    unrelated.Buttons = new(Button(control)); unrelated.PromptActive = false; Tick();
    unrelated.PromptActive = true; Settle();
}
Check(ScreenReader.Messages.Count == 0 && unrelated.ButtonReads == readBeforeGameplay,
    "pickup/reactivation and keyboard screen-reader commands never read gameplay HUD buttons");
NavigationManager.currentlySelected = selection;
unrelated._activePromptNavigables = new(new[] { 10 }); Settle();
Check(ScreenReader.Messages.Count == 0, "even explicitly selected gameplay hints remain silent until a menu owns input");
GameplayInputState.Owns = false; Settle();
Equal("Talk: L3", ScreenReader.Messages.Single().Text, "entering a real owned menu reads the latest current device binding");
unrelated.Buttons = new(Button("Left Alt")); Settle();
unrelated.Buttons = new(Button("R3")); Settle();
Check(ScreenReader.Messages.Count == 1, "keyboard/controller round trip within same menu does not replay hints");
unrelated._promptDescription.text = "Continue"; Settle();
Equal("Continue: R3", ScreenReader.Messages.Last().Text, "newly available menu action still reads its latest binding");
GameplayInputState.Owns = true; Settle(); GameplayInputState.Owns = false; Settle();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text == "Continue: R3",
    "closing and reopening a menu renews its hints without enabling gameplay narration");
ResetPrompts(); PlayerFinder.Player = new GameObject(); GameplayInputState.Owns = false; ButtonPromptAnnouncer.Track(unrelated); Settle();
Check(ScreenReader.Messages.Count == 0, "player existence alone never enables unrelated HUD speech in menus");
ResetPrompts(); NavigationManager.currentlySelected = selection; ButtonPromptAnnouncer.Track(prompt); Tick(); ButtonPromptAnnouncer.Remove(prompt); Settle();
Check(ScreenReader.Messages.Count == 0, "removed unsettled prompt never becomes stale queued speech");
ResetPrompts(); NavigationManager.currentlySelected = selection; ButtonPromptAnnouncer.Track(prompt); prompt.WasCollected = true; Settle();
Check(ScreenReader.Messages.Count == 0, "expired prompt wrapper removed without announcing"); prompt.WasCollected = false;
Console.WriteLine($"Passed {checks} input binding and prompt integration checks.");
