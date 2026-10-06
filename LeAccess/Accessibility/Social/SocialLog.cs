using System;
using BepInEx.Logging;

namespace LeAccess.Accessibility.Social;

/// <summary>Notes the game's "User service error: " log line, the only trace the party-invite window leaves when a
/// username cannot be found (UserServiceError.ToString, logged by PartyController's invite confirmation).</summary>
/// <remarks>See reference/social-reading.md#feedback.</remarks>
internal sealed class SocialLog : ILogListener
{
    private const string Marker = "User service error: ";
    private static SocialLog _instance;
    private static volatile bool _pending;

    public LogLevel LogLevelFilter => LogLevel.All;

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        string text = eventArgs?.Data?.ToString();
        if (text != null && text.IndexOf(Marker, StringComparison.Ordinal) >= 0) _pending = true;
    }

    public void Dispose() { }

    internal static void Install()
    {
        if (_instance != null) return;
        _instance = new SocialLog();
        BepInEx.Logging.Logger.Listeners.Add(_instance);
    }

    internal static void Uninstall()
    {
        if (_instance == null) return;
        BepInEx.Logging.Logger.Listeners.Remove(_instance);
        _instance = null;
        _pending = false;
    }

    /// <summary>Whether a user-service error was logged since the last call.</summary>
    internal static bool TakeUserError()
    {
        bool pending = _pending;
        _pending = false;
        return pending;
    }
}
