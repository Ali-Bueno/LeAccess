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
    internal class RectTransform : Transform { }
    public class GameObject : Object
    {
        internal bool Active = true;
        internal string name { get; set; }
        internal GameObject Parent;
        internal readonly List<Component> Components = new();
        internal readonly List<GameObject> Children = new();
        internal readonly Transform transform;
        internal GameObject() => transform = new Transform { gameObject = this };
        internal bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        internal T Add<T>(T component) where T : Component
        { component.gameObject = this; Components.Add(component); return component; }
        internal GameObject Child()
        { var child = new GameObject { Parent = this }; Children.Add(child); return child; }
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
    internal sealed class Canvas : Component { }
    internal sealed class CanvasGroup : Component { internal float alpha = 1; internal bool ignoreParentGroups { get; set; } }
    internal sealed class CanvasRenderer : Object { internal bool cull { get; set; } }
    internal struct Color { internal float a; }
    internal static class Application { internal static bool isFocused = true; }
}
internal sealed class NativeArray<T> : NativeObject, IEnumerable<T>
{
    private readonly List<T> _items;
    internal NativeArray(IEnumerable<T> items) => _items = items.ToList();
    internal int Length { get { Guard(); return _items.Count; } }
    internal int Count => Length;
    internal T this[int index] { get { Guard(); return _items[index]; } }
    public IEnumerator<T> GetEnumerator() { Guard(); return _items.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
namespace TMPro
{
    internal class TMP_Text : UnityEngine.Component
    {
        internal string text;
        internal float alpha = 1;
        internal UnityEngine.CanvasRenderer canvasRenderer = new();
    }
    internal sealed class TextMeshProUGUI : TMP_Text { }
    internal sealed class TMP_InputField : UnityEngine.Component { }
}
namespace UnityEngine.UI
{
    internal class Selectable : UnityEngine.Component { }
    internal sealed class Text : UnityEngine.Component
    { internal string text { get; set; } internal UnityEngine.Color color = new() { a = 1 }; }
    internal sealed class InputField : UnityEngine.Component { }
}
namespace LE.UI { internal static class LoadingScreen { internal static bool IsVisible; } }
namespace LeAccess.Accessibility.ScreenContent
{ internal static class ScreenContentAnnouncer { internal static bool HasActiveContent, HasPendingContent; internal static UnityEngine.GameObject Root; internal static bool OwnsFocus(UnityEngine.GameObject go) => Root is not null && go.transform.IsChildOf(Root.transform); } }
namespace LeAccess.Accessibility.Dialogue
{ internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true;
        internal static bool SpokeThisFrame;
        internal static readonly List<(string Text, bool Interrupt)> Messages = new();
        internal static bool IsAvailable() => Available;
        internal static bool IsSpeaking() => throw new InvalidOperationException("Unreliable backend status must not gate skills");
        internal static void Speak(string text, bool interrupt)
        { Messages.Add((text, interrupt)); SpokeThisFrame = true; }
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Warnings = new();
        internal void LogInfo(string text) { }
        internal void LogWarning(string text) => Warnings.Add(text);
    }
}
