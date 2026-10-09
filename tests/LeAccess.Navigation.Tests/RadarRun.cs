using System.Numerics;
using LeAccess.Accessibility.Navigation;

internal sealed class RadarRun
{
    // Body width 3 keeps round numbers: close 7, open 10, passage 20, edge jump 3, reach 40.
    internal RadarEngine Engine { get; }
    internal Vector2[] Axes { get; } = { Vector2.UnitY, -Vector2.UnitX, Vector2.UnitX, -Vector2.UnitY };
    internal RadarReading[] Readings { get; } = Enumerable.Repeat(new RadarReading(20, true), 4).ToArray();
    internal Vector2 Position;
    internal double Time = 1;
    internal long Session = 1;

    internal RadarRun(float baseline = 20, RadarSettings settings = null)
    {
        Engine = new RadarEngine(settings ?? RadarSettings.FromBodyWidth(3));
        Readings[0] = new RadarReading(baseline, true);
        Program.Silent(Step(dt: 0), "Initial baseline is silent.");
    }

    internal RadarState[] Step(float? depth = null, Vector2? position = null, double dt = 0.05,
        bool available = true, bool continues = true)
    {
        if (depth.HasValue) Readings[0] = new RadarReading(depth.Value, available, continues);
        if (position.HasValue) Position = position.Value;
        Time += dt;
        return Engine.Tick(Position, Axes, Readings, Time, Session);
    }

    internal void WarnWall()
    {
        Program.Silent(Step(6, new Vector2(0, 9)), "Wall starts as a candidate.");
        Program.Check(Step(5, new Vector2(0, 10))[0] == RadarState.Blocked, "Approach warns about the wall.");
    }
}
