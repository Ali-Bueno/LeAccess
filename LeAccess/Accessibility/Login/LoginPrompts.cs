using LE.UI.Login.UnityUI;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Login;

/// <summary>Finds the first-login display-name and email forms and their locked buttons.</summary>
/// <remarks>See reference/first-login.md.</remarks>
internal static class LoginPrompts
{
    internal readonly struct Form
    {
        internal readonly Behaviour Panel;
        internal readonly TMP_InputField Input;
        internal readonly Button Submit;
        internal readonly TMP_Text Error;

        internal Form(Behaviour panel, TMP_InputField input, Button submit, LoginHeader error)
        {
            Panel = panel;
            Input = input;
            Submit = submit;
            Error = IsAlive(error) ? error.label : null;
        }

        internal string ReadError() => TextUtils.IsVisibleLabel(Error) ? TextUtils.CleanText(Error.text) : null;

        internal bool IsError(Component label) =>
            IsAlive(Error) && IsAlive(label) && label.transform.IsChildOf(Error.transform);
    }

    internal static bool TryFind(GameObject element, out Form form)
    {
        form = default;
        if (!IsAlive(element)) return false;
        var name = element.GetComponentInParent<DisplayNamePromptPanel>();
        if (IsAlive(name)) form = new Form(name, name.displayNameText, name.submitButton, name.errorHeader);
        else
        {
            var email = element.GetComponentInParent<EmailPromptPanel>();
            if (IsAlive(email)) form = new Form(email, email.emailText, email.submitButton, email.errorHeader);
        }
        return IsAlive(form.Panel) && IsAlive(form.Input);
    }

    internal static bool IsPromptInput(TMP_InputField input) =>
        IsAlive(input) && TryFind(input.gameObject, out var form) && form.Input.Pointer == input.Pointer;

    // Disabled buttons the game still lets focus reach: Accept before the terms end, Submit while too short or sending.
    internal static bool IsLockedButton(Button button)
    {
        if (!IsAlive(button) || button.interactable) return false;
        var terms = button.GetComponentInParent<LE.Tos.TosPanel>();
        if (IsAlive(terms)) return IsAlive(terms._acceptButton) && terms._acceptButton.Pointer == button.Pointer;
        return TryFind(button.gameObject, out var form) && IsAlive(form.Submit) && form.Submit.Pointer == button.Pointer;
    }
}
