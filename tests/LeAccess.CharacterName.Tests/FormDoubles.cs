internal sealed class NavigationManager : UnityEngine.Behaviour
{
    internal static NativeObject currentlySelected;
    internal static NavigationManager instance;
    internal readonly Dictionary<int, NativeObject> Registered = new();
    internal bool GetNavigableFromID(int id, out NativeObject navigation) => Registered.TryGetValue(id, out navigation);
}
internal class Navigable : UnityEngine.Behaviour
{
    internal UnityEngine.UI.Selectable Selected;
    internal bool Exclude;
    internal int SelectionCount;
    internal bool ShouldExcludeSelectable(UnityEngine.UI.Selectable control) => Exclude;
    internal void TrySelect(UnityEngine.UI.Selectable control)
    {
        if (Selected == control) return;
        Selected = control; SelectionCount++;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(control.gameObject);
    }
}
internal sealed class PrioritizedNavigable : Navigable
{ internal UnityEngine.UI.Selectable ActiveSelectable; }
internal sealed class ScreenKeyboardNavigable : Navigable { }
internal sealed class EpochInputManager : UnityEngine.Behaviour
{
    internal static bool IsControllerActive = true;
    internal static EpochInputManager instance;
    internal bool isInputFieldActive;
}
internal sealed class ButtonInputListener : UnityEngine.Behaviour { internal bool CanBePressed() => true; }
internal sealed class CharacterSelect : UnityEngine.Behaviour
{
    internal static CharacterSelect instance;
    internal TMPro.TMP_InputField newCharacterNameInputField;
    internal TMPro.TMP_Text newCharacterError;
}
internal sealed class LocalCharacterSlots : UnityEngine.Behaviour
{
    internal static LocalCharacterSlots instance;
    internal TMPro.TMP_InputField inputTextTMP;
    internal TMPro.TMP_Text errorTextTMP;
}
internal sealed class CharacterNameInputValidator : UnityEngine.Behaviour
{ internal TMPro.TMP_InputField input; internal TMPro.TMP_Text errorUI; }
internal sealed class ScreenKeyboardManager : UnityEngine.Behaviour
{
    internal static ScreenKeyboardManager Instance;
    internal static ScreenKeyboardManager _Instance_k__BackingField => Instance;
    internal bool _enabled;
    internal float _enabledAnimation;
    internal UnityEngine.GameObject _selectedGameObject;
    internal TMPro.TMP_InputField _substituteInputField;
    internal UnityEngine.GameObject _keyboardGO;
    internal IKeyboardInputField _selectedInputField;
    internal ScreenKeyboardNavigable _navigable;
    internal int _previousSelectedNavigableId;
    internal int CloseCount, EarlyReturns;
    internal Action DuringClose;
    internal string _originalText;
    internal HeathenEngineering.UX.uGUIExtras.KeyboardFunction LastFunction;
    internal UnityEngine.GameObject SelectedGameObject { get; set; }
    internal bool ShouldBeEnabled => true;
    internal void Disable()
    {
        LeAccess.Accessibility.Patches.CharacterNameKeyboardPatches.Close.Prefix(this, out var state);
        if (!_enabled) EarlyReturns++;
        else
        {
            // Installed native order: clear editor/selection, disable, restore saved navigation, callbacks.
            CloseCount++;
            var input = _selectedInputField?.gameObject.GetComponent<TMPro.TMP_InputField>();
            if (input is not null) { input.isFocused = false; input.ActivationPending = false; }
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            _selectedInputField = null;
            if (LeAccess.Accessibility.Patches.CharacterNameKeyboardPatches.SelectEditor.Prefix(this, null))
                _selectedGameObject = null;
            _enabled = false;
            NavigationManager.currentlySelected = null;
            if (NavigationManager.instance?.GetNavigableFromID(_previousSelectedNavigableId, out var owner) == true)
                NavigationManager.currentlySelected = owner;
            DuringClose?.Invoke();
        }
        LeAccess.Accessibility.Patches.CharacterNameKeyboardPatches.Close.Postfix(this, state);
    }
    internal void Cancel()
        => OnFunctionPress(HeathenEngineering.UX.uGUIExtras.KeyboardFunction.Cancel);
    internal void OnFunctionPress(HeathenEngineering.UX.uGUIExtras.KeyboardFunction function)
    {
        LeAccess.Accessibility.Patches.CharacterNameKeyboardPatches.Function.Prefix(this, ref function);
        LastFunction = function;
        // OnFunctionPress(Cancel) restores text inline before calling Disable.
        var input = _selectedInputField?.gameObject.GetComponent<TMPro.TMP_InputField>();
        if (function == HeathenEngineering.UX.uGUIExtras.KeyboardFunction.Cancel && input is not null)
            input.text = _originalText;
        if (function is HeathenEngineering.UX.uGUIExtras.KeyboardFunction.Cancel or HeathenEngineering.UX.uGUIExtras.KeyboardFunction.Submit)
            Disable();
    }
    internal void OnCancel()
    {
        if (LeAccess.Accessibility.Patches.CharacterNameKeyboardPatches.Cancel.Prefix(this))
        {
            var input = _selectedInputField?.gameObject.GetComponent<TMPro.TMP_InputField>();
            if (input is not null) input.text = _originalText;
            Disable();
        }
    }
    internal bool PressLetter(char letter)
    {
        bool allowed = EpochInputManager.IsControllerActive && _selectedInputField is not null;
        LeAccess.Accessibility.Patches.CharacterNameKeyboardPatches.Enable.Postfix(this, ref allowed);
        if (!allowed) return false;
        var input = _selectedInputField.gameObject.GetComponent<TMPro.TMP_InputField>();
        input.text += letter;
        _substituteInputField.text = input.text;
        return true;
    }
    internal readonly List<UnityEngine.Event> Submitted = new();
    // Installed non-deferred SubmitEvent: the original editor processes the key, then the substitute copies its text.
    internal void SubmitEvent(UnityEngine.Event key)
    {
        Submitted.Add(key);
        var input = _selectedInputField?.gameObject.GetComponent<TMPro.TMP_InputField>();
        if (input is null) return;
        input.text ??= "";
        if (key.keyCode == UnityEngine.KeyCode.Backspace) { if (input.text.Length > 0) input.text = input.text[..^1]; }
        else if (key.type == UnityEngine.EventType.KeyDown && key.character != '\0') input.text += key.character;
        _substituteInputField.text = input.text;
    }
    internal void ResumePendingCursor(int position)
    {
        // Native ForceCursor exits after its yield when Disable has released the adapter.
        if (_selectedInputField is null) return;
        var input = _selectedInputField.gameObject.GetComponent<TMPro.TMP_InputField>();
        input.caretPosition = input.stringPosition = position;
    }
}
namespace HeathenEngineering.UX.uGUIExtras
{
    internal enum KeyboardFunction { CapsLock, BackSpace, Space, Submit, Cancel, CursorLeft, CursorRight }
}
internal sealed class IKeyboardInputField : NativeObject { internal UnityEngine.GameObject gameObject; }
namespace LE.UI.PanelSystem { internal sealed class MainMenuPanel : UnityEngine.Behaviour { } }
namespace LeAccess.Accessibility.ControllerMenus
{ internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
namespace UnityEngine.EventSystems
{
    internal sealed class EventSystem : UnityEngine.Behaviour
    {
        internal static EventSystem current;
        internal UnityEngine.GameObject currentSelectedGameObject;
        internal void SetSelectedGameObject(UnityEngine.GameObject selected) => currentSelectedGameObject = selected;
    }
    internal sealed class PointerEventData : NativeObject
    {
        internal enum InputButton { Left, Right, Middle }
        internal InputButton button;
    }
}
namespace HarmonyLib
{
    internal enum MethodType { Setter, Getter }
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute
    { internal HarmonyPatch(Type type, string name, params object[] args) { } }
}
namespace LeAccess.Accessibility.Social
{
    internal enum SocialField { PartyInvite, AddFriend, Block }
    internal static class SocialWindows
    {
        internal static TMPro.TMP_InputField Username;
        internal static bool TryGetField(UnityEngine.GameObject element, out TMPro.TMP_InputField input, out SocialField field)
        {
            field = SocialField.PartyInvite;
            input = Username;
            return Username is not null && ReferenceEquals(element, Username.gameObject);
        }
    }
}
