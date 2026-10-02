using LeAccess.Accessibility.Navigation;
using static Program;

internal static class QueueTests
{
    internal static void Run()
    {
        var queue = new RadarCueQueue();
        Check(!queue.TryTake(out _, out var absent) && absent == RadarState.Unknown, "Empty queue yields no cue.");
        queue.Enqueue(new[] { RadarState.Open, RadarState.Unknown, RadarState.Unknown, RadarState.Blocked });
        Check(queue.TryTake(out var direction, out var state) && direction == RadarDirection.Down
            && state == RadarState.Blocked, "Obstacle takes priority over background opening.");
        Check(queue.TryTake(out direction, out state) && direction == RadarDirection.Up
            && state == RadarState.Open, "Lower-priority opening remains pending.");

        queue.Clear();
        queue.Enqueue(Enumerable.Repeat(RadarState.Blocked, 4).ToArray());
        foreach (RadarDirection expected in Enum.GetValues<RadarDirection>())
            Check(queue.TryTake(out direction, out state) && direction == expected,
                "Equal priorities follow round robin.");
        queue.Enqueue(new[] { RadarState.Blocked, RadarState.Blocked, RadarState.Unknown, RadarState.Unknown });
        Check(queue.TryTake(out direction, out _) && direction == RadarDirection.Up, "Round robin continues from last played direction.");
        queue.Enqueue(new[] { RadarState.Blocked, RadarState.Unknown, RadarState.Unknown, RadarState.Unknown });
        Check(queue.TryTake(out direction, out _) && direction == RadarDirection.Left, "Repeated first direction cannot starve its neighbour.");

        queue.Clear();
        queue.Enqueue(new[] { RadarState.Open, RadarState.Unknown, RadarState.Unknown, RadarState.Unknown });
        queue.Enqueue(new[] { RadarState.Blocked, RadarState.Unknown, RadarState.Open, RadarState.Unknown });
        Check(queue.PendingCount == 2, "Coalescing bounds queue to one cue per direction.");
        Check(queue.TryTake(out direction, out state) && direction == RadarDirection.Up
            && state == RadarState.Blocked, "Newer state replaces older cue in the same direction.");
        queue.Retain(new RadarState[4]);
        Check(queue.PendingCount == 0, "State loss cancels queued stale opening.");

        queue.Enqueue(new[] { RadarState.Blocked, RadarState.Open, RadarState.Unknown, RadarState.Unknown });
        queue.Retain(new[] { RadarState.Blocked, RadarState.Blocked, RadarState.Unknown, RadarState.Unknown });
        Check(queue.PendingCount == 1, "Retain preserves matching cue and discards mismatching cue.");
        queue.Clear();
        Check(queue.PendingCount == 0 && !queue.TryTake(out _, out _), "Suspension clears all pending cues.");

        var run = new RadarRun();
        run.Step(5);
        queue.Enqueue(run.Step(5, System.Numerics.Vector2.UnitY));
        Check(queue.PendingCount == 1, "Confirmed engine event can be queued.");
        run.Step(0, available: false);
        queue.Retain(run.Engine.States);
        Check(queue.PendingCount == 0, "Unavailable geometry cancels engine audio already pending in queue.");
    }
}
