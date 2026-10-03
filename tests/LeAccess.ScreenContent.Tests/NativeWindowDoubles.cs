namespace UnityEngine
{
    internal class Behaviour : Component { public bool isActiveAndEnabled = true; }
}

internal static class NavigationManager { internal static UnityEngine.Object currentlySelected; }
internal class Navigable : UnityEngine.Behaviour { internal UnityEngine.Behaviour ActiveSelectable; }
internal sealed class NavigableMediator : UnityEngine.Behaviour
{ internal LE.UI.PanelSystem.Panel FocusedPanel; }
internal sealed class DungeonFloorExitPanel : LE.UI.PanelSystem.Panel { }
internal sealed class DungeonEnterPanelUI : UnityEngine.Behaviour { }
internal sealed class DungeonFloorExitPanelUI : UnityEngine.Behaviour { }

namespace LE.UI.Dungeons
{ internal sealed class BossConfirmationPanelUI : UnityEngine.Behaviour { } }

namespace LE.UI.PanelSystem
{
    internal class Panel : UnityEngine.Behaviour { }
    internal interface IModal { bool WasCollected { get; } }
    internal sealed class GenericNativeModal : Panel, IModal { bool IModal.WasCollected => WasCollected; }
    internal sealed class WarningConfirmationModal : Panel { }
    internal sealed class AlertModal : Panel { }
    internal sealed class LightlessArborEntrancePanel : Panel { }
    internal sealed class SoulfireBastionEntrancePanel : Panel { }
    internal sealed class TemporalSanctumEntrancePanel : Panel { }
    internal sealed class BossConfirmationPanel : Panel { }
}
