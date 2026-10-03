using TMPro;
using UnityEngine;

internal class WaypointQuestIndicators : Component
{ internal bool hasActiveMainQuest, hasActiveSideQuest, hasPotentialMainQuest, haPotentialSideQuest; }
internal class UIWaypoint : Component
{
    internal GameObject label;
    internal bool isActive = true, playerIsHere;
    internal WaypointQuestIndicators questIndicators;
    internal bool GetIsActive() => isActive;
    internal bool GetIsPlayerHere() => playerIsHere;
}
internal sealed class UIWaypointStandard : UIWaypoint { internal TextMeshProUGUI locationLabel; }
internal sealed class UIWaypointRegion : Component
{
    internal TextMeshProUGUI regionLabel, locationCountLabel;
    internal GameObject playerLocation;
    internal bool hasActiveMainQuest, hasActiveSideQuest, hasPotentialMainQuest, haPotentialSideQuest;
}
internal sealed class WaypointDescriptionPopup : Component { internal TextMeshProUGUI description; }
internal sealed class MapPanel : Component { internal WaypointDescriptionPopup waypointDescriptionPopup; }
namespace LE.UI.PanelSystem { internal sealed class WorldMapPanel : Component { internal MapPanel _mapPanel; } }
