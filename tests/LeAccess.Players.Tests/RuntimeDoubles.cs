internal class NativeObject
{
    private static int _next;
    internal bool WasCollected;
    internal IntPtr Pointer { get; } = new(++_next);
    internal T TryCast<T>() where T : class => this as T;
}
namespace UnityEngine
{
    internal class Object : NativeObject
    {
        internal bool Destroyed;
        internal int GetInstanceID() => Pointer.ToInt32();
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b) || (a is null || a.Destroyed) && (b is null || b.Destroyed);
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => GetInstanceID();
    }
    internal class GameObject : Object
    {
        internal bool activeInHierarchy = true;
        internal SceneManagement.Scene scene = new(1);
        internal Transform transform;
        internal GameObject() => transform = new(this);
    }
    internal class Transform : Object
    {
        internal GameObject gameObject;
        internal Transform parent;
        internal Vector3 position;
        internal Transform(GameObject owner) => gameObject = owner;
        internal bool IsChildOf(Transform root)
        { for (var node = this; node != null; node = node.parent) if (ReferenceEquals(node, root)) return true; return false; }
    }
    internal class MonoBehaviour : Object
    {
        internal GameObject gameObject = new();
        internal Transform transform => gameObject.transform;
        internal bool isActiveAndEnabled = true;
    }
    internal readonly record struct Vector3(float x, float y, float z)
    { public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z); }
    internal static class Time { internal static double unscaledTimeAsDouble; }
}
namespace UnityEngine.SceneManagement
{
    internal readonly record struct Scene(int handle)
    { internal bool IsValid() => handle != 0; internal bool isLoaded => handle != 0; }
    internal static class SceneManager { internal static int Handle = 1; internal static Scene GetActiveScene() => new(Handle); }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{ internal class Il2CppReferenceArray<T> : List<T> where T : class { internal Il2CppReferenceArray(int count) : base(Enumerable.Repeat<T>(null, count)) { } } }
internal sealed class PlayerCollection : NativeObject
{
    internal readonly List<PlayerActorSync> Values = new();
    internal int Count => Values.Count;
    internal void CopyTo(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PlayerActorSync> array, int index)
    { for (int i=0;i<Values.Count;i++) array[index+i] = Values[i]; }
}
namespace LE.Networking
{ internal static class PlayerStore { internal static PlayerCollection Players = new(); internal static PlayerCollection GetPlayerActorSyncs() => Players; } }
namespace EHG.Multiplayer { internal static class GameplayEnvironment { internal static bool IsOnlineClient = true; } }
internal class ActorSync : UnityEngine.MonoBehaviour { internal ActorVisuals actorVisuals; }
internal sealed class PlayerActorSync : ActorSync { internal LocalPlayer localPlayer; }
internal sealed class LocalPlayer : UnityEngine.MonoBehaviour { internal ActorSync actorSync; }
internal sealed class UserIdentity : NativeObject { internal string Username, MasterAccountID = "SECRET_ACCOUNT_ID"; }
internal sealed class ActorVisuals : UnityEngine.MonoBehaviour
{
    internal bool isPlayer = true, dead, ThrowIdentity;
    internal ActorSync actorSync;
    internal FloatingPlayerName floatingPlayerName;
    internal UserIdentity Identity;
    internal UserIdentity UserIdentity => ThrowIdentity ? throw new InvalidOperationException("Bad player") : Identity;
    internal UnityEngine.Vector3 position() => transform.position;
}
internal sealed class Label : UnityEngine.MonoBehaviour { internal string text; }
internal sealed class FloatingPlayerName : UnityEngine.MonoBehaviour
{
    internal ActorVisuals identityProvider;
    internal bool hasBeenInitialized = true, updateLabelsNextFrame, Default;
    internal Label text;
    internal bool IsDefaultText() => Default;
}
internal static class PlayerFinder
{
    internal static UnityEngine.GameObject player;
    internal static ActorVisuals playerActorVisuals;
    internal static LocalPlayer Local;
    internal static LocalPlayer getLocalPlayerInMultiplayer() => Local;
}
internal sealed class TargetingConfiguration : NativeObject { internal float playersRange = 7; }
internal sealed class EpochInputManager : UnityEngine.MonoBehaviour { internal static EpochInputManager instance; internal TargetingConfiguration targetingConfiguration = new(); }
namespace LeAccess.Accessibility.Utils { internal static class TextUtils { internal static string CleanText(string text) => text?.Replace("<b>", "").Replace("</b>", "").Trim(); } }
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { internal static bool IsSuspended; }
    internal static class GameplayInputState { internal static bool Owns = true; internal static bool OwnsInput() => Owns; }
}
namespace LeAccess.Accessibility.ScreenContent { internal static class ScreenContentAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue { internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true, SpokeThisFrame;
        internal static readonly List<(string Text,bool Interrupt)> Messages = new();
        internal static bool IsAvailable() => Available;
        internal static void Speak(string text, bool interrupt) => Messages.Add((text,interrupt));
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal sealed class Logger { internal readonly List<string> Warnings = new(); internal void LogInfo(string text) { } internal void LogWarning(string text) => Warnings.Add(text); }
}
