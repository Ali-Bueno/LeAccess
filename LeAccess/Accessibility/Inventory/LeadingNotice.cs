namespace LeAccess.Accessibility.Inventory;

/// <summary>A notice spoken before the next inventory selection's brief: kept until a brief carries it, then dropped
/// once the selection moves.</summary>
internal sealed class LeadingNotice
{
    private string _text;
    private object _spokenWith;

    internal void Raise(string text)
    {
        _text = string.IsNullOrWhiteSpace(text) ? null : text;
        _spokenWith = null;
    }

    /// <summary>The notice to lead this selection's brief, or null.</summary>
    internal string For(object selection)
    {
        if (_spokenWith != null && !Equals(selection, _spokenWith)) Clear();
        return _text;
    }

    /// <summary>A brief carrying the notice was accepted for this selection.</summary>
    internal void Spoken(object selection)
    {
        if (_text != null) _spokenWith ??= selection;
    }

    internal void Clear()
    {
        _text = null;
        _spokenWith = null;
    }
}
