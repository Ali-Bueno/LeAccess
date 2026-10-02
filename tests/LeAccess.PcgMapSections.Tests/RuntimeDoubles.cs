internal class NativeObject
{
    private static int next;
    internal IntPtr Pointer = new(++next);
    internal bool WasCollected;
}
internal class NativeArray<T> : NativeObject, IEnumerable<T>
{
    internal T[] Values;
    internal NativeArray(params T[] values) => Values = values;
    internal int Length => Values.Length;
    internal T this[int i] { get => Values[i]; set => Values[i] = value; }
    public IEnumerator<T> GetEnumerator()=>((IEnumerable<T>)Values).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
}
namespace UnityEngine
{
    internal class Object : NativeObject { internal bool Destroyed; }
    internal class Transform : Object
    {
        internal Transform parent;
        internal bool IsChildOf(Transform owner)
        { for (var t = this; t is not null; t = t.parent) if (t == owner) return true; return false; }
    }
    internal class GameObject : Object
    {
        internal bool activeInHierarchy = true;
        internal SceneManagement.Scene scene;
        internal Transform transform = new();
        internal AI.NavMeshAgent Agent;
        internal T GetComponentInChildren<T>() where T : class => Agent as T;
    }
    internal struct Vector3
    { internal float x,y,z; internal Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; } }
    internal struct Matrix4x4
    {
        internal float[] Values;
        internal float this[int i] => Values[i];
        internal float determinant => Values[0]*Values[5]*Values[10];
        internal Matrix4x4 inverse => Diagonal(1/Values[0],1/Values[5],1/Values[10],-Values[12]/Values[0],-Values[13]/Values[5],-Values[14]/Values[10]);
        internal static Matrix4x4 Diagonal(float x=1,float y=1,float z=1,float tx=0,float ty=0,float tz=0)
        { var a=new float[16];a[0]=x;a[5]=y;a[10]=z;a[15]=1;a[12]=tx;a[13]=ty;a[14]=tz;return new(){Values=a}; }
        internal Vector3 MultiplyPoint3x4(Vector3 p) => new(p.x*Values[0]+Values[12],p.y*Values[5]+Values[13],p.z*Values[10]+Values[14]);
    }
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene
    { internal int handle;internal string name,path;internal bool isLoaded,Valid;internal bool IsValid()=>Valid; }
    internal static class SceneManager { internal static Scene Scene;internal static Scene GetActiveScene()=>Scene; }
}
namespace UnityEngine.AI
{
    internal class NavMeshAgent : UnityEngine.Object
    { internal UnityEngine.GameObject gameObject;internal UnityEngine.Transform transform=>gameObject.transform;internal bool isActiveAndEnabled=true;internal float baseOffset;internal int agentTypeID; }
    internal struct NavMeshBuildSettings { internal float agentClimb; }
    internal static class NavMesh { internal static float Climb;internal static NavMeshBuildSettings GetSettingsByID(int id)=>new(){agentClimb=Climb}; }
}
namespace Unity.Mathematics
{
    internal struct int2 { internal int x,y;internal int2(int x,int y){this.x=x;this.y=y;} }
    internal struct float3 { internal float x,y,z;internal float3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
}
namespace LE.PCG.Data.Tiles
{
    internal enum TileKey { Empty,Floor }
    internal struct TileHash { internal TileKey BottomLeft,BottomRight,TopRight,TopLeft; }
    internal class Tileset : NativeObject { internal Unity.Mathematics.float3 TileSize=new(1,1,1); }
    internal class BakedTilemap : NativeObject
    {
        internal Unity.Mathematics.int2 Size=new(4,4);
        internal static NativeArray<Unity.Mathematics.int2> hashPosLookup=new(new(0,0),new(1,0),new(1,1),new(0,1));
        internal NativeArray<float> Heights=new(new float[16]);
        internal TileHash Hash=new(){BottomLeft=TileKey.Floor,BottomRight=TileKey.Floor,TopRight=TileKey.Floor,TopLeft=TileKey.Floor};
        internal bool InBounds(Unity.Mathematics.int2 p)=>p.x>=0&&p.y>=0&&p.x<Size.x&&p.y<Size.y;
        internal TileHash GetTileHash(Unity.Mathematics.int2 p)=>Hash;
    }
}
namespace LE.PCG
{
    internal enum LevelArchetype { None,Tomb,Cave }
    internal struct IntRect { internal Unity.Mathematics.int2 min,size; }
    internal class GrammarMetadata : NativeObject { internal bool IsEntrance,IsExit; }
    internal struct BakedDoor { internal int Index,Size;internal Unity.Mathematics.int2 Position;internal bool Occupied; }
    internal class BakedRoom : NativeObject { internal IntRect Rect;internal GrammarMetadata GrammarMetadata=new();internal NativeArray<BakedDoor> Doors;internal Data.Rooms.RoomTemplate Template=new(); }
    internal class BakedLevel : NativeObject
    { internal uint Seed=1;internal NativeArray<BakedRoom> Rooms;internal Data.Tiles.BakedTilemap Tilemap=new();internal Data.Tiles.Tileset Tileset=new(); }
    internal class GeneratorData : NativeObject { internal BakedLevel Level; }
    internal class Level : UnityEngine.Object
    { internal bool isActiveAndEnabled=true;internal bool IsLevelGenerationCompleted=true;internal UnityEngine.SceneManagement.Scene RootScene;internal GeneratorData GeneratorData;internal UnityEngine.Matrix4x4 LevelToWorld=UnityEngine.Matrix4x4.Diagonal();internal LevelArchetype Archetype=LevelArchetype.Cave; }
    internal static class LevelUtility
    { internal static Level Level;internal static bool Found=true;internal static bool Throw;internal static bool TryGetLevel(UnityEngine.SceneManagement.Scene scene,out Level level){if(Throw)throw new Exception("native failure");level=Level;return Found;} }
}
namespace LE.PCG.Data
{
    internal static class Constants { internal static int ROOM_TAG_HALLWAY=0; }
}
namespace LE.PCG.Data.Rooms
{
    internal class RoomTemplate : NativeObject { internal RoomTagMask TagMask=new(); }
    internal struct RoomTagMask
    {
        internal static int TAG_COUNT=64;
        internal ulong Value;
        internal bool Throw;
        internal bool HasTag(int tag) { if(Throw)throw new Exception("tag unavailable"); return (Value & (1UL<<tag))!=0; }
    }
}
internal static class PlayerFinder { internal static UnityEngine.GameObject player; }
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationObjectLifetime
    { internal static bool IsAlive(UnityEngine.Object value)=>value is not null&&!value.WasCollected&&!value.Destroyed; }
}
namespace LeAccess.Accessibility.MapSections
{
    internal readonly record struct MapSectionWorldSnapshot(long Session,int SceneHandle,string SceneName,string ScenePath,double X,double Y,double Z,double BodyRadius,int? Variant);
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log=new(); }
    internal class Logger { internal List<string> Warnings=new();internal void LogWarning(string message)=>Warnings.Add(message); }
}
