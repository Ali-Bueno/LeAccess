public sealed class EpochInputManager : UnityEngine.Behaviour
{
    public static bool IsControllerActive = true;
}
namespace LeAccess.Accessibility.Login
{
    // The real form lookup is tested in the CharacterName suite; menu reading only needs it configurable.
    internal static class LoginPrompts
    {
        internal static TMPro.TMP_InputField PromptInput;
        internal static UnityEngine.UI.Button LockedButton;
        internal static bool IsPromptInput(TMPro.TMP_InputField input) => PromptInput is not null && ReferenceEquals(input, PromptInput);
        internal static bool IsLockedButton(UnityEngine.UI.Button button) => LockedButton is not null && ReferenceEquals(button, LockedButton);
    }
}
