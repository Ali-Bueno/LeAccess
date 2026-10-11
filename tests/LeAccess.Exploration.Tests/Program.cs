internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        try
        {
            MapTests.Run();
            BeaconTests.Run();
            SynthTests.Run();
            StoreTests.Run();
            KeyTests.Run();
            UnexploredTests.Run();
            ArrivalTests.Run();
            TownTests.Run();
            Console.WriteLine($"Exploration tests passed: {_checks} checks.");
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

    internal static void Near(double expected, double actual, double tolerance, string description) =>
        Check(Math.Abs(expected - actual) <= tolerance, $"{description}: expected {expected}, got {actual}");
}
