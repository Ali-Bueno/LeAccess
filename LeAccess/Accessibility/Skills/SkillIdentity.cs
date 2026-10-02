using System;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

internal static class SkillIdentity
{
    internal static bool TryReadPrimary(Ability ability, out Ability primary)
    {
        primary = null;
        if (!IsAlive(ability)) return false;
        try
        {
            if (!ability.overridePrimaryAbility) { primary = ability; return true; }
            // GetPrimaryAbility falls back to the variant when its reference is unresolved.
            var reference = ability.customPrimaryAbility;
            return reference is not null && !reference.WasCollected && reference.TryGetAbility(out primary) && IsAlive(primary);
        }
        catch (Exception) { return false; }
    }

    internal static bool Matches(Ability first, Ability second) =>
        IsAlive(first) && IsAlive(second) && (first.Pointer == second.Pointer ||
        TryReadPrimary(first, out var firstPrimary) && TryReadPrimary(second, out var secondPrimary) &&
        firstPrimary.Pointer == secondPrimary.Pointer);
}
