using System;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Utils;

/// <summary>An ability's name in the game's language, as its skill UI shows it.</summary>
internal static class AbilityNames
{
    internal static string Read(Ability ability)
    {
        if (!IsAlive(ability)) return null;
        string raw = ability.abilityName;
        try
        {
            string key = LocalizationSystem.Conventions.GetAbilityNameKey(raw);
            return TextUtils.CleanText(string.IsNullOrWhiteSpace(key) ? raw : Localization.GetText(key, raw));
        }
        catch (Exception) { return TextUtils.CleanText(raw); }
    }
}
