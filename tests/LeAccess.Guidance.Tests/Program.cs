internal static class Program
{
    private static int _checks;
    private static int Main()
    {
        try
        {
            GeometryTests.Run();
            RouteTests.Run();
            Console.WriteLine($"Guidance tests passed: {_checks} checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    internal static void Check(bool condition, string description)
    { _checks++; if (!condition) throw new InvalidOperationException(description); }
    internal static void Near(float actual, float expected, string description) =>
        Check(Math.Abs(actual - expected) < 0.0001f, description);
}
