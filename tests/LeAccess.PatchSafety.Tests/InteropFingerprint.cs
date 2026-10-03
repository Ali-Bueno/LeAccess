using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

internal static class InteropFingerprint
{
    private const int LoaderHashBufferBytes = 81920;
    internal static void Verify(string gameDirectory)
    {
        string bepinex = Path.Combine(gameDirectory, "BepInEx");
        string marker = Path.Combine(bepinex, "interop", "assembly-hash.txt");
        if (!File.Exists(marker))
            throw new InvalidOperationException("Installed interop has no assembly-hash.txt; generate fresh interop for the current game before auditing hooks.");
        string current = Compute(Path.Combine(gameDirectory, "GameAssembly.dll"),
            Path.Combine(bepinex, "unity-libs"), Path.Combine(bepinex, "DeobfuscationMap.csv.gz"),
            ReadVersion(Path.Combine(bepinex, "core", "Il2CppInterop.Generator.dll")),
            ReadVersion(Path.Combine(bepinex, "core", "Cpp2IL.Core.dll")));
        string recorded = File.ReadAllText(marker);
        if (recorded != current)
            throw new InvalidOperationException($"Installed interop is stale for the current game/loader inputs: assembly-hash.txt={recorded}, current={current}. Generate fresh interop for the installed game and rerun this audit; the old DLLs and MethodAddressToToken.db cannot establish native hook compatibility.");
    }

    // Match installed BepInEx Il2CppInteropManager.ComputeHash, including filesystem enumeration order.
    internal static string Compute(string gameAssembly, string unityLibraries, string renameMap,
        string generatorVersion, string cpp2IlVersion)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        AppendFile(gameAssembly);
        if (Directory.Exists(unityLibraries))
            foreach (string library in Directory.EnumerateFiles(unityLibraries, "*.dll", SearchOption.TopDirectoryOnly))
            {
                hash.AppendData(Encoding.UTF8.GetBytes(Path.GetFileName(library)));
                AppendFile(library);
            }
        if (File.Exists(renameMap)) AppendFile(renameMap);
        hash.AppendData(Encoding.UTF8.GetBytes(generatorVersion));
        hash.AppendData(Encoding.UTF8.GetBytes(cpp2IlVersion));
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();

        void AppendFile(string path)
        {
            using var stream = File.OpenRead(path);
            byte[] buffer = new byte[LoaderHashBufferBytes];
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                hash.AppendData(buffer, 0, count);
        }
    }

    private static string ReadVersion(string path)
    {
        using var stream = File.OpenRead(path);
        using var image = new PEReader(stream);
        return image.GetMetadataReader().GetAssemblyDefinition().Version.ToString();
    }
}
