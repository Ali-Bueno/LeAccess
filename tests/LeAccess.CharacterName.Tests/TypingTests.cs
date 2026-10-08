using HeathenEngineering.UX.uGUIExtras;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Physical keys while the screen keyboard is open over a field opened with Cross.
internal static class TypingTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }

        string Edits(string typed, bool escape = false) => string.Join(",", PhysicalTypingKeys.Translate(typed, escape)
            .Select(key => key.Action == PhysicalTypingAction.Character ? key.Character.ToString() : key.Action.ToString()));
        Check(Edits("Ana") == "A,n,a", "Typed characters keep their order and case");
        Check(Edits("ñá@#") == "ñ,á,@,#", "Characters from AltGr, Shift and dead keys are typed as the layout gives them");
        Check(Edits("ab\b") == "a,b,Backspace", "Backspace erases in typing order");
        Check(Edits("a\nb") == "a,Submit" && Edits("\r") == "Submit", "Enter commits; later keys in the same frame are dropped");
        Check(Edits("\u0001\u0016\t\u007f\u001b") == "", "Ctrl shortcuts, Tab, Ctrl+Backspace and the Escape character never type");
        Check(Edits("", escape: true) == "Cancel" && Edits("a", escape: true) == "a,Cancel", "Escape closes after this frame's characters");
        Check(Edits("\n", escape: true) == "Submit", "Escape adds nothing once Enter has closed the keyboard");
        Check(Edits(null) == "" && Edits("") == "", "No typing yields no edits");

        CharacterNameKeyboard.Reset(); ScreenKeyboardTyping.Reset();
        LocalCharacterSlots.instance = null; LeAccess.Accessibility.Social.SocialWindows.Username = null;
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false; EpochInputManager.IsControllerActive = true;
        var manager = new GameObject().Add(new EpochInputManager()); EpochInputManager.instance = manager;
        var form = new GameObject().Add(new CharacterSelect()); CharacterSelect.instance = form;
        var input = form.gameObject.Child().Add(new TMP_InputField { text = "" }); form.newCharacterNameInputField = input;
        NavigationManager.instance = new GameObject().Add(new NavigationManager());
        EventSystem.current = new GameObject().Add(new EventSystem());
        var owner = form.gameObject.Add(new Navigable());
        NavigationManager.instance.Registered[110] = owner;
        var canvas = new GameObject();
        var keyboard = canvas.Add(new ScreenKeyboardManager
        {
            _keyboardGO = canvas.Child(), _substituteInputField = new GameObject().Add(new TMP_InputField()),
            _previousSelectedNavigableId = 110
        });
        keyboard._navigable = canvas.Add(new ScreenKeyboardNavigable());
        ScreenKeyboardManager.Instance = keyboard;

        void Show(TMP_InputField field)
        {
            keyboard._selectedInputField = new() { gameObject = field.gameObject };
            keyboard._selectedGameObject = keyboard._keyboardGO.Child();
            keyboard._enabled = true;
            keyboard._originalText = field.text;
            NavigationManager.currentlySelected = keyboard._navigable;
            EventSystem.current.SetSelectedGameObject(keyboard._selectedGameObject);
            owner.Selected = null;
        }
        void Open(TMP_InputField field)
        {
            EpochInputManager.IsControllerActive = true;
            CharacterNameKeyboardPatches.Submit.Prefix(field);
            Show(field);
        }
        void Frame(string typed = "", bool escape = false)
        {
            Input.inputString = typed;
            Input.Down.Clear();
            if (escape) Input.Down.Add(KeyCode.Escape);
            ScreenKeyboardTyping.Update();
        }
        bool HotkeyAllowed() { bool result = true; CharacterNameKeyboardPatches.ButtonHotkey.Postfix(ref result); return result; }

        Show(input); Frame("a");
        Check(input.text == "" && !ScreenKeyboardTyping.OwnsKeyboard && !manager.isInputFieldActive && HotkeyAllowed(),
            "A keyboard not opened with Cross leaves physical keys to the game's own device switch");

        Open(input); Frame();
        Check(ScreenKeyboardTyping.OwnsKeyboard && manager.isInputFieldActive && !HotkeyAllowed(),
            "Opening with Cross keeps controller mode, raises the game's typing flag and blocks button hotkeys");
        Frame("Al");
        Check(input.text == "Al" && keyboard._substituteInputField.text == "Al" && keyboard._enabled,
            "The first physical key already types, through the keyboard's own event path");
        Frame("\b");
        Check(input.text == "A" && keyboard.Submitted.Last().keyCode == KeyCode.Backspace, "Backspace erases the last character");
        Frame("ñá@#");
        Check(input.text == "Añá@#", "Layout characters reach the field unchanged");
        Frame("\u0001"); Frame();
        Check(input.text == "Añá@#" && keyboard._enabled, "Ctrl, Shift or Insert alone neither type nor close the keyboard");
        manager.isInputFieldActive = false; Frame();
        Check(manager.isInputFieldActive, "The typing flag is raised again after the field's own deselect clears it");

        bool flagDuringClose = true;
        keyboard.DuringClose = () => flagDuringClose = manager.isInputFieldActive;
        Frame("x\nz");
        keyboard.DuringClose = null;
        Check(input.text == "Añá@#x" && !keyboard._enabled && keyboard.LastFunction == KeyboardFunction.Submit,
            "Enter commits through the keyboard's Submit and drops keys after it");
        Check(!flagDuringClose && !manager.isInputFieldActive,
            "The typing flag returns to its earlier value before the keyboard closes and focuses the field");
        Check(owner.Selected == input && EventSystem.current.currentSelectedGameObject == input.gameObject,
            "Enter returns controller focus to the field");
        Check(ScreenKeyboardTyping.OwnsKeyboard && !HotkeyAllowed(),
            "Controller mode and hotkey blocking last until Enter is released");
        ScreenKeyboardTyping.KeyboardIdle();
        Check(!ScreenKeyboardTyping.OwnsKeyboard && HotkeyAllowed(), "With every key released the game owns the keyboard again");
        Frame("q");
        Check(input.text == "Añá@#x", "After closing, physical keys no longer reach the field");

        Open(input); keyboard._originalText = "Original"; Frame("na"); Frame(escape: true);
        Check(input.text == "Añá@#xna" && !keyboard._enabled && keyboard.LastFunction == KeyboardFunction.Submit,
            "Escape leaves like Circle, keeping the typed name");
        ScreenKeyboardTyping.KeyboardIdle();

        manager.isInputFieldActive = true;
        Open(input); Frame(); Frame("\n");
        Check(!manager.isInputFieldActive, "A flag raised before opening is not stranded once a keyboard key has focus");
        ScreenKeyboardTyping.KeyboardIdle();
        manager.isInputFieldActive = true;
        Open(input); Frame();
        EventSystem.current.SetSelectedGameObject(input.gameObject);
        NavigationManager.currentlySelected = new GameObject().Add(new Navigable());
        Frame();
        Check(manager.isInputFieldActive, "A flag the game had raised is restored while its field has focus again");
        manager.isInputFieldActive = false; ScreenKeyboardTyping.KeyboardIdle();

        Open(input); Frame("b"); keyboard.Cancel();
        Check(input.text == "Añá@#xnab" && !keyboard._enabled && !manager.isInputFieldActive,
            "Controller Circle still keeps the name and ends physical typing");
        ScreenKeyboardTyping.KeyboardIdle();

        Open(input); Frame();
        int submitted = keyboard.Submitted.Count;
        input.isFocused = true; EventSystem.current.SetSelectedGameObject(input.gameObject);
        Frame("c");
        Check(keyboard.Submitted.Count == submitted, "A natively focused editor types the key itself; it is never sent twice");
        input.isFocused = false; EventSystem.current.SetSelectedGameObject(keyboard._selectedGameObject);

        NavigationManager.currentlySelected = new GameObject().Add(new Navigable());
        string before = input.text;
        Frame("q");
        Check(input.text == before && !manager.isInputFieldActive && !HotkeyAllowed(),
            "A pop-up taking navigation ends typing; keys already down stay with the session until released");
        ScreenKeyboardTyping.KeyboardIdle();

        Open(input); Frame(); EpochInputManager.IsControllerActive = false;
        Frame("q");
        Check(input.text == before && !ScreenKeyboardTyping.OwnsKeyboard && !manager.isInputFieldActive,
            "A switch to physical mode (mouse) ends the session and its key hold");

        var ordinary = form.gameObject.Child().Add(new TMP_InputField { text = "" });
        Open(ordinary); Frame("q");
        Check(ordinary.text == "" && !ScreenKeyboardTyping.OwnsKeyboard, "Other editors keep the game's own screen keyboard");

        var username = new GameObject().Add(new TMP_InputField { text = "" });
        LeAccess.Accessibility.Social.SocialWindows.Username = username;
        Open(username); Frame("Name#12");
        Check(username.text == "Name#12", "Social username fields opened with Cross take physical keys too");
        LeAccess.Accessibility.Social.SocialWindows.Username = null;

        EpochInputManager.instance = null;
        Open(input); before = input.text; Frame("z");
        Check(input.text == before + "z", "Typing works without an input manager to raise the flag");
        EpochInputManager.instance = manager;

        Open(input); Frame(); ScreenKeyboardTyping.Reset();
        Check(!ScreenKeyboardTyping.OwnsKeyboard && !manager.isInputFieldActive && HotkeyAllowed(),
            "Unload restores the typing flag and releases the keyboard");

        CharacterNameKeyboard.Reset(); ScreenKeyboardTyping.Reset();
        keyboard._enabled = false; EpochInputManager.IsControllerActive = true;
        return checks;
    }
}
