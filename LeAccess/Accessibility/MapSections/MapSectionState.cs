namespace LeAccess.Accessibility.MapSections;

internal sealed class MapSectionState
{
    // Brief absences are edge flicker while walking along one footprint.
    internal const double RepeatGapSeconds = 5;
    private readonly bool _confirmArrival;
    private long _session;
    private bool _hasSession, _hasCandidate;
    private string _candidateId, _candidateText, _currentId;
    private string _spoken, _remembered;
    private double _nullSince = double.NaN, _absentSince = double.NaN;
    internal bool HasPendingContent { get; private set; }

    internal MapSectionState(bool confirmArrival = true) => _confirmArrival = confirmArrival;

    internal bool Observe(long session, MapSection section, bool canSpeak, double now, out string textKey)
        => Observe(session, section?.SpeechGroup, section?.TextKey, canSpeak, now, out textKey);

    internal bool Observe(long session, string sectionId, string text, bool canSpeak, double now, out string result)
    {
        result = null;
        if (sectionId == null) text = null;
        if (!_hasSession || _session != session) { Reset(); _hasSession = true; _session = session; }
        if (sectionId == null && (!_hasCandidate || _candidateId != null)) _nullSince = now;
        bool stable = _hasCandidate && sectionId == _candidateId && text == _candidateText;
        _hasCandidate = true;
        _candidateId = sectionId;
        _candidateText = text;
        // Replace obsolete pending content immediately, including the first outside observation.
        HasPendingContent = sectionId != null && !string.IsNullOrWhiteSpace(text) &&
            (sectionId != _currentId ? !IsRepeat(text, now) : text != _spoken);
        if (!stable && (_confirmArrival || sectionId == null)) return false;
        if (sectionId != _currentId)
        {
            bool repeat = sectionId != null && IsRepeat(text, now);
            _currentId = sectionId;
            _spoken = repeat ? text : null;
            if (sectionId == null) { if (double.IsNaN(_absentSince)) _absentSince = _nullSince; }
            else _absentSince = double.NaN;
        }
        if (sectionId == null) { HasPendingContent = false; return false; }
        HasPendingContent = !string.IsNullOrWhiteSpace(text) && text != _spoken;
        if (!HasPendingContent || !canSpeak) return false;
        _spoken = _remembered = result = text;
        HasPendingContent = false;
        return true;
    }

    internal void ForgetSpoken() => _remembered = null;

    private bool IsRepeat(string text, double now) => text != null && text == _remembered &&
        (double.IsNaN(_absentSince) || now - _absentSince < RepeatGapSeconds);

    internal void Reset()
    {
        _remembered = null; _nullSince = _absentSince = double.NaN;
        _session = 0; _hasSession = _hasCandidate = false;
        _candidateId = _candidateText = _currentId = _spoken = null; HasPendingContent = false;
    }
}
