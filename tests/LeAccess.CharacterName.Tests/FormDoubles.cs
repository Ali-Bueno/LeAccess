internal static class NavigationManager { internal static NativeObject currentlySelected; }
internal sealed class PrioritizedNavigable : UnityEngine.Behaviour
{ internal UnityEngine.UI.Selectable ActiveSelectable; }
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
    internal bool _enabled;
    internal UnityEngine.GameObject _selectedGameObject;
}
namespace LE.UI.PanelSystem { internal sealed class MainMenuPanel : UnityEngine.Behaviour { } }
namespace LeAccess.Accessibility.ControllerMenus
{ internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
