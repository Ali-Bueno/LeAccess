namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; }
    public class ConfigFile
    {
        private readonly Dictionary<string, object> entries = new();
        public ConfigEntry<T> Bind<T>(string section, string key, T value, string description)
        {
            var entry = new ConfigEntry<T> { Value = value }; entries[key] = entry; return entry;
        }
        public void Set<T>(string key, T value) => ((ConfigEntry<T>)entries[key]).Value = value;
        public T Get<T>(string key) => ((ConfigEntry<T>)entries[key]).Value;
    }
}
namespace UnityEngine
{
    public class Object { public bool WasCollected; }
    public class Behaviour : Object { }
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static double unscaledTimeAsDouble; }
    public enum KeyCode { None, R }
}
public class EpochInputManager : UnityEngine.Behaviour
{
    public static EpochInputManager instance = new();
    public bool isInputFieldActive;
}
public class ScreenKeyboardManager : UnityEngine.Behaviour
{
    public static ScreenKeyboardManager Instance = new();
    public bool _enabled;
}
namespace LE.UI { public static class LoadingScreen { public static bool IsVisible; } }
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { internal static bool IsSuspended; }
    internal static class GameplayInputState
    {
        internal static bool Gameplay = true;
        internal static bool OwnsInput() => Gameplay;
    }
}
namespace LeAccess.Accessibility.Vitals
{
    internal readonly record struct VitalsSnapshot(long Session, double Health, double MaxHealth,
        double Mana, double MaxMana, bool ManaReplaced, int Potions = -1, int MaxPotions = -1);
    internal static class VitalsEffectsProbe
    {
        internal static readonly List<string> Effects = new();
        internal static string LastFailure = null;
        internal static List<string> Read() => new(Effects);
    }
    internal static class VitalsProbe
    {
        internal static bool Available = true, Throw;
        internal static int Reads;
        internal static VitalsSnapshot Snapshot = new(1,100,100,100,100,false);
        internal static bool TryRead(out VitalsSnapshot snapshot)
        {
            Reads++;
            if (Throw) throw new InvalidOperationException("Probe failure");
            snapshot = Snapshot; return Available;
        }
    }
    internal static class VitalsInput
    {
        internal static bool KeyboardPressed, ControllerPressed;
        internal static int Calls;
        internal static bool ReportPressed(UnityEngine.KeyCode key, bool controller)
        {
            Calls++;
            return key != UnityEngine.KeyCode.None && KeyboardPressed || controller && ControllerPressed;
        }
        internal static void Reset() { KeyboardPressed = ControllerPressed = false; }
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true, SpokeThisFrame;
        internal static bool OutputAccepted = true;
        internal static readonly List<(string Text, bool Interrupt)> Spoken = new();
        internal static bool IsAvailable() => Available;
        internal static void Speak(string text, bool interrupt)
            => TrySpeak(text, interrupt);
        internal static bool TrySpeak(string text, bool interrupt = true)
        {
            if (!Available || !OutputAccepted) return false;
            Spoken.Add((text,interrupt)); SpokeThisFrame = true;
            return true;
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static TestLog Log = new(); }
    internal class TestLog
    {
        internal readonly List<string> Warnings = new(), Information = new();
        internal void LogWarning(string text) => Warnings.Add(text);
        internal void LogInfo(string text) => Information.Add(text);
    }
}
