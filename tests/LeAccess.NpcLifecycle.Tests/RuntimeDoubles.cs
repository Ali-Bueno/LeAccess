namespace BepInEx.Configuration
{
    internal sealed class ConfigEntry<T> { internal T Value; }
    internal sealed class ConfigFile
    {
        private readonly Dictionary<string, object> _entries = new();
        internal ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, object description)
        {
            var entry = new ConfigEntry<T> { Value = defaultValue };
            _entries[$"{section}/{key}"] = entry;
            return entry;
        }
        internal void Set<T>(string key, T value) => ((ConfigEntry<T>)_entries[$"NPC sounds/{key}"]).Value = value;
    }
    internal sealed class ConfigDescription
    { internal ConfigDescription(string text, object acceptableValues) { } }
    internal sealed class AcceptableValueRange<T>
    { internal AcceptableValueRange(T minimum, T maximum) { } }
}

namespace UnityEngine
{
    internal static class Time { internal static double unscaledTimeAsDouble; }
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { internal static bool IsSuspended; }
    internal static class GameplayInputState
    {
        internal static bool Owns = true;
        internal static bool OwnsInput() => Owns;
    }
}
namespace LeAccess.Accessibility.ScreenContent
{ internal static class ScreenContentAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue
{ internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Npcs
{
    internal readonly record struct NpcCueTarget(long Session, int InstanceId, string Name,
        float Distance, float Range, float Pan, float Forward, string NameSource = null);
    internal sealed class NpcWorldProbe
    {
        internal static NpcCueTarget Target;
        internal static bool Available;
        internal static bool Throw;
        internal static string CaptureFailure;
        internal string LastFailure { get; private set; }
        internal static int Captures;
        internal static int Resets;
        internal static float LastMultiplier;
        internal bool TryCapture(float multiplier, out NpcCueTarget target)
        {
            Captures++;
            LastMultiplier = multiplier;
            LastFailure = CaptureFailure;
            if (Throw) throw new InvalidOperationException("Probe failed");
            target = Target;
            return Available;
        }
        internal void Reset() { Resets++; LastFailure = null; }
        internal static void Clear()
        {
            Target = default;
            Available = Throw = false;
            CaptureFailure = null;
            Captures = Resets = 0;
            LastMultiplier = 0;
        }
    }
}
namespace LeAccess.Accessibility.Audio
{
    internal static class NpcCueSynthesizer
    {
        internal const double DurationSeconds = 0.084;
        internal static readonly List<(float Pan, float Forward, float Volume)> Calls = new();
        internal static float[] Synthesize(float pan, float forward, float volume)
        {
            Calls.Add((pan, forward, volume));
            return new[] { pan, forward, volume, 0 };
        }
    }
    internal sealed class RadarAudioOutput : IDisposable
    {
        internal static readonly List<RadarAudioOutput> Instances = new();
        internal static int ConstructorCalls;
        internal static bool ThrowConstructor;
        internal bool IsRunning = true;
        internal bool IsPlaying;
        internal bool RecoverySucceeds = true;
        internal bool ThrowRecovery;
        internal bool ThrowPlay;
        internal bool Disposed;
        internal int RecoveryCalls;
        internal int SilenceCalls;
        internal int PlayCalls;
        internal int DisposeCalls;
        internal string Description;
        internal readonly List<float[]> Cues = new();
        internal RadarAudioOutput(string description, int playbackLatencyMilliseconds = 50)
        {
            ConstructorCalls++;
            if (ThrowConstructor) throw new InvalidOperationException("Output creation failed");
            Description = description;
            Instances.Add(this);
        }
        internal bool TryRecover(out string status)
        {
            RecoveryCalls++;
            if (ThrowRecovery) throw new InvalidOperationException("Recovery failed");
            status = RecoverySucceeds ? "NPC output recovered" : "NPC output waiting";
            if (RecoverySucceeds) { IsRunning = true; IsPlaying = false; }
            return RecoverySucceeds;
        }
        internal void Play(float[] samples)
        {
            PlayCalls++;
            if (ThrowPlay) throw new InvalidOperationException("Playback submission failed");
            if (IsPlaying) throw new InvalidOperationException("Overlapping cue");
            Cues.Add(samples);
            IsPlaying = true;
        }
        internal void Silence() { SilenceCalls++; IsPlaying = false; }
        public void Dispose() { DisposeCalls++; Disposed = true; IsRunning = IsPlaying = false; }
        internal static void Clear()
        { Instances.Clear(); ConstructorCalls = 0; ThrowConstructor = false; }
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Info = new();
        internal readonly List<string> Warnings = new();
        internal void LogInfo(string text) => Info.Add(text);
        internal void LogWarning(string text) => Warnings.Add(text);
    }
}
namespace LeAccess.Accessibility.Inventory
{ internal static class InventoryAccessibility { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Items
{ internal static class GroundLootAccessibility { internal static bool HasPendingContent; } }
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true;
        internal static bool SpokeThisFrame;
        internal static readonly List<(string Text, bool Interrupt)> Messages = new();
        internal static bool IsAvailable() => Available;
        internal static bool IsSpeaking() => throw new InvalidOperationException("Unreliable backend status must not gate NPC names");
        internal static void Speak(string text, bool interrupt)
        { Messages.Add((text, interrupt)); SpokeThisFrame = true; }
    }
}
