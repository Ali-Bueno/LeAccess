# Nearby other-player names

`NearbyPlayerAccessibility.Update` reads the nearest named other player inside
the game's current `TargetingConfiguration.playersRange`. It speaks the name
once on approach or target/name change, without interruption. Leaving the radius,
opening a menu, losing the scene/player, or closing gameplay clears pending names.
Higher-priority content can defer the name; obsolete targets are never retained.
This is naming only, with no new audio cue or automatic navigation.

## Authoritative installed APIs

- Native `KeyBasedInteractableTargeting.UpdatePlayerTargets`, RVA `0x1890150`,
  calls `LE.Networking.PlayerStore.GetPlayerActorSyncs` and reads the corresponding
  `actorVisuals.position()`. At `0x189074D` it compares distance against the
  configuration's `playersRange`; no guessed radius is used.
- `PlayerStore.GetPlayerActorSyncs` is exposed in
  `last epoch code/LE.Networking/PlayerStore.cs:2088`. Each candidate must be a
  live active `PlayerActorSync`, with a live active `isPlayer` visual and the
  reciprocal `visuals.actorSync` pointer matching that registry entry.
- The local visual is authenticated through
  `PlayerFinder.getLocalPlayerInMultiplayer`, matching `LocalPlayer.actorSync`,
  `ActorVisuals.actorSync`, `PlayerActorSync.localPlayer`, and `actorVisuals`.
  Separate local visual roots are permitted. Local sync, visual pointer and
  local-player hierarchy exclusions prevent self announcements.
- `FloatingPlayerName.UpdatePlayerName`, RVA `0x173F6C0`, renders
  `ActorVisuals.UserIdentity.Username` uppercased. This is the public in-game
  player name, not the saved character name, master account ID or title account ID.
  The reader never accesses those IDs or Unity object names.

The preferred label must have the exact visual as `identityProvider`, be
initialized, have no pending label refresh, and not contain native default text.
Its cleaned text must match the current public username ignoring case; otherwise
the current username supplies the name. Render visibility is intentionally not
required: game settings or rendering optimizations can hide a valid name label.
No tooltip or name update method is invoked.

Networking entities can live in loaded persistent scenes different from the
active gameplay scene. Ownership and loaded/active lifetime gates therefore
replace an incorrect scene-handle equality assumption. The active scene and local
player instance identify the announcement session. The native player-targeting
branch does not impose an ActorVisuals death gate, so an otherwise active remote
player is not rejected merely for being dead. Offline clients produce no players.

Polling reuses `NpcCueTiming.PollSeconds` (0.1 seconds) and
`NpcNameAnnouncementState`. The registry's ICollection is copied into a bounded
snapshot of its current count because IL2CPP generic interface enumerators do not
expose the C# foreach protocol. No Unity discovery scan, hook, configuration or
runtime dependency was added.

The source-linked player suite passes **29 checks**, covering public-name
ownership/fallback, local and foreign syncs, separate local roots, persistent and
unloaded scenes, dead remote players, stale/destroyed/inactive entities, current
range, nearest/tie selection, per-candidate failures, deferred speech and reentry.
The main no-deploy build passes with zero warnings/errors. The game remained
closed; online proximity/listening has not been tested in a live multiplayer scene.
