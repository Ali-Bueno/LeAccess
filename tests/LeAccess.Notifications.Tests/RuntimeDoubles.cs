using System.Collections;

namespace UnityEngine
{
    public class Object
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public bool WasCollected;
        public bool Destroyed;
        public int GetInstanceID() { Guard(); return _id; }
        private void Guard()
        {
            if (WasCollected) throw new InvalidOperationException("Collected wrapper accessed");
        }
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => _id;
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool Enabled = true;
        public bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() where T : Component => gameObject.Components.OfType<T>().ToArray();
        public T GetComponentInParent<T>() where T : Component
        {
            for (var current = gameObject; current is not null; current = current.Parent)
            {
                var component = current.GetComponent<T>();
                if (component is not null) return component;
            }
            return null;
        }
    }

    public sealed class Transform : Component
    {
        public Transform parent => gameObject.Parent?.transform;
        public int childCount => gameObject.Children.Count;
        public Transform GetChild(int index) => gameObject.Children[index].transform;
        public bool IsChildOf(Transform ancestor)
        {
            for (var current = gameObject; current is not null; current = current.Parent)
                if (ReferenceEquals(current.transform, ancestor)) return true;
            return false;
        }
    }

    public class GameObject : Object
    {
        public bool Active = true;
        public GameObject Parent;
        public readonly List<Component> Components = new();
        public readonly List<GameObject> Children = new();
        public readonly Transform transform;
        public GameObject() => transform = new Transform { gameObject = this };
        public bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        public T Add<T>(T component) where T : Component
        {
            component.gameObject = this;
            Components.Add(component);
            return component;
        }
        public GameObject Child()
        {
            var child = new GameObject { Parent = this };
            Children.Add(child);
            return child;
        }
        public T GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
        public T GetComponentInChildren<T>() where T : Component => GetComponentsInChildren<T>(false).FirstOrDefault();
        public NativeArray<T> GetComponentsInChildren<T>(bool inactive) where T : Component
        {
            var result = new List<T>();
            if (inactive || activeInHierarchy)
            {
                result.AddRange(Components.OfType<T>());
                foreach (var child in Children) result.AddRange(child.GetComponentsInChildren<T>(inactive));
            }
            return new NativeArray<T>(result);
        }
    }
    internal sealed class Canvas : Component { }
    internal sealed class CanvasGroup : Component { public float alpha = 1; public bool ignoreParentGroups; }
    internal sealed class CanvasRenderer : Object { public bool cull; }
    internal struct Color { public float a; }
    internal static class Time { public static int frameCount; }
    internal static class Application { public static bool isFocused = true; }
}

public sealed class NativeArray<T> : IEnumerable<T>
{
    private readonly List<T> _items;
    public bool WasCollected => false;
    public NativeArray(List<T> items) => _items = items;
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
namespace TMPro
{
    internal class TMP_Text : UnityEngine.Component
    {
        public string text;
        public float alpha = 1;
        public UnityEngine.CanvasRenderer canvasRenderer = new();
    }
    internal class TextMeshProUGUI : TMP_Text { }
    internal sealed class TMP_InputField : UnityEngine.Component { }
}
namespace UnityEngine.UI
{
    internal sealed class Text : UnityEngine.Component
    {
        public string text;
        public UnityEngine.Color color = new() { a = 1 };
    }
    internal sealed class InputField : UnityEngine.Component { }
}
internal sealed class ControllerNotification : UnityEngine.Component { public UnityEngine.Component notification; }
internal sealed class UIQuickNotificationText : UnityEngine.Component { public TMPro.TMP_Text _label; }
internal sealed class OverChatNotification : UnityEngine.Component { public UnityEngine.Component notification; }
internal sealed class LocationNotificationManager : UnityEngine.Component { public UnityEngine.GameObject holder; }
namespace LeAccess.Accessibility.Zones
{
    internal static class ZoneAnnouncer
    {
        public static LocationNotificationManager LastTracked;
        public static void TrackLocation(LocationNotificationManager notification) => LastTracked = notification;
    }
}
internal sealed class Notifications : UnityEngine.Component
{
    public static Notifications instance;
    public UnityEngine.GameObject holder;
}
namespace LE.UI.NotificationSystem
{
    internal class NotificationBase : UnityEngine.Component { }
    internal sealed class RageOfMorditasOutOfGauntletNotification : NotificationBase
    {
        internal TMPro.TextMeshProUGUI _countdownText;
        internal CountdownCoroutine _currentCountdownCoroutine = new();
    }
    internal sealed class CountdownCoroutine
    {
        private static long _nextId;
        internal IntPtr Pointer = new(++_nextId);
        internal bool WasCollected;
    }
}
internal sealed class EpochInputManager : UnityEngine.Component
{
    internal static EpochInputManager instance;
    internal bool Typing, ThrowOnQuery;
    internal bool isInputFieldActive => ThrowOnQuery ? throw new InvalidOperationException("Input query failed") : Typing;
    public static void SetActiveVirtualMouse(bool active) { }
}
internal sealed class ScreenKeyboardManager : UnityEngine.Component
{
    internal static ScreenKeyboardManager Instance;
    internal bool Enabled, ThrowOnQuery;
    internal bool _enabled => ThrowOnQuery ? throw new InvalidOperationException("Keyboard query failed") : Enabled;
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { internal static bool IsSuspended; }
    internal static class GameplayInputState
    {
        internal static bool Owns = true, ThrowOnQuery;
        internal static bool OwnsInput() => ThrowOnQuery ? throw new InvalidOperationException("Ownership query failed") : Owns;
    }
}
namespace LE.UI { internal static class LoadingScreen { public static bool IsVisible; } }
namespace LeAccess.Accessibility.ScreenContent
{
    internal static class ScreenContentAnnouncer
    {
        public static bool HasActiveContent, GenericWindowDelivered;
        public static bool BlocksNotifications => HasActiveContent && !GenericWindowDelivered;
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        public static bool Available = true;
        public static bool OutputAccepted = true;
        public static bool SpokeThisFrame;
        public static readonly List<(string Text, bool Interrupt)> Messages = new();
        public static bool IsAvailable() => Available;
        public static void Speak(string text, bool interrupt) => TrySpeak(text, interrupt);
        public static bool TrySpeak(string text, bool interrupt = true)
        {
            if (!Available || !OutputAccepted) return false;
            Messages.Add((text, interrupt)); return true;
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public readonly List<string> Warnings = new();
        public void LogInfo(string message) { }
        public void LogWarning(string message) => Warnings.Add(message);
    }
}
