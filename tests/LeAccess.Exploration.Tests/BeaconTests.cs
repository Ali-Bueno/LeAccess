using System.Numerics;
using LeAccess.Accessibility.Exploration;
using static Program;

internal static class BeaconTests
{
    private static readonly Vector3 Forward = new(0, 0, 1), Right = new(1, 0, 0);
    private static readonly BeaconRouter Router = Route;
    private static int _calls;
    private static bool _routes = true;
    private static Vector3[] _shape;

    internal static void Run()
    {
        Cadence();
        Pings();
        Bearing();
        Arrival();
        LostRoute();
        LockRules();
        Refresh();
    }

    private static void Cadence()
    {
        Near(BeaconState.NearPingSeconds, BeaconState.PingInterval(0), 1e-9, "nearest cadence");
        Near(BeaconState.FarPingSeconds, BeaconState.PingInterval(BeaconState.FarDistance), 1e-9, "far cadence");
        Near(BeaconState.FarPingSeconds, BeaconState.PingInterval(500), 1e-9, "cadence saturates");
        Near(BeaconState.NearPingSeconds, BeaconState.PingInterval(-3), 1e-9, "negative remaining is near");
        Near(BeaconState.FarPingSeconds, BeaconState.PingInterval(float.NaN), 1e-9, "unknown remaining is far");
        Near(0.68, BeaconState.PingInterval(15), 1e-6, "cadence is linear in the remaining route");
    }

    private static void Pings()
    {
        Reset();
        var state = new BeaconState();
        var target = Target("a", 0, 20);
        Check(state.Lock(target, Vector3.Zero, 0, Router) == BeaconLock.Locked && state.LockedKey == "a", "lock");
        Check(!Tick(state, Vector3.Zero, target, 0.3).Ping, "silent while the name is spoken");
        var first = Tick(state, Vector3.Zero, target, 0.6);
        Check(first.Ping && first.Routed, "first ping 0.6 s after locking");
        Near(20, first.Remaining, 0.01, "remaining route");
        double next = 0.6 + BeaconState.PingInterval(20);
        Check(!Tick(state, Vector3.Zero, target, next - 0.05).Ping, "no ping before the interval");
        Check(Tick(state, Vector3.Zero, target, next + 0.01).Ping, "ping at the interval");
    }

    private static void Bearing()
    {
        foreach (var (x, z, pan, ahead, name) in new[]
        {
            (10f, 0f, 1f, 0f, "right"), (-10f, 0f, -1f, 0f, "left"), (0f, -10f, 0f, -1f, "behind"), (0f, 10f, 0f, 1f, "ahead"),
        })
        {
            Reset();
            var state = new BeaconState();
            var target = Target("t", x, z);
            state.Lock(target, Vector3.Zero, 0, Router);
            var update = Tick(state, Vector3.Zero, target, 0.6);
            Check(update.Ping, $"ping {name}");
            Near(pan, update.Pan, 1e-5, $"pan {name}");
            Near(ahead, update.Ahead, 1e-5, $"ahead {name}");
        }

        Reset();
        _shape = new[] { Vector3.Zero, new Vector3(0, 0, 2), new Vector3(10, 0, 2) };
        var corner = new BeaconState();
        var around = Target("corner", 10, 2);
        corner.Lock(around, Vector3.Zero, 0, Router);
        var turn = Tick(corner, Vector3.Zero, around, 0.6);
        Check(turn.Pan > 0.6f && turn.Ahead > 0.6f, $"the look-ahead turns the corner early ({turn.Pan}, {turn.Ahead})");
        Near(12, turn.Remaining, 0.01, "remaining follows the route, not the straight line");
        _shape = null;
    }

    private static void Arrival()
    {
        Reset();
        var state = new BeaconState();
        var target = Target("a", 0, 5);
        state.Lock(target, Vector3.Zero, 0, Router);
        Check(Tick(state, new Vector3(0, 0, 2), target, 0.1).Release == BeaconRelease.None, "not yet arrived");
        var arrived = Tick(state, new Vector3(0, 0, 4), target, 0.2);
        Check(arrived.Release == BeaconRelease.Arrived && arrived.Target.Name == "A" && state.LockedKey == null,
            "arrival within the minimum radius releases");
        var wide = Target("n", 0, 10, 3);
        state.Lock(wide, Vector3.Zero, 0, Router);
        Check(Tick(state, new Vector3(0, 0, 7.5f), wide, 0.1).Release == BeaconRelease.Arrived, "arrival uses the target's radius");
        var high = Target("h", 0, 5);
        state.Lock(high, Vector3.Zero, 0, Router);
        Check(Tick(state, new Vector3(0, 3, 4), high, 0.1).Release == BeaconRelease.Arrived, "arrival is measured on the ground plane");

        // Within range is not enough: the target must be at hand (no wall or other level between them).
        var walled = new BeaconState { AtHand = (player, _) => player.X >= 0 };
        var exit = Target("x", 0, 5, 3);
        walled.Lock(exit, new Vector3(-10, 0, 0), 0, Router);
        Check(Tick(walled, new Vector3(-1, 0, 4), exit, 0.1).Release == BeaconRelease.None && walled.LockedKey == "x",
            "in range but not at hand is no arrival");
        Check(Tick(walled, new Vector3(-9, 0, 0), exit, 0.15).Release == BeaconRelease.None, "out of range the hand is not asked");
        Check(Tick(walled, new Vector3(1, 0, 4), exit, 0.2).Release == BeaconRelease.Arrived, "in range and at hand arrives");
    }

    private static void LostRoute()
    {
        Reset();
        var state = new BeaconState();
        var target = Target("a", 0, 20);
        state.Lock(target, Vector3.Zero, 0, Router);
        _routes = false;
        var gone = Tick(state, Vector3.Zero, target, 0.5);
        Check(!gone.Routed && !gone.Ping && gone.Release == BeaconRelease.None, "a missing route is silent");
        Check(!Tick(state, Vector3.Zero, target, 2.0).Ping, "still silent");
        Check(Tick(state, Vector3.Zero, target, 3.4).Release == BeaconRelease.None, "kept for three seconds");
        var lost = Tick(state, Vector3.Zero, target, 3.5);
        Check(lost.Release == BeaconRelease.RouteLost && lost.Target.Key == "a" && state.LockedKey == null, "released after three seconds");

        Reset();
        state = new BeaconState();
        state.Lock(target, Vector3.Zero, 0, Router);
        _routes = false;
        Tick(state, Vector3.Zero, target, 0.5);
        _routes = true;
        Check(Tick(state, Vector3.Zero, target, 1.0).Routed, "a returning route resumes");
        Check(Tick(state, Vector3.Zero, target, 3.6).Release == BeaconRelease.None, "the loss timer restarted");

        Reset();
        state = new BeaconState();
        state.Lock(target, Vector3.Zero, 0, Router);
        _routes = false;
        Tick(state, Vector3.Zero, target, 0.5);
        state.Suspend();
        Check(Tick(state, Vector3.Zero, target, 3.2).Release == BeaconRelease.None, "time in menus does not count");
        Check(Tick(state, Vector3.Zero, target, 6.1).Release == BeaconRelease.None, "counted again from the resume");
        Check(Tick(state, Vector3.Zero, target, 6.25).Release == BeaconRelease.RouteLost, "released three seconds after resuming");
    }

    private static void LockRules()
    {
        Reset();
        var state = new BeaconState();
        Check(state.Lock(Target("a", 0, 20), Vector3.Zero, 0, Router) == BeaconLock.Locked, "lock a");
        Check(state.Lock(Target("b", 5, 20), Vector3.Zero, 0, Router) == BeaconLock.Retargeted && state.LockedKey == "b", "retarget to b");
        Check(state.Lock(Target("b", 5, 20), Vector3.Zero, 0, Router) == BeaconLock.Cancelled && state.LockedKey == null, "same key cancels");
        state.Lock(Target("a", 0, 20), Vector3.Zero, 0, Router);
        _routes = false;
        Check(state.Lock(Target("c", 9, 9), Vector3.Zero, 0, Router) == BeaconLock.Refused && state.LockedKey == "a",
            "a refused retarget keeps the old lock");
        _routes = true;
        Check(state.Cancel() && state.LockedKey == null, "cancel");
        Check(!state.Cancel(), "nothing to cancel");
        Check(state.Lock(new ExplorationTarget(null, "x", Vector3.One, 0), Vector3.Zero, 0, Router) == BeaconLock.Invalid, "no key");
        Check(state.Lock(Target("n", float.NaN, 0), Vector3.Zero, 0, Router) == BeaconLock.Invalid, "no position");
        _routes = false;
        Check(state.Lock(Target("a", 0, 20), Vector3.Zero, 0, Router) == BeaconLock.Refused && state.LockedKey == null, "refused lock");
        _routes = true;

        var a = Target("a", 0, 20);
        var b = Target("b", 0, 25);
        state.Lock(a, Vector3.Zero, 0, Router);
        Check(Tick(state, Vector3.Zero, a, 0.6).Ping, "pinging a");
        state.Lock(b, Vector3.Zero, 1.0, Router);
        Check(!Tick(state, Vector3.Zero, b, 1.3).Ping, "a retarget waits for its name");
        Check(Tick(state, Vector3.Zero, b, 1.6).Ping, "then pings the new target");
        var vanished = state.Tick(Vector3.Zero, null, Forward, Right, 1.7, Router);
        Check(vanished.Release == BeaconRelease.TargetLost && vanished.Target.Key == "b" && state.LockedKey == null, "a vanished target releases");
        state.Lock(a, Vector3.Zero, 2, Router);
        Check(Tick(state, Vector3.Zero, b, 2.1).Release == BeaconRelease.TargetLost, "another entry is not the locked target");
        Check(Tick(state, Vector3.Zero, a, 2.2).Release == BeaconRelease.None, "nothing locked: nothing happens");
    }

    private static void Refresh()
    {
        Reset();
        var state = new BeaconState();
        var target = Target("a", 0, 20);
        state.Lock(target, Vector3.Zero, 0, Router);
        Check(_calls == 1, "the lock proves a route");
        Tick(state, Vector3.Zero, target, 0.1);
        Check(_calls == 1, "no refresh within 0.4 s");
        var moved = Target("a", 3, 20);
        Tick(state, Vector3.Zero, moved, 0.2);
        Check(_calls == 2, "a target that moved more than two units refreshes");
        var strayed = new Vector3(-3, 0, 5);
        Tick(state, strayed, moved, 0.25);
        Check(_calls == 3, "a player off the path refreshes");
        Tick(state, strayed, moved, 0.3);
        Check(_calls == 3, "back on the new path: no refresh");
        Tick(state, strayed, moved, 0.66);
        Check(_calls == 4, "refreshed every 0.4 s");
    }

    private static BeaconUpdate Tick(BeaconState state, Vector3 player, ExplorationTarget target, double now) =>
        state.Tick(player, target, Forward, Right, now, Router);

    private static ExplorationTarget Target(string key, float x, float z, float arrival = 0) =>
        new(key, key.ToUpperInvariant(), new Vector3(x, 0, z), arrival);

    private static void Reset()
    {
        _calls = 0;
        _routes = true;
        _shape = null;
    }

    private static bool Route(Vector3 from, ExplorationTarget target, out IReadOnlyList<Vector3> path, out float length)
    {
        _calls++;
        path = Array.Empty<Vector3>();
        length = 0;
        if (!_routes) return false;
        var points = _shape ?? new[] { from, target.Position };
        for (int index = 1; index < points.Length; index++) length += Vector3.Distance(points[index - 1], points[index]);
        path = points;
        return true;
    }
}
