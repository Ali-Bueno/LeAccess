using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

internal static class Speech
{
    internal static string T(string key) => ModText.Get(key);
    internal static string F(string key, params object[] args) => ModText.Format(key, args);
    internal static string Join(params string[] parts) => ScreenContentTracker.Combine(parts);
    internal static string DisabledMark() => F("control.disabled", "").Trim();
}
