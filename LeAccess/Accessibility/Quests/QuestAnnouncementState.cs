using LeAccess.Accessibility.ScreenContent;

namespace LeAccess.Accessibility.Quests;

internal sealed class QuestAnnouncementState
{
    private string _selection;
    private string _candidateSummary;
    private string _candidateDetails;
    private string _spokenSummary;
    private string _spokenDetails;
    internal bool HasPendingContent { get; private set; }

    internal bool Observe(string selection, string summary, string details, bool canSpeak, out string announcement)
    {
        announcement = null;
        if (selection != _selection) { Reset(); _selection = selection; }
        bool stable = summary == _candidateSummary && details == _candidateDetails;
        _candidateSummary = summary;
        _candidateDetails = details;
        bool hasDetails = !string.IsNullOrWhiteSpace(details);
        HasPendingContent = !string.IsNullOrWhiteSpace(selection) && !string.IsNullOrWhiteSpace(summary) &&
            (summary != _spokenSummary || hasDetails && details != _spokenDetails);
        if (!HasPendingContent || !stable || !canSpeak) return false;
        _spokenSummary = summary;
        // Hiding detail does not revoke already-spoken detail for the same selection.
        if (hasDetails) _spokenDetails = details;
        HasPendingContent = false;
        announcement = ScreenContentTracker.Combine(new[] { summary, details });
        return true;
    }

    internal void Reset()
    {
        _selection = _candidateSummary = _candidateDetails = _spokenSummary = _spokenDetails = null;
        HasPendingContent = false;
    }
}
