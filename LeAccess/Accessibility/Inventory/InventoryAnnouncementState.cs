using System;

namespace LeAccess.Accessibility.Inventory;

internal sealed class InventoryAnnouncementState
{
    private string _selection;
    private string _candidate;
    private string _spoken;

    internal bool HasPendingContent => !string.IsNullOrWhiteSpace(_candidate) && _candidate != _spoken;

    internal bool Observe(string selection, string text, bool canSpeak, out string announcement, bool acknowledge = true)
    {
        announcement = null;
        if (selection != _selection)
        {
            Reset();
            _selection = selection;
        }
        bool stable = text == _candidate;
        _candidate = text;
        if (string.IsNullOrWhiteSpace(selection) || string.IsNullOrWhiteSpace(text) ||
            !stable || !canSpeak || text == _spoken) return false;
        if (acknowledge) _spoken = text;
        announcement = text;
        return true;
    }

    internal void Acknowledge(string selection, string text)
    {
        if (_selection == selection && _candidate == text) _spoken = text;
    }

    internal bool HasSpoken(string selection, string text) => _selection == selection && _spoken == text;

    internal void Reset() { _selection = null; _candidate = null; _spoken = null; }
}
