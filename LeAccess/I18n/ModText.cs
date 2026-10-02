using System;
using System.Globalization;

namespace LeAccess.I18n;

/// <summary>Resolves mod-owned speech without translating game-supplied text.</summary>
internal static class ModText
{
    private static TranslationCatalog _catalog = new(null, null);
    private static CultureInfo _culture = CultureInfo.GetCultureInfo("en");

    internal static string Language => _culture.Name;

    internal static void Initialize(string directory, Action<string> warning)
    {
        _catalog = new TranslationCatalog(directory, warning);
        _culture = CultureInfo.GetCultureInfo("en");
    }

    internal static bool SetLanguage(string language)
    {
        var culture = ResolveCulture(language);
        if (_culture.Name == culture.Name) return false;
        _culture = culture;
        return true;
    }

    internal static string Get(string key) => _catalog.Get(_culture, key);

    internal static string Format(string key, params object[] arguments) =>
        _catalog.Format(_culture, key, arguments);

    private static CultureInfo ResolveCulture(string language)
    {
        string code = language?.Trim().Replace('_', '-');
        if (!string.IsNullOrEmpty(code))
        {
            // Only language tags can become translation filenames.
            bool valid = true;
            foreach (char character in code)
                if (!(character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' ||
                    character >= '0' && character <= '9' || character == '-')) valid = false;
            if (valid)
            {
                try { return CultureInfo.GetCultureInfo(code); }
                catch (CultureNotFoundException) { }
            }
        }
        return CultureInfo.GetCultureInfo("en");
    }
}
