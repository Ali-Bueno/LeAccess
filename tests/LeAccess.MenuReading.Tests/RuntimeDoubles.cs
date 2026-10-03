using System.Collections;

public class NativeObject
{
    private static int _next;
    public bool WasCollected;
    public IntPtr Pointer { get; } = new(++_next);
    public T TryCast<T>() where T : class => this as T;
}
public sealed class NativeArray<T> : NativeObject, IEnumerable<T>
{
    private readonly List<T> _items;
    public NativeArray(IEnumerable<T> items) => _items = items.ToList();
    public int Length => _items.Count;
    public T this[int index] => _items[index];
    public bool Contains(T value) => _items.Contains(value);
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
namespace UnityEngine
{
    public class Object : NativeObject
    {
        public bool Destroyed;
        public int GetInstanceID() { Guard(); return Pointer.ToInt32(); }
        private void Guard() { if (WasCollected) throw new InvalidOperationException("Collected wrapper accessed"); }
        public static bool operator ==(Object a, Object b)
        {
            a?.Guard(); b?.Guard();
            return (a is null || a.Destroyed) && (b is null || b.Destroyed) || ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.ToInt32();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public string name => gameObject.name;
        public bool Enabled = true;
        public bool isActiveAndEnabled => Enabled && gameObject.activeInHierarchy;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public NativeArray<T> GetComponents<T>() where T : Component => new(gameObject.Components.OfType<T>());
        public NativeArray<T> GetComponentsInChildren<T>(bool inactive = false) where T : Component => gameObject.GetComponentsInChildren<T>(inactive);
        public T GetComponentInChildren<T>() where T : Component => GetComponentsInChildren<T>().FirstOrDefault();
        public T GetComponentInParent<T>() where T : Component
        {
            for (var current = gameObject; current is not null; current = current.Parent)
                if (current.GetComponent<T>() is T component) return component;
            return null;
        }
    }
    public class Behaviour : Component { }
    public class Transform : Component
    {
        public Transform parent => gameObject.Parent?.transform;
        public int childCount => gameObject.Children.Count;
        public Transform GetChild(int index) => gameObject.Children[index].transform;
        public int GetSiblingIndex() => gameObject.Parent?.Children.IndexOf(gameObject) ?? 0;
        public bool IsChildOf(Transform ancestor)
        {
            for (var current = gameObject; current is not null; current = current.Parent)
                if (ReferenceEquals(current.transform, ancestor)) return true;
            return false;
        }
    }
    public class GameObject : Object
    {
        public string name = "Control";
        public bool Active = true;
        public GameObject Parent;
        public readonly List<Component> Components = new();
        public readonly List<GameObject> Children = new();
        public readonly Transform transform;
        public GameObject(bool rectangular = false) => transform = rectangular
            ? new RectTransform { gameObject = this } : new Transform { gameObject = this };
        public bool activeInHierarchy => Active && (Parent is null || Parent.activeInHierarchy);
        public T Add<T>(T value) where T : Component { value.gameObject = this; Components.Add(value); return value; }
        public GameObject Child(string label = "Control")
        {
            var child = new GameObject { Parent = this, name = label }; Children.Add(child); return child;
        }
        public T GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
        public T GetComponentInParent<T>() where T : Component => transform.GetComponentInParent<T>();
        public T GetComponentInChildren<T>() where T : Component => GetComponentsInChildren<T>().FirstOrDefault();
        public NativeArray<T> GetComponentsInChildren<T>(bool inactive = false) where T : Component
        {
            var result = new List<T>();
            if (inactive || activeInHierarchy)
            {
                result.AddRange(Components.OfType<T>());
                foreach (var child in Children) result.AddRange(child.GetComponentsInChildren<T>(inactive));
            }
            return new NativeArray<T>(result);
        }
    }
    public sealed class Canvas : Component
    {
        public Canvas Root;
        public Canvas rootCanvas => Root ?? this;
        public RenderMode renderMode;
        public bool overrideSorting;
        public Camera worldCamera;
    }
    public sealed class CanvasGroup : Component { public float alpha = 1; public bool ignoreParentGroups; }
    public sealed class CanvasRenderer : Object { public bool cull; public float Alpha = 1; public float GetAlpha() => Alpha; }
    public struct Color { public float a; }
    public static class Time { public static int frameCount; public static float unscaledTime; }
    public static class Application { public static bool isFocused = true; }
    public static class Mathf
    {
        public static int Min(int a, int b) => Math.Min(a,b);
        public static int RoundToInt(float value) => (int)Math.Round(value);
    }
}
namespace TMPro
{
    public class TMP_Text : UnityEngine.UI.MaskableGraphic
    { public string text; public float alpha { get => color.a; set => color = new() { a = value }; } }
    public class TextMeshProUGUI : TMP_Text { }
    public class TMP_InputField : UnityEngine.UI.Selectable { public string text; public UnityEngine.Component placeholder; }
    public class TMP_Dropdown : UnityEngine.UI.Selectable { public TMP_Text captionText; }
}
namespace UnityEngine.UI
{
    public class Graphic : Component
    {
        public Color color = new() { a = 1 };
        public CanvasRenderer canvasRenderer = new();
        public RectTransform rectTransform => transform.TryCast<RectTransform>();
        public Canvas canvas => GetComponentInParent<Canvas>();
    }
    public class MaskableGraphic : Graphic { public bool maskable = true; }
    public class Selectable : Component { public bool Interactable = true; public bool IsInteractable() => Interactable; }
    public class Toggle : Selectable { public bool isOn; public Component group; }
    public class Slider : Selectable { public float value; }
    public class Button : Selectable { }
    public class InputField : Selectable { }
    public class Text : MaskableGraphic { public string text; }
}
namespace LE.Settings
{
    public class BoolSettingUI : UnityEngine.Component { public UnityEngine.GameObject _settingContainer; }
    public class NumericSettingUI : UnityEngine.Component { public UnityEngine.GameObject _settingContainer; }
    public class SpinnerSettingUI : UnityEngine.Component { public UnityEngine.GameObject _settingContainer; }
    public class EnumToggleSettingUI : UnityEngine.Component { public UnityEngine.GameObject _settingContainer; }
    public class EnumSettingUI : UnityEngine.Component { public UnityEngine.GameObject _settingContainer; }
    public class StringDropdownSettingUI : UnityEngine.Component { public UnityEngine.GameObject _settingContainer; }
}
namespace LeAccess.Accessibility.Settings
{
    internal readonly record struct SettingsFocusSnapshot(int OwnerId, int TabId, UnityEngine.GameObject Selected, string TabName, string TabSummary, UnityEngine.GameObject Panel = null);
    internal static class SettingsFocusReader
    {
        internal static SettingsFocusSnapshot? Current;
        internal static string Details;
        internal static bool TryRead(out SettingsFocusSnapshot snapshot)
        { snapshot = Current.GetValueOrDefault(); return Current.HasValue; }
        internal static bool TryReadTabDetails(out string text) { text = Details; return !string.IsNullOrWhiteSpace(text); }
    }
}
namespace ChatView { public class ChatToggleField : UnityEngine.Component { public bool IsActive; } }
namespace UnityEngine.UI.Extensions.ColorPicker { public class ColorPickerControl : Component { } }
public class InfoForTooltip : UnityEngine.Component
{
    public TooltipInfo tooltipInfo;
    public int SetTextCalls;
    public void SetText() { SetTextCalls++; }
}
public class NumberFloatInput : UnityEngine.Component { public TMPro.TMP_InputField _input; }
public class SliderInput : UnityEngine.Component
{
    public UnityEngine.UI.Slider _slider;
    public NumberFloatInput _numberInput;
}
public class TooltipInfo { public string nameText, mainText; }
public class CharacterCreationSelection : UnityEngine.Component { public CharacterClass characterClass; }
public class CharacterClass { public string className; }
public class MasteryPanel : UnityEngine.Component { }
public class LocalCharacterSlots : UnityEngine.Component { }
public class CharacterSelect : UnityEngine.Component { }
public interface INavigable
{
    bool WasCollected { get; }
    IntPtr Pointer { get; }
    T TryCast<T>() where T : class;
    int GetNavigableID();
}
public class Navigable : UnityEngine.Behaviour, INavigable
{
    bool INavigable.WasCollected => WasCollected;
    public int Id;
    public UnityEngine.UI.Selectable ActiveSelectable;
    public int GetNavigableID() => Id;
}
public class CustomNavigable : UnityEngine.Behaviour, INavigable
{
    bool INavigable.WasCollected => WasCollected;
    public int GetNavigableID() => 99;
}
public class VerticalNavigable : CustomNavigable
{
    public NativeArray<UnityEngine.UI.Button> selectables;
    public int currentlySelectedIdx;
}
public class CraftingMaterialsPanelSearchInputFieldNavigable : CustomNavigable
{
    public TMPro.TMP_InputField inputField;
}
public class NavigableMediator : CustomNavigable
{
    public LE.UI.PanelSystem.Panel FocusedPanel;
}
namespace LE.UI.PanelSystem { public class Panel : UnityEngine.Behaviour { } }
public static class NavigationManager { public static INavigable currentlySelected; }
public static class PlayerFinder { public static UnityEngine.GameObject Player; public static UnityEngine.GameObject getPlayer() => Player; }
public class MultiButtonsActionPrompt : UnityEngine.Component
{
    public bool CanUse = true;
    public TMPro.TMP_Text _promptDescription;
    public NativeArray<int> _activePromptNavigables;
    public bool _exceptSelectedNavigables, PromptActive = true;
    public bool IsPromptActive() => PromptActive;
    public Il2CppSystem.Collections.Generic.List<ButtonInfo> Buttons;
    public bool ThrowButtons;
    public int ButtonReads;
    public Il2CppSystem.Collections.Generic.List<ButtonInfo> GetButtonsInfo()
    { ButtonReads++; if (ThrowButtons) throw new InvalidOperationException("Native prompt bindings unavailable"); return Buttons; }
}
public class ButtonInfo : NativeObject
{
    public string Name;
    public bool IsHoldAction, ThrowName;
    public string ButtonName => ThrowName ? throw new InvalidOperationException("Native button unavailable") : Name;
}
public class PromptsInfoProvider : UnityEngine.Component
{
    public static PromptsInfoProvider Instance;
    public bool Throw;
    public List<(string Action, bool Keyboard)> Requests = new();
    public Dictionary<(string, bool), Il2CppSystem.Collections.Generic.List<ButtonInfo>> Mappings = new();
    public Il2CppSystem.Collections.Generic.List<ButtonInfo> GetButtonsInfo(string action, bool keyboard)
    { Requests.Add((action, keyboard)); if (Throw) throw new InvalidOperationException("Native provider unavailable"); return Mappings.GetValueOrDefault((action, keyboard)); }
}
namespace Il2CppSystem.Collections.Generic
{
    public class List<T> : NativeObject, IEnumerable<T>
    {
        public readonly System.Collections.Generic.List<T> Values;
        public List(params T[] values) => Values = values.ToList();
        public int Count => Values.Count;
        public T this[int index] => Values[index];
        public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
namespace Rewired { public static class ReInput { public static bool isReady = true; } }
namespace LE.UI { public static class LoadingScreen { public static bool IsVisible; } }
namespace LeAccess.Accessibility.Navigation { public static class GameplayInputState { public static bool Owns = true; public static bool OwnsInput() => Owns; } }
namespace LeAccess.Accessibility.Map
{
    internal static class WaypointReader
    { internal static bool TryRead(UnityEngine.GameObject go, bool includeDetails, out string text) { text = null; return false; } }
}
namespace LeAccess.Accessibility.CharacterCreation
{
    internal readonly record struct ClassSelection(string Name, string Description);
    internal static class ClassSelectionReader
    { internal static bool TryReadControl(UnityEngine.GameObject go, out ClassSelection selection) { selection = default; return false; } }
    internal static class CharacterTileReader
    { internal static bool TryRead(UnityEngine.GameObject go, out string text) { text = null; return false; } }
    internal static class ClassCarouselHandler
    { internal static bool TryHandleFocus(UnityEngine.GameObject go) => false; }
    public static class MasteryReader
    { public static bool TryGetMasteryInfo(UnityEngine.GameObject go, out string name, out string description) { name = description = null; return false; } }
}
namespace LeAccess.Accessibility.ScreenContent
{
    public static class ScreenContentAnnouncer
    {
        public static bool HasActiveContent, HasPendingContent;
        public static UnityEngine.GameObject Root;
        public static bool OwnsFocus(UnityEngine.GameObject go) => Root is not null && go.transform.IsChildOf(Root.transform);
    }
}
namespace LeAccess.Accessibility.Dialogue
{
    public static class DialogueAnnouncer
    {
        public static bool HasActiveContent, HasPendingContent;
        public static UnityEngine.GameObject Root;
        public static bool OwnsFocus(UnityEngine.GameObject go) => Root is not null && go.transform.IsChildOf(Root.transform);
    }
}
namespace LeAccess.Accessibility.Items
{
    internal static class MerchantMenuReader
    {
        internal static UnityEngine.GameObject OwnedTab;
        internal static bool TryReadTab(UnityEngine.GameObject go, out string text)
        { text = go == OwnedTab ? "Recompra" : null; return text != null; }
    }
}
namespace LeAccess.Accessibility.Inventory { public static class InventoryAccessibility { public static bool TryHandleFocus(UnityEngine.GameObject go) => false; } }
namespace LeAccess.Accessibility.Inventory
{
    internal static class InventoryPromptReader
    {
        internal static bool Allowed = true;
        internal static bool ItemPrompt;
        internal static bool IsItemPrompt(MultiButtonsActionPrompt prompt) => ItemPrompt;
        internal static bool CanRead(MultiButtonsActionPrompt prompt) => Allowed;
    }
}
namespace LeAccess.Accessibility.Skills { public static class SkillAccessibility { public static bool TryHandleFocus(UnityEngine.GameObject go) => false; } }
namespace LeAccess.Accessibility.Passives
{
    public static class PassiveAccessibility
    {
        public static UnityEngine.GameObject OwnedRoot;
        public static bool TryHandleFocus(UnityEngine.GameObject go) => OwnedRoot != null && go.transform.IsChildOf(OwnedRoot.transform);
    }
}
namespace LeAccess.Accessibility.Quests
{
    public static class QuestAccessibility
    {
        public static UnityEngine.GameObject OwnedRoot;
        public static bool TryHandleFocus(UnityEngine.GameObject go) => OwnedRoot != null && go.transform.IsChildOf(OwnedRoot.transform);
    }
}
namespace LeAccess.Accessibility.ControllerMenus { public static class ControllerMenuAccessibility { public static bool TryHandleFocus(UnityEngine.GameObject go) => false; } }
namespace LeAccess.Accessibility
{
    public static class ScreenReader
    {
        public static bool SpokeThisFrame, Speaking;
        public static readonly List<(string Text, bool Interrupt)> Messages = new();
        public static bool IsAvailable() => true;
        public static bool IsSpeaking() => Speaking;
        public static void Speak(string text, bool interrupt) { Messages.Add((text,interrupt)); SpokeThisFrame = true; }
    }
}
namespace LeAccess
{
    public static class Plugin { public static Logger Log = new(); }
    public class Logger
    {
        public void LogDebug(string message) { }
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}
