using System;
using System.Collections.Generic;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Reads visible name-validation errors from the currently owned character form.</summary>
internal static class CharacterNameAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static readonly List<string> Errors = new();
    private static Behaviour _owner;
    private static TMP_InputField _input;
    private static CharacterNameInputValidator[] _validators;
    private static int _context;
    private static string _lastFailure;

    internal static bool HasPendingContent { get; private set; }

    /// <summary>The visible name field of the form the player currently owns, as of the last update.</summary>
    internal static TMP_InputField OwnedInput { get; private set; }

    internal static void Update()
    {
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible ||
                ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
                ControllerMenuAccessibility.HasActiveContent || !TryRead(out int context, out string text))
            { Reset(); return; }
            if (_context != context) { Tracker.Reset(); _context = context; }
            HasPendingContent = Tracker.Observe(context, text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[CharacterName] {announcement}");
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Character name feedback unavailable: {ex}");
        }
    }

    private static bool TryRead(out int context, out string text)
    {
        context = 0;
        text = null;
        var character = CharacterSelect.instance;
        if (IsAlive(character) && TryReadForm(character, character.newCharacterNameInputField,
            character.newCharacterError, out text))
        { context = character.newCharacterNameInputField.GetInstanceID(); return true; }
        var slots = LocalCharacterSlots.instance;
        if (IsAlive(slots) && TryReadForm(slots, slots.inputTextTMP, slots.errorTextTMP, out text))
        { context = slots.inputTextTMP.GetInstanceID(); return true; }
        return false;
    }

    private static bool TryReadForm(Behaviour owner, TMP_InputField input, TMP_Text error, out string text)
    {
        text = null;
        if (!owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject) || !IsAlive(input) ||
            !input.isActiveAndEnabled || !UiVisibility.IsVisible(input.gameObject) || !OwnsInput(owner, input)) return false;
        if (!IsAlive(_owner) || _owner.Pointer != owner.Pointer || !IsAlive(_input) || _input.Pointer != input.Pointer)
        {
            _owner = owner;
            _input = input;
            var validators = owner.GetComponentsInChildren<CharacterNameInputValidator>(true);
            _validators = new CharacterNameInputValidator[validators.Length];
            for (int i = 0; i < validators.Length; i++) _validators[i] = validators[i];
        }
        Errors.Clear();
        Add(error);
        foreach (var validator in _validators)
            if (IsAlive(validator) && validator.isActiveAndEnabled && IsAlive(validator.input) &&
                validator.input.Pointer == input.Pointer) Add(validator.errorUI);
        text = ScreenContentTracker.Combine(Errors);
        OwnedInput = input;
        return true;
    }

    private static bool OwnsInput(Component owner, TMP_InputField input)
    {
        var keyboard = ScreenKeyboardManager.Instance;
        if (IsAlive(keyboard) && keyboard._enabled && IsAlive(keyboard._selectedGameObject))
            return keyboard._selectedGameObject.transform.IsChildOf(input.transform) ||
                input.transform.IsChildOf(keyboard._selectedGameObject.transform);
        var navigation = NavigationManager.currentlySelected;
        var current = navigation is null || navigation.WasCollected ? null : navigation.TryCast<Behaviour>();
        if (!IsAlive(current) || !current.isActiveAndEnabled || !UiVisibility.IsVisible(current.gameObject) ||
            IsAlive(current.GetComponentInParent<MainMenuPanel>())) return false;
        var prioritized = navigation.TryCast<PrioritizedNavigable>();
        if (IsAlive(prioritized) && IsAlive(prioritized.ActiveSelectable) &&
            IsAlive(prioritized.ActiveSelectable.GetComponentInParent<MainMenuPanel>())) return false;
        return current.transform.IsChildOf(owner.transform) || input.transform.IsChildOf(current.transform);
    }

    private static void Add(TMP_Text label)
    {
        if (TextUtils.IsVisibleLabel(label)) Errors.Add(TextUtils.CleanText(label.text));
    }

    internal static void Reset()
    {
        Tracker.Reset();
        Errors.Clear();
        HasPendingContent = false;
        OwnedInput = null;
        _context = 0;
        _owner = null;
        _input = null;
        _validators = null;
    }
}
