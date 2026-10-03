using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation.Guidance;
using UnityEngine;
using UnityEngine.AI;

internal static partial class Program
{
    private static void RouteTests()
    {
        NavMesh.Reset();
        MapPlayerFloor.Agent = new();
        MapPlayerFloor.Allowance = 0.5;
        var world = new MapSectionWorldSnapshot(1, 1, 0, 0, 0, 0.5);
        var target = new Vector3(0, 0, 10);
        NavMesh.Corners = new[] { new Vector3(0, 0, 0), new Vector3(0.01f, 0, 0), target };
        var route = GuidanceRouteProbe.Read(world, target, verifyNext: true);
        Check(route.Complete && route.Next.z == 10, "verified open segment skips tiny already-reached native corner");
        Check(NavMesh.Calculations == 1, "first segment verification uses one calculated path");
        Check(NavMesh.Rays == 3, "candidate shortcut and resulting first segment both verified");

        NavMesh.Reset();
        NavMesh.Corners = new[] { new Vector3(0, 0, 0), new Vector3(0.4f, 0, 0), target };
        NavMesh.RayBlocked = (from, to) => to.z > 0;
        route = GuidanceRouteProbe.Read(world, target, verifyNext: true);
        Check(route.Complete && route.Next.x == 0.4f, "near wall bend retained when shortcut is blocked");
        Check(route.DirectOpen == false, "complete route can go around blocked direct destination bearing");

        NavMesh.Reset();
        NavMesh.Corners = new[] { new Vector3(0, 0, 0), new Vector3(2, 0, 0), target };
        NavMesh.RayBlocked = (from, to) => to.x > 0;
        route = GuidanceRouteProbe.Read(world, target, verifyNext: true);
        Check(!route.Complete, "dynamic first-segment obstruction silences passive guidance");
        Check(GuidanceRouteProbe.Read(world, target).Complete, "manual guidance default preserves existing behavior");

        NavMesh.Reset();
        NavMesh.Status = NavMeshPathStatus.PathPartial;
        Check(!GuidanceRouteProbe.Read(world, target, verifyNext: true).Complete, "partial route rejected");
        NavMesh.Reset();
        NavMesh.SettingsCount = 2;
        Check(!GuidanceRouteProbe.Read(world, target, verifyNext: true).Complete && NavMesh.Calculations == 0,
            "unsupported native filter arrangement never calls unsafe path overload");
        NavMesh.Reset();
    }
}
