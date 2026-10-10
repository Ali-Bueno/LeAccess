using System;
using BepInEx.Configuration;

namespace LeAccess.I18n;

internal static class GameLocale
{
    private static ConfigEntry<string> _language;
    private static string _lastGameLanguage = "en";
    private static bool _readFailureLogged;

    internal static void Initialize(ConfigFile config)
    {
        Reset();
        _language = config.Bind("Localization", "Language", "auto",
            "Speech language: auto follows the game text language; a culture code selects a translation (for example en or pt-BR).");
        Refresh();
    }

    internal static bool Refresh()
    {
        string configured = _language?.Value?.Trim();
        if (!string.IsNullOrEmpty(configured) &&
            !string.Equals(configured, "auto", StringComparison.OrdinalIgnoreCase))
            return ModText.SetLanguage(configured);

        try
        {
            string locale = global::Localization._initialized ? global::Localization.Locale : null;
            _lastGameLanguage = string.IsNullOrWhiteSpace(locale) ? "en" : ResolveGameLanguage(locale.Trim());
        }
        catch (Exception ex)
        {
            if (!_readFailureLogged)
            {
                _readFailureLogged = true;
                Plugin.Log?.LogWarning($"Game text language unavailable; retaining {_lastGameLanguage}: {ex.Message}");
            }
        }

        return ModText.SetLanguage(_lastGameLanguage);
    }

    internal static void Reset()
    {
        _language = null;
        _lastGameLanguage = "en";
        _readFailureLogged = false;
    }

    // Localization.Locale is the selected Unity Locale's LocaleName, e.g. "Spanish (Spain) (es-ES)" (reference/localization.md).
    private static string ResolveGameLanguage(string locale)
    {
        var languageKeys = LocaleNames.LanguageKeys;
        if (languageKeys != null && languageKeys.TryGetValue(locale, out LanguageKey language) &&
            CatalogLanguage(language) is { } catalogLanguage)
            return catalogLanguage;
        return locale.Replace('_', '-');
    }

    private static string CatalogLanguage(LanguageKey language) => language switch
    {
        LanguageKey.English => "en",
        LanguageKey.Portuguese => "pt",
        LanguageKey.Chinese => "zh",
        LanguageKey.German => "de",
        LanguageKey.Polish => "pl",
        LanguageKey.Korean => "ko",
        LanguageKey.French => "fr",
        LanguageKey.Spanish => "es",
        LanguageKey.Russian => "ru",
        LanguageKey.Japanese => "ja",
        _ => null
    };
}
