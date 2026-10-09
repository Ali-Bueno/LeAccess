using TMPro;
using UnityEngine;

internal class WaypointQuestIndicators : Component
{ internal bool hasActiveMainQuest, hasActiveSideQuest, hasPotentialMainQuest, haPotentialSideQuest; }
internal class UIWaypoint : Component
{
    internal GameObject label;
    internal bool isActive = true, playerIsHere, noWaypointInScene;
    internal WaypointQuestIndicators questIndicators;
    internal string sceneName;
    internal bool GetIsActive() => isActive;
    internal bool representsSceneInEra(string scene, SceneList.Era era) => sceneName == scene;
    internal bool GetIsPlayerHere() => playerIsHere;
    internal MonolithIslandInfo monolithIslandInfo;
    internal static Action<UIWaypoint> Selected;
    internal void SelectSceneLocation() => Selected?.Invoke(this);
}
internal sealed class UIWaypointStandard : UIWaypoint { internal TextMeshProUGUI locationLabel; }
internal sealed class UIWaypointRegion : Component
{
    internal string regioniName;
    internal TextMeshProUGUI regionLabel, locationCountLabel;
    internal GameObject playerLocation;
    internal Il2CppSystem.Collections.Generic.List<UIWaypoint> waypointsInRegion = new();
    internal bool hasActiveMainQuest, hasActiveSideQuest, hasPotentialMainQuest, haPotentialSideQuest;
}
internal sealed class WaypointDescriptionPopup : Component { internal TextMeshProUGUI description; }
internal static class SceneList { internal enum Era { Ancient, Divine } }
internal enum SubmitType { A_EX, X_SQUARE }
internal enum Direction { UP, DOWN, LEFT, RIGHT }
internal sealed class TooltipInfo : NativeObject { internal string nameText, mainText; }
internal class InfoForTooltip : Component { internal TooltipInfo tooltipInfo = new(); internal virtual void SetText() { } }
internal sealed class GeneralTooltop : InfoForTooltip
{
    internal string title, body;
    internal override void SetText() { tooltipInfo.nameText = title; tooltipInfo.mainText = body; }
}
internal class ItemContainerUI : Component { }
internal sealed class BlessingContainerUI : ItemContainerUI { }
internal sealed class MonolithIslandInfo : Component
{
    internal TextMeshProUGUI IslandName, corruptionText;
    internal TMP_Text alternateTimelineLabelText, empoweredTimelineLabelText;
    internal GameObject alternateTimelineLabel, conqueredTimelineLabel, empoweredTimelineLabel, corruptionDisplay, harbinger;
}
internal sealed class EpochMasteryCheckboxUI : Component { internal TextMeshProUGUI label; internal bool IsComplete; }
internal sealed class EpochMasteryUI : Component
{
    internal GameObject progressionPanel, completionPanel;
    internal UnityEngine.UI.Button acknowledgeButton;
    internal EpochMasteryCheckboxUI[] progressionPointsCheckBoxes;
    internal TextMeshProUGUI masteryDescription;
}
internal sealed class QuestPanel : Component { internal bool questsHidden; }
internal sealed class WorldMapArrow : Component
{
    internal readonly List<GameObject> Centered = new();
    internal UIWaypoint FocusTarget;
    internal void CenterOnGameobject(GameObject target) => Centered.Add(target);
}
internal sealed class WorldMapZoom : Component
{
    internal float currentZoom = 1, regionZoomLevel = 0.6f, labelZoomRegionBreakpoint = 0.3f;
    internal bool manualZoomScrollBlocked;
    internal void ZoomTo(float zoom) => currentZoom = zoom;
}
internal sealed class UIWaypointController : Component
{
    internal Il2CppSystem.Collections.Generic.List<UIWaypoint> waypointsInMenu = new();
    internal Il2CppSystem.Collections.Generic.List<UIWaypointRegion> regionsInMenu = new();
    internal Il2CppSystem.Collections.Generic.List<GeneralTooltop> IconsInMenu = new();
}
internal sealed class MapPanel : Component
{
    internal sealed class UIEra : NativeObject { internal SceneList.Era era; internal string mapTitle; internal UIWaypointController waypointController; }
    internal QuestPanel questPanel;
    internal WorldMapArrow worldMapArrow;
    internal WorldMapZoom mapZoom;
    internal bool mapTransitionBlocked;
    internal void DeselectCurrentLocation() => focusedSceneID = null;
    internal WaypointDescriptionPopup waypointDescriptionPopup;
    internal TextMeshProUGUI mapTitle, selectedLocationLabel, selectedLocationLevelLabel;
    internal SceneList.Era CurrentEra;
    internal string focusedSceneID, focusedRegionID;
    internal Il2CppSystem.Collections.Generic.List<UIEra> eras = new();
    internal string GetFocusedSceneID() => focusedSceneID;
    internal string GetFocusedRegionID() => focusedRegionID;
}
namespace Il2CppSystem.Collections.Generic
{
    internal class List<T> : NativeObject
    {
        private readonly System.Collections.Generic.List<T> _values = new();
        internal int Count => _values.Count;
        internal T this[int index] => _values[index];
        internal void Add(T item) => _values.Add(item);
    }
}
namespace LE.UI.PanelSystem { internal sealed class WorldMapPanel : Component { internal MapPanel _mapPanel; } }
namespace LeAccess.Accessibility.Zones
{
    internal static class ZoneEra
    {
        internal static Func<SceneList.Era, string> Names = _ => null;
        internal static string Name(SceneList.Era era) => Names(era);
    }
}
namespace LeAccess.Accessibility.Map
{
    internal static class MapItemName
    {
        internal static Func<ItemContainerUI, string> Names = _ => null;
        internal static string Of(ItemContainerUI slot) => Names(slot);
    }
    internal static class MapRegionName
    {
        internal static string Get(UIWaypointRegion region) => region?.regionLabel?.text;
    }
}
