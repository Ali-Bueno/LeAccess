// Minimal host doubles exercise the production lifecycle without loading Unity or IL2CPP.
namespace UnityEngine
{
    internal class Object
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public bool WasCollected { get; set; }
        public bool Destroyed { get; set; }
        public int GetInstanceID() => _id;
        public IntPtr Pointer => new(_id);
        public static bool operator ==(Object left, Object right)
        {
            if ((left?.WasCollected ?? false) || (right?.WasCollected ?? false))
                throw new InvalidOperationException("Object was garbage collected in IL2CPP domain");
            return ReferenceEquals(left?.Destroyed == true ? null : left, right?.Destroyed == true ? null : right);
        }
        public static bool operator !=(Object left, Object right) => !(left == right);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => _id;
    }

    internal class GameObject : Object
    {
        public bool activeInHierarchy = true;
        public SceneManagement.Scene scene = SceneManagement.SceneManager.GetActiveScene();
        public Transform transform = new();
        private AI.NavMeshAgent _agent;
        public AI.NavMeshAgent Agent
        {
            get => _agent;
            set
            {
                _agent = value;
                if (value is not null && value.gameObject is null) value.gameObject = this;
            }
        }
        public GameObject() { Agent = new(); }
        public T GetComponent<T>() where T : Object => Agent as T;
        public T GetComponentInChildren<T>() where T : Object => Agent as T;
    }

    internal class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
    }

    internal class Transform
    {
        public Transform parent;
        public bool IsChildOf(Transform ancestor) => ReferenceEquals(this, ancestor) || (parent?.IsChildOf(ancestor) ?? false);
        public Vector3 position = default;
        public Quaternion rotation = default;
        public Vector3 forward = new(0, 0, 1);
        public Vector3 right = new(1, 0, 0);
    }

    internal class Camera : Object
    {
        public static Camera main = new();
        public bool isActiveAndEnabled = true;
        public Transform transform = new();
    }

    internal struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x * x + y * y + z * z;
        public void Normalize()
        {
            float length = MathF.Sqrt(sqrMagnitude);
            x /= length; y /= length; z /= length;
        }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.x * b, a.y * b, a.z * b);
    }

    internal readonly record struct Quaternion;
    internal static class Mathf
    {
        public const float Epsilon = float.Epsilon;
        // Unity's documented relative float comparison; the game adapter calls the real API.
        public static bool Approximately(float a, float b) =>
            MathF.Abs(b - a) < MathF.Max(0.000001f * MathF.Max(MathF.Abs(a), MathF.Abs(b)), Epsilon * 8);
    }
    internal static class Application { public static bool isFocused = true; }
    internal static class Time { public static float timeScale = 1; public static double unscaledTimeAsDouble; }
}

namespace UnityEngine.AI
{
    internal class NavMeshAgent : Component
    {
        public bool isActiveAndEnabled = true;
        public bool isOnNavMesh = true;
        public float radius = 1;
        public int areaMask = -1;
        public int agentTypeID = 0;
    }
    internal struct NavMeshHit { public Vector3 position; }
    internal struct NavMeshBuildSettings { public float agentRadius, agentClimb; }
    internal static class NavMesh
    {
        public static NavMeshBuildSettings Settings = new() { agentRadius = float.NaN, agentClimb = float.NaN };
        public static Func<Vector3, float, Vector3?> TargetSample;
        public static int Rays;
        public static NavMeshBuildSettings GetSettingsByID(int type) => Settings;
        public static float Depth = 20;
        public static bool OriginAvailable = true;
        public static bool ThrowOnQuery;
        public static int Queries;
        public static float SampleRadius;
        public static Vector3 SampleOffset;
        public static readonly List<(int Type, int Mask)> Filters = new();
        public static bool SamplePositionFilter(Vector3 position, out NavMeshHit hit, float radius, int type, int mask)
        {
            Queries++;
            SampleRadius = radius;
            Filters.Add((type, mask));
            if (ThrowOnQuery) throw new InvalidOperationException("Simulated native query failure");
            if (TargetSample != null && TargetSample(position, radius) is { } target)
            { hit = new NavMeshHit { position = target }; return true; }
            hit = new NavMeshHit { position = position + SampleOffset };
            return OriginAvailable && TargetSample == null;
        }
        public static bool RaycastFilter(Vector3 position, Vector3 target, out NavMeshHit hit, int type, int mask)
        {
            Filters.Add((type, mask));
            Rays++;
            var direction = new Vector3(target.x - position.x, 0, target.z - position.z);
            float length = MathF.Sqrt(direction.sqrMagnitude);
            direction.Normalize();
            hit = new NavMeshHit { position = position + direction * Depth };
            return Depth < length;
        }
    }
}

namespace UnityEngine.SceneManagement
{
    internal struct Scene { public int handle; public bool isLoaded => handle > 0; public bool IsValid() => handle > 0; }
    internal static class SceneManager
    {
        public static int Handle = 1;
        public static Scene GetActiveScene() => new() { handle = Handle };
    }
}

internal static class PlayerFinder
{
    public static UnityEngine.GameObject Player = new();
    private static Actor _actor;
    public static Actor Actor
    {
        get => _actor;
        set { _actor = value; if (value is not null && value.gameObject is null) value.gameObject = Player; }
    }
    private static ActorVisuals _visuals;
    public static ActorVisuals playerActorVisuals
    {
        get => _visuals;
        set { _visuals = value; if (value is not null && value.gameObject is null) value.gameObject = Player; }
    }
    public static UnityEngine.GameObject player => Player;
    public static Actor playerActorComponent => Actor;
    public static UnityEngine.GameObject getPlayer() => Player;
    public static Actor getPlayerActor() => Actor;
}
internal class Actor : UnityEngine.Component
{
    public ActorSync actorSync;
    public bool isActiveAndEnabled = true;
    public float Radius;
    public float getRadius() => Radius;
}
internal class ActorSync : UnityEngine.Component
{
    public Actor gameplayActor;
    public ActorVisuals actorVisuals;
}
internal class ActorVisuals : UnityEngine.Component
{
    public ActorSync actorSync;
    public bool isActiveAndEnabled = true;
    public bool dead;
    public float Radius;
    public float getRadius() => Radius;
}
namespace LeAccess.Accessibility.MapSections
{
    internal static class MapPlaceDiagnostics
    { internal static void Report(string reason, string detail) { } }
}
internal class LocalPlayer : UnityEngine.Component
{
    public static LocalPlayer instance = null;
    public UnityEngine.AI.NavMeshAgent navMeshAgent = null;
    public ActorSync actorSync;
}
namespace EHG.Multiplayer { internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
internal class DialogueSystemNavigable : UnityEngine.Object { public bool isActiveAndEnabled = true; }
namespace LeAccess.Accessibility.Navigation
{
    internal static class GameplayInputState
    {
        internal static bool Owns = true;
        internal static bool OwnsInput() => Owns;
    }
}
namespace LE.UI { internal static class LoadingScreen { public static bool IsVisible; } }

namespace BepInEx.Configuration
{
    public class ConfigFile
    {
        public ConfigEntry<T> Bind<T>(string section, string key, T value, object description) => new() { Value = value };
    }
    public class ConfigEntry<T> { public T Value; }
    public class ConfigDescription { public ConfigDescription(string text, object range) { } }
    public class AcceptableValueRange<T> { public AcceptableValueRange(T minimum, T maximum) { } }
}
namespace LeAccess
{
    internal static class Plugin { public static TestLogger Log = new(); }
    internal class TestLogger
    {
        public readonly List<string> Warnings = new();
        public readonly List<string> Information = new();
        public void LogInfo(string message) => Information.Add(message);
        public void LogWarning(string message) => Warnings.Add(message);
    }
}
namespace LeAccess.Accessibility.Audio
{
    internal class RadarAudioOutput : IDisposable
    {
        public static int Played;
        public static int ConfiguredBufferMilliseconds;
        public RadarAudioOutput() { }
        public RadarAudioOutput(string description, int playbackLatencyMilliseconds = 50)
        { ConfiguredBufferMilliseconds = playbackLatencyMilliseconds; }
        public bool IsPlaying => false;
        public bool IsRunning => true;
        public bool TryRecover(out string status) { status = null; return true; }
        public string Diagnostics => $"test output submitted={Played}";
        public void Play(float[] cue) => Played++;
        public void Silence() { }
        public void Dispose() { }
    }
}
