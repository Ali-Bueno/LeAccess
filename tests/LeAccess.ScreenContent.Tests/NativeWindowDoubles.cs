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
    internal sealed class ModalTemplate : UnityEngine.Component { public TMPro.TMP_Text _title, _message; }
}

namespace LE.UI.Components
{ internal sealed class ModalTemplate : UnityEngine.Component { public TMPro.TMP_Text _title, _message; } }

namespace UnityEngine.UI
{
    internal class Selectable : UnityEngine.Component { }
    internal sealed class Button : Selectable { }
    internal sealed class ScrollRect : UnityEngine.Component { }
}

namespace LeAccess.Accessibility.Endgame
{
    // The endgame readers are tested with the menu reading suite; window discovery only needs them to cover nothing.
    internal static class EndgameWindows
    {
        internal static bool Covers(LE.UI.PanelSystem.Panel panel) => false;
        internal static System.Func<string> Reader(LE.UI.PanelSystem.Panel panel) => null;
    }
}
