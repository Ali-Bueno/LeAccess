using System;

namespace LeAccess.Accessibility.Inventory;

internal sealed class InventoryAnnouncementState
{
    private string _selection;
    private string _candidate, _candidateIdentity;
    // The last selection actually spoken; unspoken selections and a lost focus only interrupt it.
    private string _spokenSelection, _spoken, _spokenIdentity;
    private int _spokenMark;
    private bool _interrupted;

    internal bool HasPendingContent => !string.IsNullOrWhiteSpace(_candidate) && !HasSpoken(_selection, _candidate);

    /// <summary>The latest observation regained the spoken selection after an interruption and stayed silent.</summary>
    internal bool Resumed { get; private set; }

    /// <param name="identity">The part of <paramref name="text"/> that names the selection; when only the rest
    /// (position, actions) changes for the same selection, the new text is taken silently.</param>
    /// <param name="speechMark">The reader's output count; the spoken selection regained with nothing said since is not
    /// repeated (reference/inventory-drop-focus.md#refresh-after-an-online-sync-2026-10-09).</param>
    internal bool Observe(string selection, string text, bool canSpeak, out string announcement, bool acknowledge = true,
        string identity = null, int speechMark = 0)
    {
        announcement = null;
        Resumed = false;
        if (selection != _selection)
        {
            _selection = selection;
            _candidate = _candidateIdentity = null;
            if (selection != _spokenSelection) _interrupted = true;
        }
        bool stable = text == _candidate;
        _candidate = text;
        _candidateIdentity = identity;
        if (string.IsNullOrWhiteSpace(selection) || string.IsNullOrWhiteSpace(text) || !stable) return false;
        if (selection == _spokenSelection && (!_interrupted || speechMark == _spokenMark) &&
            (text == _spoken || identity != null && identity == _spokenIdentity))
        {
            Resumed = _interrupted;
            _spoken = text;
            _interrupted = false;
            return false;
        }
        if (!canSpeak) return false;
        if (acknowledge) Acknowledge(selection, text, speechMark);
        announcement = text;
        return true;
    }

    /// <param name="speechMark">The reader's output count right after this speech.</param>
    internal void Acknowledge(string selection, string text, int speechMark = 0)
    {
        if (_selection != selection || _candidate != text) return;
        _spokenSelection = selection;
        _spoken = text;
        _spokenIdentity = _candidateIdentity;
        _spokenMark = speechMark;
        _interrupted = false;
    }

    internal bool HasSpoken(string selection, string text) =>
        !_interrupted && _spokenSelection == selection && _spoken == text;

    /// <summary>Forgets the current candidate but keeps the last spoken selection, as when the grid briefly loses focus.</summary>
    internal void Interrupt()
    {
        _selection = null;
        _candidate = _candidateIdentity = null;
        Resumed = false;
        _interrupted = true;
    }

    internal void Reset()
    {
        Interrupt();
        _spokenSelection = _spoken = _spokenIdentity = null;
        _spokenMark = 0;
        _interrupted = false;
    }
}
