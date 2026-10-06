# Multiplayer local-player resolution

Every world system resolves the local player through `Players/LocalPlayerSource`
(`Player`, `Visuals`). Offline it returns the `PlayerFinder` fields unchanged.
On an online client it starts from `LocalPlayer.instance`: the root is that
component's GameObject, and the rig is `instance.actorSync.actorVisuals`, accepted
only when `rig.actorSync` points back to the same sync. A dead or missing
`LocalPlayer.instance` falls back to the `PlayerFinder` fields.

## Symptom (co-op join, 2026-10-06, game 1.5.1.2, build 25711439)

The friend's log (`LogOutput.log`) shows the client alone in Z12 working normally.
He then used the party panel's "Portal to player". The game rebuilt the connection
(`Destroying all net objects`, `PlayerSync.local destroyed`, line 973) and loaded
Z12 again (line 985). From then on the online-only gates failed: enemy cues reported
`hostile registry unavailable` (1034), landmarks `player_or_scene_unavailable` (1035),
the nearest enemy `online player unavailable or dead`, and exploration refused to
start (1054). The radar kept running, but `body_radius` fell from 0.450 to the
helper agent's 0.100 (1031).

The game errors near the load are not the cause. `Player Ability List count…` and
`Corresponding ability null…` also appear on the first, working load (lines 621–634).
`ReceiveKillCount failed… 148C…` names the host's identity: its data arrived before
the host's `PlayerActorSync` (D41) existed.

## Native evidence (GameAssembly RVAs, this build)

- PlayerFinder static fields: `+0x08 player`, `+0x10 localPlayer`,
  `+0x30 playerActorVisuals`, `+0x38 playerActorComponent`.
- `FindPlayer` (`0x17A7EC0`) zeroes `+0x08…+0x60`, then sets
  `player = GameObject.FindGameObjectWithTag("Player")` (literal index `0x673D`) and
  invokes `playerChangedEvent`. Its only callers are `getPlayer` (`0x17AB800`) and
  `TryGetPlayer`, and they call it only when the cached `player` is Unity-null.
  Per-frame callers such as `ControllerManager.Update`, `MiniMapMover.OnUpdateTick`
  and `HealthPotionChargeIndicator.OnUpdateTick` therefore re-find a destroyed
  player within a frame.
- Only PlayerFinder's own lazy getters write those fields. `getPlayerVisuals`
  (`0x17AB690`) fills `+0x30` with `getPlayer().GetComponentInChildren<ActorVisuals>()`.
  Its callers are ability use, `FloatingHealthBarAttacher.EnableAllPlayersHPBars`,
  `PresetSoundPlayer` and the equip-update state machines. Online,
  `getAllPlayerActorVisuals` (`0x17A8B10`) tail-calls
  `PlayerStore.GetPlayerActorVisuals` and does not fill the field.
  `getLocalActorSync` (`0x17A9B70`) returns `getLocalPlayerInMultiplayer()+0x88`,
  the LocalPlayer's `actorSync`.
- `LocalPlayer.Awake` (`0x27F7960`) unconditionally stores `instance = this`.
  `OnFirstTick` (`0x27F7E10`) makes that GameObject the camera target.
  `ExistentAndAlive` (`0x27F79C0`) follows `instance → +0x88 → +0x68 → byte +0x12C`:
  sync, then rig, then rig death (field names inferred).
- Assets (`PermaLoad.bundle` + `monoscripts.bundle`): the `Local Player` prefab root
  has `m_Tag` 6 (built-in `Player`). It holds `LocalPlayer`, `NavMeshAgent`,
  `CapsuleCollider` and 41 other client behaviours, but no `Actor`. Remote players
  come from the `PlayerSync` and `ActorSync Player` (`PlayerActorSync`) prefabs, both
  `m_Tag` 0. The tag search therefore finds only the local root.

## Cause

Verified: re-finding the player clears `playerActorVisuals`. Online, nothing
refills it until gameplay happens to call `getPlayerVisuals()`.

Verified from the mod's code paths and the log: after the join the field was zero.
The radar skips rig resolution only when that wrapper is C# `null`, which is why it
stayed available and used the agent radius. The gates that require the rig
(`NearestEnemyProbe.IsAvailable`, `LandmarkProbe`, `MapSectionWorld`, `EnemyScanProbe`)
all failed. `PlayerFinder.player` itself was the live local root, because the radar's
player, scene and agent gates passed. The radar's position moved independently of
the host's: for example, the friend was stuck against walls at -48.4,81.8. Its first
sample equals the host's idle spot because the portal places the arrival there.

Inferred: on the first load a gameplay `getPlayerVisuals()` ran after the last
re-find, but after the join it did not. The exact call order is unmeasured.

## Mod behaviour

`LocalPlayerSource` replaces every raw `PlayerFinder.playerActorVisuals` read: radar,
`MapPlayerVisuals`, landmarks, nearest enemy, enemy scan/cues, exploration death,
map sections, world cues, NPC probe, vitals, nearby players, encounter guidance and
boss life. The same files also read `Player` from it. This identity does not depend
on PlayerFinder's lazy refresh, on the other player's presence or on a stale
`player`. Session keys are unchanged because the root is the same tagged object.

`LocalPlayerDiagnostics` logs one line per scene/root/rig change, for example:
`[LocalPlayer] scene=Z12 root=Local Player(Clone)#… rig=v_… source=LocalPlayer.instance
finder_player=same finder_rig=missing`. In the next co-op test, `finder_rig=missing`
after the join confirms the cleared cache, and `rig=` shows the rig the mod uses.

Tests: the Players suite covers the resolver and diagnostics. The Navigation,
LandmarkProbe and MapSectionWorld suites reproduce the cleared cache and another
player's rig left in it.

## Remaining runtime checks

No co-op session has run with this change. Confirm the `[LocalPlayer]` line after a
portal-to-player join, `body_radius` near 0.45, and that landmarks, nearest enemy,
enemy cues and exploration resume.
