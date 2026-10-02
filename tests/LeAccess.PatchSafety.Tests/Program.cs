string repository = FindRepository();
int regressionChecks = SafetyRegressionTests.Run();
bool candidateInput = args.Contains("--stdin");
var hooks = candidateInput ? HookSource.Parse(Console.In.ReadToEnd(), "<candidate input>") : HookSource.ReadRepository(repository);
if (hooks.Count == 0) throw new InvalidOperationException("No hook declarations were supplied for audit.");
if (args.Contains("--self-test"))
{
    Console.WriteLine($"Patch safety self-tests passed: {regressionChecks} checks; discovered {hooks.Count} production hook declarations.");
    return;
}
string directoryArgument = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal));
string gameDirectory = directoryArgument == null ? @"D:\games\steam\steamapps\common\Last Epoch" : Path.GetFullPath(directoryArgument);
using var installed = new InstalledInterop(gameDirectory);
var failures = new List<string>();
var unique = new HashSet<MethodKey>();
foreach (var hook in hooks)
{
    try
    {
        var target = installed.Resolve(hook);
        if (target.ContainsGenericParameters) throw new InvalidOperationException("Open generic target is not supported.");
        installed.CheckCallbacks(hook, target);
        if (SignatureSafety.HasUnsafeObjectSignature(target))
            throw new InvalidOperationException("Exact Il2CppObjectBase argument/return cannot safely pass through the native Harmony bridge.");
        var key = MethodKey.From(target);
        unique.Add(key);
        if (!installed.NativeMethods.HasAddress(key))
            throw new InvalidOperationException("Target has no native address in the installed method map; safety cannot be established.");
        var aliases = installed.NativeMethods.Aliases(key).ToArray();
        if (aliases.Length != 0)
            throw new InvalidOperationException($"Target shares its native body with {aliases.Length} other methods: {string.Join(", ", aliases.Take(3).Select(installed.Describe))}.");
    }
    catch (Exception ex)
    {
        failures.Add($"{hook.Location}: {hook.TypeName}.{hook.MethodName}: {ex.GetType().Name}: {ex.Message}");
    }
}
if (failures.Count != 0)
    throw new InvalidOperationException($"Patch safety failed for {failures.Count}/{hooks.Count} declarations under {gameDirectory}:\n" + string.Join(Environment.NewLine, failures));
string scope = candidateInput ? "candidate input only" : "all production patch files";
Console.WriteLine($"Patch safety tests passed: {regressionChecks} regression checks; {hooks.Count} declarations / {unique.Count} native targets ({scope}). Installation: {gameDirectory}");

static string FindRepository()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "LeAccess", "LeAccess.csproj"))) return directory.FullName;
    throw new DirectoryNotFoundException("Cannot locate the LeAccess repository from the test output directory.");
}
