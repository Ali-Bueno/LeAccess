namespace LeAccess.Accessibility.Settings;

/// <summary>Remembers the spoken settings heading while the same settings panel stays visible.</summary>
internal sealed class SettingsHeadingTracker
{
    private int _owner, _tab;
    private string _heading;

    internal bool Update(int owner, int tab, string heading)
    {
        bool changed = _owner != owner || _tab != tab || _heading != heading;
        _owner = owner; _tab = tab; _heading = heading;
        return changed;
    }

    internal void Retain(bool panelVisible) { if (!panelVisible) Reset(); }

    internal void Reset() { _owner = _tab = 0; _heading = null; }
}

/// <summary>Chooses between a full control announcement and just its changed value.</summary>
internal static class SettingsValueSpeech
{
    internal static string Plan(bool sameControl, string lastLabel, string lastValue, string label, string value, string fullText) =>
        sameControl && !string.IsNullOrEmpty(label) && label == lastLabel &&
        !string.IsNullOrEmpty(value) && value != lastValue ? value : fullText;
}

/// <summary>Detects a focused control's value change from a cheap signature, so unchanged frames skip the full rebuild.</summary>
internal sealed class SettingsValueGate
{
    private int _id;
    private double _signature;

    internal bool Changed(int id, double? signature)
    {
        if (signature is null) { _id = 0; return false; }
        bool changed = _id == id && _signature != signature.Value;
        _id = id; _signature = signature.Value;
        return changed;
    }

    internal void Reset() => _id = 0;
}
