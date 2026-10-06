using System;
using HeathenEngineering.UX.uGUIExtras;
using LeAccess.Accessibility.Announcements;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Requires an explicit submit or click before the screen keyboard takes focus on the character-name
/// field or a Social username field.</summary>
internal static class CharacterNameKeyboard
{
    private static TMP_InputField _requestedInput;
    private static bool _controllerRequest;
    private static string _lastFailure;

    internal static void RequestEditing(TMP_InputField input)
    {
        try
        {
            if (IsNameInput(input) && CanEdit(input))
            {
                _requestedInput = input;
                _controllerRequest = EpochInputManager.IsControllerActive;
            }
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static bool AllowSelection(ScreenKeyboardManager keyboard, GameObject target)
    {
        try
        {
            if (!IsAlive(keyboard)) return true;
            if (IsAlive(target) && (IsAlive(keyboard._substituteInputField) &&
                target.Pointer == keyboard._substituteInputField.gameObject.Pointer ||
                IsAlive(keyboard._keyboardGO) && target.transform.IsChildOf(keyboard._keyboardGO.transform) ||
                IsAlive(keyboard._navigable) && target.transform.IsChildOf(keyboard._navigable.transform))) return true;
            // EventSystem may briefly clear selection between keys; navigation still owns the edit.
            if (!IsAlive(target) && OwnsNavigation(keyboard) && Requested(GetNameEditor(keyboard))) return true;
            var input = IsAlive(target) ? target.GetComponent<TMP_InputField>() : null;
            if (!IsNameInput(input)) { Reset(); return true; }
            // The native setter registers keyboard navigation before evaluating ShouldBeEnabled.
            return EpochInputManager.IsControllerActive && Requested(input);
        }
        catch (Exception ex) { ReportFailure(ex); return true; }
    }

    internal static bool FilterEnabled(ScreenKeyboardManager keyboard, bool nativeResult)
    {
        try
        {
            if (!IsAlive(keyboard)) { Reset(); return nativeResult; }
            var input = GetNameEditor(keyboard);
            if (!IsAlive(input)) { Reset(); return nativeResult; }
            if (nativeResult && Requested(input)) return true;
            Reset();
            // Update only flips _enabled; it does not release the editor or keyboard navigation.
            if (keyboard._enabled || OwnsNavigation(keyboard)) keyboard.Disable();
            return false;
        }
        catch (Exception ex) { ReportFailure(ex); return nativeResult; }
    }

    internal static void CompleteClick(TMP_InputField input)
    {
        try
        {
            if (EpochInputManager.IsControllerActive || !IsNameInput(input) || !CanEdit(input)) return;
            var events = EventSystem.current;
            if (!IsAlive(events)) return;
            events.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static KeyboardFunction BackAction(ScreenKeyboardManager keyboard, KeyboardFunction action)
    {
        try
        {
            if (action == KeyboardFunction.Cancel && IsAlive(keyboard) && CanEdit(GetNameEditor(keyboard)))
                return KeyboardFunction.Submit;
        }
        catch (Exception ex) { ReportFailure(ex); }
        return action;
    }

    internal static bool AllowCancel(ScreenKeyboardManager keyboard)
    {
        if (BackAction(keyboard, KeyboardFunction.Cancel) != KeyboardFunction.Submit) return true;
        try { keyboard.OnFunctionPress(KeyboardFunction.Submit); return false; }
        catch (Exception ex) { ReportFailure(ex); return true; }
    }

    internal static PhysicalTransfer CapturePhysicalTransfer(bool controllerActive)
    {
        try
        {
            if (controllerActive || !EpochInputManager.IsControllerActive) return null;
            var keyboard = ScreenKeyboardManager._Instance_k__BackingField;
            if (!IsAlive(keyboard) || !(keyboard._enabled || OwnsNavigation(keyboard))) return null;
            var input = GetNameEditor(keyboard);
            if (!Requested(input)) return null;
            var events = EventSystem.current;
            if (!IsAlive(events)) return null;
            var selected = events.currentSelectedGameObject;
            if (IsAlive(selected) && selected.Pointer != input.gameObject.Pointer &&
                !(IsAlive(keyboard._navigable) && selected.transform.IsChildOf(keyboard._navigable.transform))) return null;
            var navigation = NavigationManager.currentlySelected;
            if (navigation is not null && !OwnsNavigation(keyboard))
            {
                var owner = navigation.WasCollected ? null : navigation.TryCast<Behaviour>();
                if (!IsAlive(owner) || !input.transform.IsChildOf(owner.transform)) return null;
            }
            return new PhysicalTransfer
            {
                Input = input, Keyboard = keyboard,
                Caret = input.caretPosition, StringPosition = input.stringPosition
            };
        }
        catch (Exception ex) { ReportFailure(ex); return null; }
    }

    internal static void CompletePhysicalTransfer(PhysicalTransfer transfer)
    {
        if (transfer is null) return;
        try
        {
            var input = transfer.Input;
            if (EpochInputManager.IsControllerActive || !IsNameInput(input) || !CanEdit(input) ||
                !IsAlive(transfer.Keyboard) || transfer.Keyboard._enabled || transfer.Keyboard._selectedInputField is not null) return;
            var events = EventSystem.current;
            if (!IsAlive(events)) return;
            var selected = events.currentSelectedGameObject;
            if (IsAlive(selected) && selected.Pointer != input.gameObject.Pointer &&
                !(IsAlive(transfer.Keyboard) && IsAlive(transfer.Keyboard._navigable) &&
                selected.transform.IsChildOf(transfer.Keyboard._navigable.transform))) return;
            var navigation = NavigationManager.currentlySelected;
            if (navigation is not null)
            {
                var owner = navigation.WasCollected ? null : navigation.TryCast<Behaviour>();
                if (!IsAlive(owner) || !input.transform.IsChildOf(owner.transform)) return;
            }

            Reset();
            bool selectAll = input.m_OnFocusSelectAll;
            try
            {
                input.m_OnFocusSelectAll = false;
                events.SetSelectedGameObject(input.gameObject);
                // ActivateInputField alone waits for LateUpdate and can miss the first physical key.
                input.ActivateInputFieldInternal();
                input.m_ShouldActivateNextUpdate = false;
                input.caretPosition = transfer.Caret;
                input.stringPosition = Math.Clamp(transfer.StringPosition, 0, input.text?.Length ?? 0);
                input.m_isSelectAll = false;
                input.ForceLabelUpdate();
            }
            finally { if (IsAlive(input)) input.m_OnFocusSelectAll = selectAll; }
            Plugin.Log.LogInfo("[CharacterKeyboard] transferred the name field to physical typing");
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal sealed class PhysicalTransfer
    {
        internal TMP_InputField Input;
        internal ScreenKeyboardManager Keyboard;
        internal int Caret, StringPosition;
    }

    internal static TMP_InputField BeginClose(ScreenKeyboardManager keyboard)
    {
        Reset();
        try
        {
            if (!IsAlive(keyboard)) return null;
            var input = GetNameEditor(keyboard);
            if (!IsAlive(input)) return null;
            // Native Disable otherwise returns without cleaning up an already-disabled editor.
            if (!keyboard._enabled && IsAlive(NavigationManager.instance) && IsAlive(keyboard._navigable) &&
                (keyboard._enabledAnimation > 0 || OwnsNavigation(keyboard)))
                keyboard._enabled = true;
            return input;
        }
        catch (Exception ex) { ReportFailure(ex); return null; }
    }

    private static TMP_InputField GetNameEditor(ScreenKeyboardManager keyboard)
    {
        var editor = keyboard._selectedInputField;
        var go = editor is null || editor.WasCollected ? null : editor.gameObject;
        var input = IsAlive(go) ? go.GetComponent<TMP_InputField>() : null;
        return IsNameInput(input) ? input : null;
    }

    private static bool Requested(TMP_InputField input) => _controllerRequest && CanEdit(input) &&
        IsAlive(_requestedInput) && _requestedInput.Pointer == input.Pointer;

    internal static void CompleteClose(ScreenKeyboardManager keyboard, TMP_InputField input)
    {
        Reset();
        try
        {
            if (!IsNameInput(input) || !CanEdit(input) || !EpochInputManager.IsControllerActive ||
                !IsAlive(keyboard) || keyboard._enabled || keyboard._selectedInputField is not null) return;
            var manager = NavigationManager.instance;
            if (!IsAlive(manager) || !manager.GetNavigableFromID(keyboard._previousSelectedNavigableId, out var navigation) ||
                navigation is null || navigation.WasCollected) return;
            var current = NavigationManager.currentlySelected;
            var owner = navigation.TryCast<Navigable>();
            if (current is null || current.WasCollected || current.Pointer != navigation.Pointer ||
                !IsAlive(owner) || !owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject) ||
                !input.transform.IsChildOf(owner.transform) || owner.ShouldExcludeSelectable(input)) return;
            var events = EventSystem.current;
            if (!IsAlive(events)) return;
            var selected = events.currentSelectedGameObject;
            if (IsAlive(selected) && !selected.transform.IsChildOf(owner.transform) &&
                !(IsAlive(keyboard._keyboardGO) && selected.transform.IsChildOf(keyboard._keyboardGO.transform))) return;
            owner.TrySelect(input);
            // TrySelect does nothing if the navigable already remembers this same control.
            if (!IsAlive(events.currentSelectedGameObject) || events.currentSelectedGameObject.Pointer != input.gameObject.Pointer)
                events.SetSelectedGameObject(input.gameObject);
            if (IsAlive(events.currentSelectedGameObject) && events.currentSelectedGameObject.Pointer == input.gameObject.Pointer)
                Plugin.Log.LogInfo("[CharacterKeyboard] closed; returned to the name field");
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static bool OwnsNavigation(ScreenKeyboardManager keyboard)
    {
        var navigation = NavigationManager.currentlySelected;
        return IsAlive(keyboard._navigable) && navigation is { WasCollected: false } &&
            navigation.Pointer == keyboard._navigable.Pointer;
    }

    private static bool CanEdit(TMP_InputField input) => Application.isFocused && !LE.UI.LoadingScreen.IsVisible &&
        IsAlive(input) && input.isActiveAndEnabled && input.IsInteractable() && !input.readOnly &&
        UiVisibility.IsVisible(input.gameObject);

    private static bool IsNameInput(TMP_InputField input)
    {
        if (!IsAlive(input)) return false;
        var character = CharacterSelect.instance;
        if (IsAlive(character) && IsAlive(character.newCharacterNameInputField) &&
            character.newCharacterNameInputField.Pointer == input.Pointer) return true;
        var slots = LocalCharacterSlots.instance;
        if (IsAlive(slots) && IsAlive(slots.inputTextTMP) && slots.inputTextTMP.Pointer == input.Pointer) return true;
        return Social.SocialWindows.TryGetField(input.gameObject, out var username, out _) && username.Pointer == input.Pointer;
    }

    internal static void Reset() { _requestedInput = null; _controllerRequest = false; }

    private static void ReportFailure(Exception ex)
    {
        Reset();
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Character name keyboard guard unavailable: {ex}");
    }
}
