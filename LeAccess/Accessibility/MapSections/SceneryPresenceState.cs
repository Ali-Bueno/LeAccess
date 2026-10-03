namespace LeAccess.Accessibility.MapSections;

internal sealed class SceneryPresenceState
{
    private readonly MapSectionState _places = new(confirmArrival: false);
    private long _session;
    private string _lastId;
    private bool _tentSpoken, _outsideObserved, _exitPending;
    internal bool HasPendingContent => _places.HasPendingContent || _exitPending;

    internal bool Observe(long session, string id, string text, bool tent, bool canSpeak, double now, string exitText, out string result,
        bool validObservation = true)
    {
        if (_session != session) { Reset(); _session = session; }
        if (id != null)
        {
            _lastId = id;
            _outsideObserved = _exitPending = false;
            if (!tent) _tentSpoken = false;
        }
        else if (validObservation)
        {
            _exitPending = _tentSpoken && _outsideObserved;
            _outsideObserved = true;
        }
        else _outsideObserved = _exitPending = false;
        if (_places.Observe(session, validObservation ? id : _lastId, validObservation ? text : null, canSpeak, now, out result))
        {
            _tentSpoken = tent;
            return true;
        }
        if (!_exitPending || !canSpeak || string.IsNullOrWhiteSpace(exitText)) return false;
        result = exitText;
        _tentSpoken = _exitPending = false;
        _places.ForgetSpoken();
        return true;
    }

    internal void Reset()
    {
        _places.Reset();
        _session = 0;
        _lastId = null;
        _tentSpoken = _outsideObserved = _exitPending = false;
    }
}
