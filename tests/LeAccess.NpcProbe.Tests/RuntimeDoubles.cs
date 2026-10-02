namespace UnityEngine
{
    internal class Object
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public bool WasCollected { get; set; }
        public int GetInstanceID() { Guard(); return _id; }
        protected void Guard()
        {
            if (WasCollected) throw new InvalidOperationException("Collected wrapper access");
        }
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => _id;
    }
    internal class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled = true;
        public T GetComponent<T>() where T : Component => gameObject.Components.OfType<T>().FirstOrDefault();
        public Registry<T> GetComponentsInChildren<T>(bool inactive) where T : Component
        {
            var result = new Registry<T>();
            if (inactive || gameObject.activeInHierarchy)
            {
                result.AddRange(gameObject.Components.OfType<T>());
                foreach (var child in gameObject.Children) result.AddRange(child.transform.GetComponentsInChildren<T>(inactive));
            }
            return result;
        }
        public T GetComponentInParent<T>(bool inactive = false) where T : Component
        {
            for (var current = gameObject; current is not null; current = current.Parent)
            {
                var component = current.Components.OfType<T>().FirstOrDefault();
                if (component is not null) return component;
            }
            return null;
        }
    }
    internal sealed class GameObject : Object
    {
        public readonly Transform transform;
        public GameObject Parent;
        public readonly List<GameObject> Children = new();
        public GameObject() => transform = new Transform { gameObject = this };
        public readonly List<Component> Components = new();
        public bool activeInHierarchy = true;
        public T Add<T>(T component) where T : Component
        {
            component.gameObject = this;
            Components.Add(component);
            return component;
        }
        public GameObject Child()
        {
            var child = new GameObject { Parent = this };
            Children.Add(child);
            return child;
        }
    }
    internal sealed class Transform : Component
    {
        public string NativeActorName;
        public Vector3 position;
        public Vector3 forward = new(0, 0, 1);
        public Vector3 right = new(1, 0, 0);
    }
    internal sealed class Camera : Component
    {
        public static Camera Current;
        public static int MainReads;
        public static bool ThrowOnMain;
        public static Camera main
        {
            get
            {
                if (ThrowOnMain) throw new InvalidOperationException("Native camera lookup failed");
                MainReads++;
                return Current;
            }
        }
    }
    internal struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
    }
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene { public int handle; }
    internal static class SceneManager
    {
        public static int CurrentHandle = 1;
        public static Scene GetActiveScene() => new() { handle = CurrentHandle };
    }
}
internal sealed class Registry<T> : List<T> { public bool WasCollected { get; set; } }
internal sealed class WorldObjectClickListener : UnityEngine.Component
{
    public static Registry<WorldObjectClickListener> allListener = new();
    public float interactionRange = 5;
    public bool controllerTargetingUsePlayerY { get; set; }
    public RelayNPCInteraction _NPCinteraction { get; set; }
    public LE.Dialog.LEConversationTrigger _leConversationTrigger { get; set; }
    public bool Interactable = true, ThrowOnCanInteract;
    public bool CanInteract()
    {
        if (ThrowOnCanInteract) throw new InvalidOperationException("Invalid native candidate");
        return Interactable;
    }
}
internal class NPCInteraction : UnityEngine.Component { }
internal sealed class RelayNPCInteraction : NPCInteraction { public Registry<ConditionHandler> otherInteractables = new(); }
internal sealed class ConditionHandler : UnityEngine.Component { }
internal sealed class OpenShop : UnityEngine.Component { }
internal sealed class OpenStash : UnityEngine.Component { }
internal sealed class OpenMenuInteraction : UnityEngine.Component
{
    internal enum MenuToOpen { Map, Stash, CraftingPanel, MtxShop }
    internal MenuToOpen menu;
}
internal sealed class EnableWaypointInteraction : UnityEngine.Component { }
internal sealed class ConversationTrigger : LE.Dialog.LEConversationTrigger { }
namespace LE.Dialog
{
    internal class LEConversationTrigger : UnityEngine.Component
    {
        public UnityEngine.Transform conversant;
        public int ConversationID;
        public bool useExternalDb;
        public PixelCrushers.DialogueSystem.DialogueDatabase ExternalDb;
    }
}
internal sealed class NpcNameTooltip : UnityEngine.Component
{
    public string TopText { get; set; }
    public string TopTextKey { get; set; }
    public string BottomText { get; set; }
    public string BottomTextKey { get; set; }
}
internal sealed class ActorVisuals : UnityEngine.Component { public bool dead; }
internal sealed class BaseDisplayInformation : UnityEngine.Component
{
    public string LocalizedName;
    public string GetLocalizedName() => LocalizedName;
}
namespace PixelCrushers.DialogueSystem
{
    internal sealed class DialogueDatabase : UnityEngine.Object
    {
        public Dictionary<int, Conversation> Conversations = new();
        public Dictionary<int, Actor> Actors = new();
        public Conversation GetConversation(int id) { Guard(); return Conversations.GetValueOrDefault(id); }
        public Actor GetActor(int id) { Guard(); return Actors.GetValueOrDefault(id); }
    }
    internal sealed class Conversation
    {
        public bool WasCollected;
        private int _conversantId;
        public int ActorID;
        public int ConversantID
        {
            get { if (WasCollected) throw new InvalidOperationException("Collected conversation"); return _conversantId; }
            set { _conversantId = value; }
        }
    }
    internal sealed class Actor
    {
        public bool WasCollected, IsPlayer;
        private string _name;
        public string Name
        {
            get { if (WasCollected) throw new InvalidOperationException("Collected actor"); return _name; }
            set { _name = value; }
        }
    }
    internal sealed class DialogueSystemController : UnityEngine.Component { public DialogueDatabase initialDatabase; }
    internal static class DialogueManager { public static DialogueSystemController instance; }
    internal sealed class OverrideActorName : UnityEngine.Component
    {
        public static UnityEngine.Transform LastActorTransform;
        public static string GetActorName(UnityEngine.Transform transform)
        {
            LastActorTransform = transform;
            return transform.NativeActorName;
        }
        public string DisplayName;
        public string GetOverrideName() => DisplayName;
    }
}
internal static class PlayerFinder
{
    public static UnityEngine.GameObject player;
    public static ActorVisuals playerActorVisuals;
}
internal static class Localization
{
    public static bool Throw;
    public static readonly Dictionary<string, string> Translations = new();
    public static bool TryGetText(string key, out string text)
    {
        if (Throw) throw new InvalidOperationException("Localization unavailable");
        return Translations.TryGetValue(key, out text);
    }
    public static string GetText(string key, string fallback)
    {
        if (Throw) throw new InvalidOperationException("Localization unavailable");
        return key == "npc.blacksmith" ? "Herrero" : Translations.GetValueOrDefault(key, fallback);
    }
}
namespace LeAccess.I18n
{
    internal static class ModText
    {
        internal static string Get(string key) => key switch
        {
            "service.merchant" => "Mercader", "service.stash" => "Alijo",
            "service.forge" => "Forja", "service.waypoint" => "Punto de viaje", _ => key
        };
    }
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { public static bool IsSuspended; }
    internal static class GameplayInputState
    {
        public static bool Gameplay = true;
        public static bool OwnsInput() => Gameplay;
    }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils { public static string CleanText(string text) => text?.Trim(); }
}
