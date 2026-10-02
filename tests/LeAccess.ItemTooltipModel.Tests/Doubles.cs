namespace Il2CppSystem
{
    internal class Object
    {
        private static int next;
        internal IntPtr Pointer = (IntPtr)(++next);
        internal bool WasCollected;
    }
}
namespace Il2CppSystem.Collections.Generic
{
    internal class List<T> : System.Collections.Generic.List<T> { internal bool WasCollected; }
}
namespace UnityEngine
{
    internal class Object : Il2CppSystem.Object { }
    internal class GameObject : Object { internal bool activeInHierarchy = true; }
    internal class Behaviour : Object
    {
        internal bool isActiveAndEnabled = true;
        internal GameObject gameObject = new();
    }
}
internal class ItemData : Il2CppSystem.Object
{
    internal string Id = Guid.NewGuid().ToString();
    internal ItemDataUnpacked Unpacked;
    internal bool IdEquals(ItemData other) => Id == other.Id;
    internal ItemDataUnpacked getAsUnpacked() => this as ItemDataUnpacked ?? Unpacked;
}
internal class ItemDataUnpacked : ItemData { internal string LoreText, FactionRequirement; }
internal class ItemContainerEntry : Il2CppSystem.Object { internal ItemData data; }
internal enum ContainerType { INVENTORY, EQUIPPED, STASH, SHOP, CRAFTING }
internal class IItemContainer : Il2CppSystem.Object { internal ContainerType Type; }
internal class ItemContainerUI : UnityEngine.Behaviour
{
    internal ItemContainerEntry currentlySelectedItem;
    internal IItemContainer Container = new();
}
internal static class NavigationManager { internal static Il2CppSystem.Object currentlySelected; }
internal class UIBase : UnityEngine.Object
{
    internal static UIBase instance;
    internal LE.UI.Tooltips.TooltipSystem TooltipSystem;
}
internal enum TooltipMode { None, DefaultInfo, DefaultModRanges, Explanation, CompareModRanges, CompareDefault }
internal class TooltipItemManager
{
    internal static string ReqFaction(ItemDataUnpacked item, SlotType slot) => item.FactionRequirement;
    internal enum SlotType { INVENTORY, EQUIPPED, GROUND, CRAFTING, STASH, SHOP, UNDEFINED }
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
