using LeAccess.Accessibility;
using LeAccess.Accessibility.Passives;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.Tooltips;
using UnityEngine;

int checks = 0;
void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
var tree = new GameObject().Add(new Tree { unspentPoints = 3 });
var first = new GameObject().Add(new SkillTreeNode { tree = tree });
var second = new GameObject().Add(new SkillTreeNode { tree = tree, Name = "Second passive" });
var owner = new GameObject().Add(new PassiveTreeNavigable { _currentColumn = 2, _currentRow = 1 });
var cells = new Il2CppSystem.Array { Cells = new NativeObject[4, 3] };
var cell = first.gameObject;
var source = cell.Add(new SkillnodeTooltip { node = first });
cells.Cells[2, 1] = cell;
owner._navigationElements = cells;
NavigationManager.currentlySelected = owner;
var bareModel = new NodeTooltipInfo(first, false);
bareModel.Refresh();
Check(bareModel.Name is null && bareModel.Description is null, "native bare constructor and Refresh do not initialize strings");
Check(PassiveSelectionReader.TryRead(out var read), "selected cell readable");
Check(NodeTooltipContentBuilder.Builds == 2, "fallback and keyword text each use the populated native builder once");
Check(read.Text.Contains("Localized passive") && read.Text.Contains(F("node.points", 0, 5)) &&
    read.Text.Contains("Game description") && read.Text.Contains("Localized modifier") &&
    read.Text.Contains("Localized requirement"), "full localized data");
int refreshes = NodeTooltipInfo.Refreshes;
for (int i = 0; i < 20; i++) Check(PassiveSelectionReader.TryRead(out _), "stable reader");
Check(NodeTooltipInfo.Refreshes == refreshes, "stable frames do not rebuild modifiers");
Check(UnityEngine.Object.Searches == 0, "no scene scans");
first.pointsAllocated = 1;
Check(PassiveSelectionReader.TryRead(out read) && read.Text.Contains(F("node.points", 1, 5)), "allocated points update");
Check(NodeTooltipInfo.Refreshes == refreshes + 1, "changed points refresh");
tree.unspentPoints--;
PassiveSelectionReader.TryRead(out _);
Check(NodeTooltipInfo.Refreshes == refreshes + 2, "other tree allocation refresh");
Check(PassiveAccessibility.TryHandleFocus(cell), "owned focus handled");
Check(!PassiveAccessibility.TryHandleFocus(new GameObject()), "unrelated focus untouched");
void Tick() { ScreenReader.SpokeThisFrame = false; PassiveAccessibility.Update(); }
Tick(); Check(PassiveAccessibility.HasPendingContent, "first observation pending");
Tick(); Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Interrupt, "stable selection interrupts");
Tick(); Check(ScreenReader.Messages.Count == 1, "held selection silent");
first.pointsAllocated++;
Tick(); Tick(); Check(ScreenReader.Messages.Count == 2, "point allocation announces once");
ScreenContentAnnouncer.HasActiveContent = true;
Tick(); Check(!PassiveAccessibility.HasActiveContent && !PassiveAccessibility.HasPendingContent, "modal priority");
ScreenContentAnnouncer.HasActiveContent = false;
LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = true;
Check(!PassiveSelectionReader.TryRead(out _), "dialogue priority");
LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = true;
Check(!PassiveSelectionReader.TryRead(out _), "wheel priority");
LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = false;
owner._currentColumn = -1;
Check(!PassiveSelectionReader.TryRead(out _), "negative index safe");
owner._currentColumn = 4;
Check(!PassiveSelectionReader.TryRead(out _), "out of bounds safe");
owner._currentColumn = 2; cells.Rank = 1;
Check(!PassiveSelectionReader.TryRead(out _), "wrong rank safe");
cells.Rank = 2; first.Passive = false;
Check(!PassiveSelectionReader.TryRead(out _), "specialization excluded");
first.Passive = true; cell.Active = false;
Check(!PassiveSelectionReader.TryRead(out _), "hidden selected cell excluded");
cell.Active = true; first.WasCollected = true;
Check(!PassiveSelectionReader.TryRead(out _), "collected node safe");
first.WasCollected = false;
PassiveAccessibility.Reset();
var system = new GameObject().Add(new TooltipSystem());
system._nodeTooltip = new GameObject().Add(new NodeTooltip());
system._activeNodeTooltipInfo = NodeTooltipContentBuilder.GetNodeTooltipInfo(second, false);
TooltipSystem.Found = system;
UIBase.instance = new GameObject().Add(new UIBase { TooltipSystem = system });
system.ActiveNodeTooltipNode = second;
system._nodeTooltip._info = system._activeNodeTooltipInfo;
Check(PassiveSelectionReader.TryRead(out read) && !read.Text.Contains("Second passive"), "stale tooltip never speaks");
system._activeNodeTooltipInfo = NodeTooltipContentBuilder.GetNodeTooltipInfo(first, false);
system._activeNodeTooltipInfo.PointBonusDescription = "Owned bonus";
system.ActiveNodeTooltipNode = first;
Check(PassiveSelectionReader.TryRead(out read) && !read.Text.Contains("Owned bonus"), "mismatched bound info excluded");
system._nodeTooltip._info = system._activeNodeTooltipInfo;
Check(PassiveSelectionReader.TryRead(out read) && read.Text.Contains("Owned bonus"), "matching displayed tooltip preferred");
Check(read.Text.Contains(F("node.requirement_unmet", "Localized requirement")), "unmet state preserved after cleaning colors");
system._activeNodeTooltipInfo.Requirements[0].IsMet = true;
Check(PassiveSelectionReader.TryRead(out read) && read.Text.Contains(F("node.requirement_met", "Localized requirement")), "met state preserved");
system.ActiveNodeTooltipNode = second;
Check(PassiveSelectionReader.TryRead(out read) && !read.Text.Contains("Owned bonus"), "mismatched active node excluded");
system.ActiveNodeTooltipNode = first;
system._nodeTooltip.Enabled = false;
Check(PassiveSelectionReader.TryRead(out read) && !read.Text.Contains("Owned bonus"), "disabled tooltip excluded");
system._nodeTooltip.Enabled = true;
first.Enabled = false;
Check(!PassiveSelectionReader.TryRead(out _), "disabled node excluded");
first.Enabled = true;
system._nodeTooltipRequiresLayout = true;
Check(PassiveSelectionReader.TryRead(out read) && !read.Text.Contains("Owned bonus"), "unfinished tooltip ignored");
Application.isFocused = false;
Check(!PassiveSelectionReader.TryRead(out _), "unfocused application excluded");
Application.isFocused = true;
PassiveAccessibility.Reset();
Check(!PassiveAccessibility.HasActiveContent && !PassiveAccessibility.HasPendingContent, "reset releases ownership");
system._nodeTooltipRequiresLayout = true;
Tick(); Tick();
refreshes = NodeTooltipInfo.Refreshes;
NavigationManager.currentlySelected = null;
Tick();
NavigationManager.currentlySelected = owner;
Tick();
Check(NodeTooltipInfo.Refreshes == refreshes + 2, "reopen refreshes fallback and keyword text despite unchanged node counts");
var fullscreen = new GameObject().Add(new LE.UI.PanelSystem.FullScreenPanelPassiveTree { ActiveChildNavigable = owner });
NavigationManager.currentlySelected = fullscreen;
Check(PassiveSelectionReader.TryRead(out read), "modern fullscreen delegates to active passive child");
Check(read.Summary.Contains("Localized passive") && read.Summary.Contains(F("node.points", first.pointsAllocated, first.maxPoints)) &&
    read.Summary.Contains(T("node.locked")) && !read.Summary.Contains("Game description"), "brief selection contains points and status only");
Check(read.Details.Contains("Game description") && read.Details.Contains("Localized modifier"), "full native details retained");
var anchor = new GameObject().Add(new SkillTreeNode { tree = first.tree, Name = "Valiente" });
first.requirements = new(new[] { new RequirementFromNode { node = anchor, requirement = 1 } });
first.tree.nodeList = new(new[] { anchor, first });
PassiveSelectionReader.Reset();
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains(F("skill.requires", F("skill.requires_point", 1, anchor.Name, 0))) &&
    !read.Summary.Contains(T("node.locked")), "A node held back by another names it and the points it lacks: " + read.Summary);
Check(read.Details.Contains(F("skill.path", anchor.Name)), "Details give the route to the node");
PassiveTreeLinks.Read(anchor, node => node.Name, out string anchorRequires, out string anchorLinks);
Check(anchorRequires == null && anchorLinks == F("skill.leads_to", first.Name), "A node names what it leads to");
first.requirements = null; first.tree.nodeList = null; PassiveSelectionReader.Reset();
fullscreen.Enabled = false;
Check(!PassiveSelectionReader.TryRead(out _), "disabled modern panel rejected");
fullscreen.Enabled = true; fullscreen.gameObject.Active = false;
Check(!PassiveSelectionReader.TryRead(out _), "hidden modern panel rejected");
fullscreen.gameObject.Active = true;
fullscreen.ActiveChildNavigable = new GameObject().Add(new Canvas());
Check(!PassiveSelectionReader.TryRead(out _), "modern header or other active child cannot reuse passive selection");
fullscreen.ActiveChildNavigable = null;
Check(!PassiveSelectionReader.TryRead(out _), "missing modern child rejected");
fullscreen.ActiveChildNavigable = owner;
PassiveAccessibility.Reset();
int spoken = ScreenReader.Messages.Count;
Tick(); Tick();
Check(ScreenReader.Messages.Count == spoken + 1 && !ScreenReader.Messages.Last().Text.Contains("Game description"),
    "modern navigation speaks only brief");
first.requirementsMet = true;
Tick(); Tick();
Check(ScreenReader.Messages.Count == spoken + 2 && !ScreenReader.Messages.Last().Text.Contains(T("node.locked")),
    "Meeting the requirements speaks the brief once, now without a lock");
Check(PassiveAccessibility.ReadCurrentDetails() && ScreenReader.Messages.Last().Text.Contains("Game description") &&
    ScreenReader.Messages.Last().Text.Contains("Localized modifier"), "manual gesture speaks native details");
Check(!ScreenReader.Messages.Last().Text.Contains(F("list.position", 1, 1)) &&
    ScreenReader.Messages.Last().Text.StartsWith("Localized passive"), "A node's details request starts at its name");
spoken = ScreenReader.Messages.Count;
Tick(); Tick();
Check(ScreenReader.Messages.Count == spoken, "polling does not interrupt manual details");
NodeTooltipContentBuilder.KeywordText = "<color=#fff>Chill</color> slows enemies.";
PassiveSelectionReader.Reset(); KeywordExplanation.Reset();
Check(PassiveSelectionReader.TryRead(out read) && read.Details.Contains(Join(T("node.more_info"), "Chill slows enemies.")),
    "The keyword explanation joins the details, cleaned and led in");
Check(LeAccess.Plugin.Log.Information.Any(line => line.StartsWith("[Tooltips] keyword text: Chill slows enemies.")), "What the game returned is logged");
KeywordExplanation.Enabled = false; PassiveSelectionReader.Reset();
Check(PassiveSelectionReader.TryRead(out read) && !read.Details.Contains("Chill"), "The keyword explanation can be switched off");
KeywordExplanation.Enabled = true; NodeTooltipContentBuilder.KeywordText = null; PassiveSelectionReader.Reset(); KeywordExplanation.Reset();
Check(PassiveSelectionReader.TryRead(out read) && !read.Details.Contains(T("node.more_info")) &&
    LeAccess.Plugin.Log.Information.Any(line => line.StartsWith("[Tooltips] keyword text: empty")), "An empty alt text adds nothing and is logged");
PassiveAccessibility.Reset();
Tick();
Check(PassiveAccessibility.HasPendingContent, "new brief pending before manual request");
PassiveAccessibility.ReadCurrentDetails();
spoken = ScreenReader.Messages.Count;
Tick(); Tick();
Check(ScreenReader.Messages.Count == spoken && !PassiveAccessibility.HasPendingContent,
    "manual request acknowledges not-yet-delivered brief");
ScreenContentAnnouncer.HasActiveContent = true;
Check(!PassiveAccessibility.ReadCurrentDetails(), "manual request respects modal priority");
ScreenContentAnnouncer.HasActiveContent = false;
NavigationManager.currentlySelected = null;
Check(!PassiveAccessibility.ReadCurrentDetails(), "manual request cannot reuse closed passive tree");
NavigationManager.currentlySelected = owner;
source.node = null;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains("Localized passive"),
    "empty native tooltip cache resolves selected same-object node");
Check(source.node is null, "reader never mutates native tooltip cache");
source.node = second;
Check(PassiveSelectionReader.TryRead(out read) && !read.Summary.Contains("Second passive"),
    "foreign stored tooltip node cannot replace selected same-object node");
Check(source.node == second, "reader leaves foreign tooltip cache unmodified");
source.node = first;
cells.Cells[2, 1] = first;
Check(PassiveSelectionReader.TryRead(out read), "native Component cell resolves its game object");
int builds = NodeTooltipContentBuilder.Builds;
first.Name = "Translated passive"; first.Description = "Translated description"; Localization.Locale = "en";
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains("Translated passive") &&
    read.Details.Contains("Translated description") && NodeTooltipContentBuilder.Builds == builds + 2,
    "locale revision rebuilds static localized strings as well as modifiers");
PassiveAccessibility.Reset();
NodeTooltipContentBuilder.ReturnNull = true;
Time.unscaledTimeAsDouble = 100;
Tick();
Check(!PassiveAccessibility.HasActiveContent && PassiveSelectionReader.LastStatus.Contains("builder returned no model"),
    "builder failure identifies exact rejection stage");
int diagnostics = LeAccess.Plugin.Log.Information.Count(line => line.StartsWith("[PassiveStatus]"));
Tick(); Tick(); Time.unscaledTimeAsDouble += 3; Tick();
Check(LeAccess.Plugin.Log.Information.Count(line => line.StartsWith("[PassiveStatus]")) == diagnostics,
    "unchanged unavailable status never floods log");
NodeTooltipContentBuilder.ReturnNull = false;
Time.unscaledTimeAsDouble += .2;
Tick(); Tick();
Check(PassiveAccessibility.HasActiveContent && LeAccess.Plugin.Log.Information.Last(line => line.StartsWith("[PassiveStatus]")).Contains("ready"),
    "successful recovery logs native model source");
owner._currentColumn = -1;
Time.unscaledTimeAsDouble += 3; Tick();
Check(LeAccess.Plugin.Log.Information.Last(line => line.StartsWith("[PassiveStatus]")).Contains("cell index invalid"),
    "invalid coordinate status includes failure stage");
owner._currentColumn = 2;
NavigationManager.currentlySelected = new GameObject().Add(new Canvas());
Time.unscaledTimeAsDouble += 3;
Check(!PassiveAccessibility.TryHandleFocus(cell) &&
    LeAccess.Plugin.Log.Information.Last(line => line.StartsWith("[PassiveStatus]")).Contains("not PassiveTreeNavigable"),
    "actual passive focus logs unexpected navigation instead of silent failure");
NavigationManager.currentlySelected = owner;
PassiveAccessibility.Reset();
first.Name = "";
Check(!PassiveSelectionReader.TryRead(out _), "unnamed native fallback remains unavailable");
builds = NodeTooltipContentBuilder.Builds;
for(int frame=0;frame<10;frame++) PassiveSelectionReader.TryRead(out _);
Check(NodeTooltipContentBuilder.Builds == builds, "pending native name retries are bounded rather than rebuilding each frame");
first.Name = "Recovered passive";
Time.unscaledTimeAsDouble += .2;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains("Recovered passive"),
    "fallback retries a missing name without requiring a point, locale or selection change");
PassiveAccessibility.Reset();
first.Name = "";
Tick();
builds = NodeTooltipContentBuilder.Builds;
for (int frame = 0; frame < 10; frame++) Tick();
Check(NodeTooltipContentBuilder.Builds == builds && !PassiveAccessibility.HasActiveContent,
    "service polling retains the pending native name retry deadline while releasing speech ownership");
first.Name = "Service recovered passive";
Time.unscaledTimeAsDouble += .2;
Tick(); Tick();
Check(PassiveAccessibility.HasActiveContent && ScreenReader.Messages.Last().Text.Contains(first.Name),
    "service recovers a late native name once its retry deadline expires");
PassiveAccessibility.Reset();
NodeTooltipContentBuilder.ReturnNull = true;
Tick();
builds = NodeTooltipContentBuilder.Builds;
for (int frame = 0; frame < 10; frame++) Tick();
Check(NodeTooltipContentBuilder.Builds == builds && !PassiveAccessibility.HasPendingContent,
    "service polling also bounds retries when the native builder returns no model");
NavigationManager.currentlySelected = null;
Tick();
NodeTooltipContentBuilder.ReturnNull = false;
NavigationManager.currentlySelected = owner;
Tick(); Tick();
Check(PassiveAccessibility.HasActiveContent && NodeTooltipContentBuilder.Builds == builds + 2,
    "closing the passive tree releases its pending model so reopening reads immediately");
foreach (string gate in new[] { "popup", "dialogue", "radial menu", "loading", "focus", "selection" })
{
    PassiveAccessibility.Reset();
    spoken = ScreenReader.Messages.Count;
    NodeTooltipContentBuilder.OnBuild = () =>
    {
        ScreenContentAnnouncer.HasActiveContent = gate == "popup";
        LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = gate == "dialogue";
        LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = gate == "radial menu";
        LE.UI.LoadingScreen.IsVisible = gate == "loading";
        Application.isFocused = gate != "focus";
        if (gate == "selection") cells.Cells[2, 1] = second;
    };
    PassiveAccessibility.ReadCurrentDetails();
    Check(ScreenReader.Messages.Count == spoken && !PassiveAccessibility.HasPendingContent,
        "manual passive inspection cancels output when ownership changes during model construction: " + gate);
    NodeTooltipContentBuilder.OnBuild = null;
    ScreenContentAnnouncer.HasActiveContent = false;
    LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
    LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = false;
    LE.UI.LoadingScreen.IsVisible = false;
    Application.isFocused = true;
    cells.Cells[2, 1] = first;
}
PassiveAccessibility.Reset(); ScreenReader.Messages.Clear();
ScreenReader.OutputAccepted = false;
Tick(); Tick();
Check(ScreenReader.Messages.Count == 0 && PassiveAccessibility.HasPendingContent,
    "Rejected passive summary remains pending until output is accepted");
ScreenReader.OutputAccepted = true;
Tick(); Tick();
Check(ScreenReader.Messages.Count == 1 && !PassiveAccessibility.HasPendingContent,
    "Recovered passive summary is acknowledged exactly once");
PassiveAccessibility.Reset(); ScreenReader.Messages.Clear();
ScreenReader.OutputAccepted = false;
PassiveAccessibility.ReadCurrentDetails();
Check(PassiveAccessibility.HasPendingContent, "Rejected passive manual request retains current intent");
first.Description = "Fresh passive details after recovery.";
first.pointsAllocated++;
ScreenReader.OutputAccepted = true;
ScreenReader.SpokeThisFrame = true; PassiveAccessibility.Update();
Check(ScreenReader.Messages.Count == 0 && PassiveAccessibility.HasPendingContent,
    "Automatic passive detail retry yields to output already accepted this frame");
Tick(); Tick();
Check(ScreenReader.Messages.Single().Text.Contains("Fresh passive details after recovery"),
    "Recovered passive details are rebuilt from the current native model");
ScreenReader.Available = false;
PassiveAccessibility.ReadCurrentDetails();
Check(PassiveAccessibility.HasPendingContent, "Unavailable passive manual intent stays pending after an accepted summary");
ScreenReader.Available = true; Tick(); Tick();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("Fresh passive details after recovery"),
    "Late passive reader accepts current manual intent exactly once");
foreach (var gate in new[] { "changed", "hidden", "collected", "unready", "unavailable" })
{
    PassiveAccessibility.Reset(); ScreenReader.Messages.Clear();
    ScreenReader.OutputAccepted = false; ScreenReader.Available = gate != "unavailable";
    PassiveAccessibility.ReadCurrentDetails();
    if (gate == "changed") cells.Cells[2, 1] = second;
    else if (gate == "collected") owner.WasCollected = true;
    else if (gate == "unready") owner._navigationElements = null;
    else owner.gameObject.Active = false;
    ScreenReader.OutputAccepted = ScreenReader.Available = true;
    Tick(); Tick();
    Check(ScreenReader.Messages.All(message => !message.Text.Contains("Fresh passive details after recovery")) &&
        (gate == "changed" ? ScreenReader.Messages.Single().Text.Contains(second.Name) : ScreenReader.Messages.Count == 0),
        "Failed passive details cannot replay after ownership loss: " + gate);
    cells.Cells[2, 1] = first; owner.gameObject.Active = true;
    owner.WasCollected = false; owner._navigationElements = cells;
}
PassiveAccessibility.Reset();

string T(string key) => LeAccess.I18n.ModText.Get(key);
string F(string key, params object[] args) => LeAccess.I18n.ModText.Format(key, args);
string Join(params string[] parts) => ScreenContentTracker.Combine(parts);
string Tree(string name, int points, string role) => Join(F("passive.tree", name, points), role);
string overviewPrefix = F("passive.overview", "").TrimEnd();
TMPro.TMP_Text Label(string text) => new GameObject().Add(new TMPro.TMP_Text { text = text });
MasteryTabButton Tab(int index, string points, bool locked = false)
{
    var lockedPoints = new GameObject { Active = locked };
    return new GameObject().Add(new MasteryTabButton
    {
        _tabButton = new GameObject().Add(new TabButton { SelectedIndex = index, _assignedPoints = Label(points), _lockedPoints = lockedPoints })
    });
}
ScreenReader.Messages.Clear();
first.mastery = 1; first.masteryRequirement = 20; first.requirementsMet = false;
fullscreen._masteryDataList = new(new[] {
    new PassivePanelData.MasteryData { name = "Sentinel" }, new PassivePanelData.MasteryData { name = "Paladin", isMastery = true },
    new PassivePanelData.MasteryData { name = "Forge Guard", isMastery = true }, new PassivePanelData.MasteryData { name = "Void Knight", isMastery = true } });
fullscreen._masteryTabButtons = new(new[] { Tab(0, "20"), Tab(1, "15"), Tab(2, "0", locked: true), Tab(3, "3", locked: true) });
fullscreen._localTreeData = new LocalTreeData { chosenMastery = 1, masteryLevels = new(new byte[] { 20, 15, 0, 3 }) };
fullscreen._currentMasteryIndex = 1;
fullscreen._masteryNameText = Label("Visible Paladin");
fullscreen._pointsAllocatedText = Label("15");
fullscreen._pointsAvailableText = Label("2 points available");
fullscreen._masteryBarDisplay = new GameObject().Add(new MasteryBarDisplay { _hasFillValues = true, _lastFillPoints = 15, _lastFillMaxPoints = 113 });
NavigationManager.currentlySelected = fullscreen;
fullscreen.ActiveChildNavigable = owner;
Tick(); Tick();
string opened = ScreenReader.Messages.Single().Text;
string LocalizedName(PassivePanelData.MasteryData data) =>
    LeAccess.Accessibility.CharacterCreation.MasteryReader.LocalizedName(data.name, data.isMastery);
var trees = fullscreen._masteryDataList;
Check(opened.StartsWith(F("passive.overview", string.Join("; ",
    Tree(LocalizedName(trees[0]), 20, T("passive.tree_class")), Tree(LocalizedName(trees[1]), 15, T("passive.tree_chosen")),
    Tree(LocalizedName(trees[2]), 0, T("passive.tree_locked")), Tree(LocalizedName(trees[3]), 3, T("passive.tree_locked"))))),
    "Opening the panel lists every tree's points, the chosen mastery and locked trees: " + opened);
string paladin = fullscreen._masteryNameText.text;
Check(opened.Contains(Join(Tree(paladin, 15, T("passive.tree_chosen")), fullscreen._pointsAvailableText.text)) &&
    opened.IndexOf(paladin, StringComparison.Ordinal) < opened.IndexOf(first.Name, StringComparison.Ordinal),
    "The displayed tree's visible name, points and unspent points precede the first node");
Check(opened.Contains(F("passive.column_locked", 1, 20, 15)) && !opened.Contains(F("passive.node_threshold", 20, 15)),
    "Entering a column above the tree's points names its threshold once, in the column label: " + opened);
cells.Cells[2, 1] = second;
Tick(); Tick();
Check(ScreenReader.Messages.Count == 2 && !ScreenReader.Messages.Last().Text.Contains(overviewPrefix) &&
    !ScreenReader.Messages.Last().Text.Contains(paladin), "Moving within the same tree speaks only the node");
Check(!ScreenReader.Messages.Last().Text.Contains(F("passive.node_threshold", 20, 15)), "Nodes of another page never borrow this tree's threshold");
fullscreen._currentMasteryIndex = 2;
fullscreen._masteryNameText = Label("Visible Forge Guard");
fullscreen._pointsAllocatedText = Label("0");
cells.Cells[2, 1] = first;
Tick(); Tick();
Check(ScreenReader.Messages.Count == 3 &&
    ScreenReader.Messages.Last().Text.StartsWith(Tree(fullscreen._masteryNameText.text, 0, T("passive.tree_locked"))) &&
    !ScreenReader.Messages.Last().Text.Contains(overviewPrefix), "Switching trees speaks the new tree before its node, without the overview");
fullscreen._localTreeData.chosenMastery = 0;
Check(PassiveTreeReader.ReadOverview(fullscreen).StartsWith(T("passive.no_mastery")), "An unchosen mastery is stated first");
fullscreen._localTreeData.chosenMastery = 1;
fullscreen._masteryTabButtons[2]._tabButton._assignedPoints.gameObject.Active = false;
Check(PassiveTreeReader.ReadOverview(fullscreen).Contains(F("passive.tree", LocalizedName(trees[2]), 0)),
    "Hidden tab points fall back to the saved tree levels");

Ability Skill(string name) => new() { Name = name, Description = name + " description" };
AbilityTooltip Unlock(string name, byte requirement, bool mastery = false)
{
    var root = new GameObject();
    root.Add(new PassiveTreeUnlockableAbilityIcon { requirement = requirement, isMasteryAbility = mastery });
    return root.Add(new AbilityTooltip { currentAbility = Skill(name) });
}
var strip = new GameObject().Add(new PassiveTreeUnlockPanelNavigable
{ ActiveAbilityTooltips = new(new[] { Unlock("Smite", 10), Unlock("Judgement", 20, mastery: true), Unlock("Far skill", 200) }) });
fullscreen._currentMasteryIndex = 1;
fullscreen._masteryNameText = Label("Visible Paladin");
fullscreen.ActiveChildNavigable = strip;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Smite", T("passive.ability"), T("passive.ability_unlocked"), F("list.position", 1, 3)) &&
    read.Details == "Smite description", "Unlock strip reads the skill name, unlocked state and description");
Check(read.Text == Join("Smite", "Smite description") && !read.Text.Contains(F("list.position", 1, 3)),
    "A details request names the skill and describes it without repeating its place");
LeAccess.I18n.ListPosition.Enabled = false;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Smite", T("passive.ability"), T("passive.ability_unlocked")),
    "With positions off the strip drops the place and keeps its structural labels");
LeAccess.I18n.ListPosition.Enabled = true;
var sprite = strip.ActiveAbilityTooltips[0].gameObject.Child();
Check(PassiveAccessibility.TryHandleFocus(sprite), "The strip's sprite focus is owned instead of read as an object name");
strip.SelectedIndex = 1;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Judgement", T("passive.mastery_ability"), F("passive.ability_unlockable", 20, 15), F("list.position", 2, 3)),
    "A reachable skill states its threshold and the tree's points");
strip.SelectedIndex = 2;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Far skill", T("passive.ability"), F("passive.ability_unreachable", 200, 113), F("list.position", 3, 3)),
    "A skill beyond the tree's maximum says it cannot be reached");
var known = LeAccess.Accessibility.Skills.SkillLearningReader.Known;
known[strip.ActiveAbilityTooltips[2].currentAbility] = (true, "Level 2");
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Far skill", T("passive.ability"), T("skill.learned"), "Level 2",
    F("list.position", 3, 3)), "A skill the character knows is learned whatever this tree's points say");
known[strip.ActiveAbilityTooltips[2].currentAbility] = (false, "Level 10");
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Far skill", T("passive.ability"), T("skill.not_learned"), "Level 10",
    F("list.position", 3, 3)), "A level-gated class skill states its level instead of tree points");
strip.SelectedIndex = 1;
known[strip.ActiveAbilityTooltips[1].currentAbility] = (false, null);
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Judgement", T("passive.mastery_ability"), T("skill.not_learned"),
    F("passive.ability_unlockable", 20, 15), F("list.position", 2, 3)), "An unlearned tree skill keeps its point threshold");
strip.SelectedIndex = 0;
known[strip.ActiveAbilityTooltips[0].currentAbility] = (false, null);
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Smite", T("passive.ability"), T("skill.not_learned"),
    F("list.position", 1, 3)), "An unlearned skill never claims to be unlocked because the threshold is met");
known.Clear();
strip.SelectedIndex = 3;
Check(!PassiveSelectionReader.TryRead(out _), "Out-of-range strip selection is rejected");
strip.SelectedIndex = 0;
fullscreen._masteryBarDisplay._hasFillValues = false;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Smite", T("passive.ability"), F("list.position", 1, 3)), "Without bar values no state is guessed");
NavigationManager.currentlySelected = null;
Tick();
NavigationManager.currentlySelected = fullscreen;
ScreenReader.Messages.Clear();
Tick(); Tick();
Check(ScreenReader.Messages.Single().Text.StartsWith(overviewPrefix), "Reopening the panel speaks the overview again");
ScreenContentAnnouncer.HasActiveContent = true; Tick();
ScreenContentAnnouncer.HasActiveContent = false; strip.SelectedIndex = 1; Tick(); Tick();
Check(ScreenReader.Messages.Count == 2 && !ScreenReader.Messages.Last().Text.Contains(overviewPrefix) &&
    !ScreenReader.Messages.Last().Text.Contains(paladin), "A modal inside the open panel does not repeat the tree context");
// Controller passive panel: reading order, moves, exits, banner and tree switching.
NavigationManager.currentlySelected = null; Tick();
PassiveAccessibility.Reset(); ScreenReader.Messages.Clear();
var classTree = new GameObject();
var pages = new[] { classTree.Child(), classTree.Child() };
pages[1].Active = false;
var characterTree = classTree.Add(new CharacterTree { unspentPoints = 4, masteries = new(pages) });
var grid = new Il2CppSystem.Array { Cells = new NativeObject[11, 5] };
var pageOwner = pages[0].Add(new PassiveTreeNavigable { _navigationElements = grid });
SkillTreeNode Place(Il2CppSystem.Array cellsOf, GameObject page, string name, int column, int row)
{
    var go = page.Child();
    var node = go.Add(new SkillTreeNode { tree = characterTree, Name = name, Description = name + " details" });
    go.Add(new SkillnodeTooltip { node = node });
    cellsOf.Cells[column, row] = node;
    return node;
}
var nodeA = Place(grid, pages[0], "Node A", 1, 3);
var nodeB = Place(grid, pages[0], "Node B", 1, 1);
var nodeC = Place(grid, pages[0], "Node C", 3, 4);
var nodeD = Place(grid, pages[0], "Node D", 3, 2);
var nodeE = Place(grid, pages[0], "Node E", 5, 1);
grid.Cells[7, 4] = nodeA;
Place(grid, pages[0], "Hidden node", 7, 2).gameObject.Active = false;
var order = new List<(int Column, int Row)> { (1, 3), (1, 1), (3, 4), (3, 2), (5, 1) };
PassiveNavigationOrder.Move Plan(int column, int row, Direction direction) => PassiveNavigationOrder.Plan(order, column, row, direction);
PassiveNavigationOrder.Move Select(int column, int row) => new(PassiveNavigationOrder.MoveKind.Select, column, row);
var exit = new PassiveNavigationOrder.Move(PassiveNavigationOrder.MoveKind.Exit);
var stay = new PassiveNavigationOrder.Move(PassiveNavigationOrder.MoveKind.Stay);
Check(Plan(1, 3, Direction.DOWN) == Select(1, 1) && Plan(1, 1, Direction.DOWN) == exit &&
    Plan(3, 2, Direction.DOWN) == stay && Plan(5, 1, Direction.DOWN) == stay,
    "Down steps down its column only; only the first column's bottom leaves the grid, other bottoms stay");
Check(Plan(1, 1, Direction.UP) == Select(1, 3) && Plan(1, 3, Direction.UP) == exit && Plan(3, 4, Direction.UP) == stay,
    "Up steps up its column only; only the first column's top leaves the grid");
Check(Plan(1, 3, Direction.RIGHT) == Select(3, 4) && Plan(1, 1, Direction.RIGHT) == Select(3, 2) &&
    Plan(3, 2, Direction.RIGHT) == Select(5, 1) && Plan(5, 1, Direction.LEFT) == Select(3, 4) &&
    Plan(3, 2, Direction.LEFT) == Select(1, 1) && Plan(1, 1, Direction.LEFT) == exit,
    "Right and Left keep the position in the neighbouring column, clamped to its size; Left from the first column leaves");
Check(Plan(5, 1, Direction.RIGHT) == new PassiveNavigationOrder.Move(PassiveNavigationOrder.MoveKind.Stay),
    "Right from the last column stays put");
Check(Plan(2, 0, Direction.DOWN) == Select(3, 4) && Plan(2, 0, Direction.UP) == Select(1, 1) &&
    Plan(2, 0, Direction.RIGHT) == Select(3, 4) && Plan(2, 0, Direction.LEFT) == Select(1, 3),
    "A cursor left on an empty cell resumes from its place in the order");
Check(Plan(1, 2, Direction.DOWN) == Select(1, 1) && Plan(1, 2, Direction.UP) == Select(1, 3),
    "An empty cell inside a column sits between the nodes above and below it");
Check(Plan(1, 3, (Direction)9) == new PassiveNavigationOrder.Move(PassiveNavigationOrder.MoveKind.Native),
    "Unknown directions fall back to the game");

var mastered = new GameObject();
MasteryButton Diamond(string name, string points, bool mastery, bool locked = false)
{
    var go = mastered.Child();
    var lockIcon = go.Child();
    lockIcon.Active = locked;
    var button = go.Add(new MasteryButton { Name = name, Definition = name + " definition", isMasteryClass = mastery,
        levelTMP = go.Child().Add(new TMPro.TextMeshProUGUI { text = points }), lockIcon = lockIcon });
    button.button = go.Add(new UnityEngine.UI.Button());
    return button;
}
var diamonds = new[] { Diamond("Rogue", "5", false), Diamond("Bladedancer", "3", true), Diamond("Falconer", "0", true, locked: true) };
var manager = new GameObject().Add(new PassivePanelManager { activeTree = characterTree,
    openedBanner = mastered.Add(new MasteryBanner { masteryButtons = new(diamonds) }) });
PassivePanelManager.instance = manager;
PlayerFinder.Actor = new GameObject().Add(new Actor { localTreeData = new LocalTreeData { chosenMastery = 1, masteryLevels = new(new byte[] { 5, 3, 0 }) } });

pageOwner._currentColumn = 1; pageOwner._currentRow = 3;
NavigationManager.currentlySelected = pageOwner;
Tick(); Tick();
string controllerOpened = ScreenReader.Messages.Single().Text;
string rogueTree = Join(Tree("Rogue", 5, T("passive.tree_class")), F("passive.unspent", 4));
Check(controllerOpened.StartsWith(F("passive.overview", string.Join("; ", Tree("Rogue", 5, T("passive.tree_class")),
    Tree("Bladedancer", 3, T("passive.tree_chosen")), Tree("Falconer", 0, T("passive.tree_locked"))))) &&
    controllerOpened.Contains(rogueTree) && controllerOpened.Contains(Join(F("passive.column", 1), nodeA.Name)) && controllerOpened.EndsWith(F("list.position", 1, 2)),
    "Opening the controller panel speaks every tree, the shown tree and the first node's place: " + controllerOpened);
PassiveTreeNavigation.Navigate(pageOwner, Direction.DOWN);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (1, 1) && pageOwner.NativeSteps.Count == 0 &&
    pageOwner.Selected.Last() == (1, 1) && nodeA.GetComponent<SkillnodeTooltip>().Exits == 1,
    "A handled move ends the old hover, places the cursor and selects the node natively");
Tick(); Tick();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains(nodeB.Name) &&
    ScreenReader.Messages.Last().Text.EndsWith(F("list.position", 2, 2)) && !ScreenReader.Messages.Last().Text.Contains(rogueTree) &&
    !ScreenReader.Messages.Last().Text.Contains(F("passive.column", 1)),
    "Moving within a column speaks the node and its place only");
PassiveTreeNavigation.Navigate(pageOwner, new Vector3 { x = 0.9f });
Check((pageOwner._currentColumn, pageOwner._currentRow) == (3, 2),
    "A stick flick moves like the d-pad");
Tick(); Tick();
Check(ScreenReader.Messages.Last().Text.Contains(Join(F("passive.column", 2), nodeD.Name)) &&
    ScreenReader.Messages.Last().Text.EndsWith(F("list.position", 2, 2)), "Entering another column puts its label before the node");
Check(PassiveSelectionReader.TryRead(out var entered) && PassiveSelectionReader.TryRead(out var enteredAgain) &&
    entered.Summary == enteredAgain.Summary && entered.Summary.StartsWith(F("passive.column", 2)),
    "The column label stays for as long as the same node is selected");
PassiveTreeNavigation.Navigate(pageOwner, Direction.UP);
Tick(); Tick();
Check(ScreenReader.Messages.Last().Text.Contains(nodeC.Name) && !ScreenReader.Messages.Last().Text.Contains(F("passive.column", 2)),
    "Moving inside the same column does not repeat the label");
PassiveTreeNavigation.Navigate(pageOwner, Direction.DOWN);
pageOwner._currentColumn = 5; pageOwner._currentRow = 1;
Tick(); Tick();
int beforeStay = ScreenReader.Messages.Count;
PassiveTreeNavigation.Navigate(pageOwner, Direction.RIGHT);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (5, 1) && pageOwner.NativeSteps.Count == 0,
    "Right from the last column keeps the cursor");
Tick(); Tick();
Check(ScreenReader.Messages.Count == beforeStay + 1 && ScreenReader.Messages.Last().Text.Contains(nodeE.Name) &&
    !ScreenReader.Messages.Last().Text.Contains(F("list.position", 1, 1)), "A move that cannot go further repeats the node, and a lone entry has no place");
PassiveTreeNavigation.Navigate(pageOwner, Direction.DOWN);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (5, 1) && pageOwner.NativeSteps.Count == 0,
    "Down from the bottom of a later column keeps the cursor instead of leaving the grid");
pageOwner._currentColumn = 1; pageOwner._currentRow = 1;
PassiveTreeNavigation.Navigate(pageOwner, Direction.DOWN);
Check(pageOwner.NativeSteps.Last() == "vertical -1 from 1,0", "Down from the bottom of the first column takes the game's step to the strip");
pageOwner._currentColumn = 1; pageOwner._currentRow = 3;
PassiveTreeNavigation.Navigate(pageOwner, Direction.UP);
Check(pageOwner.NativeSteps.Last() == "vertical 1 from 1,4", "Up from the first node takes the game's step from the top border");
pageOwner._currentRow = 3;
PassiveTreeNavigation.Navigate(pageOwner, Direction.LEFT);
Check(pageOwner.NativeSteps.Last() == "horizontal -1 from 0,3", "Left from the first column takes the game's step toward the banner");
var unreadable = pageOwner._navigationElements;
pageOwner._navigationElements = null;
PassiveTreeNavigation.Navigate(pageOwner, Direction.RIGHT);
pageOwner._navigationElements = unreadable;
Check(pageOwner.NativeSteps.Last() == "horizontal 1 from 0,3" && pageOwner.NativeSteps.Count == 4,
    "An unreadable grid moves exactly as the game's dispatch would");

pageOwner._currentColumn = 3; pageOwner._currentRow = 2;
NavigationManager.previousSelectedNavigableId = NavigableID.PASSIVE_TREE_TOP_BUTTONS;
PassiveTreeNavigation.AfterSelected(pageOwner);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (1, 3), "Coming down from the top buttons lands on the first node");
NavigationManager.previousSelectedNavigableId = NavigableID.PASSIVE_TREE_UNLOCK_PANEL;
PassiveTreeNavigation.AfterSelected(pageOwner);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (1, 1), "Coming up from the skill strip lands on the bottom of the first column, where it was left");
pageOwner._currentColumn = 3; pageOwner._currentRow = 2;
NavigationManager.previousSelectedNavigableId = NavigableID.PASSIVE_TREE_BANNER;
PassiveTreeNavigation.AfterSelected(pageOwner);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (1, 3), "Coming back from the banner lands on the first node");
pageOwner._currentColumn = 3; pageOwner._currentRow = 2;
EpochInputManager.IsControllerActive = false;
NavigationManager.previousSelectedNavigableId = NavigableID.PASSIVE_TREE_TOP_BUTTONS;
PassiveTreeNavigation.AfterSelected(pageOwner);
EpochInputManager.IsControllerActive = true;
owner._currentColumn = 2; owner._currentRow = 1;
PassiveTreeNavigation.AfterSelected(owner);
Check((pageOwner._currentColumn, pageOwner._currentRow) == (3, 2) && (owner._currentColumn, owner._currentRow) == (2, 1),
    "Other entries, mouse input and trees outside the controller panel keep the game's landing");
NavigationManager.previousSelectedNavigableId = NavigableID.NONE;

// Like the skill trees: column thresholds, quiet open nodes, points left after a point, and why Cross did nothing.
PassiveAccessibility.Reset(); ScreenReader.Messages.Clear();
foreach (var n in new[] { nodeA, nodeB, nodeC, nodeD, nodeE }) n.requirementsMet = true;
nodeC.masteryRequirement = 5; nodeD.masteryRequirement = 10; nodeE.masteryRequirement = 10;
NavigationManager.currentlySelected = pageOwner;
pageOwner._currentColumn = 3; pageOwner._currentRow = 4;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.StartsWith(Join(F("passive.column_threshold", 2, 5), nodeC.Name)) &&
    !read.Summary.Contains(T("node.locked")), "An open column names the points it needs; a node that can be raised has no status: " + read.Summary);
pageOwner._currentRow = 2;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains(F("passive.node_threshold", 10, 5)),
    "A node needing more than its column names its own threshold");
pageOwner._currentColumn = 5; pageOwner._currentRow = 1;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.StartsWith(Join(F("passive.column_locked", 3, 10, 5), nodeE.Name)) &&
    !read.Summary.Contains(T("passive.node_locked")) && !read.Summary.Contains(F("passive.node_threshold", 10, 5)),
    "Entering a locked column says so once, in its label: " + read.Summary);
nodeA.masteryRequirement = nodeB.masteryRequirement = 10;
pageOwner._currentColumn = 1; pageOwner._currentRow = 3;
PassiveSelectionReader.TryRead(out _);
pageOwner._currentRow = 1;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains(T("passive.node_locked")) &&
    !read.Summary.Contains(F("passive.node_threshold", 10, 5)), "Later nodes of a locked column say only that they are locked");
nodeA.masteryRequirement = nodeB.masteryRequirement = 0;
pageOwner._currentColumn = 3; pageOwner._currentRow = 4;
PassiveSelectionReader.TryRead(out _);
nodeC.pointsAllocated = 1; characterTree.unspentPoints = 3; diamonds[0].levelTMP.text = "10";
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains(Join(F("passive.unspent", 3), F("passive.column_unlocked", 3))),
    "A point spent says the points left and the column it opened: " + read.Summary);
Check(PassiveSelectionReader.TryRead(out var again) && again.Summary == read.Summary, "That notice stays while the node stays selected");
pageOwner._currentRow = 2;
Check(PassiveSelectionReader.TryRead(out read) && !read.Summary.Contains(F("passive.unspent", 3)) &&
    !read.Summary.Contains(F("passive.node_threshold", 10, 10)), "The next node no longer repeats it and its threshold is met");
var tree0 = new PassiveTreeState.State("tree", 5, 4);
var thresholds = new List<int> { 0, 5, 10 };
Check(PassiveTreeState.Changes(null, tree0, thresholds) is null && PassiveTreeState.Changes(tree0 with { Tree = "other" }, tree0, thresholds) is null &&
    PassiveTreeState.Changes(tree0, tree0, thresholds) is null, "A new tree or no change adds nothing; the tree introduction covers a new tree");
Check(PassiveTreeState.Changes(tree0, tree0 with { Points = 4, Unspent = 5 }, thresholds) ==
    Join(F("passive.unspent", 5), F("passive.column_relocked", 2)), "Removing points says the points left and the column it closed");
var probe = new GameObject().Add(new SkillTreeNode { requirementsMet = true, maxPoints = 5 });
Check(PassiveTreeState.Refusal(probe, 5, 0, 4) is null, "A node that can be raised has no refusal");
probe.pointsAllocated = 5;
Check(PassiveTreeState.Refusal(probe, 5, 0, 4) == T("node.full"), "A full node says so");
probe.pointsAllocated = 0; probe.masteryRequirement = 10;
Check(PassiveTreeState.Refusal(probe, 5, 0, 4) == F("passive.node_threshold", 10, 5), "A node above the tree's points names its threshold");
probe.masteryRequirement = 0; probe.requirementsMet = false;
Check(PassiveTreeState.Refusal(probe, 5, 0, 4) == T("node.locked"), "Unmet requirements are named");
probe.requirementsMet = true;
Check(PassiveTreeState.Refusal(probe, 5, 0, 0) == T("node.no_points"), "Without unspent points Cross says none are left");
probe.RespecMode = true;
Check(PassiveTreeState.Refusal(probe, 5, 0, 0) is null, "While the game removes points Cross gives no refusal");
characterTree.unspentPoints = 0;
pageOwner._currentRow = 4;
PassiveTreeState.BeforeSubmit(pageOwner, SubmitType.Y_TRIANGLE);
Check(PassiveSelectionReader.TryRead(out read) && !read.Summary.Contains(T("node.no_points")), "Other buttons give no refusal");
PassiveTreeState.BeforeSubmit(pageOwner, SubmitType.A_EX);
Check(PassiveSelectionReader.TryRead(out read) && read.Summary.Contains(T("node.no_points")), "Cross with no points left says so on the node");
pageOwner._currentRow = 2;
Check(PassiveSelectionReader.TryRead(out read) && !read.Summary.Contains(T("node.no_points")), "The refusal ends with the selection");
// The native submit is replayed (its prologue cannot be detoured): only Cross clicks the selected cell's node.
PassiveTreeNavigation.Submit(pageOwner, SubmitType.A_EX);
PassiveTreeNavigation.Submit(pageOwner, SubmitType.X_SQUARE);
Check(nodeD.Clicks == 1 && nodeC.Clicks == 0, "Cross clicks the selected node once; other buttons do nothing");
pageOwner._currentColumn = 2; pageOwner._currentRow = 0;
PassiveTreeNavigation.Submit(pageOwner, SubmitType.A_EX);
pageOwner._currentColumn = 99;
PassiveTreeNavigation.Submit(pageOwner, SubmitType.A_EX);
Check(nodeD.Clicks == 1 && nodeC.Clicks == 0, "An empty or out-of-range cell clicks nothing instead of throwing");
pageOwner._currentColumn = 3; pageOwner._currentRow = 2;
foreach (var n in new[] { nodeA, nodeB, nodeC, nodeD, nodeE }) { n.requirementsMet = false; n.masteryRequirement = 0; n.pointsAllocated = 0; }
characterTree.unspentPoints = 4; diamonds[0].levelTMP.text = "5";
PassiveAccessibility.Reset();
pageOwner._currentColumn = 1; pageOwner._currentRow = 3;
Tick(); Tick();

var bannerNavigable = mastered.Add(new PassiveTreeBannerNavigable());
var shoulderFocus = diamonds[1].gameObject.Child();
Check(PassiveAccessibility.TryHandleFocus(shoulderFocus) && !PassiveAccessibility.TryHandleFocus(new GameObject()),
    "Banner diamonds are owned so the shoulder-button focus hops stay silent");
int beforeSwitch = ScreenReader.Messages.Count;
bannerNavigable._currentButton = diamonds[1].GetComponent<UnityEngine.UI.Button>();
manager.activeMastery = 1;
NavigationManager.currentlySelected = bannerNavigable;
Tick(); Tick(); Tick();
Check(ScreenReader.Messages.Count == beforeSwitch, "Nothing is spoken while the clicked mastery's page is still loading");
var masteryGrid = new Il2CppSystem.Array { Cells = new NativeObject[11, 5] };
var masteryOwner = pages[1].Add(new PassiveTreeNavigable { _navigationElements = masteryGrid, _currentColumn = 0, _currentRow = 4 });
var nodeG = Place(masteryGrid, pages[1], "Node G", 0, 4);
nodeG.mastery = 1; nodeG.masteryRequirement = 10;
pages[0].Active = false; pages[1].Active = true;
NavigationManager.currentlySelected = masteryOwner;
Tick(); Tick();
string switched = ScreenReader.Messages.Last().Text;
Check(ScreenReader.Messages.Count == beforeSwitch + 1 &&
    switched.StartsWith(Join(Tree("Bladedancer", 3, T("passive.tree_chosen")), F("passive.unspent", 4),
        F("passive.column_locked", 1, 10, 3), nodeG.Name)) && !switched.Contains(overviewPrefix),
    "The new tree's name and points lead its first node in one utterance: " + switched);
bannerNavigable._currentButton = diamonds[2].GetComponent<UnityEngine.UI.Button>();
NavigationManager.currentlySelected = bannerNavigable;
Tick(); Tick();
Check(ScreenReader.Messages.Last().Text == Join(T("passive.banner"), Tree("Falconer", 0, T("passive.tree_locked")), T("passive.open_tree"),
    F("list.position", 3, 3)),
    "Browsing the banner reads each mastery's points and lock, and that Cross opens a tree not shown: " + ScreenReader.Messages.Last().Text);
bannerNavigable._currentButton = diamonds[1].GetComponent<UnityEngine.UI.Button>();
Tick(); Tick();
Check(ScreenReader.Messages.Last().Text == Join(Tree("Bladedancer", 3, T("passive.tree_chosen")), T("passive.tree_shown"),
    F("list.position", 2, 3)),
    "The diamond of the shown tree says so, with no open hint");
PlayerFinder.Actor.localTreeData.chosenMastery = 0;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join(Tree("Bladedancer", 3, T("passive.tree_locked")), T("passive.tree_shown"), F("list.position", 2, 3)),
    "Before a mastery is chosen, mastery trees are marked locked: " + read.Summary);
bannerNavigable._currentButton = diamonds[0].GetComponent<UnityEngine.UI.Button>();
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join(Tree("Rogue", 5, T("passive.tree_class")), T("passive.open_tree"), F("list.position", 1, 3)),
    "The base class tree is never locked");
PlayerFinder.Actor.localTreeData.chosenMastery = 1;
bannerNavigable._currentButton = diamonds[1].GetComponent<UnityEngine.UI.Button>();
Check(PassiveAccessibility.ReadCurrentDetails() && ScreenReader.Messages.Last().Text.EndsWith(diamonds[1].Definition),
    "Details on a diamond read the mastery's description");
bannerNavigable.isDescriptionSelected = true;
Check(!PassiveSelectionReader.TryRead(out _), "The scrolled description is left to the generic reader");
bannerNavigable.isDescriptionSelected = false;
// The banner as a list: Up/Down through the base class and masteries, ends stay, Right back to the tree, Left stays.
bannerNavigable.Tree = pageOwner;
bannerNavigable._currentButton = diamonds[0].button;
Check(PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.DOWN) && bannerNavigable._currentButton == diamonds[1].button &&
    PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.DOWN) && bannerNavigable._currentButton == diamonds[2].button,
    "Down steps through the trees in the panel's order");
int bannerFocus = bannerNavigable.Focused;
Check(PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.DOWN) && bannerNavigable._currentButton == diamonds[2].button &&
    bannerNavigable.Focused == bannerFocus, "The last tree stays instead of wrapping");
Check(PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.UP) && bannerNavigable._currentButton == diamonds[1].button &&
    PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.LEFT) && bannerNavigable._currentButton == diamonds[1].button,
    "Up steps back; Left stays");
Check(PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.RIGHT) &&
    pageOwner.Entries.Last() == (new Vector2Int(0, 4), PassiveTreeNavigable.PassiveSkillTreeNodeSearchDirection.Down),
    "Right returns to the tree the game's own way, from its top-left cell searching down");
diamonds[2].gameObject.Active = false;
Check(PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.DOWN) && bannerNavigable._currentButton == diamonds[1].button,
    "A hidden diamond is not stepped onto");
diamonds[2].gameObject.Active = true;
bannerNavigable.isDescriptionSelected = true;
Check(!PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.DOWN), "The scrolled description keeps the game's scrolling");
bannerNavigable.isDescriptionSelected = false;
bannerNavigable._currentButton = new GameObject().Add(new UnityEngine.UI.Button());
Check(!PassiveBannerNavigation.TryNavigate(bannerNavigable, Direction.DOWN), "The mastery skill button keeps the game's moves");
manager.activeMastery = 1;
bannerNavigable._currentButton = diamonds[2].button;
PassiveBannerNavigation.AfterEntered(bannerNavigable, new Vector2Int(1, 0));
Check(bannerNavigable._currentButton == diamonds[2].button, "Other entries keep the game's landing");
PassiveBannerNavigation.AfterEntered(bannerNavigable, new Vector2Int(0, 1));
Check(bannerNavigable._currentButton == diamonds[1].button, "Entering from the tree starts on the shown tree's diamond");
var controllerStrip = new GameObject().Add(new PassiveTreeUnlockPanelNavigable { ActiveAbilityTooltips = new(new[] { Unlock("Shift", 10) }) });
NavigationManager.currentlySelected = controllerStrip;
Check(PassiveSelectionReader.TryRead(out read) &&
    read.Summary == Join("Shift", T("passive.ability"), F("passive.ability_unlockable", 10, 3)),
    "The controller strip uses the shown tree's points: " + read.Summary);
NavigationManager.currentlySelected = masteryOwner;
Tick(); Tick();
int beforeGap = ScreenReader.Messages.Count;
NavigationManager.currentlySelected = null; Tick();
NavigationManager.currentlySelected = masteryOwner; Tick(); Tick();
Check(ScreenReader.Messages.Count == beforeGap + 1 && !ScreenReader.Messages.Last().Text.Contains(overviewPrefix) &&
    !ScreenReader.Messages.Last().Text.Contains(F("passive.unspent", 4)), "An empty selection inside the open panel keeps the tree context");
// Closing disables the page navigable, whose OnDisable clears the selection.
classTree.Active = false; NavigationManager.currentlySelected = null;
Tick();
classTree.Active = true; NavigationManager.currentlySelected = masteryOwner;
Tick(); Tick();
Check(ScreenReader.Messages.Last().Text.StartsWith(overviewPrefix) && ScreenReader.Messages.Last().Text.Contains(F("passive.column", 1)),
    "Reopening the controller panel speaks the overview and the first node's column again");
PassivePanelManager.instance = null;
PlayerFinder.Actor = null;
PassiveAccessibility.Reset();
Console.WriteLine($"Passed {checks} passive checks.");
