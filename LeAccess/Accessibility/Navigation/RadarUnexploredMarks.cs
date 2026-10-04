using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Navigation;

// D2R's unexplored qualifier: an opening cue toward a screen quadrant holding an unexplored area the
// scanner lists ends in a double pip. No cue of its own. See reference/navigation-radar.md#unexplored-mark.
public static class RadarUnexploredMarks
{
    // The scanner republishes twice a second; positions older than a few refreshes are not trusted.
    public const double StaleSeconds = 2;
    private static readonly List<Vector2> Areas = new();
    private static double _published = double.NegativeInfinity;
    private static float _range;

    public static int Count => Areas.Count;

    // Areas on the XZ plane; range is how far an area may lie and still count as "that way".
    public static void Publish(IEnumerable<Vector2> areas, float range, double now)
    {
        Areas.Clear();
        _range = float.IsFinite(range) && range > 0 ? range : 0;
        _published = now;
        if (areas is null) return;
        foreach (var area in areas)
            if (float.IsFinite(area.X) && float.IsFinite(area.Y)) Areas.Add(area);
    }

    public static void Clear()
    {
        Areas.Clear();
        _published = double.NegativeInfinity;
    }

    // The quadrant is the dominant screen axis of the offset, as in D2R's frontier vote.
    public static bool Marked(RadarDirection direction, Vector2 position, Vector2[] axes, double now)
    {
        if (Areas.Count == 0 || _range <= 0 || !(now - _published <= StaleSeconds) || now < _published ||
            axes is null || axes.Length != RadarEngine.DirectionCount) return false;
        var up = axes[(int)RadarDirection.Up];
        var right = axes[(int)RadarDirection.Right];
        foreach (var area in Areas)
        {
            var offset = area - position;
            if (offset.LengthSquared() > _range * _range) continue;
            float ahead = Vector2.Dot(offset, up), side = Vector2.Dot(offset, right);
            var quadrant = MathF.Abs(ahead) >= MathF.Abs(side)
                ? ahead >= 0 ? RadarDirection.Up : RadarDirection.Down
                : side >= 0 ? RadarDirection.Right : RadarDirection.Left;
            if (quadrant == direction) return true;
        }
        return false;
    }
}
