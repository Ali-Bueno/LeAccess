using LeAccess.Accessibility;
using LeAccess.Accessibility.Map;
using LeAccess.Accessibility.ScreenContent;
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

// Map selection poller.
var mapObject = new GameObject();
var map = mapObject.Add(new MapPanel
{
    mapTitle = Label(mapObject, "Era Antigua"), selectedLocationLabel = Label(mapObject, "Ciudad Eterna"),
    selectedLocationLevelLabel = Label(mapObject, "Nivel 10"), focusedSceneID = "EoT"
});
var controller = mapObject.Add(new UIWaypointController());
map.eras.Add(new MapPanel.UIEra { waypointController = controller });
var here = Waypoint(mapObject.Child(), "Ciudad Eterna").Also(w => { w.sceneName = "EoT"; w.playerIsHere = true; });
controller.waypointsInMenu.Add(here);
var regionObject = mapObject.Child();
var mapRegion = regionObject.Add(new UIWaypointRegion { regioniName = "Eterra", hasActiveSideQuest = true });
controller.regionsInMenu.Add(mapRegion);
void Frame(bool quest = false) { ScreenReader.SpokeThisFrame = false; MapSelectionAnnouncer.Update(map, quest); }
void Reset() { ScreenReader.Messages.Clear(); MapSelectionAnnouncer.Reset(); }
Reset(); Frame();
Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text == "Era Antigua. Ciudad Eterna. Nivel 10. Estás aquí" &&
    ScreenReader.Messages[0].Interrupt, "opening the map speaks the era, selection, level and waypoint states");
Frame();
Check(ScreenReader.Messages.Count == 1, "unchanged selection is silent");
map.selectedLocationLabel.text = "Puerto"; map.focusedSceneID = "Port";
Frame();
Check(ScreenReader.Messages[^1].Text == "Puerto. Nivel 10", "new location without a matching waypoint speaks label and level only");
map.focusedSceneID = null; map.focusedRegionID = "Eterra"; map.selectedLocationLabel.text = "Eterra";
Frame();
Check(ScreenReader.Messages[^1].Text == "Eterra. Nivel 10. Misión secundaria activa", "focused region speaks its quest states");
int count = ScreenReader.Messages.Count;
map.selectedLocationLabel.text = "Otra"; Frame(quest: true);
Check(ScreenReader.Messages.Count == count + 1 && ScreenReader.Messages[^1].Text.StartsWith("Otra. Nivel 10") &&
    !ScreenReader.Messages[^1].Interrupt, "quest-driven selection queues its location after the quest brief");
Frame();
Check(ScreenReader.Messages.Count == count + 1, "quest-driven change is not repeated afterwards");
count = ScreenReader.Messages.Count;
map.selectedLocationLabel.text = "Nueva"; ScreenReader.SpokeThisFrame = true; MapSelectionAnnouncer.Update(map, false);
Check(ScreenReader.Messages.Count == count, "focus path speech in the same frame is not doubled");
map.CurrentEra = SceneList.Era.Divine; map.mapTitle.text = "Era Divina"; Frame();
Check(ScreenReader.Messages[^1].Text.StartsWith("Era Divina."), "era change speaks the new map title");
count = ScreenReader.Messages.Count;
mapObject.Active = false; Frame(); mapObject.Active = true; Frame();
Check(ScreenReader.Messages.Count == count + 1, "closing the map resets so reopening announces again");

var target = Waypoint(mapObject.Child(), "Llanuras").Also(w => w.sceneName = "Plains");
void Travel(UIWaypoint waypoint)
{
    MapTravelAnnouncer.BeginSubmit();
    if (waypoint is not null) MapTravelAnnouncer.AnnounceTravel(waypoint);
    MapTravelAnnouncer.EndSubmit(map, SubmitType.A_EX);
}
Reset(); Travel(target);
Check(ScreenReader.Messages.Single() == (ModText.Format("waypoint.traveling_to", target.locationLabel.text), true),
    "Cross on an unlocked waypoint says where it travels, once");
Reset(); target.isActive = false; Travel(target);
Check(ScreenReader.Messages.Single().Text == ModText.Get("waypoint.locked"), "A locked travel target says why nothing happens");
Reset(); target.isActive = true; target.playerIsHere = true; Travel(target);
Check(ScreenReader.Messages.Single().Text == ModText.Get("waypoint.here"), "Travelling to the current location says you are already there");
Reset(); target.playerIsHere = false; target.noWaypointInScene = true; Travel(target);
Check(ScreenReader.Messages.Single().Text == ModText.Get("waypoint.no_travel_point"), "A location without a waypoint says so");
Reset(); map.focusedSceneID = "Port"; map.focusedRegionID = null; map.selectedLocationLabel.text = "Puerto"; Travel(null);
Check(ScreenReader.Messages.Single().Text == ScreenContentTracker.Combine(new[] {
        ModText.Get("map.travel_unavailable"), map.selectedLocationLabel.text, map.selectedLocationLevelLabel.text }),
    "A travel button that does not respond reports the focused location instead of silence");
Reset(); map.focusedSceneID = null; Travel(null);
Check(ScreenReader.Messages.Count == 0, "Cross on a region zooms in; the selection reader speaks it");
Reset(); map.focusedSceneID = "Port"; MapTravelAnnouncer.BeginSubmit(); MapTravelAnnouncer.EndSubmit(map, SubmitType.X_SQUARE);
Check(ScreenReader.Messages.Count == 0, "Other map buttons keep their own behaviour");
Reset(); Frame();
map.CurrentEra = SceneList.Era.Ancient; map.mapTitle.text = "Era Antigua";
Check(!MapSelectionAnnouncer.Update(map, true), "An era switch alone does not consume a pending quest move");
map.selectedLocationLabel.text = "Destino de misión";
Check(MapSelectionAnnouncer.Update(map, true) && !ScreenReader.Messages[^1].Interrupt,
    "The quest's tweened location consumes the pending move and queues behind the brief");

Console.WriteLine($"Waypoint tests passed: {checks} checks");

static class Extensions { internal static T Also<T>(this T value, Action<T> action) { action(value); return value; } }
