using System;
using BepInEx.Logging;

namespace LeAccess.Accessibility.Forge;

/// <summary>Notes the game's own "[Forge Craft] Result" log line, written by OnCraftingAttempted just before it fills
/// the result labels, offline and online alike; avoids hooking a method that takes structs by value.</summary>
/// <remarks>See reference/crafting-reading.md#controller-forge-2026-10-05.</remarks>
internal sealed class ForgeResultLog : ILogListener
{
    private const string Marker = "[Forge Craft] Result";
    private static ForgeResultLog _instance;
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
        _instance = new ForgeResultLog();
        BepInEx.Logging.Logger.Listeners.Add(_instance);
    }

    internal static void Uninstall()
    {
        if (_instance == null) return;
        BepInEx.Logging.Logger.Listeners.Remove(_instance);
        _instance = null;
        _pending = false;
    }

    /// <summary>A craft result arrived and has not been spoken; its labels are final by the next frame.</summary>
    internal static bool HasResult => _pending;

    internal static void Clear() => _pending = false;
}
