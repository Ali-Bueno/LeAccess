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
string T(string key) => ModText.Get(key);
string J(params string[] fragments) => ScreenContentTracker.Combine(fragments);
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
Check(Read(button) == J(waypoint.locationLabel.text, T("waypoint.here")), "current location is spoken");
ModText.SetLanguage("es");
Check(Read(button) == J(waypoint.locationLabel.text, T("waypoint.here")), "Spanish wording for the current location");
waypoint.playerIsHere = false; waypoint.isActive = false;
Check(Read(button) == J(waypoint.locationLabel.text, T("waypoint.locked")), "inactive waypoint is locked");
waypoint.playerIsHere = true;
waypoint.questIndicators = root.Add(new WaypointQuestIndicators
    { hasActiveMainQuest = true, haPotentialSideQuest = true });
Check(Read(button) == J(waypoint.locationLabel.text, T("waypoint.here"), T("waypoint.locked"), T("waypoint.quest_main_active"),
        T("waypoint.quest_side_available")),
    "here, locked and quests combine in a fixed order");
waypoint.questIndicators.hasActiveMainQuest = waypoint.questIndicators.haPotentialSideQuest = false;
waypoint.questIndicators.hasActiveSideQuest = waypoint.questIndicators.hasPotentialMainQuest = true;
Check(Read(button) == J(waypoint.locationLabel.text, T("waypoint.here"), T("waypoint.locked"), T("waypoint.quest_side_active"),
        T("waypoint.quest_main_available")),
    "side active and main available are distinct states");
ModText.SetLanguage("en");
Check(Read(button).EndsWith(J(T("waypoint.quest_side_active"), T("waypoint.quest_main_available"))), "English quest wording");
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
Check(Read(regionRoot.Child()) == J(region.regionLabel.text, region.locationCountLabel.text, T("waypoint.here"),
    T("waypoint.quest_main_available")), "region reads label, count, here and quests");
region.playerLocation.Active = false; region.hasPotentialMainQuest = false; region.hasActiveSideQuest = true;
Check(Read(regionRoot) == J(region.regionLabel.text, region.locationCountLabel.text, T("waypoint.quest_side_active")), "region without the player omits the here state");
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
Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text == J(map.mapTitle.text, map.selectedLocationLabel.text, map.selectedLocationLevelLabel.text, T("waypoint.here")) &&
    ScreenReader.Messages[0].Interrupt, "opening the map speaks the era, selection, level and waypoint states");
Frame();
Check(ScreenReader.Messages.Count == 1, "unchanged selection is silent");
map.selectedLocationLabel.text = "Puerto"; map.focusedSceneID = "Port";
Frame();
Check(ScreenReader.Messages[^1].Text == J(map.selectedLocationLabel.text, map.selectedLocationLevelLabel.text), "new location without a matching waypoint speaks label and level only");
map.focusedSceneID = null; map.focusedRegionID = "Eterra"; map.selectedLocationLabel.text = "Eterra";
Frame();
Check(ScreenReader.Messages[^1].Text == J(map.selectedLocationLabel.text, map.selectedLocationLevelLabel.text, T("waypoint.quest_side_active")), "focused region speaks its quest states");
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
Check(ScreenReader.Messages[^1].Text.StartsWith(map.mapTitle.text + "."), "era change speaks the new map title");
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

// List navigation with the quests hidden (D-pad up/down) and the era/quest-panel announcements.
var listObject = new GameObject();
var listMap = listObject.Add(new MapPanel
{
    mapTitle = Label(listObject, "Era Antigua"), selectedLocationLabel = Label(listObject, "Aldea"),
    selectedLocationLevelLabel = Label(listObject, "Nivel 3"), focusedSceneID = "A",
    questPanel = listObject.Add(new QuestPanel { questsHidden = true }),
    worldMapArrow = listObject.Add(new WorldMapArrow()), mapZoom = listObject.Add(new WorldMapZoom())
});
var ancient = listObject.Add(new UIWaypointController());
var divine = listObject.Add(new UIWaypointController());
listMap.eras.Add(new MapPanel.UIEra { era = SceneList.Era.Ancient, waypointController = ancient });
listMap.eras.Add(new MapPanel.UIEra { era = SceneList.Era.Divine, waypointController = divine });
UIWaypointStandard Place(UIWaypointController owner, string name, string scene)
{
    var place = Waypoint(listObject.Child(), name).Also(w => w.sceneName = scene);
    owner.waypointsInMenu.Add(place);
    return place;
}
UIWaypointRegion Area(string name, params UIWaypoint[] members)
{
    var areaObject = listObject.Child();
    var area = areaObject.Add(new UIWaypointRegion { regionLabel = Label(areaObject, name) });
    foreach (var member in members) area.waypointsInRegion.Add(member);
    ancient.regionsInMenu.Add(area);
    return area;
}
var aldea = Place(ancient, "Aldea", "A"); var bosque = Place(ancient, "Bosque", "B");
var costa = Place(ancient, "Costa", "C"); var desierto = Place(ancient, "Desierto", "D");
var templo = Place(divine, "Templo", "E");
var north = Area("Norte", bosque, aldea);
var south = Area("Sur", costa);
north.regionLabel.gameObject.Active = false;
UIWaypoint.Selected = w =>
{
    listMap.focusedSceneID = w.sceneName;
    listMap.selectedLocationLabel.text = ((UIWaypointStandard)w).locationLabel.text;
};
string Level() => listMap.selectedLocationLevelLabel.text;
string Pos(int index, int total) => ModText.Format("list.position", index, total);
void Poll() { ScreenReader.SpokeThisFrame = false; MapSelectionAnnouncer.Update(listMap, false); }

Reset(); Poll();
Check(ScreenReader.Messages.Single().Text == J(listMap.mapTitle.text, Pos(1, 2), T("map.quests_hidden"), "Aldea", Level()),
    "opening with the quests hidden names the era tab, its position and the list mode");
listMap.questPanel.questsHidden = false; Poll();
Check(ScreenReader.Messages[^1].Text == T("map.quests_shown"), "showing the quests says so, without repeating the place");
listMap.questPanel.questsHidden = true; Poll();
Check(ScreenReader.Messages[^1].Text == T("map.quests_hidden"), "hiding the quests says the D-pad now walks the places");
listMap.CurrentEra = SceneList.Era.Divine; listMap.mapTitle.text = "Era Divina"; Poll();
Check(ScreenReader.Messages[^1].Text.StartsWith(J(listMap.mapTitle.text, Pos(2, 2))), "a new era tab says its title and position");
listMap.CurrentEra = SceneList.Era.Ancient; listMap.mapTitle.text = "Era Antigua"; Poll();

string Nav(Direction direction)
{
    ScreenReader.Messages.Clear(); ScreenReader.SpokeThisFrame = false;
    return MapLocationList.TryNavigate(listMap, direction) ? ScreenReader.Messages.LastOrDefault().Text ?? "" : null;
}
var centered = listMap.worldMapArrow.Centered;
listMap.questPanel.questsHidden = false;
Check(Nav(Direction.DOWN) is null && centered.Count == 0, "with the quests shown the D-pad keeps changing the quest");
listMap.questPanel.questsHidden = true;
Check(Nav(Direction.LEFT) is null && Nav(Direction.RIGHT) is null, "LB and RB keep switching eras");
listMap.focusedSceneID = null;
Check(Nav(Direction.DOWN) == J("Norte", "Bosque", Level(), Pos(1, 2)) && centered[^1] == bosque.gameObject &&
    listMap.focusedSceneID == bosque.sceneName, "Down with nothing focused centres and selects the first region's first place, naming the region even with its label hidden");
MapSelectionAnnouncer.Update(listMap, false);
Check(ScreenReader.Messages.Count == 1, "the selection poller does not repeat the list's own speech");
aldea.playerIsHere = true;
Check(Nav(Direction.DOWN) == J("Aldea", Level(), T("waypoint.here"), Pos(2, 2)), "the next place in the region follows its order, with waypoint states");
Check(Nav(Direction.DOWN) == J("Sur", "Costa", Level()), "entering another region names it; a single place has no position");
Check(Nav(Direction.DOWN) == J("Desierto", Level()), "places in no region follow the regions, unnamed");
int snaps = centered.Count;
Check(Nav(Direction.DOWN) == J("Desierto", Level()) && centered.Count == snaps, "the last place repeats instead of wrapping");
Check(Nav(Direction.UP) == J("Sur", "Costa", Level()), "Up walks back and names the region entered");
listMap.focusedSceneID = null;
Check(Nav(Direction.UP) == J("Desierto", Level()), "Up with nothing focused starts from the last place");
listMap.mapZoom.currentZoom = listMap.mapZoom.labelZoomRegionBreakpoint;
Nav(Direction.UP);
Check(listMap.mapZoom.currentZoom == listMap.mapZoom.regionZoomLevel, "zoomed out to regions, a snap first zooms in to where places can be focused");
snaps = centered.Count; listMap.mapTransitionBlocked = true;
Check(Nav(Direction.DOWN) == "" && centered.Count == snaps, "an era crossfade swallows the press without moving");
listMap.mapTransitionBlocked = false; listMap.mapZoom.manualZoomScrollBlocked = true;
Check(Nav(Direction.DOWN) == "" && centered.Count == snaps, "a zoom tween swallows the press without moving");
listMap.mapZoom.manualZoomScrollBlocked = false;
listMap.CurrentEra = SceneList.Era.Divine; listMap.focusedSceneID = null;
Check(Nav(Direction.DOWN) == J(templo.locationLabel.text, Level()), "only the current era's places are listed");
listMap.CurrentEra = SceneList.Era.Ancient;
Check(MapLocationList.Step(4, -1, true) == 0 && MapLocationList.Step(4, -1, false) == 3 &&
    MapLocationList.Step(4, 3, true) == -1 && MapLocationList.Step(4, 0, false) == -1, "steps start from either end and never wrap");

Nav(Direction.DOWN); MapLocationList.CheckSnap(listMap); MapLocationList.CheckSnap(listMap);
Check(LeAccess.Plugin.Log.Warnings.Count == 0, "a snapped selection that holds logs nothing");
Nav(Direction.DOWN); MapLocationList.CheckSnap(listMap); listMap.focusedSceneID = "elsewhere"; MapLocationList.CheckSnap(listMap);
Check(LeAccess.Plugin.Log.Warnings.Count == 1 && LeAccess.Plugin.Log.Warnings[0].Contains("elsewhere"), "a snapped selection the game replaced is logged");

Console.WriteLine($"Waypoint tests passed: {checks} checks");

static class Extensions { internal static T Also<T>(this T value, Action<T> action) { action(value); return value; } }
