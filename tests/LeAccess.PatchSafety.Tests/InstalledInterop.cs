using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

internal sealed class InstalledInterop : IDisposable
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
        BindingFlags.Static | BindingFlags.DeclaredOnly;
    private readonly MetadataLoadContext _context;
    private readonly Assembly[] _assemblies;
    internal NativeMethodMap NativeMethods { get; }

    internal InstalledInterop(string gameDirectory)
    {
        InteropFingerprint.Verify(gameDirectory);
        string interop = Path.Combine(gameDirectory, "BepInEx", "interop");
        string core = Path.Combine(gameDirectory, "BepInEx", "core");
        if (!Directory.Exists(interop) || !Directory.Exists(core))
            throw new DirectoryNotFoundException($"Fresh BepInEx interop/core directories are required under {gameDirectory}.");
        var paths = new[] { RuntimeEnvironment.GetRuntimeDirectory(), interop, core }
            .SelectMany(directory => Directory.GetFiles(directory, "*.dll")).ToArray();
        _context = new MetadataLoadContext(new PathAssemblyResolver(paths));
        _assemblies = Directory.GetFiles(interop, "*.dll").Select(_context.LoadFromAssemblyPath)
            .Append(_context.CoreAssembly).Distinct().ToArray();
        using var stream = File.OpenRead(Path.Combine(interop, "MethodAddressToToken.db"));
        NativeMethods = NativeMethodMap.Read(stream);
    }

    internal MethodInfo Resolve(Hook hook)
    {
        var type = ResolveType(hook.TypeName, hook);
        var methods = type.GetMethods(Declared).Where(method => method.Name == hook.MethodName).ToArray();
        if (hook.ParameterTypes != null)
        {
            var expected = hook.ParameterTypes.Select(name => ResolveType(name, hook)).ToArray();
            methods = methods.Where(method => method.GetParameters().Select(parameter => parameter.ParameterType)
                .SequenceEqual(expected)).ToArray();
        }
        if (methods.Length != 1)
            throw new InvalidOperationException($"{hook.TypeName}.{hook.MethodName} has {methods.Length} matching installed signatures.");
        return methods[0];
    }

    internal void CheckCallbacks(Hook hook, MethodInfo target)
    {
        HookSource.CheckStateTypes(hook);
        foreach (var parameter in hook.CallbackParameters)
        {
            // Harmony keeps __state between managed callbacks; it is not a native argument.
            if (parameter.Name == "__state") continue;
            var callbackType = ResolveType(parameter.Type, hook);
            Type originalType;
            if (parameter.Name == "__instance")
            {
                if (target.IsStatic) throw new InvalidOperationException("Static target cannot supply __instance.");
                originalType = target.DeclaringType;
            }
            else if (parameter.Name == "__result") originalType = target.ReturnType;
            else
            {
                var argument = target.GetParameters().SingleOrDefault(candidate => candidate.Name == parameter.Name);
                if (argument == null) throw new InvalidOperationException($"Callback argument '{parameter.Name}' does not exist in {Describe(target)}.");
                originalType = argument.ParameterType;
                if (originalType.IsByRef) originalType = originalType.GetElementType();
            }
            if (!callbackType.IsAssignableFrom(originalType))
                throw new InvalidOperationException($"Callback '{parameter.Type} {parameter.Name}' cannot receive {originalType.FullName}.");
        }
    }

    private Type ResolveType(string name, Hook hook)
    {
        name = name.Replace("global::", "");
        if (hook.Aliases.TryGetValue(name, out string alias)) name = alias;
        // A constructed generic such as IReadOnlyList<Vector3>: resolve the definition and each argument.
        int open = name.IndexOf('<');
        if (open > 0 && name.EndsWith(">", StringComparison.Ordinal))
        {
            var arguments = GenericArguments(name[(open + 1)..^1]).Select(argument => ResolveType(argument, hook)).ToArray();
            return ResolveType($"{name[..open]}`{arguments.Length}", hook).MakeGenericType(arguments);
        }
        name = name switch
        {
            "string" => "System.String", "float" => "System.Single", "bool" => "System.Boolean",
            "int" => "System.Int32", "void" => "System.Void", "object" => "System.Object", _ => name
        };
        string[] candidates = hook.Namespaces.Select(ns => ns + "." + name).Prepend(name).ToArray();
        var matches = _assemblies.SelectMany(assembly => candidates.Select(candidate => FindType(assembly, candidate)))
            .Where(type => type != null).Distinct().ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"Type '{name}' resolves to {matches.Length} installed types.");
        return matches[0];
    }

    // Top-level comma-separated arguments of a generic name, nested generics kept whole.
    internal static IEnumerable<string> GenericArguments(string arguments)
    {
        int depth = 0, start = 0;
        for (int index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] == '<') depth++;
            else if (arguments[index] == '>') depth--;
            else if (arguments[index] == ',' && depth == 0)
            {
                yield return arguments[start..index].Trim();
                start = index + 1;
            }
        }
        yield return arguments[start..].Trim();
    }

    private static Type FindType(Assembly assembly, string name)
    {
        var type = assembly.GetType(name);
        if (type != null) return type;
        for (int dot = name.LastIndexOf('.'); dot >= 0; dot = name.LastIndexOf('.', dot - 1))
        {
            name = name[..dot] + "+" + name[(dot + 1)..];
            type = assembly.GetType(name);
            if (type != null) return type;
            if (dot == 0) break;
        }
        return null;
    }

    internal string Describe(MethodKey key)
    {
        var assembly = _assemblies.SingleOrDefault(candidate => candidate.GetName().Name == key.Assembly);
        if (assembly == null) return key.ToString();
        using var stream = File.OpenRead(assembly.Location);
        using var image = new PEReader(stream);
        var metadata = image.GetMetadataReader();
        var method = metadata.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.Handle(key.Token));
        var type = metadata.GetTypeDefinition(method.GetDeclaringType());
        return $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}.{metadata.GetString(method.Name)} [{key.Assembly}:0x{key.Token:X8}]";
    }

    internal static string Describe(MethodInfo method) => $"{method.DeclaringType.FullName}.{method.Name}({string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})";
    public void Dispose() => _context.Dispose();
}

internal readonly record struct MethodKey(string Assembly, int Token)
{
    internal static MethodKey From(MethodInfo method) => new(method.Module.Assembly.GetName().Name, method.MetadataToken);
}

internal sealed class NativeMethodMap
{
    private readonly Dictionary<MethodKey, HashSet<long>> _addresses = new();
    private readonly Dictionary<long, HashSet<MethodKey>> _methods = new();

    internal static NativeMethodMap Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        // Il2CppInterop.Common.Maps.MethodAddressToTokenMapFileHeader, version 1.
        if (reader.ReadInt32() != 1297370453 || reader.ReadInt32() != 1)
            throw new InvalidDataException("Unsupported native method map format; update the offline audit before patching.");
        int assemblyCount = reader.ReadInt32(), methodCount = reader.ReadInt32(), offset = reader.ReadInt32();
        if (assemblyCount <= 0 || methodCount <= 0 || offset < stream.Position ||
            offset + (long)methodCount * 16 > stream.Length)
            throw new InvalidDataException("Invalid or incomplete native method map.");
        var assemblies = Enumerable.Range(0, assemblyCount).Select(_ => new AssemblyName(reader.ReadString()).Name).ToArray();
        if (stream.Position > offset) throw new InvalidDataException("Native method map assembly table overlaps method data.");
        stream.Position = offset;
        var addresses = Enumerable.Range(0, methodCount).Select(_ => reader.ReadInt64()).ToArray();
        var result = new NativeMethodMap();
        for (int index = 0; index < methodCount; index++)
        {
            int token = reader.ReadInt32(), assembly = reader.ReadInt32();
            if (assembly < 0 || assembly >= assemblies.Length) throw new InvalidDataException("Invalid native method assembly index.");
            var key = new MethodKey(assemblies[assembly], token);
            if (!result._addresses.TryGetValue(key, out var locations)) result._addresses.Add(key, locations = new());
            locations.Add(addresses[index]);
            if (!result._methods.TryGetValue(addresses[index], out var methods)) result._methods.Add(addresses[index], methods = new());
            methods.Add(key);
        }
        return result;
    }

    internal bool HasAddress(MethodKey method) => _addresses.TryGetValue(method, out var addresses) && addresses.All(address => address != 0);
    internal IEnumerable<MethodKey> Aliases(MethodKey method) => _addresses.TryGetValue(method, out var addresses)
        ? addresses.SelectMany(address => _methods[address]).Where(candidate => candidate != method).Distinct()
        : Enumerable.Empty<MethodKey>();
}

internal static class SignatureSafety
{
    internal static bool HasUnsafeObjectSignature(MethodInfo method) => IsUnsafeObjectType(method.ReturnType) ||
        method.GetParameters().Any(parameter => IsUnsafeObjectType(parameter.ParameterType));

    internal static bool IsUnsafeObjectType(Type type)
    {
        while (type.IsByRef || type.IsPointer) type = type.GetElementType();
        return type.FullName == "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase";
    }
}
