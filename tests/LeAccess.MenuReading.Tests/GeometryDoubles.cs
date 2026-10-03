namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public sealed class Il2CppStructArray<T> where T : struct
    {
        private readonly T[] _values;
        public Il2CppStructArray(int length) => _values = new T[length];
        public int Length => _values.Length;
        public T this[int i] { get => _values[i]; set => _values[i] = value; }
    }
}
namespace UnityEngine
{
    public struct Vector2 { public float x, y; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public struct Vector4 { public float x, y, z, w; }
    public struct Rect
    {
        public float xMin, yMin, width, height;
        public float xMax => xMin + width;
        public float yMax => yMin + height;
        public Rect(float x, float y, float w, float h) { xMin=x; yMin=y; width=w; height=h; }
    }
    public sealed class RectTransform : Transform
    {
        public Rect Bounds = new(10, 10, 100, 20);
        public Vector3 Offset;
        public float Depth = 1;
        public Rect rect => Bounds;
        public Vector3 TransformPoint(Vector3 point) => new(point.x + Offset.x, point.y + Offset.y, Depth);
        public void GetWorldCorners(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> corners)
        {
            corners[0] = TransformPoint(new(Bounds.xMin, Bounds.yMin, 0));
            corners[1] = TransformPoint(new(Bounds.xMin, Bounds.yMax, 0));
            corners[2] = TransformPoint(new(Bounds.xMax, Bounds.yMax, 0));
            corners[3] = TransformPoint(new(Bounds.xMax, Bounds.yMin, 0));
        }
    }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public sealed class Camera : Component
    {
        public Rect pixelRect = new(0, 0, 1920, 1080);
        public Func<Vector3, Vector3> Projection = point => point;
        public Vector3 WorldToScreenPoint(Vector3 point) => Projection(point);
    }
    public static class RectTransformUtility
    {
        public static Vector2 WorldToScreenPoint(Camera camera, Vector3 point)
        { if (camera != null) point = camera.WorldToScreenPoint(point); return new() { x=point.x, y=point.y }; }
    }
    public static class Screen { public static int width = 1920, height = 1080; }
}
namespace UnityEngine.UI
{
    public sealed class RectMask2D : Component
    { public RectTransform rectTransform => transform.TryCast<RectTransform>(); public Vector4 padding; }
    public sealed class Mask : Component
    {
        public Graphic graphic => GetComponent<Graphic>();
        public RectTransform rectTransform => transform.TryCast<RectTransform>();
        public bool Active = true;
        public bool MaskEnabled() => isActiveAndEnabled && Active;
    }
}
