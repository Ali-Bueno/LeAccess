namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; }
    public class ConfigDescription { public ConfigDescription(string description, object acceptable = null) { } }
    public class AcceptableValueList<T> { public AcceptableValueList(params T[] values) { } }
    public class AcceptableValueRange<T> { public AcceptableValueRange(T minimum, T maximum) { } }
    public class ConfigFile
    {
        private readonly Dictionary<string, object> entries = new();
        public ConfigEntry<T> Bind<T>(string section, string key, T value, string description)
        {
            var entry = new ConfigEntry<T> { Value = value }; entries[key] = entry; return entry;
        }
        public ConfigEntry<T> Bind<T>(string section, string key, T value, ConfigDescription description) =>
            Bind(section, key, value, (string)null);
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
    internal static class LowLifeCue
    {
        internal static bool Wanted;
        internal static readonly List<int> Readings = new();
        internal static void Initialize(BepInEx.Configuration.ConfigFile config) { }
        internal static void Update(int healthPercent) => Readings.Add(healthPercent);
        internal static void Cleanup() { }
    }
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
        internal static bool Gold;
        internal static bool GoldPressed() => Gold;
        internal static void Reset() { KeyboardPressed = ControllerPressed = Gold = false; }
    }
    internal static class GoldReport
    {
        internal static int? Amount = 1234;
        internal static bool TryRead(out int gold) { gold = Amount ?? 0; return Amount.HasValue; }
    }
}
namespace LeAccess.Accessibility.Progression
{
    internal static class ProgressionProbe
    {
        internal static bool Available;
        internal static ProgressionSnapshot Snapshot = new(1, 12, 100, 40, 100, 0, new List<SkillPoint>(), false, null);
        internal static bool TryRead(out ProgressionSnapshot snapshot) { snapshot = Snapshot; return Available; }
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
        internal static bool TrySpeakPickup(string text) => TrySpeak(text, false);
        internal static bool TrySpeakOverPickup(string text, bool interrupt) => TrySpeak(text, interrupt);
        internal static readonly List<string> LifeLines = new();
        internal static bool TrySpeakLife(string text)
        {
            if (!TrySpeak(text, true)) return false;
            LifeLines.Add(text);
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
