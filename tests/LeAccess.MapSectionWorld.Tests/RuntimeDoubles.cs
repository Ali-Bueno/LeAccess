internal class NativeObject
{
    private static int _next;
    private readonly IntPtr _pointer = new(++_next);
    internal bool WasCollected;
    internal IntPtr Pointer { get { Guard(); return _pointer; } }
    protected void Guard() { if (WasCollected) throw new Exception("Collected wrapper accessed"); }
}
namespace UnityEngine
{
    internal static class Time { internal static double unscaledTimeAsDouble; }
    internal class Object : NativeObject
    {
        internal bool Destroyed;
        internal int GetInstanceID() => Pointer.ToInt32();
        public static bool operator ==(Object a, Object b)
        { a?.Guard(); b?.Guard(); return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a,b); }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this,other);
        public override int GetHashCode() => GetInstanceID();
    }
    internal class Component : Object
    {
        internal GameObject gameObject;
        internal Transform transform => gameObject.transform;
        internal string name => gameObject.name;
    }
    internal class Behaviour : Component
    { internal bool Enabled = true; internal bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy; }
    internal class Transform : Component
    {
        internal Matrix4x4 localToWorldMatrix = new();
        internal Transform parent => gameObject.Parent?.transform;
        internal Vector3 position;
        internal int childCount => gameObject.Children.Count;
        internal Transform GetChild(int i) => gameObject.Children[i].transform;
        internal bool IsChildOf(Transform root)
        { for (var current = gameObject; current is not null; current = current.Parent) if (ReferenceEquals(current.transform,root)) return true; return false; }
    }
    internal class GameObject : Object
    {
        internal string name;
        internal bool Active = true;
        internal GameObject Parent;
        internal List<GameObject> Children = new();
        internal List<Component> Components = new();
        internal SceneManagement.Scene scene;
        internal Transform transform;
        internal GameObject(string value = "") { name = value; transform = new Transform { gameObject = this }; }
        internal bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        internal GameObject Child(string value) { var child = new GameObject(value) { Parent = this, scene = scene }; Children.Add(child); return child; }
        internal T Add<T>(T component) where T : Component { component.gameObject = this; Components.Add(component); return component; }
        internal NativeArray<T> GetComponents<T>() where T : Component => new() { Items = Components.OfType<T>().ToList() };
    }
    internal class BoxCollider : Component
    { internal bool enabled=true,isTrigger; internal Vector3 center; internal Vector3 size=new(1,1,1); }
    internal struct Vector3
    {
        internal float x,y,z; internal Vector3(float a,float b,float c) { x=a; y=b; z=c; }
        internal float this[int axis] => axis==0 ? x : axis==1 ? y : z;
    }
    internal class Matrix4x4
    {
        private readonly float[,] _values = {{1,0,0,0},{0,1,0,0},{0,0,1,0},{0,0,0,1}};
        internal float this[int row,int column] { get => _values[row,column]; set => _values[row,column] = value; }
    }
}
internal sealed class NativeArray<T> : NativeObject, System.Collections.Generic.IEnumerable<T>
{
    internal List<T> Items = new();
    internal int Length => Items.Count;
    internal T this[int index] => Items[index];
    public IEnumerator<T> GetEnumerator() { Guard(); return Items.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene
    {
        internal int handle;
        internal string name,path;
        internal bool isLoaded, Valid;
        internal bool IsValid() => Valid;
        internal NativeArray<UnityEngine.GameObject> GetRootGameObjects() { SceneManager.RootReads++; return SceneManager.Roots; }
    }
    internal static class SceneManager
    {
        internal static Scene Active;
        internal static int RootReads;
        internal static NativeArray<UnityEngine.GameObject> Roots;
        internal static Scene GetActiveScene() => Active;
    }
}
internal sealed class Actor : UnityEngine.Behaviour
{ internal bool Dying; internal float Radius = 0.5f; internal bool isDying() => Dying; internal float getRadius() => Radius; }
internal sealed class ActorVisuals : UnityEngine.Behaviour { internal bool dead; }
internal static class PlayerFinder
{ internal static UnityEngine.GameObject player; internal static Actor playerActorComponent; internal static ActorVisuals playerActorVisuals; }
internal static class SceneList
{ internal static bool Pcg,Echo; internal static bool IsPcgScene(string name) => Pcg; internal static bool IsMonolithEcho(UnityEngine.SceneManagement.Scene scene) => Echo; }
namespace EHG.Multiplayer { internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
namespace LE.Services
{
    internal static class ServiceProvider { internal static VariantService SceneVariantService; }
    internal sealed class VariantService : NativeObject
    { internal bool HasVariant; internal int Index; internal bool TryGetActiveVariantIndex(out int index) { Guard(); index=Index; return HasVariant; } }
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal sealed class Logger { internal List<string> Warnings = new(); internal void LogWarning(string text) => Warnings.Add(text); }
}
