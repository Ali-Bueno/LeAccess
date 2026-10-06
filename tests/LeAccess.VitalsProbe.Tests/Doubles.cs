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
        internal SceneManagement.Scene scene = SceneManagement.SceneManager.Active;
        internal Transform transform;
        internal GameObject() => transform = new Transform { gameObject = this };
    }
    internal sealed class Transform
    {
        internal GameObject gameObject;
        internal Transform parent;
        internal bool IsChildOf(Transform owner)
        {
            for (var current = this; current is not null; current = current.parent)
                if (ReferenceEquals(current, owner)) return true;
            return false;
        }
    }
    internal class Component : Object
    {
        internal GameObject gameObject = new();
        internal Transform transform => gameObject.transform;
    }
    internal class Behaviour : Component { internal bool isActiveAndEnabled = true; }
}
namespace EHG.Multiplayer
{ internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
internal sealed class Actor : UnityEngine.Behaviour
{ internal BaseHealth health; internal BaseMana mana; internal HealthPotion healthPotion; }
internal class HealthPotionCharges : UnityEngine.Behaviour { internal int currentCharges = 3, maxCharges = 4; }
internal sealed class HealthPotion : HealthPotionCharges { }
internal sealed class ActorVisuals : UnityEngine.Behaviour { internal ActorSync actorSync; }
internal class ActorSync : UnityEngine.Behaviour { internal ActorVisuals actorVisuals; }
internal sealed class PlayerActorSync : ActorSync { internal LocalPlayer localPlayer; }
internal sealed class LocalPlayer : UnityEngine.Behaviour
{
    internal static LocalPlayer instance;
    internal ActorSync actorSync; internal BaseHealth playerHealth; internal BaseMana playerMana;
    internal HealthPotionCharges healthPotion;
}
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
