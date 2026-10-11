namespace LeAccess.Accessibility.Zones;

/// <summary>Speaks a transition pop-up once each time it appears or its text changes while shown.</summary>
internal sealed class ZoneTransitionState
{
    private string _spoken;

    /// <summary>The visible pop-up text, or null while it is hidden or empty; true when it needs speaking.</summary>
    internal bool Observe(string visibleText, out string announcement)
    {
        announcement = null;
        if (string.IsNullOrWhiteSpace(visibleText)) { _spoken = null; return false; }
        if (visibleText == _spoken) return false;
        announcement = visibleText;
        return true;
    }

    internal void Acknowledge(string text) => _spoken = text;

    internal void Reset() => _spoken = null;
}
