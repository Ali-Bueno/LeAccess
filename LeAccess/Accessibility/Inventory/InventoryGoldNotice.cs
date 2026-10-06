using LeAccess.I18n;

namespace LeAccess.Accessibility.Inventory;

/// <summary>"Oro: N" before the first item read after the inventory or a shop opens, and again when gold changes
/// while a shop is open (a purchase or sale); kept until spoken, then dropped when the selection moves.</summary>
/// <remarks>See reference/vitals-api.md#gold-2026-10-05.</remarks>
internal sealed class InventoryGoldNotice
{
    private readonly LeadingNotice _notice = new();
    private bool _open, _shop;
    private int? _seen;

    /// <summary>Every frame: whether the inventory or a shop is open, and the current gold when it is.</summary>
    internal void Observe(bool open, bool shop, int? gold)
    {
        if (!open) { Reset(); return; }
        // A shop opening over an already open inventory counts as opening too.
        bool opened = !_open || shop && !_shop;
        bool changed = shop && gold is int now && _seen is int then && now != then;
        _open = true;
        _shop = shop;
        if (gold is not null) _seen = gold;
        if (opened || changed) _notice.Raise(gold is int amount ? ModText.Format("gold.amount", amount) : null);
    }

    /// <summary>The notice to lead this selection's brief, or null.</summary>
    internal string For(object selection) => _notice.For(selection);

    /// <summary>A brief carrying the notice was accepted for this selection.</summary>
    internal void Spoken(object selection) => _notice.Spoken(selection);

    internal void Reset()
    {
        _open = _shop = false;
        _seen = null;
        _notice.Clear();
    }
}
