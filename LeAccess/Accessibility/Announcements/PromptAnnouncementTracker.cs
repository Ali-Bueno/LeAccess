using System.Collections.Generic;

namespace LeAccess.Accessibility.Announcements;

internal sealed class PromptAnnouncementTracker
{
    private readonly Dictionary<int, string> _previous = new();
    private readonly HashSet<string> _announced = new();
    private long _context;

    internal string Observe(long context, IReadOnlyDictionary<int, string> visible, bool canSpeak,
        IReadOnlyDictionary<int, string> actions = null)
    {
        if (_context != context)
        {
            // Moving between child navigables does not make unchanged HUD hints new information.
            _previous.Clear();
            _announced.IntersectWith(actions?.Values ?? visible.Values);
            _context = context;
        }

        var messages = new List<string>();
        foreach (var entry in visible)
        {
            string identity = actions != null && actions.TryGetValue(entry.Key, out string action) ? action : entry.Value;
            // Require the same visible text on two frames, after layout and localization settle.
            if (!canSpeak || string.IsNullOrWhiteSpace(entry.Value) ||
                !_previous.TryGetValue(entry.Key, out var previous) || previous != entry.Value ||
                _announced.Contains(identity)) continue;

            // Rebinding or switching input devices updates the text, not the action's novelty.
            _announced.Add(identity);
            if (!messages.Contains(entry.Value)) messages.Add(entry.Value);
        }

        _previous.Clear();
        foreach (var entry in visible) _previous[entry.Key] = entry.Value;
        return string.Join(". ", messages);
    }

    internal void Reset()
    {
        _previous.Clear();
        _announced.Clear();
        _context = 0;
    }
}
