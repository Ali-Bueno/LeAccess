using LeAccess.Accessibility.Navigation;

internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        try
        {
            EngineTests.Run();
            QueueTests.Run();
            AudioTests.Run();
            MarkTests.Run();
            LifecycleTests.Run();
            BumpTests.Run();
            Console.WriteLine($"Navigation tests passed: {_checks} checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    internal static void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(description);
    }

    internal static void Silent(RadarState[] events, string description) =>
        Check(events.All(state => state == RadarState.Unknown), description);

    internal static void Near(float actual, float expected, string description) =>
        Check(Math.Abs(actual - expected) < 0.00001f, description);
}
