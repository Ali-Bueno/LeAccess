using System;
using UnityEngine;
using UnityEngine.AI;

namespace LeAccess.Accessibility.Navigation;

// Temporary diagnostic (reference/navigation-radar.md#radar-trace-2026-10-11): an alternative reading logged beside the
// live one, never heard. Remove or adopt it once a play session's trace is analysed.
internal sealed partial class RadarWorldProbe
{
    private const int LaneCount = 3;
    private readonly RadarReading[] _shadowReadings = new RadarReading[4];
    private readonly float[] _laneDepths = new float[LaneCount];
    private readonly Vector3[] _laneHits = new Vector3[LaneCount];
    /// <summary>Off for the live probe's own query-count checks.</summary>
    internal bool ProbeShadowReadings { get; init; } = true;

    // The live rays, but from an origin moved off a touching wall and three across the body width, merged by their median.
    private float ProbeShadow(Vector3 origin, float bodyRadius, int agentTypeId, int areaMask)
    {
        float edge = float.NaN;
        Vector3 away = default;
        if (NavMesh.FindClosestEdgeFilter(origin, out var edgeHit, agentTypeId, areaMask))
        {
            edge = HorizontalDistance(origin, edgeHit.position);
            away = new Vector3(origin.x - edgeHit.position.x, 0f, origin.z - edgeHit.position.z);
        }
        Vector3 centre = origin;
        if (edge < bodyRadius)
        {
            // On the edge itself the edge gives no direction; the live rays that touch a wall do.
            if (away.sqrMagnitude <= Mathf.Epsilon) away = AwayFromTouchingWalls(bodyRadius);
            if (away.sqrMagnitude > Mathf.Epsilon)
            {
                away.Normalize();
                Vector3 moved = origin + away * (bodyRadius - edge);
                if (!NavMesh.RaycastFilter(origin, moved, out _, agentTypeId, areaMask)) centre = moved;
            }
        }

        for (int i = 0; i < _axes.Length; i++)
        {
            var direction = new Vector3(_axes[i].X, 0f, _axes[i].Y);
            var side = new Vector3(direction.z, 0f, -direction.x) * bodyRadius;
            Lane(0, origin, centre, centre, direction, agentTypeId, areaMask);
            Lane(1, origin, centre, centre + side, direction, agentTypeId, areaMask);
            Lane(2, origin, centre, centre - side, direction, agentTypeId, areaMask);
            int median = MedianLane();
            float depth = _laneDepths[median];
            bool continues = depth >= _settings.ReachDepth || depth < _settings.OpenDepth || depth >= _settings.PassageDepth ||
                Continues(_laneHits[median], direction, agentTypeId, areaMask);
            _shadowReadings[i] = new RadarReading(depth, float.IsFinite(depth) && depth >= 0f, continues);
        }
        return edge;
    }

    // Depth along the axis measured from the player's origin; a lane whose start lies beyond a wall is blocked at once.
    private void Lane(int lane, Vector3 origin, Vector3 centre, Vector3 start, Vector3 direction, int agentTypeId, int areaMask)
    {
        if (HorizontalDistance(start, centre) > 0f)
        {
            if (NavMesh.RaycastFilter(centre, start, out var reach, agentTypeId, areaMask))
            {
                _laneDepths[lane] = 0f;
                _laneHits[lane] = reach.position;
                return;
            }
        }
        float offset = (start.x - origin.x) * direction.x + (start.z - origin.z) * direction.z;
        Vector3 target = start + direction * _settings.ReachDepth;
        bool blocked = NavMesh.RaycastFilter(start, target, out var hit, agentTypeId, areaMask);
        _laneDepths[lane] = Math.Max(0f, offset + (blocked ? HorizontalDistance(start, hit.position) : _settings.ReachDepth));
        _laneHits[lane] = blocked ? hit.position : target;
    }

    // The lane that is neither the shortest nor the last-found longest; ties resolve to an equal depth.
    private int MedianLane()
    {
        int shortest = 0, longest = 0;
        for (int lane = 1; lane < LaneCount; lane++)
        {
            if (_laneDepths[lane] < _laneDepths[shortest]) shortest = lane;
            if (_laneDepths[lane] >= _laneDepths[longest]) longest = lane;
        }
        return LaneCount * (LaneCount - 1) / 2 - shortest - longest;
    }

    private Vector3 AwayFromTouchingWalls(float bodyRadius)
    {
        Vector3 away = default;
        for (int i = 0; i < _axes.Length; i++)
            if (_readings[i].Available && _readings[i].Depth < bodyRadius)
                away = away - new Vector3(_axes[i].X, 0f, _axes[i].Y);
        return away;
    }
}
