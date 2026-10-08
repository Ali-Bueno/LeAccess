using LeAccess.Accessibility.Panels;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Cosmetics;

/// <summary>Builds what is spoken for the cosmetics panel's slots, class filters and tabs.</summary>
/// <remarks>See reference/cosmetics-reading.md.</remarks>
internal static class CosmeticsText
{
    internal static string Slot(string slot, string cosmetic, bool locked) =>
        ScreenContentTracker.Combine(new[] { slot, string.IsNullOrWhiteSpace(cosmetic) ? ModText.Get("cosmetics.empty") : cosmetic,
            locked ? ModText.Get("cosmetics.locked") : null });

    internal static string ClassFilter(string className, bool on) =>
        ModText.Format(on ? "cosmetics.filter_on" : "cosmetics.filter_off", className);

    internal static string Tab(string name, bool selected) => PanelText.Tab(name, selected, 0, 0);
}
