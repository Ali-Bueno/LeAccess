using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class VisitedRoomTests
{
    internal static void Run()
    {
        OnlyVisitedRoomsAreSeeded();
        OtherFloorsStayUnexplored();
        MeshReadRetriesAreSpaced();
        RegionScanIsBounded();
        DetourThroughVisitedRoomRoutes();
    }

    // A 40 x 10 floor at y 0 (two triangles) and a 40 x 10 floor at y 10 above it.
    private static bool ReadTwoFloors(NavMeshSeed seed)
    {
        var vertices = new[]
        {
            new Vector3(0, 0, 0), new Vector3(40, 0, 0), new Vector3(40, 0, 10), new Vector3(0, 0, 10),
            new Vector3(0, 10, 0), new Vector3(40, 10, 0), new Vector3(40, 10, 10), new Vector3(0, 10, 10)
        };
        seed.Begin(vertices, new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 }, new int[4], 1);
        return true;
    }

    private static int Drain(VisitedRoomSeed rooms, long session, ExploredMap map, List<MinimapRoom> visited)
    {
        int finished = 0;
        for (int step = 0; step < 50; step++)
        {
            finished += rooms.Update(session, map, visited, 0, ReadTwoFloors, out _);
            if (!rooms.Running && step > 0) break;
        }
        return finished;
    }

    private static void OnlyVisitedRoomsAreSeeded()
    {
        var rooms = new VisitedRoomSeed();
        var map = new ExploredMap();
        var visited = new List<MinimapRoom> { new(0, new Vector3(5, 0, 5), 6, true) };
        Check(Drain(rooms, 1, map, visited) == 1, "the visited room is seeded");
        Check(map.Explored(new Vector3(5, 0, 5)) && map.Explored(new Vector3(9, 0, 5)), "its ground is explored");
        Check(!map.Explored(new Vector3(20, 0, 5)) && !map.Explored(new Vector3(35, 0, 5)), "ground outside it is not");
        int cells = map.CellCount;
        Check(rooms.Update(1, map, visited, 0, ReadTwoFloors, out _) == 0 && map.CellCount == cells, "a seeded room is not seeded again");
        visited.Add(new MinimapRoom(1, new Vector3(35, 0, 5), 6, false));
        Check(Drain(rooms, 1, map, visited) == 1 && map.Explored(new Vector3(35, 0, 5)), "a room visited later is added");
        Check(rooms.SeededRooms == 2, "both rooms counted");
        var next = new ExploredMap();
        Check(Drain(rooms, 2, next, visited) == 2 && next.Explored(new Vector3(5, 0, 5)), "a new scene session seeds again");
    }

    private static void OtherFloorsStayUnexplored()
    {
        var rooms = new VisitedRoomSeed();
        var map = new ExploredMap();
        Drain(rooms, 1, map, new List<MinimapRoom> { new(0, new Vector3(5, 0, 5), 6, true) });
        Check(!map.Explored(new Vector3(5, 10, 5)), "the floor above, outside the room's sphere, stays unexplored");
    }

    private static void MeshReadRetriesAreSpaced()
    {
        var rooms = new VisitedRoomSeed();
        var map = new ExploredMap();
        var visited = new List<MinimapRoom> { new(0, new Vector3(5, 0, 5), 6, true) };
        int reads = 0;
        bool Fail(NavMeshSeed _) { reads++; return false; }
        rooms.Update(1, map, visited, 0, Fail, out _);
        rooms.Update(1, map, visited, 1, Fail, out _);
        Check(reads == 1 && map.CellCount == 0, "a failed mesh read waits before retrying");
        rooms.Update(1, map, visited, VisitedRoomSeed.RetrySeconds + 0.1, Fail, out _);
        Check(reads == 2, "and retries after the interval");
        Check(rooms.Update(1, map, null, 99, ReadTwoFloors, out _) == 0, "no rooms, nothing to do");
        var bad = new List<MinimapRoom> { new(3, new Vector3(float.NaN, 0, 0), 6, true), new(4, Vector3.Zero, 0, true) };
        Check(rooms.Update(1, map, bad, 99, ReadTwoFloors, out _) == 0 && map.CellCount == 0, "invalid rooms are skipped");
    }

    // Far triangles are rejected cheaply, but a step still looks at a bounded number of them.
    private static void RegionScanIsBounded()
    {
        int far = NavMeshSeed.TrianglesPerStep * NavMeshSeed.RegionScanFactor * 2;
        var vertices = new[] { new Vector3(500, 0, 500), new Vector3(502, 0, 500), new Vector3(500, 0, 502) };
        var seed = new NavMeshSeed();
        seed.Begin(vertices, Enumerable.Range(0, far).SelectMany(_ => new[] { 0, 1, 2 }).ToArray(), new int[far], 1);
        seed.Restart(new[] { (Vector3.Zero, 5f) });
        var map = new ExploredMap();
        seed.Step(map);
        Check(!seed.Done && map.CellCount == 0, "one step scans a bounded number of triangles");
        seed.Step(map);
        Check(seed.Done && map.CellCount == 0, "the scan ends without cells outside the region");
        seed.Restart(Array.Empty<(Vector3, float)>());
        seed.Step(map);
        Check(map.CellCount > 0, "without regions the whole mesh counts again");
    }

    // A U-shaped detour (down, right, up) through a room never seen from close up routes once that room is seeded.
    private static void DetourThroughVisitedRoomRoutes()
    {
        // 26 x 10 tiles; a wall across x 10..15 from z 0 to 5, open ground beyond z 6.
        var grid = new FakeGrid(Enumerable.Range(0, 10).Select(z => new string(Enumerable.Range(0, 26)
            .Select(x => x >= 10 && x < 16 && z < 6 ? '#' : '.').ToArray())).ToArray());
        var map = new ExploredMap();
        map.Import(new[] { new Vector3(1, 0, 1), new Vector3(3, 0, 1), new Vector3(23, 0, 1), new Vector3(25, 0, 1) });
        var from = new Vector3(1, 0, 1);
        var target = new Vector3(25, 0, 1);
        Check(!map.TryRoute(from, target, 1.5f, grid, out _, out _), "without the room no route is known");
        var rooms = new VisitedRoomSeed();
        bool ReadFloor(NavMeshSeed seed)
        {
            seed.Begin(new[] { new Vector3(0, 0, 0), new Vector3(26, 0, 0), new Vector3(26, 0, 10), new Vector3(0, 0, 10) },
                new[] { 0, 1, 2, 0, 2, 3 }, new int[2], 1);
            return true;
        }
        for (int step = 0; step < 10; step++)
            rooms.Update(1, map, new List<MinimapRoom> { new(0, new Vector3(13, 0, 8), 14, true) }, 0, ReadFloor, out _);
        Check(map.TryRoute(from, target, 1.5f, grid, out var path, out _), $"the seeded room gives the detour ({map.LastFailure})");
        Check(path.Any(point => point.Z > 6), "the route goes around the wall");
    }
}
