namespace UnityEngine.SceneManagement
{
    internal struct Scene { internal int handle; internal bool isLoaded; }
    internal static class SceneManager
    {
        internal static Scene Active = new() { handle = 1, isLoaded = true };
        internal static Scene GetActiveScene() => Active;
    }
}
namespace UnityEngine
{
    internal class Object
    {
        private static int _next;
        internal bool WasCollected, Destroyed;
        internal readonly IntPtr Pointer = new(++_next);
        internal int GetInstanceID() => Pointer.ToInt32();
        internal T TryCast<T>() where T : class => this as T;
        public static bool operator ==(Object left, Object right) =>
            (left is null || left.Destroyed) && (right is null || right.Destroyed) || ReferenceEquals(left, right);
        public static bool operator !=(Object left, Object right) => !(left == right);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.ToInt32();
    }
    internal sealed class GameObject : Object
    {
        internal bool activeInHierarchy = true;
        internal SceneManagement.Scene scene = SceneManagement.SceneManager.Active;
        internal Transform transform;
        internal readonly List<Object> Components = new();
        internal T GetComponent<T>() where T : class => Components.OfType<T>().FirstOrDefault();
        internal T GetComponentInParent<T>() where T : class
        {
            for (var t = transform; t != null; t = t.parent)
            { var result = t.gameObject.GetComponent<T>(); if (result != null) return result; }
            return null;
        }
        internal GameObject() => transform = new Transform { gameObject = this };
    }
    internal sealed class Transform
    {
        internal Vector3 position;
        internal GameObject gameObject;
        internal Transform parent;
        internal bool IsChildOf(Transform owner)
        {
            for (var current = this; current is not null; current = current.parent)
                if (ReferenceEquals(current, owner)) return true;
            return false;
        }
    }
    internal struct Vector3
    {
        internal float x, y, z;
        internal float sqrMagnitude => x*x + y*y + z*z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new() { x=a.x-b.x, y=a.y-b.y, z=a.z-b.z };
    }
    internal class Behaviour : Object
    {
        internal GameObject gameObject = new();
        internal Transform transform => gameObject.transform;
        internal bool isActiveAndEnabled = true;
    }
}
namespace EHG.Multiplayer
{ internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
internal sealed class Actor : UnityEngine.Behaviour { internal BaseHealth health; internal BaseMana mana; internal bool Dead; internal bool isDying()=>Dead; }
internal sealed class ActorVisuals : UnityEngine.Behaviour { internal ActorSync actorSync; internal bool dead; }
internal class ActorSync : UnityEngine.Behaviour { }
internal sealed class PlayerActorSync : ActorSync { internal LocalPlayer localPlayer; }
internal sealed class LocalPlayer : UnityEngine.Behaviour
{ internal ActorSync actorSync; internal BaseHealth playerHealth; internal BaseMana playerMana; }
internal sealed class BaseHealth : UnityEngine.Behaviour
{ internal Actor actor; internal float currentHealth = 76.5f; internal int maxHealth = 100; }
internal sealed class BaseMana : UnityEngine.Behaviour
{
    internal float currentMana = 40.5f;
    internal int maxMana = 80;
    internal bool Replaced;
    internal bool manaReplacedByRage() => Replaced;
}
internal static class PlayerFinder
{
    internal static UnityEngine.GameObject player;
    internal static Actor playerActorComponent;
    internal static ActorVisuals playerActorVisuals;
    internal static LocalPlayer Local;
    internal static LocalPlayer getLocalPlayerInMultiplayer() => Local;
}
internal sealed class NativeList<T> : UnityEngine.Object
{
    internal readonly List<T> Items = new();
    internal int Count => Items.Count;
    internal T this[int index] => Items[index];
}
internal sealed class WorldObjectClickListener : UnityEngine.Behaviour
{
    internal static NativeList<WorldObjectClickListener> allListener = new();
    internal HoverTagOnMouseOver hoverTagOnMouseOver;
    internal float interactionRange = 5;
    internal bool controllerTargetingUsePlayerY, Broken, Allowed = true;
    internal bool CanInteract() { if (Broken) throw new InvalidOperationException("broken listener"); return Allowed; }
}
internal sealed class HoverTagOnMouseOver : UnityEngine.Behaviour
{
    internal enum HoverTargetType { ZONE_TRANSITION, NPC, SIGNPOST, OTHER, DUNGEON_TRANSITION, SAME_SCENE_TRANSITION, LOCALIZATION_KEY }
    internal HoverTargetType hoverTargetType = HoverTargetType.SIGNPOST;
    internal string hoverText;
    internal TransitionOrGateInteraction sceneInteraction, dungeonTransitionInteraction;
}
internal sealed class TransitionOrGateInteraction : UnityEngine.Behaviour { internal string sceneName; }
namespace Networking.Multiplayer.Interactables.Portals
{
    internal sealed class PortalVisualController : UnityEngine.Behaviour
    {
        internal HoverTagOnMouseOver hoverTag;
        internal UnityEngine.GameObject mouseListenerObj;
        internal bool Blocked;
        internal bool isNonInteractableByPlayer() => Blocked;
    }
}
internal sealed class SceneDetails : UnityEngine.Object { internal string LocalizedName; }
internal static class SceneList
{
    internal static readonly Dictionary<string,SceneDetails> Scenes = new();
    internal static bool TryGetNonDefaultSceneDetails(string name,out SceneDetails details)=>Scenes.TryGetValue(name,out details);
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        internal static string CleanText(string text)=>text is null ? null :
            System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(text,"<[^>]+>",""),@"\s+"," ").Trim();
    }
}
