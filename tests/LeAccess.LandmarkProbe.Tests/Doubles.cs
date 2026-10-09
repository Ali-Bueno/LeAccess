namespace UnityEngine.SceneManagement
{
    internal struct Scene { internal int handle; internal bool isLoaded; internal string name; internal bool IsValid() => handle != 0; }
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
        internal readonly List<GameObject> Children = new();
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
    internal class Component : Object
    {
        internal GameObject gameObject = new();
        internal Transform transform => gameObject.transform;
        internal T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        internal T GetComponentInParent<T>(bool inactive = false) where T : class => gameObject.GetComponentInParent<T>();
        internal T GetComponentInChildren<T>(bool inactive) where T : class => GetComponentsInChildren<T>(inactive).FirstOrDefault();
        internal List<T> GetComponentsInChildren<T>(bool inactive) where T : class
        {
            var result = new List<T>();
            void Read(GameObject root)
            {
                if (!inactive && !root.activeInHierarchy) return;
                result.AddRange(root.Components.OfType<T>());
                foreach (var child in root.Children) Read(child);
            }
            Read(gameObject);
            return result;
        }
    }
    internal sealed class SkinnedMeshRenderer : Component { }
    internal class Behaviour : Component
    {
        internal bool enabled = true;
        internal bool isActiveAndEnabled { get => enabled && gameObject.activeInHierarchy; set => enabled = value; }
    }
}
namespace EHG.Multiplayer
{ internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
internal sealed class Actor : UnityEngine.Behaviour { internal BaseHealth health; internal BaseMana mana; internal bool Dead; internal bool isDying()=>Dead; }
internal sealed class ActorVisuals : UnityEngine.Behaviour { internal ActorSync actorSync; internal bool dead; }
internal class ActorSync : UnityEngine.Behaviour { internal ActorVisuals actorVisuals; }
internal sealed class PlayerActorSync : ActorSync { internal LocalPlayer localPlayer; }
internal sealed class LocalPlayer : UnityEngine.Behaviour
{ internal static LocalPlayer instance; internal ActorSync actorSync; internal BaseHealth playerHealth; internal BaseMana playerMana; }
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
    internal string DialogueName;
    internal static NativeList<WorldObjectClickListener> allListener = new();
    internal HoverTagOnMouseOver hoverTagOnMouseOver;
    internal RelayNPCInteraction _NPCinteraction;
    internal LEConversationTrigger _leConversationTrigger;
    internal float interactionRange = 5;
    internal bool controllerTargetingUsePlayerY, Broken, Allowed = true;
    internal bool CanInteract() { if (Broken) throw new InvalidOperationException("broken listener"); return Allowed; }
}
internal class NPCInteraction : UnityEngine.Behaviour { }
internal sealed class RelayNPCInteraction : NPCInteraction { internal ServiceList<ConditionHandler> otherInteractables = new(); }
internal sealed class ServiceList<T> : List<T> { internal bool WasCollected; }
internal sealed class ConditionHandler : UnityEngine.Behaviour { }
internal sealed class NpcNameTooltip : UnityEngine.Behaviour { }
internal sealed class LEConversationTrigger : UnityEngine.Behaviour { }
internal sealed class OpenShop : UnityEngine.Behaviour { }
internal sealed class OpenStash : UnityEngine.Behaviour { }
internal sealed class OpenMenuInteraction : UnityEngine.Behaviour
{
    internal enum MenuToOpen { Map, Stash, CraftingPanel, MtxShop }
    internal MenuToOpen menu;
}
internal sealed class EnableWaypointInteraction : UnityEngine.Behaviour { }
namespace LeAccess.I18n
{
    internal static class ModText
    {
        internal static string Get(string key) => key switch
        {
            "service.merchant" => "Mercader", "service.stash" => "Alijo",
            "service.forge" => "Forja", "service.waypoint" => "Punto de viaje",
            "exploration.shrine" => "santuario", "exploration.time_rift" => "fisura temporal",
            "exploration.nemesis" => "Némesis", "exploration.chest" => "cofre", _ => key
        };
    }
}
internal sealed class HoverTagOnMouseOver : UnityEngine.Behaviour
{
    internal enum HoverTargetType { ZONE_TRANSITION, NPC, SIGNPOST, OTHER, DUNGEON_TRANSITION, SAME_SCENE_TRANSITION, LOCALIZATION_KEY }
    internal HoverTargetType hoverTargetType = HoverTargetType.SIGNPOST;
    internal string hoverText, localizationKey;
    internal TransitionOrGateInteraction sceneInteraction, dungeonTransitionInteraction;
}
internal class TransitionOrGateInteraction : UnityEngine.Behaviour { internal string sceneName; }
internal sealed class UsePlayerSpawnInteraction : TransitionOrGateInteraction { internal string ToScene; }
internal sealed class CaveLobbyExitInteraction : UnityEngine.Behaviour { }
namespace LE.PCG
{
    internal sealed class Level : UnityEngine.Behaviour { internal UnityEngine.SceneManagement.Scene ParentScene; }
    internal static class LevelUtility
    {
        internal static readonly Dictionary<int, Level> Levels = new();
        internal static bool TryGetLevel(UnityEngine.SceneManagement.Scene scene, out Level level) =>
            Levels.TryGetValue(scene.handle, out level);
    }
}
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

internal sealed class Shrine : UnityEngine.Behaviour { }
internal sealed class ShrineSync : UnityEngine.Behaviour { }
internal sealed class ShrineVisualsCreator : UnityEngine.Behaviour { }
internal sealed class ShrinesManager : UnityEngine.Behaviour
{
    internal static readonly Dictionary<string, ShrinesManager> Scenes = new();
    internal readonly NativeList<UnityEngine.GameObject> Objects = new();
    internal static bool TryGetForScene(string sceneName, out ShrinesManager manager) =>
        Scenes.TryGetValue(sceneName ?? "", out manager);
    internal NativeList<UnityEngine.GameObject> GetAllShrineObjects() => Objects;
}
namespace UnityEngine { internal static class Time { internal static double unscaledTimeAsDouble; } }
internal sealed class TimeRiftInteraction : UnityEngine.Behaviour { }
internal sealed class ActivateTimeRift : UnityEngine.Behaviour { }
internal sealed class TimeBeastRift : UnityEngine.Behaviour { }
internal sealed class MonolithPortal : UnityEngine.Behaviour { }
internal sealed class ShatteredRoadPortalInteractable : UnityEngine.Behaviour { }
internal sealed class NemesisEncounterInteractableObject : UnityEngine.Behaviour { }
internal sealed class OneShotCacheInteractableObject : UnityEngine.Behaviour { }
internal static class Localization
{
    internal static readonly Dictionary<string, string> Texts = new();
    internal static string GetText(string key, string fallback = "") => Texts.TryGetValue(key, out var value) ? value : string.IsNullOrEmpty(fallback) ? key : fallback;
}

namespace LeAccess.Accessibility.Npcs
{
    internal static class NpcNameReader
    {
        internal static string Read(WorldObjectClickListener listener, out string source) { source = "dialogue database"; return listener.DialogueName; }
    }
}
