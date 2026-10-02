using LeAccess.Accessibility.Vitals;
using Rewired;
using UnityEngine;

int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
Joystick Reset()
{
    VitalsInput.Reset(); Input.Held.Clear(); Input.Down.Clear(); Input.Throw = false; Input.Reads = 0;
    LeAccess.Plugin.Log.Warnings.Clear();
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
device.Template.back.justPressed = true;
Check(VitalsInput.ReportPressed(KeyCode.None, true), "Assigned gamepad Back/View edge reports vitals");
device.Template.back.justPressed = false; device.Template.back.Held = true;
Check(!VitalsInput.ReportPressed(KeyCode.None, true), "Holding Back/View does not repeat the command");
device.Template.back.justPressed = true;
Check(!VitalsInput.ReportPressed(KeyCode.None, false) && device.Reads == 2, "Controller disabled skips all controller reads");
device = Reset();
var unassigned = new Joystick { Template = new IGamepadTemplate { back = new IControllerTemplateButton { justPressed = true } } };
Check(!VitalsInput.ReportPressed(KeyCode.None, true) && unassigned.Reads == 0, "Unassigned hardware is never consulted");
device.Template = null;
Check(!VitalsInput.ReportPressed(KeyCode.None, true), "Template-less assigned hardware is ignored");
foreach (string missing in new[] { "manager", "player", "controllers", "list", "joystick", "template", "button" })
{
    device = Reset(); device.Template.back.justPressed = true;
    if (missing == "manager") EpochInputManager.instance.WasCollected = true;
    if (missing == "player") EpochInputManager.instance.rewiredPlayer.WasCollected = true;
    if (missing == "controllers") EpochInputManager.instance.rewiredPlayer.controllers.WasCollected = true;
    if (missing == "list") EpochInputManager.instance.rewiredPlayer.controllers.Joysticks.WasCollected = true;
    if (missing == "joystick") device.WasCollected = true;
    if (missing == "template") device.Template.WasCollected = true;
    if (missing == "button") device.Template.back.WasCollected = true;
    Check(!VitalsInput.ReportPressed(KeyCode.None, true), $"Collected {missing} wrapper is ignored");
}
device = Reset(); device.Throw = true;
Check(!VitalsInput.ReportPressed(KeyCode.R, true), "Broken controller fails gracefully");
VitalsInput.ReportPressed(KeyCode.R, true);
Check(LeAccess.Plugin.Log.Warnings.Count == 1 && LeAccess.Plugin.Log.Warnings[0].Contains("controller"), "Controller failure is logged once");
Input.Down.Add(KeyCode.R);
Check(VitalsInput.ReportPressed(KeyCode.R, true), "Controller failure cannot disable keyboard edge");
Input.Down.Clear(); VitalsInput.Reset(); VitalsInput.ReportPressed(KeyCode.R, true);
Check(LeAccess.Plugin.Log.Warnings.Count == 2, "Reset clears diagnostic deduplication");
device = Reset(); Input.Throw = true; device.Template.back.justPressed = true;
Check(VitalsInput.ReportPressed(KeyCode.R, true) && LeAccess.Plugin.Log.Warnings.Single().Contains("keyboard"),
    "Keyboard failure is diagnosed independently and cannot disable controller edge");
device = Reset(); device.Throw = true;
var healthy = new Joystick { Template = new IGamepadTemplate { back = new IControllerTemplateButton { justPressed = true } } };
EpochInputManager.instance.rewiredPlayer.controllers.Joysticks.Items.Add(healthy);
Check(VitalsInput.ReportPressed(KeyCode.None, true), "Broken first assigned device cannot suppress another assigned device");
Console.WriteLine($"Vitals input tests passed: {checks} checks.");
