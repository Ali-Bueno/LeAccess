using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class UnexploredTests
{
    private const float TestRadius = 8f;

    // The test's own names, built from the room number like the catalog's templates.
    private static string RoomName(int number) => $"room-{number}";
    private static string UnexploredName(int number) => $"unexplored-{number}";
    private static string AreaName(int number) => $"area-{number}";

    internal static void Run()
    {
        CorridorFrontier();
        GroundNoRoomCovers();
        PillarPocket();
        Doorway();
        Budget();
        Rooms();
        Entering();
        Glimpses();
        Nearest();
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
        var tracker = new RoomTracker();
        Check(tracker.AddFrontier(map, areas, UnexploredName, entries) == 1, "the frontier becomes one scanner entry");
        var entry = entries[0];
        Check(entry.Category == ExplorationCategory.UnexploredRoom && entry.Name == UnexploredName(1),
            $"frontier entries are numbered unexplored rooms ({entry.Name})");
        Check(entry.Key == $"{RoomTracker.FrontierKeyPrefix}{areas[0].X},{areas[0].Z},{areas[0].Floor}", $"frontier key ({entry.Key})");
        Near(map.CellSize * ExploredMap.FrontierArrivalCells, entry.ArrivalRadius, 1e-6, "frontier arrival");
        Check(tracker.AddFrontier(null, areas, UnexploredName, entries) == 0, "no map, no frontier");
        entries.Clear();
        tracker.AddFrontier(map, new List<FrontierArea>(), UnexploredName, entries);
        Check(entries.Count == 1 && entries[0].Name == UnexploredName(1), "an area keeps its number on the same map");

        var route = MapTests.Router(map, grid);
        Check(route(new Vector3(30.5f, 0, 5f), ExplorationTarget.From(entry), out var path, out _) && path.Count >= 2,
            "the beacon can route to the edge through explored ground");
    }

    private static void GroundNoRoomCovers()
    {
        // The corridor leaves the room the player stands in; which room covers its unseen edge decides what is listed.
        var grid = new FakeGrid(Rows(60, 12, (x, z) => x < 12 || z is 4 or 5 ? '.' : ' '));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        var player = new Vector3(5.5f, 0, 5.5f);
        Settle(map, grid, player);
        var edge = Frontier(map, grid).Single().Anchor;
        float cell = map.CellSize;
        var memory = new ExplorationSeenMemory();
        memory.Enter("Z12");
        var standing = new MinimapRoom(0, new Vector3(5, 0, 5), 6, false);
        List<ExplorationEntry> Listed(params MinimapRoom[] rooms)
        {
            var tracker = new RoomTracker();
            var entries = new List<ExplorationEntry>();
            tracker.AddRooms(rooms, "1.2", player, memory, map, RoomName, UnexploredName, entries, null, out _);
            tracker.AddFrontier(map, new List<FrontierArea>(), AreaName, entries, rooms);
            return entries.Where(e => e.Category == ExplorationCategory.UnexploredRoom).ToList();
        }
        string Names(List<ExplorationEntry> entries) => string.Join(", ", entries.Select(e => e.Name));

        var alone = Listed(standing);
        Check(alone.Count == 1 && alone[0].Key.StartsWith(RoomTracker.FrontierKeyPrefix, StringComparison.Ordinal) &&
            alone[0].Name == AreaName(1), $"ground past every room is an unexplored area ({Names(alone)})");
        var glimpsed = Listed(standing, new MinimapRoom(1, edge, 1.5f * cell, false));
        Check(glimpsed.Count == 1 && glimpsed[0].Key == RoomTracker.UnexploredKeyPrefix + 1,
            $"a listed room takes the edge into it ({Names(glimpsed)})");
        Check(Listed(standing, new MinimapRoom(1, edge, 1.5f * cell, true)).Count == 0,
            "a revealed room is explored ground, not an edge");
        var unseen = new MinimapRoom(1, edge + new Vector3(cell, 0, 0), 1.2f * cell, false);
        Check(!map.AnyExploredWithin(unseen.Centre, unseen.Radius), "precondition: the far room holds no explored ground");
        var beyond = Listed(standing, unseen);
        Check(beyond.Count == 1 && beyond[0].Name == AreaName(1), $"an unseen room leaves its edge listed ({Names(beyond)})");
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
        var tracker = new RoomTracker();
        var entries = new List<ExplorationEntry>();
        int Add(Vector3 player, string version, RoomTracker using_, out int seen)
        {
            entries.Clear();
            return using_.AddRooms(rooms, version, player, memory, map, RoomName, UnexploredName, entries, null, out seen);
        }
        int listed = Add(new Vector3(5, 0, 5), layout, tracker, out int visited);
        var unexplored = entries.Where(e => e.Category == ExplorationCategory.UnexploredRoom).ToList();
        var known = entries.Where(e => e.Category == ExplorationCategory.Room).ToList();
        Check(listed == 1 && unexplored.Count == 1, $"only the seen, unentered room is unexplored ({listed})");
        Check(visited == 2 && known.Count == 2, $"the room stood in and the game-revealed room are rooms ({visited})");
        var entry = unexplored[0];
        Check(entry.Key == RoomTracker.UnexploredKeyPrefix + 1 && entry.Name == UnexploredName(2) &&
            entry.Position == rooms[1].Centre, $"unexplored room identity and number ({entry.Key}, {entry.Name})");
        Near(6, entry.ArrivalRadius, 1e-6, "a room is reached on entering it");
        Check(known.Any(e => e.Key == RoomTracker.RoomKeyPrefix + 0 && e.Name == RoomName(1)) &&
            known.Any(e => e.Key == RoomTracker.RoomKeyPrefix + 3 && e.Name == RoomName(4)), "visited rooms are numbered from one");
        Check(memory.Contains(RoomTracker.VisitKey(layout, 0)), "standing in a room remembers it");

        Add(new Vector3(40, 0, 5), layout, tracker, out visited);
        Check(entries.Count(e => e.Category == ExplorationCategory.UnexploredRoom) == 1 && visited == 2,
            "a visited room stays explored after leaving it");
        Add(new Vector3(16, 0, 6), layout, tracker, out visited);
        Check(entries.All(e => e.Category == ExplorationCategory.Room) && visited == 3, "entering the listed room explores it");
        Add(new Vector3(40, 0, 5), "2.7", tracker, out visited);
        Check(entries.Count(e => e.Category == ExplorationCategory.UnexploredRoom) == 2 && visited == 1,
            "a changed volume layout does not reuse old visits");
        entries.Clear();
        Check(new RoomTracker().AddRooms(rooms, layout, new Vector3(40, 0, 5), memory, null, RoomName, UnexploredName,
            entries, null, out _) == 0, "no explored ground, no unexplored room");
        Check(tracker.AddRooms(null, layout, Vector3.Zero, memory, map, RoomName, UnexploredName, entries, null, out visited) == 0 &&
            visited == 0, "no rooms read");
        Check(map.AnyExploredWithin(new Vector3(16, 0, 5), 1.5f) && !map.AnyExploredWithin(new Vector3(16, 0, 5), 0.5f),
            "explored ground inside a sphere is found by distance");
        Check(!map.AnyExploredWithin(new Vector3(15, 0, 5), float.NaN), "an invalid radius finds nothing");
    }

    private static void Entering()
    {
        var map = new ExploredMap { ExploreRadius = TestRadius };
        map.Import(new[] { new Vector3(15, 0, 5) });
        var memory = new ExplorationSeenMemory();
        memory.Enter("Z62");
        var rooms = new List<MinimapRoom> { new(0, new Vector3(5, 0, 5), 6, false), new(1, new Vector3(15, 0, 5), 6, false) };
        var tracker = new RoomTracker();
        var entries = new List<ExplorationEntry>();
        var entered = new List<MinimapRoom>();
        void Pass(IReadOnlyList<MinimapRoom> list, string layout, Vector3 player)
        {
            entries.Clear();
            tracker.AddRooms(list, layout, player, memory, map, RoomName, UnexploredName, entries, entered, out _);
        }
        Pass(rooms, "1.2", new Vector3(5, 0, 5));
        Check(entered.Count == 0, "the room the player starts in is the silent baseline");
        Pass(rooms, "1.2", new Vector3(16, 0, 5));
        Check(entered.Count == 1 && entered[0].Index == 1, "stepping into an unvisited room is an entry");
        entered.Clear();
        Pass(rooms, "1.2", new Vector3(5, 0, 5));
        Pass(rooms, "1.2", new Vector3(16, 0, 5));
        Check(entered.Count == 0, "returning to a visited room is silent");
        var revealed = new List<MinimapRoom> { rooms[0], rooms[1], new(2, new Vector3(25, 0, 5), 6, false) };
        Pass(revealed, "1.3", new Vector3(16, 0, 5));
        revealed[2] = revealed[2] with { Revealed = true };
        Pass(revealed, "1.3", new Vector3(26, 0, 5));
        Check(entered.Count == 1 && entered[0].Index == 2, "the game revealing the room as it is entered still counts");
    }

    private static void Glimpses()
    {
        var map = new ExploredMap { ExploreRadius = TestRadius };
        map.Import(new[] { new Vector3(100, 0, 5), new Vector3(5, 0, 5) });
        var rooms = new List<MinimapRoom> { new(0, new Vector3(100, 0, 5), 6, false), new(1, new Vector3(140, 0, 5), 6, false) };
        var tracker = new RoomTracker();
        var entries = new List<ExplorationEntry>();
        var player = new Vector3(5, 0, 5);
        int Unexplored(Vector3 at, ExploredMap ground)
        {
            entries.Clear();
            return tracker.AddRooms(rooms, "1.2", at, null, ground, RoomName, UnexploredName, entries, null, out _);
        }
        Check(Unexplored(player, map) == 1 && entries.Any(e => e.Key == RoomTracker.UnexploredKeyPrefix + 0),
            "the first pass over a map finds saved ground in far rooms");
        map.Import(new[] { new Vector3(140, 0, 5) });
        Check(Unexplored(player, map) == 1, "later passes search only rooms within reach of the player");
        Check(Unexplored(new Vector3(140, 0, 25), map) == 2, "a room gains its glimpse once the player comes within reach");
        Check(Unexplored(player, new ExploredMap { ExploreRadius = TestRadius }) == 0, "another map starts its glimpses over");
    }

    private static void Nearest()
    {
        var forward = new Vector3(0, 0, 1);
        var right = new Vector3(1, 0, 0);
        var rooms = new List<MinimapRoom>
        {
            new(0, new Vector3(0, 0, 0), 5, false),
            new(1, new Vector3(20, 0, 0), 5, false),
            new(2, new Vector3(0, 0, -30), 5, false),
        };
        Check(RoomTracker.TryNearest(rooms, new Vector3(0, 0, 3), forward, right, out float distance, out float pan, out float front) &&
            Math.Abs(distance - (MathF.Sqrt(409) - 5)) < 1e-3, $"the room the player stands in never sounds ({distance})");
        Check(RoomTracker.TryNearest(rooms, new Vector3(9, 0, 0), forward, right, out distance, out pan, out front) &&
            Math.Abs(distance - 4) < 1e-3 && pan < -0.99f, $"distance and pan go to the nearest point ({distance}, {pan})");
        Check(RoomTracker.TryNearest(rooms, new Vector3(0, 0, -20), forward, right, out distance, out pan, out front) &&
            front < 0 && Math.Abs(pan) < 1e-3, $"a room behind has negative front ({front})");
        Check(!RoomTracker.TryNearest(new List<MinimapRoom>(), Vector3.Zero, forward, right, out _, out _, out _), "no rooms, no loop");
        Check(!RoomTracker.TryNearest(rooms, new Vector3(float.NaN, 0, 0), forward, right, out _, out _, out _), "no player, no loop");
    }

    private static void Keys()
    {
        Check(RoomTracker.IsUnexplored(RoomTracker.UnexploredKeyPrefix + 3), "unexplored room keys are unexplored");
        Check(RoomTracker.IsUnexplored(RoomTracker.FrontierKeyPrefix + "1,2,0"), "frontier keys are unexplored");
        Check(!RoomTracker.IsUnexplored(RoomTracker.RoomKeyPrefix + 3), "a visited room is not");
        Check(!RoomTracker.IsUnexplored("listener:5") && !RoomTracker.IsUnexplored(null), "places are not");
        Check(ExplorationSeenMemory.Persistent("1|Salida hacia X|3|4") && ExplorationSeenMemory.Persistent("2|Punto|0|0") &&
            ExplorationSeenMemory.Persistent("4|Alijo|0|0") && ExplorationSeenMemory.Persistent("3|Maeve") &&
            ExplorationSeenMemory.Persistent("room|1.61|4") && ExplorationSeenMemory.Persistent("death|1.0|2.0|3.0"),
            "exits, waypoints, the stash, NPCs, rooms and the death place are saved");
        Check(!ExplorationSeenMemory.Persistent("7|Portal|0|0") && !ExplorationSeenMemory.Persistent("11|Cofre|0|0") &&
            !ExplorationSeenMemory.Persistent("12|Santuario|0|0") && !ExplorationSeenMemory.Persistent("13|Nota|0|0") &&
            !ExplorationSeenMemory.Persistent("x") && !ExplorationSeenMemory.Persistent(null),
            "portals, chests, shrines and other objects are not saved (D2R)");
        var memory = new ExplorationSeenMemory();
        memory.Enter("Z62");
        memory.Add("death|1.0|0.0|2.0");
        memory.Add("room|1.61|3");
        int version = memory.Version;
        Check(memory.RemovePrefix("death|") == 1 && memory.Version == version + 1 && !memory.WithPrefix("death|").Any() &&
            memory.Contains("room|1.61|3"), "a new death replaces only the old death place");
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
