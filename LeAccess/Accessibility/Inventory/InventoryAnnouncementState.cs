using System;

namespace LeAccess.Accessibility.Inventory;

internal sealed class InventoryAnnouncementState
{
    private string _selection;
    private string _candidate, _candidateIdentity;
    private string _spoken, _spokenIdentity;

    internal bool HasPendingContent => !string.IsNullOrWhiteSpace(_candidate) && _candidate != _spoken;

    /// <param name="identity">The part of <paramref name="text"/> that names the selection; when only the rest
    /// (position, actions) changes for the same selection, the new text is taken silently.</param>
    internal bool Observe(string selection, string text, bool canSpeak, out string announcement, bool acknowledge = true,
        string identity = null)
    {
        announcement = null;
        if (selection != _selection)
        {
            Reset();
            _selection = selection;
        }
        bool stable = text == _candidate;
        _candidate = text;
        _candidateIdentity = identity;
        if (string.IsNullOrWhiteSpace(selection) || string.IsNullOrWhiteSpace(text) || !stable) return false;
        if (identity != null && _spoken != null && identity == _spokenIdentity) { _spoken = text; return false; }
        if (!canSpeak || text == _spoken) return false;
        if (acknowledge) Acknowledge(selection, text);
        announcement = text;
        return true;
    }

    internal void Acknowledge(string selection, string text)
    {
        if (_selection != selection || _candidate != text) return;
        _spoken = text;
        _spokenIdentity = _candidateIdentity;
    }

    internal bool HasSpoken(string selection, string text) => _selection == selection && _spoken == text;

    internal void Reset() { _selection = null; _candidate = _candidateIdentity = null; _spoken = _spokenIdentity = null; }
}
