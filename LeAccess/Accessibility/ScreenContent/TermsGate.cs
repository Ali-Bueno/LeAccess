using LE.Tos;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ScreenContent;

/// <summary>Explains the terms panel's scroll gate: Accept stays disabled until the text reaches its end.</summary>
/// <remarks>See reference/first-login.md.</remarks>
internal static class TermsGate
{
    internal static string ReadHint(TosPanel panel)
    {
        if (!panel._requireAcceptance || panel._hasScrolledToEnd) return null;
        string accept = Caption(panel._acceptButton);
        if (accept is null) return null;
        string jump = Caption(panel._scrollToEndButton);
        return ScreenContentTracker.Combine(new[]
            { ModText.Format("terms.locked", accept), jump is null ? null : ModText.Format("terms.scroll_button", jump) });
    }

    internal static string ReadReady(TosPanel panel)
    {
        if (!panel._requireAcceptance || !panel._hasScrolledToEnd || !IsAlive(panel._acceptButton) ||
            !panel._acceptButton.interactable) return null;
        string accept = Caption(panel._acceptButton);
        return accept is null ? null : ModText.Format("terms.unlocked", accept);
    }

    private static string Caption(Button button) =>
        IsAlive(button) && button.isActiveAndEnabled ? TextUtils.GetTextFromHierarchy(button.gameObject) : null;
}
