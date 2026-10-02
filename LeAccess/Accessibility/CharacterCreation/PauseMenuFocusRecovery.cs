using System;
using LeAccess.Accessibility.Announcements;
using LE.UI.PanelSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

internal static class PauseMenuFocusRecovery
{
    private static Snapshot _latest, _bound, _pending;
    private static int _closedFrame;
    private static string _lastFailure;

    internal static void Update()
    {
        try
        {
            if (!TryGetContext(out var character)) { Clear(); return; }
            if (_latest != null && !SameSession(_latest, character)) _latest = null;
            if (_bound != null && !SameSession(_bound, character)) _bound = null;
            if (_pending != null && Time.frameCount > _closedFrame)
            {
                var pending = _pending;
                _pending = null;
                Restore(pending, character);
            }
            var ui = UIBase.instance;
            if (IsAlive(ui) && !ui.menuOpen) _latest = Observe(character) ?? _latest;
        }
        catch (Exception ex) { Clear(); ReportFailure(ex); }
    }

    internal static void Capture(MainMenuPanel panel, PanelSystem system)
    {
        try
        {
            _pending = null;
            if (!IsAlive(panel) || !IsAlive(system) || !TryGetContext(out var character)) { Clear(); return; }
            if (_bound != null && Same(_bound.Panel, panel) && Same(_bound.System, system)) return;
            var snapshot = Observe(character);
            if (snapshot == null && _latest != null && SameSession(_latest, character) && OwnerAvailable(_latest))
                snapshot = _latest;
            _latest = null;
            _bound = snapshot;
            if (snapshot == null) return;
            snapshot.Panel = panel;
            snapshot.System = system;
            Plugin.Log.LogInfo($"[PauseFocus] captured owner={snapshot.Owner.GetInstanceID()} state={snapshot.State} scene={snapshot.Scene}");
        }
        catch (Exception ex) { Clear(); ReportFailure(ex); }
    }

    internal static Snapshot BeginClose(PanelSystem system, Panel panel, bool isPreloading)
    {
        try
        {
            return !isPreloading && _bound != null && Same(_bound.Panel, panel) && Same(_bound.System, system)
                ? _bound : null;
        }
        catch (Exception ex) { Clear(); ReportFailure(ex); return null; }
    }

    internal static void CompleteClose(Snapshot snapshot, bool closed)
    {
        if (!closed || snapshot == null || !ReferenceEquals(snapshot, _bound)) return;
        _bound = _latest = null;
        _pending = snapshot;
        _closedFrame = Time.frameCount;
    }

    private static Snapshot Observe(CharacterSelect character)
    {
        var navigation = NavigationManager.currentlySelected;
        if (navigation is null || navigation.WasCollected) return null;
        GameObject editor = null;
        if (IsAlive(navigation.TryCast<ScreenKeyboardNavigable>()))
        {
            var keyboard = ScreenKeyboardManager._Instance_k__BackingField;
            var manager = NavigationManager.instance;
            if (!IsAlive(keyboard) || !keyboard._enabled || !IsAlive(manager)) return null;
            editor = keyboard._selectedGameObject;
            if (!UiVisibility.IsVisible(editor) ||
                !manager.GetNavigableFromID(keyboard._previousSelectedNavigableId, out navigation) ||
                navigation is null || navigation.WasCollected) return null;
        }
        var owner = navigation.TryCast<Behaviour>();
        if (!IsAlive(owner) || !owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject) ||
            !owner.gameObject.scene.isLoaded ||
            (int)owner.gameObject.scene.handle != (int)character.gameObject.scene.handle ||
            IsAlive(navigation.TryCast<ScreenKeyboardNavigable>()) ||
            IsAlive(owner.GetComponentInParent<MainMenuPanel>()) ||
            editor is not null && !editor.transform.IsChildOf(owner.transform)) return null;
        var navigable = navigation.TryCast<Navigable>();
        return new Snapshot
        {
            Navigation = navigation, Owner = owner,
            Control = editor is not null ? editor.GetComponent<Selectable>() :
                IsAlive(navigable) ? navigable._activeSelectable : null,
            Character = character, State = character.currentState,
            Scene = (int)SceneManager.GetActiveScene().handle
        };
    }

    private static bool TryGetContext(out CharacterSelect character)
    {
        character = CharacterSelect.instance;
        return Application.isFocused && !LE.UI.LoadingScreen.IsVisible && EpochInputManager.IsControllerActive &&
            IsAlive(character) && character.isActiveAndEnabled && character.gameObject.scene.isLoaded &&
            character.currentState != CharacterSelect.CharacterSelectState.None && SceneManager.GetActiveScene().isLoaded;
    }

    private static bool SameSession(Snapshot snapshot, CharacterSelect character) =>
        Same(snapshot.Character, character) && character.currentState == snapshot.State &&
        (int)SceneManager.GetActiveScene().handle == snapshot.Scene && SceneManager.GetActiveScene().isLoaded;

    private static bool OwnerAvailable(Snapshot snapshot) =>
        snapshot.Navigation is not null && !snapshot.Navigation.WasCollected && IsAlive(snapshot.Owner) &&
        IsAlive(snapshot.Character) &&
        (int)snapshot.Owner.gameObject.scene.handle == (int)snapshot.Character.gameObject.scene.handle &&
        snapshot.Owner.isActiveAndEnabled && snapshot.Owner.gameObject.scene.isLoaded &&
        UiVisibility.IsVisible(snapshot.Owner.gameObject);

    private static void Restore(Snapshot snapshot, CharacterSelect character)
    {
        var ui = UIBase.instance;
        if (!SameSession(snapshot, character) || !OwnerAvailable(snapshot) ||
            NavigationManager.currentlySelected is not null || !IsAlive(snapshot.System) ||
            snapshot.System.IsAnyPanelOpen || !IsAlive(ui) || ui.menuOpen)
        { Plugin.Log.LogDebug("[PauseFocus] discarded: context or ownership changed"); return; }
        var opening = snapshot.System._openingPanels;
        if (opening is not null && (opening.WasCollected || opening.Count > 0))
        { Plugin.Log.LogDebug("[PauseFocus] discarded: another panel is opening"); return; }

        PanelSystem.RestoreFocus(snapshot.Navigation);
        ControllerMapsManager.UpdateMap();
        var selected = NavigationManager.currentlySelected;
        if (selected is null || selected.WasCollected || selected.Pointer != snapshot.Navigation.Pointer)
        { Plugin.Log.LogDebug("[PauseFocus] native restoration did not select saved owner"); return; }
        var owner = selected.TryCast<Navigable>();
        var control = snapshot.Control;
        if (IsAlive(owner) && IsAlive(control) && control.isActiveAndEnabled && control.IsInteractable() &&
            UiVisibility.IsVisible(control.gameObject) && control.transform.IsChildOf(owner.transform) &&
            !owner.ShouldExcludeSelectable(control) && !Same(owner._activeSelectable, control))
            owner.TrySelect(control);
        Plugin.Log.LogInfo($"[PauseFocus] restored owner={snapshot.Owner.GetInstanceID()} state={snapshot.State}");
    }

    private static bool Same(UnityEngine.Object first, UnityEngine.Object second) =>
        IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;

    private static void Clear() { _latest = _bound = _pending = null; }
    internal static void Reset() { Clear(); _lastFailure = null; }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Pause menu focus recovery unavailable: {ex.Message}");
    }

    internal sealed class Snapshot
    {
        internal INavigable Navigation;
        internal Behaviour Owner;
        internal Selectable Control;
        internal CharacterSelect Character;
        internal CharacterSelect.CharacterSelectState State;
        internal int Scene;
        internal MainMenuPanel Panel;
        internal PanelSystem System;
    }
}
