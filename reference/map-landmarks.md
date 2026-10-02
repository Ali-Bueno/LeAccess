# Named places inside maps — Steam 1.5.0.1

Investigated 2026-10-02 against build 25663618, with the game closed. The existing
zone reader announces the localized scene name once on entry. The additional
landmark reader announces **nearby named signposts and zone/dungeon destinations**.
This is a proximity label, not a claim that the player has entered a named room.

The later [spatial map-section system](map-sections.md) adds extracted structural
entry polygons across many fixed maps, actual generated rooms and current floor
descriptions independently of the nearby-label reader below. Its initial pilot
covered two bridges in The Old Road.

## Implemented behavior

`LandmarkProbe` examines the game's `WorldObjectClickListener.allListener`
registry five times per second. It accepts live available objects in the current
player's scene, within each listener's native `interactionRange`, including the
game's optional horizontal-distance rule. The nearest eligible named object wins;
equal distances use instance ID for a stable choice. Dead players and stale
scene/player objects are rejected. No global scene search, new native hook or
dependency is involved.

The following explicit `HoverTagOnMouseOver.HoverTargetType` values are read:

| Type | Authoritative text |
|---|---|
| `SIGNPOST` | The game's authored `hoverText`, cleaned of formatting. |
| `ZONE_TRANSITION` | Live `sceneInteraction.sceneName`, resolved through known scene metadata and `LocalizedName`. |
| `DUNGEON_TRANSITION` | Live `dungeonTransitionInteraction.sceneName`, resolved the same way. |
| `OTHER` / `LOCALIZATION_KEY` on an owned modern portal | The native initialized portal's localized `hoverText`, including its destination or player name. |

Modern portals were previously silent because their native initialization uses
`OTHER` and `LOCALIZATION_KEY`, neither of which the landmark reader accepted.
These types now require an enabled current-scene `PortalVisualController` whose
`mouseListenerObj` is exactly the candidate listener's GameObject and whose
`hoverTag` is exactly the tag being read. The declared portal tag also resolves
a null listener association when no same-object tag exists. A generic tag, nearby
portal, stale explicit association or different listener cannot supply identity.
The native `isNonInteractableByPlayer()` check and the listener's existing
`CanInteract()` and range checks must all permit the candidate.

The reader uses the listener's explicit `hoverTagOnMouseOver` association. When
that optional field is null, it reads the tag on the **exact same GameObject**;
it never searches parents, descendants or nearby objects. A stale explicit
association is rejected rather than replaced. Destination names come from
the live interaction, so a recycled or old UI label cannot name a previous exit.
Unknown destinations stay silent. Signpost text is game-authored and may remain
English if that is how the game supplies it.

After two matching observations, speech queues as **“Nearby: {name}”** or
**“Cerca: {nombre}”**, depending on the mod's language. It yields to menus,
dialogue, pending scene/loot/NPC speech and announcements in the same frame.
Staying beside the same label is silent; leaving range and approaching again can
repeat it. Closing a menu beside a previously announced object does not repeat
its label. Pending targets are revalidated before speaking. Critical life/mana
feedback retains its existing priority.

Enabled by default; the next launch creates:

```ini
[Landmarks]
AnnounceNearbyNames = true
```

Actual announcements log `[Landmark]`. Changed probe stage counts log
`[LandmarkStatus]` at most once per five seconds; unchanged status does not repeat.
Counts distinguish active listeners, available tags, exact-owner tags, native
range candidates and named competitive candidates. The existing zone entry reading and world
object sounds remain separate from this proximity text.

## What the investigation did not establish

There is no verified general native API for “currently on the bridge”, “in the kitchen”,
“on the stairs” or a named street within one scene. An authored sign or a
destination with such a name can be read, but unlabeled scenery cannot acquire a
reliable semantic name from this label reader. Authored spatial sections now
provide a separate path for individually verified map features. Pure collider transitions,
signs without a registered click listener and unlabelled decorative objects are
not covered. `SAME_SCENE_TRANSITION` is deliberately excluded: its game label is
the owning scene's name, not the destination room.

- `WorldAreaEnterListener` (`:223/:290/:328`) exposes geometry, trigger tags and
  callbacks, without a localized section-name field. Exit/collider listeners do
  not establish named room boundaries either.
- `LocationNotificationInteraction` exposes popup flags and chapter metadata.
  Native `LocationNotificationManager.ActivateNotification` RVA `0x25BB620` reads
  the current scene; `UpdateFields` RVA `0x25BB9D0` localizes scene/chapter names.
  Relaxing the current zone reader's scene-name ownership would not add subzones.
- `SceneLocation` (`:36/:51/:64`) is a registry of numeric IDs and transforms for
  positional anchors. `LocationDetector` tracks ability start/target positions.
  Neither provides a player-facing section name.
- `MinimapTextManager.updateText` RVA `0x1771D40` also uses
  `SceneDetails.LocalizedName`. `MinimapIconManager.icons` supplies icon/UI pairs,
  but native `Add` RVA `0x1F3D230` generates letter indices through `Fun.ToLetter`.
  Known `DMMapIcon.ToggleIndex` callers are encounter-window systems. Those
  letters are not evidence of named streets or rooms.
- Installed assets include 26,665 hashed UnityFS bundles. No targeted serialized
  scene/prefab evidence establishing named interior boundaries was recovered.
  This does not prove that every asset lacks them; broad asset extraction and
  decorative GameObject-name guesses were not used to fabricate coverage.

## Native label evidence

Follow-up read-only disassembly of the same installed build identifies
`PortalVisualController._InitializeAsync_d__25.MoveNext` RVA `0x190C750`.
It initially assigns `OTHER` at `0x190D6C9`, then branches by portal type to
initialize localized labels. Destination branches call
`SceneList.TryGetNonDefaultSceneDetails`, `SceneDetails.LocalizedName` and
`HoverTagOnMouseOver.SetHoverText` (examples at `0x190D8EE` / `0x190D9F3` /
`0x190DA0A` and `0x190DB28` / `0x190DC06` / `0x190DC1D`). These branches
assign `LOCALIZATION_KEY` at `0x190D996` / `0x190DBAF`. The tag's native setter
stores the localized result back into `hoverText`; the mod reads it without
calling the setter. Personal/return/town portal variants use this explicit modern
owner path. Older or collider-only portal implementations remain unverified.

`HoverTagOnMouseOver.SetHoverText()` RVA `0x1150960` dispatches on the named
type enum. Signposts render `hoverText` directly. Zone/dungeon cases resolve
their interaction's destination through `SceneList.GetSceneDisplayName`.
Same-scene transitions resolve the owning scene's build index. The string
overload, RVA `0x1150B80`, stores text and invokes that dispatcher. Neither setter
is called by the mod; both can mutate game/UI state.

`FloatingSceneTransition.SetTransitionText` RVA `0x17405B0` also calls
`SceneList.GetSceneDisplayName` (call at `0x17407C1`). `SceneDetails.LocalizedName`
RVA `0x15787B0` delegates to the same lookup at `0x1579B50`. Transition gate IDs
are technical spawn identifiers, not separately localized interior destinations.

## Runtime validation still required

The reported offline session emitted zone entry names but no landmark speech.
Installed `scene_Z12_unity.bundle` and `scene_Z22_unity.bundle` establish a
concrete missed association in the original probe:

| Scene | Listener path ID | Exact-owner tag path ID | Live destination interaction |
|---|---:|---:|---|
| Z12 | 17206 | 18302 | 16432 → Z22 |
| Z22 | 15565 | 16190 | 15211 → Z12 |
| Z22 | 15566 | 16191 | 15210 → Z32 |

Each listener's serialized `hoverTagOnMouseOver` is null, while its tag shares
the same `Logic` GameObject. Listeners, tags and destination interactions are
enabled, and the native interaction range is 9 metres. Native
`WorldObjectClickListener.Start` RVA `0x18A1380` caches NPC/conversation components
at `0xB8`/`0xC0`; it does not initialize the hover-tag field at `0xE8` (read by
`UseHoverTagOffset`, RVA `0x18A1580`). The fix reads the authored exact-owner tag
without changing that field or invoking any game setters. Disabled tags and
interactions still remain unavailable; next-session diagnostics distinguish
these runtime conditions from missing associations.

Reproduction evidence is in
`%TEMP%/LeAccess-landmark-runtime-20261002/{Z12,Z22}.json`, extracted with UnityPy
1.25.3 using cached MonoScript path IDs from
`%TEMP%/LeAccess-scene-assets-20261002/scripts.json`. The extraction helper
`probe.py` and native `disasm.py`/`map.json` are in that task directory. These
records prove installed ownership, not that the patched reader was heard in a
running game.

Two source-linked suites pass 107 checks: probe 53 and service 54. Current-scene
ownership, ranges, nearest selection, stale destination text, per-listener
failure isolation, menu/lifecycle handling, deduplication, speech priority and
real en/es formatting are covered with host doubles. Four existing suites also
pass: localization 132, controller menus/zones 38, world cues 89 and patch safety
31. Main build/deployment has zero warnings/errors and installed/built DLL hashes
match; see `STATUS.md`. Existing external catalogs are preserved, with the new
landmark key supplied by the embedded en/es defaults when absent.

Try a named sign and zone/dungeon entrance in both offline and online play. Walk
into range, wait, stay nearby, open/close a menu, leave and approach again. Confirm
the spoken destination matches the current doorway and language. Also check
locked/unavailable entrances, scene transitions and respawning. The static/native
investigation and host tests do not establish prefab coverage or audible results.
