using LE.UI.Login.UnityUI;
using LeAccess.Accessibility;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Login;
using LeAccess.Accessibility.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// First-login display-name and email forms behave like the character-name field.
internal static class LoginTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new Exception("Login: " + message); }
        foreach (bool email in new[] { false, true })
        {
            string kind = email ? "email" : "display-name";
            CharacterNameKeyboard.Reset(); CharacterNameAccessibility.Reset();
            CharacterSelect.instance = null; LocalCharacterSlots.instance = null;
            LeAccess.Accessibility.Social.SocialWindows.Username = null;
            ScreenKeyboardManager.Instance = null; EpochInputManager.IsControllerActive = true;
            Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
            ScreenReader.Messages.Clear(); ScreenReader.SpokeThisFrame = false;

            var root = new GameObject();
            var input = root.Child().Add(new TMP_InputField());
            var header = root.Child().Add(new LoginHeader());
            header.label = header.gameObject.Child().Add(new TMP_Text { text = "<b>Name taken</b>" });
            var submit = root.Child().Add(new UnityEngine.UI.Button());
            Behaviour panel;
            if (email) panel = root.Add(new EmailPromptPanel { emailText = input, submitButton = submit, errorHeader = header });
            else panel = root.Add(new DisplayNamePromptPanel { displayNameText = input, submitButton = submit, errorHeader = header });

            var keyboard = new GameObject().Add(new ScreenKeyboardManager
            { _keyboardGO = new GameObject(), _substituteInputField = new GameObject().Add(new TMP_InputField()) });
            bool Select(GameObject target) => CharacterNameKeyboardPatches.SelectEditor.Prefix(keyboard, target);
            bool Enabled() { bool result = true; CharacterNameKeyboardPatches.Enable.Postfix(keyboard, ref result); return result; }
            void Click() => CharacterNameKeyboardPatches.Click.Prefix(input, new PointerEventData { button = PointerEventData.InputButton.Left });

            Check(LoginPrompts.IsPromptInput(input), $"{kind} input is recognised");
            Check(!LoginPrompts.IsPromptInput(root.Child().Add(new TMP_InputField())), $"{kind} form ignores a foreign input");
            Check(!Select(input.gameObject), $"{kind} selection alone does not open the screen keyboard");
            CharacterNameKeyboardPatches.Submit.Prefix(input);
            Check(Select(input.gameObject), $"{kind} submit requests editing");
            keyboard._selectedInputField = new() { gameObject = input.gameObject };
            keyboard._selectedGameObject = input.gameObject;
            Check(Enabled(), $"{kind} keyboard stays open while authorised");
            CharacterNameKeyboardPatches.Close.Postfix(keyboard, null);
            Check(!Select(input.gameObject), $"{kind} needs a new Cross after closing");

            NavigationManager.instance = new GameObject().Add(new NavigationManager());
            EventSystem.current = new GameObject().Add(new EventSystem());
            var owner = panel.gameObject.Add(new Navigable());
            NavigationManager.instance.Registered[7] = owner;
            keyboard._previousSelectedNavigableId = 7;
            keyboard._navigable = keyboard._keyboardGO.Add(new ScreenKeyboardNavigable());
            Click();
            keyboard._selectedInputField = new() { gameObject = input.gameObject };
            keyboard._selectedGameObject = keyboard._keyboardGO.Child();
            keyboard._enabled = true;
            NavigationManager.currentlySelected = keyboard._navigable;
            EventSystem.current.currentSelectedGameObject = keyboard._selectedGameObject;
            keyboard._originalText = "before"; input.text = "typed";
            keyboard.Cancel();
            Check(input.text == "typed" && !keyboard._enabled, $"{kind} Back keeps the text and closes the keyboard");
            Check(ReferenceEquals(NavigationManager.currentlySelected, owner) && owner.Selected == input &&
                EventSystem.current.currentSelectedGameObject == input.gameObject, $"{kind} focus returns to the field");

            // Error reading: via the navigation active selectable and via the keyboard editor.
            var navigable = panel.gameObject.Child().Add(new PrioritizedNavigable { ActiveSelectable = input });
            NavigationManager.currentlySelected = navigable;
            void Settle() { for (int i = 0; i < 3; i++) { ScreenReader.SpokeThisFrame = false; CharacterNameAccessibility.Update(); } }
            Settle();
            Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0] == ("Name taken", true),
                $"{kind} error read through navigation, cleaned");
            Check(ReferenceEquals(CharacterNameAccessibility.OwnedInput, input), $"{kind} owns the input so the echo works");
            Settle(); Check(ScreenReader.Messages.Count == 1, $"{kind} unchanged error is not repeated");

            CharacterNameAccessibility.Reset(); ScreenReader.Messages.Clear();
            header.label.gameObject.Active = false; Settle();
            Check(ScreenReader.Messages.Count == 0, $"{kind} hidden error is silent");
            header.label.gameObject.Active = true;

            NavigationManager.currentlySelected = new GameObject().Add(new PrioritizedNavigable { ActiveSelectable = input });
            CharacterNameAccessibility.Reset(); Settle();
            Check(ScreenReader.Messages.Count == 0, $"{kind} form outside the focused navigation is ignored");

            NavigationManager.currentlySelected = null;
            ScreenKeyboardManager.Instance = new GameObject().Add(new ScreenKeyboardManager
            { _enabled = true, _selectedGameObject = input.gameObject.Child() });
            CharacterNameAccessibility.Reset(); Settle();
            Check(ScreenReader.Messages.Count == 1 && ReferenceEquals(CharacterNameAccessibility.OwnedInput, input),
                $"{kind} error read through the keyboard editor");
            ScreenKeyboardManager.Instance = new GameObject().Add(new ScreenKeyboardManager
            { _enabled = true, _selectedGameObject = new GameObject() });
            CharacterNameAccessibility.Reset(); ScreenReader.Messages.Clear(); Settle();
            Check(ScreenReader.Messages.Count == 0, $"{kind} foreign keyboard target is ignored");
            ScreenKeyboardManager.Instance = null;
        }
        return checks;
    }
}
