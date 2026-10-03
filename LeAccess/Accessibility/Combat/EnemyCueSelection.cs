using System;
using System.Collections.Generic;
using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Combat;

internal enum EnemyCueKind { Boss, Ordinary }

// Slot is the cadence stream: the boss class, or an ordinary enemy's rank by distance.
internal readonly record struct EnemyCue(int Id, EnemyCueKind Kind, float Distance, float Pan, float Forward,
    double IntervalSeconds, int Slot);

// Picks the enemy streams after D2RAccess entity_cues.cpp Select; see reference/enemy-cues.md.
internal sealed class EnemyCueSelector
{
    // D2R: three enemy streams, as diablo-access plays its three nearest emitters.
    internal const int MaxStreams = 3;
    internal const int BossSlot = 0;
    internal const int SlotCount = BossSlot + 1 + MaxStreams;
    private static readonly Comparison<(EnemyCue Cue, EnemyContact Contact)> ByDistance =
        (a, b) => Nearer(a.Cue, b.Cue) ? -1 : Nearer(b.Cue, a.Cue) ? 1 : 0;
    private readonly List<(EnemyCue Cue, EnemyContact Contact)> _ordinary = new();

    // The nearest boss in sight first, then the nearest ordinary enemies in sight in the streams left.
    internal void Select(IReadOnlyList<EnemyContact> contacts, Vector3 player, Vector3 forward, Vector3 right,
        float range, bool bosses, bool ordinary, Func<EnemyContact, TerrainSight> sight, List<EnemyCue> selected)
    {
        selected.Clear();
        _ordinary.Clear();
        if (contacts == null || sight == null || !GuidanceGeometry.Finite(player) ||
            !float.IsFinite(range) || range <= 0) return;
        EnemyCue? boss = null;
        foreach (var contact in contacts)
        {
            // A disabled boss class growls instead, as D2R does for a disabled ranged class.
            var kind = contact.Boss && bosses ? EnemyCueKind.Boss : EnemyCueKind.Ordinary;
            if (kind == EnemyCueKind.Ordinary && !ordinary || !GuidanceGeometry.Finite(contact.Position)) continue;
            var delta = contact.Position - player;
            float distance = delta.Length();
            if (!float.IsFinite(distance) || distance > range) continue;
            GuidanceGeometry.Bearing(delta, forward, right, out float pan, out float ahead);
            var cue = new EnemyCue(contact.Id, kind, distance, pan, ahead,
                EntityCueCadence.Interval(distance, range), BossSlot);
            // Ordinary enemies are ranked first so only the nearest few pay for a terrain ray.
            if (kind == EnemyCueKind.Ordinary) { _ordinary.Add((cue, contact)); continue; }
            if (boss.HasValue && !Nearer(cue, boss.Value) || !InSight(contact, sight(contact))) continue;
            boss = cue;
        }
        if (boss.HasValue) selected.Add(boss.Value);
        _ordinary.Sort(ByDistance);
        int rank = 0;
        foreach (var (cue, contact) in _ordinary)
        {
            if (selected.Count >= MaxStreams) break;
            if (!InSight(contact, sight(contact))) continue;
            selected.Add(cue with { Slot = BossSlot + 1 + rank++ });
        }
        _ordinary.Clear();
    }

    // A boss off the walkable surface (water, ledge) stays audible: it is still in targeting range.
    internal static bool InSight(EnemyContact contact, TerrainSight sight) =>
        sight == TerrainSight.Clear || sight == TerrainSight.OffMesh && contact.Boss;

    private static bool Nearer(EnemyCue a, EnemyCue b) =>
        a.Distance < b.Distance || a.Distance == b.Distance && a.Id < b.Id;
}
