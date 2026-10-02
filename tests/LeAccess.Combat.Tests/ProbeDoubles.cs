using System.Collections;

namespace UnityEngine
{
    internal class Object
    {
        public int Id;
        public static int InvalidAccesses;
        public bool WasCollected;
        public bool Destroyed;
        public IntPtr Pointer { get { Guard(); return new IntPtr(Id); } }
        public int GetInstanceID() { Guard(); return Id; }
        protected void Guard()
        {
            if (WasCollected)
            {
                InvalidAccesses++;
                throw new InvalidOperationException("Collected IL2CPP wrapper was accessed");
            }
        }
        public static bool operator ==(Object left, Object right)
        {
            left?.Guard();
            right?.Guard();
            return (left is null || left.Destroyed) && (right is null || right.Destroyed) || ReferenceEquals(left, right);
        }
        public static bool operator !=(Object left, Object right) => !(left == right);
        public override bool Equals(object value) => ReferenceEquals(this, value);
        public override int GetHashCode() => Id;
    }

    internal struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator -(Vector3 left, Vector3 right) =>
            new(left.x - right.x, left.y - right.y, left.z - right.z);
    }

    internal class Transform : Object
    {
        public Vector3 position;
    }
}

internal sealed class NativeList<T> : IEnumerable<T>
{
    private readonly List<T> _items;
    public bool WasCollected;
    public NativeList(params T[] items) => _items = new(items);
    public int Count { get { Guard(); return _items.Count; } }
    public T this[int index] { get { Guard(); return _items[index]; } }
    public IEnumerator<T> GetEnumerator() { Guard(); return _items.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    private void Guard()
    {
        if (WasCollected)
        {
            UnityEngine.Object.InvalidAccesses++;
            throw new InvalidOperationException("Collected native collection was accessed");
        }
    }
}

internal sealed class Alignment : UnityEngine.Object
{
    public Alignment Foe;
    public bool IsFoe(Alignment other) { Guard(); return ReferenceEquals(Foe, other); }
}

internal sealed class BaseDisplayInformation : UnityEngine.Object
{
    public string Name;
    public int Reads;
    public string GetLocalizedName() { Guard(); Reads++; return Name; }
}

internal sealed class ActorVisuals : UnityEngine.Object
{
    public UnityEngine.Transform transform = new();
    public Alignment Alignment;
    public bool isActiveAndEnabled = true;
    public bool dead;
    public bool InvalidTarget;
    public BaseDisplayInformation BaseDisplayInformation;
    public bool SelectedByAim;
    public bool CausedDamage;
    public bool ThrowOnAlignment;
    public Alignment getAlignment() { Guard(); if (ThrowOnAlignment) throw new InvalidOperationException("broken visuals"); return Alignment; }
    public bool isInvalidTarget() { Guard(); return InvalidTarget; }
}

internal sealed class ActorManager : UnityEngine.Object
{
    public static ActorManager instance;
    public NativeList<ActorVisualsList> visuals;
    public NativeList<ActorList> actors;

    internal sealed class ActorVisualsList
    {
        public bool WasCollected;
        public Alignment alignment;
        public NativeList<ActorVisuals> visuals;
    }
    internal sealed class ActorList
    {
        public bool WasCollected;
        public Alignment alignment;
        public NativeList<Actor> actors;
    }
}

internal sealed class Actor : UnityEngine.Object
{
    public UnityEngine.Transform transform = new();
    public Alignment Alignment;
    public bool isActiveAndEnabled = true;
    public bool InvalidTarget, Dying, ThrowOnAlignment;
    public BaseDisplayInformation Display;
    public ActorSync actorSync;
    public ActorData data;
    public Alignment GetAlignment() { Guard(); if (ThrowOnAlignment) throw new InvalidOperationException("broken actor"); return Alignment; }
    public UnityEngine.Vector3 position() { Guard(); return transform.position; }
    public bool isInvalidTarget() { Guard(); return InvalidTarget || Dying; }
    public bool isDying() { Guard(); return Dying; }
    public T GetComponent<T>() where T : class { Guard(); return Display as T; }
}

internal sealed class ActorSync : UnityEngine.Object
{
    public Actor gameplayActor;
    public ActorVisuals actorVisuals;
}

internal sealed class ActorData : UnityEngine.Object
{
    public string actorName, LocalizedName;
    public int Reads;
    public string GetLocalizedActorName() { Guard(); Reads++; return LocalizedName; }
}

namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        public static string CleanText(string text) => text?.Trim();
    }
}
