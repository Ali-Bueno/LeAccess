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
Check(NodeTooltipContentBuilder.Builds == 1, "fallback uses populated native builder");
Check(read.Text.Contains("Localized passive") && read.Text.Contains("0 of 5") &&
    read.Text.Contains("Game description") && read.Text.Contains("Localized modifier") &&
    read.Text.Contains("Localized requirement"), "full localized data");
int refreshes = NodeTooltipInfo.Refreshes;
for (int i = 0; i < 20; i++) Check(PassiveSelectionReader.TryRead(out _), "stable reader");
Check(NodeTooltipInfo.Refreshes == refreshes, "stable frames do not rebuild modifiers");
Check(UnityEngine.Object.Searches == 0, "no scene scans");
first.pointsAllocated = 1;
Check(PassiveSelectionReader.TryRead(out read) && read.Text.Contains("1 of 5"), "allocated points update");
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
Check(read.Text.Contains("requirement not met"), "unmet state preserved after cleaning colors");
system._activeNodeTooltipInfo.Requirements[0].IsMet = true;
Check(PassiveSelectionReader.TryRead(out read) && read.Text.Contains("requirement met"), "met state preserved");
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
Check(NodeTooltipInfo.Refreshes == refreshes + 1, "reopen refreshes fallback despite unchanged node counts");
var fullscreen = new GameObject().Add(new LE.UI.PanelSystem.FullScreenPanelPassiveTree { ActiveChildNavigable = owner });
NavigationManager.currentlySelected = fullscreen;
Check(PassiveSelectionReader.TryRead(out read), "modern fullscreen delegates to active passive child");
Check(read.Summary.Contains("Localized passive") && read.Summary.Contains("points allocated") &&
    read.Summary.Contains("Locked") && !read.Summary.Contains("Game description"), "brief selection contains points and status only");
Check(read.Details.Contains("Game description") && read.Details.Contains("Localized modifier"), "full native details retained");
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
Check(ScreenReader.Messages.Count == spoken + 2 && ScreenReader.Messages.Last().Text.Contains("Available"),
    "requirements status update speaks brief once");
Check(PassiveAccessibility.ReadCurrentDetails() && ScreenReader.Messages.Last().Text.Contains("Game description") &&
    ScreenReader.Messages.Last().Text.Contains("Localized modifier"), "manual gesture speaks native details");
spoken = ScreenReader.Messages.Count;
Tick(); Tick();
Check(ScreenReader.Messages.Count == spoken, "polling does not interrupt manual details");
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
    read.Details.Contains("Translated description") && NodeTooltipContentBuilder.Builds == builds + 1,
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
Check(PassiveAccessibility.HasActiveContent && NodeTooltipContentBuilder.Builds == builds + 1,
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
Check(opened.Contains(F("passive.node_threshold", 20, 15)), "A node above the tree's points names its threshold");
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
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Smite", T("passive.ability"), T("passive.ability_unlocked")) &&
    read.Details == "Smite description", "Unlock strip reads the skill name, unlocked state and description");
var sprite = strip.ActiveAbilityTooltips[0].gameObject.Child();
Check(PassiveAccessibility.TryHandleFocus(sprite), "The strip's sprite focus is owned instead of read as an object name");
strip.SelectedIndex = 1;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Judgement", T("passive.mastery_ability"), F("passive.ability_unlockable", 20, 15)),
    "A reachable skill states its threshold and the tree's points");
strip.SelectedIndex = 2;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Far skill", T("passive.ability"), F("passive.ability_unreachable", 200, 113)),
    "A skill beyond the tree's maximum says it cannot be reached");
strip.SelectedIndex = 3;
Check(!PassiveSelectionReader.TryRead(out _), "Out-of-range strip selection is rejected");
strip.SelectedIndex = 0;
fullscreen._masteryBarDisplay._hasFillValues = false;
Check(PassiveSelectionReader.TryRead(out read) && read.Summary == Join("Smite", T("passive.ability")), "Without bar values no state is guessed");
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
PassiveAccessibility.Reset();
Console.WriteLine($"Passed {checks} passive checks.");
