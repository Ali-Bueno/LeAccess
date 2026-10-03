using System.Security.Cryptography;
using System.Text;

internal static class InteropFingerprintTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string label)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("Interop fingerprint regression: " + label);
        }
        void Reject(Action action, string label)
        {
            checks++;
            try { action(); }
            catch (InvalidOperationException) { return; }
            catch (IOException) { return; }
            throw new InvalidOperationException("Interop fingerprint regression: " + label);
        }
        string scratch = Path.Combine(Path.GetTempPath(), "LeAccess-interop-fingerprint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            string native = Path.Combine(scratch, "GameAssembly.dll");
            string bepinex = Path.Combine(scratch, "BepInEx");
            string libraries = Path.Combine(bepinex, "unity-libs");
            string rename = Path.Combine(bepinex, "DeobfuscationMap.csv.gz");
            Directory.CreateDirectory(libraries);
            File.WriteAllText(native, "native", Encoding.UTF8);
            string Compute(string generator = "1.5.3.0", string cpp2Il = "2022.1.0.0") =>
                InteropFingerprint.Compute(native, libraries, rename, generator, cpp2Il);
            string baseline = Compute();
            var input = File.ReadAllBytes(native).Concat(Encoding.UTF8.GetBytes("1.5.3.02022.1.0.0")).ToArray();
            Check(baseline == Convert.ToHexString(MD5.HashData(input)).ToLowerInvariant(),
                "no-library fixture matches the loader's byte sequence and lowercase MD5 encoding");
            File.WriteAllText(native, "changed", Encoding.UTF8);
            Check(Compute() != baseline, "native content changes invalidate old interop");
            File.WriteAllText(native, "native", Encoding.UTF8);
            Check(Compute("1.5.4.0") != baseline && Compute(cpp2Il: "2022.2.0.0") != baseline,
                "each generator version invalidates old interop");
            string library = Path.Combine(libraries, "UnityEngine.CoreModule.dll");
            File.WriteAllText(library, "library");
            string libraryHash = Compute();
            Check(libraryHash != baseline, "Unity libraries participate in the loader fingerprint");
            File.WriteAllText(library, "changed");
            Check(Compute() != libraryHash, "Unity library content changes invalidate old interop");
            File.WriteAllText(library, "library");
            File.Move(library, Path.Combine(libraries, "Renamed.dll"));
            Check(Compute() != libraryHash, "Unity library filenames participate in the loader fingerprint");
            string renamedLibraryHash = Compute();
            File.WriteAllText(Path.Combine(libraries, "ignored.txt"), "ignored");
            Check(Compute() == renamedLibraryHash, "non-DLL files do not affect the loader fingerprint");
            string beforeRename = Compute();
            File.WriteAllText(rename, "rename mapping");
            Check(Compute() != beforeRename, "optional deobfuscation map participates in the loader fingerprint");
            File.Delete(rename);
            Check(Compute() == beforeRename, "removing the optional map restores the corresponding fingerprint");
            File.Delete(native);
            Reject(() => Compute(), "missing native assembly cannot be audited");
            File.WriteAllText(native, "native", Encoding.UTF8);
            Reject(() => InteropFingerprint.Verify(scratch), "missing interop fingerprint cannot pass");
            string interop = Path.Combine(bepinex, "interop");
            Directory.CreateDirectory(interop);
            string marker = Path.Combine(interop, "assembly-hash.txt");
            File.WriteAllText(marker, baseline);
            Reject(() => InteropFingerprint.Verify(scratch), "missing generator assemblies cannot pass");
            string core = Path.Combine(bepinex, "core");
            Directory.CreateDirectory(core);
            string sourceAssembly = typeof(InteropFingerprintTests).Assembly.Location;
            File.Copy(sourceAssembly, Path.Combine(core, "Il2CppInterop.Generator.dll"));
            File.Copy(sourceAssembly, Path.Combine(core, "Cpp2IL.Core.dll"));
            string version = typeof(InteropFingerprintTests).Assembly.GetName().Version!.ToString();
            string current = Compute(version, version);
            File.WriteAllText(marker, current);
            InteropFingerprint.Verify(scratch);
            Check(true, "matching installed fingerprint allows signature audit");
            File.Delete(Path.Combine(core, "Cpp2IL.Core.dll"));
            Reject(() => InteropFingerprint.Verify(scratch), "missing Cpp2IL version input cannot pass");
            File.Copy(sourceAssembly, Path.Combine(core, "Cpp2IL.Core.dll"));
            File.WriteAllText(native, "different current native binary", Encoding.UTF8);
            Reject(() => InteropFingerprint.Verify(scratch), "stale fingerprint rejects signature-only false success");
        }
        finally { Directory.Delete(scratch, recursive: true); }
        return checks;
    }
}
