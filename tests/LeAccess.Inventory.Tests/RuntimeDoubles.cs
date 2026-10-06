using LeAccess.Accessibility.ScreenContent;

namespace LeAccess.Accessibility.Items
{
    internal static class MerchantItemReader
    {
        internal static string Context;
        internal static string ReadContext(ItemData item, ItemContainerUI container) => Context;
    }
    internal static class ItemTooltipModelReader
    {
        internal static string Text;
        internal static string LastFailure => null;
        internal static int Reads;
        internal static Action OnRead;
        internal static ItemData LastItem;
        internal static ItemContainerUI LastContainer;
        internal static bool OwnsSelection(ItemData item, ItemContainerUI container) =>
            LeAccess.Accessibility.Inventory.InventorySelectionReader.OwnsSelection(item, container);
        internal static bool ParametersAvailable = true;
        internal static bool TryGetParameters(ItemData item, ItemContainerUI container, out TooltipItemManager.SlotType slot, out int mode)
        { slot = TooltipItemManager.SlotType.INVENTORY; mode = 0; return ParametersAvailable; }
        internal static bool TryRead(ItemData item, ItemContainerUI container, out string text)
        { Reads++; LastItem = item; LastContainer = container; OnRead?.Invoke(); text = Text; return text != null; }
    }
}

namespace Il2CppInterop.Runtime.InteropTypes
{
internal class Il2CppObjectBase
{
    private static int _nextPointer;
    public IntPtr Pointer { get; } = new(++_nextPointer);
    public bool WasCollected;
    public T TryCast<T>() where T : class => this as T;
}

}
internal class NativeObject : Il2CppSystem.Object { }
internal static class Localization
{
    internal static readonly Dictionary<string, string> Texts = new();
    internal static string GetText(string key, string fallback) => Texts.GetValueOrDefault(key, fallback);
}

namespace UnityEngine
{
    internal class Object : NativeObject
    {
        public static bool operator ==(Object left, Object right)
        {
            if (left?.WasCollected == true || right?.WasCollected == true)
                throw new InvalidOperationException("Collected Unity null comparison");
            return ReferenceEquals(left, right);
        }
        public static bool operator !=(Object left, Object right) => !(left == right);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.ToInt32();
        public int GetInstanceID() => Pointer.ToInt32();
    }
    internal class Component : Object
    {
        private GameObject _gameObject;
        public GameObject gameObject
        {
            get => _gameObject;
            set { _gameObject?.Components.Remove(this); _gameObject = value; value?.Components.Add(this); }
        }
        public Component() => gameObject = new();
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled = true;
        public T GetComponentInChildren<T>() where T : Component => gameObject.GetComponentsInChildren<T>(false).FirstOrDefault();
        public T GetComponentInParent<T>() where T : Component
        {
            for (var current = transform; current is not null; current = current.parent)
                if (current.gameObject.Components.OfType<T>().FirstOrDefault() is { } component) return component;
            return null;
        }
    }
    internal sealed class GameObject : Object
    {
        public readonly Transform transform;
        public bool Visible = true;
        public bool activeInHierarchy => Visible && (transform.parent?.gameObject.activeInHierarchy ?? true);
        public string Text;
        public readonly List<Component> Components = new();
        public readonly List<GameObject> Children = new();
        public GameObject() => transform = new Transform(this);
        public T GetComponentInChildren<T>() where T : Component => GetComponentsInChildren<T>(false).FirstOrDefault();
        public NativeList<T> GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            var result = new NativeList<T>();
            if (!Visible && !includeInactive) return result;
            result.AddRange(Components.OfType<T>());
            foreach (var child in Children) result.AddRange(child.GetComponentsInChildren<T>(includeInactive));
            return result;
        }
    }
    internal class Transform : Object
    {
        public readonly GameObject gameObject;
        private Transform _parent;
        public Transform parent
        {
            get => _parent;
            set { _parent?.gameObject.Children.Remove(gameObject); _parent = value; value?.gameObject.Children.Add(gameObject); }
        }
        public Transform(GameObject owner) => gameObject = owner;
        public bool IsChildOf(Transform ancestor)
        {
            for (Transform current = this; current is not null; current = current.parent)
                if (ReferenceEquals(current, ancestor)) return true;
            return false;
        }
    }
    internal sealed class RectTransform : Transform { internal RectTransform(GameObject owner) : base(owner) { } }
    internal struct Vector2Int { public int x { get; set; } public int y { get; set; } }
    internal sealed class CanvasRenderer : Object { public bool cull; }
    internal static class Application { public static bool isFocused = true; }
    internal static class Time { public static float unscaledTime; }
}
internal sealed class NativeList<T> : List<T> { public bool WasCollected; }
namespace TMPro
{
    internal sealed class TMP_Text : UnityEngine.Component
    {
        public string text;
        public float alpha = 1;
        public UnityEngine.CanvasRenderer canvasRenderer = new();
    }
}
namespace UnityEngine.UI { internal sealed class ScrollRect : UnityEngine.Component { public UnityEngine.Transform content; } }

internal class ItemData : NativeObject
{
    public string Id;
    public NativeItemBytes id => Id is null ? null : new() { Length = Id.Length };
    public ItemDataUnpacked Unpacked { get; set; }
    public bool IdEquals(ItemData other) => Id == other.Id;
    public ItemDataUnpacked getAsUnpacked() => Unpacked ?? this as ItemDataUnpacked;
}
internal sealed class NativeItemBytes : NativeObject { public int Length; }
internal sealed class ItemDataUnpacked : ItemData
{
    public string FullName;
    public string LocalisedBaseTypeName;
}
internal sealed class ItemContainerEntry : NativeObject
{ public ItemData data; public UnityEngine.Vector2Int Position; public UnityEngine.Vector2Int size = new() { x = 1, y = 1 }; }
internal enum Direction { LEFT, UP, RIGHT, DOWN }
internal class ItemContainerUI : UnityEngine.Component
{
    public IItemContainer container = new();
    public IItemContainer Container => container;
    public ContainerID identifier;
    public ItemContainerEntry currentlySelectedItem;
    public UnityEngine.Vector2Int currentFocusSlot { get; set; }
    public bool forceUpdate;
    public bool singleSlotControllerNavigation;
    internal UnityEngine.Vector2Int NearestSlot;
    internal readonly List<ItemContainerEntry> Focused = new();
    internal void FocusNearestItem() => currentFocusSlot = NearestSlot;
    internal void SetFocusItem(ItemContainerEntry item) { currentlySelectedItem = item; Focused.Add(item); }
}
internal sealed class OneSlotItemContainer : NativeObject { internal bool Holding; internal bool HasContent() => Holding; }
internal sealed class ItemContainersManager : UnityEngine.Component
{
    internal static ItemContainersManager Instance;
    internal OneSlotItemContainer cursor = new();
}
internal enum ContainerID { UNDEFINED, INVENTORY, EQ_HELMET, EQ_ARMOR, EQ_WEAPON, EQ_OFFHAND, EQ_GLOVES, EQ_BELT,
    EQ_BOOTS, EQ_RIGHTRING, EQ_LEFTRING, EQ_AMULET, EQ_RELIC, SHOP, SHOP_BUYBACK }
namespace LE.Factions { internal sealed class ShopContainerUI : ItemContainerUI { } }
// The forge's item slot is an inventory-style container the forge reader speaks.
internal sealed class CraftingMainUI : ItemContainerUI { }
internal enum ContainerType { INVENTORY, EQUIPPED, STASH, SHOP }
internal sealed class IItemContainer : NativeObject
{
    internal ContainerType Type;
    internal ContainerID Id;
    internal ItemContainerEntry Entry;
    internal UnityEngine.Vector2Int LastReadPosition;
    internal ContainerID GetContainerID() => Id;
    internal Dictionary<(int, int), ItemContainerEntry> Slots;
    internal UnityEngine.Vector2Int Size = new() { x = 14, y = 8 };
    internal UnityEngine.Vector2Int GetSize() => Size;
    internal NativeList<ItemContainerEntry> GetContent()
    {
        var content = new NativeList<ItemContainerEntry>();
        if (Slots is not null) foreach (var entry in Slots.Values) if (!content.Contains(entry)) content.Add(entry);
        return content;
    }
    internal ItemContainerEntry GetEntryAt(UnityEngine.Vector2Int slot)
    {
        LastReadPosition = slot;
        return Slots is null ? Entry : Slots.GetValueOrDefault((slot.x, slot.y));
    }
}
internal sealed class EquippedItemsNavigable : UnityEngine.Component
{
    internal Il2CppSystem.Object navigables, rects;
    internal UnityEngine.Vector2Int currentlySelectedIdx;
}
internal static class NavigationManager { public static NativeObject currentlySelected; }
internal sealed class TooltipItemManager : UnityEngine.Component
{
    public enum SlotType { INVENTORY, EQUIPPED, GROUND, STASH }
    public enum TooltipInitializationStage { None, SetContent, Completed }
    public sealed class ItemTooltipParameters : NativeObject
    {
        public ItemDataUnpacked _Item_k__BackingField;
        public UnityEngine.GameObject _Opener_k__BackingField;
        public SlotType _SlotType_k__BackingField;
        public TooltipMode _TooltipMode_k__BackingField = TooltipMode.None;
    }
    public bool active = true, autoCompareEnabled;
    public TooltipInitializationStage activeInitializationStage = TooltipInitializationStage.Completed;
    public ItemTooltipParameters activeParameters;
    public UITooltipItem itemTooltip;
    public UnityEngine.Transform ActiveTooltipRect => itemTooltip?.transform;
}
internal sealed class UIBase : UnityEngine.Component
{
    public static UIBase instance;
    public LE.UI.Tooltips.TooltipSystem TooltipSystem;
    internal bool InventoryOpen, ShopOpen, GamblingOpen;
    internal bool IsInventoryOpen() => InventoryOpen;
    internal bool IsShopOpen() => ShopOpen;
    internal bool IsGamblingOpen() => GamblingOpen;
    internal bool IsSoulGamblingOpen() => false;
    internal bool IsDungeonKeysTraderPanelOpen() => false;
    internal bool IsFactionGamblingCoFOpen() => false;
    internal bool IsFactionGamblingMGOpen() => false;
    internal bool IsFactionTheWeaverVendorPanelOpen() => false;
    internal bool IsHunterVendorPanelOpen() => false;
    internal bool IsTimeglassVendorPanelOpen() => false;
    internal bool IsBazaarPanelOpen() => false;
}
namespace LeAccess.Accessibility.Vitals
{
    internal static class GoldReport
    {
        internal static int? Amount;
        internal static bool TryRead(out int gold) { gold = Amount ?? 0; return Amount.HasValue; }
    }
}
namespace LE.UI.Tooltips
{
    internal static class ItemTooltipPromptHelper
    {
        internal static NativeList<LE.UI.PanelSystem.PanelActionPrompt> Actions = new();
        internal static int Reads;
        internal static Action OnRead;
        internal static NativeActions GetActions(ItemDataUnpacked item, TooltipItemManager.SlotType slot)
        { Reads++; OnRead?.Invoke(); return new(Actions); }
    }
    internal sealed class TooltipSystem : UnityEngine.Component
    {
        public TooltipItemManager _tooltipItemManager;
        public ItemTooltip _itemTooltip, _itemTooltipComparison;
        public ItemTooltipInfo _activeComparisonItemTooltipInfo;
        public TooltipItemManager.ItemTooltipParameters _activeItemTooltipParameters;
        public ItemTooltipInfo _activeItemTooltipInfo;
        public ItemDataUnpacked ActiveItemTooltipItem;
        public UnityEngine.Transform ActiveItemTooltipRect;
        public bool IsItemTooltipReady = true;
    }
    internal sealed class ItemTooltipInfo : NativeObject
    { public ItemDataUnpacked _Item_k__BackingField; public TooltipMode _TooltipMode_k__BackingField = TooltipMode.DefaultInfo; }
    internal sealed class TooltipTemplate : UnityEngine.Component { public UnityEngine.UI.ScrollRect _scrollRect; }
    internal sealed class ItemTooltip : UnityEngine.Component
    {
        public ItemTooltipInfo _info;
        public UnityEngine.Transform RectTransform => transform;
        public TooltipTemplate _tooltipTemplate;
        public TMPro.TMP_Text _name, _primaryType, _secondaryType, _unidentifiedText, _description, _requirements;
        public ItemTooltipModifier _secondaryNameSection;
        public UnityEngine.Component _topTag, _potential, _implicitsSection, _weaponRange,
            _weaponSpeed, _prefixesSection, _suffixesSection, _sealedSection, _uniqueModifiersSection, _setItemsSection,
            _setBonusesSection, _addsAffixesSection, _canRollOnSection, _loreText, _priceList, _factionRankRequirement;
        public ItemTooltipModifiersSection _combinedDifferencesSection;
        public UnityEngine.Component _prompts;
    }
    internal sealed class ItemTooltipModifiersSection : UnityEngine.Component { }
    internal sealed class ItemTooltipModifier : UnityEngine.Component
    { public TMPro.TMP_Text _modifierText, _explanationText, _rangeText; public ItemTooltipModifierInfo Info; }
    internal sealed class ItemTooltipModifierInfo : NativeObject { public ItemTooltipAssetList.ItemTooltipModifierIconSprite IconSprite; }
    internal static class ItemTooltipAssetList
    {
        internal enum ItemTooltipModifierIconSprite { Plain, Compare_Added, Compare_Removed, Compare_NoChange, Compare_Increase,
            Compare_Decrease, Compare_CombinedIncrease, Compare_CombinedDecrease }
    }
}
internal sealed class UITooltipItem : UnityEngine.Component
{
    public ItemTooltipPrompts _itemTooltipPrompts;
    public bool tooltipActive = true, contentVisible = true;
    public UnityEngine.GameObject content = new();
    public UnityEngine.GameObject requirementsFooter;
    public UnityEngine.GameObject compareContent = new();
    public TMPro.TMP_Text nameText = new(), typeText = new();
    // The older tooltip's comparison column (compare*) and its own rows, which hold explanations and ranges.
    public TMPro.TMP_Text compareNameText, compareTypeText, comparisonText, comparisonGainedText, comparisonLostText, compareImplicitText;
    public UnityEngine.GameObject comparisonGainedHeader, comparisonLostHeader;
    public Il2CppSystem.Collections.Generic.List<UITooltipItemAffix> compareUniqueAffixesText, comparePrefixes, compareSuffixes;
    public UITooltipItemAffix compareSealedAffix, compareSealedPrimordialAffix, compareSealedCorruptedAffix;
    public TMPro.TMP_Text implicitText, forgingPotentialAltText, legendaryPotentialAltText, weaversWillAltText, weaversTouchAltText;
    public Il2CppSystem.Collections.Generic.List<UITooltipItemAffix> uniqueAffixesText, prefixesText, suffixesText;
    public UITooltipItemAffix sealedAffix, sealedPrimordialAffix, sealedCorruptedAffix;
}
internal sealed class UITooltipItemAffix : UnityEngine.Component { public TMPro.TMP_Text _text; }

internal sealed class NativeActions : NativeObject
{
    private readonly NativeList<LE.UI.PanelSystem.PanelActionPrompt> _actions;
    internal NativeActions(NativeList<LE.UI.PanelSystem.PanelActionPrompt> actions) => _actions = actions;
    internal Il2CppSystem.Collections.IEnumerator GetEnumerator() => new(_actions);
}
namespace Il2CppSystem
{
    internal class Object : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase { }
    internal sealed class Array : Object
    {
        internal Object[,] Cells;
        internal int Rank = 2;
        internal int GetLength(int dimension) => Cells.GetLength(dimension);
        internal Object GetValue(int x, int y) => Cells[x, y];
    }
    internal class IDisposable : NativeObject { internal void Dispose() { } }
}
namespace Il2CppSystem.Collections.Generic
{
    internal sealed class List<T> : Il2CppSystem.Object
    {
        private readonly System.Collections.Generic.List<T> _items = new();
        internal int Count => _items.Count;
        internal T this[int index] => _items[index];
        internal void Add(T item) => _items.Add(item);
    }
}
namespace Il2CppSystem.Collections
{
    internal sealed class IEnumerator : Il2CppSystem.IDisposable
    {
        private readonly System.Collections.Generic.IEnumerator<LE.UI.PanelSystem.PanelActionPrompt> _iterator;
        internal IEnumerator(IEnumerable<LE.UI.PanelSystem.PanelActionPrompt> actions) => _iterator = actions.GetEnumerator();
        internal bool MoveNext() => _iterator.MoveNext();
        internal LE.UI.PanelSystem.PanelActionPrompt Current => _iterator.Current;
    }
}
internal sealed class ItemTooltipPrompts : UnityEngine.Component { }
internal sealed class MultiButtonsActionPrompt : UnityEngine.Component { public bool CanUse = true; }

namespace LE.UI { internal static class LoadingScreen { public static bool IsVisible { get; set; } } }
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiVisibility
    {
        internal static bool IsVisible(UnityEngine.GameObject root)
        {
            for (var node = root; node is not null; node = node.transform.parent?.gameObject)
                if (node.WasCollected || !node.Visible) return false;
            return root is not null;
        }
    }
}
namespace LeAccess.Accessibility.ScreenContent
{
    internal static class ScreenContentAnnouncer { public static bool HasActiveContent; }
    internal static class ScreenContentReader
    {
        internal static string Read(UnityEngine.GameObject root, TMPro.TMP_Text header, TMPro.TMP_Text body) =>
            ScreenContentTracker.Combine(new[] { header?.text, body?.text, root.Text });
    }
}
namespace LeAccess.Accessibility.Dialogue { internal static class DialogueAnnouncer { public static bool HasActiveContent { get; set; } } }
namespace LeAccess.Accessibility.ControllerMenus { internal static class ControllerMenuAccessibility { public static bool HasActiveContent { get; set; } } }
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        public static string CleanText(string text) => text?.Trim();
        public static bool IsVisibleLabel(TMPro.TMP_Text label) => label is not null && !label.WasCollected &&
            label.alpha > 0 && LeAccess.Accessibility.Announcements.UiVisibility.IsVisible(label.gameObject);
    }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        public static readonly List<string> Messages = new();
        public static bool Available = true;
        public static bool OutputAccepted = true;
        public static bool SpokeThisFrame { get; set; }
        public static bool IsAvailable() => Available;
        public static void Speak(string text, bool interrupt) => TrySpeak(text, interrupt);
        public static bool TrySpeak(string text, bool interrupt)
        {
            if (!Available || !OutputAccepted) return false;
            Messages.Add(text); return true;
        }
    }
}
namespace LeAccess
{
    internal static class Plugin { public static readonly Logger Log = new(); }
    internal sealed class Logger
    {
        public readonly List<string> Warnings = new(), Information = new();
        public void LogInfo(string text) => Information.Add(text);
        public void LogWarning(string text) => Warnings.Add(text);
    }
}

internal static class EpochInputManager { internal static bool IsControllerActive = true; }
internal sealed class ButtonInfo : NativeObject { internal string ButtonName; }
internal sealed class NativeNames : NativeObject
{
    internal string[] Values = Array.Empty<string>(); internal int Length => Values.Length;
    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)Values).GetEnumerator();
}
namespace UnityEngine { internal sealed class CanvasGroup : Object { internal float alpha = 1; } }
namespace LE.UI.PanelSystem
{
    internal class PanelAction { [Flags] internal enum ControlType { None = 0, MouseAndKeyboard = 1, Gamepad = 2, All = 3 } }
    internal sealed class PanelActionPrompt : NativeObject
    {
        internal bool CanUse = true, IsHoldAction;
        internal NativeNames GamepadButtonNamesOverride, MouseAndKeyboardButtonNamesOverride;
        internal string ActionName;
        internal string Label;
        internal PanelAction.ControlType Controls = PanelAction.ControlType.All;
        internal string GetActionName() => ActionName;
    }
    internal sealed class PanelActionUIPrompt : UnityEngine.Component
    {
        internal TMPro.TMP_Text _label;
        internal UnityEngine.CanvasGroup _cg;
        internal PanelActionPrompt _panelAction;
        internal bool showKeyboardGylphs;
        internal NativeList<ButtonInfo> BuildOverrideButtonsInfo(NativeNames names, bool controller)
            { var result = new NativeList<ButtonInfo>(); result.AddRange(names.Values.Select(x => new ButtonInfo { ButtonName = x })); return result; }
    }
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class InputBindingText
    {
        internal static readonly Dictionary<string, string> Bindings = new(), Localized = new();
        internal static string LocalizeButton(string name) => name != null && Localized.TryGetValue(name, out var text) ? text : name;
        internal static string ReadAction(string name, bool keyboard = false, bool? holdOverride = null) => Hold(Bindings.GetValueOrDefault(name ?? ""), holdOverride);
        internal static string ReadButtons(NativeList<ButtonInfo> buttons, bool? holdOverride = null) => Hold(string.Join(" + ", buttons.Select(x => LocalizeButton(x.ButtonName))), holdOverride);
        private static string Hold(string text, bool? hold) => hold == true && !string.IsNullOrWhiteSpace(text) ? LeAccess.I18n.ModText.Format("input.hold", text) : text;
    }
}
internal enum TooltipMode { None, DefaultInfo, DefaultModRanges, Explanation, CompareModRanges, CompareDefault }
