namespace LeAccess.Accessibility.Inventory;

/// <summary>A notice spoken before the next inventory selection's brief: kept until a brief carries it, then dropped
/// once another selection is spoken.</summary>
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
    /// <remarks>An unspoken selection in between, such as an online refresh, does not end it.</remarks>
    internal string For(object selection) => _spokenWith == null || Equals(selection, _spokenWith) ? _text : null;

    /// <summary>A brief carrying the notice was accepted for this selection.</summary>
    internal void Spoken(object selection)
    {
        if (_spokenWith != null && !Equals(selection, _spokenWith)) Clear();
        else if (_text != null) _spokenWith ??= selection;
    }

    internal void Clear()
    {
        _text = null;
        _spokenWith = null;
    }
}
