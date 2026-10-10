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
    // Mirrors the game's static table (build 25816319), keyed by Unity LocaleName.
    public static Dictionary<string, LanguageKey> LanguageKeys = new()
    {
        ["English (en)"] = LanguageKey.English, ["Portuguese (pt)"] = LanguageKey.Portuguese,
        ["Chinese (Simplified) (zh)"] = LanguageKey.Chinese, ["German (de)"] = LanguageKey.German,
        ["Polish (pl)"] = LanguageKey.Polish, ["Korean (ko)"] = LanguageKey.Korean, ["French (fr)"] = LanguageKey.French,
        ["Spanish (Spain) (es-ES)"] = LanguageKey.Spanish, ["Russian (ru)"] = LanguageKey.Russian,
        ["Japanese (jp)"] = LanguageKey.Japanese
    };
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
