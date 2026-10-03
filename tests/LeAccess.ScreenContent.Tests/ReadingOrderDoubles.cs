namespace UnityEngine
{
    internal partial class Object { internal T TryCast<T>() where T : class => this as T; }
    internal partial class Transform
    {
        internal Transform parent => gameObject.Parent?.transform;
        internal int GetSiblingIndex() => gameObject.Parent?.Children.IndexOf(gameObject) ?? 0;
    }
    internal partial class GameObject
    {
        internal T GetComponentInParent<T>() where T : Component => transform.GetComponentInParent<T>();
    }
    internal struct Rect
    {
        internal float xMin, yMin, width, height;
        internal float xMax => xMin + width;
        internal float yMax => yMin + height;
        internal Rect(float x, float y, float w, float h) { xMin = x; yMin = y; width = w; height = h; }
    }
    internal sealed class RectTransform : Transform { internal Rect Bounds; }
    internal enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    internal sealed class Canvas : Component
    {
        internal bool overrideSorting;
        internal RenderMode renderMode;
        internal int renderOrder, sortingOrder, sortingLayerID;
    }
    internal static class SortingLayer { internal static int GetLayerValueFromID(int id) => id; }
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiScreenGeometry
    {
        internal static bool TryGetScreenRect(UnityEngine.RectTransform rect, out UnityEngine.Rect bounds)
        {
            bounds = rect?.Bounds ?? default;
            return rect != null && bounds.width > 0 && bounds.height > 0;
        }
    }
}
