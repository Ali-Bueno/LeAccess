using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static MapTests;
using static Program;

// Ways toward a target beyond explored ground (reference/exploration-mode.md "Ways toward a target beyond explored ground").
internal static class WayTests
{
    private const float TestRadius = 8f;
    private const float OpeningRadius = 4f;
    private static readonly Vector3 Forward = new(0, 0, 1), Right = new(1, 0, 0);

    internal static void Run()
    {
        Fork();
        Behind();
        Seam();
        Guided();
        Unguided();
        Failures();
        Keeping();
        DeadEnd();
        GuidedLead();
        Continuation();
        RouteKnown();
        NoWaysLeft();
    }

    // A corridor meets a crossing hall; the target lies far up a passage off its east end, so the east opening is
    // the better way although the west one is about as near.
    private static void Fork()
    {
        static char Tile(int x, int z) => x >= 28 && x <= 31 && z <= 21 || z >= 18 && z <= 21 || x >= 55 && x <= 58 && z >= 18 ? '.' : '#';
        var grid = new FakeGrid(Rows(60, 60, Tile));
        var map = new ExploredMap { ExploreRadius = TestRadius };
        Walk(map, grid, new Vector3(29.5f, 0, 1.5f), new Vector3(29.5f, 0, 19.5f));
        var openings = new List<ExploredOpening>
        {
            new(new Vector3(20, 0, 19.5f), OpeningRadius), new(new Vector3(40, 0, 19.5f), OpeningRadius),
            new(new Vector3(5, 0, 50), OpeningRadius)
        };
        var target = new Vector3(56.5f, 0, 56.5f);
        var player = new Vector3(29.5f, 0, 10);
        var ways = new List<ExploredWay>();

        Check(map.FindWays(player, target, openings, BeaconState.MaxWays, grid, ways) == 2, "both reached openings are ways, the island is not");
        Check(ways[0].Opening == 1 && ways[1].Opening == 0, "the east opening promises less than the west one");
        Check(ways[0].Estimate <= ways[1].Estimate, "ways come best first");
        Near(ExploredMap.Horizontal(openings[1].Position, target), ways[0].Beyond, 1.5, "the way on is the straight line from the opening");
        Check(Walkable(ways[0].Path, grid) && Walkable(ways[1].Path, grid), "every way walks explored ground to its opening");
        Check(ExploredMap.Horizontal(ways[0].Path[^1], openings[1].Position) <= OpeningRadius, "a way ends in its opening");

        Check(map.FindWays(player, target, openings, 1, grid, ways) == 1 && ways[0].Opening == 1, "a limit keeps the best");
        Check(map.FindWays(player, target, new List<ExploredOpening> { openings[2] }, BeaconState.MaxWays, grid, ways) == 0 &&
            map.LastFailure == "no opening reached", "an opening off the explored ground is no way");
    }

    // The tester's report: the way led away from the quest. An opening just behind the player promises less than one
    // near the target at the end of a winding explored loop, also once the player stands in it.
    private static void Behind()
    {
        static char Tile(int x, int z) => x >= 28 && x <= 31 && z <= 21 || z >= 18 && z <= 21 && x >= 28 && x <= 51 ||
            x >= 48 && x <= 51 && z >= 18 && z <= 41 || z >= 38 && z <= 41 && x >= 28 && x <= 51 ? '.' : '#';
        var grid = new FakeGrid(Rows(60, 60, Tile));
        var map = Imported(60, 60, Tile);
        var openings = new List<ExploredOpening> { new(new Vector3(30, 0, 1), OpeningRadius), new(new Vector3(30, 0, 40), OpeningRadius) };
        var target = new Vector3(30, 0, 58);
        var ways = new List<ExploredWay>();

        Check(map.FindWays(new Vector3(29.5f, 0, 10), target, openings, BeaconState.MaxWays, grid, ways) == 2 && ways[0].Opening == 1,
            "the opening near the target leads, not the one behind");
        Check(ways[0].Length > ways[1].Length && ways[0].Beyond < ways[1].Beyond, "although its walk is the longer one");
        Check(map.FindWays(new Vector3(29.5f, 0, 2), target, openings, BeaconState.MaxWays, grid, ways) == 2 && ways[0].Opening == 1,
            "standing in the opening behind, the way ahead still leads");
    }

    // An opening beyond a seam the grid cannot link is reached by the engine's path over explored ground.
    private static void Seam()
    {
        static char Tile(int x, int z) => x is 15 or 16 ? 'L' : '.';
        var grid = new FakeGrid(Rows(40, 10, Tile));
        var map = Imported(40, 10, Tile, (x, _) => x < 34);
        var openings = new List<ExploredOpening> { new(new Vector3(37, 0, 5), OpeningRadius) };
        var ways = new List<ExploredWay>();
        Check(map.FindWays(new Vector3(5.5f, 0, 5.5f), new Vector3(60, 0, 5), openings, BeaconState.MaxWays, grid, ways) == 1 &&
            map.MeshRoutes == 1, $"the opening beyond the seam is a way ({map.LastFailure})");
        Check(ExploredMap.Horizontal(ways[0].Path[^1], openings[0].Position) <= OpeningRadius + 0.01f, "it ends where it enters the opening");
        Near(23, ways[0].Beyond, 1, "the way on from the opening");
    }

    // The Risen Lake log and the user's decision: the opening nearer the target is a dead-end pocket; the engine's path
    // to the target leaves explored ground by the far one, so that one leads. An unlisted leaving point guides nothing.
    private static void Guided()
    {
        static char Tile(int x, int z) => z >= 10 && z <= 13 && x >= 5 && x <= 54 || x >= 20 && x <= 25 && z >= 14 && z <= 25 ||
            x >= 50 && x <= 54 && z >= 14 && z <= 50 || z >= 46 && z <= 50 && x >= 20 && x <= 54 ? '.' : '#';
        var grid = new FakeGrid(Rows(60, 60, Tile));
        var map = Imported(60, 60, Tile, (_, z) => z <= 13);
        var openings = new List<ExploredOpening> { new(new Vector3(22.5f, 0, 16), OpeningRadius), new(new Vector3(52, 0, 16), OpeningRadius) };
        var player = new Vector3(10.5f, 0, 11.5f);
        var target = new Vector3(22.5f, 0, 48.5f);
        var ways = new List<ExploredWay>();

        Check(map.FindWays(player, target, openings, BeaconState.MaxWays, grid, ways) == 2 && ways[0].Opening == 1 &&
            ways[0].Guided && !ways[1].Guided && map.LastGuide == "1", $"the engine's path picks the far opening ({map.LastGuide})");
        Check(ways[0].Estimate > ways[1].Estimate, "although the pocket promises less");
        Check(map.FindWays(player, target, new List<ExploredOpening> { openings[0] }, BeaconState.MaxWays, grid, ways) == 1 &&
            !ways[0].Guided && map.LastGuide == "unlisted", "leaving where no opening is listed guides nothing");
    }

    // The second Risen Lake log said only "guide=none": a marker far off the walkable ground and one the engine's path
    // cannot reach are told apart.
    private static void Unguided()
    {
        static char Tile(int x, int z) => z >= 10 && z <= 13 && x >= 5 && x <= 30 || x >= 40 && x <= 50 && z >= 40 && z <= 50 ? '.' : '#';
        var grid = new FakeGrid(Rows(60, 100, Tile));
        var map = Imported(60, 100, Tile, (_, z) => z <= 13);
        var openings = new List<ExploredOpening> { new(new Vector3(29.5f, 0, 11.5f), OpeningRadius) };
        var player = new Vector3(10.5f, 0, 11.5f);
        var ways = new List<ExploredWay>();

        map.FindWays(player, new Vector3(45.5f, 0, 45.5f), openings, BeaconState.MaxWays, grid, ways);
        Check(map.LastGuide == "pathless", $"an island the engine's path cannot reach guides nothing ({map.LastGuide})");
        map.FindWays(player, new Vector3(45.5f, 0, 50 + map.ExploreRadius + 10), openings, BeaconState.MaxWays, grid, ways);
        Check(map.LastGuide == "far", $"nor does a marker beyond the explore radius from walkable ground ({map.LastGuide})");
    }

    private static void Failures()
    {
        var grid = new FakeGrid(Rows(10, 10, (_, _) => '.'));
        var map = new ExploredMap();
        var ways = new List<ExploredWay>();
        var openings = new List<ExploredOpening> { new(new Vector3(5, 0, 5), OpeningRadius) };
        Check(map.FindWays(new Vector3(50, 0, 50), Vector3.Zero, openings, 3, grid, ways) == 0 && map.LastFailure == "player off NavMesh",
            "off the mesh");
        Check(map.FindWays(new Vector3(1.5f, 0, 1.5f), Vector3.Zero, openings, 3, grid, ways) == 0 && map.LastFailure == "player cell unexplored",
            "nothing explored");
        Check(map.FindWays(new Vector3(1.5f, 0, 1.5f), Vector3.Zero, openings, 0, grid, ways) == 0 && map.LastFailure == "invalid", "no limit");
    }

    private static readonly ExplorationTarget Quest = new("quest", "Quest", new Vector3(0, 0, 100), 2f);

    private static BeaconWay Way(string key, float x, float z, float beyond)
    {
        var opening = new ExplorationTarget(key, key, new Vector3(x, 0, z), OpeningRadius, true);
        return new BeaconWay(opening, new List<Vector3> { Vector3.Zero, opening.Position }, opening.Position.Length(), beyond);
    }

    private static bool Unrouted(Vector3 from, ExplorationTarget target, out IReadOnlyList<Vector3> path, out float length)
    {
        path = null;
        length = 0;
        return false;
    }

    private static bool Straight(Vector3 from, ExplorationTarget target, out IReadOnlyList<Vector3> path, out float length)
    {
        path = new List<Vector3> { from, target.Position };
        length = ExploredMap.Horizontal(from, target.Position);
        return true;
    }

    // Lock picks the best way or the preferred one; a slightly better way does not take over, a clearly better one does.
    private static void Keeping()
    {
        IReadOnlyList<BeaconWay> listed = new[] { Way("a", 0, 20, 30), Way("b", 10, 20, 40) };
        BeaconWays ways = (_, _) => listed;
        var state = new BeaconState { WaySettleSeconds = 1 };
        Check(state.Lock(Quest, Vector3.Zero, 0, Unrouted, (_, _) => Array.Empty<BeaconWay>()) == BeaconLock.Refused,
            "no route and no ways refuses");
        Check(state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways) == BeaconLock.Locked && state.Way?.Opening.Key == "a",
            "the best way leads first");
        state.Release();
        Check(state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways, "b") == BeaconLock.Locked && state.Way?.Opening.Key == "b",
            "a chosen way leads first");
        state.Release();
        state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways);

        float within = (Way("a", 0, 20, 30).Estimate - BeaconState.WaySwitchMargin / 2 - 22.4f) / ExploredMap.BeyondWeight;
        listed = new[] { Way("b", 10, 20, within), Way("a", 0, 20, 30) };
        var update = state.Tick(Vector3.Zero, Quest, Forward, Right, 1, Unrouted, ways);
        Check(update.Routed && update.Way.Key == "a" && update.News == BeaconNews.None, "a way within the margin does not take over");
        Near(20, update.Remaining, 0.01, "the pace follows the opening, not the target beyond it");
        listed = new[] { Way("b", 10, 20, 10), Way("a", 0, 20, 30) };
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2, Unrouted, ways);
        Check(update.Way.Key == "b" && update.News == BeaconNews.None, "a clearly better way takes over silently");
    }

    // The way entered led nowhere new: once the map has taken in its ground, the next best is one listed before.
    private static void DeadEnd()
    {
        IReadOnlyList<BeaconWay> listed = new[] { Way("a", 0, 20, 30), Way("b", 10, 20, 40) };
        BeaconWays ways = (_, _) => listed;
        var state = new BeaconState { WaySettleSeconds = 1 };
        state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways);
        listed = new[] { Way("b", 10, 20, 40) };
        var update = state.Tick(Vector3.Zero, Quest, Forward, Right, 1, Unrouted, ways);
        Check(update.Routed && update.Way.Key == "a" && update.News == BeaconNews.None, "an entered way keeps leading while the map settles");
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 1.5, Unrouted, ways);
        Check(update.Way.Key == "a", "still settling");
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2, Unrouted, ways);
        Check(update.Way.Key == "b" && update.News == BeaconNews.DeadEnd, "back to an earlier opening is a dead end");
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2.5, Unrouted, ways);
        Check(update.Way.Key == "b" && update.News == BeaconNews.None, "a dead end is said once");
    }

    // The guided way leads at once, and going on by an opening listed earlier is its way through, not a dead end.
    private static void GuidedLead()
    {
        IReadOnlyList<BeaconWay> listed = new[] { Way("a", 0, 20, 30), Way("b", 10, 20, 40) };
        BeaconWays ways = (_, _) => listed;
        var state = new BeaconState { WaySettleSeconds = 1 };
        state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways);
        listed = new[] { Way("b", 10, 20, 40) with { Guided = true }, Way("a", 0, 20, 30) };
        var update = state.Tick(Vector3.Zero, Quest, Forward, Right, 1, Unrouted, ways);
        Check(update.Way.Key == "b" && update.News == BeaconNews.None, "the guided way takes over at once");
        listed = new[] { Way("a", 0, 20, 30) with { Guided = true } };
        state.Tick(Vector3.Zero, Quest, Forward, Right, 1.5, Unrouted, ways);
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 3, Unrouted, ways);
        Check(update.Way.Key == "a" && update.News == BeaconNews.None, "on by an earlier opening the engine's path takes, no dead end");
    }

    // The way entered opened onto new ground: the next opening is new, so nothing is said.
    private static void Continuation()
    {
        IReadOnlyList<BeaconWay> listed = new[] { Way("a", 0, 20, 30), Way("b", 10, 20, 40) };
        BeaconWays ways = (_, _) => listed;
        var state = new BeaconState { WaySettleSeconds = 1 };
        state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways);
        listed = new[] { Way("c", 0, 40, 10), Way("b", 10, 20, 40) };
        state.Tick(Vector3.Zero, Quest, Forward, Right, 1, Unrouted, ways);
        var update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2, Unrouted, ways);
        Check(update.Way.Key == "c" && update.News == BeaconNews.None, "a new opening beyond is followed silently");
    }

    private static void RouteKnown()
    {
        IReadOnlyList<BeaconWay> listed = new[] { Way("a", 0, 20, 30) };
        BeaconWays ways = (_, _) => listed;
        var state = new BeaconState { WaySettleSeconds = 1 };
        state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways);
        var update = state.Tick(Vector3.Zero, Quest, Forward, Right, 1, Straight, ways);
        Check(update.Routed && update.News == BeaconNews.RouteKnown && update.Way.Key == null && state.Way == null,
            "a route found replaces the way and is said");
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2, Straight, ways);
        Check(update.News == BeaconNews.None, "said once");
        Near(100, update.Remaining, 0.01, "the pace follows the target again");

        var routed = new BeaconState { WaySettleSeconds = 1 };
        routed.Lock(Quest, Vector3.Zero, 0, Straight, ways);
        Check(routed.Way == null, "a target with a route takes no way");
        update = routed.Tick(Vector3.Zero, Quest, Forward, Right, 1, Straight, ways);
        Check(update.News == BeaconNews.None, "a route from the start is not news");
    }

    // The last way entered and nothing left to try: the lock ends as a lost route, naming the way it was on.
    private static void NoWaysLeft()
    {
        IReadOnlyList<BeaconWay> listed = new[] { Way("a", 0, 20, 30) };
        BeaconWays ways = (_, _) => listed;
        var state = new BeaconState { WaySettleSeconds = 1 };
        state.Lock(Quest, Vector3.Zero, 0, Unrouted, ways);
        listed = Array.Empty<BeaconWay>();
        state.Tick(Vector3.Zero, Quest, Forward, Right, 1, Unrouted, ways);
        var update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2, Unrouted, ways);
        Check(!update.Routed && update.Release == BeaconRelease.None, "no way left: unrouted for a while");
        update = state.Tick(Vector3.Zero, Quest, Forward, Right, 2 + BeaconState.RouteLossSeconds, Unrouted, ways);
        Check(update.Release == BeaconRelease.RouteLost && update.Way.Key == "a", "then lost, naming the last way");
        Check(state.LockedKey == null && state.Way == null, "released");
    }
}
