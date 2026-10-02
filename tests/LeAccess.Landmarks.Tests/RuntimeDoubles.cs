namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; }
    public class ConfigFile
    {
        public ConfigEntry<bool> Enabled;
        public ConfigEntry<bool> Bind(string section, string key, bool value, string description) =>
            Enabled = new ConfigEntry<bool> { Value = value };
    }
}
namespace UnityEngine
{
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static double unscaledTimeAsDouble; }
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
namespace LeAccess.Accessibility.Landmarks
{
    internal readonly record struct LandmarkTarget(long Session, int InstanceId, string Name);
    internal static class LandmarkProbe
    {
        internal static string LastStatus;
        internal static string LastFailure;
        internal static bool Found = true, Throw;
        internal static int Reads;
        internal static LandmarkTarget Target = new(1,10,"The Keeper's Bridge");
        internal static bool TryRead(out LandmarkTarget target)
        {
            Reads++;
            if (Throw) throw new InvalidOperationException("Target read failure");
            target = Target;
            return Found;
        }
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true, SpokeThisFrame;
        internal static readonly List<(string Text, bool Interrupt)> Spoken = new();
        internal static bool IsAvailable() => Available;
        internal static void Speak(string text, bool interrupt) { Spoken.Add((text,interrupt)); SpokeThisFrame = true; }
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
