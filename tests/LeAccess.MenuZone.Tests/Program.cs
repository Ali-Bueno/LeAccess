using LeAccess.Accessibility;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Zones;
using UnityEngine;
using UnityEngine.SceneManagement;

int checks = 0;
LeAccess.I18n.ModText.SetLanguage("es");
void Check(bool result, string message)
{
    checks++;
    if (!result) throw new InvalidOperationException(message);
}
void MenuFrame() { ScreenReader.SpokeThisFrame = false; ControllerMenuAccessibility.Update(); }
void ZoneFrame() { ScreenReader.SpokeThisFrame = false; ZoneAnnouncer.Update(); }
void ChangeScene(int id, string key, string name)
{
    SceneManager.Current = new() { handle = id, name = key, isLoaded = true };
    SceneList.Known[key] = new() { LocalizedName = name };
}

var first = new RadialMenuSector();
first._optionText.text = "Inventario";
var radial = new RadialMenu { _selectedSector = first };
var menu = new ControllerMenu { FocusedRadialMenu = radial };
var map = new ControllerMenuMap { controllerMenu = menu };
ControllerManager.instance = new() { _CurrentMap_k__BackingField = map };
Check(ControllerMenuAccessibility.HasActiveContent && ControllerMenuAccessibility.TryHandleFocus(new GameObject()),
    "Dedicated visible radial menu owns generic/background focus even without a Unity Selectable");
MenuFrame();
Check(ControllerMenuAccessibility.HasPendingContent && ScreenReader.Messages.Count == 0, "Radial option waits for settled localized text");
MenuFrame();
Check(ScreenReader.Messages.Single() == ("Menú del mando. Inventario", true), "Radial opening and its selected option are spoken with interruption");
MenuFrame(); MenuFrame();
Check(ScreenReader.Messages.Count == 1, "Holding stick/unchanged radial selection does not repeat");
var second = new RadialMenuSector();
second._optionText.text = "Habilidades";
radial._selectedSector = second;
MenuFrame();
radial._selectedSector = first;
MenuFrame(); MenuFrame();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Inventario", "Rapid selection replaces stale option and returning to previous option rereads it");
var secondary = new RadialMenuSector { _assignedOption = new() { m_TextKey = "menu.inventory" } };
menu.FocusedRadialMenu = new() { _selectedSector = secondary };
MenuFrame(); MenuFrame();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text == "Inventario", "Secondary radial uses its actual focused sector and localized option key when label is absent");
secondary._selected = false;
MenuFrame(); MenuFrame();
Check(ScreenReader.Messages.Count == 3 && !ControllerMenuAccessibility.HasPendingContent, "Retained unselected sector does not announce");
secondary._selected = true;
menu.gameObject.Visible = false;
MenuFrame();
Check(!ControllerMenuAccessibility.HasActiveContent, "Invisible startup/closed menu does not own focus");
menu.gameObject.Visible = true;
ScreenContentAnnouncer.HasActiveContent = true;
MenuFrame(); MenuFrame();
Check(!ControllerMenuAccessibility.HasActiveContent && ScreenReader.Messages.Count == 3, "Tutorial/modal ownership blocks radial background speech");
ScreenContentAnnouncer.HasActiveContent = false;
DialogueAnnouncer.HasActiveContent = true;
MenuFrame();
Check(!ControllerMenuAccessibility.HasActiveContent, "Dialogue retains foreground ownership over radial menu");
DialogueAnnouncer.HasActiveContent = false;
ControllerManager.instance._CurrentMap_k__BackingField = new NativeObject();
MenuFrame();
Check(!ControllerMenuAccessibility.HasActiveContent, "Changing controller input map invalidates retained radial state");
ControllerManager.instance._CurrentMap_k__BackingField = map;
secondary.WasCollected = true;
MenuFrame();
Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Text == "Menú del mando" &&
    LeAccess.Plugin.Log.Warnings.Count == 0, "A collected sector is not read, but the reopened menu can announce itself");
secondary.WasCollected = false;
ControllerMenuAccessibility.Reset();
ControllerManager.instance._CurrentMap_k__BackingField = new NativeObject();
ScreenReader.Messages.Clear();
ControllerManager.instance._CurrentMap_k__BackingField = map;
secondary.WasCollected = false;
menu.FocusedRadialMenu._selectedSector = null;
MenuFrame(); MenuFrame(); MenuFrame();
Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0] == ("Menú del mando", true),
    "Holding the radial button without stick movement announces opening once even with no selected sector");
menu.FocusedRadialMenu._selectedSector = first;
MenuFrame(); MenuFrame();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Inventario",
    "Moving from the neutral position reads the selected option without repeating the opening title");
ControllerManager.instance._CurrentMap_k__BackingField = new NativeObject(); MenuFrame();
ControllerManager.instance._CurrentMap_k__BackingField = map;
menu.FocusedRadialMenu._selectedSector = null;
ScreenReader.Available = false; MenuFrame();
Check(ControllerMenuAccessibility.HasPendingContent && ScreenReader.Messages.Count == 2,
    "Opening remains pending while the screen reader is unavailable");
ScreenReader.Available = true; MenuFrame();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text == "Menú del mando",
    "Reopening the same radial widget announces a new opening when speech becomes available");
ControllerMenuAccessibility.Reset();
ControllerManager.instance._CurrentMap_k__BackingField = new NativeObject();
ScreenReader.Messages.Clear();
PlayerFinder.player = new();

var state = new ZoneAnnouncementState();
state.EnterScene(1);
Check(!state.Observe("First area", false, out _) && state.HasPendingContent, "Zone remains pending while output unavailable");
state.EnterScene(2);
Check(!state.Observe("Second area", true, out _), "New scene discards older queued zone");
Check(state.Observe("Second area", true, out var spoken) && spoken == "Second area", "Only latest stable scene is delivered");
state.Suspend();
Check(!state.Observe("Second area", true, out _) && !state.HasPendingContent, "Focus/loading suspension cannot repeat a previously delivered zone");

ChangeScene(10, "camp", "Campamento");
LE.UI.LoadingScreen.IsVisible = true;
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == 0 && !ZoneAnnouncer.HasPendingContent, "Loading screen never speaks world area text");
LE.UI.LoadingScreen.IsVisible = false;
ZoneFrame();
Check(ZoneAnnouncer.HasPendingContent, "Loaded actual scene name settles before announcing");
ZoneFrame();
Check(ScreenReader.Messages.Single() == ("Campamento", true), "Zone entry interrupts old passive speech backlog with authoritative localized name");
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == 1, "Same scene is narrated only once");
Application.isFocused = false;
ZoneFrame();
Application.isFocused = true;
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == 1, "Alt-tab return does not replay the current zone");

ChangeScene(11, "old", "Antigua zona");
ScreenContentAnnouncer.HasActiveContent = true;
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == 1 && ZoneAnnouncer.HasPendingContent, "Blocking tutorial keeps zone locally pending instead of queueing it in the screen reader");
ChangeScene(12, "new", "Zona nueva");
ZoneFrame();
ScreenContentAnnouncer.HasActiveContent = false;
ZoneFrame();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text == "Zona nueva", "Pending old area is discarded when player enters another scene");

var notification = new LocationNotificationManager();
notification.nameText.text = "Zona nueva";
notification.holder.Text = "Año antiguo";
ZoneAnnouncer.TrackLocation(notification);
ChangeScene(13, "forest", "Bosque en llamas");
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Last().Text == "Bosque en llamas", "Previous-scene notification cannot supply an old title/year");
ChangeScene(14, "river", "Río");
ZoneAnnouncer.TrackLocation(notification);
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Last().Text == "Río", "Same-scene callback with stale wrong title cannot replace current localized name");
ChangeScene(15, "harbour", "Puerto");
notification.nameText.text = "Puerto";
notification.holder.Text = "Era Divina: -12 a. E.";
ZoneAnnouncer.TrackLocation(notification);
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Last() == ("Puerto. Era Divina: -12 a. E.", true), "Matching visible location notification may add actual era/year content once");
int delivered = ScreenReader.Messages.Count;
ZoneAnnouncer.TrackLocation(notification);
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == delivered, "Repeated location callback cannot duplicate a delivered scene announcement");

ChangeScene(16, "cliffs", "Acantilados");
GameplayInputState.Gameplay = false;
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == delivered && ZoneAnnouncer.HasPendingContent, "Inventory/nonpausing menu cannot be interrupted by area narration");
GameplayInputState.Gameplay = true;
ScreenReader.SpokeThisFrame = true;
ZoneAnnouncer.Update();
Check(ScreenReader.Messages.Count == delivered, "Existing foreground speech wins its frame");
ZoneFrame();
Check(ScreenReader.Messages.Last().Text == "Acantilados", "Pending zone speaks as soon as gameplay foreground is available");
ChangeScene(17, "unavailable", "Unused");
SceneList.Known.Remove("unavailable");
ZoneFrame(); ZoneFrame();
Check(!ZoneAnnouncer.HasPendingContent && ScreenReader.Messages.Count == delivered + 1, "Unknown scene metadata never falls back to an internal scene code");
ChangeScene(18, "stale", "Stale");
SceneList.Known["stale"].WasCollected = true;
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == delivered + 1 && LeAccess.Plugin.Log.Warnings.Count == 0, "Collected scene metadata stays silent without wrapper access");
ChangeScene(19, "paused", "Paused area");
NavigationAudioState.IsSuspended = true;
ZoneFrame(); ZoneFrame();
Check(ScreenReader.Messages.Count == delivered + 1, "Paused gameplay does not trigger a zone interruption");
NavigationAudioState.IsSuspended = false;
ZoneFrame();
Check(ScreenReader.Messages.Last().Text == "Paused area", "Pending zone survives pause until gameplay resumes");
ZoneAnnouncer.Reset();
ControllerMenuAccessibility.Reset();
Console.WriteLine($"Controller menu/zone tests passed: {checks} checks.");
