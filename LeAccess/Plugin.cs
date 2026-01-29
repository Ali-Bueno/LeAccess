using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LeAccess.Accessibility;

namespace LeAccess;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    private Harmony _harmony;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"LeAccess v{MyPluginInfo.PLUGIN_VERSION} loading...");

        // Initialize screen reader
        if (!ScreenReader.Initialize())
        {
            Log.LogWarning("No screen reader detected. TTS features will be disabled.");
        }
        else
        {
            Log.LogInfo($"Screen reader initialized: {ScreenReader.DetectedScreenReader}");
            ScreenReader.Speak("Last Epoch Accessibility mod loaded");
        }

        // Apply Harmony patches
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Log.LogInfo("Harmony patches applied");

        // Register UI component for menu accessibility
        AddComponent<MenuAccessibility>();

        Log.LogInfo("LeAccess loaded successfully!");
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        ScreenReader.Shutdown();
        return base.Unload();
    }
}
