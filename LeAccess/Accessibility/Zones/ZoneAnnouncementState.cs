namespace LeAccess.Accessibility.Zones;

internal sealed class ZoneAnnouncementState
{
    private int _scene;
    private bool _hasScene;
    private bool _delivered;
    private string _candidate;
    internal bool HasDelivered => _delivered;
    internal bool HasPendingContent => !_delivered && !string.IsNullOrWhiteSpace(_candidate);

    internal void EnterScene(int scene)
    {
        if (_hasScene && _scene == scene) return;
        Reset();
        _scene = scene;
        _hasScene = true;
    }

    internal bool Observe(string text, bool canSpeak, out string announcement)
    {
        announcement = null;
        bool stable = _candidate == text;
        _candidate = text;
        if (!_hasScene || _delivered || !stable || !canSpeak || string.IsNullOrWhiteSpace(text)) return false;
        _delivered = true;
        announcement = text;
        return true;
    }

    internal void Suspend() => _candidate = null;
    internal void Reset() { _hasScene = false; _delivered = false; _candidate = null; }
}
