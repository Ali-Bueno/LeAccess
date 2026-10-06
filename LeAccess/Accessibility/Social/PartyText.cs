using System;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Social;

/// <summary>Spoken wording for the party overview panel's member cards and portal buttons.</summary>
/// <remarks>See reference/social-reading.md.</remarks>
internal static class PartyText
{
    private const int PercentScale = 100;

    /// <summary>A bar's fill (0 to 1) as a whole percentage.</summary>
    internal static int Percent(float fill) => (int)Math.Round(Math.Clamp(fill, 0f, 1f) * PercentScale);

    /// <summary>One member: name, class, leader mark, then dead, or life and ward in the same zone, or elsewhere.</summary>
    internal static string Member(string name, string className, bool leader, bool dead, bool sameZone, int health,
        int ward)
    {
        string state = !sameZone ? ModText.Get("party.other_zone") :
            dead ? ModText.Get("party.dead") :
            ScreenContentTracker.Combine(new[]
            {
                ModText.Format("party.health", health), ward > 0 ? ModText.Format("party.ward", ward) : null,
                ModText.Get("party.same_zone")
            });
        return ScreenContentTracker.Combine(new[] { name, className, leader ? ModText.Get("party.leader") : null, state });
    }

    /// <summary>The button that opens a portal to the member.</summary>
    internal static string Portal(string name, bool available)
    {
        string caption = string.IsNullOrWhiteSpace(name) ? ModText.Get("party.portal_anyone") :
            ModText.Format("party.portal", name);
        return ModText.Format(available ? "social.button" : "control.disabled", caption);
    }
}
