using System;
using LeAccess.Accessibility.MapSections;
using UnityEngine;
using UnityEngine.AI;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal readonly record struct GuidanceRoute(bool Complete, bool? DirectOpen, float Distance,
    Vector3 Next, float BodyWidth);

internal static class GuidanceRouteProbe
{
    private static NavMeshPath _path;

    internal static GuidanceRoute Read(MapSectionWorldSnapshot world, Vector3 destination, bool verifyNext = false)
    {
        var position = new Vector3((float)world.X, (float)world.Y, (float)world.Z);
        float directDistance = Vector3.Distance(position, destination);
        float width = (float)world.BodyRadius * 2;
        var unknown = new GuidanceRoute(false, null, directDistance, destination, width);
        if (!GuidanceGeometry.Finite(Convert(position)) || !GuidanceGeometry.Finite(Convert(destination))) return unknown;
        if (!MapPlayerFloor.TryGetQueryAgent(world, out var agent)) return unknown;
        float radius = agent.radius;
        var settings = NavMesh.GetSettingsByID(agent.agentTypeID);
        if (float.IsFinite(settings.agentRadius)) radius = Math.Max(radius, settings.agentRadius);
        if (!float.IsFinite(radius) || radius <= 0 || agent.areaMask == 0) return unknown;
        width = Math.Max(width, radius * 2);
        unknown = unknown with { BodyWidth = width };
        double heightAllowance = MapPlayerFloor.HeightAllowance(world);
        if (!double.IsFinite(heightAllowance) || heightAllowance < 0) return unknown;
        if (!NavMesh.SamplePositionFilter(position, out var origin, radius, agent.agentTypeID, agent.areaMask) ||
            Math.Abs(origin.position.y - position.y) > heightAllowance ||
            !NavMesh.SamplePositionFilter(destination, out var goal, radius, agent.agentTypeID, agent.areaMask) ||
            Math.Abs(goal.position.y - destination.y) > heightAllowance ||
            !GuidanceGeometry.Finite(Convert(origin.position)) || !GuidanceGeometry.Finite(Convert(goal.position))) return unknown;
        bool blocked = NavMesh.RaycastFilter(origin.position, goal.position, out _, agent.agentTypeID, agent.areaMask);
        unknown = unknown with { DirectOpen = !blocked };
        // The filter-object overload has malformed restored IL. The native mask
        // overload is only used when the installed NavMesh has one matching agent type.
        if (NavMesh.GetSettingsCount() != 1 || NavMesh.GetSettingsByIndex(0).agentTypeID != agent.agentTypeID)
            return unknown;
        _path ??= new NavMeshPath();
        _path.ClearCorners();
        if (!NavMesh.CalculatePath(origin.position, goal.position, agent.areaMask, _path) ||
            _path.status != NavMeshPathStatus.PathComplete) return unknown;
        var nativeCorners = _path.corners;
        if (nativeCorners is null || nativeCorners.WasCollected) return unknown;
        var corners = new NVector3[nativeCorners.Length];
        for (int i = 0; i < corners.Length; i++) corners[i] = Convert(nativeCorners[i]);
        if (!GuidanceGeometry.TryRoute(corners, Convert(origin.position), Convert(goal.position), radius,
            out float distance, out var next)) return unknown;
        if (verifyNext)
        {
            // Skip already reached corners only when the player's current floor has an open direct segment.
            for (int i = 2; i < corners.Length && NVector3.Distance(Convert(origin.position), next) <= radius; i++)
            {
                var candidate = new Vector3(corners[i].X, corners[i].Y, corners[i].Z);
                if (NavMesh.RaycastFilter(origin.position, candidate, out _, agent.agentTypeID, agent.areaMask)) break;
                next = corners[i];
            }
            var nextPoint = new Vector3(next.X, next.Y, next.Z);
            if (NavMesh.RaycastFilter(origin.position, nextPoint, out _, agent.agentTypeID, agent.areaMask)) return unknown;
        }
        return new GuidanceRoute(true, !blocked, distance, new Vector3(next.X, next.Y, next.Z), width);
    }

    internal static NVector3 Convert(Vector3 point) => new(point.x, point.y, point.z);
    internal static void Reset() { _path = null; }
}
