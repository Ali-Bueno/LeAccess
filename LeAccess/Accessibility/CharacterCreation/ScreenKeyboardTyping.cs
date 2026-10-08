using System;
using HeathenEngineering.UX.uGUIExtras;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Types physical keys into the screen keyboard the player opened with Cross, without leaving controller mode.</summary>
/// <remarks>See reference/character-name-reading.md#physical-keys-on-the-open-screen-keyboard-2026-10-08.</remarks>
internal static class ScreenKeyboardTyping
{
    private static EpochInputManager _gateOwner;
    private static bool _active, _savedGate, _awaitRelease, _logged;
    private static string _lastFailure;

    /// <summary>A session, or the release of its keys, owns the keyboard: controller mode stays and button hotkeys wait.</summary>
    internal static bool OwnsKeyboard => _active || _awaitRelease;

    /// <summary>Runs after the game's Update input handling, so a closing key cannot reach it later in the frame.</summary>
    internal static void Update()
    {
        try
        {
            var keyboard = ScreenKeyboardManager._Instance_k__BackingField;
            var input = CharacterNameKeyboard.TypingTarget(keyboard);
            if (input is null)
            {
                End();
                // Once the game has left controller mode (mouse), there is no mode left to hold.
                if (!EpochInputManager.IsControllerActive) _awaitRelease = false;
                return;
            }
            Begin();
            string typed = Input.inputString;
            bool escape = Input.GetKeyDown(KeyCode.Escape);
            if (string.IsNullOrEmpty(typed) && !escape) return;
            // A natively focused editor already receives physical keys itself.
            var events = EventSystem.current;
            if (input.isFocused && IsAlive(events) && IsAlive(events.currentSelectedGameObject) &&
                events.currentSelectedGameObject.Pointer == input.gameObject.Pointer) return;
            foreach (var key in PhysicalTypingKeys.Translate(typed, escape))
                if (!Apply(keyboard, key)) break;
        }
        catch (Exception ex) { End(); ReportFailure(ex); }
    }

    private static void Begin()
    {
        if (!_active)
        {
            var manager = EpochInputManager.instance;
            _active = true;
            _logged = false;
            _gateOwner = IsAlive(manager) ? manager : null;
            _savedGate = _gateOwner is not null && _gateOwner.isInputFieldActive;
        }
        // The flag the game raises while a text field has focus: typed keys reach no chat, hotkey or gameplay action.
        if (IsAlive(_gateOwner)) _gateOwner.isInputFieldActive = true;
    }

    // Same native path as the keyboard's own keys (OnKeyPress -> SubmitEvent, OnFunctionPress for Submit/Cancel).
    private static bool Apply(ScreenKeyboardManager keyboard, PhysicalTypingKey key)
    {
        if (!_logged)
        {
            _logged = true;
            Plugin.Log.LogInfo("[CharacterKeyboard] physical keys typed through the screen keyboard");
        }
        switch (key.Action)
        {
            case PhysicalTypingAction.Character:
                keyboard.SubmitEvent(new Event { type = EventType.KeyDown, character = key.Character });
                return true;
            case PhysicalTypingAction.Backspace:
                keyboard.SubmitEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Backspace });
                return true;
            default:
                End();
                // Cancel keeps the text for these fields, exactly like Circle (CharacterNameKeyboard.BackAction).
                keyboard.OnFunctionPress(key.Action == PhysicalTypingAction.Submit ? KeyboardFunction.Submit : KeyboardFunction.Cancel);
                return false;
        }
    }

    /// <summary>Ends the session before the keyboard closes, so the field focused next sets the game's flag itself.</summary>
    internal static void End()
    {
        if (!_active) return;
        _active = false;
        _awaitRelease = true;
        var owner = _gateOwner;
        _gateOwner = null;
        // A raised flag blocks gameplay input, so it is restored only while a field still has focus.
        try { if (IsAlive(owner)) owner.isInputFieldActive = _savedGate && SelectsInputField(); }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static bool SelectsInputField()
    {
        var events = EventSystem.current;
        var selected = IsAlive(events) ? events.currentSelectedGameObject : null;
        return IsAlive(selected) && IsAlive(selected.GetComponent<TMP_InputField>());
    }

    /// <summary>The game saw no keyboard or mouse activity this frame.</summary>
    internal static void KeyboardIdle() => _awaitRelease = false;

    internal static void Reset()
    {
        End();
        _awaitRelease = false;
        _lastFailure = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Screen keyboard typing unavailable: {ex}");
    }
}
