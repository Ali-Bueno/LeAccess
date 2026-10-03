namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; }
    public class ConfigFile
    {
        public ConfigEntry<bool> Enabled, Describe, Surfaces;
        public ConfigEntry<bool> Bind(string section, string key, bool value, string description)
        {
            var entry = new ConfigEntry<bool> { Value = value };
            if (key == "AnnounceSections") Enabled = entry;
            else if (key == "DescribeMapOnEntry") Describe = entry;
            else Surfaces = entry;
            return entry;
        }
    }
}
namespace UnityEngine
{
    public static class Application { public static bool isFocused = true; public static string dataPath; }
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
namespace LeAccess.Accessibility.MapSections
{
    internal readonly record struct MapSectionWorldSnapshot(long Session, int SceneHandle, string SceneName,
        string ScenePath, double X, double Y, double Z, double BodyRadius, int? Variant)
    {
        internal bool IsFixedLayout { get; init; } = true;
    }
    internal readonly record struct PcgSectionSnapshot(long Session, string SectionId, int Number,
        bool IsEntrance, bool IsExit, int? ConnectedPassages, string KindKey);
    internal static class PcgMapSections
    {
        internal static bool Available;
        internal static PcgSectionSnapshot Snapshot;
        internal static bool TryCapture(MapSectionWorldSnapshot world, out PcgSectionSnapshot snapshot)
        { snapshot = Snapshot; return Available; }
        internal static void Reset() { }
    }
    internal static class MapSectionSources
    {
        internal static bool Available = true;
        internal static bool IsAvailable(MapSection section, MapSectionWorldSnapshot world) => Available;
        internal static void Reset() { }
    }
    internal static class MapSurfaceProbe
    {
        internal static bool Available;
        internal static string Key = "map.surface.wood";
        internal static bool TryRead(MapSectionWorldSnapshot world, out string key)
        { key = Key; return Available; }
        internal static void Reset() { }
    }
    internal readonly record struct RuntimeMapPlace(string Identity, string TextKey, string SpeechGroup);
    internal static class MapPlaceDiagnostics
    {
        internal static void Report(string stage, string detail) { }
    }
    internal static class RuntimeSceneryPlaces
    {
        internal static void Initialize(string buildHash) { }
        internal static bool HasValidObservation = true;
        internal static bool Available;
        internal static int Captures, Resets;
        internal static RuntimeMapPlace Place = new("gate-owner", "map.place.gate", "map.place.gate");
        internal static bool TryRead(MapSectionWorldSnapshot world, out RuntimeMapPlace place)
        { Captures++; place = Place; return Available; }
        internal static void Reset() { Resets++; }
    }
    internal static class RuntimeMapPlaces
    {
        internal static bool Available;
        internal static int Captures;
        internal static RuntimeMapPlace Place = new("bridge-owner", "map.section.bridge", "map.section.bridge");
        internal static bool TryRead(MapSectionWorldSnapshot world, out RuntimeMapPlace place)
        { Captures++; place = Place; return Available; }
        internal static void Reset() { }
    }
    internal static class MapSectionWorld
    {
        internal static bool Available = true, AnchorsValid = true, Throw;
        internal static int Captures, Validations, Resets;
        internal static MapSectionWorldSnapshot Snapshot = new(1,1,"ControlledTown","Assets/ControlledTown.unity",2,5,2,.5,0);
        internal static bool TryCapturePlayer(out MapSectionWorldSnapshot snapshot)
        {
            Captures++;
            if (Throw) throw new InvalidOperationException("World capture failed");
            snapshot = Snapshot; return Available;
        }
        internal static bool ValidateAnchors(SectionMap map, MapSectionWorldSnapshot snapshot)
        {
            Validations++;
            return AnchorsValid && snapshot.ScenePath == map.ScenePath;
        }
        internal static void Reset() { Resets++; }
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
