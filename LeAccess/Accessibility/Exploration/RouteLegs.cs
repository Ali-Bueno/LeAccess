using System;
using System.Collections.Generic;
using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Exploration;

// One straight stretch of a route: its screen direction key, horizontal length and height gained.
internal readonly record struct RouteLeg(string Direction, float Length, float Rise);

// A route read the way a sighted player reads it off the minimap: stretches by screen direction, climbs included.
internal static class RouteLegs
{
    /// <summary>The stretches between a route's corners, consecutive ones in the same direction joined.</summary>
    internal static List<RouteLeg> From(IReadOnlyList<Vector3> corners, Vector3 forward, Vector3 right)
    {
        var legs = new List<RouteLeg>();
        if (corners is null) return legs;
        for (int index = 1; index < corners.Count; index++)
        {
            var delta = corners[index] - corners[index - 1];
            string direction = GuidanceGeometry.Direction(delta, forward, right);
            if (direction == null) continue;
            float length = new Vector2(delta.X, delta.Z).Length();
            if (legs.Count > 0 && legs[^1].Direction == direction)
                legs[^1] = legs[^1] with { Length = legs[^1].Length + length, Rise = legs[^1].Rise + delta.Y };
            else legs.Add(new RouteLeg(direction, length, delta.Y));
        }
        return legs;
    }

    /// <summary>Up to limit legs as one phrase, then how many more; null when fewer than two legs are a step long.</summary>
    internal static string Describe(IReadOnlyList<RouteLeg> legs, int limit, float stepLength, float levelHeight,
        ExplorationText text)
    {
        var spoken = new List<(RouteLeg Leg, int Steps)>();
        foreach (var leg in legs)
        {
            int steps = ExplorationCatalog.Steps(leg.Length, stepLength);
            if (steps >= 1) spoken.Add((leg, steps));
        }
        if (limit < 1 || spoken.Count < 2) return null;
        string phrase = null;
        for (int index = 0; index < Math.Min(limit, spoken.Count); index++)
        {
            var (leg, steps) = spoken[index];
            string part = text("exploration.route_leg", text(leg.Direction),
                steps == 1 ? text("exploration.steps_one") : text("exploration.steps_many", steps));
            // A stretch that changes floor says so, with the same floor height as the level phrase.
            if (float.IsFinite(levelHeight) && levelHeight > 0 && Math.Abs(leg.Rise) >= levelHeight)
                part = text(leg.Rise > 0 ? "exploration.route_leg_up" : "exploration.route_leg_down", part);
            phrase = phrase == null ? part : text("exploration.route_leg_then", phrase, part);
        }
        int left = spoken.Count - limit;
        return left <= 0 ? phrase
            : text(left == 1 ? "exploration.route_legs_more_one" : "exploration.route_legs_more_many", phrase, left);
    }
}
