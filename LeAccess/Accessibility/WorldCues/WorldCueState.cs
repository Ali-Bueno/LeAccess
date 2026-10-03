using System;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.WorldCues;

internal enum WorldCueCategory { Coins, Equipment, Interactable }
internal readonly record struct WorldCueKey(WorldCueCategory Category, int InstanceId);
internal readonly record struct WorldCueTarget(long Session, WorldCueKey Key, float Distance, float Range,
    float Pan, float Forward, NaturalCueKind? Sound = null, float Gain = 1);

// D2RAccess ground_item_service.cpp: exits, waypoints and the stash loop from beyond their
// native click radius so the player can walk to them; other objects keep the native radius.
internal static class WorldCueReach
{
    // A sounding target keeps its turn this far past its edge, so walking along the edge cannot chop it.
    internal const float ExitMargin = 0.5f;
    // Landmarks fade to this share of full volume at the edge of their audible range.
    internal const float FarGain = 0.35f;

    internal static bool IsLandmark(NaturalCueKind? kind) =>
        kind is NaturalCueKind.Portal or NaturalCueKind.ZoneTransition or NaturalCueKind.Stash;

    internal static float Limit(float range, bool held) => held ? range + ExitMargin : range;

    // Full volume inside the native radius, fading linearly to FarGain at the audible edge.
    internal static float Gain(float distance, float nativeRange, float audibleRange)
    {
        if (!(distance > nativeRange) || !(audibleRange > nativeRange)) return 1;
        float t = Math.Clamp((distance - nativeRange) / (audibleRange - nativeRange), 0, 1);
        return FarGain + (1 - FarGain) * (1 - t);
    }
}

internal sealed class WorldCueState
{
    // Match D2R's far NPC interval to bound bursts while cycling nearby objects.
    internal const double MinimumIntervalSeconds = 0.45;
    private long _session;
    private readonly double[] _lastPlayed = { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity };
    private double _nextCue;
    private double _lastTime;

    internal bool IsDue(WorldCueTarget target, double now)
    {
        if (!double.IsFinite(now)) return false;
        if (_session != target.Session || now < _lastTime) Reset();
        _session = target.Session;
        _lastTime = now;
        return now >= _nextCue;
    }

    internal bool TryChoose(WorldCueTarget?[] candidates, WorldCueTarget preferred, double now,
        out WorldCueTarget target)
    {
        for (int i = 0; i < _lastPlayed.Length; i++)
            if (!candidates[i].HasValue) _lastPlayed[i] = double.NegativeInfinity;
        target = preferred;
        bool found = false;
        foreach (var candidate in candidates)
        {
            if (!candidate.HasValue || !IsDue(candidate.Value, now)) continue;
            if (found)
            {
                double previous = _lastPlayed[(int)target.Key.Category];
                double next = _lastPlayed[(int)candidate.Value.Key.Category];
                if (next > previous || next == previous && target.Key == preferred.Key) continue;
                if (next == previous && candidate.Value.Key != preferred.Key && candidate.Value.Distance >= target.Distance) continue;
            }
            target = candidate.Value;
            found = true;
        }
        return found;
    }

    internal void MarkPlayed(WorldCueTarget target, double now)
    {
        _session = target.Session;
        _lastPlayed[(int)target.Key.Category] = now;
        _nextCue = now + MinimumIntervalSeconds;
        _lastTime = now;
    }

    internal void Reset()
    {
        _session = 0;
        Array.Fill(_lastPlayed, double.NegativeInfinity);
        _nextCue = _lastTime = 0;
    }
}
