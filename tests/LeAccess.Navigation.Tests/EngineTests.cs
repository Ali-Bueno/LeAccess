using System.Numerics;
using LeAccess.Accessibility.Navigation;
using static Program;

internal static class EngineTests
{
    internal static void Run()
    {
        ScaleSettings();
        QuietCorridorAndFourDirections();
        Hysteresis();
        Confirmation();
        Unavailable();
        RetreatAndDoorway();
        OpeningCooldown(false);
        OpeningCooldown(true);
        LifecycleResets();
        CameraDriftDoesNotEraseConfirmation();
    }

    private static void ScaleSettings()
    {
        var original = RadarSettings.FromBodyWidth(3);
        Near(original.OpenDepth, 10, "D2 body-width tuning retains the open ratio.");
        Near(original.CloseDepth, 7, "Closing leaves one body-width of hysteresis.");
        Near(original.ReachDepth, 20, "Reach retains the D2 ratio.");
        Near(original.ConfirmDistance, 1, "Confirmation scales with body width.");
        var scaled = RadarSettings.FromBodyWidth(0.6f);
        Near(scaled.OpenDepth, 2, "Unity width scales the open depth.");
        Near(scaled.CloseDepth, 1.4f, "Unity width scales the close depth.");
        Near(scaled.ReachDepth, 4, "Unity width scales reach.");
        Near(scaled.ConfirmDistance, 0.2f, "Unity width scales confirmation.");
        var tuned = RadarSettings.FromBodyWidth(3, 8);
        Check(tuned.ReachDepth >= tuned.OpenDepth + 3, "Reach exceeds configurable open depth.");
        try { RadarSettings.FromBodyWidth(float.NaN); Check(false, "Invalid width must be rejected."); }
        catch (ArgumentOutOfRangeException) { Check(true, "Invalid width is rejected."); }
    }

    private static void CameraDriftDoesNotEraseConfirmation()
    {
        var run = new RadarRun();
        ulong revision = run.Engine.Revision;
        bool heardWall = false;
        for (int poll = 0; poll < 8; poll++)
        {
            float angle = (poll % 2 == 0 ? 1 : -1) * 0.001f;
            var rotation = Matrix3x2.CreateRotation(angle);
            run.Axes[0] = Vector2.TransformNormal(Vector2.UnitY, rotation);
            run.Axes[1] = Vector2.TransformNormal(-Vector2.UnitX, rotation);
            run.Axes[2] = Vector2.TransformNormal(Vector2.UnitX, rotation);
            run.Axes[3] = Vector2.TransformNormal(-Vector2.UnitY, rotation);
            heardWall |= run.Step(5)[0] == RadarState.Blocked;
        }
        Check(heardWall, "A wall confirms despite small camera-axis drift on every poll.");
        Check(run.Engine.Revision == revision, "Sub-confirmation camera drift does not silently rebase every poll.");

        var turn = Matrix3x2.CreateRotation(0.1f);
        for (int i = 0; i < run.Axes.Length; i++) run.Axes[i] = Vector2.TransformNormal(run.Axes[i], turn);
        Silent(run.Step(20), "A camera turn large enough to change the probed corridor rebases silently.");
        Check(run.Engine.Revision > revision && run.Engine.LastResetReason == RadarResetReason.CameraTurn,
            "A real camera turn remains an explicit reset reason.");
    }

    private static void QuietCorridorAndFourDirections()
    {
        var run = new RadarRun();
        RadarState[] reusable = run.Engine.Events;
        for (int i = 0; i < 20; i++) Silent(run.Step(position: new Vector2(0, i)), "Open corridor stays quiet.");
        Check(ReferenceEquals(reusable, run.Engine.Events), "Events use the same buffer across ticks.");
        for (int i = 0; i < 4; i++) run.Readings[i] = new RadarReading(5, true);
        Silent(run.Step(), "Each direction starts its own wall candidate.");
        Check(run.Step(position: run.Position + Vector2.UnitY).All(s => s == RadarState.Blocked),
            "All four directions can confirm in the same tick.");
        Check(run.Engine.States.All(s => s == RadarState.Blocked), "All confirmed states are exposed.");
        Silent(run.Step(), "Unchanged walls do not repeat.");
    }

    private static void Hysteresis()
    {
        var run = new RadarRun(5);
        foreach (float depth in new[] { 7.1f, 8, 9.9f, 7, 8 })
            Silent(run.Step(depth), "A committed wall stays blocked inside the hysteresis band.");
        Check(run.Engine.States[0] == RadarState.Blocked, "Jitter never clears the committed obstacle.");
        Silent(run.Step(10), "The open threshold starts a candidate.");
        Check(run.Step(10, Vector2.UnitY)[0] == RadarState.Open, "Traversable depth confirms open.");
        Silent(run.Step(8), "Open remains open inside the hysteresis band.");
        Check(run.Engine.States[0] == RadarState.Open, "Mid-band depth preserves open.");
        Silent(run.Step(7), "The closing threshold starts a wall candidate.");
        Check(run.Step(7, Vector2.UnitY * 2)[0] == RadarState.Blocked, "Closing threshold confirms a wall.");
        var midBand = new RadarRun(8);
        Check(midBand.Engine.States[0] == RadarState.Open, "Initial mid-band baseline is a verdict, not unknown.");
    }

    private static void Confirmation()
    {
        var moving = new RadarRun();
        Silent(moving.Step(5, new Vector2(0, 0.1f)), "Changed observation starts a candidate.");
        for (int i = 2; i <= 10; i++)
            Silent(moving.Step(5, new Vector2(0, i / 10f)), "Continued small movements never time-confirm.");
        Check(moving.Step(5, new Vector2(0, 1.2f))[0] == RadarState.Blocked,
            "A persistent candidate confirms after sufficient actual displacement.");

        var stationary = new RadarRun();
        Silent(stationary.Step(5), "Stationary change starts a candidate.");
        Silent(stationary.Step(5), "One stationary poll cannot confirm.");
        Silent(stationary.Step(5), "Stationary confirmation also waits for the stopped window.");
        Check(stationary.Step(5, dt: 0.06)[0] == RadarState.Blocked,
            "Stationary player confirms after both time requirements.");

        var flicker = new RadarRun();
        Silent(flicker.Step(5), "Flicker starts a candidate.");
        Silent(flicker.Step(20, Vector2.UnitY), "A reverted candidate is forgotten.");
        Silent(flicker.Step(5, Vector2.UnitY * 2), "Reappearing wall starts a fresh candidate.");
    }

    private static void Unavailable()
    {
        var run = new RadarRun();
        Silent(run.Step(5), "Wall candidate starts before read loss.");
        Silent(run.Step(0, Vector2.UnitY, available: false), "Unavailable depth never means obstacle.");
        Check(run.Engine.States[0] == RadarState.Unknown, "Read loss cancels queued directional audio.");
        Silent(run.Step(5, Vector2.UnitY * 2), "Availability return starts a fresh candidate.");
        Check(run.Step(5, Vector2.UnitY * 3)[0] == RadarState.Blocked, "Reliable reading confirms again.");
        Silent(run.Step(float.NaN), "Invalid depth is unavailable.");
        Silent(run.Step(-1), "Negative depth is unavailable.");
        Silent(run.Step(float.PositiveInfinity), "Infinite depth is unavailable.");
        var initial = new RadarEngine(RadarSettings.FromBodyWidth(3));
        var absent = Enumerable.Repeat(new RadarReading(0, false), 4).ToArray();
        Silent(initial.Tick(Vector2.Zero, run.Axes, absent, 1, 1), "Absent initial geometry is silent.");
        var found = Enumerable.Repeat(new RadarReading(5, true), 4).ToArray();
        Silent(initial.Tick(Vector2.Zero, run.Axes, found, 1.05, 1), "First reliable geometry establishes a silent baseline.");
    }

    private static void RetreatAndDoorway()
    {
        var retreat = new RadarRun(15);
        retreat.WarnWall();
        Silent(retreat.Step(10, new Vector2(0, 5)), "Retreat starts a longer-clearance candidate.");
        Silent(retreat.Step(11, new Vector2(0, 4)), "The same wall farther away is not an opening.");
        Check(retreat.Engine.States[0] == RadarState.Open, "Retreat changes internal state while audio remains suppressed.");
        for (int x = 1; x <= 21; x++)
            Silent(retreat.Step(11, new Vector2(x, 4)), "Discarded retreat opening never plays later along the wall.");

        var reapproach = new RadarRun(15);
        reapproach.WarnWall();
        reapproach.Step(10, new Vector2(0, 5));
        reapproach.Step(11, new Vector2(0, 4));
        Silent(reapproach.Step(6, new Vector2(0, 9)), "Reapproach starts another wall candidate.");
        Check(reapproach.Step(5, new Vector2(0, 10))[0] == RadarState.Blocked,
            "Approaching the same wall warns again.");

        var doorway = new RadarRun(15);
        doorway.WarnWall();
        Silent(doorway.Step(20, new Vector2(1, 10)), "A doorway begins a new open candidate.");
        Check(doorway.Step(20, new Vector2(2, 10))[0] == RadarState.Open,
            "Actual doorway clearance beyond the remembered wall sounds open.");

        var lateral = new RadarRun(15);
        lateral.WarnWall();
        for (int x = 1; x <= 21; x++) Silent(lateral.Step(5, new Vector2(x, 10)), "Sidestepping along wall changes no state.");
        Silent(lateral.Step(10, new Vector2(21, 5)), "Distant lateral opening starts a candidate.");
        Check(lateral.Step(11, new Vector2(21, 4))[0] == RadarState.Open,
            "A wall left outside the lateral neighbourhood no longer suppresses new openings.");
    }

    private static void OpeningCooldown(bool replaceHeld)
    {
        var run = new RadarRun(5);
        run.Step(20);
        Check(run.Step(20, Vector2.UnitY)[0] == RadarState.Open, "First opening is announced.");
        run.Step(5, Vector2.UnitY * 2);
        Check(run.Step(5, Vector2.UnitY * 3)[0] == RadarState.Blocked, "Wall is never delayed by opening cooldown.");
        run.Step(20, Vector2.UnitY * 4);
        Silent(run.Step(20, Vector2.UnitY * 5), "Second opening waits during opening cooldown.");
        if (replaceHeld)
        {
            run.Step(5, Vector2.UnitY * 6);
            Check(run.Step(5, Vector2.UnitY * 7)[0] == RadarState.Blocked, "New confirmed wall replaces a held opening.");
            for (int i = 0; i < 12; i++) Silent(run.Step(), "Superseded opening is never replayed.");
        }
        else
        {
            int openings = 0;
            for (int i = 0; i < 12; i++)
                if (run.Step()[0] == RadarState.Open) openings++;
            Check(openings == 1, "Held opening eventually plays once, then remains quiet.");
        }
    }

    private static void LifecycleResets()
    {
        foreach (string cause in new[] { "session", "gap", "time", "teleport", "axes", "explicit" })
        {
            var run = new RadarRun();
            run.Step(5);
            Check(run.Step(5, Vector2.UnitY)[0] == RadarState.Blocked, "Prepare a confirmed obstacle before reset.");
            ulong previousRevision = run.Engine.Revision;
            if (cause == "session") run.Session++;
            if (cause == "time") run.Time -= 1;
            if (cause == "teleport") run.Position += Vector2.UnitY * run.Engine.Settings.TeleportDistance;
            if (cause == "axes")
                for (int i = 0; i < 4; i++) run.Axes[i] = new Vector2(-run.Axes[i].Y, run.Axes[i].X);
            if (cause == "explicit") run.Engine.Reset();
            Silent(run.Step(20, dt: cause == "gap" ? 0.3 : 0.05), $"{cause} establishes a fresh silent baseline.");
            Check(run.Engine.Revision > previousRevision, $"{cause} invalidates pending and playing audio by revision.");
            Check(run.Engine.States[0] == RadarState.Open, $"{cause} discards old obstacle state.");
            Silent(run.Step(), $"{cause} does not replay previous cues.");
        }
        var invalid = new RadarRun();
        Silent(invalid.Step(position: new Vector2(float.NaN, 0)), "Invalid player position resets safely.");
        Check(invalid.Engine.States.All(s => s == RadarState.Unknown), "Invalid position exposes no usable states.");
        Silent(invalid.Step(position: Vector2.Zero), "Valid position returns with silent baseline.");
        invalid.Axes[0] = Vector2.Zero;
        Silent(invalid.Step(), "Missing camera direction resets safely.");
    }
}
