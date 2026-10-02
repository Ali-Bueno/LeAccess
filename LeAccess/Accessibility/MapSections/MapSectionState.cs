namespace LeAccess.Accessibility.MapSections;

internal sealed class MapSectionState
{
    private readonly bool _confirmArrival;
    private long _session;
    private bool _hasSession, _hasCandidate;
    private string _candidateId, _candidateText, _currentId;
    private string _spoken;
    internal bool HasPendingContent { get; private set; }

    internal MapSectionState(bool confirmArrival = true) => _confirmArrival = confirmArrival;

    internal bool Observe(long session, MapSection section, bool canSpeak, out string textKey)
        => Observe(session, section?.SpeechGroup, section?.TextKey, canSpeak, out textKey);

    internal bool Observe(long session, string sectionId, string text, bool canSpeak, out string result)
    {
        result = null;
        if (sectionId == null) text = null;
        if (!_hasSession || _session != session) { Reset(); _hasSession = true; _session = session; }
        bool stable = _hasCandidate && sectionId == _candidateId && text == _candidateText;
        _hasCandidate = true;
        _candidateId = sectionId;
        _candidateText = text;
        // Replace obsolete pending content immediately, including the first outside observation.
        HasPendingContent = sectionId != null && !string.IsNullOrWhiteSpace(text) &&
            (sectionId != _currentId || text != _spoken);
        if (!stable && (_confirmArrival || sectionId == null)) return false;
        if (sectionId != _currentId)
        {
            _currentId = sectionId;
            _spoken = null;
        }
        if (sectionId == null) { HasPendingContent = false; return false; }
        HasPendingContent = !string.IsNullOrWhiteSpace(text) && text != _spoken;
        if (!HasPendingContent || !canSpeak) return false;
        _spoken = result = text;
        HasPendingContent = false;
        return true;
    }

    internal void Reset()
    {
        _session = 0; _hasSession = _hasCandidate = false;
        _candidateId = _candidateText = _currentId = _spoken = null; HasPendingContent = false;
    }
}
