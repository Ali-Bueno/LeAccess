namespace LeAccess.Accessibility.Npcs;

internal sealed class NpcNameAnnouncementState
{
    private long _session;
    private int _target;
    private string _candidate;
    private string _spoken;

    internal bool HasPendingContent => !string.IsNullOrWhiteSpace(_candidate) && _candidate != _spoken;

    internal bool Observe(long session, int target, string name, bool canSpeak, out string announcement)
    {
        announcement = null;
        if (session != _session || target != _target)
        {
            Reset();
            _session = session;
            _target = target;
        }
        string current = name?.Trim();
        bool stable = current == _candidate;
        _candidate = current;
        if (!stable || !canSpeak || !HasPendingContent) return false;
        _spoken = current;
        announcement = current;
        return true;
    }

    internal void Reset() { _session = 0; _target = 0; _candidate = null; _spoken = null; }
}
