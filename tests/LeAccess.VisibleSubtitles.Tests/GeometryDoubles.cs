namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    internal sealed class Il2CppStructArray<T> where T : struct
    {
        private readonly T[] _values;
        internal Il2CppStructArray(int length) => _values = new T[length];
        internal int Length => _values.Length;
        internal T this[int i] { get => _values[i]; set => _values[i] = value; }
    }
}
namespace UnityEngine
{
    internal partial class Object
    {
        internal IntPtr Pointer => new(GetInstanceID());
        internal T TryCast<T>() where T : class => this as T;
    }
    internal partial class GameObject { internal Scene scene = new() { handle = 1, isLoaded = true }; }
    internal struct Scene { internal int handle; internal bool isLoaded; }
    internal partial class Transform { internal Vector3 position = new() { z = 1 }; }
    internal struct Vector2 { internal float x, y; }
    internal struct Vector3
    {
        internal float x, y, z;
        internal float sqrMagnitude => x*x + y*y + z*z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new() {x=a.x+b.x,y=a.y+b.y,z=a.z+b.z};
        public static Vector3 operator -(Vector3 a, Vector3 b) => new() {x=a.x-b.x,y=a.y-b.y,z=a.z-b.z};
    }
    internal struct Rect
    {
        internal float xMin, yMin, width, height;
        internal float xMax => xMin + width;
        internal float yMax => yMin + height;
        internal Rect(float x,float y,float w,float h) { xMin=x;yMin=y;width=w;height=h; }
    }
    internal sealed class RectTransform : Transform
    {
        internal Rect Bounds = new(10, 10, 100, 20);
        internal float Depth = 1;
        internal void GetWorldCorners(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> corners)
        {
            corners[0]=new(){x=Bounds.xMin,y=Bounds.yMin,z=Depth};
            corners[1]=new(){x=Bounds.xMin,y=Bounds.yMax,z=Depth};
            corners[2]=new(){x=Bounds.xMax,y=Bounds.yMax,z=Depth};
            corners[3]=new(){x=Bounds.xMax,y=Bounds.yMin,z=Depth};
        }
    }
    internal enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    internal sealed class Canvas : Component
    {
        internal bool isActiveAndEnabled = true;
        internal RenderMode renderMode;
        internal Canvas Root;
        internal Canvas rootCanvas => Root ?? this;
        internal Camera worldCamera;
    }
    internal sealed class Camera : Component
    {
        internal bool isActiveAndEnabled = true;
        internal Rect pixelRect = new(0, 0, 1920, 1080);
        internal Func<Vector3,Vector3> Projection = point => point;
        internal Vector3 WorldToScreenPoint(Vector3 point) => Projection(point);
    }
    internal static class RectTransformUtility
    {
        internal static Vector2 WorldToScreenPoint(Camera camera, Vector3 point)
        {
            if(camera != null)point=camera.WorldToScreenPoint(point);
            return new(){x=point.x,y=point.y};
        }
    }
    internal static class Screen { internal static int width = 1920, height = 1080; }
}
namespace TMPro
{ internal partial class TMP_Text { internal UnityEngine.RectTransform rectTransform = new(); } }
internal static class CameraManager { internal static UnityEngine.Camera Main; internal static UnityEngine.Camera getMainCamera() => Main; }
internal static class PlayerFinder { internal static UnityEngine.GameObject player; }
internal sealed class WorldObjectClickListener : UnityEngine.Component
{
    internal bool isActiveAndEnabled=true, controllerTargetingUsePlayerY;
    internal float interactionRange=5;
}
