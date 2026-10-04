using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

internal static class KeyboardTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
        CharacterNameKeyboard.Reset(); LocalCharacterSlots.instance = null;
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
        var form = new GameObject().Add(new CharacterSelect()); CharacterSelect.instance = form;
        var input = form.gameObject.Child().Add(new TMP_InputField()); form.newCharacterNameInputField = input;
        var keyboard = new GameObject().Add(new ScreenKeyboardManager
        {
            _keyboardGO = new GameObject(),
            _substituteInputField = new GameObject().Add(new TMP_InputField())
        });
        bool Select(GameObject target) => CharacterNameKeyboardPatches.SelectEditor.Prefix(keyboard, target);
        bool Enabled(bool native = true)
        { bool result = native; CharacterNameKeyboardPatches.Enable.Postfix(keyboard, ref result); return result; }
        void Click(PointerEventData.InputButton button = PointerEventData.InputButton.Left) =>
            CharacterNameKeyboardPatches.Click.Prefix(input, new PointerEventData { button = button });

        Check(!Select(input.gameObject), "Automatic focus is blocked before native setter registers keyboard navigation");
        Check(!Enabled(false), "Native disabled predicate remains false");
        Click();
        Check(Select(input.gameObject), "Controller submit's native left pointer click authorizes the first setter");
        keyboard._selectedInputField = new() { gameObject = input.gameObject };
        keyboard._selectedGameObject = input.gameObject;
        Check(Enabled(), "First update opens keyboard after the selected-input setter");
        Check(Select(keyboard._substituteInputField.gameObject) && Enabled(), "Native substitute-field focus preserves the editing session");
        Check(Select(keyboard._keyboardGO.Child()) && Enabled(), "Keyboard key navigation preserves the editing session");
        Check(Select(input.gameObject) && Enabled(), "Repeated native polling never consumes active editing permission");
        CharacterNameKeyboardPatches.Close.Postfix(keyboard, null);
        Check(!Select(input.gameObject) && !Enabled(), "Cancel/submit closes and requires a new explicit action");
        foreach (var button in new[] { PointerEventData.InputButton.Right, PointerEventData.InputButton.Middle })
        { Click(button); Check(!Select(input.gameObject), $"{button} click cannot open editing"); }
        CharacterNameKeyboardPatches.Click.Prefix(input, null);
        Check(!Select(input.gameObject), "Missing pointer event cannot activate editor");
        CharacterNameKeyboardPatches.Click.Prefix(input, new PointerEventData { WasCollected = true });
        Check(!Select(input.gameObject), "Collected pointer event is ignored safely");
        CharacterNameKeyboardPatches.Submit.Prefix(input);
        Check(Select(input.gameObject) && Enabled(), "Unity submit explicitly permits editing");
        Check(!Enabled(false), "Native mouse/keyboard mode retains its disabled screen-keyboard result");
        Check(!Select(input.gameObject), "Later controller mode cannot automatically reopen the prior session");

        var ordinary = form.gameObject.Child().Add(new TMP_InputField());
        Check(Select(ordinary.gameObject), "Other inputs retain native automatic focus behavior");
        keyboard._selectedInputField = new() { gameObject = ordinary.gameObject };
        Check(Enabled() && !Enabled(false), "Other input predicates are preserved in both directions");
        CharacterNameKeyboardPatches.Submit.Prefix(ordinary);
        Check(!Select(input.gameObject), "Submitting an unrelated editor cannot authorize character-name entry");
        keyboard._selectedInputField = new() { gameObject = input.gameObject };
        Click(); Select(input.gameObject);
        Check(Select(form.gameObject.Child()) && !Enabled(), "Moving to an ordinary control revokes name editing even if native editor pointer lingers");
        Check(Select(null) && !Select(input.gameObject), "Clearing native selection revokes editing");

        foreach (string gate in new[] { "hidden", "disabled", "read-only", "non-interactable", "unfocused", "loading" })
        {
            CharacterNameKeyboard.Reset();
            if (gate == "hidden") input.gameObject.Active = false;
            if (gate == "disabled") input.Enabled = false;
            if (gate == "read-only") input.readOnly = true;
            if (gate == "non-interactable") input.Interactable = false;
            if (gate == "unfocused") Application.isFocused = false;
            if (gate == "loading") LE.UI.LoadingScreen.IsVisible = true;
            Click();
            Check(!Select(input.gameObject) && !Enabled(), $"{gate} input cannot open keyboard");
            input.gameObject.Active = true; input.Enabled = true; input.readOnly = false; input.Interactable = true;
            Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
            Check(!Select(input.gameObject), $"Returning from {gate} requires a fresh explicit action");
        }
        Click(); Select(input.gameObject); form.gameObject.Active = false;
        Check(!Enabled(), "Closing form ends existing keyboard permission without depending on Disable event");
        form.gameObject.Active = true;
        Check(!Select(input.gameObject), "Reopening form does not reuse an old click");
        input.WasCollected = true;
        Check(!Enabled(false), "Collected input is safe while native keyboard closes"); input.WasCollected = false;

        var slots = new GameObject().Add(new LocalCharacterSlots()); LocalCharacterSlots.instance = slots;
        slots.inputTextTMP = slots.gameObject.Child().Add(new TMP_InputField());
        Check(!Select(slots.inputTextTMP.gameObject), "Local-slot character naming also requires explicit editing");
        CharacterNameKeyboardPatches.Submit.Prefix(slots.inputTextTMP);
        Check(Select(slots.inputTextTMP.gameObject), "Local-slot submit permits its own field");
        keyboard._selectedInputField = new() { gameObject = slots.inputTextTMP.gameObject };
        Check(Enabled() && !Select(input.gameObject), "Local-slot permission never opens a different character-name field");
        keyboard._selectedInputField.WasCollected = true;
        Check(Enabled(), "Collected keyboard adapter fails open to the original native predicate");
        CharacterNameKeyboard.Reset();
        Check(!Select(slots.inputTextTMP.gameObject), "Unload/reset clears editing ownership");

        LocalCharacterSlots.instance = null;
        NavigationManager.instance = new GameObject().Add(new NavigationManager());
        EventSystem.current = new GameObject().Add(new EventSystem());
        var owner = form.gameObject.Add(new Navigable());
        NavigationManager.instance.Registered[110] = owner;
        keyboard._previousSelectedNavigableId = 110;
        keyboard._navigable = keyboard._keyboardGO.Add(new ScreenKeyboardNavigable());
        var ordinaryControl = form.gameObject.Child().Add(new UnityEngine.UI.Selectable());
        void Open(bool enabled = true)
        {
            EpochInputManager.IsControllerActive = true;
            Click();
            keyboard._selectedInputField = new() { gameObject = input.gameObject };
            keyboard._selectedGameObject = keyboard._keyboardGO.Child();
            keyboard._enabled = enabled;
            NavigationManager.currentlySelected = keyboard._navigable;
            owner.Selected = ordinaryControl;
            EventSystem.current.currentSelectedGameObject = keyboard._selectedGameObject;
            keyboard._originalText = "Original";
            input.text = "Edited";
        }
        foreach (bool enabled in new[] { true, false })
        {
            Open(enabled);
            bool reopensDuringClose = false;
            keyboard.DuringClose = () => reopensDuringClose = Select(input.gameObject);
            int closed = keyboard.CloseCount;
            keyboard.Cancel();
            Check(keyboard.CloseCount == closed + 1 && keyboard.EarlyReturns == 0,
                $"Cancel completes native cleanup when enabled={enabled}, including stale-disabled state");
            Check(input.text == "Edited", "Circle returns to the form while preserving the current name");
            Check(!keyboard._enabled && keyboard._selectedInputField is null && keyboard._selectedGameObject is null,
                "Cancel releases the adapter, selected keyboard object and enabled flag together");
            Check(!reopensDuringClose && !Select(input.gameObject), "Native close callbacks cannot recapture the name editor");
            Check(ReferenceEquals(NavigationManager.currentlySelected, owner) && owner.Selected == input,
                "Native saved owner regains the specific name field instead of its default button");
            Click();
            Check(Select(input.gameObject), "A fresh explicit click can reopen after cancellation");
        }
        keyboard.DuringClose = null;
        Open(); keyboard.Disable();
        Check(input.text == "Edited" && owner.Selected == input, "Commit closes and restores focus without reverting the entered name");
        Open(); owner.Selected = input;
        int sameControlSelections = owner.SelectionCount;
        keyboard.Cancel();
        Check(owner.SelectionCount == sameControlSelections && EventSystem.current.currentSelectedGameObject == input.gameObject,
            "Same active control still regains EventSystem focus when native TrySelect short-circuits");
        Open(); Select(form.gameObject.Child());
        Check(!Enabled() && keyboard._selectedInputField is null && !keyboard._enabled,
            "Revoked permission runs native cleanup before Update can strand disabled keyboard navigation");
        Open(); EpochInputManager.IsControllerActive = false;
        Check(!Enabled(false) && keyboard._selectedInputField is null && owner.Selected == ordinaryControl,
            "Native device-mode disable cleans the editor without forcing controller focus back");
        Open(false);
        NavigationManager.currentlySelected = owner;
        EpochInputManager.IsControllerActive = false;
        int physicalCloses = keyboard.CloseCount;
        keyboard.Disable();
        Check(keyboard.CloseCount == physicalCloses && keyboard._selectedInputField is not null,
            "An inactive adapter for physical typing is never forced through virtual-keyboard close");
        keyboard.EarlyReturns = 0;
        Open(false); CharacterNameKeyboard.Reset();
        Check(!Enabled() && keyboard._selectedInputField is null,
            "Already-disabled keyboard navigation is also released during predicate polling");
        Open(false); keyboard._enabledAnimation = 0.5f;
        NavigationManager.currentlySelected = owner;
        int animatedCloses = keyboard.CloseCount;
        keyboard.Disable();
        Check(keyboard.CloseCount == animatedCloses + 1 && keyboard._selectedInputField is null,
            "A still-visible keyboard finishes native cleanup after navigation has returned to its owner");
        keyboard._enabledAnimation = 0;

        foreach (string gate in new[] { "foreign-owner", "different-selection", "foreign-event-focus", "missing-events", "missing-owner", "hidden", "disabled-owner", "excluded", "unfocused", "loading" })
        {
            Open();
            var unrelated = new GameObject().Add(new Navigable());
            if (gate == "foreign-owner") NavigationManager.instance.Registered[110] = unrelated;
            if (gate == "different-selection") keyboard.DuringClose = () => NavigationManager.currentlySelected = unrelated;
            if (gate == "foreign-event-focus") keyboard.DuringClose = () => EventSystem.current.SetSelectedGameObject(unrelated.gameObject);
            if (gate == "missing-events") EventSystem.current = null;
            if (gate == "missing-owner") NavigationManager.instance.Registered.Clear();
            if (gate == "hidden") input.gameObject.Active = false;
            if (gate == "disabled-owner") owner.Enabled = false;
            if (gate == "excluded") owner.Exclude = true;
            if (gate == "unfocused") Application.isFocused = false;
            if (gate == "loading") LE.UI.LoadingScreen.IsVisible = true;
            int selections = owner.SelectionCount;
            keyboard.Disable();
            Check(owner.SelectionCount == selections && unrelated.SelectionCount == 0,
                $"Close never steals navigation when {gate}");
            keyboard.DuringClose = null;
            NavigationManager.instance.Registered[110] = owner;
            EventSystem.current ??= new GameObject().Add(new EventSystem());
            input.gameObject.Active = owner.Enabled = Application.isFocused = true;
            owner.Exclude = LE.UI.LoadingScreen.IsVisible = false;
        }
        Open(false); keyboard._selectedInputField = new() { gameObject = ordinary.gameObject };
        int before = keyboard.CloseCount;
        keyboard.Disable();
        Check(keyboard.CloseCount == before && keyboard.EarlyReturns == 1,
            "Unrelated disabled editors retain the native early return");

        Open(); keyboard.OnCancel();
        Check(input.text == "Edited" && keyboard._selectedInputField is null,
            "Standalone native cancel also retains the name and completes cleanup");
        Open(); keyboard._selectedInputField = new() { gameObject = ordinary.gameObject };
        ordinary.text = "Changed elsewhere";
        keyboard.Cancel();
        Check(ordinary.text == "Original", "Unrelated editors retain their native cancel-and-revert behavior");

        Open();
        var keyboardCanvas = new GameObject();
        keyboard._navigable = keyboardCanvas.Add(new ScreenKeyboardNavigable());
        keyboard._keyboardGO = keyboardCanvas.Child();
        NavigationManager.currentlySelected = keyboard._navigable;
        input.text = "";
        foreach (char letter in "Alina")
        {
            Check(Select(keyboardCanvas.Child()), "Controls owned by keyboard navigation preserve permission outside the visual keyboard root");
            Check(Select(null), "Transient EventSystem null retains the current native keyboard owner");
            Check(keyboard.PressLetter(letter), "Per-key ShouldBeEnabled cannot discard a letter after internal focus changes");
        }
        Check(input.text == "Alina" && keyboard._substituteInputField.text == "Alina",
            "Consecutive controller letters reach the original editor and synchronize its display");
        keyboard.Cancel();
        Check(input.text == "Alina", "Returning from a multi-letter controller edit retains the complete name");

        keyboard._selectedInputField = null; keyboard._enabled = false;
        NavigationManager.currentlySelected = owner;
        EpochInputManager.IsControllerActive = false;
        EventSystem.current.SetSelectedGameObject(null);
        input.ActivationPending = false;
        Check(!CharacterNameKeyboardPatches.Click.Prefix(input, new PointerEventData { button = PointerEventData.InputButton.Right }) &&
            !input.ActivationPending, "A right click replays the native no-op and never runs the detoured original");
        var leftClick = new PointerEventData { button = PointerEventData.InputButton.Left };
        Check(!CharacterNameKeyboardPatches.Click.Prefix(input, leftClick), "A left click is replayed without the detoured original");
        Check(EventSystem.current.currentSelectedGameObject == input.gameObject && input.ActivationPending,
            "Mouse click explicitly focuses and activates the actual TMP field for physical typing");
        int physicalCloseCount = keyboard.CloseCount;
        Check(!Select(input.gameObject), "Physical name input never registers the virtual-keyboard adapter");
        Check(!Enabled(false) && keyboard.CloseCount == physicalCloseCount,
            "Physical typing cannot be deactivated by a virtual-keyboard predicate");
        EpochInputManager.IsControllerActive = true;
        Check(!Select(input.gameObject), "Switching back to controller mode cannot reuse the physical click");
        Click(); Check(Select(input.gameObject), "Fresh controller activation remains available after physical typing");

        ScreenKeyboardManager.Instance = keyboard;
        Open();
        input.text = "Alna";
        input.caretPosition = input.stringPosition = 2;
        input.m_ShouldActivateNextUpdate = true;
        CharacterNameKeyboardPatches.DeviceMode.Prefix(false, out var transfer);
        Check(transfer is not null, "An explicit active name editor is captured before native device cleanup");
        EpochInputManager.IsControllerActive = false;
        keyboard.Disable();
        EventSystem.current.SetSelectedGameObject(null);
        CharacterNameKeyboardPatches.DeviceMode.Postfix(transfer);
        Check(input.isFocused && !input.m_ShouldActivateNextUpdate && !input.m_isSelectAll && input.m_OnFocusSelectAll,
            "Physical transfer immediately activates TMP without pending select-all or changing its focus preference");
        Check(input.text == "Alna" && input.caretPosition == 2 && input.stringPosition == 2,
            "Native device cleanup preserves the name and restoration preserves the insertion point");
        Check(input.PhysicalType('i') && input.text == "Alina",
            "Physical input processed after the mode setter inserts once at the restored caret");
        keyboard.ResumePendingCursor(2);
        Check(input.caretPosition == 3 && input.stringPosition == 3 && !input.m_ShouldActivateNextUpdate,
            "A pending native cursor coroutine cannot rewind physical typing after adapter cleanup");
        Check(!Select(input.gameObject), "Physical transfer cannot register the screen keyboard again");

        foreach (bool stillEnabled in new[] { true, false })
        {
            Open();
            CharacterNameKeyboardPatches.DeviceMode.Prefix(false, out transfer);
            EpochInputManager.IsControllerActive = false;
            keyboard._enabled = stillEnabled;
            input.isFocused = false;
            CharacterNameKeyboardPatches.DeviceMode.Postfix(transfer);
            Check(!input.isFocused, "Physical focus cannot resume while a stale keyboard adapter can still move its cursor");
        }

        foreach (string gate in new[] { "hidden", "readonly", "loading", "unfocused", "foreign-focus", "foreign-navigation", "controller-again", "replaced-input" })
        {
            Open();
            CharacterNameKeyboardPatches.DeviceMode.Prefix(false, out transfer);
            Check(transfer is not null, $"Transfer setup captures active name before {gate}");
            EpochInputManager.IsControllerActive = false;
            keyboard.Disable();
            var foreign = new GameObject().Add(new Navigable());
            if (gate == "hidden") input.gameObject.Active = false;
            if (gate == "readonly") input.readOnly = true;
            if (gate == "loading") LE.UI.LoadingScreen.IsVisible = true;
            if (gate == "unfocused") Application.isFocused = false;
            if (gate == "foreign-focus") EventSystem.current.SetSelectedGameObject(foreign.gameObject);
            if (gate == "foreign-navigation") NavigationManager.currentlySelected = foreign;
            if (gate == "controller-again") EpochInputManager.IsControllerActive = true;
            if (gate == "replaced-input") form.newCharacterNameInputField = ordinary;
            CharacterNameKeyboardPatches.DeviceMode.Postfix(transfer);
            Check(!input.isFocused, $"Physical transfer cannot steal focus after {gate}");
            input.gameObject.Active = Application.isFocused = true;
            input.readOnly = LE.UI.LoadingScreen.IsVisible = false;
            form.newCharacterNameInputField = input;
        }
        Open(); CharacterNameKeyboard.Reset();
        CharacterNameKeyboardPatches.DeviceMode.Prefix(false, out transfer);
        Check(transfer is null, "Automatic name selection cannot trigger physical-focus recovery without a click");
        Open();
        CharacterNameKeyboardPatches.DeviceMode.Prefix(true, out transfer);
        Check(transfer is null, "Switching to controller mode never forces physical text focus");
        Open(); NavigationManager.currentlySelected = new GameObject().Add(new Navigable());
        CharacterNameKeyboardPatches.DeviceMode.Prefix(false, out transfer);
        Check(transfer is null, "A background keyboard cannot capture ownership from a pause or different menu");
        Open(); EventSystem.current.SetSelectedGameObject(new GameObject());
        CharacterNameKeyboardPatches.DeviceMode.Prefix(false, out transfer);
        Check(transfer is null, "Foreign EventSystem focus prevents capturing a background name editor");
        CharacterNameKeyboard.Reset();
        return checks;
    }
}
