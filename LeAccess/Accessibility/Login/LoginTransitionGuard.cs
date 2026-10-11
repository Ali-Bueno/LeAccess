using System;
using ClientAppState;
using LE.UI;
using LE.UI.Login;
using LE.UI.Login.UnityUI;
using LeAccess.Accessibility.Announcements;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Login;

internal static class LoginTransitionGuard
{
    private sealed class Request
    {
        internal LandingZonePanel Panel;
        internal LoginController Controller;
        internal FSMController<LoginController.LoginUIState> StateMachine;
        internal Button Button;
        internal bool Online;
        internal int Scene;
    }

    private static Request _pending;
    private static string _lastFailure;

    internal static bool MayExecute(LandingZonePanel panel, bool online)
    {
        // The landing buttons answer Cross through paths the mod menu cannot all close; with it open they never start.
        if (ModMenu.InputBlock.Blocking)
        {
            Plugin.Log.LogInfo($"[Mod menu] landing click ignored: {(online ? "online" : "offline")}");
            return false;
        }
        bool deferred = false;
        try
        {
            if (!TryPhase(out var phase, out bool transitioning) ||
                phase != ClientAppStateType.SystemLoading && phase != ClientAppStateType.Login)
            { _pending = null; return true; }
            if (phase == ClientAppStateType.Login && !transitioning)
            { _pending = null; return true; }
            var request = Capture(panel, online);
            if (request is null) { _pending = null; return true; }
            bool first = _pending is null || !IsAlive(_pending.Panel);
            bool changed = first || _pending.Panel.Pointer != panel.Pointer || _pending.Online != online;
            _pending = request;
            deferred = true;
            if (changed) Plugin.Log.LogInfo($"[StartupMode] deferred mode={(online ? "online" : "offline")} state={phase}");
            if (first) ScreenReader.Speak(ModText.Get("login.waiting"));
            return false;
        }
        catch (Exception ex)
        {
            if (!deferred) _pending = null;
            Report(ex);
            return !deferred;
        }
    }

    internal static void Update()
    {
        if (_pending is null) return;
        try
        {
            var request = _pending;
            if (!StillOwned(request)) { Discard(); return; }
            if (!TryPhase(out var phase, out bool transitioning)) return;
            if (phase != ClientAppStateType.SystemLoading && phase != ClientAppStateType.Login)
            { Discard(); return; }
            if (phase != ClientAppStateType.Login || transitioning || !Application.isFocused || LoadingScreen.IsVisible)
                return;

            // Resume only the user's captured click after native startup finishes; never force a login state.
            _pending = null;
            Plugin.Log.LogInfo($"[StartupMode] resuming mode={(request.Online ? "online" : "offline")}");
            if (request.Online) request.Panel.OnPlayOnlineClicked();
            else request.Panel.OnPlayOfflineClicked();
        }
        // Observation failures preserve intent; dispatch clears it before any native side effect.
        catch (Exception ex) { Report(ex); }
    }

    private static Request Capture(LandingZonePanel panel, bool online)
    {
        if (!IsAlive(panel)) return null;
        var controller = panel.loginController;
        if (controller is null || controller.WasCollected) return null;
        var fsm = controller.FSM;
        var request = new Request
        {
            Panel = panel, Controller = controller, StateMachine = fsm,
            Button = online ? panel.playOnlineButton : panel.playOfflineButton,
            Online = online, Scene = (int)panel.gameObject.scene.handle
        };
        return StillOwned(request) ? request : null;
    }

    private static bool StillOwned(Request request)
    {
        var panel = request.Panel;
        if (!IsAlive(panel) || !panel.isActiveAndEnabled || panel._destroyed ||
            !panel.gameObject.scene.isLoaded || (int)panel.gameObject.scene.handle != request.Scene ||
            !UiVisibility.IsVisible(panel.gameObject)) return false;
        var controller = panel.loginController;
        if (controller is null || controller.WasCollected || request.Controller.WasCollected ||
            controller.Pointer != request.Controller.Pointer || !IsAlive(controller.landingZonePanel) ||
            controller.landingZonePanel.Pointer != panel.Pointer) return false;
        var fsm = controller.FSM;
        if (fsm is null || fsm.WasCollected || request.StateMachine is null || request.StateMachine.WasCollected ||
            fsm.Pointer != request.StateMachine.Pointer || fsm.CurrentState != LoginController.LoginUIState.LandingZone)
            return false;
        var button = request.Online ? panel.playOnlineButton : panel.playOfflineButton;
        return IsAlive(button) && IsAlive(request.Button) && button.Pointer == request.Button.Pointer &&
            button.isActiveAndEnabled && button.IsInteractable() && UiVisibility.IsVisible(button.gameObject) &&
            button.transform.IsChildOf(panel.transform);
    }

    private static bool TryPhase(out ClientAppStateType phase, out bool transitioning)
    {
        phase = ClientStateManager.CurrentClientAppStateType;
        transitioning = LoginTransitionState.IsTransitioning();
        return true;
    }

    private static void Discard()
    {
        _pending = null;
        Plugin.Log.LogDebug("[StartupMode] discarded: startup ownership changed");
    }

    private static void Report(Exception error)
    {
        string failure = $"{error.GetType().Name}: {error.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"[StartupMode] {error}");
    }

    internal static void Reset() { _pending = null; _lastFailure = null; }
}
