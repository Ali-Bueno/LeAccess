namespace UnityEngine
{
    public class Transform { public Vector3 position; public Vector3 forward = Vector3.forward; }
    public class GameObject { public Transform transform = new(); }
    public class Camera { public static Camera main = new(); public Transform transform = new(); }
    public static class Time { public static float deltaTime = 0.2f; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 forward => new(0,0,1);
        public static Vector3 right => new(1,0,0);
        public float sqrMagnitude => x*x+y*y+z*z;
        public Vector3 normalized { get { float length=MathF.Sqrt(sqrMagnitude); return new(x/length,y/length,z/length); } }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 value) => new(-value.x,-value.y,-value.z);
        public static Vector3 operator *(Vector3 value,float factor) => new(value.x*factor,value.y*factor,value.z*factor);
    }
    public static class Mathf
    {
        public static float Clamp01(float value) => Math.Clamp(value,0,1);
        public static float Lerp(float from,float to,float fraction) => from+(to-from)*Clamp01(fraction);
    }
}
public static class PlayerFinder
{
    public static UnityEngine.GameObject Player = new();
    public static bool Throw;
    public static UnityEngine.GameObject getPlayer() => Throw ? throw new InvalidOperationException("Native player unavailable") : Player;
}
public static class NavMeshHelper
{
    public static UnityEngine.Vector3 getFurthestPointTowardsPointWithRayCast(UnityEngine.Vector3 origin,UnityEngine.Vector3 target) => origin;
    public static UnityEngine.Vector3 getFurthestPointTowardsPoint(UnityEngine.Vector3 origin,UnityEngine.Vector3 target) => origin;
}
namespace LeAccess
{
    public static class Plugin { public static bool ContinuousSonarEnabled = true; public static Logger Log = new(); }
    public class Logger
    {
        public void LogInfo(string message) { }
        public void LogDebug(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}
namespace LeAccess.Accessibility.Navigation
{
    public static class NavigationAudioState { public static bool IsSuspended; }
    public static class GameplayInputState { public static bool Owned = true; public static bool OwnsInput() => Owned; }
}
