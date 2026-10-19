# Danger and encounter warning sources

Read-only audit of Steam build **25690669**, 2026-10-03. The binary and metadata
hashes are recorded in [Steam port notes](steam-port.md). Native addresses below
describe this build only; production code does not use them as offsets.

**Steam 1.5 (2026-10-19).** The Morditas timer, enemies-left and leaving widgets are now created with
`NotificationSystem.CreateImmediateNotification`, which bypasses `ShowOnChannel`, so the channel hook never sees
them. The [Rage of Morditas reader](rage-of-morditas.md) announces the hunt, the gauntlet, leaving the circle, the
abandonment time and returning from the game's own state and hooks; current addresses are in
[its hook table](rage-of-morditas.md#hooks). The addresses below are of build 25690669.

| Source | Meaning and current coverage |
|---|---|
| Vitals | Actual local health, ward and mana changes use the existing critical/band reader. This reports player state, not the shape or timing of an incoming attack. |
| Native warning/modal text | The [window reader](dungeon-warning-reading.md) reads the focused visible warning. A modal's existence is not proof of a nearby environmental hazard. |
| Encounter abandonment | `RageOfMorditasOutOfGauntletNotification` renders a localized countdown when all players have left the gauntlet. The [Rage of Morditas reader](rage-of-morditas.md) says leaving, the abandonment time and returning; the [typed notification reader](announcement-lifecycle.md) reads the widget only when that reader follows no encounter. |
| Morditas return arrow | The native arrow supplies a guarded local return target for manual and automatic orientation. Automatic guidance still requires a verified route. |
| Attack telegraphs and dangerous ground | General visual-only damage areas still lack a verified unified player-facing source. Ability object names, visual effects and haptics are insufficient to infer hostility, affected floor, safe direction or time to impact. |

## Countdown evidence

`RageOfMorditasGauntletVisualController.OnAllPlayersOutOfGauntletChanged`
(RVA `1514C40`; 1.5 `151DC90`) forwards the native status and encounter configuration duration
to `Notifications.ToggleRageOfMorditasOutOfGauntletNotification` (`1780B50`; 1.5 `17848F0`).
That method creates the notice through `NotificationSystem` if needed (1.5:
`CreateImmediateNotification`, never through the channel hook). For an
existing notice, another true status calls `StartCountdown` directly; false
calls `Hide`. Thus the generic channel-delivery hook alone cannot detect every
restart of the same visible widget.

`StartCountdown` (`2077EA0`) assigns a newly allocated coroutine to
`_currentCountdownCoroutine` on each call. Its `MoveNext` (`207E350`) renders an
integer countdown using `Localization.GetTextFormat("Encounter_AbandonTitle",
remaining, fallback)`, with fallback `Abandoning Event in {remaining}s`. It
waits one second, subtracts one, and repeats until hiding the notice. The full
instruction therefore changes every second; it is not an unstable per-frame
decimal label. The existing two-observation content tracker alone would repeat
the full sentence each second.

These facts were resolved from freshly generated `LE.dll` metadata and the
installed `MethodAddressToToken.db`, then checked against `GameAssembly.dll`.
String literals were decoded using the installed LibCpp2IL metadata-39 format:
12-byte section headers and four-byte literal data indices. No networking
method, game method or audio device was executed for this inspection.

## Arrow evidence and remaining validation

`OnInGauntletStatusChanged` (`15150C0`) negates the player's in-zone status
before calling `SetPointerActive`; when the pointer has not loaded yet, it
retains that negated status. Pointer `Init` (`151CC60`) stores its supplied
look-at target and obtains the local player's transform from
`PlayerFinder.getLocalActorSync`. The reader uses distinct return-to-encounter
wording, not a quest or zone-exit label. The arrow alone does not establish a
safe route or imply that the region outside it deals damage.

Further current-build checks resolve `UnityExtensions.CleanNulls` (`955990`)
and `Component.get_transform` (`40192B0`) after `getLocalActorSync`: the stored
player transform belongs to that exact sync component. It is not an inferred
actor visual or gameplay transform. `FixedUpdate` (`151CA60`) rotates toward
`_lookAtTransform` and follows `_playerTransform`.

The asynchronous `CreateGauntletWarningVisual.MoveNext` (`1535F00`) supplies the
gauntlet visual controller's own transform as the target. Its native body
**inlines** pointer initialization, including both stored transforms and the
`SetActive` call. An `Init` postfix alone therefore cannot reliably discover
these pointers. A read-only postfix on Unity's actual `FixedUpdate` callback
registers the pointer instead. Its fresh native address has no other method
aliases and passes the installed signature audit: 60 declarations / 59 targets.

The installed catalog resolves `GauntletWarningPointerVisualControllerSoftRef`
to `assets_b8a1e08455f403cb.bundle`. Its two-object prefab contains
`RageOfMorditas_WarningPointerVFX` and an initially inactive `Pointer` child;
the latter owns an enabled `MeshRenderer`. The controller's `pointerObject`
reference names that child. This is a world indicator, not a screen-space label.
These asset/native observations identify the source, not live confirmation of
visibility or audible direction.

## Return guidance ownership and timing

`EncounterGuidanceTargets` requires the pointer, its owned renderer and target
to be active in the captured player's loaded world. The renderer must be enabled
and not force-hidden. The target must be the exact gauntlet controller whose
pointer field refers back; its encounter sync must also refer back, and both
native states must still be `Gauntlet`. The stored player transform must match
the current local sync. Online ownership follows that sync's current, reciprocal,
living player visuals without requiring a server gameplay actor; offline follows
the current living actor. Network sync components may inhabit other loaded
scenes, while player presentation and the target must belong to the active world.
The captured player session must still match. Destroyed/unloaded sources expire;
plugin cleanup clears the registry. No scene-wide object scan is used.

While the arrow qualifies, its return target temporarily supersedes ordinary
quest/exit targets. Manual F6/right-stick-down preserves an explicit bearing-only
fallback when no route can be verified. Automatic orientation keeps the existing
complete-path, floor, next-segment and camera checks; it does not redirect toward
an unrelated quest when that return route fails. Hiding the native arrow ends
return guidance and allows ordinary target selection again.

The installed data gives the abandonment countdown four seconds. A newly stable
return target therefore bypasses the ordinary six-second narration gap, retaining
the one-second target settling and current-frame speech guard. An early pass runs
after the native abandonment notice and before ambient quest/world narration,
at most twice per second. A successful urgent probe occupies the normal probe
turn too. Menus, typing, dialogue, loading, pause, unavailable local ownership and
foreground subtitles still gate output. Existing automatic sound/speech settings
apply; exit-specific switches/range limits do not classify this as a zone exit.

Source-linked target tests now pass 148 checks, including 57 encounter ownership,
lifecycle and online/offline regressions. The passive service/state suite covers
early priority, accepted-output pacing and stale acknowledgement. No game,
networking action, movement or audio device was invoked during validation.

Live checks remain: countdown appearance and return/restart while objectives
and loot are updating, online ownership of the return arrow, and actual
attack/ground telegraphs in each dungeon. Automated notification tests establish
delivery, deduplication and ownership logic; they do not prove audible timing.
