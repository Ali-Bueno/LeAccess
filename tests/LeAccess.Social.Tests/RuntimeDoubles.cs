using System.Collections;

public class NativeObject
{
    private static long _nextPointer;
    private readonly IntPtr _pointer = new(++_nextPointer);
    internal bool WasCollected;
    internal IntPtr Pointer { get { Guard(); return _pointer; } }
    protected void Guard()
    {
        if (WasCollected) throw new InvalidOperationException("Collected wrapper accessed");
    }
    internal T TryCast<T>() where T : class { Guard(); return this as T; }
}

namespace UnityEngine
{
    public class Object : NativeObject
    {
        internal bool Destroyed { get; set; }
        internal int GetInstanceID() => Pointer.ToInt32();
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.ToInt32();
    }
    internal class Component : Object
    {
        internal GameObject gameObject;
        internal Transform transform => gameObject.transform;
        internal bool Enabled = true;
        internal bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy;
        internal T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        internal NativeArray<T> GetComponents<T>() where T : Component => new(gameObject.Components.OfType<T>());
        internal NativeArray<T> GetComponentsInChildren<T>(bool inactive = false) where T : Component =>
            gameObject.GetComponentsInChildren<T>(inactive);
        internal T GetComponentInChildren<T>(bool inactive = false) where T : Component =>
            GetComponentsInChildren<T>(inactive).FirstOrDefault();
        internal T GetComponentInParent<T>(bool inactive = false) where T : Component
        {
            for (var current = gameObject; current is not null; current = current.Parent)
            {
                var component = current.GetComponent<T>();
                if (component is not null) return component;
            }
            return null;
        }
    }
    internal class Behaviour : Component { internal bool enabled { get => Enabled; set => Enabled = value; } }
    internal class MonoBehaviour : Behaviour { }
    internal class Transform : Component
    {
        internal Transform parent => gameObject.Parent?.transform;
        internal int childCount => gameObject.Children.Count;
        internal Transform GetChild(int index) => gameObject.Children[index].transform;
        internal bool IsChildOf(Transform ancestor)
        {
            for (var current = gameObject; current is not null; current = current.Parent)
                if (ReferenceEquals(current.transform, ancestor)) return true;
            return false;
        }
    }
    public class GameObject : Object
    {
        internal bool Active = true;
        internal string name { get; set; } = "GameObject";
        internal GameObject Parent;
        internal readonly List<Component> Components = new();
        internal readonly List<GameObject> Children = new();
        internal readonly Transform transform;
        internal GameObject() => transform = new Transform { gameObject = this };
        internal bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        internal T Add<T>(T component) where T : Component
        { component.gameObject = this; Components.Add(component); return component; }
        internal GameObject Child(string childName = "GameObject")
        { var child = new GameObject { Parent = this, name = childName }; Children.Add(child); return child; }
        internal T GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
        internal T GetComponentInParent<T>(bool inactive = false) where T : Component => transform.GetComponentInParent<T>(inactive);
        internal T GetComponentInChildren<T>(bool inactive = false) where T : Component =>
            GetComponentsInChildren<T>(inactive).FirstOrDefault();
        internal NativeArray<T> GetComponentsInChildren<T>(bool inactive = false) where T : Component
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
    internal sealed class Canvas : Behaviour { }
    internal sealed class CanvasGroup : Behaviour { internal float alpha = 1; internal bool ignoreParentGroups { get; set; } }
    internal sealed class CanvasRenderer : Object { internal bool cull { get; set; } }
    internal struct Color { internal float a; }
    internal static class Time { internal static int frameCount; }
    internal static class Application { internal static bool isFocused = true; }
}
internal sealed class NativeArray<T> : NativeObject, IEnumerable<T>
{
    private readonly List<T> _items;
    internal NativeArray(IEnumerable<T> items) => _items = items.ToList();
    internal int Length { get { Guard(); return _items.Count; } }
    public IEnumerator<T> GetEnumerator() { Guard(); return _items.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
namespace UnityEngine.UI
{
    internal class Graphic : Behaviour { }
    internal class Selectable : Behaviour
    {
        internal bool interactable = true;
        internal bool IsInteractable() => interactable;
    }
    internal sealed class Button : Selectable { }
    internal sealed class Toggle : Selectable { internal bool isOn; }
    internal sealed class Text : Graphic
    { internal string text { get; set; } internal UnityEngine.Color color = new() { a = 1 }; }
}
namespace TMPro
{
    internal class TMP_Text : UnityEngine.UI.Graphic
    {
        internal string text;
        internal float alpha = 1;
        internal UnityEngine.CanvasRenderer canvasRenderer = new();
    }
    internal sealed class TextMeshProUGUI : TMP_Text { }
    internal sealed class TMP_InputField : UnityEngine.UI.Selectable
    {
        internal string text = "";
        internal TMP_Text textComponent;
        internal UnityEngine.UI.Graphic placeholder;
    }
    internal class TMP_Dropdown : UnityEngine.UI.Selectable { internal TMP_Text captionText; }
    internal sealed class TMP_LocalizedDropdown : TMP_Dropdown { }
}
namespace LE.UI { internal static class LoadingScreen { internal static bool IsVisible; } }
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true;
        internal static bool OutputAccepted = true;
        internal static bool SpokeThisFrame;
        internal static readonly List<(string Text, bool Interrupt)> Messages = new();
        internal static bool IsAvailable() => Available;
        internal static void Speak(string text, bool interrupt) => TrySpeak(text, interrupt);
        internal static bool TrySpeak(string text, bool interrupt)
        {
            if (!Available || !OutputAccepted) return false;
            Messages.Add((text, interrupt)); SpokeThisFrame = true; return true;
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Warnings = new();
        internal readonly List<string> Information = new();
        internal void LogInfo(string text) => Information.Add(text);
        internal void LogWarning(string text) => Warnings.Add(text);
    }
}
namespace BepInEx.Logging
{
    internal enum LogLevel { None = 0, All = 63 }
    internal sealed class LogEventArgs : System.EventArgs { internal object Data { get; init; } }
    internal interface ILogListener : System.IDisposable
    {
        LogLevel LogLevelFilter { get; }
        void LogEvent(object sender, LogEventArgs eventArgs);
    }
    internal static class Logger { internal static readonly List<ILogListener> Listeners = new(); }
}
