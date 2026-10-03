namespace UnityEngine
{
    internal static class Time
    {
        internal static int frameCount;
        internal static double unscaledTimeAsDouble;
    }
}

namespace DavyKager
{
    internal static class Tolk
    {
        internal static bool Loaded, OutputAccepted = true;
        internal static string Name;
        internal static Exception LoadFailure, DetectionFailure, OutputFailure;
        internal static int Loads, Detections, Outputs, Unloads, Silences, Brailles;
        internal static readonly List<(string Text, bool Interrupt)> Messages = new();
        internal static void Reset()
        {
            Loaded = false; OutputAccepted = true; Name = null;
            LoadFailure = DetectionFailure = OutputFailure = null;
            Loads = Detections = Outputs = Unloads = Silences = Brailles = 0;
            Messages.Clear();
        }
        public static void Load()
        {
            Loads++;
            if (LoadFailure != null) throw LoadFailure;
            Loaded = true;
        }
        public static bool IsLoaded() => Loaded;
        public static string DetectScreenReader()
        {
            Detections++;
            if (DetectionFailure != null) throw DetectionFailure;
            return Loaded ? Name : null;
        }
        public static bool Output(string text, bool interrupt)
        {
            Outputs++;
            if (OutputFailure != null) throw OutputFailure;
            if (!Loaded || Name == null || !OutputAccepted) return false;
            Messages.Add((text, interrupt));
            return true;
        }
        public static void Unload() { Unloads++; Loaded = false; }
        public static bool Silence() { Silences++; return true; }
        public static bool Braille(string text) { Brailles++; return true; }
        public static bool IsSpeaking() => false;
    }
}

namespace LeAccess
{
    internal static class Plugin { internal static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Info = new(), Warnings = new(), Errors = new();
        internal void LogInfo(string text) => Info.Add(text);
        internal void LogWarning(string text) => Warnings.Add(text);
        internal void LogError(string text) => Errors.Add(text);
    }
}
