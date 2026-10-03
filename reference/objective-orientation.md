# Objective orientation

Current validation targets Steam build 25690669. Live listening remains pending;
the game was not launched for development.

## Controls and scope

During gameplay, right-stick-down from neutral or F6 reports a visible tracked
objective marker. The existing right-stick-up menu tooltip gesture is unchanged.
The controller gesture uses the same half-axis threshold and neutral edge as
menu details; holding the stick does not repeat requests. Input is observed,
never consumed, and typing, menus, conversations, loading and focus loss gate it.
Native game bindings still apply. `[Orientation]` exposes `Enabled`, `ReportKey`
and `RightStickDown` if the player needs another keyboard key or wants to disable
the controller gesture.

The marker's localized quest/objective is named on every report. Pinned quests
take priority over automatically tracked main and side quests. Within the same
priority, the last target remains selected while eligible so closer/farther
comparisons do not silently switch destinations; first selection uses distance.
Pin the desired quest in the native journal to prioritize it.
An active, owned [Morditas return arrow](hazard-sources.md) temporarily takes
precedence and is named as a return to that encounter. It uses the actual native
target even without a ready minimap registry, and disappears when the arrow hides.
When no eligible quest marker exists, a visible named zone/dungeon transition
can be reported. This is explicitly an unconfirmed exit for the current quest,
not evidence that it is the correct dungeon branch. Hidden objective markers and
guessed exits are excluded. The game exposes no verified point-by-point fog
query; a requested route toward a visible marker can cross unvisited walkable
ground. It is not a claim that the entire route has already been explored.

## Direction and progress

Eight direction sectors use the gameplay camera's horizontal forward and right
axes, matching the radar's screen-relative directions. A complete NavMesh path
allows the next corner and remaining path length to be read on request. This can
point sideways to go around a wall even when the objective itself is straight
ahead. An absent, partial, invalid or unverifiable path falls back to an explicitly
labeled straight-line bearing. A verified blocked direct ray is stated separately.
Distance is rounded upward in the game's world units, not converted into invented
steps or real-world metres.

Progress compares consecutive requests for the same session, target and distance
kind. A difference of at least one body diameter (bounded by the actual navigation
radius) produces closer/farther feedback; smaller movement accumulates against
the prior accepted comparison baseline. Rejected output does not advance that
baseline or log a successful report; another explicit request reads fresh state.
A changed target, loading, materially moved marker,
or switch between route and direct distance resets comparison. Being within a
body diameter means only **near the map marker**, never quest completion.

## Native query safety

The current Unity interop's filter-object `CalculatePath` and its restored
`CalculatePathFilterInternal` contain invalid managed-reference/Span IL. They are
not called. The existing native-invoked `CalculatePath(Vector3, Vector3, int,
NavMeshPath)` is used only with one registered navigation build setting matching
the local player's owned agent. The mask and sampling radius come from that
agent and native baked radius. Unsupported agent layouts return a bearing.

Both endpoints first use the proven primitive `SamplePositionFilter` with the
owned agent type and area mask. Floor projection is bounded; sampled endpoints
must match the complete path's first/last corners. Every corner and accumulated
length must be finite. `PathPartial` is never described as a route. Only local
query objects are allocated; no destination, player position, quest state, UI
selection or navigation setting is changed. NavMesh results describe traversable
geometry at query time, not a guarantee about future dynamic obstacles.

Target ownership, visibility and source evidence are documented in
[the marker reader notes](guidance-targets.md). Quest progress speech has its own
[state observer](quest-updates.md), independent of this manual control. The old
automatic quest audio ping remains removed.
For the new hands-free route sound and turn announcements, see
[automatic orientation](automatic-orientation.md); the manual report described
here remains available independently.

## Listening checks

`LeAccess.Guidance.Tests` passes 157 source-linked geometry, route, input and
service checks. `LeAccess.GuidanceTargets.Tests` passes 148 registry/visibility/
ownership checks. Main build/deployment succeeds with no warnings or errors;
these checks use game doubles and do not establish audible runtime behavior.

1. With a tracked objective visible, press F6 or push the right stick down. Check
   the named objective, screen direction and route/bearing wording.
2. Move around a wall and query again; a verified route should report its next
   bend, not point through the wall. Compare distance on the same route.
3. Walk closer/farther, hold the stick down, and return it to neutral before
   repeating. Check no repeated request while held.
4. Hide/untrack/complete the objective, pin another, and change zones. No old
   target or comparison should survive invalidation.
5. Try a dungeon with a visible named exit. If no quest marker is available,
   verify the explicit warning that this exit is not confirmed for the mission.
6. Test typing, menus, dialogue, death, reload and online play. No game input
   should be replaced or movement automated.
7. Leave an active Morditas gauntlet and request direction. Check return wording,
   route/bearing distinction and disappearance when the native arrow hides.
