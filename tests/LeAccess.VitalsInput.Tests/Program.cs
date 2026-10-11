using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Vitals;
using Rewired;
using UnityEngine;

int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
Joystick Reset()
{
    VitalsInput.Reset(); Input.Held.Clear(); Input.Down.Clear(); Input.Throw = false; Input.Reads = 0;
    LeAccess.Plugin.Log.Warnings.Clear();
    ExplorationInput.Command = ExplorationCommand.None; GameplayInputState.Owns = false;
    var joystick = new Joystick { Template = new IGamepadTemplate { back = new IControllerTemplateButton() } };
    var list = new Il2CppSystem.Collections.Generic.IList<Joystick>(); list.Items.Add(joystick);
    EpochInputManager.instance = new EpochInputManager
    { rewiredPlayer = new Player { controllers = new Player.ControllerHelper { Joysticks = list } } };
    return joystick;
}
var device = Reset(); Input.Down.Add(KeyCode.R);
Check(VitalsInput.ReportPressed(KeyCode.R, false), "Plain R edge reports vitals");
Input.Down.Clear(); Input.Held.Add(KeyCode.R);
Check(!VitalsInput.ReportPressed(KeyCode.R, false), "Holding R does not repeat the command");
foreach (var modifier in new[] { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftShift, KeyCode.RightShift })
{
    Reset(); Input.Down.Add(KeyCode.R); Input.Held.Add(modifier);
    Check(!VitalsInput.ReportPressed(KeyCode.R, false), $"{modifier} suppresses plain shortcut");
}
device = Reset(); Input.Down.Add(KeyCode.None);
Check(!VitalsInput.ReportPressed(KeyCode.None, false) && Input.Reads == 0, "None disables keyboard without input reads");
ExplorationInput.Command = ExplorationCommand.Vitals;
Check(VitalsInput.ReportPressed(KeyCode.None, true), "Create+Square command reports vitals");
Check(!VitalsInput.ReportPressed(KeyCode.None, false), "Controller disabled ignores the Create+Square command");
Check(!VitalsInput.GoldPressed(), "Create+Square does not read gold");
foreach (var other in new[] { ExplorationCommand.None, ExplorationCommand.Lock, ExplorationCommand.EntryNext, ExplorationCommand.CategoryPrevious,
    ExplorationCommand.Gold, ExplorationCommand.Status })
{
    ExplorationInput.Command = other;
    Check(!VitalsInput.ReportPressed(KeyCode.None, true), $"{other} does not report vitals");
    Check(VitalsInput.GoldPressed() == (other == ExplorationCommand.Gold), $"{other} reads gold only for the gold command");
}
ExplorationInput.Command = ExplorationCommand.None;
device = Reset(); device.Template.back.justPressed = true;
Check(!VitalsInput.ReportPressed(KeyCode.None, true) && device.Reads == 0, "Raw Create press alone no longer reports and VitalsInput reads no hardware");
device = Reset(); Input.Throw = true; ExplorationInput.Command = ExplorationCommand.Vitals;
Check(VitalsInput.ReportPressed(KeyCode.R, true) && LeAccess.Plugin.Log.Warnings.Single().Contains("keyboard"),
    "Keyboard failure is diagnosed independently and cannot disable the controller command");
Input.Throw = false; VitalsInput.Reset(); LeAccess.Plugin.Log.Warnings.Clear();
Input.Throw = true; VitalsInput.ReportPressed(KeyCode.R, true); VitalsInput.ReportPressed(KeyCode.R, true);
Check(LeAccess.Plugin.Log.Warnings.Count == 1, "Keyboard failure is logged once");
Input.Throw = false; VitalsInput.Reset(); VitalsInput.ReportPressed(KeyCode.R, true);
device = Reset();
device.Template.rightStick = new IControllerTemplateThumbStick { value = new() { y = 1 } };
Check(LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Right stick up requests current menu details");
device.Template.rightStick.valuePrev = new() { y = 1 };
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Holding stick cannot repeat details");
device.Template.rightStick.valuePrev = new() { x = 1 };
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Rotating a held stick requires neutral before another detail request");
device.Template.rightStick.valuePrev = new();
device.Template.rightStick.value = new() { y = 0.1f };
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Small stick drift cannot request details");
device.Template.rightStick.value = new() { x = 1, y = 0.6f };
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Horizontal dominant gestures retain their normal behavior");
device.Template.rightStick.value = new() { y = -1 };
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Down is not the detail gesture");
device.Template.rightStick.value = new() { y = 1 };
Application.isFocused = false;
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Inactive game ignores detail gesture");
Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = true;
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Loading ignores detail gesture");
LE.UI.LoadingScreen.IsVisible = false;
device.Template.rightStick.WasCollected = true;
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Collected stick is safely ignored");
GameplayInputState.Owns = true; device.Template.rightStick.WasCollected = false; device.Template.rightStick.value = new() { y = 1 };
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.ReadRequested(), "Right stick up no longer reads details in gameplay");
bool OnlyReaderKeys(params KeyCode[] keys) => LeAccess.Accessibility.Navigation.ControllerModeKeys.OnlyScreenReaderKeys(keys);
Check(OnlyReaderKeys(KeyCode.LeftControl) && OnlyReaderKeys(KeyCode.RightControl) && OnlyReaderKeys(KeyCode.LeftShift, KeyCode.Insert) &&
    OnlyReaderKeys(KeyCode.CapsLock) && OnlyReaderKeys(KeyCode.LeftAlt, KeyCode.AltGr), "Silencing, pausing and reader-modifier keys keep controller mode");
Check(!OnlyReaderKeys(KeyCode.R) && !OnlyReaderKeys(KeyCode.LeftControl, KeyCode.R) && !OnlyReaderKeys(KeyCode.Tab) &&
    !OnlyReaderKeys(KeyCode.Keypad0), "Any other key, alone or with a modifier, still switches to the keyboard");
Check(!OnlyReaderKeys(), "No key activity leaves the game's own verdict alone");
Check(LeAccess.Accessibility.Announcements.MenuDetailInput.IsPush(-1, 0, 0, 0) && LeAccess.Accessibility.Announcements.MenuDetailInput.IsPush(0, -0.6f, 0.1f, 0.1f),
    "A stick leaving rest toward any direction is a push");
Check(!LeAccess.Accessibility.Announcements.MenuDetailInput.IsPush(1, 0, 0, 1) && !LeAccess.Accessibility.Announcements.MenuDetailInput.IsPush(0.2f, 0.1f, 0, 0),
    "A held stick or drift is not a push");
Console.WriteLine($"Vitals/menu detail input tests passed: {checks} checks.");
