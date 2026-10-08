using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Gifting;

internal enum GiftResonance { None, Gold, Obsidian }

/// <summary>Wording for the item gifting panel's stops and the inventory beside it.</summary>
/// <remarks>See reference/social-reading.md#item-gifting-2026-10-07.</remarks>
internal static class GiftText
{
    /// <summary>The confirm button with what it would send, or why the game keeps it unavailable.</summary>
    internal static string Confirm(string caption, bool ready, string item, string requirement)
    {
        string button = ModText.Format("gift.button", string.IsNullOrWhiteSpace(caption) ? ModText.Get("gift.confirm") : caption);
        return Combine(ready ? button : ModText.Format("control.disabled", button),
            item == null ? ModText.Get("gift.needs_item") : ModText.Format("gift.item", item),
            ready || item == null ? null : requirement);
    }

    internal static string Slot(string item) =>
        item == null ? ModText.Get("gift.slot_empty") : ModText.Format("gift.slot_item", item);

    internal static string Resonance(GiftResonance slotted) => Combine(Name(slotted), ModText.Get("gift.resonance_hint"));

    internal static string ResonanceRow(GiftResonance kind, string player, string amount) =>
        Combine(Name(kind), player, string.IsNullOrWhiteSpace(amount) ? null : ModText.Format("gift.amount", amount));

    internal static string Close() => ModText.Get("gift.close");

    internal static string Hint() => ModText.Get("gift.hint");

    internal static string InventoryHint() => ModText.Get("gift.inventory_hint");

    internal static string MovedIn(string item) => ModText.Format("gift.moved_in", item);

    internal static string MoveRefused() => ModText.Get("gift.move_refused");

    private static string Name(GiftResonance kind) => ModText.Get(kind switch
    {
        GiftResonance.Gold => "gift.resonance_gold",
        GiftResonance.Obsidian => "gift.resonance_obsidian",
        _ => "gift.resonance_none"
    });

    private static string Combine(params string[] parts) => ScreenContentTracker.Combine(parts);
}
