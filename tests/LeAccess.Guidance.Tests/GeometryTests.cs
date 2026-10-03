using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;
using static Program;

internal static class GeometryTests
{
    internal static void Run()
    {
        Bearings();
        Routes();
        Progress();
    }

    private static void Bearings()
    {
        var directions = new[] { "up", "up_right", "right", "down_right", "down", "down_left", "left", "up_left" };
        var deltas = new[] { new Vector3(0, 0, 1), new(1, 0, 1), new(1, 0, 0), new(1, 0, -1),
            new(0, 0, -1), new(-1, 0, -1), new(-1, 0, 0), new(-1, 0, 1) };
        for (int index = 0; index < deltas.Length; index++)
        {
            Check(GuidanceGeometry.Direction(deltas[index], Vector3.UnitZ, Vector3.UnitX) ==
                "guidance.direction." + directions[index], "Each screen sector has its own direction.");
            var rotate = Matrix4x4.CreateRotationY(MathF.PI / 2);
            Check(GuidanceGeometry.Direction(Vector3.Transform(deltas[index], rotate),
                Vector3.Transform(Vector3.UnitZ, rotate), Vector3.Transform(Vector3.UnitX, rotate)) ==
                "guidance.direction." + directions[index], "A rotated camera preserves screen-relative bearing.");
        }
        Check(GuidanceGeometry.Direction(new(0, 9, 2), new(0, -2, 4), new(3, 1, 0)) == "guidance.direction.up",
            "Camera tilt and non-unit axes do not alter horizontal bearing.");
        foreach (var delta in new[] { Vector3.Zero, new Vector3(0, 2, 0), new(float.NaN, 0, 1), new(float.PositiveInfinity, 0, 1) })
            Check(GuidanceGeometry.Direction(delta, Vector3.UnitZ, Vector3.UnitX) is null,
                "Coincident or invalid horizontal direction is unavailable.");
        Check(GuidanceGeometry.Direction(Vector3.One, Vector3.Zero, Vector3.UnitX) is null, "Zero forward cannot orient guidance.");
        Check(GuidanceGeometry.Direction(Vector3.One, Vector3.UnitZ, Vector3.Zero) is null, "Zero right cannot orient guidance.");
        Check(GuidanceGeometry.Direction(Vector3.One, new(float.NaN, 0, 1), Vector3.UnitX) is null, "Invalid forward cannot orient guidance.");
        Check(GuidanceGeometry.Direction(Vector3.One, Vector3.UnitZ, new(float.PositiveInfinity, 0, 0)) is null, "Invalid right cannot orient guidance.");
    }

    private static void Routes()
    {
        var goal = new Vector3(4, 0, 3);
        Vector3[] corners = { Vector3.Zero, new(0, 0, 3), goal };
        Check(GuidanceGeometry.TryRoute(corners, Vector3.Zero, goal, 0.5f, out float distance, out var next),
            "A complete detour provides route geometry.");
        Near(distance, 7, "Walking distance sums path segments instead of the straight-line shortcut.");
        Check(next == new Vector3(0, 0, 3), "Guidance points at the first meaningful corner before the destination.");
        Check(GuidanceGeometry.TryRoute(new[] { Vector3.Zero, Vector3.Zero, new Vector3(0, 0, 0.1f), goal },
            Vector3.Zero, goal, 0.5f, out _, out next) && next == new Vector3(0, 0, 0.1f),
            "A tight turn inside body radius is preserved while exact duplicate corners are skipped.");
        Check(GuidanceGeometry.TryRoute(new[] { Vector3.Zero, Vector3.Zero }, Vector3.Zero, Vector3.Zero,
            0.5f, out distance, out next) && distance == 0 && next == Vector3.Zero, "A coincident marker yields zero distance.");
        foreach (var invalid in new Vector3[][] { null, Array.Empty<Vector3>(), new[] { Vector3.Zero },
            new[] { Vector3.UnitX, goal }, new[] { Vector3.Zero, Vector3.UnitZ },
            new[] { Vector3.Zero, new Vector3(float.NaN, 0, 0), goal },
            new[] { Vector3.Zero, new Vector3(float.PositiveInfinity, 0, 0), goal } })
            Check(!GuidanceGeometry.TryRoute(invalid, Vector3.Zero, goal, 0.5f, out _, out _),
                "Missing, truncated, displaced or invalid corners cannot claim a route.");
        foreach (float tolerance in new[] { 0f, -1, float.NaN, float.PositiveInfinity })
            Check(!GuidanceGeometry.TryRoute(corners, Vector3.Zero, goal, tolerance, out _, out _),
                "Invalid native tolerance cannot validate route endpoints.");
        Check(!GuidanceGeometry.TryRoute(corners, new Vector3(float.NaN), goal, 0.5f, out _, out _), "Invalid origin rejects route.");
        Check(!GuidanceGeometry.TryRoute(corners, Vector3.Zero, new Vector3(float.NaN), 0.5f, out _, out _), "Invalid destination rejects route.");
    }

    private static void Progress()
    {
        var progress = new GuidanceProgress();
        string Observe(float distance, int target = 1, bool route = true, long session = 1, Vector3? goal = null) =>
            progress.Observe(session, target, route, distance, goal ?? new Vector3(0, 0, 10), 1);
        Check(Observe(10) is null, "First report makes no progress claim.");
        Check(Observe(9.6f) is null && Observe(9.2f) is null, "Sub-body distance fluctuations remain quiet.");
        Check(Observe(8.8f) == "guidance.closer", "Small successive movement accumulates into meaningful progress.");
        Check(Observe(8.8f) is null, "An unchanged report does not repeat progress.");
        Check(Observe(10) == "guidance.farther", "Backtracking beyond body width is reported.");
        Check(Observe(2, route: false) is null, "Switching route to straight distance clears incomparable progress.");
        Check(Observe(8, route: true) is null, "Switching straight distance to route clears incomparable progress.");
        Check(Observe(20, target: 2) is null, "New target does not inherit previous target progress.");
        Check(Observe(2, target: 2, session: 2) is null, "New player/scene session clears progress.");
        Check(Observe(20, target: 2, session: 2, goal: new(0, 0, 20)) is null, "A moving objective invalidates distance comparison.");
        Check(Observe(float.NaN) is null && Observe(10) is null, "Invalid distance clears the baseline.");
        Check(progress.Observe(1, 1, true, 0, Vector3.Zero, 0) is null && Observe(10) is null,
            "Missing body size cannot create progress thresholds.");
        Check(Observe(-1) is null && Observe(10) is null, "Negative distances clear baseline.");
        progress.Reset();
        Check(Observe(0) is null, "Explicit lifecycle reset clears progress.");
        progress.Reset();
        Check(Observe(10, goal: new(0, 0, 10)) is null, "A moving-marker test starts with a fresh baseline.");
        Check(Observe(9.6f, goal: new(0, 0, 9.6f)) is null && Observe(9.2f, goal: new(0, 0, 9.2f)) is null,
            "Small target drift does not immediately report player progress.");
        Check(Observe(8.8f, goal: new(0, 0, 8.8f)) is null,
            "Cumulative marker drift beyond body width resets comparison rather than claiming the player moved closer.");
    }
}
