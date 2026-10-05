using LeAccess.I18n;

namespace LeAccess.Accessibility.Inventory;

/// <summary>"Oro: N" before the first item read after the inventory or a shop opens, and again when gold changes
/// while a shop is open (a purchase or sale); kept until spoken, then dropped when the selection moves.</summary>
/// <remarks>See reference/vitals-api.md#gold-2026-10-05.</remarks>
internal sealed class InventoryGoldNotice
{
    private bool _open, _shop;
    private int? _seen;
    private string _notice;
    private object _spokenWith;

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
        if (opened || changed) Raise(gold);
    }

    /// <summary>The notice to lead this selection's brief, or null.</summary>
    internal string For(object selection)
    {
        if (_spokenWith != null && !Equals(selection, _spokenWith))
        {
            _notice = null;
            _spokenWith = null;
        }
        return _notice;
    }

    /// <summary>A brief carrying the notice was accepted for this selection.</summary>
    internal void Spoken(object selection)
    {
        if (_notice != null) _spokenWith ??= selection;
    }

    internal void Reset()
    {
        _open = _shop = false;
        _seen = null;
        _notice = null;
        _spokenWith = null;
    }

    private void Raise(int? gold)
    {
        _spokenWith = null;
        _notice = gold is int amount ? ModText.Format("gold.amount", amount) : null;
    }
}
