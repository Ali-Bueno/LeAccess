using LeAccess.Accessibility.Navigation.Guidance;

internal static partial class Program
{
    private static int _checks;
    private static void Check(bool pass, string message)
    { _checks++; if (!pass) throw new Exception(message); }

    private static void Main()
    {
        StateTests();
        RouteTests();
        ServiceTests();
        AcceptanceTests();
        Console.WriteLine($"PASS: {_checks} passive orientation state, service, route and audio checks");
    }
}
