internal class NativeObject
{
    internal bool WasCollected;
    internal T TryCast<T>() where T : class => this as T;
}
namespace UnityEngine
{
    internal class Object : NativeObject
    {
        internal bool Destroyed = false;
        public static bool operator ==(Object a, Object b)
        {
            if ((a?.WasCollected ?? false) || (b?.WasCollected ?? false)) throw new Exception("Collected native object");
            return ReferenceEquals(a?.Destroyed == true ? null : a, b?.Destroyed == true ? null : b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => base.GetHashCode();
    }
    internal struct Vector2 { internal float x, y; internal Vector2(float x, float y) { this.x = x; this.y = y; } }
    internal struct Vector3
    {
        internal float x, y, z;
        internal Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        internal float sqrMagnitude => x * x + y * y + z * z;
        internal static float Distance(Vector3 a, Vector3 b) => MathF.Sqrt((a - b).sqrMagnitude);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    internal sealed class Transform { internal Vector3 forward = new(0, 0, 1), right = new(1, 0, 0); }
    internal sealed class Camera : Object
    { internal static Camera main = new(); internal bool isActiveAndEnabled = true; internal Transform transform = new(); }
    internal static class Application { internal static bool isFocused = true; }
    internal static class Time { internal static double unscaledTimeAsDouble = 0; internal static float timeScale = 1; }
    internal enum KeyCode { None, F6, LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift }
    internal static class Input
    {
        internal static readonly HashSet<KeyCode> Down = new(), Held = new();
        internal static bool Throw;
        internal static bool GetKey(KeyCode key)
        { if (Throw) throw new Exception("Input read failed"); return Held.Contains(key); }
        internal static bool GetKeyDown(KeyCode key) => Down.Contains(key);
    }
}
namespace UnityEngine.AI
{
    internal sealed class NativeCorners : NativeObject
    {
        internal Vector3[] Values = Array.Empty<Vector3>();
        internal int Length => Values.Length;
        internal Vector3 this[int i] => Values[i];
    }
    internal enum NavMeshPathStatus { PathComplete, PathPartial, PathInvalid }
    internal sealed class NavMeshPath
    {
        internal NavMeshPathStatus status;
        internal NativeCorners corners;
        internal int Clears;
        internal void ClearCorners() { Clears++; corners = null; }
    }
    internal sealed class NavMeshAgent { internal float radius = 0.1f; internal int agentTypeID, areaMask = -1; }
    internal struct NavMeshBuildSettings { internal float agentRadius; internal int agentTypeID; }
    internal struct NavMeshHit { internal Vector3 position; }
    internal static class NavMesh
    {
        internal static int SettingsCount = 1, SettingsType, Samples, Rays, Calculations;
        internal static float BakedRadius = 0.5f;
        internal static bool OriginAvailable = true, GoalAvailable = true, Blocked, PathAvailable = true;
        internal static Func<Vector3, Vector3, bool> RayBlocked;
        internal static bool CollectedCorners, NullCorners, PreserveCorners;
        internal static Vector3 OriginOffset, GoalOffset;
        internal static Vector3[] Corners;
        internal static NavMeshPathStatus Status = NavMeshPathStatus.PathComplete;
        internal static readonly List<(float Radius, int Type, int Mask)> Filters = new();
        internal static NavMeshBuildSettings GetSettingsByID(int id) => new() { agentRadius = BakedRadius, agentTypeID = id };
        internal static int GetSettingsCount() => SettingsCount;
        internal static NavMeshBuildSettings GetSettingsByIndex(int index) => new() { agentRadius = BakedRadius, agentTypeID = SettingsType };
        internal static bool SamplePositionFilter(Vector3 point, out NavMeshHit hit, float radius, int type, int mask)
        {
            bool origin = Samples++ % 2 == 0;
            Filters.Add((radius, type, mask));
            hit = new() { position = point + (origin ? OriginOffset : GoalOffset) };
            return origin ? OriginAvailable : GoalAvailable;
        }
        internal static bool RaycastFilter(Vector3 from, Vector3 to, out NavMeshHit hit, int type, int mask)
        { Rays++; Filters.Add((0, type, mask)); hit = default; return RayBlocked?.Invoke(from, to) ?? Blocked; }
        internal static bool CalculatePath(Vector3 from, Vector3 to, int mask, NavMeshPath path)
        {
            Calculations++;
            path.status = Status;
            if (!PreserveCorners)
                path.corners = NullCorners ? null : new() { Values = Corners ?? new[] { from, to }, WasCollected = CollectedCorners };
            return PathAvailable;
        }
        internal static void Reset()
        {
            SettingsCount = 1; SettingsType = Samples = Rays = Calculations = 0; BakedRadius = 0.5f;
            OriginAvailable = GoalAvailable = PathAvailable = true;
            Blocked = CollectedCorners = NullCorners = PreserveCorners = false;
            OriginOffset = GoalOffset = default; Corners = null; Status = NavMeshPathStatus.PathComplete; Filters.Clear();
            RayBlocked = null;
        }
    }
}
namespace Il2CppSystem.Collections.Generic
{
    internal class ICollection<T> : NativeObject { internal virtual int Count => 0; }
    internal sealed class IList<T> : ICollection<T>
    {
        internal readonly System.Collections.Generic.List<T> Items = new();
        internal override int Count => Items.Count;
        internal T this[int index] => Items[index];
    }
}
namespace Rewired
{
    internal sealed class IControllerTemplateThumbStick : NativeObject { internal UnityEngine.Vector2 value, valuePrev; }
    internal sealed class IGamepadTemplate : NativeObject { internal IControllerTemplateThumbStick rightStick = new(); }
    internal sealed class Joystick : NativeObject
    {
        internal IGamepadTemplate Template = new();
        internal T GetTemplate<T>() where T : class => Template as T;
    }
    internal sealed class Player : NativeObject
    {
        internal ControllerHelper controllers = new();
        internal sealed class ControllerHelper : NativeObject
        { internal Il2CppSystem.Collections.Generic.IList<Joystick> Joysticks = new(); }
    }
}
internal sealed class EpochInputManager : UnityEngine.Object
{ internal static EpochInputManager instance = new(); internal Rewired.Player rewiredPlayer = new(); internal bool isInputFieldActive; }
internal sealed class ScreenKeyboardManager : UnityEngine.Object
{ internal static ScreenKeyboardManager Instance = new(); internal bool _enabled; }
namespace LE.UI { internal static class LoadingScreen { internal static bool IsVisible; } }
namespace BepInEx.Configuration
{
    internal sealed class ConfigDescription { internal ConfigDescription(string text, object range) { } }
    internal sealed class AcceptableValueRange<T> { internal AcceptableValueRange(T min, T max) { } }
    internal sealed class ConfigEntry<T> { internal T Value; }
    internal sealed class ConfigFile
    {
        internal readonly Dictionary<string, object> Values = new();
        internal ConfigEntry<T> Bind<T>(string section, string key, T value, string description)
        { var entry = new ConfigEntry<T> { Value = value }; Values[key] = entry; return entry; }
        internal ConfigEntry<T> Bind<T>(string section, string key, T value, ConfigDescription description)
            => Bind(section, key, value, "");
    }
}
namespace LeAccess.Accessibility.MapSections
{
    internal readonly record struct MapSectionWorldSnapshot(long Session, int SceneHandle, double X, double Y, double Z, double BodyRadius);
    internal static class MapSectionWorld
    {
        internal static bool Available = true;
        internal static MapSectionWorldSnapshot World = new(1, 1, 0, 0, 0, 0.5);
        internal static bool TryCapturePlayer(out MapSectionWorldSnapshot world) { world = World; return Available; }
    }
    internal static class MapPlayerFloor
    {
        internal static UnityEngine.AI.NavMeshAgent Agent = new();
        internal static double Allowance = 0.5;
        internal static bool TryGetQueryAgent(MapSectionWorldSnapshot world, out UnityEngine.AI.NavMeshAgent agent)
        { agent = Agent; return agent is not null; }
        internal static double HeightAllowance(MapSectionWorldSnapshot world) => Allowance;
    }
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState
    {
        private static bool _suspended;
        internal static bool IsSuspended
        {
            get => _suspended || !UnityEngine.Application.isFocused || UnityEngine.Time.timeScale <= 0 || LE.UI.LoadingScreen.IsVisible;
            set => _suspended = value;
        }
    }
    internal static class GameplayInputState { internal static bool Owned = true; internal static bool OwnsInput() => Owned; }
}
namespace LeAccess.Accessibility.Navigation.Guidance
{
    internal static class EncounterGuidanceTargets
    {
        internal static bool HasTrackedPointers;
        internal static int Reads;
        internal static void Collect(LeAccess.Accessibility.MapSections.MapSectionWorldSnapshot world, List<GuidanceTarget> targets)
        {
            Reads++;
            targets.AddRange(GuidanceTargetReader.Targets.Where(target => target.Kind == GuidanceTargetKind.EncounterReturn));
        }
    }
    internal static class GuidanceTargetReader
    {
        internal static string LastFailure => null;
        internal static bool Available = true, Throw;
        internal static bool LastIncludeExits;
        internal static int Reads;
        internal static readonly List<GuidanceTarget> Targets = new();
        internal static bool TryRead(LeAccess.Accessibility.MapSections.MapSectionWorldSnapshot world, List<GuidanceTarget> targets, bool includeExits = false)
        { Reads++; LastIncludeExits = includeExits; if (Throw) throw new Exception("Target read failed"); targets.AddRange(Targets); return Available; }
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true, OutputAccepted = true;
        internal static bool SpokeThisFrame = false, Speaking = false;
        internal static readonly List<string> Spoken = new();
        internal static bool IsAvailable() => Available;
        internal static bool IsSpeaking() => Speaking;
        internal static void Speak(string text, bool interrupt) => TrySpeak(text, interrupt);
        internal static bool TrySpeak(string text, bool interrupt)
        { if (!Available || !OutputAccepted) return false; Spoken.Add(text); return true; }
    }
}
namespace LeAccess.I18n
{
    internal static class ModText
    {
        internal static string Get(string key) => key;
        internal static string Format(string key, params object[] args) => key + "[" + string.Join("|", args) + "]";
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Warnings = new(), Info = new();
        internal void LogWarning(string text) => Warnings.Add(text);
        internal void LogInfo(string text) => Info.Add(text);
    }
}
