using System;
using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.Vitals;

namespace LeAccess.Accessibility.Combat;

internal readonly record struct BossSample(int Id, string Name, int Percent, bool Dead);

internal readonly record struct BossLifeEntry(int Id, string Name, int Percent, bool Died);

internal sealed class BossLifeUpdate
{
    internal static readonly BossLifeUpdate Empty = new(new List<BossLifeEntry>());
    internal IReadOnlyList<BossLifeEntry> Entries { get; }
    internal bool HasContent => Entries.Count > 0;

    internal BossLifeUpdate(IReadOnlyList<BossLifeEntry> entries) { Entries = entries; }

    internal string Text(Func<string, int, string> format) =>
        string.Join(". ", Entries.Select(entry => format(entry.Name, entry.Percent)));
}

// Tracks each boss's life bands (D2R src/boss_life.cpp); output stays queued, never interrupting.
internal sealed class BossLifeState
{
    private readonly Dictionary<int, VitalsAnnouncementState> _bosses = new();

    internal BossLifeUpdate Observe(IReadOnlyList<BossSample> samples)
    {
        var seen = new HashSet<int>();
        var entries = new List<BossLifeEntry>();
        foreach (var sample in samples)
        {
            seen.Add(sample.Id);
            bool tracked = _bosses.TryGetValue(sample.Id, out var bands);
            if (sample.Dead || sample.Percent == 0)
            {
                // A boss never baselined while alive has nothing to announce.
                if (tracked) entries.Add(new BossLifeEntry(sample.Id, sample.Name, 0, true));
                continue;
            }
            if (sample.Percent < 0) continue;
            if (!tracked) _bosses[sample.Id] = bands = new VitalsAnnouncementState();
            var change = bands.Observe(sample.Percent, -1, acknowledge: false);
            if (change.HealthPercent >= 0)
                entries.Add(new BossLifeEntry(sample.Id, sample.Name, change.HealthPercent, false));
        }
        // Unloaded bosses were not seen dead, so they are forgotten silently.
        foreach (int id in _bosses.Keys.Where(id => !seen.Contains(id)).ToList()) _bosses.Remove(id);
        return entries.Count == 0 ? BossLifeUpdate.Empty : new BossLifeUpdate(entries);
    }

    internal void Acknowledge(BossLifeUpdate update)
    {
        foreach (var entry in update.Entries)
        {
            if (entry.Died) _bosses.Remove(entry.Id);
            else if (_bosses.TryGetValue(entry.Id, out var bands)) bands.Acknowledge(entry.Percent, -1);
        }
    }

    internal void Reset() => _bosses.Clear();
}
