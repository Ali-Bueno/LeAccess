namespace BepInEx.Configuration
{
    public sealed class ConfigEntry<T> { public T Value { get; set; } }
    public sealed class ConfigFile
    {
        public ConfigEntry<string> Language { get; } = new() { Value = "auto" };
        public ConfigEntry<string> Bind(string section, string key, string value, string description) => Language;
    }
}

public static class Localization
{
    public static bool _initialized;
    public static string Current;
    public static bool Throw;
    public static string Locale => Throw ? throw new InvalidOperationException("Transient game failure") : Current;
}

public enum LanguageKey { English, Portuguese, Chinese, German, Polish, Korean, French, Spanish, Russian, Japanese }
public static class LocaleNames
{
    public static Dictionary<string, LanguageKey> LanguageKeys = new() { ["game_spanish"] = LanguageKey.Spanish };
}

namespace LeAccess
{
    internal static class Plugin { internal static TestLog Log = new(); }
    internal sealed class TestLog
    {
        internal readonly List<string> Warnings = new();
        internal void LogWarning(string message) => Warnings.Add(message);
    }
}
