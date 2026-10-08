using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Exploration;

internal enum ExplorationCommand { None, CategoryPrevious, CategoryNext, EntryPrevious, EntryNext, Lock, Vitals, Gold, Status }

// Pure stick logic: no Unity or Rewired types so it can be unit tested.
internal static class StickFlick
{
    // Match D2R's deliberate half-travel gesture on Rewired's normalized axes.
    internal const float PushThreshold = 0.5f;

    // Up/down step the category, left/right the entry. Only a push from neutral counts,
    // and the dominant axis decides, so one flick yields one command.
    internal static ExplorationCommand Detect(float x, float y, float previousX, float previousY)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(previousX) || !float.IsFinite(previousY))
            return ExplorationCommand.None;
        if (Math.Abs(previousX) >= PushThreshold || Math.Abs(previousY) >= PushThreshold)
            return ExplorationCommand.None;
        float ax = Math.Abs(x), ay = Math.Abs(y);
        if (Math.Max(ax, ay) < PushThreshold) return ExplorationCommand.None;
        if (ay >= ax) return y > 0 ? ExplorationCommand.CategoryPrevious : ExplorationCommand.CategoryNext;
        return x > 0 ? ExplorationCommand.EntryNext : ExplorationCommand.EntryPrevious;
    }
}

// Remembers exactly which bindings the mod disabled so only those are ever re-enabled.
internal sealed class BindingSuppression<T>
{
    private readonly List<T> _disabled = new();

    internal int Count => _disabled.Count;

    internal void Record(T item) => _disabled.Add(item);

    // Re-enables (via enable) every item whose own hardware is no longer held. Items still held
    // stay in the ledger for a later frame. Returns the number restored.
    internal int ReleaseWhenUp(Func<T, bool> isHeld, Action<T> enable)
    {
        int restored = 0;
        for (int i = _disabled.Count - 1; i >= 0; i--)
        {
            var item = _disabled[i];
            bool held;
            try { held = isHeld(item); }
            catch { held = false; }
            if (held) continue;
            _disabled.RemoveAt(i);
            Restore(item, enable);
            restored++;
        }
        return restored;
    }

    // Re-enables everything regardless of hardware state; the ledger is always emptied.
    internal int RestoreAll(Action<T> enable)
    {
        var items = _disabled.ToArray();
        _disabled.Clear();
        foreach (var item in items) Restore(item, enable);
        return items.Length;
    }

    private static void Restore(T item, Action<T> enable)
    {
        try { enable(item); }
        catch { /* a collected map object has nothing left to restore */ }
    }
}
