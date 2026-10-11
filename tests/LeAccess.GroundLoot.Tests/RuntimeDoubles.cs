namespace Il2CppInterop.Runtime.InteropTypes
{
    public class Il2CppObjectBase
    {
        private static long _nextPointer;
        public bool WasCollected;
        private readonly IntPtr _pointer = new(++_nextPointer);
        public IntPtr Pointer { get { Guard(); return _pointer; } }
        protected void Guard()
        {
            if (WasCollected) throw new InvalidOperationException("Collected wrapper accessed");
        }
        public T TryCast<T>() where T : class { Guard(); return this as T; }
    }
}
namespace UnityEngine
{
    public class Object : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public bool Destroyed;
        public int GetInstanceID() { Guard(); return _id; }
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => _id;
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool Enabled = true;
        public bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() where T : Component => gameObject.Components.OfType<T>().ToArray();
        public T GetComponentInParent<T>() where T : Component
        {
            for (var current = gameObject; current is not null; current = current.Parent)
            {
                var component = current.GetComponent<T>();
                if (component is not null) return component;
            }
            return null;
        }
    }
    public class MonoBehaviour : Component { }
    public class Transform : Component { public Transform parent => gameObject.Parent?.transform; }
    public class GameObject : Object
    {
        public bool Active = true;
        public GameObject Parent;
        public readonly List<Component> Components = new();
        public readonly List<GameObject> Children = new();
        public readonly Transform transform;
        public GameObject() => transform = new Transform { gameObject = this };
        public bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        public T Add<T>(T component) where T : Component
        { component.gameObject = this; Components.Add(component); return component; }
        public GameObject Child()
        { var child = new GameObject { Parent = this }; Children.Add(child); return child; }
        public T GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
        public T GetComponentInChildren<T>() where T : Component => GetComponent<T>() ??
            Children.Select(child => child.GetComponentInChildren<T>()).FirstOrDefault(value => value is not null);
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component =>
            !includeInactive && !activeInHierarchy ? Array.Empty<T>() : Components.OfType<T>()
                .Concat(Children.SelectMany(child => child.GetComponentsInChildren<T>(includeInactive))).ToArray();
    }
    public class Canvas : Component { }
    public class CanvasGroup : Component { public float alpha = 1; public bool ignoreParentGroups; }
    public class CanvasRenderer : Object { public bool cull; }
    public struct Color { public float a; }
    public static class Time { public static int frameCount; }
}
namespace TMPro
{
    public class TMP_Text : UnityEngine.Component
    {
        public string text;
        public float alpha = 1;
        public UnityEngine.CanvasRenderer canvasRenderer = new();
    }
    public class TextMeshProUGUI : TMP_Text { }
}
namespace UnityEngine.UI { public class Text : UnityEngine.Component { public string text; public UnityEngine.Color color = new() { a = 1 }; } }
public class ItemData : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase { }
public class ItemDataUnpacked : ItemData { }
public class PickupableGroundLabel : UnityEngine.MonoBehaviour { public TMPro.TextMeshProUGUI itemText; }
public class GroundItemLabel : PickupableGroundLabel { public GroundItemVisuals visuals; public TooltipItem tooltipItem; }
public class GroundItemVisuals : UnityEngine.MonoBehaviour { public ItemDataUnpacked itemData; public uint id; }
public class TooltipItem : UnityEngine.MonoBehaviour
{
    public static TooltipItem highlightedTooltipItem;
    public TooltipItemManager.SlotType slotType;
}
public static class TooltipItemManager { public enum SlotType { GROUND, INVENTORY, EQUIPPED } }
public class Targetable : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
{
    public enum TargetType { Interactable, GroundLabel, Player }
    public TargetType type;
    public UnityEngine.MonoBehaviour item;
}
public class InteractableTargeting : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
{ public Targetable ActiveTarget; }
public class IControllerMap : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase { }
public class DefaultMap : IControllerMap { }
public class InventoryMap : IControllerMap { }
public class ControllerManager : UnityEngine.MonoBehaviour
{
    public static ControllerManager instance;
    public IControllerMap _CurrentMap_k__BackingField;
}
public class EpochInputManager : UnityEngine.MonoBehaviour
{
    public static EpochInputManager instance;
    public InteractableTargeting InteractableTargeting;
    public static bool IsControllerActive = true;
    public static void SetActiveVirtualMouse(bool active) { }
}
public enum NavigableID { NONE, NAVIGATION_MANGER, GROUND_ITEM_LABELS, INVENTORY_CONTAINER, SETTINGS_PANEL }
public class INavigable : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
{
    public NavigableID Id;
    public NavigableID GetNavigableID() { Guard(); return Id; }
}
public static class NavigationManager { public static INavigable currentlySelected; }
public static class PlayerFinder { public static UnityEngine.GameObject player; }
namespace LeAccess.Accessibility.Navigation
{ internal static class NavigationAudioState { public static bool IsSuspended; } }
namespace LeAccess.Accessibility.ScreenContent
{ internal static class ScreenContentAnnouncer { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue
{ internal static class DialogueAnnouncer { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Inventory
{ internal static class InventoryAccessibility { public static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Items
{
    internal static class ItemTooltipReader
    {
        internal static UnityEngine.GameObject Opener;
        internal static ItemData Item;
        internal static string Text;
        internal static TooltipItemManager.SlotType Slot;
        internal static bool TryReadForOpener(UnityEngine.GameObject expectedOpener,
            TooltipItemManager.SlotType expectedSlot, out string text, ItemData expectedItem = null)
        {
            text = null;
            if (!ReferenceEquals(Opener, expectedOpener) || Slot != expectedSlot ||
                !ReferenceEquals(Item, expectedItem)) return false;
            text = Text;
            return !string.IsNullOrWhiteSpace(text);
        }
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true;
        internal static bool Speaking;
        internal static bool SpokeThisFrame;
        internal static readonly List<(string Text, bool Interrupt)> Messages = new();
        internal static bool IsAvailable() => Available;
        internal static bool IsSpeaking() => Speaking;
        internal static void Speak(string text, bool interrupt)
        { Messages.Add((text, interrupt)); SpokeThisFrame = true; }
        internal static bool TrySpeakPickup(string text) { Speak(text, false); return true; }
    }
}
namespace LeAccess
{
    internal static class Plugin { internal static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Warnings = new();
        internal void LogInfo(string message) { }
        internal void LogWarning(string message) => Warnings.Add(message);
    }
}
