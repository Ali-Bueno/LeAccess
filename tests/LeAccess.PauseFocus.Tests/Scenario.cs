using LeAccess.Accessibility.CharacterCreation;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class Scenario
{
    public readonly CharacterSelect Character;
    public readonly Navigable Owner;
    public readonly TMP_InputField Input;
    public readonly Navigable PauseNavigation;
    public readonly MainMenuPanel Pause;
    public readonly PanelSystem System;
    public readonly ScreenKeyboardManager Keyboard;
    public readonly ScreenKeyboardNavigable KeyboardNavigation;
    public PauseMenuFocusRecovery.Snapshot Closing;

    public Scenario()
    {
        PauseMenuFocusRecovery.Reset();
        PanelSystem.Restored.Clear(); PanelSystem.Events.Clear();
        PanelSystem.NativeSelects = true; PanelSystem.DuringRestore = null;
        ControllerMapsManager.Updates = 0; NativeObject.InvalidAccesses = 0;
        LeAccess.Plugin.Log.Messages.Clear();
        Time.frameCount = 10; Application.isFocused = true;
        LE.UI.LoadingScreen.IsVisible = false; EpochInputManager.IsControllerActive = true;
        SceneManager.Current = new Scene { handle = 1, isLoaded = true };
        Character = CharacterSelect.instance = new GameObject().Add(new CharacterSelect());
        Owner = Character.gameObject.Child().Add(new Navigable());
        Input = Owner.gameObject.Child().Add(new TMP_InputField());
        Owner._activeSelectable = Input;
        NavigationManager.instance = new GameObject().Add(new NavigationManager());
        NavigationManager.instance.Registry[42] = Owner;
        NavigationManager.currentlySelected = Owner;
        UIBase.instance = new GameObject().Add(new UIBase());
        KeyboardNavigation = new GameObject().Add(new ScreenKeyboardNavigable());
        Keyboard = ScreenKeyboardManager._Instance_k__BackingField = new GameObject().Add(new ScreenKeyboardManager
        { _previousSelectedNavigableId = 42, _selectedGameObject = Input.gameObject });
        Pause = new GameObject().Add(new MainMenuPanel());
        PauseNavigation = Pause.gameObject.Child().Add(new Navigable());
        System = new GameObject().Add(new PanelSystem());
    }

    public void Open(bool withKeyboard = true)
    {
        NavigationManager.currentlySelected = withKeyboard ? KeyboardNavigation : Owner;
        PauseMenuFocusRecovery.Update();
        PauseMenuFocusRecovery.Capture(Pause, System);
        NavigationManager.currentlySelected = PauseNavigation;
        UIBase.instance.menuOpen = System.IsAnyPanelOpen = true;
    }

    public void Close(bool success = true)
    {
        Closing = PauseMenuFocusRecovery.BeginClose(System, Pause, false);
        if (success)
        {
            NavigationManager.currentlySelected = null;
            UIBase.instance.menuOpen = System.IsAnyPanelOpen = false;
        }
        PauseMenuFocusRecovery.CompleteClose(Closing, success);
    }

    public static void Frame() { Time.frameCount++; PauseMenuFocusRecovery.Update(); }
    public static int RestoreCount => PanelSystem.Restored.Count;
}
