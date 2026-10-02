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
        internal Transform parent => gameObject.Parent?.transform;
        internal bool IsChildOf(Transform owner)
        { for (var g = gameObject; g is not null; g = g.Parent) if (g.transform == owner) return true; return false; }
    }
    internal class GameObject : Object
    {
        internal SceneManagement.Scene scene;
        internal GameObject Parent;
        internal bool Active = true;
        internal string name = "Object";
        internal int layer = 7;
        internal Transform transform;
        internal int Discoveries;
        internal List<Component> Components = new();
        internal AI.NavMeshAgent Agent;
        internal T GetComponentInChildren<T>() where T : class => Agent as T;
        internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<FootstepRaycastTrigger> Triggers;
        internal bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        internal GameObject() { transform = new() { gameObject = this }; }
        internal T Add<T>(T c) where T : Component { c.gameObject = this; Components.Add(c); return c; }
        internal Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T> GetComponentsInChildren<T>() where T : Component
        { Discoveries++; return new(Triggers?.Values.OfType<T>().ToArray() ?? Array.Empty<T>()); }
    }
    internal class Collider : Component { internal bool enabled = true, isTrigger; }
    internal struct Vector3
    {
        internal float x, y, z;
        internal Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        internal static Vector3 down => new(0, -1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    internal struct RaycastHit { internal Collider collider; internal float distance; internal Vector3 normal, point; }
    internal enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    internal static class Time { internal static double unscaledTimeAsDouble; }
    internal static class Physics
    {
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
    internal struct Scene { internal int handle; internal string name, path; internal bool Valid, isLoaded; internal bool IsValid() => Valid; }
    internal static class SceneManager { internal static Scene Scene; internal static Scene GetActiveScene() => Scene; }
}
namespace UnityEngine.AI
{
    internal class NavMeshAgent : UnityEngine.Behaviour { internal float baseOffset; internal int agentTypeID; }
    internal struct NavMeshBuildSettings { internal float agentClimb; }
    internal static class NavMesh { internal static float Climb; internal static NavMeshBuildSettings GetSettingsByID(int id) => new() { agentClimb = Climb }; }
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
