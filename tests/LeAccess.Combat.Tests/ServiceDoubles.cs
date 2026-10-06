namespace BepInEx.Configuration
{
    public sealed class ConfigEntry<T> { public T Value { get; set; } }
    public sealed class ConfigFile
    {
        public readonly Dictionary<string, object> Entries = new();
        public ConfigEntry<T> Bind<T>(string section, string key, T value, string description)
        {
            if (Entries.TryGetValue(key, out var existing)) return (ConfigEntry<T>)existing;
            var entry = new ConfigEntry<T> { Value = value }; Entries.Add(key, entry); return entry;
        }
        public void Set<T>(string key, T value) => ((ConfigEntry<T>)Entries[key]).Value = value;
    }
}
namespace EHG.Multiplayer { internal static class GameplayEnvironment { public static bool IsOnlineClient; } }
namespace UnityEngine { internal static class Time { public static double unscaledTimeAsDouble; } }
namespace UnityEngine.SceneManagement
{
    internal struct Scene
    {
        public int handle;
        public string name;
        public bool Unloaded;
        public bool isLoaded => !Unloaded;
        public bool IsValid() => handle > 0;
    }
    internal static class SceneManager
    {
        public static Scene Current = new() { handle = 1, name = "Test area" };
        public static Scene GetActiveScene() => Current;
    }
}
internal static class PlayerFinder
{
    public static UnityEngine.GameObject player;
    public static Actor playerActorComponent;
    public static ActorVisuals playerActorVisuals;
}
internal static class SceneList
{
    public static bool NonCombat;
    public static bool IsNonCombatZone(string name) => NonCombat;
}
internal static class ControllerTargetingParadigm { public static float _SkillTargetMaxDistance = 10; }
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public readonly List<string> Messages = new();
        public void LogInfo(object value) => Messages.Add(value.ToString());
        public void LogWarning(object value) => Messages.Add(value.ToString());
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        public static bool Available = true, SpokeThisFrame, Speaking;
        public static readonly List<(string Text, bool Interrupt)> Messages = new();
        public static bool IsAvailable() => Available;
        public static bool IsSpeaking() => Speaking;
        public static void Speak(string text, bool interrupt) { Messages.Add((text, interrupt)); SpokeThisFrame = true; }
    }
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState
    {
        public static string SuspensionReason;
        public static bool IsSuspended => SuspensionReason is not null;
    }
    internal static class GameplayInputState { public static bool Owns = true; public static bool OwnsInput() => Owns; }
}
namespace LeAccess.Accessibility.ScreenContent { internal static class ScreenContentAnnouncer { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue { internal static class DialogueAnnouncer { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Inventory { internal static class InventoryAccessibility { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Items { internal static class GroundLootAccessibility { public static bool HasPendingContent; } }
namespace LeAccess.Accessibility.ControllerMenus { internal static class ControllerMenuAccessibility { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Zones { internal static class ZoneAnnouncer { public static bool HasPendingContent; } }
namespace LeAccess.Accessibility.Npcs { internal static class NpcSounds { public static bool HasPendingName; } }
namespace LeAccess.Accessibility.Skills { internal static class SkillAccessibility { public static bool HasActiveContent; } }
