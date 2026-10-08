using LeAccess.Accessibility;
using LeAccess.Accessibility.Players;
using LE.Networking;
using UnityEngine;

int checks = 0;
void Check(bool value,string why) { checks++; if (!value) throw new InvalidOperationException(why); }
PlayerActorSync Add(string name,float x=2)
{
    var visuals = new ActorVisuals { Identity = new() { Username = name } };
    visuals.transform.position = new(x,0,0);
    var sync = new PlayerActorSync { actorVisuals = visuals };
    visuals.actorSync = sync;
    PlayerStore.Players.Values.Add(sync);
    return sync;
}
void Reset()
{
    NearbyPlayerAccessibility.Reset(); PlayerStore.Players = new();
    PlayerFinder.player = new(); PlayerFinder.Local = new();
    var local = Add("Myself",0); local.localPlayer = PlayerFinder.Local;
    PlayerFinder.Local.actorSync = local; PlayerFinder.playerActorVisuals = local.actorVisuals;
    LocalPlayer.instance = null; LocalPlayerDiagnostics.Reset(); LeAccess.Plugin.Log.Infos.Clear();
    EpochInputManager.instance = new(); EHG.Multiplayer.GameplayEnvironment.IsOnlineClient = true;
    UnityEngine.SceneManagement.SceneManager.Handle = 1; Time.unscaledTimeAsDouble = 0;
    LeAccess.Accessibility.Navigation.NavigationAudioState.IsSuspended = false;
    LeAccess.Accessibility.Navigation.GameplayInputState.Owns = true;
    LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = false;
    LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
    ScreenReader.Messages.Clear(); ScreenReader.Available = true; ScreenReader.SpokeThisFrame = false;
}
void Tick(double time,bool pending=false) { Time.unscaledTimeAsDouble = time; NearbyPlayerAccessibility.Update(pending); }
Reset(); Check(!NearbyPlayerProbe.TryRead(out _),"Only the local player is quiet");
var remote = Add("<b>Amiga</b>");
Check(NearbyPlayerProbe.TryRead(out var target) && target.Name == "Amiga","Separate local visual root is authorized by bidirectional local sync ownership");
remote.actorVisuals.floatingPlayerName = new() { identityProvider = remote.actorVisuals, text = new() { text="AMIGA" } };
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "AMIGA","Owned native rendered public name is preferred");
remote.actorVisuals.floatingPlayerName.identityProvider = PlayerFinder.playerActorVisuals;
remote.actorVisuals.floatingPlayerName.text.text = "BORROWED";
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "Amiga","A foreign label cannot lend a name; public identity remains valid");
remote.actorVisuals.floatingPlayerName.identityProvider = remote.actorVisuals;
remote.actorVisuals.floatingPlayerName.updateLabelsNextFrame = true;
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "Amiga","Pending pooled label does not lend stale text");
remote.actorVisuals.Identity.Username = "";
Check(!NearbyPlayerProbe.TryRead(out _),"Blank identity never falls back to account IDs or stale labels");
remote.actorVisuals.Identity.Username = "Amiga";
remote.actorVisuals.dead = true;
Check(NearbyPlayerProbe.TryRead(out _),"Native player targeting imposes no invented remote death restriction");
remote.actorVisuals.gameObject.scene = new(2);
Check(NearbyPlayerProbe.TryRead(out _),"Loaded network/persistent scene may differ from active gameplay scene");
remote.actorVisuals.gameObject.scene = new(0);
Check(!NearbyPlayerProbe.TryRead(out _),"Unloaded remote scene rejects retained visual");
remote.actorVisuals.gameObject.scene = new(1);
remote.actorVisuals.actorSync = new ActorSync();
Check(!NearbyPlayerProbe.TryRead(out _),"Foreign back-pointer cannot claim player registry identity");
remote.actorVisuals.actorSync = remote;
remote.actorVisuals.isPlayer = false;
Check(!NearbyPlayerProbe.TryRead(out _),"An NPC or enemy in a corrupted player registry is excluded");
remote.actorVisuals.isPlayer = true; remote.actorVisuals.gameObject.activeInHierarchy = false;
Check(!NearbyPlayerProbe.TryRead(out _),"Inactive remote entity is excluded");
remote.actorVisuals.gameObject.activeInHierarchy = true; remote.actorVisuals.WasCollected = true;
Check(!NearbyPlayerProbe.TryRead(out _),"Collected remote visual is never read");
remote.actorVisuals.WasCollected = false; remote.actorVisuals.Destroyed = true;
Check(!NearbyPlayerProbe.TryRead(out _),"Destroyed remote visual is excluded");
remote.actorVisuals.Destroyed = false; remote.actorVisuals.transform.position = new(8,0,0);
Check(!NearbyPlayerProbe.TryRead(out _),"Native player radius constrains proximity");
EpochInputManager.instance.targetingConfiguration.playersRange = 9;
Check(NearbyPlayerProbe.TryRead(out _),"Changed native range is observed immediately");
EpochInputManager.instance.targetingConfiguration.playersRange = float.NaN;
Check(!NearbyPlayerProbe.TryRead(out _),"Invalid radius has no guessed fallback");
Reset(); remote = Add("Remote"); PlayerFinder.Local.actorSync = new ActorSync();
Check(!NearbyPlayerProbe.TryRead(out _),"Foreign local visual ownership rejects world origin");
Reset(); remote = Add("Remote"); ((PlayerActorSync)PlayerFinder.Local.actorSync).localPlayer = new();
Check(!NearbyPlayerProbe.TryRead(out _),"Local sync back-pointer must match the current local player");
Reset(); remote = Add("Remote"); EHG.Multiplayer.GameplayEnvironment.IsOnlineClient = false;
Check(!NearbyPlayerProbe.TryRead(out _),"Offline has no invented other players");
Reset(); var first = Add("First",3); var nearer = Add("Nearer",1);
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "Nearer","Nearest named remote wins");
first.actorVisuals.transform.position = new(1,0,0);
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "First","Equal distance uses stable instance identity");
first.actorVisuals.ThrowIdentity = true;
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "Nearer" && NearbyPlayerProbe.LastFailure != null,"Broken candidate does not hide another good player");
Reset(); remote = Add("Remote"); Tick(0); Tick(.11);
Check(ScreenReader.Messages.Single() == ("Remote",false),"Settled nearby name is noninterrupting");
Tick(.22); Tick(.33); Check(ScreenReader.Messages.Count == 1,"Standing nearby does not repeat the same player");
remote.actorVisuals.transform.position = new(20,0,0); Tick(.44);
remote.actorVisuals.transform.position = new(2,0,0); Tick(.55); Tick(.66);
Check(ScreenReader.Messages.Count == 2,"Leaving and approaching rearms naming");
Reset(); remote = Add("Remote"); Tick(0,true); Tick(.11,true);
Check(ScreenReader.Messages.Count == 0 && NearbyPlayerAccessibility.HasPendingContent,"Higher priority speech defers the current player name");
remote.actorVisuals.Identity.Username = "Current"; Tick(.22,true); Tick(.33);
Check(ScreenReader.Messages.Single().Text == "Current","Only the current name remains pending after native identity changes");
LeAccess.Accessibility.Navigation.GameplayInputState.Owns = false; Tick(.34);
Check(!NearbyPlayerAccessibility.HasPendingContent,"Menus clear pending player speech immediately");

// Identity speech: username, then character name and level when known.
string Level(int value) => LeAccess.I18n.ModText.Format("progression.level", value);
Reset(); LeAccess.Accessibility.Social.FriendCharacterNames.ByUsername.Clear(); remote = Add("Canario");
Check(NearbyPlayerProbe.TryRead(out target) && target.Character == null && target.Level == 0,"Unknown character and level stay empty, never invented");
Check(PlayerIdentityText.Compose("Canario", null, 0) == "Canario","Username alone when nothing else is known");
remote.actorVisuals.clientCharacterInfoProvider = new() { level = 34 };
LeAccess.Accessibility.Social.FriendCharacterNames.ByUsername["Canario"] = "Ezio";
Check(NearbyPlayerProbe.TryRead(out target) && target.Character == "Ezio" && target.Level == 34,"Probe carries the friend character name and synced level");
Check(PlayerIdentityText.Compose(target.Name, target.Character, target.Level) == $"Canario, Ezio, {Level(34)}","Username, character name, level");
Check(PlayerIdentityText.Compose("Canario", null, 34) == $"Canario, {Level(34)}","Level without character name");
Check(PlayerIdentityText.Compose("Canario", " ", 0) == "Canario","Blank character name is skipped");
var all = new List<OtherPlayer>();
Check(NearbyPlayerProbe.TryReadAll(all) && all.Single().Level == 34 && all.Single().Character == "Ezio","Scanner list carries character and level");
Reset(); LeAccess.Accessibility.Social.FriendCharacterNames.ByUsername["Canario"] = "Ezio";
remote = Add("Canario"); remote.actorVisuals.clientCharacterInfoProvider = new() { level = 34 };
Tick(0); Tick(.11); Tick(.22);
Check(ScreenReader.Messages.Single().Text == $"Canario, Ezio, {Level(34)}" && !ScreenReader.Messages.Single().Interrupt,"Approach says username, character and level once");
LeAccess.Accessibility.Social.FriendCharacterNames.ByUsername.Clear(); remote.actorVisuals.clientCharacterInfoProvider.level = 0;
Tick(.33); Tick(.44);
Check(ScreenReader.Messages.Count == 1,"A flickering character or level read does not repeat the announcement");
remote.actorVisuals.clientCharacterInfoProvider.level = 35; Tick(.55); Tick(.66);
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages[1].Text == $"Canario, Ezio, {Level(35)}","A real level change is announced");
LeAccess.Accessibility.Social.FriendCharacterNames.ByUsername.Clear();

// Shared local-player source: PlayerFinder.FindPlayer zeroes the rig cache on re-find (co-op join).
ActorVisuals JoinedHost()
{
    Reset(); remote = Add("Host");
    PlayerFinder.Local.gameObject = PlayerFinder.player;
    LocalPlayer.instance = PlayerFinder.Local; PlayerFinder.playerActorVisuals = null;
    return PlayerFinder.Local.actorSync.actorVisuals;
}
var ownRig = JoinedHost();
Check(LocalPlayerSource.Visuals == ownRig && LocalPlayerSource.Player == PlayerFinder.player && LocalPlayerSource.FromLocalPlayer,
    "Cleared PlayerFinder rig cache resolves through LocalPlayer's own sync");
Check(NearbyPlayerProbe.TryRead(out target) && target.Name == "Host","Nearby names survive the cleared rig cache");
PlayerFinder.playerActorVisuals = remote.actorVisuals;
Check(LocalPlayerSource.Visuals == ownRig && NearbyPlayerProbe.TryRead(out target) && target.Name == "Host",
    "Another player's rig in the cache is never taken as the local rig");
PlayerFinder.player.Destroyed = true; var spawned = new GameObject(); PlayerFinder.Local.gameObject = spawned;
Check(LocalPlayerSource.Player == spawned,"Stale PlayerFinder root yields to the newly awakened local root");
ownRig = JoinedHost(); ownRig.actorSync = remote;
Check(LocalPlayerSource.Visuals is null,"A rig that links to another sync is rejected");
JoinedHost(); PlayerFinder.Local.actorSync.WasCollected = true;
Check(LocalPlayerSource.Visuals is null,"Collected local sync is never dereferenced");
ownRig = JoinedHost(); PlayerFinder.Local.Destroyed = true; PlayerFinder.playerActorVisuals = ownRig;
Check(!LocalPlayerSource.FromLocalPlayer && LocalPlayerSource.Visuals == ownRig && LocalPlayerSource.Player == PlayerFinder.player,
    "Without a live LocalPlayer the PlayerFinder caches remain the source");
JoinedHost(); EHG.Multiplayer.GameplayEnvironment.IsOnlineClient = false; PlayerFinder.playerActorVisuals = null;
Check(!LocalPlayerSource.FromLocalPlayer && LocalPlayerSource.Visuals is null && LocalPlayerSource.Player == PlayerFinder.player,
    "Offline resolution is unchanged PlayerFinder state");

// One diagnostic line per scene/root/rig change.
ownRig = JoinedHost(); PlayerFinder.player.name = "Local Player(Clone)"; ownRig.gameObject.name = "v_Rig";
LocalPlayerDiagnostics.Update(); LocalPlayerDiagnostics.Update();
var line = LeAccess.Plugin.Log.Infos.Single();
Check(line.Contains("root=Local Player(Clone)#") && line.Contains("rig=v_Rig#") && line.Contains("source=LocalPlayer.instance") &&
    line.Contains("finder_player=same") && line.Contains("finder_rig=missing"),"Diagnostics name the resolved root, rig and stale cache once");
UnityEngine.SceneManagement.SceneManager.Handle = 2; LocalPlayerDiagnostics.Update();
Check(LeAccess.Plugin.Log.Infos.Count == 2 && LeAccess.Plugin.Log.Infos[1].Contains("scene=Scene2"),"A scene load logs the new resolution");
PlayerFinder.Local.actorSync.actorVisuals = null; LocalPlayerDiagnostics.Update();
Check(LeAccess.Plugin.Log.Infos.Count == 3 && LeAccess.Plugin.Log.Infos[2].Contains("rig=none"),"A lost rig is reported as a change");
JoinedHost(); PlayerFinder.Local.Destroyed = true; PlayerFinder.player = null; LocalPlayerDiagnostics.Update();
Check(LeAccess.Plugin.Log.Infos.Count == 0,"No line while no local root exists");
Console.WriteLine($"Nearby player tests passed: {checks} checks.");
