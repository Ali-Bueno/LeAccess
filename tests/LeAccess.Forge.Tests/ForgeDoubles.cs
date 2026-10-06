// Game types the forge readers use, with only the members they read.
internal enum NavigableID { NONE }
internal static class NavigationManager { internal static NativeObject currentlySelected; }
internal static class EpochInputManager { internal static bool IsControllerActive = true; }
namespace Il2CppSystem
{
    internal sealed class Array : NativeObject
    {
        private readonly System.Array _values;
        internal Array(System.Array values) => _values = values;
        internal int Rank { get { Guard(); return _values.Rank; } }
        internal int GetLength(int dimension) { Guard(); return _values.GetLength(dimension); }
        internal NativeObject GetValue(int x, int y) { Guard(); return _values.GetValue(x, y) as NativeObject; }
    }
}
internal sealed class ItemDataUnpacked : NativeObject { internal string FullName; }
internal sealed class ItemData : NativeObject
{
    internal ItemDataUnpacked Unpacked;
    internal ItemDataUnpacked getAsUnpacked() { Guard(); return Unpacked; }
}
internal sealed class ItemContainerEntry : NativeObject { internal ItemData data; }
internal class ItemContainerUI : UnityEngine.Component
{
    internal IItemContainer Container = new();
    internal UnityEngine.Vector2Int currentFocusSlot;
}
internal sealed class IItemContainer : NativeObject
{
    internal readonly Dictionary<(int, int), ItemContainerEntry> Slots = new();
    internal ItemContainerEntry GetEntryAt(UnityEngine.Vector2Int slot) { Guard(); return Slots.GetValueOrDefault((slot.x, slot.y)); }
}
internal sealed class OneSlotItemContainer : NativeObject { internal bool Holding; internal bool HasContent() { Guard(); return Holding; } }
internal sealed class ItemContainersManager : UnityEngine.Component
{
    internal static ItemContainersManager Instance;
    internal OneSlotItemContainer cursor = new();
}
internal sealed class OneItemContainer : NativeObject
{
    internal readonly List<ItemContainerEntry> Items = new();
    internal NativeArray<ItemContainerEntry> GetContent() { Guard(); return new(Items); }
    internal void Put(ItemData data) { Items.Clear(); Items.Add(new() { data = data }); }
    internal void Clear() => Items.Clear();
    internal void Put(string name) { Items.Clear(); Items.Add(new() { data = new() { Unpacked = new() { FullName = name } } }); }
}
internal sealed class CraftingSlotManager : UnityEngine.Component
{
    internal UnityEngine.Vector2Int currentlySelectedIdx;
    internal NativeObject activeNavigationElements;
    internal OneItemContainer main = new(), modifier = new(), support = new();
    internal UnityEngine.UI.Button forgeButton;
    internal bool canForge = true;
    internal TMPro.TextMeshProUGUI forgeButtonText, cannotForgeText, minForgingPotentialText, maxForgingPotentialText,
        forgingPotentialText, singlelineFractureChanceTMP, minorFractureChanceTMP, criticalChanceOutcomeTMP,
        lostForgingPotentialTMP, typeOfForgeTMP;
}
internal sealed class AffixSlotForge : UnityEngine.Component
{
    internal enum AffixSlotID { PREFIX_ONE, PREFIX_TWO, SUFFIX_ONE, SUFFIX_TWO }
    internal AffixSlotID slotID;
    internal TMPro.TextMeshProUGUI nameTMP, tierTMP, countTMP;
    internal UnityEngine.UI.Button addAffixButton;
    internal CraftingUpgradeButton upgradeButton;
    internal bool availableUpgrade;
}
internal sealed class CraftingUpgradeButton : UnityEngine.Component
{ internal UnityEngine.UI.Button _upgradeButton; internal AffixSlotForge affixSlotForge; }
internal sealed class CraftingMainUI : ItemContainerUI { }
internal sealed class CraftingSupportUI : UnityEngine.Component { }
internal sealed class CraftingModifierUI : UnityEngine.Component { }
internal sealed class CraftingRuneButton : UnityEngine.Component { }
internal sealed class CraftingMaterialsToggleButton : UnityEngine.Component { }
namespace LE.UI.PanelSystem
{
    internal sealed class ForgePanel : UnityEngine.Component
    {
        internal CraftingSlotManager CraftingSlotManager;
        internal ItemContainerUI _main, _modifier, _support;
    }
    internal sealed class OpenPanels : UnityEngine.Component
    {
        internal UnityEngine.Component Open;
        // The game returns a panel only while it is shown.
        internal T GetPanelIfOpen<T>() where T : UnityEngine.Component => Open is { gameObject.activeInHierarchy: true } ? Open as T : null;
    }
}
internal sealed class UIBase : UnityEngine.Component
{
    internal static UIBase instance;
    internal bool InventoryOpen;
    internal LE.UI.PanelSystem.OpenPanels PanelSystem;
    internal bool IsInventoryOpen() => InventoryOpen;
}
namespace LeAccess.Accessibility.Inventory
{
    // The inventory reader is tested in its own suite; the forge notices only hand it text to lead the next brief.
    internal static class InventoryAccessibility
    {
        internal static readonly List<string> Leads = new();
        internal static void Lead(string notice) => Leads.Add(notice);
    }
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class ForgeSummaryReader
    {
        internal static string Summary;
        internal static bool TryRead(UnityEngine.GameObject element, out string text) { text = Summary; return text != null; }
    }
}
namespace LeAccess.Accessibility.ControllerMenus
{ internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
namespace BepInEx.Logging
{
    internal enum LogLevel { None = 0, All = 63 }
    internal sealed class LogEventArgs : System.EventArgs { internal object Data { get; init; } }
    internal interface ILogListener : System.IDisposable
    {
        LogLevel LogLevelFilter { get; }
        void LogEvent(object sender, LogEventArgs eventArgs);
    }
    internal static class Logger { internal static readonly List<ILogListener> Listeners = new(); }
}
