using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Npcs;
using UnityEngine;
using UnityEngine.SceneManagement;

int checks = 0;
void Check(bool result, string message)
{
    checks++;
    if (!result) throw new InvalidOperationException(message);
}
WorldObjectClickListener Candidate(float x, float y, float z, bool npc = true)
{
    var root = new GameObject();
    root.transform.position = new(x, y, z);
    var listener = root.Add(new WorldObjectClickListener());
    if (npc) root.Add(new NPCInteraction());
    WorldObjectClickListener.allListener.Add(listener);
    return listener;
}
var player = new GameObject();
PlayerFinder.player = player;
PlayerFinder.playerActorVisuals = player.Add(new ActorVisuals());
Camera.Current = new GameObject().Add(new Camera());
var probe = new NpcWorldProbe();
Check(!probe.TryCapture(1, out _), "Empty registry has no NPC target");
var scenery = Candidate(0, 0, 0.5f, false);
var pet = Candidate(0, 0, 0.6f, false);
var enemy = Candidate(0, 0, 0.7f, false);
Check(!probe.TryCapture(1, out _), "Nearby scenery, pets and enemies without NPC interaction cannot become NPC cues");
var npc = Candidate(3, 0, 4);
npc.gameObject.Add(new NpcNameTooltip { TopTextKey = "npc.blacksmith", TopText = "Blacksmith" });
Check(probe.TryCapture(1, out var result) && result.InstanceId == npc.GetInstanceID() && result.Name == "Herrero",
    "Talkable marked NPC at action range boundary is selected and localized");
Check(result.Distance == 5 && result.Range == 5 && Math.Abs(result.Pan - .6f) < .00001 && Math.Abs(result.Forward - .8f) < .00001,
    "World distance and normalized camera-relative stereo/front direction are derived from game data");
int reads = Camera.MainReads;
Check(probe.TryCapture(1, out _) && Camera.MainReads == reads, "Gameplay camera is cached between observations");
Check(!probe.TryCapture(.5f, out _), "Smaller multiplier contracts the actual NPC interaction range");
Check(!probe.TryCapture(0, out _) && !probe.TryCapture(float.NaN, out _) && !probe.TryCapture(float.PositiveInfinity, out _),
    "Invalid configured ranges never produce targets");
npc.interactionRange = 2;
Check(!probe.TryCapture(2, out _) && probe.TryCapture(3, out result) && result.Range == 6,
    "Positive multiplier scales each listener range without an invented common radius");
npc.interactionRange = 5;
var disabled = Candidate(0, 0, 1);
disabled.isActiveAndEnabled = false;
var blocked = Candidate(0, 0, 1.1f);
blocked.Interactable = false;
var deadWrapper = Candidate(0, 0, 1.2f);
deadWrapper.WasCollected = true;
var broken = Candidate(0, 0, 1.3f);
broken.ThrowOnCanInteract = true;
var inactive = Candidate(0, 0, 1.4f);
inactive.gameObject.activeInHierarchy = false;
var invalidPosition = Candidate(float.NaN, 0, 0);
Check(probe.TryCapture(1, out result) && result.InstanceId == npc.GetInstanceID(),
    "Disabled, noninteractable, collected, throwing, inactive and invalid nearest candidates cannot hide a valid NPC");
Check(probe.LastFailure == "InvalidOperationException: Invalid native candidate",
    "Per-candidate native failures remain available for diagnostics without losing a valid target");
WorldObjectClickListener.allListener.Clear();
var elevated = Candidate(0, 6, 0);
elevated.controllerTargetingUsePlayerY = true;
Check(!probe.TryCapture(1, out _), "Unverified controller height flag cannot collapse full 3D interaction distance");
Check(probe.LastFailure is null, "Normal out-of-range capture clears earlier failures and remains quiet");
elevated.transform.position = new(0, 0, 0);
Check(probe.TryCapture(1, out result) && result.Distance == 0 && result.Pan == 0 && result.Forward == 0,
    "Zero offset has finite neutral bearing");
Check(result.Name is null, "Missing diagnostic NPC name does not suppress its sound target");
WorldObjectClickListener.allListener.Clear();
var relay = Candidate(0, 0, 2, false);
relay._NPCinteraction = relay.gameObject.Add(new RelayNPCInteraction());
Check(probe.TryCapture(1, out result) && result.InstanceId == relay.GetInstanceID(), "Cached relay NPC marker covers vendor/quest interactions");
relay._NPCinteraction.isActiveAndEnabled = false;
Check(!probe.TryCapture(1, out _), "Disabled NPC marker is not actionable");
relay.gameObject.Add(new NpcNameTooltip { TopText = "Traveler" });
Check(!probe.TryCapture(1, out _), "NPC label alone does not prove a talkable interaction");
relay._leConversationTrigger = relay.gameObject.Add(new ConversationTrigger());
Check(probe.TryCapture(1, out result) && result.Name == "Traveler", "Live direct conversation plus NPC label provides alternative eligibility");
relay._leConversationTrigger.isActiveAndEnabled = false;
Check(!probe.TryCapture(1, out _), "Inactive direct dialogue cannot supply NPC cues");
WorldObjectClickListener.allListener.Clear();
var first = Candidate(-2, 0, 0);
var second = Candidate(2, 0, 0);
WorldObjectClickListener.allListener.Reverse();
Check(probe.TryCapture(1, out result) && result.InstanceId == first.GetInstanceID(), "Equal-distance ties use stable instance identity rather than registry order");
first.transform.position = new(0, 0, 2);
second.Interactable = false;
Camera.Current.transform.forward = new(1, -1, 0);
Camera.Current.transform.right = new(0, 0, -1);
Check(probe.TryCapture(1, out result) && result.Pan == -1 && result.Forward == 0, "Cached camera rotation updates bearing on every observation");
first.gameObject.Add(new NpcNameTooltip { TopTextKey = "npc.blacksmith" });
Localization.Throw = true;
Check(probe.TryCapture(1, out result) && result.Name is null, "Unavailable localization cannot remove an otherwise valid sound target");
Localization.Throw = false;
long session = result.Session;
SceneManager.CurrentHandle++;
Check(probe.TryCapture(1, out result) && result.Session != session && Camera.MainReads == reads + 1,
    "Scene transition changes session and reacquires main camera");
session = result.Session;
PlayerFinder.player = new GameObject();
Check(probe.TryCapture(1, out result) && result.Session != session, "Replacement local player changes cue session");
PlayerFinder.playerActorVisuals.dead = true;
Check(!probe.TryCapture(1, out _), "Dead local player cannot receive NPC proximity cues");
PlayerFinder.playerActorVisuals = null;
Check(probe.TryCapture(1, out _), "Actual active player can be used when optional visuals are unavailable");
NavigationAudioState.IsSuspended = true;
Check(!probe.TryCapture(1, out _), "Pause/loading/dialogue suspension blocks the probe");
NavigationAudioState.IsSuspended = false;
GameplayInputState.Gameplay = false;
Check(!probe.TryCapture(1, out _), "Nonpausing menu ownership blocks retained world NPCs");
GameplayInputState.Gameplay = true;
WorldObjectClickListener.allListener.WasCollected = true;
Check(!probe.TryCapture(1, out _), "Collected native registry is rejected before enumeration");
WorldObjectClickListener.allListener.WasCollected = false;
Camera.Current.WasCollected = true;
Check(!probe.TryCapture(1, out _), "Collected camera never undergoes unsafe Unity null comparison");
Camera.Current = new GameObject().Add(new Camera());
Check(probe.TryCapture(1, out _), "Camera recreation recovers without reinitializing the service");
Camera.Current.transform.forward = new(0, -1, 0);
Check(!probe.TryCapture(1, out _), "Degenerate camera projection cannot produce an invented direction");
probe.Reset();
Camera.ThrowOnMain = true;
Check(!probe.TryCapture(1, out _) && probe.LastFailure == "InvalidOperationException: Native camera lookup failed",
    "Unexpected outer native failure can be diagnosed separately from an empty registry");
Camera.ThrowOnMain = false;
Camera.Current.transform.forward = new(0, 0, 1);
Check(probe.TryCapture(1, out _), "Explicit reset can safely reacquire the gameplay context");
Check(probe.LastFailure is null, "Successful next capture clears the old native failure");
WorldObjectClickListener.allListener.Clear();
var nestedName = Candidate(0, 0, 1);
nestedName.gameObject.Child().Add(new NpcNameTooltip { TopTextKey = "npc.blacksmith", TopText = "Blacksmith" });
Check(probe.TryCapture(1, out result) && result.Name == "Herrero" && result.NameSource == "NPC tooltip",
    "NPC label on a child component is localized without scanning another registry");

WorldObjectClickListener.allListener.Clear();
var parentRoot = new GameObject();
parentRoot.Add(new NpcNameTooltip { TopText = "Anciana" });
var collider = parentRoot.Child().Add(new WorldObjectClickListener());
collider.gameObject.Add(new NPCInteraction());
WorldObjectClickListener.allListener.Add(collider);
Check(probe.TryCapture(1, out result) && result.Name == "Anciana",
    "Listener on a collider child can read its NPC parent's authoritative name component");

WorldObjectClickListener.allListener.Clear();
var siblingRoot = new GameObject();
var npcMarker = siblingRoot.Add(new NPCInteraction());
siblingRoot.Child().Add(new NpcNameTooltip { TopText = "Mercader" });
var siblingCollider = siblingRoot.Child().Add(new WorldObjectClickListener());
siblingCollider.gameObject.Add(new NPCInteraction());
siblingCollider._NPCinteraction = siblingRoot.Add(new RelayNPCInteraction());
WorldObjectClickListener.allListener.Add(siblingCollider);
Check(probe.TryCapture(1, out result) && result.Name == "Mercader",
    "Explicit NPC relay root resolves a sibling visual name without climbing into unrelated scene roots");

WorldObjectClickListener.allListener.Clear();
var displayNpc = Candidate(0, 0, 1);
displayNpc.gameObject.Add(new BaseDisplayInformation { LocalizedName = "Guardiana" });
Check(probe.TryCapture(1, out result) && result.Name == "Guardiana" && result.NameSource == "actor display",
    "Authoritative localized actor display name covers NPCs without a tooltip component");

WorldObjectClickListener.allListener.Clear();
var dialogNpc = Candidate(0, 0, 1);
var conversant = new GameObject();
conversant.Add(new PixelCrushers.DialogueSystem.OverrideActorName { DisplayName = "Erza" });
dialogNpc._leConversationTrigger = dialogNpc.gameObject.Add(new ConversationTrigger { conversant = conversant.transform });
Check(probe.TryCapture(1, out result) && result.Name == "Erza" && result.NameSource == "dialogue actor",
    "Explicit conversation conversant supplies its actual dialogue display name");
PlayerFinder.player.Add(new PixelCrushers.DialogueSystem.OverrideActorName { DisplayName = "Mi personaje" });
dialogNpc._leConversationTrigger.conversant = PlayerFinder.player.transform;
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Conversation player actor cannot be mislabeled as the nearby NPC");

WorldObjectClickListener.allListener.Clear();
var unnamedOwner = Candidate(0, 0, 1);
var unrelatedRoot = unnamedOwner.gameObject.Child();
unrelatedRoot.Add(new WorldObjectClickListener());
unrelatedRoot.Add(new NpcNameTooltip { TopText = "Otro NPC" });
Check(probe.TryCapture(1, out result) && result.Name is null,
    "A nested distinct world listener's label is never borrowed by an unnamed NPC");
var invalidName = unnamedOwner.gameObject.Add(new NpcNameTooltip { TopText = "Obsoleto", WasCollected = true });
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected name component is rejected before native access without removing the sound target");
unnamedOwner.gameObject.Child().Add(new BaseDisplayInformation { LocalizedName = "Nombre válido" });
Check(probe.TryCapture(1, out result) && result.Name == "Nombre válido",
    "Valid alternate owned name survives a collected tooltip source");

// Installed Z22: listener + relay on Grael/Grael; only the explicitly referenced child owns dialogue.
WorldObjectClickListener.allListener.Clear();
var grael = Candidate(0, 0, 1, false);
var graelRelay = grael.gameObject.Add(new RelayNPCInteraction());
grael._NPCinteraction = graelRelay;
var questBranch = grael.gameObject.Child();
questBranch.transform.NativeActorName = "Grael";
var condition = questBranch.Add(new ConditionHandler());
graelRelay.otherInteractables.Add(condition);
var database = new PixelCrushers.DialogueSystem.DialogueDatabase();
database.Conversations[702] = new() { ActorID = 1, ConversantID = 300 };
database.Actors[1] = new() { Name = "Player", IsPlayer = true };
database.Actors[300] = new() { Name = "Z_Overhaul_Grael" };
Localization.Translations["Actor_Name_Grael"] = "Grael";
Localization.Translations["Actor_Name_Player"] = "Mi personaje";
var questTrigger = questBranch.Add(new LE.Dialog.LEConversationTrigger
    { ConversationID = 702, useExternalDb = true, ExternalDb = database });
Check(grael._leConversationTrigger is null && questTrigger.conversant is null &&
    probe.TryCapture(1, out result) && result.Name == "Grael" && result.NameSource == "dialogue database",
    "Actual Z22 relay-child structure resolves its DB conversant without a cached listener trigger or scene conversant");
Check(PixelCrushers.DialogueSystem.OverrideActorName.LastActorTransform == questBranch.transform &&
    !Localization.Translations.ContainsKey("Actor_Name_Z_Overhaul_Grael"),
    "Native null-conversant fallback uses exactly the trigger transform, not the unlocalized DB name");
questBranch.transform.NativeActorName = "Z_Overhaul_Grael";
Check(probe.TryCapture(1, out result) && result.Name is null,
    "An untranslated name returned by the native conversant resolver is never announced raw");
questBranch.transform.NativeActorName = null;
Check(probe.TryCapture(1, out result) && result.Name is null, "Missing native speaker name cannot borrow a DB identifier");
questBranch.transform.NativeActorName = "Grael";
var explicitSpeaker = new GameObject();
explicitSpeaker.transform.NativeActorName = "Erza";
Localization.Translations["Actor_Name_Erza"] = "Erza localizada";
questTrigger.conversant = explicitSpeaker.transform;
Check(probe.TryCapture(1, out result) && result.Name == "Erza localizada" &&
    PixelCrushers.DialogueSystem.OverrideActorName.LastActorTransform == explicitSpeaker.transform,
    "Explicit native conversant takes precedence over the trigger's transform");
explicitSpeaker.transform.WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected explicit conversant cannot be replaced with a guessed trigger identity");
explicitSpeaker.transform.WasCollected = false;
explicitSpeaker.Add(new WorldObjectClickListener());
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Explicit conversant owned by another listener is rejected");
questTrigger.conversant = PlayerFinder.player.transform;
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Player transform cannot become the NPC even when the conversation DB conversant is a nonplayer");
questTrigger.conversant = null;
Localization.Translations["Actor_Name_Grael"] = "Grael localizado";
Check(probe.TryCapture(1, out result) && result.Name == "Grael localizado",
    "Proximity reads current game localization rather than an internal DB identifier or stale name cache");
database.Conversations[702].ConversantID = 1;
Check(probe.TryCapture(1, out result) && result.Name is null,
    "DB player actor cannot become an NPC name even when its localization exists");
database.Conversations[702].ConversantID = 300;
Localization.Translations.Remove("Actor_Name_Grael");
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Failed native actor-name lookup leaves the sound available without speaking Z_Overhaul_Grael");
Localization.Translations["Actor_Name_Grael"] = "Actor_Name_Grael";
Check(probe.TryCapture(1, out result) && result.Name is null, "Untranslated localization keys cannot become speech");
Localization.Translations["Actor_Name_Grael"] = "  ";
Check(probe.TryCapture(1, out result) && result.Name is null, "Blank localized NPC names remain unavailable");
Localization.Translations["Actor_Name_Grael"] = "Grael";
questTrigger.isActiveAndEnabled = false;
Check(probe.TryCapture(1, out result) && result.Name is null, "Disabled dialogue branch cannot supply a name");
questTrigger.isActiveAndEnabled = true;
questBranch.activeInHierarchy = false;
Check(probe.TryCapture(1, out result) && result.Name is null, "Inactive quest branch cannot supply a name");
questBranch.activeInHierarchy = true;
condition.WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected relay targets are rejected before accessing their native component");
condition.WasCollected = false;
questTrigger.WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected conversation triggers leave proximity audio intact");
questTrigger.WasCollected = false;
database.WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected databases are rejected before Unity comparison or DB lookup");
database.WasCollected = false;
database.Conversations[702].WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected conversation records are not read");
database.Conversations[702].WasCollected = false;
database.Actors[300].WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected database actors are not read");
database.Actors[300].WasCollected = false;
questTrigger.ConversationID = 703;
Check(probe.TryCapture(1, out result) && result.Name is null, "Unknown conversation ID never borrows another conversation");
questTrigger.ConversationID = 702;
database.Conversations[702].ConversantID = 999;
Check(probe.TryCapture(1, out result) && result.Name is null, "Unknown conversant never falls back to the conversation actor");
database.Conversations[702].ConversantID = 300;
database.Actors[300].Name = null;
Check(probe.TryCapture(1, out result) && result.Name == "Grael", "Authoritative native conversant name is independent of the DB internal identifier");
database.Actors[300].Name = "Z_Overhaul_Grael";
var manager = new GameObject().Add(new PixelCrushers.DialogueSystem.DialogueSystemController { initialDatabase = database });
PixelCrushers.DialogueSystem.DialogueManager.instance = manager;
questTrigger.ExternalDb = null;
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Missing explicitly selected external DB cannot silently switch to a global DB with matching IDs");
questTrigger.useExternalDb = false;
Check(probe.TryCapture(1, out result) && result.Name == "Grael",
    "Native non-external route reads the already existing manager initialDatabase");
manager.WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected manager is never used or replaced by a singleton-creating accessor");
PixelCrushers.DialogueSystem.DialogueManager.instance = null;
Check(probe.TryCapture(1, out result) && result.Name is null, "Absent manager stays absent when reading a non-external trigger");
questTrigger.useExternalDb = true;
questTrigger.ExternalDb = database;
var foreignListener = questBranch.Add(new WorldObjectClickListener());
Check(probe.TryCapture(1, out result) && result.Name is null,
    "A relay link into another listener cannot borrow that listener's conversation actor");
questBranch.Components.Remove(foreignListener);
graelRelay.otherInteractables.Clear();
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Nearby child dialogue without an explicit relay link is not inferred as this NPC's identity");
var unlinkedContainer = grael.gameObject.Child();
graelRelay.otherInteractables.Add(unlinkedContainer.Add(new ConditionHandler()));
var siblingDialogue = unlinkedContainer.Child().Add(new LE.Dialog.LEConversationTrigger
    { ConversationID = 702, useExternalDb = true, ExternalDb = database });
Check(probe.TryCapture(1, out result) && result.Name is null,
    "A linked container cannot borrow an arbitrary descendant or sibling conversation trigger");
graelRelay.otherInteractables.Clear();
graelRelay.otherInteractables.Add(condition);
var otherBranch = grael.gameObject.Child();
otherBranch.transform.NativeActorName = "Grael";
graelRelay.otherInteractables.Add(otherBranch.Add(new ConditionHandler()));
var otherTrigger = otherBranch.Add(new LE.Dialog.LEConversationTrigger
    { ConversationID = 704, useExternalDb = true, ExternalDb = database });
database.Conversations[704] = new() { ActorID = 1, ConversantID = 300 };
Check(probe.TryCapture(1, out result) && result.Name == "Grael",
    "Several authored conversation branches for the same DB actor agree on NPC identity");
database.Conversations[704].ConversantID = 301;
database.Actors[301] = new() { Name = "Another_Grael" };
Localization.Translations["Actor_Name_Another_Grael"] = "Grael";
Check(probe.TryCapture(1, out result) && result.Name is null,
    "Different active DB actors remain ambiguous even if their display names coincide");
otherTrigger.isActiveAndEnabled = false;
Check(probe.TryCapture(1, out result) && result.Name == "Grael", "Inactive competing branch cannot hide the live NPC identity");
otherTrigger.isActiveAndEnabled = true;
otherTrigger.ExternalDb = null;
Check(probe.TryCapture(1, out result) && result.Name is null, "Unreadable active conversation branch prevents a misleading partial identity");
otherTrigger.isActiveAndEnabled = false;
graelRelay.otherInteractables.WasCollected = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is null,
    "Collected relay list is not enumerated");
graelRelay.otherInteractables.WasCollected = false;
grael._NPCinteraction = null;
Check(probe.TryCapture(1, out result) && result.Name == "Grael",
    "Same-GO authored relay remains usable before the listener relay cache is populated");
Localization.Throw = true;
Check(probe.TryCapture(1, out result) && result.Name is null && probe.LastFailure is not null,
    "Native dialogue localization failure remains diagnostic without suppressing the sound target");
Localization.Throw = false;
Check(probe.TryCapture(1, out result) && result.Name == "Grael" && probe.LastFailure is null,
    "Successful next localization recovers without retaining a failure or blank name");
graelRelay.otherInteractables.Clear();
grael._leConversationTrigger = questTrigger;
Check(probe.TryCapture(1, out result) && result.Name == "Grael",
    "An explicitly cached owned trigger uses the same authoritative speaker resolver");
grael._leConversationTrigger = null;
var directTrigger = grael.gameObject.Add(new LE.Dialog.LEConversationTrigger
    { ConversationID = 702, useExternalDb = true, ExternalDb = database });
grael.transform.NativeActorName = "Grael";
Check(probe.TryCapture(1, out result) && result.Name == "Grael",
    "An authored same-object trigger works before the native listener cache is populated");
grael.gameObject.Add(new NpcNameTooltip { TopText = "Nombre mostrado" });
Check(probe.TryCapture(1, out result) && result.Name == "Nombre mostrado" && result.NameSource == "NPC tooltip",
    "Actual NPC name component retains precedence over database fallback");

Console.WriteLine($"NPC probe tests passed: {checks} checks.");
