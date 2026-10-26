namespace Il2CppInterop.Runtime.InteropTypes
{
    internal class Il2CppObjectBase
    {
        private static int next;
        internal IntPtr Pointer = (IntPtr)(++next);
        internal bool WasCollected;
        internal T TryCast<T>() where T : class => this as T;
    }
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
}
namespace Il2CppSystem.Collections.Generic
{
    internal class List<T> : System.Collections.Generic.List<T> { internal bool WasCollected; }
}
namespace UnityEngine
{
    internal class Object : Il2CppSystem.Object { }
    internal class GameObject : Object
    {
        internal bool activeInHierarchy = true;
        internal Transform transform;
        internal GameObject() => transform = new(this);
    }
    internal class Transform : Object
    {
        internal GameObject gameObject;
        internal Transform parent;
        internal Transform(GameObject root) => gameObject = root;
        internal bool IsChildOf(Transform root) => this == root || parent is not null && parent.IsChildOf(root);
    }
    internal class RectTransform : Transform { internal RectTransform(GameObject root) : base(root) { } }
    internal struct Vector2Int { internal int x, y; }
    internal class Behaviour : Object
    {
        internal bool isActiveAndEnabled = true;
        internal GameObject gameObject = new();
        internal TMPro.TMP_Text Text;
        internal T GetComponentInChildren<T>() where T : class => Text as T;
    }
    internal class CanvasRenderer : Object { internal bool cull; }
}
namespace UnityEngine.UI { internal class Selectable : UnityEngine.Behaviour { } }
namespace TMPro
{
    internal class TMP_Text : UnityEngine.Behaviour
    {
        internal string text;
        internal float alpha = 1;
        internal UnityEngine.CanvasRenderer canvasRenderer = new();
    }
}
internal static class Localization
{
    internal static Dictionary<string, string> Texts = new();
    internal static string GetText(string key, string fallback = "") => Texts.GetValueOrDefault(key, fallback);
}
internal class ItemData : Il2CppSystem.Object
{
    internal string Id = Guid.NewGuid().ToString();
    internal NativeItemBytes id => Id is null ? null : new() { Length = Id.Length };
    internal ItemDataUnpacked Unpacked;
    internal bool IdEquals(ItemData other) => Id == other.Id;
    internal ItemDataUnpacked getAsUnpacked() => this as ItemDataUnpacked ?? Unpacked;
}
internal sealed class NativeItemBytes : Il2CppSystem.Object { internal int Length; }
internal class ItemDataUnpacked : ItemData { internal string LoreText, FactionRequirement; }
internal class ItemContainerEntry : Il2CppSystem.Object { internal ItemData data; internal UnityEngine.Vector2Int Position; }
internal enum ContainerType { INVENTORY, EQUIPPED, STASH, SHOP, CRAFTING }
internal enum ContainerID { UNDEFINED, INVENTORY, EQ_HELMET, EQ_ARMOR, EQ_WEAPON, EQ_OFFHAND, EQ_GLOVES,
    EQ_BELT, EQ_BOOTS, EQ_RIGHTRING, EQ_LEFTRING, EQ_AMULET, EQ_RELIC }
internal class IItemContainer : Il2CppSystem.Object
{
    internal ContainerType Type;
    internal ContainerID Id;
    internal ItemContainerEntry Entry;
    internal UnityEngine.Vector2Int LastReadPosition;
    internal ContainerID GetContainerID() => Id;
    internal ItemContainerEntry GetEntryAt(UnityEngine.Vector2Int slot) { LastReadPosition = slot; return Entry; }
}
internal class ItemContainerUI : UnityEngine.Behaviour
{
    internal ContainerID identifier;
    internal UnityEngine.Vector2Int currentFocusSlot;
    internal ItemContainerEntry currentlySelectedItem;
    internal IItemContainer Container = new();
    internal IItemContainer container => Container;
}
internal class EquippedItemsNavigable : UnityEngine.Behaviour
{
    internal Il2CppSystem.Object navigables, rects;
    internal UnityEngine.Vector2Int currentlySelectedIdx;
}
internal class BaseShopContainerUI : ItemContainerUI
{ internal UnityEngine.UI.Selectable shopBtn, buyBackBtn; }
internal static class NavigationManager { internal static Il2CppSystem.Object currentlySelected; }
internal class UIBase : UnityEngine.Object
{
    internal static UIBase instance;
    internal LE.UI.Tooltips.TooltipSystem TooltipSystem;
    internal static LE.UI.PanelSystem.ShopPanel Shop;
    internal static T GetPanelIfOpen<T>() where T : class => Shop as T;
}
namespace LE.UI.PanelSystem
{
    internal class ShopPanel : UnityEngine.Behaviour
    { internal ItemContainerUI _shop, _buyback; }
}
internal enum TooltipMode { None, DefaultInfo, DefaultModRanges, Explanation, CompareModRanges, CompareDefault }
internal class TooltipItemManager
{
    internal static string ReqFaction(ItemDataUnpacked item, SlotType slot) => item.FactionRequirement;
    internal enum SlotType { INVENTORY, EQUIPPED, GROUND, CRAFTING, STASH, SHOP, UNDEFINED, MORDITAS }
    internal class ItemTooltipParameters : Il2CppSystem.Object
    {
        internal ItemData _Item_k__BackingField;
        internal UnityEngine.GameObject _Opener_k__BackingField;
        internal SlotType _SlotType_k__BackingField;
        internal TooltipMode _TooltipMode_k__BackingField;
    }
}
namespace LE.UI.Tooltips
{
    internal class TooltipSystem : UnityEngine.Object
    { internal TooltipItemManager.ItemTooltipParameters _activeItemTooltipParameters; }
    internal class ItemTooltipInfo : Il2CppSystem.Object
    {
        internal ItemDataUnpacked Item;
        internal TooltipItemManager.SlotType SlotType;
        internal TooltipMode TooltipMode;
        internal string Name, PrimaryType, SecondaryType, Description, Requirements, Range, Speed, SetName;
        internal bool IsUnidentified, IsWeapon;
        internal ItemTooltipPotentialInfo PotentialInfo;
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipModifierInfo> Implicits = new(), UniqueModifiers = new();
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipAffixInfo> Prefixes = new(), Suffixes = new(), SealedAffixes = new(), AddsAffixes = new();
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipSetItemInfo> SetItems = new();
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipSetBonusInfo> SetBonuses = new();
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipEquipmentTypeLineItemInfo> CanRollOn = new();
        internal string ToDescriptionString(bool formatting) => Description;
        internal string ToRequirementsString(bool formatting) => Requirements;
        internal string ToRangeWithLabelString(bool formatting) => Range;
        internal string ToBaseAttackRateWithLabelString(bool formatting) => Speed;
    }
    internal class ItemTooltipModifierInfo : Il2CppSystem.Object
    {
        internal string Text;
        internal bool Throw;
        internal string ToModifierString(bool formatting)
        { if (Throw) throw new InvalidOperationException("formatter unavailable"); return Text; }
    }
    internal class ItemTooltipAffixInfo : Il2CppSystem.Object
    {
        internal ItemTooltipModifierInfo Modifier1, Modifier2;
        internal bool ShowTier;
        internal string Tier;
        internal string ToLocalizedTierString(bool formatting) => Tier;
    }
    internal class ItemTooltipPotentialInfo : Il2CppSystem.Object
    {
        internal bool ShouldShow;
        internal string Text;
        internal string ToSearchableString() => Text;
    }
    internal class ItemTooltipSetItemInfo : Il2CppSystem.Object
    { internal string Text; internal string ToItemNameString(bool formatting) => Text; }
    internal class ItemTooltipSetBonusInfo : Il2CppSystem.Object
    {
        internal string Requirement;
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipModifierInfo> Modifiers = new();
        internal string GetSetRequirementString() => Requirement;
    }
    internal class ItemTooltipEquipmentTypeLineItemInfo : Il2CppSystem.Object { internal string Text; }
    internal static class ItemTooltipContentBuilder
    {
        internal static int Calls;
        internal static ItemTooltipInfo Result;
        internal static Action BuildAction;
        internal static bool WrongItem, WrongMode, WrongSlot;
        internal static int VendorCalls;
        internal static ItemTooltipVendorInfo VendorResult;
        internal static Action VendorAction;
        internal static TooltipItemManager.SlotType VendorSlot;
        internal static ItemTooltipVendorInfo GetVendorInfo(ItemDataUnpacked item, TooltipItemManager.SlotType slot)
        { VendorCalls++; VendorSlot = slot; VendorAction?.Invoke(); return VendorResult; }
        internal static ItemTooltipInfo GetItemTooltipInfo(ItemDataUnpacked item, TooltipItemManager.SlotType slot, TooltipMode mode)
        {
            Calls++;
            BuildAction?.Invoke();
            if (Result is null) return null;
            Result.Item = WrongItem ? new ItemDataUnpacked() : item;
            Result.SlotType = WrongSlot ? TooltipItemManager.SlotType.GROUND : slot;
            Result.TooltipMode = WrongMode ? TooltipMode.None : mode;
            return Result;
        }
    }
    internal class ItemTooltipVendorInfo : Il2CppSystem.Object
    {
        internal bool IsForPurchase = true;
        internal Il2CppSystem.Collections.Generic.List<ItemTooltipPriceInfo> Prices = new();
    }
    internal class ItemTooltipPriceInfo : Il2CppSystem.Object
    {
        internal string Text;
        internal bool CanAfford = true;
        internal string ToSearchableString() => Text;
    }
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiVisibility
    { internal static bool IsVisible(UnityEngine.GameObject element) => element.activeInHierarchy; }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    { internal static string CleanText(string value) => value is null ? null : System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", "").Trim(); }
}

namespace LeAccess.Accessibility.Panels
{
    // Vendor panels are not modelled here; the ordinary shop path is what these tests cover.
    internal static class VendorReader
    {
        internal static bool TryPrice(ItemContainerUI container, out TooltipItemManager.SlotType slot, out string priceKey)
        { slot = TooltipItemManager.SlotType.UNDEFINED; priceKey = null; return false; }
    }
}

// Added so the suite compiles against the forge-aware selection reader.
internal class CraftingMainUI : UnityEngine.Behaviour { }
