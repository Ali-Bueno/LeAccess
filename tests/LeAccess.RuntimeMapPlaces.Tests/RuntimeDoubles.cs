internal class NativeObject
{
    private static int next;
    internal IntPtr Pointer = new(++next);
    internal bool WasCollected;
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    internal class Il2CppReferenceArray<T> : NativeObject, IEnumerable<T>
    {
        internal T[] Values;
        internal Il2CppReferenceArray(params T[] values) => Values = values;
        internal T this[int index] { get => Values[index]; set => Values[index] = value; }
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    internal class Il2CppStructArray<T> : Il2CppReferenceArray<T> where T : struct
    { internal Il2CppStructArray(params T[] values) : base(values) { } }
}
namespace UnityEngine
{
    internal class Object : NativeObject { internal bool Destroyed; internal int GetInstanceID() => (int)Pointer; }
    internal class Component : Object
    {
        internal GameObject gameObject;
        internal Transform transform => gameObject.transform;
        internal string name => gameObject.name;
        internal T GetComponentInParent<T>() where T : class
        { for (var g = gameObject; g is not null; g = g.Parent) { var value = g.Components.OfType<T>().FirstOrDefault(); if (value is not null) return value; } return null; }
    }
    internal class Behaviour : Component { internal bool Enabled = true; internal bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy; }
    internal class Transform : Component
    {
        internal Vector3 position;
        internal Vector3 lossyScale = new(1, 1, 1);
        internal float Yaw;
        internal Vector3 TransformPoint(Vector3 p)
        {
            float c = MathF.Cos(Yaw), s = MathF.Sin(Yaw);
            p = new(p.x * lossyScale.x, p.y * lossyScale.y, p.z * lossyScale.z);
            return position + new Vector3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
        }
        internal Vector3 InverseTransformPoint(Vector3 p)
        {
            p -= position; float c = MathF.Cos(Yaw), s = MathF.Sin(Yaw);
            return new((p.x * c - p.z * s) / lossyScale.x, p.y / lossyScale.y, (p.x * s + p.z * c) / lossyScale.z);
        }
        internal Transform parent => gameObject.Parent?.transform;
        internal bool IsChildOf(Transform owner)
        { for (var g = gameObject; g is not null; g = g.Parent) if (g.transform == owner) return true; return false; }
    }
    internal class GameObject : Object
    {
        internal static readonly List<GameObject> All = new();
        internal SceneManagement.Scene scene;
        internal GameObject Parent;
        internal bool Active = true;
        internal string name = "Object";
        internal int layer = 7;
        internal Transform transform;
        internal int Discoveries;
        internal List<Component> Components = new();
        internal AI.NavMeshAgent Agent;
        internal T GetComponent<T>() where T : class => Components.OfType<T>().FirstOrDefault();
        internal T GetComponentInChildren<T>() where T : class => Agent as T;
        internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<FootstepRaycastTrigger> Triggers;
        internal bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        internal GameObject() { transform = new() { gameObject = this }; All.Add(this); }
        internal T Add<T>(T c) where T : Component { c.gameObject = this; Components.Add(c); return c; }
        internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T> GetComponentsInChildren<T>() where T : Component
        { Discoveries++; return new(Triggers?.Values.OfType<T>().ToArray() ?? Array.Empty<T>()); }
        internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T> GetComponentsInChildren<T>(bool includeInactive) where T : Component
        { Discoveries++; return new(All.Where(g => g.transform.IsChildOf(transform) && (includeInactive || g.activeInHierarchy)).SelectMany(g => g.Components).OfType<T>().ToArray()); }
    }
    internal class Collider : Component { internal bool enabled = true, isTrigger; }
    internal class MeshRenderer : Component { internal bool enabled = true; internal Bounds localBounds; }
    internal class MeshFilter : Component { internal Mesh sharedMesh; }
    internal class Mesh : Object { internal string name; internal int vertexCount, subMeshCount; }
    internal struct Bounds
    {
        internal Vector3 min, max;
        internal Bounds(Vector3 center, Vector3 size) { min = center - size / 2; max = center + size / 2; }
    }
    internal struct Vector3
    {
        internal float x, y, z;
        internal Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        internal static Vector3 down => new(0, -1, 0);
        internal float magnitude => MathF.Sqrt(x*x + y*y + z*z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator /(Vector3 a, float b) => new(a.x / b, a.y / b, a.z / b);
    }
    internal struct RaycastHit { internal Collider collider; internal float distance; internal Vector3 normal, point; }
    internal enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    internal static class Time { internal static double unscaledTimeAsDouble; }
    internal static class Physics
    {
        internal const int DefaultRaycastLayers = -5;
        internal static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit> Hits;
        internal static Queue<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit>> FootHits = new();
        internal static Vector3 Origin, Direction;
        internal static float Distance;
        internal static int Mask, Calls;
        internal static QueryTriggerInteraction Query;
        internal static bool Throw;
        internal static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit> RaycastAll(Vector3 o, Vector3 d, float distance, int mask, QueryTriggerInteraction query)
        { Calls++; Origin = o; Direction = d; Distance = distance; Mask = mask; Query = query; if (Throw) throw new Exception("physics unavailable"); return FootHits.Count > 0 ? FootHits.Dequeue() : Hits; }
    }
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene
    {
        internal int handle; internal string name, path; internal bool Valid, isLoaded; internal bool IsValid() => Valid;
        internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.GameObject> GetRootGameObjects()
        { int h = handle; return new(UnityEngine.GameObject.All.Where(g => g.Parent is null && g.scene.handle == h).ToArray()); }
    }
    internal static class SceneManager { internal static Scene Scene; internal static Scene GetActiveScene() => Scene; }
}
namespace UnityEngine.AI
{
    internal class NavMeshAgent : UnityEngine.Behaviour { internal float baseOffset, radius = .1f, height = 2; internal int agentTypeID, areaMask = 1; }
    internal struct NavMeshBuildSettings { internal float agentClimb, agentRadius; }
    internal struct NavMeshHit { internal UnityEngine.Vector3 position; }
    internal static class NavMesh
    {
        internal static float Climb;
        internal static float BakedRadius;
        internal static bool SampleAvailable = true, Blocked;
        internal static int SampleCalls, RayCalls, Type, Mask;
        internal static float Radius;
        internal static UnityEngine.Vector3 Offset;
        internal static Func<UnityEngine.Vector3, float, (bool Found, UnityEngine.Vector3 Position)> Sample;
        internal static NavMeshBuildSettings GetSettingsByID(int id) => new() { agentClimb = Climb, agentRadius = BakedRadius };
        internal static bool SamplePositionFilter(UnityEngine.Vector3 p, out NavMeshHit hit, float radius, int type, int mask)
        {
            SampleCalls++; Radius = radius; Type = type; Mask = mask;
            var result = Sample?.Invoke(p, radius) ?? (SampleAvailable, p + Offset);
            hit = new() { position = result.Item2 }; return result.Item1;
        }
        internal static bool RaycastFilter(UnityEngine.Vector3 a, UnityEngine.Vector3 b, out NavMeshHit hit, int type, int mask)
        { RayCalls++; Type = type; Mask = mask; hit = new() { position = b }; return Blocked; }
    }
}
internal class ActorVisuals : UnityEngine.Behaviour { internal bool dead; }
internal class Actor : UnityEngine.Behaviour { internal ActorSync actorSync; }
internal class ActorSync : UnityEngine.Behaviour { internal Actor gameplayActor; internal ActorVisuals actorVisuals; }
internal static class PlayerFinder { internal static UnityEngine.GameObject player; internal static ActorVisuals playerActorVisuals; internal static Actor playerActorComponent; }
internal class FootstepRaycastTrigger : UnityEngine.Behaviour
{
    internal ActorVisuals actorVisuals;
    internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.Transform> feet;
    internal float raycastDistance = 3;
    internal static int raycastMask = 128;
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationObjectLifetime
    { internal static bool IsAlive(UnityEngine.Object o) => o is not null && !o.WasCollected && !o.Destroyed; }
}
namespace LeAccess.Accessibility.MapSections
{
    internal readonly record struct MapSectionWorldSnapshot(long Session, int SceneHandle, string SceneName, string ScenePath, double X, double Y, double Z, double BodyRadius, int? Variant);
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal class Logger
    { internal List<string> Warnings = new(), Infos = new(); internal void LogWarning(string s) => Warnings.Add(s); internal void LogInfo(string s) => Infos.Add(s); }
}
