using LeAccess.Accessibility.CharacterCreation;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

int checks = 0;
void Check(bool value, string message)
{ checks++; if (!value) throw new InvalidOperationException(message); }
void Safe(string label) => Check(NativeObject.InvalidAccesses == 0, label + ": no collected native access");
void Cancel(string label, Action<Scenario> change)
{
    var test = new Scenario(); test.Open(); test.Close(); change(test);
    Scenario.Frame(); Scenario.Frame();
    Check(Scenario.RestoreCount == 0 && ControllerMapsManager.Updates == 0, label + ": cancelled");
    Safe(label);
}

var s = new Scenario(); s.Open();
Check(Scenario.RestoreCount == 0, "Opening Start never restores before closing");
s.Owner._activeSelectable = s.Owner.gameObject.Child().Add(new Button());
s.Close();
Check(s.Closing is not null, "Closing matching pause retains the keyboard's previous owner");
PauseMenuFocusRecovery.Update();
Check(Scenario.RestoreCount == 0, "Closing frame allows native panel cleanup to finish");
Scenario.Frame();
Check(Scenario.RestoreCount == 1 && ReferenceEquals(PanelSystem.Restored[0], s.Owner), "Next frame restores exact name-screen owner, not keyboard or pause");
Check(s.Owner.SelectionCalls == 1 && ReferenceEquals(s.Owner.SelectedControl, s.Input), "Name input control is restored after owner selection");
Check(s.Input.text == "Ariadna", "Restoring focus preserves entered name");
Check(PanelSystem.Events.SequenceEqual(new[] { "restore", "map" }), "Input map refresh follows native restoration");
Scenario.Frame(); Scenario.Frame();
PauseMenuFocusRecovery.CompleteClose(s.Closing, true); Scenario.Frame();
Check(Scenario.RestoreCount == 1 && s.Owner.SelectionCalls == 1, "Repeated updates and duplicate close result do not restore twice");
Safe("normal keyboard path");

s = new Scenario(); s.Open(false); s.Close(); Scenario.Frame();
Check(Scenario.RestoreCount == 1 && s.Owner.SelectionCalls == 0, "Existing selected control stays unchanged when native restoration retains it");
s = new Scenario(); s.Owner._activeSelectable = s.Owner.gameObject.Child().Add(new Button());
s.Open(); s.Close(); Scenario.Frame();
Check(ReferenceEquals(s.Owner.SelectedControl, s.Input), "Keyboard editor is authoritative even when owner's old selected control differs");

foreach (var mode in new[] { "same owner", "another owner", "collected owner" })
{
    s = new Scenario(); s.Open(); s.Close();
    var restored = mode == "same owner" ? s.Owner : new GameObject().Add(new Navigable());
    if (mode == "collected owner") restored.WasCollected = true;
    NavigationManager.currentlySelected = restored;
    Scenario.Frame();
    Check(Scenario.RestoreCount == 0 && ReferenceEquals(NavigationManager.currentlySelected, restored), "Never steals existing selection: " + mode);
    NavigationManager.currentlySelected = null; Scenario.Frame();
    Check(Scenario.RestoreCount == 0, "Cancelled restore cannot fire later: " + mode);
    Safe(mode);
}

Cancel("loading", _ => LE.UI.LoadingScreen.IsVisible = true);
Cancel("unfocused application", _ => Application.isFocused = false);
Cancel("device switched to keyboard/mouse", _ => EpochInputManager.IsControllerActive = false);
Cancel("new active scene", _ => SceneManager.Current = new Scene { handle = 2, isLoaded = true });
Cancel("active scene unloading", _ => SceneManager.Current = new Scene { handle = 1, isLoaded = false });
Cancel("character state changed", t => t.Character.currentState = CharacterSelect.CharacterSelectState.ClassSelection);
Cancel("character screen replaced", _ => CharacterSelect.instance = new GameObject().Add(new CharacterSelect()));
Cancel("character destroyed", t => t.Character.Destroyed = true);
Cancel("character collected", t => t.Character.WasCollected = true);
Cancel("character hidden", t => t.Character.gameObject.Active = false);
Cancel("character disabled", t => t.Character.Enabled = false);
Cancel("owner destroyed", t => t.Owner.Destroyed = true);
Cancel("owner collected", t => t.Owner.WasCollected = true);
Cancel("owner hidden", t => t.Owner.gameObject.Active = false);
Cancel("owner disabled", t => t.Owner.Enabled = false);
Cancel("owner transparent", t => t.Owner.gameObject.Add(new CanvasGroup { alpha = 0 }));
Cancel("owner scene unloaded", t => t.Owner.gameObject.scene = new Scene { handle = 1, isLoaded = false });
Cancel("owner moved to another scene", t => t.Owner.gameObject.scene = new Scene { handle = 2, isLoaded = true });
Cancel("panel system destroyed", t => t.System.Destroyed = true);
Cancel("panel system collected", t => t.System.WasCollected = true);
Cancel("UI singleton destroyed", _ => UIBase.instance.Destroyed = true);
Cancel("UI singleton collected", _ => UIBase.instance.WasCollected = true);
Cancel("another panel remains open", t => t.System.IsAnyPanelOpen = true);
Cancel("pause remains open", _ => UIBase.instance.menuOpen = true);
Cancel("another panel opening", t => t.System._openingPanels.Value = 1);
Cancel("opening list collected", t => t.System._openingPanels.WasCollected = true);

s = new Scenario(); s.Open(); s.Close(); EpochInputManager.IsControllerActive = false; Scenario.Frame();
EpochInputManager.IsControllerActive = true; Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Returning to controller does not revive cancelled recovery");
s = new Scenario(); s.Open(); s.Character.currentState = CharacterSelect.CharacterSelectState.ClassSelection;
Scenario.Frame(); s.Character.currentState = CharacterSelect.CharacterSelectState.NameCreation;
s.Close(); Scenario.Frame();
Check(s.Closing is null && Scenario.RestoreCount == 0, "Returning to old screen state cannot revive snapshot invalidated during pause");

foreach (var mode in new[] { "keyboard disabled", "keyboard collected", "keyboard destroyed", "manager collected", "unregistered owner", "editor hidden", "editor collected", "editor reparented", "owner from other scene", "pause itself" })
{
    s = new Scenario();
    switch (mode)
    {
        case "keyboard disabled": s.Keyboard._enabled = false; break;
        case "keyboard collected": s.Keyboard.WasCollected = true; break;
        case "keyboard destroyed": s.Keyboard.Destroyed = true; break;
        case "manager collected": NavigationManager.instance.WasCollected = true; break;
        case "unregistered owner": NavigationManager.instance.Registry.Clear(); break;
        case "editor hidden": s.Input.gameObject.Active = false; break;
        case "editor collected": s.Input.gameObject.WasCollected = true; break;
        case "editor reparented": s.Input.gameObject.Parent = new GameObject(); break;
        case "owner from other scene": s.Owner.gameObject.scene = new Scene { handle = 2, isLoaded = true }; break;
        case "pause itself":
            NavigationManager.instance.Registry[42] = s.PauseNavigation;
            s.Input.gameObject.Parent = s.PauseNavigation.gameObject;
            break;
    }
    s.Open(); s.Close(); Scenario.Frame();
    Check(s.Closing is null && Scenario.RestoreCount == 0, "Unavailable/incorrect keyboard owner is not captured: " + mode);
    Safe(mode);
}

foreach (var mode in new[] { "gameplay", "null focus", "character none", "loading at capture", "mouse at capture" })
{
    s = new Scenario();
    if (mode == "gameplay") CharacterSelect.instance = null;
    if (mode == "character none") s.Character.currentState = CharacterSelect.CharacterSelectState.None;
    if (mode == "loading at capture") LE.UI.LoadingScreen.IsVisible = true;
    if (mode == "mouse at capture") EpochInputManager.IsControllerActive = false;
    NavigationManager.currentlySelected = null;
    PauseMenuFocusRecovery.Update(); PauseMenuFocusRecovery.Capture(s.Pause, s.System);
    s.Close(); Scenario.Frame();
    Check(s.Closing is null && Scenario.RestoreCount == 0, "No invented context: " + mode);
    Safe(mode);
}

s = new Scenario(); s.Open();
var otherPanel = new GameObject().Add(new Panel());
var otherSystem = new GameObject().Add(new PanelSystem());
Check(PauseMenuFocusRecovery.BeginClose(s.System, otherPanel, false) is null, "Options close cannot consume outer pause snapshot");
Check(PauseMenuFocusRecovery.BeginClose(otherSystem, s.Pause, false) is null, "Matching panel in different system is ignored");
Check(PauseMenuFocusRecovery.BeginClose(s.System, s.Pause, true) is null, "Preloading close is ignored");
Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Nested options do not restore while pause is open");
s.Close(false); Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Failed native close does not schedule recovery");
s.Close(); Scenario.Frame();
Check(Scenario.RestoreCount == 1, "Successful later close still restores original context");
s = new Scenario(); s.Open();
var panelAlias = new GameObject().Add(new MainMenuPanel { NativePointer = s.Pause.NativePointer });
var systemAlias = new GameObject().Add(new PanelSystem { NativePointer = s.System.NativePointer });
var aliasSnapshot = PauseMenuFocusRecovery.BeginClose(systemAlias, panelAlias, false);
Check(aliasSnapshot is not null, "Matching native panel/system identity survives a different managed wrapper");
NavigationManager.currentlySelected = null; UIBase.instance.menuOpen = s.System.IsAnyPanelOpen = false;
PauseMenuFocusRecovery.CompleteClose(aliasSnapshot, true); Scenario.Frame();
Check(Scenario.RestoreCount == 1, "Matching native aliases close the saved pause correctly");

s = new Scenario(); s.Open();
PauseMenuFocusRecovery.Capture(s.Pause, s.System);
s.Close(); Scenario.Frame();
Check(Scenario.RestoreCount == 1 && ReferenceEquals(PanelSystem.Restored[0], s.Owner), "Duplicate open notification does not overwrite snapshot with pause focus");

s = new Scenario(); PauseMenuFocusRecovery.Update();
NavigationManager.currentlySelected = null;
PauseMenuFocusRecovery.Capture(s.Pause, s.System);
s.Close(); Scenario.Frame();
Check(Scenario.RestoreCount == 1 && ReferenceEquals(PanelSystem.Restored[0], s.Owner), "Previously observed owner survives native focus clearing before open hook");
s = new Scenario(); s.Open(); s.Close(); s.System._openingPanels = null; Scenario.Frame();
Check(Scenario.RestoreCount == 1, "Missing opening list is treated as empty");

s = new Scenario(); s.Open(); s.Close(); Scenario.Frame();
var next = s.Character.gameObject.Child().Add(new Navigable());
var nextControl = next.gameObject.Child().Add(new Button()); next._activeSelectable = nextControl;
NavigationManager.currentlySelected = next; PauseMenuFocusRecovery.Update();
PauseMenuFocusRecovery.Capture(s.Pause, s.System);
NavigationManager.currentlySelected = s.PauseNavigation; UIBase.instance.menuOpen = s.System.IsAnyPanelOpen = true;
var oldClose = s.Closing;
PauseMenuFocusRecovery.CompleteClose(oldClose, true); Scenario.Frame();
Check(Scenario.RestoreCount == 1, "Old close callback cannot close newly captured pause");
s.Close(); Scenario.Frame();
Check(Scenario.RestoreCount == 2 && ReferenceEquals(PanelSystem.Restored[1], next), "Reopened pause snapshots the new owner");

foreach (var mode in new[] { "destroyed", "collected", "hidden", "disabled", "noninteractable", "excluded", "reparented" })
{
    s = new Scenario(); s.Open(); s.Close();
    s.Owner._activeSelectable = null;
    switch (mode)
    {
        case "destroyed": s.Input.Destroyed = true; break;
        case "collected": s.Input.WasCollected = true; break;
        case "hidden": s.Input.gameObject.Active = false; break;
        case "disabled": s.Input.Enabled = false; break;
        case "noninteractable": s.Input.Interactable = false; break;
        case "excluded": s.Owner.ExcludeControl = true; break;
        case "reparented": s.Input.gameObject.Parent = new GameObject(); break;
    }
    Scenario.Frame();
    Check(Scenario.RestoreCount == 1 && s.Owner.SelectionCalls == 0, "Recover owner without forcing invalid old control: " + mode);
    Safe("invalid control " + mode);
}

s = new Scenario(); s.Open(); s.Close(); PanelSystem.NativeSelects = false; Scenario.Frame(); Scenario.Frame();
Check(Scenario.RestoreCount == 1 && s.Owner.SelectionCalls == 0, "Native refusal is not retried and cannot force a control");
s = new Scenario(); s.Open(); s.Close();
PanelSystem.DuringRestore = _ => NavigationManager.currentlySelected = new GameObject().Add(new Navigable());
Scenario.Frame();
Check(s.Owner.SelectionCalls == 0, "Native selection of another owner is respected");

// Pause -> Settings: the restore waits while Settings is open and runs once it closes.
static Scenario ToSettings()
{
    var t = new Scenario(); t.Open(); t.Close();
    t.System.IsAnyPanelOpen = true; Scenario.Frame();
    return t;
}
s = ToSettings();
for (var i = 0; i < 5; i++) Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Restore waits while Settings is open");
Check(LeAccess.Plugin.Log.Messages.Count(m => m.StartsWith("[PauseFocus] waiting: another panel is open (selection=none)")) == 1, "Waiting is logged once");
s.System._openingPanels.Value = 1; Scenario.Frame(); s.System._openingPanels.Value = 0;
s.System.IsAnyPanelOpen = false; Scenario.Frame();
Check(Scenario.RestoreCount == 1 && ReferenceEquals(PanelSystem.Restored[0], s.Owner), "Closing Settings restores the class screen owner");
Scenario.Frame(); Scenario.Frame();
Check(Scenario.RestoreCount == 1, "Waiting restore fires only once");
Safe("settings wait");

s = ToSettings(); s.Owner.gameObject.Active = false; Scenario.Frame();
s.Owner.gameObject.Active = true; s.System.IsAnyPanelOpen = false; Scenario.Frame();
Check(Scenario.RestoreCount == 1, "Owner hidden under Settings is only checked at restore time");
s = ToSettings(); s.Owner.gameObject.Active = false; s.System.IsAnyPanelOpen = false; Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Owner still hidden when panels close is discarded");

// Settings closes back to the pause menu: the waiting snapshot is bound again.
s = ToSettings(); NavigationManager.currentlySelected = null;
PauseMenuFocusRecovery.Update(); PauseMenuFocusRecovery.Capture(s.Pause, s.System);
NavigationManager.currentlySelected = s.PauseNavigation; UIBase.instance.menuOpen = s.System.IsAnyPanelOpen = true;
Scenario.Frame(); Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Reopened pause does not restore while open");
s.Close(); Scenario.Frame();
Check(Scenario.RestoreCount == 1 && ReferenceEquals(PanelSystem.Restored[0], s.Owner), "Continue restores snapshot kept across Settings");
Safe("settings reopen pause");

foreach (var mode in new[] { "state", "scene" })
{
    s = ToSettings(); Scenario.Frame();
    if (mode == "state") s.Character.currentState = CharacterSelect.CharacterSelectState.ClassSelection;
    else SceneManager.Current = new Scene { handle = 2, isLoaded = true };
    Scenario.Frame();
    if (mode == "state") s.Character.currentState = CharacterSelect.CharacterSelectState.NameCreation;
    else SceneManager.Current = new Scene { handle = 1, isLoaded = true };
    s.System.IsAnyPanelOpen = false; Scenario.Frame(); Scenario.Frame();
    Check(Scenario.RestoreCount == 0, "Change while waiting discards: " + mode);
}

s = ToSettings(); var native = new GameObject().Add(new Navigable());
NavigationManager.currentlySelected = native; s.System.IsAnyPanelOpen = false; Scenario.Frame();
NavigationManager.currentlySelected = null; Scenario.Frame();
Check(Scenario.RestoreCount == 0 && s.Owner.SelectionCalls == 0, "Native selection after Settings is not overwritten or revived");


s = new Scenario(); s.Open(); s.Close(); PauseMenuFocusRecovery.Reset(); Scenario.Frame();
Check(Scenario.RestoreCount == 0, "Lifecycle reset drops pending recovery");
Console.WriteLine($"Pause focus tests passed: {checks} checks.");
