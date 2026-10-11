using System.Numerics;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Navigation.Guidance;
using static MapTests;
using static Program;

// Route-aware scanner readouts and reachable goals (reference/exploration-mode.md, 2026-10-11 tester report).
internal static class RouteTests
{
    private const float TestRadius = 8f;
    private const float Step = 1f;
    private static readonly Vector3 Forward = new(0, 0, 1), Right = new(1, 0, 0);

    // The test's own templates: expected speech is composed from them, never from the shipped catalogs.
    private static readonly Dictionary<string, string> Words = new()
    {
        ["exploration.entry"] = "{0}; {1}; {2}", ["exploration.entry_here"] = "{0} <here>",
        ["exploration.steps_one"] = "<1 step>", ["exploration.steps_many"] = "<{0} steps>",
        ["exploration.route_steps_one"] = "<1 step by route>", ["exploration.route_steps_many"] = "<{0} steps by route>",
        ["exploration.route_heading"] = "<head {0}>", ["exploration.entry_unrouted"] = "{0} <no known route>",
        ["exploration.route_leg"] = "[{0} {1}]", ["exploration.route_leg_then"] = "{0} <then> {1}",
        ["exploration.route_leg_up"] = "{0} <climbing>", ["exploration.route_leg_down"] = "{0} <descending>",
        ["exploration.route_legs_more_one"] = "{0} <+1 leg>", ["exploration.route_legs_more_many"] = "{0} <+{1} legs>",
        ["exploration.category.exit"] = "<exits>",
        ["guidance.direction.up"] = "<up>", ["guidance.direction.up_right"] = "<up-right>",
        ["guidance.direction.right"] = "<right>", ["guidance.direction.down_right"] = "<down-right>",
        ["guidance.direction.down"] = "<down>", ["guidance.direction.down_left"] = "<down-left>",
        ["guidance.direction.left"] = "<left>", ["guidance.direction.up_left"] = "<up-left>",
    };
    private static readonly ExplorationText Text = (key, arguments) => string.Format(Words[key], arguments);

    internal static void Run()
    {
        Detour();
        Storerooms();
        Wall();
        Areas();
    }

    // Burning Forest, Grael: the exit is straight to the right, but the walkable way runs down, right and up.
    private static void Detour()
    {
        static char Tile(int x, int z) => x >= 3 && x <= 6 && z >= 2 && z <= 25 || x >= 3 && x <= 36 && z >= 2 && z <= 5 ||
            x >= 33 && x <= 36 && z >= 2 && z <= 25 ? '.' : '#';
        var grid = new FakeGrid(Rows(40, 30, Tile));
        var player = new Vector3(5, 0, 23);
        var exit = new ExplorationEntry("exit", "Exit", ExplorationCategory.Exit, new Vector3(35, 0, 23), 1.5f);
        var catalog = new ExplorationCatalog();
        var entries = new List<ExplorationEntry> { exit };
        string straight = Text("exploration.entry", "Exit", Text("exploration.steps_many", 30), Text("guidance.direction.right"));

        // Only the player's corridor is explored: the straight line is spoken and marked as not a route.
        var corridor = new ExploredMap { ExploreRadius = TestRadius };
        Walk(corridor, grid, player, new Vector3(5, 0, 4));
        catalog.Route = Scanner(corridor, grid);
        Check(catalog.StepCategory(1, entries, player, Forward, Right, Step, Text) ==
            Text("exploration.category.exit") + ". " + Text("exploration.entry_unrouted", straight), "no explored route marks the straight line");

        // The whole U explored: the route's length and the way to head now, toward the beacon's look-ahead.
        var map = new ExploredMap { ExploreRadius = TestRadius };
        Walk(map, grid, player, new Vector3(5, 0, 4));
        Walk(map, grid, new Vector3(5, 0, 4), new Vector3(35, 0, 4));
        Walk(map, grid, new Vector3(35, 0, 4), exit.Position);
        var route = BeaconState.Measure(Router(map, grid), player, ExplorationTarget.From(exit));
        Check(route is { Direct: false } && route.Value.Length > 60, $"the route walks the U ({route?.Length})");
        Check(GuidanceGeometry.Direction(route.Value.Aim - player, Forward, Right) == "guidance.direction.down",
            "the look-ahead point heads down first");
        int steps = (int)Math.Round(route.Value.Length / Step, MidpointRounding.AwayFromZero);
        catalog.Route = Scanner(map, grid);
        string routed = Text("exploration.entry", "Exit", Text("exploration.route_steps_many", steps),
            Text("exploration.route_heading", Text("guidance.direction.down")));
        Check(catalog.StepEntry(1, entries, player, Forward, Right, Step, Text) == routed, "the entry step speaks the route");
        Check(catalog.DescribeEntry(exit, player, Forward, Right, Step, Text) == routed, "one function describes an entry");

        // Legs, when read: the U is down, right and up, its corners walkable and as long as the route.
        var corners = map.Corners(BeaconPath(map, grid, player, exit), grid);
        var legs = RouteLegs.From(corners, Forward, Right);
        string spokenLegs = string.Join(",", legs.Select(leg => leg.Direction[(leg.Direction.LastIndexOf('.') + 1)..]));
        Check(spokenLegs == "down,right,up", $"the U reads down, right, up ({spokenLegs})");
        Check(Walkable(corners, grid) && MathF.Abs(legs.Sum(leg => leg.Length) - route.Value.Length) < 2 * Step,
            $"the corners walk the route ({legs.Sum(leg => leg.Length)} of {route.Value.Length})");
        catalog.Route = Scanner(map, grid, true);
        catalog.LegsSpoken = 3;
        Check(catalog.DescribeEntry(exit, player, Forward, Right, Step, Text).Contains("<down>") &&
            catalog.DescribeEntry(exit, player, Forward, Right, Step, Text).EndsWith("<up> " + Text("exploration.steps_many",
                (int)Math.Round(legs[^1].Length / Step, MidpointRounding.AwayFromZero)) + "]"),
            $"the scanner reads the legs ({catalog.DescribeEntry(exit, player, Forward, Right, Step, Text)})");
        catalog.LegsSpoken = 0;
        catalog.Route = Scanner(map, grid);

        // The beacon pans to the same point: straight down the screen.
        var state = new BeaconState();
        state.Lock(ExplorationTarget.From(exit), player, 0, Router(map, grid));
        var ping = state.Tick(player, ExplorationTarget.From(exit), Forward, Right, BeaconState.LockDelaySeconds, Router(map, grid));
        Check(ping.Ping && ping.Ahead < -0.9f, $"the beacon pans down the corridor ({ping.Pan}, {ping.Ahead})");

        // A straight walkable route keeps the plain readout.
        var near = exit with { Key = "near", Position = new Vector3(5, 0, 10) };
        Check(catalog.DescribeEntry(near, player, Forward, Right, Step, Text) ==
            Text("exploration.entry", "Exit", Text("exploration.steps_many", 13), Text("guidance.direction.down")),
            "a direct route reads as before");
    }

    // The Fortress Walls, the Storerooms (scene Z52 survey): the entrance's listener sits 3 below a pocket floor and
    // under a plateau 5 higher; the plateau is nearer on the map, the pocket in 3D. Only the pocket reaches it.
    private static void Storerooms()
    {
        static char Ground(int x, int z) => z <= 11 ? '.' : '#';
        static char Upper(int x, int z) => x <= 1 && z is >= 12 and <= 20 ? (char)('0' + (z - 11)) :
            z >= 21 || z is >= 13 and <= 20 && x >= 4 ? '=' : ' ';
        var grid = new FakeGrid(Rows(30, 30, Ground), Rows(30, 30, Upper));
        var listener = new Vector3(20.5f, -3, 12.6f);
        const float range = 9;
        var plateau = new Vector3(20.5f, FakeGrid.UpperHeight, 16);
        var pocket = new Vector3(20.5f, 0, 9);

        var map = new ExploredMap { ExploreRadius = TestRadius };
        var goal = map.ResolveGoal(listener, range, false, grid);
        Check(goal.Kind == RouteGoalKind.Approach && MathF.Abs(goal.Point.Y) < 0.01f,
            $"the approach is the pocket below, not the plateau ({goal.Kind}, {goal.Point})");

        // Seen only from the plateau: no plateau cell counts as reaching the entrance.
        Settle(map, grid, plateau);
        Check(!map.TryRoute(plateau, listener, range, grid, out _, out _), "the plateau above never reaches the entrance");
        Check(!map.AtHand(plateau, listener, range, false, grid, out string blocked) && blocked == "other level",
            $"in range on the plateau is not at hand ({blocked})");

        // Explored down the ramp into the pocket: the route ends in the pocket and every leg is walkable.
        Walk(map, grid, plateau, new Vector3(5, FakeGrid.UpperHeight, 22));
        Walk(map, grid, new Vector3(5, FakeGrid.UpperHeight, 22), new Vector3(1, FakeGrid.UpperHeight, 22));
        Walk(map, grid, new Vector3(1, FakeGrid.UpperHeight, 22), new Vector3(1, 4.5f, 20.5f));
        Walk(map, grid, new Vector3(1, 4.5f, 20.5f), new Vector3(1, 0.5f, 12.5f));
        Walk(map, grid, new Vector3(1, 0.5f, 12.5f), new Vector3(1, 0, 10));
        Walk(map, grid, new Vector3(1, 0, 10), pocket);
        Check(map.TryRoute(plateau, listener, range, grid, out var path, out float length), $"routed by the ramp ({map.LastFailure})");
        Check(MathF.Abs(path[^1].Y) < 0.01f && Walkable(path, grid) && length > 30,
            $"the route goes round to the pocket ({path[^1]}, {length})");
        Check(map.AtHand(pocket, listener, range, false, grid, out _), "in the pocket the entrance is at hand");
        Check(map.AtHand(goal.Point, listener, range, false, grid, out _), "the route's end is arrival");
        // Read as legs, the ramp is a descent and the last leg runs along the pocket floor to the way in.
        var legs = RouteLegs.From(map.Corners(path, grid), Forward, Right);
        Check(legs.Any(leg => leg.Rise <= -FakeGrid.UpperHeight + FakeGrid.RampStep) && MathF.Abs(legs[^1].Rise) < FakeGrid.Climb,
            $"the legs descend the ramp and end on the pocket floor ({string.Join("; ", legs)})");

        // The beacon holds arrival on the plateau and arrives in the pocket.
        var target = new ExplorationTarget("listener:1", "Storerooms", listener, range);
        var state = new BeaconState { AtHand = (player, locked) => map.AtHand(player, locked.Position,
            BeaconState.ArrivalRadius(locked), locked.Area, grid, out _) };
        Check(state.Lock(target, plateau, 0, Router(map, grid)) == BeaconLock.Locked, "locked from the plateau");
        Check(state.Tick(plateau, target, Forward, Right, 0.1, Router(map, grid)).Release == BeaconRelease.None,
            "no arrival above the entrance");
        Check(state.Tick(pocket, target, Forward, Right, 0.2, Router(map, grid)).Release == BeaconRelease.Arrived,
            "arrival in the pocket");
    }

    // A target on the same floor behind a wall, within its range: the route goes round, arrival waits.
    private static void Wall()
    {
        var grid = new FakeGrid(Rows(20, 20, (x, z) => z == 10 && x <= 15 ? '#' : '.'));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        for (int z = 1; z < 20; z += 3)
            for (int x = 1; x < 20; x += 3) Settle(map, grid, new Vector3(x + 0.5f, 0, z + 0.5f));
        var npc = new Vector3(5.5f, 0, 11.5f);
        var south = new Vector3(5.5f, 0, 8.5f);
        const float range = 4;
        Check(!map.AtHand(south, npc, range, false, grid, out string blocked) && blocked == "wall", $"behind the wall ({blocked})");
        Check(map.TryRoute(south, npc, range, grid, out var path, out float length), $"routed round the wall ({map.LastFailure})");
        Check(Walkable(path, grid) && path[^2].Z >= 10 && length > 15, $"the goal cell is on the target's side ({path[^2]}, {length})");
        Check(map.LastGoal.Kind == RouteGoalKind.OnMesh, "a target on the mesh is its own goal");
        Check(map.AtHand(new Vector3(5.5f, 0, 13), npc, range, false, grid, out _), "on its side it is at hand");
    }

    // Minimap rooms and frontier edges are entered: their radius alone decides.
    private static void Areas()
    {
        Check(ExplorationTarget.From(new ExplorationEntry("room:1", "Room", ExplorationCategory.Room, Vector3.Zero, 9)).Area &&
            ExplorationTarget.From(new ExplorationEntry("unexplored:1", "Unexplored", ExplorationCategory.UnexploredRoom, Vector3.Zero, 9)).Area,
            "rooms and unexplored rooms are areas");
        Check(!ExplorationTarget.From(new ExplorationEntry("listener:1", "Exit", ExplorationCategory.Exit, Vector3.Zero, 9)).Area,
            "an exit is a point");
        var grid = new FakeGrid(Rows(20, 20, (x, z) => z == 10 && x <= 15 ? '#' : '.'));
        var map = new ExploredMap();
        Check(map.AtHand(new Vector3(5.5f, 0, 8.5f), new Vector3(5.5f, 0, 11.5f), 4, true, grid, out _),
            "an area is entered across its radius");
    }

    private static Func<ExplorationEntry, Vector3, ExplorationRoute?> Scanner(ExploredMap map, IWalkability nav, bool corners = false) =>
        (entry, player) => BeaconState.Measure(Router(map, nav), player, ExplorationTarget.From(entry),
            corners ? path => map.Corners(path, nav) : null);

    private static IReadOnlyList<Vector3> BeaconPath(ExploredMap map, IWalkability nav, Vector3 player, ExplorationEntry entry)
    {
        Check(Router(map, nav)(player, ExplorationTarget.From(entry), out var path, out _), "routed");
        return path;
    }
}
