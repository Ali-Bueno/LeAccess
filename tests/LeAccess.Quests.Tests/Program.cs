using LeAccess.Accessibility;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.I18n;
using TMPro;
using UnityEngine;

int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
GameObject Child(Transform parent) { var go = new GameObject(); go.transform.parent = parent; return go; }
TextMeshProUGUI Label(Transform parent, string text) => Child(parent).Add(new TextMeshProUGUI { text = text });
void Tick() { ScreenReader.SpokeThisFrame = false; QuestAccessibility.Update(); }
string Read(QuestButtonGenerator generator)
{
    Check(QuestSelectionReader.TryRead(generator, out var selection), "valid selected quest");
    return selection.Text;
}
var panelGo = new GameObject();
var panel = panelGo.Add(new QuestPanel());
var generatorGo = Child(panelGo.transform);
var generator = generatorGo.Add(new QuestButtonGenerator { questPanel = panel });
panel.buttonGenerator = generator;
var quest = new Quest();
var buttonGo = Child(generatorGo.transform);
var button = buttonGo.Add(new QuestButton { quest = quest, buttonGenerator = generator });
button.tmp = Label(button.transform, "The Ruined World");
button.chapterText = Label(button.transform, "Chapter 1");
generator._selectedButton_k__BackingField = button;
var infoGo = Child(panelGo.transform);
var info = infoGo.Add(new QuestInformation { quest = quest, previousQuest = quest });
generator.questInformation = info;
info.allContent = Child(infoGo.transform);
info.questName = Label(info.transform, "The Ruined World");
info.status = Label(info.transform, "In progress");
info.details = Label(info.transform, "Find the keeper and discover what happened.");
info.objectivesHolder = Child(info.allContent.transform).transform;
info.rewardsHolder = Child(info.allContent.transform);
var objective = Child(info.objectivesHolder).Add(new QuestViewObjectiveUI());
objective.text = Label(objective.transform, "Speak to the keeper");
objective.check = Child(objective.transform);
objective.check.activeInHierarchy = false;
info.objectives.Add(objective);
var reward = Child(info.rewardsHolder.transform).Add(new RewardViewObjectUI());
reward.text = Label(reward.transform, "100 gold");
info.rewards.Add(reward);
var navigation = panelGo.Add(new PrioritizedNavigable { ActiveSelectable = button });
NavigationManager.currentlySelected = navigation;
ModText.Initialize(null, null);

string full = Read(generator);
foreach (string part in new[] { "The Ruined World", "Chapter 1", "Not pinned", "In progress", "Find the keeper", "Speak to the keeper", "100 gold" })
    Check(full.Contains(part), "structured content " + part);
Check(QuestAccessibility.TryHandleFocus(buttonGo), "selected button focus claimed");
Check(!QuestAccessibility.TryHandleFocus(Child(button.transform)), "nested control not swallowed");
Check(!QuestAccessibility.TryHandleFocus(Child(panel.transform)), "panel tabs not swallowed");
Tick();
Check(QuestAccessibility.HasActiveContent, "active journal");
Check(ScreenReader.Spoken.Count == 0 && QuestAccessibility.HasPendingContent, "first observation settles");
Tick();
Check(ScreenReader.Spoken.Count == 1 && ScreenReader.Spoken.Last().Contains("Speak to the keeper") &&
    ScreenReader.Spoken.Last().Contains("In progress") && !ScreenReader.Spoken.Last().Contains("100 gold") &&
    !ScreenReader.Spoken.Last().Contains("Find the keeper"), "settled navigation includes objective and state, defers long detail");
Tick();
Check(ScreenReader.Spoken.Count == 1 && !QuestAccessibility.HasPendingContent, "unchanged silent");
button.pinned = true;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("Pinned"), "pin update stationary");
button.completed = true;
objective.check.activeInHierarchy = true;
Tick(); Tick();
Check(ScreenReader.Spoken.Last().Contains("Completed") && !ScreenReader.Spoken.Last().Contains("Speak to the keeper"),
    "completed quest navigation does not read its old objectives as current tasks");
Check(Read(generator).Contains("Objective history for this quest: Speak to the keeper (Completed)"),
    "fallback manual content explicitly identifies completed rows as this quest's history");
int spoken = ScreenReader.Spoken.Count;
var secondQuest = new Quest();
button.quest = secondQuest;
button.tmp.text = "A New Journey";
string brief = Read(generator);
Check(brief.Contains("A New Journey") && !brief.Contains("keeper") && !brief.Contains("100 gold"), "old quest owner rejected");
Tick();
button.quest = quest; button.tmp.text = "The Ruined World";
Tick();
Check(ScreenReader.Spoken.Count == spoken, "obsolete pending selection discarded");
Tick();
Check(ScreenReader.Spoken.Last().Contains("The Ruined World"), "return selection speaks");
info.previousQuest = secondQuest;
Check(!Read(generator).Contains("keeper"), "render initialization incomplete");
info.previousQuest = quest;
info.questName.text = "A New Journey";
Check(!Read(generator).Contains("keeper"), "stale displayed title rejected");
info.questName.text = button.tmp.text;
objective.transform.parent = new GameObject().transform;
Check(!Read(generator).Contains("Speak to"), "foreign objective excluded");
objective.transform.parent = info.objectivesHolder;
reward.transform.parent = new GameObject().transform;
Check(!Read(generator).Contains("100 gold"), "foreign reward excluded");
reward.transform.parent = info.rewardsHolder.transform;
objective.text.canvasRenderer.cull = true;
Check(!Read(generator).Contains("Speak to"), "culled objective excluded");
objective.text.canvasRenderer.cull = false;
reward.text.alpha = 0;
Check(!Read(generator).Contains("100 gold"), "transparent reward excluded");
reward.text.alpha = 1;
info.allContent.activeInHierarchy = false;
Check(!Read(generator).Contains("keeper"), "hidden details excluded");
info.allContent.activeInHierarchy = true;
button.tmp.alpha = 0;
Check(!QuestSelectionReader.TryRead(generator, out _), "no visible selected title");
Tick();
Check(!QuestAccessibility.HasActiveContent, "no title clears active content");
button.tmp.alpha = 1;
var foreignGenerator = new GameObject().Add(new QuestButtonGenerator());
button.buttonGenerator = foreignGenerator;
Check(!QuestSelectionReader.TryRead(generator, out _), "wrong button owner rejected");
button.buttonGenerator = generator;
button.WasCollected = true;
Tick();
Check(!QuestAccessibility.HasActiveContent, "collected selected button");
button.WasCollected = false;
generator._selectedButton_k__BackingField = null;
Tick();
Check(!QuestAccessibility.HasActiveContent, "missing selected quest");
Tick();
Check(!QuestAccessibility.HasPendingContent, "missing quest drops pending");
generator._selectedButton_k__BackingField = button;
panel.questsHidden = true;
Tick();
Check(!QuestAccessibility.HasActiveContent, "collapsed journal hidden");
panel.questsHidden = false;
panelGo.activeInHierarchy = false;
Tick();
Check(!QuestAccessibility.HasActiveContent, "closed panel hidden");
panelGo.activeInHierarchy = true;
foreach (string gate in new[] { "popup", "dialogue", "wheel", "loading", "focus" })
{
    ScreenContentAnnouncer.HasActiveContent = gate == "popup";
    DialogueAnnouncer.HasActiveContent = gate == "dialogue";
    ControllerMenuAccessibility.HasActiveContent = gate == "wheel";
    LE.UI.LoadingScreen.IsVisible = gate == "loading";
    Application.isFocused = gate != "focus";
    spoken = ScreenReader.Spoken.Count;
    Tick(); Tick();
    Check(!QuestAccessibility.HasActiveContent && !QuestAccessibility.HasPendingContent && ScreenReader.Spoken.Count == spoken, "priority gate " + gate);
    Check(!QuestAccessibility.TryHandleFocus(buttonGo), "blocked focus " + gate);
}
ScreenContentAnnouncer.HasActiveContent = DialogueAnnouncer.HasActiveContent = ControllerMenuAccessibility.HasActiveContent = LE.UI.LoadingScreen.IsVisible = false;
Application.isFocused = true;
QuestAccessibility.Reset();
ScreenReader.Available = false;
Tick(); Tick();
Check(QuestAccessibility.HasPendingContent, "screen reader unavailable keeps current pending");
ScreenReader.Available = true;
Tick();
Check(!QuestAccessibility.HasPendingContent, "availability flushes current quest");
NavigationManager.currentlySelected = new GameObject().Add(new PrioritizedNavigable());
Tick();
Check(!QuestAccessibility.HasActiveContent, "unrelated navigation cannot reuse cache");
NavigationManager.currentlySelected = navigation;
ModText.SetLanguage("es");
QuestAccessibility.Reset();
Tick(); Tick();
Check(ScreenReader.Spoken.Last().Contains("Fijada") && ScreenReader.Spoken.Last().Contains("Completada"), "localized quest states");
Check(ScreenReader.Spoken.Last().Contains("The Ruined World"), "game title preserved");
ModText.SetLanguage("en");
QuestAccessibility.Reset();
Tick(); Tick();
spoken = ScreenReader.Spoken.Count;
info.allContent.activeInHierarchy = false;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken && !QuestAccessibility.HasPendingContent, "hidden detail does not repeat summary");
info.allContent.activeInHierarchy = true;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken, "restored unchanged detail does not repeat");
info.allContent.activeInHierarchy = false;
button.pinned = false;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1 && ScreenReader.Spoken.Last().Contains("Not pinned") &&
    !ScreenReader.Spoken.Last().Contains("keeper"), "hidden pin change speaks only current summary");
spoken = ScreenReader.Spoken.Count;
info.allContent.activeInHierarchy = true;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken, "same details after hidden pin change acknowledged");
info.allContent.activeInHierarchy = false;
button.completed = false;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1 && !ScreenReader.Spoken.Last().Contains("Completed") &&
    !ScreenReader.Spoken.Last().Contains("keeper"), "hidden completion change has no stale detail");
spoken = ScreenReader.Spoken.Count;
button.chapterText.text = "Chapter 2";
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1 && ScreenReader.Spoken.Last().Contains("Chapter 2"), "hidden summary change speaks");
spoken = ScreenReader.Spoken.Count;
info.allContent.activeInHierarchy = true;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1 && ScreenReader.Spoken.Last().Contains("In progress") &&
    !ScreenReader.Spoken.Last().Contains("Speak to the keeper"),
    "restored fallback clarifies the changed quest state without reintroducing completed history");
spoken = ScreenReader.Spoken.Count;
objective.text.text = "Find a different keeper";
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken, "Changing completed fallback history cannot trigger navigation speech");
objective.check.activeInHierarchy = false;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1 && ScreenReader.Spoken.Last().Contains("different keeper"), "changed objective still speaks");
spoken = ScreenReader.Spoken.Count;
info.allContent.activeInHierarchy = false;
button.quest = secondQuest;
button.tmp.text = "Another Quest";
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1 && ScreenReader.Spoken.Last().Contains("Another Quest") &&
    !ScreenReader.Spoken.Last().Contains("keeper"), "hidden cross-quest cannot reuse spoken details");
spoken = ScreenReader.Spoken.Count;
info.quest = info.previousQuest = secondQuest;
info.questName.text = "Another Quest";
info.allContent.activeInHierarchy = true;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken + 1, "new quest detail is not suppressed");
navigation.isActiveAndEnabled = false;
Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent && !QuestAccessibility.HasPendingContent, "disabled navigation drops quest ownership");
Check(!QuestAccessibility.TryHandleFocus(buttonGo), "disabled navigation does not swallow focus");
navigation.isActiveAndEnabled = true;
Tick(); Tick();
Check(QuestAccessibility.HasActiveContent, "enabled navigation restores current quest");
var mapGo = new GameObject();
var map = mapGo.Add(new MapPanel { questPanel = panel });
var worldMap = new GameObject().Add(new LE.UI.PanelSystem.WorldMapPanel { _mapPanel = map });
NavigationManager.currentlySelected = worldMap;
QuestAccessibility.Reset();
spoken = ScreenReader.Spoken.Count;
Tick(); Tick();
Check(QuestAccessibility.HasActiveContent && ScreenReader.Spoken.Count == spoken + 1,
    "modern world map owns declared quest panel without ancestor relationship");
NavigationManager.currentlySelected = map;
QuestAccessibility.Reset(); Tick(); Tick();
Check(QuestAccessibility.HasActiveContent, "legacy map owns declared quest panel");
map.isActiveAndEnabled = false;
NavigationManager.currentlySelected = worldMap;
Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent, "disabled declared map cannot borrow cached quest");
map.isActiveAndEnabled = true;
var unrelatedPanel = new GameObject().Add(new QuestPanel { buttonGenerator = generator });
map.questPanel = unrelatedPanel;
QuestAccessibility.Reset(); Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent, "map quest panel requires reciprocal generator ownership");
map.questPanel = panel;
NavigationManager.instance = new GameObject().Add(new NavigationManager { RegisteredMap = worldMap });
NavigationManager.currentlySelected = info;
QuestAccessibility.Reset(); Tick(); Tick();
Check(QuestAccessibility.HasActiveContent, "cold information navigation resolves registered map ownership");
// Remove hierarchy discovery so this specifically exercises the native registry route.
infoGo.transform.parent = null;
QuestAccessibility.Reset(); Tick(); Tick();
Check(QuestAccessibility.HasActiveContent, "separate information root discovered by exact map reference");
var foreignInfo = new GameObject().Add(new QuestInformation());
NavigationManager.currentlySelected = foreignInfo;
QuestAccessibility.Reset(); Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent, "foreign information cannot borrow registered map quest");
NavigationManager.currentlySelected = info;
worldMap.isActiveAndEnabled = false;
QuestAccessibility.Reset(); Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent, "disabled registered world map rejected");
worldMap.isActiveAndEnabled = true;
panel.questsHidden = true;
QuestAccessibility.Reset(); Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent, "map ownership does not override hidden quests");
panel.questsHidden = false;
NavigationManager.instance.RegisteredMap = null;
QuestAccessibility.Reset(); Tick(); Tick();
Check(!QuestAccessibility.HasActiveContent, "cold isolated information requires discoverable exact owner");
NavigationManager.currentlySelected = worldMap;
Tick(); Tick();
spoken = ScreenReader.Spoken.Count;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == spoken, "stable map selection does not repeat quest");
var state = new QuestAnnouncementState();
Check(!state.Observe("A", "old", null, false, out _), "state first observation");
Check(!state.Observe("B", "new", null, true, out _), "state selection changes settle");
Check(state.Observe("B", "new", null, true, out string latest) && latest == "new", "only latest speaks");
Check(!state.Observe("B", "new", null, true, out _), "state duplicate silent");
state.Reset();
Check(!state.HasPendingContent, "state reset");
Check(LeAccess.Plugin.Log.Information.Count == ScreenReader.Spoken.Count &&
    LeAccess.Plugin.Log.Information.All(line => line.StartsWith("[Quest] ")), "one diagnostic per delivered announcement");
checks += ModelTests.Run();
NavigationManager.currentlySelected = worldMap;
QuestAccessibility.Reset(); ScreenReader.Spoken.Clear(); ScreenReader.OutputAccepted = false;
Tick(); Tick();
Check(ScreenReader.Spoken.Count == 0 && QuestAccessibility.HasPendingContent,
    "Rejected journal output remains pending instead of being acknowledged");
ScreenReader.OutputAccepted = true; Tick(); Tick();
Check(ScreenReader.Spoken.Count == 1 && !QuestAccessibility.HasPendingContent,
    "Recovered current journal content speaks exactly once");
QuestAccessibility.Reset(); ScreenReader.Spoken.Clear();
ScreenReader.OutputAccepted = false;
Check(QuestAccessibility.ReadCurrentDetails() && QuestAccessibility.HasPendingContent,
    "Rejected manual quest detail preserves focus-bound request intent");
info.details.text = "Fresh current journal description";
ScreenReader.OutputAccepted = true; ScreenReader.SpokeThisFrame = true;
QuestAccessibility.Update();
Check(ScreenReader.Spoken.Count == 0 && QuestAccessibility.HasPendingContent,
    "Automatic manual-journal retry yields to an accepted same-frame output");
Tick(); Tick();
Check(ScreenReader.Spoken.Single().Contains("Fresh current journal description"),
    "Recovered journal manual intent reads fresh details rather than old text");
ScreenReader.Available = false; QuestAccessibility.ReadCurrentDetails();
Check(QuestAccessibility.HasPendingContent, "New manual journal intent remains pending while the transport is unavailable after accepted navigation");
ScreenReader.Available = true; Tick(); Tick();
Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("Fresh current journal description"),
    "Still-owned unavailable manual journal request recovers once");
QuestAccessibility.Reset(); ScreenReader.Spoken.Clear(); ScreenReader.OutputAccepted = false;
QuestAccessibility.ReadCurrentDetails();
panel.questsHidden = true; ScreenReader.OutputAccepted = true; Tick(); Tick();
Check(ScreenReader.Spoken.Count == 0 && !QuestAccessibility.HasPendingContent,
    "Hidden rejected journal details cannot replay");
panel.questsHidden = false; QuestAccessibility.Reset();
foreach (string gate in new[] { "changed", "collected", "unready", "popup" })
{
    ScreenReader.Spoken.Clear(); ScreenReader.OutputAccepted = false;
    QuestAccessibility.ReadCurrentDetails();
    var originalQuest = button.quest;
    if (gate == "changed") { button.quest = new Quest(); button.tmp.text = "Next current quest"; }
    if (gate == "collected") generator.WasCollected = true;
    if (gate == "unready") generator._selectedButton_k__BackingField = null;
    if (gate == "popup") ScreenContentAnnouncer.HasActiveContent = true;
    ScreenReader.OutputAccepted = true; Tick(); Tick();
    Check(ScreenReader.Spoken.All(text => !text.Contains("Fresh current journal description")) &&
        (gate == "changed" ? ScreenReader.Spoken.Single().Contains("Next current quest") : ScreenReader.Spoken.Count == 0),
        "Rejected manual journal intent expires when ownership changes: " + gate);
    generator.WasCollected = false; generator._selectedButton_k__BackingField = button;
    button.quest = originalQuest; button.tmp.text = info.questName.text;
    ScreenContentAnnouncer.HasActiveContent = false; QuestAccessibility.Reset();
}
Console.WriteLine($"Passed {checks} quest journal checks.");
