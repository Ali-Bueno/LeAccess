internal class NativeObject
{
    private static int _nextId;
    public IntPtr NativePointer = new(++_nextId);
    public static int InvalidAccesses;
    public bool WasCollected;
    public IntPtr Pointer { get { Guard(); return NativePointer; } }
    public T TryCast<T>() where T : class { Guard(); return this as T; }
    protected void Guard()
    {
        if (!WasCollected) return;
        InvalidAccesses++;
        throw new InvalidOperationException("Collected wrapper accessed");
    }
}

namespace UnityEngine
{
    internal class Object : NativeObject
    {
        public bool Destroyed;
        public string name = "";
        public int GetInstanceID() { Guard(); return Pointer.ToInt32(); }
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.ToInt32();
    }
    internal class Component : Object
    {
        private GameObject _gameObject;
        public GameObject gameObject { get { Guard(); return _gameObject; } set => _gameObject = value; }
        public Transform transform { get { Guard(); return gameObject.transform; } }
        public bool Enabled = true;
        public bool isActiveAndEnabled { get { Guard(); return Enabled && gameObject.activeInHierarchy; } }
        public T GetComponent<T>() where T : Component { Guard(); return gameObject.GetComponent<T>(); }
        public T[] GetComponents<T>() where T : Component { Guard(); return gameObject.GetComponents<T>(); }
        public T GetComponentInParent<T>() where T : Component
        {
            Guard();
            for (var current = gameObject; current is not null; current = current.Parent)
                if (current.GetComponent<T>() is T found) return found;
            return null;
        }
    }
    internal class Behaviour : Component { }
    internal class MonoBehaviour : Behaviour { }
    internal sealed class Transform : Component
    {
        public Transform parent { get { Guard(); return gameObject.Parent?.transform; } }
        public bool IsChildOf(Transform ancestor)
        {
            Guard();
            for (var current = gameObject; current is not null; current = current.Parent)
                if (ReferenceEquals(current.transform, ancestor)) return true;
            return false;
        }
    }
    internal sealed class GameObject : Object
    {
        public new string name = "Test control";
        public bool Active = true;
        public GameObject Parent;
        public readonly List<Component> Components = new();
        private readonly Transform _transform;
        public Transform transform { get { Guard(); return _transform; } }
        public GameObject() => _transform = new Transform { gameObject = this };
        public bool activeInHierarchy { get { Guard(); return Active && (Parent is null || Parent.activeInHierarchy); } }
        private SceneManagement.Scene _scene = new() { handle = 1, isLoaded = true };
        public SceneManagement.Scene scene { get { Guard(); return _scene; } set => _scene = value; }
        public T Add<T>(T component) where T : Component
        { component.gameObject = this; Components.Add(component); return component; }
        public GameObject Child() => new() { Parent = this, scene = scene };
        public T GetComponent<T>() where T : Component { Guard(); return Components.OfType<T>().FirstOrDefault(); }
        public T[] GetComponents<T>() where T : Component { Guard(); return Components.OfType<T>().ToArray(); }
    }
    internal sealed class Canvas : Component { }
    internal sealed class CanvasGroup : Component { public float alpha = 1; public bool ignoreParentGroups { get; set; } }
    internal static class Time { public static int frameCount; }
    internal static class Application { public static bool isFocused = true; }
}
internal interface INavigable
{
    bool WasCollected { get; }
    IntPtr Pointer { get; }
    T TryCast<T>() where T : class;
}
internal class Navigable : UnityEngine.Behaviour, INavigable
{
    bool INavigable.WasCollected => WasCollected;
    public UnityEngine.UI.Selectable _activeSelectable;
    public bool ExcludeControl;
    public int SelectionCalls;
    public UnityEngine.UI.Selectable SelectedControl;
    public bool ShouldExcludeSelectable(UnityEngine.UI.Selectable selectable) { Guard(); return ExcludeControl; }
    public void TrySelect(UnityEngine.UI.Selectable selectable)
    { Guard(); SelectionCalls++; SelectedControl = _activeSelectable = selectable; }
}
internal sealed class ScreenKeyboardNavigable : Navigable { }
internal sealed class NavigationManager : UnityEngine.Behaviour
{
    public static NavigationManager instance;
    public static INavigable currentlySelected;
    public readonly Dictionary<int, INavigable> Registry = new();
    public bool GetNavigableFromID(int id, out INavigable navigation) { Guard(); return Registry.TryGetValue(id, out navigation); }
}
internal sealed class ScreenKeyboardManager : UnityEngine.Behaviour
{
    public static ScreenKeyboardManager _Instance_k__BackingField;
    public int _previousSelectedNavigableId;
    public UnityEngine.GameObject _selectedGameObject;
    public bool _enabled = true;
}
internal sealed class CharacterSelect : UnityEngine.Behaviour
{
    public enum CharacterSelectState { None, NameCreation, ClassSelection, ExistingCharacter }
    public static CharacterSelect instance;
    public CharacterSelectState currentState = CharacterSelectState.NameCreation;
}
internal static class EpochInputManager { public static bool IsControllerActive = true; }
internal sealed class UIBase : UnityEngine.Behaviour
{
    public static UIBase instance;
    public bool menuOpen;
}
internal static class ControllerMapsManager
{
    public static int Updates;
    public static void UpdateMap() { Updates++; LE.UI.PanelSystem.PanelSystem.Events.Add("map"); }
}
namespace LE.UI.PanelSystem
{
    internal class Panel : UnityEngine.Behaviour { }
    internal sealed class MainMenuPanel : Panel { }
    internal sealed class NativePanelList : NativeObject
    {
        public int Value;
        public int Count { get { Guard(); return Value; } }
    }
    internal sealed class PanelSystem : UnityEngine.Behaviour
    {
        public bool IsAnyPanelOpen;
        public NativePanelList _openingPanels = new();
        public static readonly List<string> Events = new();
        public static readonly List<INavigable> Restored = new();
        public static bool NativeSelects = true;
        public static Action<INavigable> DuringRestore;
        public static void RestoreFocus(INavigable owner)
        {
            Events.Add("restore"); Restored.Add(owner);
            if (NativeSelects) NavigationManager.currentlySelected = owner;
            DuringRestore?.Invoke(owner);
        }
    }
}
namespace UnityEngine.SceneManagement
{
    internal struct Scene
    {
        public int handle;
        public bool isLoaded;
        public bool IsValid() => handle != 0;
    }
    internal static class SceneManager
    {
        public static Scene Current = new() { handle = 1, isLoaded = true };
        public static Scene GetActiveScene() => Current;
    }
}
namespace UnityEngine.UI
{
    internal class Selectable : Component
    {
        public bool Interactable = true;
        public bool IsInteractable() { Guard(); return Interactable; }
    }
    internal sealed class Button : Selectable { }
}
namespace TMPro
{
    internal sealed class TMP_InputField : UnityEngine.UI.Selectable { public string text = "Ariadna"; }
}
namespace LE.UI
{
    internal static class LoadingScreen { public static bool IsVisible; }
}
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public readonly List<string> Messages = new();
        public void LogInfo(object message) => Messages.Add(message.ToString());
        public void LogDebug(object message) => Messages.Add(message.ToString());
        public void LogWarning(object message) => Messages.Add(message.ToString());
        public void LogError(object message) => Messages.Add(message.ToString());
    }
}
