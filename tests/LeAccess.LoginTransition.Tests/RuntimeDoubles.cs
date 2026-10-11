internal class NativeWrapper
{
    private static int _nextId;
    private readonly IntPtr _pointer = (IntPtr)(++_nextId);
    public bool WasCollected;
    public IntPtr Pointer { get { Guard(); return _pointer; } }
    protected void Guard() { if (WasCollected) throw new InvalidOperationException("Collected wrapper accessed"); }
}
namespace UnityEngine
{
    internal class Object : NativeWrapper
    {
        public static bool operator ==(Object a, Object b)
        { a?.Guard(); b?.Guard(); return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.GetHashCode();
    }
    internal class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled = true;
    }
    internal class Transform : Object
    {
        public Transform Parent;
        public bool IsChildOf(Transform parent)
        {
            for (var current = this; current is not null; current = current.Parent)
                if (current.Pointer == parent.Pointer) return true;
            return false;
        }
    }
    internal sealed class GameObject : Object
    {
        public Transform transform = new();
        public bool activeInHierarchy = true, Visible = true;
        public Scene scene = new() { handle = 3, isLoaded = true };
    }
    internal struct Scene { public int handle; public bool isLoaded; }
    internal static class Application { public static bool isFocused = true; }
}
namespace UnityEngine.UI
{
    internal sealed class Button : UnityEngine.Component
    {
        public bool interactable = true;
        public bool IsInteractable() { Guard(); return interactable; }
    }
}
namespace ClientAppState
{
    internal enum ClientAppStateType { None, SystemLoading, Login, CharacterSelect, InGame }
    internal sealed class Transition : NativeWrapper
    {
        public bool Active;
        public bool HasValue { get { Guard(); return Active; } }
    }
    internal static class ClientStateManager
    {
        public static ClientAppStateType CurrentClientAppStateType;
        internal static Transition NativeTransition = new();
        internal static bool ThrowSnapshotRead;
        internal static int GeneratedGetterReads;
        public static Transition _activeTransition
        {
            get
            {
                GeneratedGetterReads++;
                if (NativeTransition is null || !NativeTransition.Active) throw new NullReferenceException("Empty nullable boxed to zero");
                return NativeTransition;
            }
            set { NativeTransition = value; }
        }
    }
}
namespace LE.UI
{
    internal sealed class FSMController<T> : NativeWrapper
    {
        private T _state;
        public T CurrentState { get { Guard(); return _state; } set { _state = value; } }
    }
    internal static class LoadingScreen { public static bool IsVisible; }
}
namespace LE.UI.Login
{
    internal sealed class LoginController : NativeWrapper
    {
        internal enum LoginUIState { None, CheckForSteam, SteamRequired, LoginPlayer, EmailPrompt, DisplayNamePrompt, LandingZone, Queuing, CharacterSelect }
        public LE.UI.Login.UnityUI.LandingZonePanel landingZonePanel;
        public LE.UI.FSMController<LoginUIState> FSM = new() { CurrentState = LoginUIState.LandingZone };
    }
}
namespace LE.UI.Login.UnityUI
{
    internal sealed class LandingZonePanel : UnityEngine.Component
    {
        public LoginController loginController;
        public UnityEngine.UI.Button playOnlineButton, playOfflineButton;
        public bool _destroyed;
        public int OnlineStarts, OfflineStarts;
        public bool ThrowOnStart;
        public static Func<LandingZonePanel, bool> OfflinePrefix, OnlinePrefix;
        public void OnPlayOfflineClicked() { if (OfflinePrefix(this)) NativeStart(false); }
        public void OnPlayOnlineClicked() { if (OnlinePrefix(this)) NativeStart(true); }
        private void NativeStart(bool online)
        {
            if (ClientAppState.ClientStateManager.CurrentClientAppStateType != ClientAppState.ClientAppStateType.Login ||
                ClientAppState.ClientStateManager.NativeTransition?.Active == true)
                throw new InvalidOperationException("Reproduced native early-start race");
            if (online) OnlineStarts++; else OfflineStarts++;
            playOnlineButton.interactable = playOfflineButton.interactable = false;
            if (ThrowOnStart) throw new InvalidOperationException("Native login failed");
        }
    }
}
namespace Il2CppInterop.Runtime
{
    internal static class IL2CPP
    {
        internal static int FieldLookups;
        internal static IntPtr GetIl2CppClass(string assembly, string ns, string name)
        {
            if (assembly != "LE.dll" || ns != "ClientAppState" || name != "ClientStateManager")
                throw new InvalidOperationException("Wrong native type lookup");
            return (IntPtr)1;
        }
        internal static IntPtr GetIl2CppField(IntPtr type, string field)
        {
            FieldLookups++;
            if (type == IntPtr.Zero || field != "_activeTransition") throw new InvalidOperationException("Wrong native field lookup");
            return (IntPtr)2;
        }
        internal static IntPtr il2cpp_field_get_value_object(IntPtr field, IntPtr instance)
        {
            if (field != (IntPtr)2 || instance != IntPtr.Zero) throw new InvalidOperationException("Expected static native field");
            if (ClientAppState.ClientStateManager.ThrowSnapshotRead) throw new NullReferenceException("Transient native state observation failure");
            return ClientAppState.ClientStateManager.NativeTransition?.Active == true ? (IntPtr)3 : IntPtr.Zero;
        }
    }
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiVisibility
    {
        internal static bool IsVisible(UnityEngine.GameObject element) =>
            element is not null && !element.WasCollected && element.activeInHierarchy && element.Visible;
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        public static List<string> Spoken = new();
        public static bool Throw;
        public static void Speak(string text)
        {
            if (Throw) throw new InvalidOperationException("Speech unavailable");
            Spoken.Add(text);
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public readonly List<string> Messages = new();
        public void LogInfo(string text) => Messages.Add(text);
        public void LogDebug(string text) => Messages.Add(text);
        public void LogWarning(string text) => Messages.Add(text);
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
    }
}

namespace LeAccess.Accessibility.ModMenu
{
    internal static class InputBlock { internal static bool Blocking; }
}
