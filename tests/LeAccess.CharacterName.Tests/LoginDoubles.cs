namespace UnityEngine.UI { internal class Button : Selectable { internal bool interactable = true; } }
namespace LE.Tos { internal sealed class TosPanel : UnityEngine.Behaviour { internal UnityEngine.UI.Button _acceptButton; } }
namespace LE.UI.Login.UnityUI
{
    internal sealed class LoginHeader : UnityEngine.Behaviour { internal TMPro.TMP_Text label; }
    internal sealed class DisplayNamePromptPanel : UnityEngine.Behaviour
    { internal TMPro.TMP_InputField displayNameText; internal UnityEngine.UI.Button submitButton; internal LoginHeader errorHeader; }
    internal sealed class EmailPromptPanel : UnityEngine.Behaviour
    { internal TMPro.TMP_InputField emailText; internal UnityEngine.UI.Button submitButton; internal LoginHeader errorHeader; }
}
