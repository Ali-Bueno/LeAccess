using BepInEx.Preloader.Core.Patching;

namespace LeAccess.InteropRepair;

[PatcherPluginInfo("LeAccess.InteropRepair", "LeAccess interop repair", "1.0.0")]
public sealed class InteropRepairPatcher : BasePatcher
{
    public InteropRepairPatcher()
    {
        // Constructor runs after generation but before Cecil opens the files.
        // Initialize is too late: its read handles would prevent atomic replacement.
        string directory = Environment.GetEnvironmentVariable("IL2CPP_INTEROP_DATABASES_LOCATION");
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            Log.LogError("Generated interop directory unavailable; cannot check Unity metadata.");
            return;
        }

        int files = 0, rows = 0;
        foreach (string path in Directory.EnumerateFiles(directory, "UnityEngine*.dll"))
        {
            try
            {
                int repaired = OrphanTypeRepair.RepairFile(path);
                if (repaired == 0) continue;
                files++;
                rows += repaired;
                Log.LogInfo($"Repaired {repaired} orphan type rows in {Path.GetFileName(path)}; tokens preserved.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Cannot repair {Path.GetFileName(path)}: {ex}");
            }
        }
        Log.LogInfo($"Interop check complete: {rows} repaired rows in {files} assemblies.");
    }
}
