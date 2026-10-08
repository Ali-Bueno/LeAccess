using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class ArrivalTests
{
    private const float SeenRange = 20f;
    private const string CameFrom = "A90";

    // 2026-10-08 log and the B10 bundle: arrival spawn after the A90 cinematic, and the A90 exit's listener
    // 10.5 m away (flat) and below the spawn, beyond the 9 m interaction range and the 10 m off-mesh limit.
    private static readonly Vector3 B10Spawn = new(-1.36f, 6.16f, 162.68f);
    private static readonly Vector3 B10WayBack = new(0.1f, -5.1f, 173.1f);
    // The B10 waypoint, about 100 m from the spawn.
    private static readonly Vector3 B10Waypoint = new(40f, 8.3f, 64.8f);

    internal static void Run()
    {
        WayBackKnownOnArrival();
        OnlyTheWayBack();
        ArrivalIsTheFirstPosition();
        NewSessionMovesTheArrival();
        UnknownArrival();
    }

    private static void WayBackKnownOnArrival()
    {
        var arrival = new ExplorationArrival();
        arrival.Observe(1, B10Spawn);
        Check(arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "the exit the player came through is known from the arrival spawn");
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, 10f),
            "it still has to be within the seen range of the arrival point");
    }

    private static void OnlyTheWayBack()
    {
        var arrival = new ExplorationArrival();
        arrival.Observe(1, B10Spawn);
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, "B20", CameFrom, B10WayBack, SeenRange),
            "an exit to another scene stays hidden until seen");
        Check(!arrival.IsWayBack(ExplorationCategory.Waypoint, CameFrom, CameFrom, B10WayBack, SeenRange),
            "only exits are the way back");
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, null, CameFrom, B10WayBack, SeenRange),
            "an exit without a destination is not the way back");
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, null, B10WayBack, SeenRange),
            "after login there is no previous scene");
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10Waypoint, SeenRange),
            "an exit far from the arrival point (a waypoint arrival) is not revealed");
    }

    private static void ArrivalIsTheFirstPosition()
    {
        var arrival = new ExplorationArrival();
        arrival.Observe(1, B10Spawn);
        arrival.Observe(1, B10Waypoint);
        Check(arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "walking away keeps the arrival point");
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10Waypoint + new Vector3(5, 0, 0), SeenRange),
            "a later position is not an arrival");
    }

    private static void NewSessionMovesTheArrival()
    {
        var arrival = new ExplorationArrival();
        arrival.Observe(1, B10Spawn);
        arrival.Observe(2, B10Waypoint);
        Check(arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10Waypoint + new Vector3(5, 0, 0), SeenRange),
            "a new scene session arrives where the player now stands");
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "the previous session's arrival point is forgotten");
    }

    private static void UnknownArrival()
    {
        var arrival = new ExplorationArrival();
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "nothing is the way back before an arrival is observed");
        arrival.Observe(1, new Vector3(float.NaN, 0, 0));
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "a non-finite position is not an arrival");
        arrival.Observe(1, B10Spawn);
        Check(arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "the first finite position becomes the arrival");
        arrival.Clear();
        Check(!arrival.IsWayBack(ExplorationCategory.Exit, CameFrom, CameFrom, B10WayBack, SeenRange),
            "clearing forgets the arrival");
    }
}
