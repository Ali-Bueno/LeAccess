using DMM;
using LE.UI.Minimap;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation.Guidance;
using UnityEngine;
using UnityEngine.UI;

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
var world = new MapSectionWorldSnapshot(1);
var targets = new List<GuidanceTarget>();
GameObject Child(GameObject parent = null, bool rect = false)
{
    var go = new GameObject();
    if (rect) go.transform = new RectTransform { gameObject = go };
    go.transform.parent = parent?.transform;
    return go;
}
Image ImageAt(GameObject parent)
{
    var go = Child(parent, true);
    var image = go.Add<Image>();
    image.canvas = parent.GetCanvas();
    return image;
}
MinimapIconManager Setup()
{
    var root = Child(rect: true);
    root.Add<Canvas>();
    var manager = root.Add<MinimapIconManager>();
    manager.minimap = root.Add<Minimap>();
    MinimapIconManager.Instance = manager;
    SceneList.Scenes.Clear(); Localization.Quests = null;
    return manager;
}
MinimapIconManager.IconInfo Register(MinimapIconManager manager, DMMapIcon icon)
{
    var ui = Child(manager.gameObject, true).Add<MinimapIconUI>();
    ui.MainImage = ui.gameObject.Add<Image>();
    ui.MainImage.canvas = ui.gameObject.GetCanvas();
    ui.ArrowImage = ImageAt(ui.gameObject);
    ui.ArrowImage.enabled = false;
    var entry = new MinimapIconManager.IconInfo { icon = icon, ui = ui };
    manager.icons.Add(entry);
    return entry;
}
(SetMapIconColourFromObjectiveCondition owner, Quest quest, MinimapIconManager.IconInfo entry) QuestMarker(MinimapIconManager manager)
{
    var owner = Child().Add<SetMapIconColourFromObjectiveCondition>();
    var quest = new Quest();
    quest.List = new StatefulQuestList { State = new StatefulQuest { quest = quest } };
    var step = new QuestStep { quest = quest };
    owner.objectiveCondition = owner.gameObject.Add<ObjectiveCondition>();
    owner.objectiveCondition.objective = new Objective { quest = quest, questStep = step };
    owner.icon = Child(owner.gameObject).Add<DMMapIcon>();
    owner.icon.transform.position = new(30, 4, 50);
    return (owner, quest, Register(manager, owner.icon));
}
(WorldObjectClickListener listener, DMMapWorldIcon map, MinimapIconManager.IconInfo entry) Exit(MinimapIconManager manager)
{
    var listener = Child().Add<WorldObjectClickListener>();
    var tag = listener.gameObject.Add<HoverTagOnMouseOver>();
    tag.hoverTargetType = HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION;
    tag.sceneInteraction = Child().Add<TransitionOrGateInteraction>();
    tag.sceneInteraction.sceneName = "Z22";
    SceneList.Scenes["Z22"] = new SceneDetails { LocalizedName = "Forest" };
    var map = Child(listener.gameObject).Add<DMMapWorldIcon>();
    var icon = Child(map.gameObject).Add<DMMapIcon>();
    map.MmIcon = icon;
    return (listener, map, Register(manager, icon));
}
void Empty(string message) { Check(GuidanceTargetReader.TryRead(world, targets), message + " ready"); Check(targets.Count == 0, message); }

MinimapIconManager.Instance = null;
targets.Add(default);
Check(!GuidanceTargetReader.TryRead(world, targets) && targets.Count == 0, "missing registry clears stale results");
var manager = Setup();
Check(GuidanceTargetReader.TryRead(world, targets) && targets.Count == 0, "empty ready registry");
manager.icons.WasCollected = true;
Check(!GuidanceTargetReader.TryRead(world, targets), "collected registry unavailable");
manager.icons.WasCollected = false;
var q = QuestMarker(manager);
Check(GuidanceTargetReader.TryRead(world, targets) && targets.Count == 1, "visible active tracked quest");
Check(targets[0].Kind == GuidanceTargetKind.Quest && targets[0].ArrivalRadius == 0, "marker center is not arrival or completion");
Check(targets[0].Position == q.owner.icon.transform.position, "world marker position, not UI position");
Check(targets[0].Id == q.owner.GetInstanceID(), "owner identity");
Check(targets[0].Name.Contains("Find the letter") && targets[0].Name.Contains("Objective marker"), "explicit objective label");
Check(targets[0].Priority == 2, "tracked side quest");
q.quest.mainLineQuest = true; GuidanceTargetReader.TryRead(world, targets); Check(targets[0].Priority == 1, "main quest priority");
q.quest.List.State.trackStatus = LE.Data.SavedQuest.TrackStatus.FORCE_TRACKED;
GuidanceTargetReader.TryRead(world, targets); Check(targets[0].Priority == 0, "pinned quest priority");
Localization.Quests = new(); GuidanceTargetReader.TryRead(world, targets); Check(targets[0].Name.Contains("Localized Find"), "game localization used");
q.owner.arrowIcon = q.owner.icon.gameObject.Add<DMMapIcon>();
Register(manager, q.owner.arrowIcon);
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1, "paired circle and arrow deduplicated");
manager.icons.RemoveAt(1);

q.quest.List.Tracked = false; Empty("untracked quest"); q.quest.List.Tracked = true;
q.quest.List.State.trackStatus = LE.Data.SavedQuest.TrackStatus.FORCE_UNTRACKED; Empty("explicitly unpinned");
q.quest.List.State.trackStatus = LE.Data.SavedQuest.TrackStatus.AUTO_TRACKED;
foreach (var state in new[] { QuestState.Completed, QuestState.Failed, QuestState.NoLongerRelevant, QuestState.NotStarted })
{ q.quest.List.ObjectiveState = state; Empty("nonactive objective " + state); }
q.quest.List.ObjectiveState = QuestState.Active;
q.quest.List.State.State = QuestState.Completed; Empty("quest completed while objective stale"); q.quest.List.State.State = QuestState.Active;
q.quest.playerVisible = false; Empty("hidden quest"); q.quest.playerVisible = true;
q.owner.objectiveCondition.objective.questStep.quest = new Quest(); Empty("foreign step ownership"); q.owner.objectiveCondition.objective.questStep.quest = q.quest;
q.quest.List.State.quest = new Quest(); Empty("foreign local quest projection"); q.quest.List.State.quest = q.quest;
q.owner.icon.gameObject.scene = new() { handle = 2 }; Empty("other scene marker"); q.owner.icon.gameObject.scene = new() { handle = 1 };
q.owner.icon.WasCollected = true; Empty("collected icon"); q.owner.icon.WasCollected = false;
q.owner.icon.enabled = false; Empty("disabled icon"); q.owner.icon.enabled = true;
q.owner.icon.transform.position = new(float.NaN, 0, 0); Empty("invalid marker position"); q.owner.icon.transform.position = new(30, 0, 50);
var actual = q.owner.icon; q.owner.icon = Child().Add<DMMapIcon>(); Empty("unowned nearby condition"); q.owner.icon = actual;
Localization.Quests = null;
q.owner.objectiveCondition.objective.activeText = " "; Empty("blank objective"); q.owner.objectiveCondition.objective.activeText = "Find the letter";

q.entry.ui.MainImage.enabled = false; Empty("hidden circle and arrow");
q.entry.ui.ArrowImage.enabled = true;
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1, "visible edge arrow survives hidden circle");
q.entry.ui.ArrowImage.enabled = false; q.entry.ui.MainImage.enabled = true;
q.entry.ui.MainImage.canvasRenderer.cull = true; Empty("culled marker"); q.entry.ui.MainImage.canvasRenderer.cull = false;
q.entry.ui.MainImage.canvasRenderer.Alpha = 0; Empty("transparent renderer"); q.entry.ui.MainImage.canvasRenderer.Alpha = 1;
q.entry.ui.MainImage.canvasRenderer.Alpha = float.NaN; Empty("invalid renderer alpha"); q.entry.ui.MainImage.canvasRenderer.Alpha = 1;
q.entry.ui.MainImage.color = new() { a = 0 }; Empty("transparent image"); q.entry.ui.MainImage.color = new() { a = 1 };
q.entry.ui.MainImage.rectTransform.Bounds = new(200, 200, 10, 10); Empty("outside minimap shader clip rectangle");
q.entry.ui.MainImage.rectTransform.Bounds = new(90, 90, 20, 20);
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1, "partially shown marker");
var group = q.entry.ui.MainImage.gameObject.Add<CanvasGroup>(); group.alpha = 0; Empty("hidden canvas group"); group.alpha = 1;
q.entry.ui.MainImage.canvas.enabled = false; Empty("disabled canvas"); q.entry.ui.MainImage.canvas.enabled = true;
var uiParent = q.entry.ui.transform.parent;
q.entry.ui.transform.parent = null; Empty("UI outside current manager hierarchy"); q.entry.ui.transform.parent = uiParent;
var ownedImage = q.entry.ui.MainImage;
q.entry.ui.MainImage = ImageAt(manager.gameObject); Empty("image outside paired UI hierarchy"); q.entry.ui.MainImage = ownedImage;
var ownedCanvas = ownedImage.canvas;
ownedImage.canvas = Child().Add<Canvas>(); Empty("image outside native minimap canvas"); ownedImage.canvas = ownedCanvas;
q.quest.ThrowOnRead = true;
Check(!GuidanceTargetReader.TryRead(world, targets) && targets.Count == 0 && GuidanceTargetReader.LastFailure is not null,
    "unreadable quest cannot claim no markers exist");

var e = Exit(manager);
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1 && targets[0].Kind == GuidanceTargetKind.Exit, "one failed entry preserves valid exit");
q.quest.ThrowOnRead = false;
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1 && targets[0].Kind == GuidanceTargetKind.Quest, "quest takes precedence over unrelated exits");
GuidanceTargetReader.TryRead(world, targets, includeExits: true);
Check(targets.Count == 2 && targets.Exists(target => target.Kind == GuidanceTargetKind.Quest) && targets.Exists(target => target.Kind == GuidanceTargetKind.Exit),
    "passive selection can retain verified exit candidates beside a quest whose route may be unavailable");
q.quest.List.Tracked = false;
GuidanceTargetReader.TryRead(world, targets);
Check(targets.Count == 1 && targets[0].Kind == GuidanceTargetKind.Exit && targets[0].Name == "Exit to Forest", "fallback names exit without quest claim");
Check(targets[0].ArrivalRadius == e.listener.interactionRange && targets[0].Priority == 3, "native interaction range only for real exit");
e.listener.Interactable = false; Empty("locked exit"); e.listener.Interactable = true;
e.map.awaitingDestruction = true; Empty("retiring exit icon"); e.map.awaitingDestruction = false;
e.map.MmIcon = Child().Add<DMMapIcon>(); Empty("unowned exit icon"); e.map.MmIcon = e.entry.icon;
var exitTag = e.listener.GetComponent<HoverTagOnMouseOver>();
exitTag.hoverTargetType = HoverTagOnMouseOver.HoverTargetType.SAME_SCENE_TRANSITION; Empty("same-scene label not a named destination");
exitTag.hoverTargetType = HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION;
exitTag.sceneInteraction.enabled = false;
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1, "disabled metadata valid for live native listener");
exitTag.sceneInteraction.sceneName = "unknown"; Empty("unknown destination"); exitTag.sceneInteraction.sceneName = "Z22";
e.listener.hoverTagOnMouseOver = Child().Add<HoverTagOnMouseOver>(); Empty("foreign hover owner"); e.listener.hoverTagOnMouseOver = null;
e.listener.interactionRange = float.PositiveInfinity; Empty("invalid native arrival range"); e.listener.interactionRange = 2;
manager.icons.Insert(0, null); manager.icons.Add(new() { WasCollected = true });
GuidanceTargetReader.TryRead(world, targets); Check(targets.Count == 1, "bad entries don't block valid candidate");
EncounterTests.Run(Check);
Console.WriteLine($"Passed {checks} guidance target checks.");

internal static class Helpers
{
    internal static Canvas GetCanvas(this GameObject go)
    {
        for (var current = go.transform; current != null; current = current.parent)
        {
            var canvas = current.gameObject.Components.OfType<Canvas>().FirstOrDefault();
            if (canvas != null) return canvas;
        }
        return null;
    }
}
