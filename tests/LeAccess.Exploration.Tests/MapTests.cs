using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class MapTests
{
    // The scenarios below are sized for this radius, independent of the production default.
    private const float TestRadius = 8f;

    internal static void Run()
    {
        LineOfSight();
        Corridor();
        Detour();
        Door();
        TwoFloors();
        Memory();
        Winding();
    }

    private static void LineOfSight()
    {
        var grid = new FakeGrid(Rows(20, 20, (x, _) => x == 10 ? '#' : '.'));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        var player = new Vector3(5.5f, 0, 10.5f);
        map.Observe(player, grid);
        Check(grid.Samples <= 1 + map.QueriesPerObserve, $"one observe is capped ({grid.Samples} samples)");
        Settle(map, grid, player);
        Check(map.Explored(new Vector3(7.5f, 0, 10.5f)), "open ground in sight is explored");
        Check(map.Explored(new Vector3(5.5f, 0, 16.5f)), "ground within the radius is explored");
        Check(!map.Explored(new Vector3(12.5f, 0, 10.5f)), "ground behind the wall stays unexplored");
        Check(!map.Explored(new Vector3(5.5f, 0, 19.5f)), "ground beyond the radius stays unexplored");
        grid.ResetCounters();
        map.Observe(player, grid);
        Check(grid.Samples == 1 && grid.Clears == 0, "standing still costs one sample once settled");
        int cells = map.CellCount;
        Check(map.Observe(new Vector3(float.NaN, 0, 0), grid) == 0 && map.CellCount == cells, "invalid position ignored");
        Check(map.Observe(player, null) == 0, "missing walkability ignored");
    }

    private static void Corridor()
    {
        // Room A (x 0-9), a corridor (z 4-5) twenty units long, room B (x 30-39).
        var grid = new FakeGrid(Rows(40, 10, (x, z) => x < 10 || x >= 30 || z is 4 or 5 ? '.' : '#'));
        var a = new Vector3(5f, 0, 5f);
        var b = new Vector3(35f, 0, 5f);
        var walked = new ExploredMap { ExploreRadius = TestRadius };
        Walk(walked, grid, a, b);
        Check(walked.TryRoute(b, a, 1.5f, grid, out var path, out float length), "route back through the walked corridor");
        Near(30, length, 1, "corridor route length");
        Check(Walkable(path, grid), "every corridor leg is walkable");
        Check(path.Count == 2, "a straight corridor is pulled to one leg");
        Check(ExploredMap.StraightClear(b, a, grid), "a straight corridor reads straight");

        var skipped = new ExploredMap { ExploreRadius = TestRadius };
        Settle(skipped, grid, a);
        Settle(skipped, grid, b);
        Check(!skipped.Explored(new Vector3(20, 0, 5)), "the corridor middle was never seen");
        Check(!skipped.TryRoute(b, a, 1.5f, grid, out var none, out _) && none.Count == 0, "refused across unexplored ground");
        Check(skipped.LastFailure == "no explored route", $"refusal reason ({skipped.LastFailure})");

        var target = new ExplorationTarget("room-a", "Room A", a, 0);
        var state = new BeaconState();
        Check(state.Lock(target, b, 0, Router(skipped, grid)) == BeaconLock.Refused && state.LockedKey == null,
            "the beacon refuses a lock with no explored route");
        Check(state.Lock(target, b, 0, Router(walked, grid)) == BeaconLock.Locked && state.LockedKey == "room-a",
            "the beacon locks along explored ground");
    }

    private static void Detour()
    {
        var grid = new FakeGrid(Rows(30, 30, (x, z) => x == 15 && z >= 5 && z < 25 ? '#' : '.'));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        for (int z = 1; z < 30; z += 3)
            for (int x = 1; x < 30; x += 3) Settle(map, grid, new Vector3(x + 0.5f, 0, z + 0.5f));
        var from = new Vector3(10.5f, 0, 15.5f);
        var behind = new Vector3(20.5f, 0, 15.5f);
        Check(map.TryRoute(from, behind, 1.5f, grid, out var path, out float length), "route around the wall");
        Check(length > 21, $"the detour is longer than the wall end allows ({length})");
        Check(Walkable(path, grid), "detour legs are walkable");
        Check(!ExploredMap.StraightClear(from, behind, grid), "a blocked straight line reads around");
        var open = new Vector3(10.5f, 0, 25.5f);
        Check(ExploredMap.StraightClear(from, open, grid), "an open straight line reads straight");
        Check(map.TryRoute(from, open, 1.5f, grid, out var direct, out float directLength) && direct.Count == 2,
            "a direct route is one leg");
        Near(10, directLength, 0.2, "direct route length");
        Check(map.TryRoute(from, from + new Vector3(1, 0, 0), 1.5f, grid, out var here, out _) && here.Count == 2,
            "a target within arrival is reached at once");

        map.MaxExpansions = 5;
        Check(!map.TryRoute(from, behind, 1.5f, grid, out _, out _), "the expansion cap refuses");
        Check(map.LastFailure == "expansion cap" && map.LastExpansions == 6, $"cap reason ({map.LastFailure}, {map.LastExpansions})");
        map.MaxExpansions = ExploredMap.DefaultMaxExpansions;
        Check(map.TryRoute(from, behind, 1.5f, grid, out _, out _) && map.LastExpansions <= ExploredMap.DefaultMaxExpansions,
            "the default cap routes again");
        Check(!map.TryRoute(new Vector3(-10, 0, -10), behind, 1.5f, grid, out _, out _) &&
            map.LastFailure == "player off NavMesh", "a player off the walkable ground has no route");
    }

    // A saved map of twenty lanes joined at alternate ends: the route walks them all, past the old 6000-cell cap.
    private static void Winding()
    {
        const int Size = 200, OldCap = 6000;
        static char Tile(int x, int z) => z % 10 != 9 || (z / 10 % 2 == 0 ? x >= Size - 4 : x < 4) ? '.' : '#';
        var grid = new FakeGrid(Rows(Size, Size, Tile));
        var map = Imported(Size, Size, Tile, (_, z) => z < Size - 10);
        var from = new Vector3(100.5f, 0, 1.5f);
        var top = new Vector3(100.5f, 0, Size - 12.5f);
        Check(map.TryRoute(from, top, 1.5f, grid, out var path, out float length) && map.LastExpansions > OldCap,
            $"a route over the whole saved map is found ({map.LastFailure}, {map.LastExpansions})");
        Check(length > 17 * Size && Walkable(path, grid), $"it walks every lane ({length})");
        Check(!map.TryRoute(from, new Vector3(100.5f, 0, Size - 1.5f), 1.5f, grid, out _, out _) &&
            map.LastFailure == "no explored route" && map.LastExpansions == 0, "a target beyond explored ground is refused without a search");
    }

    private static void Door()
    {
        var grid = new FakeGrid(Rows(20, 20, (x, z) => x != 10 ? '.' : z is 10 or 11 ? 'D' : '#'));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        var west = new Vector3(5.5f, 0, 10.5f);
        var east = new Vector3(15.5f, 0, 10.5f);
        Settle(map, grid, west);
        Settle(map, grid, east);
        Check(!map.TryRoute(east, west, 1.5f, grid, out _, out _), "a closed door refuses");
        grid.DoorOpen = true;
        Check(!map.TryRoute(east, west, 1.5f, grid, out _, out _), "an opened door is re-proved only beside it");
        Settle(map, grid, new Vector3(11.5f, 0, 10.5f));
        Check(map.TryRoute(east, west, 1.5f, grid, out var path, out _), "the opened door routes once observed beside it");
        Check(Walkable(path, grid), "the route passes the door");
        grid.DoorOpen = false;
        Settle(map, grid, new Vector3(12.5f, 0, 12.5f));
        Settle(map, grid, new Vector3(11.5f, 0, 10.5f));
        Check(!map.TryRoute(east, west, 1.5f, grid, out _, out _), "a closed door is re-proved beside it");
    }

    private static void TwoFloors()
    {
        // A bridge deck over open ground, reached by a ramp from x 1 to 9.
        static bool Ramp(int x, int z) => x >= 1 && x <= 9 && z is 6 or 7;
        var ground = Rows(30, 14, (x, z) => Ramp(x, z) ? '#' : '.');
        var upper = Rows(30, 14, (x, z) => z is not (6 or 7) ? ' ' : Ramp(x, z) ? (char)('0' + x) : x >= 10 && x <= 27 ? '=' : ' ');
        var grid = new FakeGrid(ground, upper);
        var map = new ExploredMap { ExploreRadius = TestRadius };
        for (int z = 1; z < 14; z += 3)
            for (int x = 1; x < 30; x += 3)
                if (!Ramp(x, z)) Settle(map, grid, new Vector3(x + 0.5f, 0, z + 0.5f));
        for (int x = 0; x <= 27; x++)
            Settle(map, grid, new Vector3(x + 0.5f, x <= 9 ? x * FakeGrid.RampStep : FakeGrid.UpperHeight, 6.5f));
        var deck = new Vector3(20.5f, FakeGrid.UpperHeight, 6.5f);
        var below = new Vector3(20.5f, 0, 2.5f);
        Check(map.Explored(new Vector3(20.5f, 0, 6.5f)) && map.Explored(deck), "the bridge and the ground below are separate cells");
        Check(map.TryRoute(deck, below, 1.5f, grid, out var down, out float downLength), "route from the bridge to the ground");
        Check(downLength > 30, $"the route goes down the ramp ({downLength})");
        Check(Walkable(down, grid), "no leg jumps between floors going down");
        Check(!ExploredMap.StraightClear(deck, below, grid), "the ground below the bridge is not straight ahead");
        Check(map.TryRoute(below, deck, 1.5f, grid, out var up, out float upLength) && upLength > 30, "route back up the ramp");
        Check(Walkable(up, grid), "no leg jumps between floors going up");
    }

    private static void Memory()
    {
        var memory = new ExploredMemory(() => new ExploredMap { ExploreRadius = TestRadius });
        var town = memory.Resolve("Town", 0, 1, true);
        Check(ReferenceEquals(town, memory.Resolve("Town", 0, 2, true)), "a fixed layout keeps its map across sessions");
        Check(!ReferenceEquals(town, memory.Resolve("Town", 1, 2, true)), "another variant has its own map");
        var dungeon = memory.Resolve("Dungeon", null, 3, false);
        Check(ReferenceEquals(dungeon, memory.Resolve("Dungeon", null, 3, false)), "a procedural map lasts its session");
        Check(ReferenceEquals(town, memory.Resolve("Town", 0, 4, true)), "returning to town keeps its map");
        var next = memory.Resolve("Dungeon", null, 5, false);
        Check(!ReferenceEquals(dungeon, next), "a new procedural session starts over");
        Check(!ReferenceEquals(dungeon, memory.Resolve("Dungeon", null, 3, false)), "an old procedural session is gone");
        Check(memory.FixedCount == 2, "fixed maps counted");
        memory.Clear();
        Check(!ReferenceEquals(town, memory.Resolve("Town", 0, 1, true)) && memory.FixedCount == 1, "clear forgets everything");
    }

    internal static BeaconRouter Router(ExploredMap map, IWalkability nav) =>
        (Vector3 from, ExplorationTarget target, out IReadOnlyList<Vector3> path, out float length) =>
            map.TryRoute(from, target.Position, BeaconState.ArrivalRadius(target), nav, out path, out length, target.Area);

    internal static string[] Rows(int width, int depth, Func<int, int, char> tile) =>
        Enumerable.Range(0, depth).Select(z => new string(Enumerable.Range(0, width).Select(x => tile(x, z)).ToArray())).ToArray();

    // Every walkable column explored, as a saved map restores it.
    internal static ExploredMap Imported(int width, int depth, Func<int, int, char> tile, Func<int, int, bool> keep = null)
    {
        var map = new ExploredMap();
        var cells = new List<Vector3>();
        for (float z = map.CellSize / 2; z < depth; z += map.CellSize)
            for (float x = map.CellSize / 2; x < width; x += map.CellSize)
                if (tile((int)x, (int)z) == '.' && (keep?.Invoke((int)x, (int)z) ?? true)) cells.Add(new Vector3(x, 0, z));
        map.Import(cells);
        return map;
    }

    internal static void Settle(ExploredMap map, FakeGrid grid, Vector3 player)
    {
        for (int call = 0; call < 20; call++) map.Observe(player, grid);
    }

    internal static void Walk(ExploredMap map, FakeGrid grid, Vector3 from, Vector3 to)
    {
        int steps = (int)MathF.Ceiling(Vector3.Distance(from, to));
        for (int step = 0; step <= steps; step++) Settle(map, grid, Vector3.Lerp(from, to, (float)step / steps));
    }

    internal static bool Walkable(IReadOnlyList<Vector3> path, FakeGrid grid)
    {
        for (int index = 1; index < path.Count; index++)
            if (!grid.Clear(path[index - 1], path[index])) return false;
        return path.Count >= 2;
    }
}
