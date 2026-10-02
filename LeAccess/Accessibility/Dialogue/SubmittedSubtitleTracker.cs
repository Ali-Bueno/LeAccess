using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Dialogue;

internal sealed class SubmittedSubtitleTracker
{
    // Accessibility backlog limits, not game timings: discard delayed speech before it becomes history.
    internal const double MaximumDelaySeconds = 10;
    internal const int MaximumPendingLines = 16;
    private readonly List<Line> _pending = new();
    private readonly List<Line> _delivered = new();
    private readonly Dictionary<int, string> _rendered = new();

    internal void Submit(string content, string sender, double now)
    {
        Expire(now);
        if (string.IsNullOrWhiteSpace(content)) return;
        var line = new Line(content.Trim(), sender?.Trim(), now);
        foreach (string rendered in _rendered.Values)
            if (line.Matches(rendered)) return;
        foreach (var pending in _pending)
            if (pending.Content == line.Content && pending.Sender == line.Sender) return;
        _delivered.RemoveAll(delivered => delivered.Content == line.Content && delivered.Sender == line.Sender);
        if (_pending.Count == MaximumPendingLines) _pending.RemoveAt(0);
        _pending.Add(line);
    }

    internal bool ObserveRendered(int id, string text, double now)
    {
        Expire(now);
        _rendered[id] = text;
        int pending = _pending.FindIndex(line => line.Matches(text));
        if (pending >= 0) _pending.RemoveAt(pending);
        int delivered = _delivered.FindIndex(line => line.Matches(text));
        if (delivered < 0) return false;
        _delivered.RemoveAt(delivered);
        return true;
    }

    internal bool TryTake(bool canSpeak, double now, out string text)
    {
        Expire(now);
        text = null;
        if (_pending.Count == 0) return false;
        if (!canSpeak) return true;
        var line = _pending[0];
        _pending.RemoveAt(0);
        if (_delivered.Count == MaximumPendingLines) _delivered.RemoveAt(0);
        _delivered.Add(line);
        text = string.IsNullOrEmpty(line.Sender) ? line.Content : $"{line.Sender}: {line.Content}";
        return _pending.Count != 0;
    }

    internal void ForgetRendered(int id) => _rendered.Remove(id);
    internal void DiscardPending() => _pending.Clear();
    internal void Reset() { _pending.Clear(); _delivered.Clear(); _rendered.Clear(); }

    private void Expire(double now)
    {
        _pending.RemoveAll(line => now - line.SubmittedAt >= MaximumDelaySeconds);
        _delivered.RemoveAll(line => now - line.SubmittedAt >= MaximumDelaySeconds);
    }

    private sealed class Line
    {
        internal readonly string Content, Sender;
        internal readonly double SubmittedAt;
        internal Line(string content, string sender, double submittedAt)
        { Content = content; Sender = sender; SubmittedAt = submittedAt; }

        internal bool Matches(string text) => text == Content ||
            !string.IsNullOrEmpty(Sender) && (text == $"{Sender}: {Content}" || text == $"{Sender} {Content}");
    }
}
