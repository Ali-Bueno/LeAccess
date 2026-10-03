using LeAccess.Accessibility.Map;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;

int checks = 0;
void Check(bool condition, string title) { checks++; if (!condition) throw new Exception(title); }
ModText.Initialize(null, null);
TextMeshProUGUI Label(GameObject parent, string text) => parent.Child().Add(new TextMeshProUGUI { text = text });
UIWaypointStandard Waypoint(GameObject root, string name)
{
    var waypoint = root.Add(new UIWaypointStandard());
    waypoint.locationLabel = Label(root, name);
    return waypoint;
}
string Read(GameObject go, bool details = false) => WaypointReader.TryRead(go, details, out string text) ? text : null;

var root = new GameObject();
var waypoint = Waypoint(root, "Ciudad Eterna");
var button = root.Child();
Check(Read(button) == "Ciudad Eterna", "unlocked waypoint away from the player reads only its label");
waypoint.playerIsHere = true;
Check(Read(button) == "Ciudad Eterna. You are here", "current location is spoken");
ModText.SetLanguage("es");
Check(Read(button) == "Ciudad Eterna. Estás aquí", "Spanish wording for the current location");
waypoint.playerIsHere = false; waypoint.isActive = false;
Check(Read(button) == "Ciudad Eterna. Bloqueado", "inactive waypoint is locked");
waypoint.playerIsHere = true;
waypoint.questIndicators = root.Add(new WaypointQuestIndicators
    { hasActiveMainQuest = true, haPotentialSideQuest = true });
Check(Read(button) == "Ciudad Eterna. Estás aquí. Bloqueado. Misión principal activa. Misión secundaria disponible",
    "here, locked and quests combine in a fixed order");
waypoint.questIndicators.hasActiveMainQuest = waypoint.questIndicators.haPotentialSideQuest = false;
waypoint.questIndicators.hasActiveSideQuest = waypoint.questIndicators.hasPotentialMainQuest = true;
Check(Read(button) == "Ciudad Eterna. Estás aquí. Bloqueado. Misión secundaria activa. Misión principal disponible",
    "side active and main available are distinct states");
ModText.SetLanguage("en");
Check(Read(button).EndsWith("Active side quest. Main quest available"), "English quest wording");
ModText.SetLanguage("es");

waypoint.locationLabel.alpha = 0;
Check(Read(button) is null, "hidden label falls back to the generic path");
waypoint.locationLabel.alpha = 1;
Check(Read(new GameObject()) is null, "non-waypoint control is not read");

var other = new GameObject();
var generic = other.Add(new UIWaypoint { label = other.Child() });
generic.label.Add(new TextMeshProUGUI { text = "Puerto" });
Check(Read(other) == "Puerto", "non-standard waypoint reads the text under its label object");

var regionRoot = new GameObject();
var region = regionRoot.Add(new UIWaypointRegion
{
    regionLabel = Label(regionRoot, "Eterra"), locationCountLabel = Label(regionRoot, "3/5"),
    playerLocation = regionRoot.Child(), hasPotentialMainQuest = true
});
Check(Read(regionRoot.Child()) == "Eterra. 3/5. Estás aquí. Misión principal disponible", "region reads label, count, here and quests");
region.playerLocation.Active = false; region.hasPotentialMainQuest = false; region.hasActiveSideQuest = true;
Check(Read(regionRoot) == "Eterra. 3/5. Misión secundaria activa", "region without the player omits the here state");
region.regionLabel.alpha = 0;
Check(Read(regionRoot) is null, "hidden region label falls back");

var mapRoot = new GameObject();
var mapPanel = mapRoot.Add(new MapPanel { waypointDescriptionPopup = mapRoot.Child().Add(new WaypointDescriptionPopup()) });
mapPanel.waypointDescriptionPopup.description = Label(mapPanel.waypointDescriptionPopup.gameObject, "Misión: Atrapado");
var mapWaypoint = Waypoint(mapRoot.Child(), "Fuerte");
Check(Read(mapWaypoint.gameObject, true) == "Fuerte. Misión: Atrapado", "details add the visible description popup");
Check(Read(mapWaypoint.gameObject) == "Fuerte", "brief reading omits the popup");
mapPanel.waypointDescriptionPopup.description.alpha = 0;
Check(Read(mapWaypoint.gameObject, true) == "Fuerte", "hidden popup adds nothing");
var worldRoot = new GameObject();
var world = worldRoot.Add(new WorldMapPanel { _mapPanel = mapPanel });
mapPanel.waypointDescriptionPopup.description.alpha = 1;
var detached = Waypoint(worldRoot.Child(), "Torre");
Check(Read(detached.gameObject, true) == "Torre. Misión: Atrapado", "world map panel owner reaches the popup");

Console.WriteLine($"Waypoint tests passed: {checks} checks");
