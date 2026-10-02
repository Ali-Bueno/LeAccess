namespace UnityEngine
{
    internal class Object
    {
        private static int _next;
        private readonly int _id = ++_next;
        internal bool WasCollected;
        internal IntPtr Pointer { get { Guard(); return new(_id); } }
        internal int GetInstanceID() { Guard(); return _id; }
        private void Guard() { if (WasCollected) throw new InvalidOperationException("Collected object dereference"); }
        public static bool operator ==(Object a, Object b) { a?.Guard(); b?.Guard(); return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => _id;
        internal T TryCast<T>() where T : Object => this as T;
    }
    internal class GameObject : Object
    {
        internal readonly List<MonoBehaviour> Components = new();
        internal bool activeInHierarchy = true;
        internal UnityEngine.SceneManagement.Scene scene = new(1);
        internal Transform transform = new();
    }
    internal class Transform : Object
    {
        internal Vector3 position;
        internal Vector3 right = new(1, 0, 0), forward = new(0, 0, 1);
    }
    internal class MonoBehaviour : Object
    {
        internal GameObject gameObject = new();
        internal MonoBehaviour() { gameObject.Components.Add(this); }
        internal T GetComponent<T>() where T : MonoBehaviour => gameObject.Components.OfType<T>().FirstOrDefault();
        internal Transform transform => gameObject.transform;
        internal bool isActiveAndEnabled = true;
    }
    internal class Camera : MonoBehaviour { internal static Camera main = new(); }
    internal class CanvasGroup : Object { internal float alpha = 1; }
    internal class CanvasRenderer : Object { internal bool cull; }
    internal struct Vector3
    {
        internal float x, y, z;
        internal Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        internal float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        internal static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
    }
    internal static class Time { internal static double unscaledTimeAsDouble; }
}
namespace UnityEngine.SceneManagement
{
    internal readonly record struct Scene(int handle)
    { internal bool isLoaded => handle != 0; internal bool IsValid() => handle != 0; }
    internal static class SceneManager { internal static int Handle = 1; internal static Scene GetActiveScene() => new(Handle); }
}
internal sealed class Registry<T> : List<T> { internal bool WasCollected; }
internal static class PlayerFinder
{
    internal static UnityEngine.GameObject player = new();
    internal static ActorVisuals playerActorVisuals = new();
}
internal sealed class ActorVisuals : UnityEngine.MonoBehaviour { internal bool dead; }
internal sealed class TextLabel : UnityEngine.MonoBehaviour
{ internal float alpha = 1; internal UnityEngine.CanvasRenderer canvasRenderer = new(); }
internal class PickupableGroundLabel : UnityEngine.MonoBehaviour { internal TextLabel itemText = new(); }
internal sealed class GroundGoldLabel : PickupableGroundLabel { }
internal sealed class GroundItemLabel : PickupableGroundLabel
{
    internal UnityEngine.CanvasGroup canvasGroup = new();
    internal ItemFiltering.Rule.RuleOutcome lastFilterResult;
}
namespace ItemFiltering { internal class Rule { internal enum RuleOutcome { SHOW, HIDE, HIGHLIGHT_DEPRECATED } } }
internal sealed class ItemData
{ internal bool WasCollected, Equipment = true; internal bool isEquipment() => Equipment; }
internal sealed class GroundGoldVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundGoldVisuals> all = new();
    internal int goldValue = 10;
    internal GroundGoldLabel label = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class GroundItemVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundItemVisuals> all = new();
    internal ItemData itemData = new();
    internal GroundItemLabel label = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class WorldObjectClickListener : UnityEngine.MonoBehaviour
{
    internal static Registry<WorldObjectClickListener> allListener = new();
    internal bool requiresClick = true, Available = true, Npc, Throw, controllerTargetingUsePlayerY;
    internal float interactionRange = 5;
    internal bool CanInteract() { if (Throw) throw new InvalidOperationException("Native availability failed"); return Available; }
}
internal sealed class Targetable
{ internal bool WasCollected; internal UnityEngine.MonoBehaviour item; }
internal sealed class Targeting
{ internal bool WasCollected; internal Targetable ActiveTarget; }
internal sealed class EpochInputManager : UnityEngine.MonoBehaviour
{
    internal static EpochInputManager instance = new();
    internal Targeting InteractableTargeting = new();
    internal TargetingConfiguration targetingConfiguration = new();
}
internal sealed class TargetingConfiguration { internal bool WasCollected; internal float itemsRange = 5; }
namespace LeAccess.Accessibility.Npcs
{ internal static class NpcWorldProbe { internal static bool IsNpc(WorldObjectClickListener listener) => listener.Npc; } }
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { internal static bool IsSuspended; }
    internal static class GameplayInputState { internal static bool Owns = true; internal static bool OwnsInput() => Owns; }
}
namespace LeAccess.Accessibility.ScreenContent
{ internal static class ScreenContentAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue
{ internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace BepInEx.Configuration
{
    internal sealed class ConfigEntry<T> { internal T Value; }
    internal sealed class ConfigFile
    {
        private readonly Dictionary<string, object> _values = new();
        internal ConfigEntry<T> Bind<T>(string section, string key, T value, object description)
        { var entry = new ConfigEntry<T> { Value = value }; _values[key] = entry; return entry; }
        internal void Set<T>(string key, T value) => ((ConfigEntry<T>)_values[key]).Value = value;
    }
    internal sealed class ConfigDescription { internal ConfigDescription(string text, object range) { } }
    internal sealed class AcceptableValueRange<T> { internal AcceptableValueRange(T from, T to) { } }
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Info = new(), Warnings = new();
        internal void LogInfo(string value) => Info.Add(value);
        internal void LogWarning(string value) => Warnings.Add(value);
    }
}
namespace LeAccess.Accessibility.Audio
{
    internal sealed class RadarAudioOutput : IDisposable
    {
        internal static readonly List<RadarAudioOutput> Instances = new();
        internal static bool ThrowInitialize;
        internal static int Creates;
        internal bool IsRunning = true, IsPlaying, Disposed;
        internal int Plays, Silences, Recoveries;
        internal string Diagnostics => $"plays={Plays}";
        internal RadarAudioOutput(string name, int latency)
        { Creates++; if (ThrowInitialize) throw new InvalidOperationException("Device unavailable"); Instances.Add(this); }
        internal bool TryRecover(out string status) { Recoveries++; status = "recovered"; IsRunning = true; return true; }
        internal void Play(float[] samples) { Plays++; }
        internal void Silence() { Silences++; }
        public void Dispose() { Disposed = true; }
    }
}
