using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class UnexploredTests
{
    private const float TestRadius = 8f;
    private const string Name = "unexplored-area-name";

    internal static void Run()
    {
        CorridorFrontier();
        PillarPocket();
        Doorway();
        Budget();
        Rooms();
        Glimpses();
        Keys();
    }

    private static void CorridorFrontier()
    {
        // A room (x 0-11) with a long corridor leaving east along z 4-5.
        var grid = new FakeGrid(Rows(60, 12, (x, z) => x < 12 || z is 4 or 5 ? '.' : ' '));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        Settle(map, grid, new Vector3(5.5f, 0, 5.5f));
        var areas = Frontier(map, grid);
        Check(areas.Count == 1, $"one unexplored area where the corridor leaves sight ({areas.Count})");
        var first = areas[0];
        Check(first.Anchor.X > 12 && first.Anchor.X < 18 && first.Anchor.Z >= 4 && first.Anchor.Z <= 6,
            $"the area is anchored at the corridor's unseen edge ({first.Anchor})");
        Check(!map.Explored(first.Anchor), "the anchor itself is unexplored ground");

        for (float x = 6; x <= 30; x += 1) Settle(map, grid, new Vector3(x + 0.5f, 0, 5f));
        areas = Frontier(map, grid);
        Check(areas.Count == 1 && areas[0].Anchor.X > 36, $"walking on moves the area ahead ({(areas.Count > 0 ? areas[0].Anchor : default)})");
        Check(areas[0].X != first.X, "the explored edge left its old area");

        var entries = new List<ExplorationEntry>();
        Check(UnexploredAreas.AddFrontier(map, areas, Name, entries) == 1, "the frontier becomes one scanner entry");
        var entry = entries[0];
        Check(entry.Category == ExplorationCategory.Unexplored && entry.Name == Name, "frontier entries are unexplored areas");
        Check(entry.Key == $"{UnexploredAreas.FrontierKeyPrefix}{areas[0].X},{areas[0].Z},{areas[0].Floor}", $"frontier key ({entry.Key})");
        Near(map.CellSize * ExploredMap.FrontierArrivalCells, entry.ArrivalRadius, 1e-6, "frontier arrival");
        Check(UnexploredAreas.AddFrontier(null, areas, Name, entries) == 0, "no map, no frontier");

        var route = MapTests.Router(map, grid);
        Check(route(new Vector3(30.5f, 0, 5f), entry.Position, entry.ArrivalRadius, out var path, out _) && path.Count >= 2,
            "the beacon can route to the edge through explored ground");
    }

    private static void PillarPocket()
    {
        // A closed room; a one-tile pillar hides a small pocket that walls close behind it.
        var grid = new FakeGrid(Rows(12, 12, (x, z) => x == 7 && z == 7 ? '#' : '.'));
        var map = new ExploredMap { ExploreRadius = 30 };
        Settle(map, grid, new Vector3(2.5f, 0, 2.5f));
        Check(!map.Explored(new Vector3(9f, 0, 9f)), "ground behind the pillar is unexplored");
        Check(Frontier(map, grid).Count == 0, "a pocket closed by walls and seen ground is not listed");
    }

    private static void Doorway()
    {
        // Room A (x 0-9), a wall at x 10 with a doorway at z 4-5, and a large room B behind it.
        var grid = new FakeGrid(Rows(30, 12, (x, z) => x != 10 || z is 4 or 5 ? '.' : '#'));
        var map = new ExploredMap { ExploreRadius = 30 };
        Settle(map, grid, new Vector3(3.5f, 0, 9.5f));
        var areas = Frontier(map, grid);
        Check(areas.Count >= 1, "the unseen room behind a doorway is listed");
        Check(areas.All(area => area.Anchor.X > 10), "room A, fully seen, has no unexplored edge");
    }

    private static void Budget()
    {
        var grid = new FakeGrid(Rows(60, 12, (x, z) => x < 12 || z is 4 or 5 ? '.' : ' '));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        Settle(map, grid, new Vector3(5.5f, 0, 5.5f));
        Check(map.FrontierPending == map.CellCount, "every new cell waits for its edge check");
        grid.ResetCounters();
        int queries = map.UpdateFrontier(grid, 5);
        Check(queries == grid.Samples + grid.Clears, "the reported cost is the native queries made");
        Check(queries > 0 && queries <= 5 + 8 * ExploredMap.FrontierRegionCells, $"one update stays near its budget ({queries})");
        Check(map.UpdateFrontier(null) == 0, "missing walkability costs nothing");
        int rounds = 0;
        while (map.FrontierPending > 0 && rounds++ < 1000) map.UpdateFrontier(grid);
        Check(map.FrontierPending == 0, $"the backlog drains ({rounds} updates)");
        grid.ResetCounters();
        map.UpdateFrontier(grid);
        Check(grid.Samples + grid.Clears == 0, "a drained frontier costs nothing");
        var imported = new ExploredMap { ExploreRadius = TestRadius };
        imported.Import(map.Points);
        Check(imported.FrontierPending == imported.CellCount, "saved cells are checked again after loading");
        Check(Frontier(imported, grid).Count == 1, "the saved map finds the same edge");
    }

    private static void Rooms()
    {
        var map = new ExploredMap { ExploreRadius = TestRadius };
        map.Import(Enumerable.Range(0, 11).Select(index => new Vector3(1 + index * 2, 0, 5)));
        var memory = new ExplorationSeenMemory();
        memory.Enter("Z62");
        var rooms = new List<MinimapRoom>
        {
            new(0, new Vector3(5, 0, 5), 6, false),
            new(1, new Vector3(15, 0, 5), 6, false),
            new(2, new Vector3(60, 0, 5), 6, false),
            new(3, new Vector3(25, 0, 5), 6, true),
            new(4, new Vector3(35, 0, 5), 0, false),
            new(5, new Vector3(float.NaN, 0, 5), 6, false),
            new(6, new Vector3(15, 20, 5), 6, false),
        };
        const string layout = "1.7";
        var entries = new List<ExplorationEntry>();
        int listed = UnexploredAreas.AddRooms(rooms, layout, new Vector3(5, 0, 5), memory, map, Name, entries, out int visited);
        Check(listed == 1 && entries.Count == 1, $"only the seen, unentered room is listed ({listed})");
        Check(visited == 2, $"the room stood in and the game-revealed room count as visited ({visited})");
        var entry = entries[0];
        Check(entry.Key == UnexploredAreas.RoomKeyPrefix + 1 && entry.Category == ExplorationCategory.Unexplored &&
            entry.Name == Name && entry.Position == rooms[1].Centre, $"room entry identity ({entry.Key})");
        Near(6, entry.ArrivalRadius, 1e-6, "a room is reached on entering it");
        Check(memory.Contains(UnexploredAreas.VisitKey(layout, 0)), "standing in a room remembers it");

        entries.Clear();
        UnexploredAreas.AddRooms(rooms, layout, new Vector3(40, 0, 5), memory, map, Name, entries, out visited);
        Check(entries.Count == 1 && visited == 2, "a visited room stays explored after leaving it");
        entries.Clear();
        UnexploredAreas.AddRooms(rooms, layout, new Vector3(16, 0, 6), memory, map, Name, entries, out visited);
        Check(entries.Count == 0 && visited == 3, "entering the listed room explores it");
        entries.Clear();
        UnexploredAreas.AddRooms(rooms, "2.7", new Vector3(40, 0, 5), memory, map, Name, entries, out visited);
        Check(entries.Count == 2 && visited == 1, "a changed volume layout does not reuse old visits");
        entries.Clear();
        Check(UnexploredAreas.AddRooms(rooms, layout, new Vector3(40, 0, 5), memory, null, Name, entries, out _) == 0,
            "no explored ground, no listed room");
        Check(UnexploredAreas.AddRooms(null, layout, Vector3.Zero, memory, map, Name, entries, out visited) == 0 && visited == 0,
            "no rooms read");
        Check(map.AnyExploredWithin(new Vector3(16, 0, 5), 1.5f) && !map.AnyExploredWithin(new Vector3(16, 0, 5), 0.5f),
            "explored ground inside a sphere is found by distance");
        Check(!map.AnyExploredWithin(new Vector3(15, 0, 5), float.NaN), "an invalid radius finds nothing");
    }

    private static void Glimpses()
    {
        var map = new ExploredMap { ExploreRadius = TestRadius };
        map.Import(new[] { new Vector3(100, 0, 5), new Vector3(5, 0, 5) });
        var rooms = new List<MinimapRoom> { new(0, new Vector3(100, 0, 5), 6, false), new(1, new Vector3(140, 0, 5), 6, false) };
        var glimpses = new RoomGlimpses();
        var entries = new List<ExplorationEntry>();
        var player = new Vector3(5, 0, 5);
        UnexploredAreas.AddRooms(rooms, "1.2", player, null, map, Name, entries, out _, glimpses);
        Check(entries.Count == 1 && entries[0].Key == UnexploredAreas.RoomKeyPrefix + 0,
            "the first pass over a map finds saved ground in far rooms");
        map.Import(new[] { new Vector3(140, 0, 5) });
        entries.Clear();
        UnexploredAreas.AddRooms(rooms, "1.2", player, null, map, Name, entries, out _, glimpses);
        Check(entries.Count == 1, "later passes search only rooms within reach of the player");
        entries.Clear();
        UnexploredAreas.AddRooms(rooms, "1.2", new Vector3(140, 0, 25), null, map, Name, entries, out _, glimpses);
        Check(entries.Count == 2, "a room gains its glimpse once the player comes within reach");
        var other = new ExploredMap { ExploreRadius = TestRadius };
        entries.Clear();
        UnexploredAreas.AddRooms(rooms, "1.2", player, null, other, Name, entries, out _, glimpses);
        Check(entries.Count == 0, "another map starts its glimpses over");
    }

    private static void Keys()
    {
        Check(UnexploredAreas.IsUnexplored(UnexploredAreas.RoomKeyPrefix + 3), "room keys are unexplored areas");
        Check(UnexploredAreas.IsUnexplored(UnexploredAreas.FrontierKeyPrefix + "1,2,0"), "frontier keys are unexplored areas");
        Check(!UnexploredAreas.IsUnexplored("listener:5") && !UnexploredAreas.IsUnexplored(null), "places are not");
    }

    private static List<FrontierArea> Frontier(ExploredMap map, FakeGrid grid)
    {
        int rounds = 0;
        while (map.FrontierPending > 0 && rounds++ < 10_000) map.UpdateFrontier(grid);
        var areas = new List<FrontierArea>();
        map.CollectFrontier(areas);
        return areas;
    }

    private static void Settle(ExploredMap map, FakeGrid grid, Vector3 player)
    {
        // Enough calls to drain the planned cells within the largest test radius.
        for (int call = 0; call < 200; call++) map.Observe(player, grid);
    }

    private static string[] Rows(int width, int depth, Func<int, int, char> tile) =>
        Enumerable.Range(0, depth).Select(z => new string(Enumerable.Range(0, width).Select(x => tile(x, z)).ToArray())).ToArray();
}
