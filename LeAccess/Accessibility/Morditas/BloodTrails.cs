using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Morditas;

// Where the game's blood trails last pointed for the local player, nearest first: scanner entries "blood trail N"
// and the drip's direction. See reference/rage-of-morditas.md#blood-trails.
internal sealed class BloodTrails
{
    internal const string KeyPrefix = "bloodtrail:";
    // Mod choice: the farthest trail still drips at this share of the nearest one's level.
    internal const float FarGain = 0.35f;
    private readonly List<Vector3> _targets = new();

    internal IReadOnlyList<Vector3> Targets => _targets;
    internal int Encounter { get; private set; }

    /// <summary>Counts the game's refreshes, so a new one is told apart from the same one read again.</summary>
    internal int Refreshes { get; private set; }

    internal void Refresh(int encounter, IEnumerable<Vector3> targets, Vector3 player)
    {
        Encounter = encounter;
        _targets.Clear();
        foreach (var target in targets)
            if (GuidanceGeometry.Finite(target)) _targets.Add(target);
        if (GuidanceGeometry.Finite(player))
            _targets.Sort((first, second) => Horizontal(first, player).CompareTo(Horizontal(second, player)));
        Refreshes++;
    }

    internal void Clear()
    {
        _targets.Clear();
        Encounter = 0;
    }

    internal static string Key(int number) => KeyPrefix + number.ToString(CultureInfo.InvariantCulture);

    internal static bool IsKey(string key) => key != null && key.StartsWith(KeyPrefix, StringComparison.Ordinal);

    internal int AddEntries(List<ExplorationEntry> entries, Func<int, string> name, float arrivalRadius)
    {
        for (int index = 0; index < _targets.Count; index++)
            entries.Add(new ExplorationEntry(Key(index + 1), name(index + 1), ExplorationCategory.BloodTrail,
                _targets[index], arrivalRadius));
        return _targets.Count;
    }

    // A locked trail follows the latest refresh: its own number while the game still draws it, else the nearest trail,
    // under the locked key so the beacon keeps its lock.
    internal static bool TryFollow(IReadOnlyList<ExplorationEntry> entries, string key, out ExplorationEntry trail)
    {
        trail = default;
        if (!IsKey(key)) return false;
        string nearest = Key(1);
        bool found = false;
        foreach (var entry in entries)
        {
            if (entry.Key == key) { trail = entry; return true; }
            if (entry.Key == nearest) { trail = entry with { Key = key }; found = true; }
        }
        return found;
    }

    /// <summary>Camera-relative pan and ahead of the nearest target, and its horizontal distance.</summary>
    internal bool TryNearest(Vector3 player, Vector3 forward, Vector3 right, out float pan, out float ahead,
        out float distance)
    {
        pan = ahead = distance = 0;
        if (_targets.Count == 0 || !GuidanceGeometry.Finite(player)) return false;
        var nearest = _targets[0];
        distance = Horizontal(nearest, player);
        foreach (var target in _targets)
        {
            float candidate = Horizontal(target, player);
            if (candidate < distance) { nearest = target; distance = candidate; }
        }
        GuidanceGeometry.Bearing(nearest - player, forward, right, out pan, out ahead);
        return true;
    }

    /// <summary>Full level up to the game's nearest trail distance, falling to FarGain at its farthest.</summary>
    internal static float Gain(float distance, float near, float far)
    {
        if (!float.IsFinite(distance) || !(far > near)) return 1;
        float t = Math.Clamp((distance - near) / (far - near), 0, 1);
        return 1 + (FarGain - 1) * t;
    }

    private static float Horizontal(Vector3 first, Vector3 second) =>
        new Vector2(first.X - second.X, first.Z - second.Z).Length();
}
