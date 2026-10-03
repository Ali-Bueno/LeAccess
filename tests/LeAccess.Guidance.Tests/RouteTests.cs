using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation.Guidance;
using UnityEngine;
using UnityEngine.AI;
using static Program;

internal static class RouteTests
{
    internal static readonly Vector3 Goal = new(4, 0, 3);
    internal static void Reset()
    {
        GuidanceRouteProbe.Reset(); NavMesh.Reset(); MapPlayerFloor.Agent = new(); MapPlayerFloor.Allowance = 0.5;
        MapSectionWorld.World = new(1, 1, 0, 0, 0, 0.5); MapSectionWorld.Available = true;
    }
    private static GuidanceRoute Read() => GuidanceRouteProbe.Read(MapSectionWorld.World, Goal);

    internal static void Run()
    {
        Reset();
        NavMesh.Blocked = true;
        NavMesh.Corners = new[] { default(Vector3), new Vector3(0, 0, 3), Goal };
        var route = Read();
        Check(route.Complete && route.DirectOpen == false, "A complete path around a wall can be reported while direct bearing is blocked.");
        Near(route.Distance, 7, "Native detour distance uses the full walkable path.");
        Near(route.Next.x, 0, "Next turn does not point diagonally through the wall.");
        Near(route.Next.z, 3, "The first walkable corner supplies guidance.");
        Near(route.BodyWidth, 1, "Baked navigation radius protects against a tiny runtime agent.");
        Check(NavMesh.Filters.Take(2).All(f => f.Radius == 0.5f) && NavMesh.Calculations == 1,
            "Origin and destination sample with the authoritative baked radius.");
        foreach (var status in new[] { NavMeshPathStatus.PathPartial, NavMeshPathStatus.PathInvalid })
        {
            Reset(); NavMesh.Status = status; NavMesh.Blocked = true; route = Read();
            Check(!route.Complete && route.DirectOpen == false, "Partial and invalid native paths never become verified guidance.");
            Near(route.Distance, 5, "Unverified route falls back to explicitly direct distance.");
        }
        foreach (Action invalidate in new Action[] { () => NavMesh.PathAvailable = false, () => NavMesh.NullCorners = true,
            () => NavMesh.CollectedCorners = true, () => NavMesh.Corners = Array.Empty<Vector3>(),
            () => NavMesh.Corners = new[] { default(Vector3), new Vector3(float.NaN, 0, 0), Goal },
            () => NavMesh.Corners = new[] { default(Vector3), new Vector3(0, 0, 1) },
            () => NavMesh.Corners = new[] { new Vector3(2, 0, 0), Goal } })
        {
            Reset(); invalidate(); route = Read();
            Check(!route.Complete && route.DirectOpen == true, "Path failure or untrusted corner data retains only measured direct visibility.");
        }
        foreach (Action changeType in new Action[] { () => NavMesh.SettingsCount = 2, () => NavMesh.SettingsCount = 0,
            () => NavMesh.SettingsType = 42 })
        {
            Reset(); changeType(); route = Read();
            Check(!route.Complete && route.DirectOpen == true && NavMesh.Calculations == 0,
                "Unfiltered native path API is not called for ambiguous or mismatched agent types.");
        }
        Reset(); MapPlayerFloor.Agent.agentTypeID = NavMesh.SettingsType = 42; MapPlayerFloor.Agent.areaMask = 8;
        Check(Read().Complete && NavMesh.Filters.All(f => f.Type == 42 && f.Mask == 8),
            "Verified nondefault singleton agent type preserves its area mask in all filtered queries.");
        foreach (Action unavailable in new Action[] { () => MapPlayerFloor.Agent = null, () => MapPlayerFloor.Agent.areaMask = 0,
            () => { MapPlayerFloor.Agent.radius = float.NaN; NavMesh.BakedRadius = float.NaN; },
            () => MapPlayerFloor.Allowance = double.NaN })
        {
            Reset(); unavailable(); route = Read();
            Check(!route.Complete && route.DirectOpen is null && NavMesh.Samples == 0,
                "Missing native agent, mask, radius or floor allowance cannot trigger geometry queries.");
        }
        foreach (Action badSample in new Action[] { () => NavMesh.OriginAvailable = false, () => NavMesh.GoalAvailable = false,
            () => NavMesh.OriginOffset = new(0, 0.6f, 0), () => NavMesh.GoalOffset = new(0, 0.6f, 0),
            () => NavMesh.OriginOffset = new(float.NaN, 0, 0), () => NavMesh.GoalOffset = new(0, float.NaN, 0) })
        {
            Reset(); badSample(); route = Read();
            Check(!route.Complete && route.DirectOpen is null && NavMesh.Rays == 0 && NavMesh.Calculations == 0,
                "Missing, invalid or wrong-floor endpoint samples cannot establish direct visibility or a route.");
        }
        Reset();
        Check(!GuidanceRouteProbe.Read(MapSectionWorld.World, new(float.NaN, 0, 0)).Complete && NavMesh.Samples == 0,
            "An invalid destination never reaches native NavMesh queries.");
        MapSectionWorld.World = MapSectionWorld.World with { X = double.NaN };
        Check(!Read().Complete && NavMesh.Samples == 0, "An invalid source never reaches native NavMesh queries.");
        Reset();
        Check(Read().Complete, "Prepare a cached complete route.");
        NavMesh.PreserveCorners = true;
        Check(!Read().Complete, "A subsequent native result without fresh corners cannot reuse a previous complete route.");
        Reset();
    }
}
