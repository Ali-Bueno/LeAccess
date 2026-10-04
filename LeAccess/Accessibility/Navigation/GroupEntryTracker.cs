namespace LeAccess.Accessibility.Navigation;

/// <summary>Says whether a selection entered a different group than the previous one; stable while the selection stays.</summary>
internal sealed class GroupEntryTracker
{
    private object _selection;
    private object _group;
    private bool _entered;

    /// <summary>True for the first selection after a reset or a group change, for as long as that selection is current.</summary>
    internal bool Entered(object selection, object group)
    {
        if (Equals(selection, _selection)) return _entered;
        _entered = !Equals(group, _group);
        _selection = selection;
        _group = group;
        return _entered;
    }

    internal void Reset()
    {
        _selection = null;
        _group = null;
        _entered = false;
    }
}
