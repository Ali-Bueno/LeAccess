using System.Reflection;

internal static class SafetyRegressionTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("Safety regression: " + message);
        }
        void Reject(Action action, string message)
        {
            checks++;
            try { action(); }
            catch (InvalidOperationException) { return; }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("Safety regression: " + message);
        }

        const string source = """
            using HarmonyLib;
            using UnityEngine;
            using Alias = UnityEngine.Component;
            // [HarmonyPatch(typeof(Fake), nameof(Fake.NotARealHook))]
            [HarmonyPatch(typeof(A), nameof(A.Open))]
            class Basic { static void Postfix(A __instance) {} }
            [HarmonyPatch(typeof(B), nameof(B.Flag), MethodType.Setter)]
            class Setter { static void Postfix(bool value) {} }
            [HarmonyPatch(typeof(C), nameof(C.Show), typeof(RectTransform), typeof(int))]
            class Overload { static void Postfix(RectTransform element) {} }
            [HarmonyPatch(typeof(D), nameof(D.Refresh), new System.Type[0])]
            class Empty { static void Postfix() {} }
            [HarmonyPatch]
            class Dynamic {
                static IEnumerable<MethodBase> TargetMethods() {
                    yield return AccessTools.Method(typeof(E), nameof(E.OnEnable), Type.EmptyTypes);
                    yield return AccessTools.Method(typeof(F), nameof(F.Show), new[] { typeof(string), typeof(float) });
                    yield return AccessTools.Method(typeof(G), "OnDisable", new Type[] { });
                }
                static void Prefix(Alias __instance) {}
            }
            """;
        var hooks = HookSource.Parse(source, "fixture.cs");
        Check(hooks.Count == 7, "all attribute/dynamic targets must be found, ignoring comments");
        Check(hooks[0].ParameterTypes == null, "omitted signature must remain distinct from zero parameters");
        Check(hooks[1].MethodName == "set_Flag", "setter maps to actual metadata method");
        Check(hooks[2].ParameterTypes.SequenceEqual(new[] { "RectTransform", "int" }), "explicit overload order");
        Check(hooks[3].ParameterTypes.Length == 0 && hooks[4].ParameterTypes.Length == 0,
            "both parameterless syntaxes are exact");
        Check(hooks[5].ParameterTypes.SequenceEqual(new[] { "string", "float" }), "dynamic overload parameters");
        Check(hooks[6].ParameterTypes.Length == 0 && hooks[6].MethodName == "OnDisable", "literal name and explicit empty array");
        Check(hooks[4].CallbackParameters.Single().Type == "Alias" && hooks[4].Aliases["Alias"] == "UnityEngine.Component",
            "shared callback context and source aliases");
        Reject(() => HookSource.Parse("[HarmonyPatch(typeof(A), GetName())] class P { static void Postfix(){} }", "bad.cs"),
            "computed target names must fail closed");
        Reject(() => HookSource.Parse("[HarmonyPatch] class P { static void Postfix(){} static MethodBase TargetMethod(){ return Choose(); } }", "bad.cs"),
            "unrecognized dynamic providers must fail closed");
        Reject(() => HookSource.Parse("[HarmonyPatch] class P { static void Postfix(){} static IEnumerable<MethodBase> TargetMethods(){} }", "bad.cs"),
            "empty dynamic providers must not hide a patch");
        Reject(() => HookSource.Parse("class P { void Install() { harmony.Patch(target); } }", "bad.cs"),
            "manual patches cannot bypass discovery");
        Reject(() => HookSource.Parse("[HarmonyPatch(typeof(A), nameof(A.Open))] class P {", "bad.cs"),
            "invalid or partially written source cannot silently omit hooks");
        Reject(() => HookSource.Parse("[HarmonyPatch(typeof(A), nameof(A.Open))][HarmonyPatch(typeof(B), nameof(B.Open))] class P { static void Postfix(){} }", "bad.cs"),
            "split attributes cannot silently hide a target");

        Hook StateHook(string prefixType, string postfixType) => HookSource.Parse(
            $"using StateAlias = Mod.OwnedSnapshot; [HarmonyPatch(typeof(A), nameof(A.Open))] class P {{ " +
            $"static void Prefix(out {prefixType} __state) {{ __state = default; }} " +
            $"static void Postfix({postfixType} __state) {{ }} }}", "state.cs").Single();
        foreach (var pair in new[] { ("Mod.OwnedSnapshot", "Mod.OwnedSnapshot"),
            ("StateAlias", "global::Mod.OwnedSnapshot"), ("bool", "System.Boolean") })
        {
            HookSource.CheckStateTypes(StateHook(pair.Item1, pair.Item2));
            Check(true, "managed state is compared without requiring an installed game type: " + pair);
        }
        Reject(() => HookSource.CheckStateTypes(StateHook("Mod.OwnedSnapshot", "Mod.OtherSnapshot")),
            "distinct local managed state types remain incompatible");
        Reject(() => HookSource.CheckStateTypes(StateHook("bool", "int")),
            "primitive state mismatches remain incompatible");

        foreach (string name in new[] { "AttemptTooltip", "ReturnObject", "ByRefArgument", "ByRefReturn" })
            Check(SignatureSafety.HasUnsafeObjectSignature(typeof(SignatureFixtures).GetMethod(name)),
                "exact-base ABI hazard must be rejected: " + name);
        Check(!SignatureSafety.HasUnsafeObjectSignature(typeof(SignatureFixtures).GetMethod("SafeDerived")),
            "derived wrapper is distinct from exact-base bridge bug");
        Check(SignatureSafety.IsUnsafeObjectType(typeof(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase).MakePointerType()),
            "pointer variant must not bypass ABI guard");

        using var mapData = BuildMap();
        var map = NativeMethodMap.Read(mapData);
        var empty = new MethodKey("Game", 0x06000001);
        Check(map.HasAddress(empty), "fixture target has a native address");
        Check(map.Aliases(empty).ToHashSet().SetEquals(new[] { new MethodKey("Game", 0x06000002), new MethodKey("UnityEngine", 0x06000001) }),
            "shared empty bodies are rejected across types and assemblies regardless of historical method names");
        Check(!map.Aliases(new MethodKey("Game", 0x06000003)).Any(), "unique method remains safe");
        Check(!map.HasAddress(new MethodKey("Game", 0x06000004)), "zero native address is unverifiable");
        Check(!map.HasAddress(new MethodKey("Game", 0x06000009)), "missing native map entry is unverifiable");
        using var invalid = new MemoryStream(new byte[20]);
        Reject(() => NativeMethodMap.Read(invalid), "unknown map format must not pass");
        Check(InstalledInterop.GenericArguments("Vector3").SequenceEqual(new[] { "Vector3" }) &&
            InstalledInterop.GenericArguments("Dictionary<int, List<string>>, Vector3")
                .SequenceEqual(new[] { "Dictionary<int, List<string>>", "Vector3" }),
            "generic callback types split only their top-level arguments");
        return checks + InteropFingerprintTests.Run();
    }

    private static MemoryStream BuildMap()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(1297370453); writer.Write(1); writer.Write(2); writer.Write(5); writer.Write(0);
        writer.Write("Game"); writer.Write("UnityEngine");
        int offset = checked((int)stream.Position);
        foreach (long address in new long[] { 64, 64, 64, 128, 0 }) writer.Write(address);
        foreach (var entry in new[] { (0x06000001, 0), (0x06000002, 0), (0x06000001, 1), (0x06000003, 0), (0x06000004, 0) })
        { writer.Write(entry.Item1); writer.Write(entry.Item2); }
        stream.Position = 16; writer.Write(offset); stream.Position = 0;
        return stream;
    }
}

// Deterministic equivalents of the previous Idol exact-base crash; independent of game version.
internal static class SignatureFixtures
{
    private static Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase _value = new();
    public static void AttemptTooltip(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase value) { }
    public static Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase ReturnObject() => _value;
    public static void ByRefArgument(ref Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase value) { }
    public static ref Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase ByRefReturn() => ref _value;
    public static void SafeDerived(DerivedWrapper value) { }
}
internal sealed class DerivedWrapper : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase { }
namespace Il2CppInterop.Runtime.InteropTypes { internal class Il2CppObjectBase { } }
