using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Combat;

// Per-stream pulse cadence after D2RAccess entity_cues.cpp Tracker; see reference/enemy-cues.md.
internal sealed class EnemyCueTracker
{
    private readonly bool[] _played = new bool[EnemyCueSelector.SlotCount];
    private readonly double[] _last = new double[EnemyCueSelector.SlotCount];
    private readonly bool[] _present = new bool[EnemyCueSelector.SlotCount];
    private bool _started;
    private double _lastStart;

    // First cues go before repeats; among repeats the most overdue wins, so a near boss cannot starve the rest.
    internal bool TryChoose(IReadOnlyList<EnemyCue> selected, double now, out EnemyCue next)
    {
        next = default;
        if (selected == null || !double.IsFinite(now)) return false;
        if (_started && now < _lastStart) Reset();
        Array.Clear(_present);
        foreach (var cue in selected)
            if (ValidSlot(cue.Slot)) _present[cue.Slot] = true;
        // A stream keeps its cadence while it stays selected, even when its nearest enemy changes.
        for (int slot = 0; slot < _played.Length; slot++)
            if (!_present[slot]) _played[slot] = false;
        if (_started && now - _lastStart < EntityCueCadence.GapSeconds) return false;
        bool found = false;
        double mostOverdue = 0;
        foreach (var cue in selected)
        {
            if (!ValidSlot(cue.Slot)) continue;
            double elapsed = now - _last[cue.Slot];
            if (_played[cue.Slot] && elapsed < cue.IntervalSeconds) continue;
            double overdue = _played[cue.Slot] ? elapsed / Math.Max(cue.IntervalSeconds, double.Epsilon)
                : double.PositiveInfinity;
            if (found && overdue <= mostOverdue) continue;
            next = cue;
            mostOverdue = overdue;
            found = true;
        }
        return found;
    }

    internal void MarkPlayed(EnemyCue cue, double now)
    {
        if (!ValidSlot(cue.Slot) || !double.IsFinite(now)) return;
        _played[cue.Slot] = true;
        _last[cue.Slot] = now;
        _started = true;
        _lastStart = now;
    }

    internal void Reset()
    {
        Array.Clear(_played);
        Array.Clear(_last);
        _started = false;
        _lastStart = 0;
    }

    private static bool ValidSlot(int slot) => slot >= 0 && slot < EnemyCueSelector.SlotCount;
}
