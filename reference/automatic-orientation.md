# Automatic map orientation

Requested after the manual F6/right-stick-down orientation feature. Enabled by
default, with separate sound and speech settings. This is local information:
the mod never changes movement, selects an interaction, or completes a quest.
Runtime listening remains pending; the game was not launched during development.

## What is heard

After a destination stays valid for one second, the screen reader names its
localized marker, the next path direction and approximate remaining path length
in game world units. A quiet natural D2R sample then repeats toward the **next
verified path corner**, including while standing still. Stereo position follows
the camera's screen axes; a lower pitch distinguishes directions behind the
screen's forward axis. The existing terrain radar continues describing obstacles
and openings separately.

The repeating sample accelerates as the remaining route becomes shorter: about
three seconds far away, approaching 0.8 seconds near the marker. Work is sampled
twice per second, so actual intervals round upward to a probe tick, and other
world sounds can delay a pulse. Cadence measures progress toward the destination,
not proximity to an intermediate bend. This sound uses D2R's `unexplored_room`
timbre but **does not claim that a room is unexplored**.

Speech reports stable direction changes of at least two of the eight screen
sectors, with at least six seconds between announcements. A diagonal wobble does
not repeatedly speak. Within one actual navigation body diameter of the marker,
the sound pauses and proximity is announced once; leaving two diameters resumes
it. Proximity is not objective completion or proof of entering a shop/room.

## Choosing a destination

Only currently displayed minimap markers with validated local source ownership
are eligible. Pinned active quest markers precede main and side quest markers.
The current target remains selected at equal priority so movement alone does
not silently change the destination. Native journal quest tracking controls this
priority; existing F6/right-stick-down reporting remains available independently.

A visible, named, interactable zone/dungeon exit can provide local orientation
when no reachable tracked marker is available. Exits require a verified route
within 32 actual navigation body widths by default. Speech names the destination
and explicitly says that this exit is not confirmed for the current quest. A
nearby exit might lead back to the previous zone; this is a landmark for the
player to choose, not an inferred best route or a dungeon solution.

## Verification and pacing

The service reuses the owned-agent and complete-NavMesh-path protections of
[manual orientation](objective-orientation.md). Automatic sound has **no
straight-line bearing fallback**. Partial, unsupported, wrong-floor and invalid
paths produce no directional sound. The next segment also requires an open
native navigation ray. A tiny reached corner is skipped only if the shortcut
also passes that ray, so a nearby wall bend is not skipped merely by distance.
Dynamic geometry is checked at query time, not guaranteed forever.

There is at most one path calculation per half-second probe. Untested candidates
are examined before retrying failures, so several blocked quests cannot starve
a reachable exit. Failed candidates retry in order after at least three seconds.
With an eligible destination retained, only one failed higher-priority candidate
is retried every ten seconds. A failed alternate probe mutes that tick, then the
retained route is freshly verified without repeating its name or settling again.
This can detect a newly opened route even while the player stands still.
No global object search runs in this service. A target disappearing, blocked
route, missing player/death or camera cancels its sound. Current visibility is
rechecked before every pulse; verified walkable paths may cross unvisited ground
because the game exposes no verified per-point exploration query.

Menus, dialogue, typing, loading, paused gameplay and lost window focus suspend
output. Sound resumes only after the current target/path settles again. Target
speech is remembered across menus, while a new scene/player resets the session.
Pending map, NPC, loot and quest speech and this frame's manual or urgent speech
have priority. Speech uses noninterrupting output and its own pacing; the backend's
unreliable busy flag cannot hold back all guidance indefinitely. Deferred
announcements always describe the current observation. Audio output failure does not disable screen-reader
feedback, and sound remains available without a detected screen reader.

## Configuration

All settings are under `[Automatic orientation]`; personal configuration is not
rewritten by deployment.

| Setting | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `true` | Enable the automatic service. |
| `Sound` | `true` | Enable repeating directional natural sound. |
| `Speech` | `true` | Name targets, major turns and marker proximity. |
| `Volume` | `35` | Sound volume, 0–100 percent. Zero keeps speech enabled. |
| `GuideToNamedExits` | `true` | Allow explicitly identified local exit guidance. |
| `LocalExitRangeInBodyWidths` | `32` | Maximum path length to a local exit, 4–100 body widths. |

## Validation

`LeAccess.PassiveGuidance.Tests` links the real state, service, route probe, audio
adapter and world audio reservation with deterministic native/device doubles.
It covers target/turn stability, near-marker hysteresis, cadence, camera axes,
bounded native work, quest/exit selection, stale-target cancellation, all output
gates, menu return, speech priority, sound-only/speech-only behavior, missing
reader/sample/device, audio arbitration/recovery and first-segment obstacles.
The existing manual orientation and native target-reader suites remain
regression checks. These are not an audible runtime confirmation.

The suite now passes **353 checks**. A regression reproduces an old queued pulse
surviving a target change while the new destination name waits for speech. Such
an ineligible pulse now releases its pending audio turn, allowing objects, NPCs
and terrain to continue. It can request a fresh turn once the new target is
ready; this cancellation does not cut off a sample already playing.

Listening checks: stand still with a visible objective, approach it around a
wall, cross a bend, reach/leave the marker, open/close a menu, untrack it, and
change zones. Verify direction and accelerating cadence without a key press.
Check nearby named exits with no reachable quest marker, including a return
exit. Confirm its name and the unconfirmed-for-quest qualification, and that
far-away exits stay quiet. Test online play, death, typing and focus loss.
