using BepInEx.Configuration;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using UnityEngine;
using UnityEngine.AI;
using static Program;

internal static class ServiceTests
{
    private static ConfigFile _config;
    private static Rewired.IControllerTemplateThumbStick Stick => EpochInputManager.instance.rewiredPlayer.controllers.Joysticks[0].Template.rightStick;
    private static void Reset()
    {
        GuidanceAccessibility.Cleanup(); RouteTests.Reset();
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false; Camera.main = new();
        NavigationAudioState.IsSuspended = false; GameplayInputState.Owned = true;
        Input.Held.Clear(); Input.Down.Clear(); Input.Throw = false; ScreenReader.Spoken.Clear(); ScreenReader.Available = true;
        EpochInputManager.instance = new(); ScreenKeyboardManager.Instance = new();
        EpochInputManager.instance.rewiredPlayer.controllers.Joysticks.Items.Add(new());
        GuidanceTargetReader.Targets.Clear(); GuidanceTargetReader.Available = true; GuidanceTargetReader.Throw = false;
        GuidanceTargetReader.Reads = 0; LeAccess.Plugin.Log.Warnings.Clear(); LeAccess.Plugin.Log.Info.Clear();
        _config = new(); GuidanceAccessibility.Initialize(_config);
    }
    private static void Add(int id = 1, string name = "Quest", float distance = 10, int priority = 0, bool quest = true) =>
        GuidanceTargetReader.Targets.Add(new(id, name, new(0, 0, distance), 0, quest) { Priority = priority });
    private static string Request()
    {
        Input.Down.Add(KeyCode.F6); GuidanceAccessibility.Update(); Input.Down.Clear();
        return ScreenReader.Spoken.LastOrDefault();
    }

    internal static void Run()
    {
        InputEdges(); RequestsAndProgress(); Selection(); Gates(); Failures();
        GuidanceAccessibility.Cleanup();
    }

    private static void InputEdges()
    {
        Check(MenuDetailInput.IsDownPush(0, -0.5f, 0, 0), "Down starts exactly at the deliberate half-travel threshold.");
        foreach (var values in new[] { (0f, -0.49f, 0f, 0f), (0f, -1f, 0f, -0.5f), (0.9f, -0.6f, 0f, 0f),
            (0f, -1f, 0.5f, 0f), (0f, -1f, 0f, 0.5f), (0f, 1f, 0f, 0f) })
            Check(!MenuDetailInput.IsDownPush(values.Item1, values.Item2, values.Item3, values.Item4),
                "Small, held, horizontal and nonneutral gestures do not request guidance.");
        Check(MenuDetailInput.IsUpPush(0, 1, 0, 0) && !MenuDetailInput.IsDownPush(0, 1, 0, 0),
            "Existing tooltip-up gesture stays separate from guidance-down.");
        Reset(); Add(); Stick.value = new(0, -1);
        GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 1, "Real source-linked controller path reports on the first down edge.");
        Stick.valuePrev = Stick.value;
        for (int frame = 0; frame < 20; frame++) GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 1, "Holding the stick never repeats guidance.");
        Stick.valuePrev = new(0, 0); GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 2, "Returning to neutral permits a second deliberate request.");
        Reset(); Add(); Stick.value = new(0, -1); Stick.WasCollected = true; GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 0, "Collected input wrappers cannot synthesize controller presses.");
    }

    private static void RequestsAndProgress()
    {
        Reset(); Add(); GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 0 && GuidanceTargetReader.Reads == 0, "No input means no automatic route scanning or speech.");
        Check(Request().Contains("guidance.route[Quest|guidance.direction.up|10]"), "F6 reports named target, camera direction and verified path distance.");
        Check(!ScreenReader.Spoken.Last().Contains("guidance.closer"), "First request makes no progress claim.");
        MapSectionWorld.World = MapSectionWorld.World with { Z = 2 };
        Check(Request().Contains("guidance.closer"), "Second request confirms actual progress toward the same route target.");
        MapSectionWorld.World = MapSectionWorld.World with { Z = 0 };
        Check(Request().Contains("guidance.farther"), "Walking back reports increasing route distance.");
        NavMesh.Status = NavMeshPathStatus.PathPartial; NavMesh.Blocked = true;
        string fallback = Request();
        Check(fallback.Contains("guidance.bearing") && fallback.Contains("guidance.direct_blocked") &&
            !fallback.Contains("guidance.farther") && !fallback.Contains("guidance.closer"),
            "Incomplete routes explicitly report blocked straight bearing without mixing distance trends.");
        NavMesh.Status = NavMeshPathStatus.PathComplete;
        Check(!Request().Contains("guidance.closer"), "Verified route recovery restarts the progress baseline.");
        MapSectionWorld.World = MapSectionWorld.World with { Z = 9.5 };
        Check(Request().StartsWith("guidance.near_marker[Quest]") && !ScreenReader.Spoken.Last().Contains("guidance.closer"),
            "Marker proximity does not claim objective completion or add progress clutter.");
        GuidanceAccessibility.Cleanup(); GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 6, "Cleanup disables further guidance.");
    }

    private static void Selection()
    {
        Reset(); Add(1, "Near", 4); Add(2, "Far", 8);
        Check(Request().Contains("[Near|"), "Nearest same-priority marker is selected initially.");
        GuidanceTargetReader.Targets.Clear(); Add(2, "Now closer", 2); Add(1, "Sticky", 6);
        Check(Request().Contains("[Sticky|"), "An available selected marker is retained when another becomes closer.");
        Add(3, "Tracked", 20, priority: -1);
        Check(Request().Contains("[Tracked|"), "A higher-priority target replaces the sticky lower-priority target.");
        GuidanceTargetReader.Targets.Clear(); Add(8, "Tie high", 5); Add(4, "Tie low", 5);
        Check(Request().Contains("[Tie low|"), "Equal-distance targets use stable ID ordering after selected target disappears.");
        GuidanceTargetReader.Targets.Clear(); Add(4, "Old", 5); Add(9, "New", 2);
        MapSectionWorld.World = MapSectionWorld.World with { Session = 2 };
        Check(Request().Contains("[New|"), "Scene/player replacement clears sticky selection.");
        GuidanceTargetReader.Targets.Clear(); Add(10, "Forest", 5, quest: false);
        Check(Request().Contains("guidance.exit_unconfirmed"), "Exit fallback clearly avoids claiming that it is the quest route.");
        GuidanceTargetReader.Targets.Clear(); Add(12, "", 1); Add(13, "Invalid", float.NaN); Add(14, "Real", 4);
        Check(Request().Contains("[Real|"), "Empty names and invalid positions do not steal target selection.");
    }

    private static void Gates()
    {
        foreach (Action block in new Action[] { () => Application.isFocused = false, () => LE.UI.LoadingScreen.IsVisible = true,
            () => NavigationAudioState.IsSuspended = true, () => GameplayInputState.Owned = false,
            () => EpochInputManager.instance.isInputFieldActive = true, () => ScreenKeyboardManager.Instance._enabled = true,
            () => ScreenReader.Available = false, () => ((ConfigEntry<bool>)_config.Values["Enabled"]).Value = false })
        {
            Reset(); Add(); block(); Request();
            Check(ScreenReader.Spoken.Count == 0 && GuidanceTargetReader.Reads == 0 && NavMesh.Calculations == 0,
                "Menus, typing, loading, focus, speech availability and configuration gate guidance before scans or speech.");
        }
        Reset(); Add(); Input.Down.Add(KeyCode.F6); GuidanceAccessibility.Update(otherContentActive: true);
        Check(ScreenReader.Spoken.Count == 0 && GuidanceTargetReader.Reads == 0, "Tutorial/dialogue content does not trigger navigation speech.");
        foreach (var modifier in new[] { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftShift, KeyCode.RightShift })
        {
            Reset(); Add(); Input.Held.Add(modifier); Request();
            Check(ScreenReader.Spoken.Count == 0, "Modified F6 is not consumed as a guidance command.");
        }
        Reset(); Add(); ((ConfigEntry<bool>)_config.Values["RightStickDown"]).Value = false; Stick.value = new(0, -1);
        GuidanceAccessibility.Update(); Check(ScreenReader.Spoken.Count == 0, "Controller guidance can be disabled independently.");
        Check(Request().Contains("Quest"), "Keyboard guidance still works when controller guidance is disabled.");
        Reset(); Add(); Input.Held.Add(KeyCode.F6); GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 0, "A held keyboard key without a down edge does not repeat.");
    }

    private static void Failures()
    {
        Reset(); Check(Request() == "guidance.no_target", "Missing visible targets give a clear manual response.");
        Reset(); GuidanceTargetReader.Available = false;
        Check(Request() == "guidance.unavailable", "Missing target state differs from a trustworthy empty set.");
        Reset(); Add(); MapSectionWorld.Available = false;
        Check(Request() == "guidance.unavailable" && NavMesh.Calculations == 0, "Missing local player snapshot never computes a route.");
        Reset(); Add(); Camera.main.isActiveAndEnabled = false;
        Check(Request() == "guidance.unavailable" && NavMesh.Calculations == 0, "Inactive gameplay camera blocks route direction.");
        Reset(); GuidanceTargetReader.Throw = true; Request(); Request();
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.All(s => s == "guidance.unavailable") &&
            LeAccess.Plugin.Log.Warnings.Count == 1, "Repeated requested failures remain understandable with bounded diagnostic logging.");
        Reset(); Add(); Input.Throw = true;
        for (int frame = 0; frame < 20; frame++) GuidanceAccessibility.Update();
        Check(ScreenReader.Spoken.Count == 0 && GuidanceTargetReader.Reads == 0 && LeAccess.Plugin.Log.Warnings.Count == 1,
            "Input failures before a confirmed request never speak automatically and log only once.");
    }
}
