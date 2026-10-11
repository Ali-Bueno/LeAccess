# Reactive navigation radar

LeAccess adapts the reactive radar of the author's unreleased Diablo 2 mod, `D:\code\c++\diablo 2 access project\d2r-access`.
This first port covers walkable ground and obstacles. It leaves movement under
the player's control and needs no extra navigation buttons.

## What the player hears

- A short knock when an obstacle becomes close in a screen direction.
- A soft breath when that direction opens: rising for up, falling for down,
  steady for the sides. Left and right use stereo panning.
- Silence for unchanged terrain, including a steady corridor or standing still.

Up, left, right and down follow the gameplay camera, rather than the character's
facing. These are flat stereo cues, without world audio attenuation or reverb.
The initial readings after loading, teleporting or resuming are silent.

## Configuration

BepInEx writes the plugin's configuration under the game's `BepInEx/config/`
directory when LeAccess loads. The entries are:

| Section | Key | Default | Meaning |
|---|---|---|---|
| Reactive radar | Enabled | true | Enable obstacle/open-ground transition cues |
| Reactive radar | Volume | 100 | Percentage of the radar cue level (D2R's 0.17 peak + 3 dB since 2026-10-09: hard to hear under spinning-hammer combat) |
| Reactive radar | OpenDepthInBodyWidths | 3.3333333 | Shortest walkable depth that can sound open when the space goes on beyond it (2026-10-09: see "Dynamic openings") |
| Reactive radar | AudioBufferMilliseconds | 100 | Output buffering; 50–300 ms, applies on next game launch |
| Navigation audio | ContinuousWallSonar | false | Also enable the previous continuous wall tones |
| World object sounds | Enabled | true | Category cue for the current or nearest available world object |
| World object sounds | Volume | 100 | Category cue volume in percent |

Reloading the configuration applies these settings. Restarting the game also
applies them. The open-depth setting accepts 2–20 body widths as a bounded local
navigation tuning range; its best value still needs listening tests in Last Epoch.
The earlier quest beacon remains removed. [Automatic orientation](automatic-orientation.md)
now provides optional sound and speech for verified walking routes, with its own
configuration and shared audio scheduling.

## Data and behavior

`RadarWorldProbe` uses the shared local root and rig from `LocalPlayerSource`
([multiplayer-local-player.md](multiplayer-local-player.md)), the local player's
`NavMeshAgent.radius`, `agentTypeID` and `areaMask`, `Actor.getRadius()`, and the active gameplay camera.
When the local Actor is absent or its radius is not ready, the owned live
`ActorVisuals.getRadius()` supplies body size, including online clients. An online
player with known body size must not inherit the narrower helper agent's warning reach.
Warning reach uses the actor's body diameter (at least the agent diameter), while
origin sampling retains the NavMesh agent radius. A small helper agent must not
shrink every warning distance. It caches actor/agent/camera references, refreshing them
on scene/player changes, destruction or IL2CPP collection. Suspension and probe
failures clear that cache. Navigation positions use the Unity XZ plane.
Ray depths and remembered wall positions now share the sampled NavMesh origin;
mixing it with the unprojected player position displaced remembered boundaries.
Negligible actor-radius float jitter preserves the settings instance; replacing
it on every tiny difference would continually discard pending confirmation.

Each of four directions calls `NavMesh.RaycastFilter` from a sampled point
within the player's radius. The NavMesh is already built for the selected agent's
footprint. A dead/inactive player or unavailable camera, agent or sampled origin suspends the
radar; missing data is never reported as a wall. A path that goes around a wall
does not make the direct probe open.
Player and navigation-agent ownership must match the active scene and local hierarchy;
a still-live old `LocalPlayer.instance.navMeshAgent` cannot supply another
player's radius or query filter. Disabled agent components remain usable when
their owner is active, as required by native controller movement. Inactive cameras
are rejected; persistent gameplay cameras need not belong to the player's scene.
Visual rigs additionally use the shared `MapPlayerVisuals` identity check: an
owned Actor's `actorSync.gameplayActor` must point back to that exact Actor before
its detached `actorVisuals` is accepted. A stale legacy visual cache cannot replace
that authoritative rig. Dead, disabled, collected or foreign owned rigs remain
ineligible. Eleven new regressions reproduce the former rejection of this valid
ownership graph; the navigation suite now passes 447 checks. This does not establish
the user's live offline hierarchy without a gameplay observation.

`RadarEngine` is independent of Unity. It confirms changes by traveled distance,
or by two observations after the player stops. Different closing/opening depths
provide hysteresis. After a wall is announced, its position is remembered:
backing away does not count as an opening unless the new probe reaches beyond
that wall. Suppressed retreat cues are discarded, rather than replayed later.
The silent baseline also remembers existing walls, so backing away after loading
or closing a menu cannot turn the same wall into a false opening.

The queue keeps the latest event in each direction, drops cues that no longer
match the observed state, prioritizes obstacles, and rotates among equal-priority
directions. A playing cue finishes before the next cue, separated by a 50 ms gap.
Disable, loading, pause, dialogue, focus loss, teleport, scene change and prolonged polling gaps clear
pending cues and re-establish a silent baseline.
Camera rotation only resets the baseline when the projected probe-tip movement
reaches the existing confirmation distance. Smaller camera drift does not erase
obstacle/opening candidates on every poll; cumulative turns still reset.

Conversation state comes from `DialogueSystemNavigable.ConversationStart`,
`ConversationEnd` and `OnDisable`, whose hooks only delegate to
`NavigationAudioState`. `GameplayInputState.OwnsInput()` also gates terrain cues:
non-pausing menus suspend them, and returning to gameplay establishes a silent baseline.
Transient audio-device startup failures retry every five seconds, with repeated
identical errors suppressed in the log.
Unexpected playback stops retain their exception. Recovery replaces the device
only after `PlaybackStopped` confirms completion, using the same retry interval;
it discards obsolete samples and does not perform an immediate Stop/Play loop.
The sample provider writes floats individually because NAudio's byte-backed
`WaveBuffer` view is incompatible with `Array.Copy` from a real float array.
Suspension reasons and recovery are logged only when they change. A bounded
five-second heartbeat reports actual geometry, states, resets, generated/submitted
cues and audio-buffer consumption. Consumed samples mean copied into NAudio's
buffer, not proof of sound reaching the listener. This distinguishes quiet geometry
from repeated resets or stopped playback without adding test noises.
The crashes reported on 2026-10-01 were traced to malformed generated
`NavMeshQueryFilter` overloads. The probe now uses `SamplePositionFilter` and
`RaycastFilter` with primitive agent type/mask arguments; see
[native evidence and retest steps](runtime-feedback.md).

## Port provenance

- `d2r-access/src/radar.cpp`: confirmation, silent baseline, wall memory,
  opening interval and reset policy.
- `src/radar_passage.cpp`: opening/closing hysteresis. The Diablo room graph,
  collision masks and grid reader are replaced by Last Epoch's NavMesh.
- `src/radar.h`: 50 ms polls, 250 ms maximum gap, 200 ms stationary detection,
  100 ms stationary confirmation and 500 ms opening interval.
- `src/radar_synth.cpp` and `src/radar_audio.h`: 48 kHz stereo, 120 ms wall knock,
  180 ms filtered-noise breath, directional pitch, pan and cue level.

Distance defaults are *navigation tuning*, not Last Epoch constants. Diablo's
10-cell opening depth, 20-cell probe reach and 1-cell confirmation distance are
normalized by its 3-cell humanoid footprint and multiplied by the actual
Last Epoch actor diameter, bounded below by the agent diameter. Closing depth is one body width nearer than opening
depth, with a minimum of one body width. No Diablo distance is copied as metres.

## Validation and remaining work

Build and deployment:

```powershell
dotnet build LeAccess/LeAccess.csproj
```

Game-independent checks, with no new packages or game assemblies:

```powershell
dotnet run --project tests/LeAccess.Navigation.Tests/LeAccess.Navigation.Tests.csproj
```

The build passes without warnings or errors and deploys `LeAccess.dll` to the
configured game plugin directory. All 344 deterministic navigation checks pass and cover
state transitions, jitter, confirmation,
retreats, real openings, invalid readings, resets, queue arbitration and sound
synthesis, body/agent size differences, float-jitter settings stability, camera
drift and diagnostic pacing. The 18 checks in `tests/LeAccess.RadarAudio.Tests`
exercise the real NAudio byte adapter and controlled output recovery without
opening a device. Live telemetry exposed repeated initial resets and stopped
playback with zero consumed samples; both causes were reproduced and corrected.
The user confirmed stable gameplay and silence with the previous build. In the
updated 16:25 offline session, five cues / 69,120 samples were fully consumed,
playback remained running with no stops, and repeated initial resets disappeared.
This confirms the repaired processing path; actual listening remains necessary for:

1. A quiet corridor and a wall approached from each screen direction.
2. A side opening followed by its closing edge; backing away from a wall.
3. Bridges, ramps, narrow passages and movement skills that teleport.
4. Pausing menus, non-pausing menus, NPC conversations and zone transitions.
5. Cue volume and open depth with headphones and the screen reader active.

The 2026-10-02 precision follow-up passes **436 navigation checks** and a production
build without warnings/errors. A regression reproduced a false opening when
retreating from a wall present at the silent baseline; startup/resume retreat
and a genuine doorway are now distinguished. Source-linked tests also cover an
old live agent after player replacement, detached/inactive owners, owned disabled
child agents, stale scenes/cameras, and online visual body size with no Actor.
`last epoch code/ActorVisuals.cs:getRadius` declares current-build method token
`100682642`; no new native hook is needed. The four screen directions, sounds and
configured reach remain unchanged. In-game listening is still required.

Doors and exits currently sound according to traversability. Map icons expose
semantic door/stairs/transition types, but no universal closed-door state was
verified. Last Epoch's fog-of-war wrapper also exposes no reliable
point-discovered query. Distinct door/exit/unexplored sounds need those data
sources and runtime checks before being added.

### Follow-up: reported choppy audio

The user subsequently confirmed audible cues but reported stuttering, including
quiet areas without combat or loot. The completed session recorded 413 cues /
5,794,560 submitted samples, 5,787,840 consumed and only 6,720 canceled samples;
the stream stayed running with zero stops/recoveries. Only four polling-gap
resets occurred. This does not support recurrent cancellation or the previous
first-cue failure as the ongoing cause.

NAudio's two buffers split the old 50 ms setting into 25 ms refill blocks.
The radar now defaults to 100 ms, matching the existing quest output, with
configurable 50–300 ms bounds (previous radar / NAudio default). This is a
buffering mitigation pending listening, not proof that hardware underruns caused
the report. NPC buffering is unchanged. Callback-gap counters use monotonic
time and the actual configured buffer duration; late callbacks are marked as
stall risks, not asserted hardware underruns. The five-second status includes
`buffer_ms`, `callback_stalls`, `cue_callback_stalls` and
`max_callback_gap_ms` without logging from the audio callback.

The audio regression suite now passes 44 checks, including exact wall/breath
samples through successive real NAudio 25 ms byte-buffer callbacks, with no
missing/interleaved zero samples. Navigation passes 345 checks; the original
audio-provider suite still passes 1024 assertions. Ground-item focus also no
longer scans a complete item tooltip every frame, reducing avoidable work there.

## NPC sounds

`Npcs/NpcSounds` adds the D2R NPC wooden tap separately from terrain radar.
It follows the nearest available NPC: stereo pan indicates
left/right on the screen, lower pitch indicates screen-down, and pulses become
faster with proximity. Changing the nearest NPC preserves the pulse interval.
The optional name announcement identifies the same nearby NPC once on approach
or a target/name change. It neither starts conversations nor moves the player
or selects an interaction.

The maintained `WorldObjectClickListener.allListener` registry supplies each
candidate. An enabled `NPCInteraction`/relay, or an NPC-name component together
with a live conversation trigger, identifies NPCs rather than other clickable
objects. `CanInteract()` filters current availability. The detection range is
each listener's runtime `interactionRange`, multiplied by the configurable
factor below; D2R collision-subtile distances are not copied as Unity metres.
Selection uses actual 3D distance with stable instance-ID ties. Bearing uses
the gameplay camera's normalized XZ axes. The game controls interaction
eligibility; no extra terrain line-of-sight or route calculation is imposed.

Source declarations: `last epoch code/WorldObjectClickListener.cs` for
`allListener`, `interactionRange`, `_NPCinteraction`, `_leConversationTrigger`
and `CanInteract`; `NPCInteraction.cs` / `RelayNPCInteraction.cs` for NPC
identity; `NpcNameTooltip.cs` for optional localized diagnostic names.

The existing BepInEx config gains an `NPC sounds` section:

| Key | Default | Meaning |
|---|---|---|
| Enabled | true | Enable nearby NPC sounds |
| AnnounceNames | true | Speak the nearest available NPC's name independently of sound volume |
| Volume | 100 | Percentage of D2R's NPC cue level |
| InteractionRangeMultiplier | 1 | Multiply each NPC's own interaction range; positive values only |

Sound design comes from D2R `src/entity_cue_synth.cpp`, `src/entity_cues.h`,
`src/entity_cues.cpp` and `docs/entity-cues.md`: an 84 ms wooden tap at 48 kHz,
850 Hz with damped partials and filtered noise; equal-power pan, rear pitch
factor 0.80, and a 150–450 ms interval polled every 100 ms. Distance affects
cadence rather than volume. These are documented audio/accessibility choices,
not inferred Last Epoch constants.

One persistent NPC output reuses the corrected stereo sample provider and
event-confirmed recovery. It is separate from the radar output so pulses cannot
replace obstacle cues. Pause, loading, lost focus, menus, active tutorials and
dialogue silence it and discard its target. No scene-wide searches, new native
hooks, runtime packages or audio files are added. Current NPC focus and sounds
still require an in-game listening test, including a merchant, a quest NPC,
disabled interactions, scene changes and controller/mouse input.

Automated checks: `tests/LeAccess.NpcAudio.Tests` passes 59 synthesis/cadence
checks and `tests/LeAccess.NpcProbe.Tests` passes 35 checks of the production
probe with host doubles. These cover nearest selection, eligibility, distance,
camera bearing, invalid native wrappers and state changes. The shared input
ownership extraction also passes the existing 58 ground-loot checks; the reused
audio output passes its 18 adapter/recovery checks without opening a device.
`tests/LeAccess.NpcLifecycle.Tests` adds 70 checks of the real service and timing,
including one persistent stream, scene/menu/dialogue cancellation, five-second
recovery throttling, cleanup and deduplicated probe/playback failures.

The integrated build passed with zero warnings/errors. The installed DLL matches
SHA-256 `6D0BE77050678B7C7FA9B3BDE140765DA18F5962D99E73E6D1C2FD0FA63C1CD7`.
The normal offline BAT launch at 17:20:05 on 2026-10-01 initialized NVDA,
configured NPC sounds and completed plugin loading with no new crash dump.
Runtime config confirms enabled=true, volume=100, range multiplier=1.
Nearby-NPC cue playback remains a listening check; startup alone does not prove it.
The subsequent user report confirms NPC cues work. It also identified missing
names: all recorded targets had empty same-object tooltip names. Name lookup
now follows the selected NPC's owned hierarchy, explicit relay/conversant roots,
localized tooltip/display data and actual dialogue name overrides. It excludes
the player and other listeners, and does not invent names from GameObject labels
or conversation titles. Name delivery waits for stable current text and higher
priority UI/zone/loot speech; leaving range, scene changes or UI ownership clears
pending names. Runtime name coverage still needs validation.

## Steam-build world cues and coordination

`WorldCues/WorldCueSounds` adds a two-chime coin cue, a single metallic equipment
strike and a rising two-note interactable cue. Their frequencies/durations are
local sound-design choices, not game constants. They use the gameplay camera's
stereo bearing and the NPC cue's lower rear pitch. The current valid game target
is preferred; otherwise the nearest eligible object wins. An unchanged target
does not repeat, and target changes share a 450 ms minimum interval.

Fresh Steam interop supplies `GroundGoldVisuals.all`, `GroundItemVisuals.all`
and `WorldObjectClickListener.allListener`. Equipment requires `isEquipment()`;
loot labels must be visible, including text alpha/culling and item canvas alpha.
Hidden, pooled and collected loot cannot fall through as generic interactables.
NPC classification remains owned by the existing NPC probe. Current selection
comes from `EpochInputManager.InteractableTargeting.ActiveTarget`.

Ranges come directly from each listener's `interactionRange`, and `CanInteract()`
controls availability. No extra guessed pickup radius or terrain occlusion test
is applied: a chest/door center may lie outside walkable NavMesh. Auto-pickup can
remove gold before a cue is heard; how much warning this actual range provides,
and whether game eligibility rejects every object behind a wall, remain runtime
checks. Menus, dialogue/tutorials, inactive/dead players, unavailable objects,
loading and lost focus suppress world cues without reopening devices.

`WorldAudioReservation` coordinates terrain, NPC, world-object and [enemy](enemy-cues.md) outputs using
FIFO requests and cue duration plus configured device buffering. A repeating NPC
cannot monopolize the channel. Each output retains the existing managed-provider
silence and event-confirmed recovery; no new Stop/Play cycle was introduced.
These reservations include queued device audio but cannot prove audible timing
under device failure or unusual scheduling delays.

Source-linked checks on 2026-10-01: navigation **357**, world cues **70**, NPC
lifecycle **109**, NPC probe **44**, NPC waveform/cadence **59**, real NAudio
adapter **44**. They cover sampled-origin consistency, gameplay/death gating,
category/visibility/ownership filtering, target deduplication, fair scheduling,
waveforms and device lifetime. They open no physical audio devices and do not
replace an in-game listening test of the new Steam build.

## Precision review and proposals (2026-10-03)

Code and log review only; nothing below is implemented or heard in game. The user
asked how precise the radar is and what would let them play by sound and speech
without pressing keys.

Live values from the 21:25 log: body radius 0.50, agent radius 0.10, close 2.33,
open 3.33, reach 6.67 (world units).

**Reliable, low resolution.** The NavMesh probe catches cliffs/void as well as walls,
and confirmation, hysteresis and wall memory avoid false cues. The limits are:

1. Four screen cardinals, one ray each: diagonal passages and corners between
   rays are never heard.
2. Binary state: a knock means "within 2.33", not how near or whether closing.
3. Transition-only: standing still (after loading or a fight) says nothing.
4. No movement direction: the mod reads no player velocity or stick intent anywhere.
5. Latency: `WorldAudioReservation` is one FIFO turn across terrain, NPC, object,
   guidance and enemy cues, each holding it for cue length plus device buffer.
   Enemy growls can delay a wall knock past impact.
6. Up and down knocks are both centred; down is only 0.86x pitch.
7. Wall, edge and door share one knock.

**Proposals, in recommended order (all passive):**

1. Bump/scrape: stick intent `MovingPlayerThumbstick.input` /
   `WorldMovementDir` (`last epoch code/MovingPlayerThumbstick.cs:144`, `:198`)
   against actual motion of `ClientMovingPlayer.myAgent` (`ClientMovingPlayer.cs:97`).
   Pushing without advancing → bump; advancing at an angle → scrape panned to the
   wall side. Ignore dashes/teleports (`CollisionAndNavMeshToggler` disables the
   agent). Stubs carry no logic: confirm live that velocity drops at walls.
2. Parking sensor along the movement direction: ticks accelerating with distance.
3. Terrain priority: an obstacle in the movement direction must not wait behind
   other world cues.
4. Distinct up/down (and front/back for guidance and enemies) by timbre, not
   only pitch; stereo cannot separate front from back.
5. Eight sectors instead of four.
6. Wall vs edge: physics check at the NavMesh hit; collider → knock, none → hollow
   edge sound. `NavMesh.FindClosestEdge` (unused) gives nearest boundary and normal.
7. Dead end / junction events, spoken or sounded once.
8. Guidance on-course feedback while moving; the far pulse (3 s) is too slow to steer.
9. Explored regions: `MinimapVolume.hasBeenTriggered` and `col`
   (`last epoch code/MinimapVolume.cs:38`, `:23`); coverage across zones unverified.
10. Shrines: `ShrinesManager.allShrines` / `_allShrineObjects`
    (`ShrinesManager.cs:1038`, `:1053`), unused by the mod.
11. Attack telegraphs: no class with position, radius and impact time was found in
    the stubs; needs a live discovery pass ([hazard sources](hazard-sources.md)).

## Latency and the unexplored mark (2026-10-04)

The user asked to "improve the radar's precision a bit". A comparison with the current D2R
radar found the confirmation, hysteresis, wall memory and queue already ported; the difference
that remained was delivery. D2R gives the radar its own output, while here
`WorldAudioReservation` made terrain cues take a FIFO turn behind NPC taps, enemy growls,
object cues and beacon pings, each holding the turn for its length plus the device buffer, so a
knock could arrive after the wall. Rejected D2R experiments (eight rays, diagonal frames,
speed-scaled depths, playback forecasts, new cue sounds) were not revisited.

- **Terrain first.** A terrain cue never waits for other world sounds and plays over one already
  sounding; the other sources still wait for the terrain cue and its buffer. The radar's own
  four directions stay serialized with the 50 ms gap. The arbitration checks in the world-cue
  (198) and enemy-cue (143) suites were rewritten for this rule.
- **Telemetry.** The five-second status adds `max_wait_ms`: the longest time a cue waited from
  becoming pending to playback in that window (radar serialization included).
- **Unexplored mark** (D2R `docs/exploration.md`, user request there of 2026-09-21): an opening
  breath toward a screen quadrant (dominant screen axis of the offset) holding an unexplored area
  the scanner lists, within twice the explored radius (`[Exploration beacon] ExploreRadius`, 40
  by default), ends in two 900 Hz pips (35 ms, 30 ms apart) after a 30 ms gap, lowered for
  screen-down like the cue (`radar.h`, `radar_synth.cpp`). Walls never carry it. Unlike D2R the
  mark is not part of the radar state: it never makes a cue of its own, so it adds no events. The
  scanner republishes areas every 0.5 s; older than 2 s they are ignored. `[Reactive radar]
  UnexploredMark` (default true) turns it off. Log: `Reactive radar cue: ... unexplored`.

Navigation suite: 504 checks (57 new: quadrants, camera turn, range, staleness, mark sound,
unchanged breath, walls unmarked). Not heard in game: check knock timing near NPCs and in fights
(`max_wait_ms` should stay near the radar's own cue spacing) and whether the pips help or clutter.

## Passages and wall following (2026-10-08)

Superseded on 2026-10-09 by "Dynamic openings" below; kept as the record of the evidence.

User report: stepping a little away from a wall made the radar say "open" again, so the user walked back to the
same spot looking for an exit that was not there. Today's log: of about 3,340 cues that turned a direction from
Blocked to Open, 3,031 came within 20 log lines (one or two seconds) of the Blocked cue in that direction; 243
`InvalidInput` resets also wiped the wall memory.

Causes. (1) The wall memory was one projection along the probe axis, taken only when the wall was confirmed, and
expired 6 m (D2 reach) sideways from that point: after following a wall the user had left its neighbourhood.
(2) Last Epoch's camera sees most walls diagonally, so every sidestep along a wall moved its intercept on the axis
farther, past the 0.45 m margin. (3) "Open" meant 3 m of walkable ground (D2's 10-subtile opening), so a wall a
little farther away, a recess or an alcove already counted. (4) A single non-finite frame reset the engine.

Changes (`RadarEngine`, `RadarSettings`):
- An opening is a passage: `PassageDepthInBodyWidths` defaults to D2's reach ratio (20/3 body widths, 6 m for the
  0.9 m body); the knock keeps D2's closing distance (7/3 widths, 2.1 m). A new key, so configs saved with the old
  default do not keep the old value. The probe now reaches twice the passage depth (12 m), so a wall's real end can
  be told from the same wall seen farther away; the wall neighbourhood and teleport distance keep D2's reach (6 m),
  and so does the camera-drift tolerance.
- While a direction is Blocked and still hits within the closing band, the remembered wall follows the latest hit,
  so walking along a wall and then stepping away from it is a retreat, not an opening. The retreat margin is half the
  hysteresis band ((6 − 2.1) / 2 = 1.95 m), which absorbs a diagonal wall's growth during confirmation.
- After a silent retreat the same wall is still followed while the direction is Open; when the probe reaches past its
  line by the margin on two consecutive polls (the wall ended) the opening sounds once.
- A non-finite position, time or camera axis skips the poll (queued cues cancelled) instead of resetting; a run
  longer than the 250 ms poll gap ends in the usual silent baseline.
- The status line adds `invalid=` (skipped polls) and `retreats=` (openings dropped as the same wall) per window.

Tests: Navigation 821 checks (a 45° wall slid along for 20 units, straight retreat to beyond the probe, a long wall
followed then left, a doorway after a long wall, a wall's real end, invalid frames). Mechanics tests keep the former
10/3-width depth (open 10, close 7, reach 20) through `RadarRun.LegacyOpenDepthInBodyWidths`. Listening pending:
walking along and away from walls (no breath), side doorways and corridor ends (breath), the wall-end breath, and how
much rarer "open" became; compare `retreats=` with the cues heard.

## Dynamic openings (2026-10-09)

The fixed 6 m passage above (same night) was too strict for very narrow or quickly bending passages, at the user's
report. It is replaced by two dynamic tests; the 6 m depth (D2 reach) remains only as the depth that opens by itself.
- **Edge rule (engine).** An opening counts only when the probe slipped past a wall's edge: its tip (position along
  the axis plus depth) jumps away by at least one body width (`RadarSettings.EdgeJump`) between two 50 ms polls while
  the direction is Blocked; a jump back toward the player (an edge closing) cancels it. Backing away and sliding
  along a diagonal wall move the tip gradually, so they commit Open silently (`retreats=`). After such a silent
  retreat the same wall is followed for D2's reach; an edge jump held for two polls (the wall really ended) sounds open.
  This replaces the projection memory and its margin.
- **Continuation (probe).** A depth from the D2 open depth (10/3 widths, 3 m) up to the passage depth needs the space
  to go on: from one body width short of the far wall, four branches at ±45° and ±90° are cast for one open depth; any
  clear branch means a passage (also narrow, bending or diagonal ones), none means a niche. Passage-deep or unblocked
  probes skip it. `RadarReading.Continues` carries the result.
- `OpenDepthInBodyWidths` is the setting again (D2 default); the probe reaches twice the passage depth (12 m).

Tests: Navigation 834 checks (edge vs. gradual, niche vs. passage, passage depth without continuation, the probe's
branch rays, plus the diagonal, retreat, doorway and wall-end cases above). Listening pending: narrow side passages
and bends should breathe, niches and receding walls should not; report doorways that stay silent.

## Wall bump (2026-10-11)

User request: know when walking into a wall stops the character, online and offline; the game keeps the run
animation and its footsteps while the stick pushes into a wall, and in some places the footsteps are very quiet.

- **Push.** `MovementIntent` reads the game's own thumbstick state, the one that walks the player: online
  `LocalPlayer.clientStateController.currentState` as `ClientThumbstickMovingPlayer`, offline
  `Actor.stateController.currentState` as `MovingPlayerThumbstick` (client first, then actor; the log names the
  source once). `WorldMovementDir`, or `input` on the camera's ground axes when it is zero. Any other state (an ability,
  a stun, standing) is no push.
- **Stall and contact** (`WallBumpDetector`, pure). Stalled: the player has not moved the radar's confirmation
  distance (body width / 3) within its stopped time (200 ms), i.e. under about 1.5 m/s for a 0.9 m body, so sliding
  along a wall is still walking. Contact: a NavMesh ray from the radar's sampled origin along the push hits within the
  body radius (`RadarWorldProbe.Clearance`). A stall without contact (an enemy, a root) stays silent and logs
  `[Wall bump] stopped without a wall within …` once.
- **Bump.** Once per contact: again after moving one confirmation step, releasing the stick, or turning the push by
  45° or more (half a radar sector: the other side of a corner), never twice within the radar's opening interval
  (0.5 s). A poll gap over 250 ms (menu, loading) starts over quietly.
- **Sound** (`BumpCueSynthesizer`): 90 ms thud, 150→70 Hz body with a muffled noise click, panned by the push's screen
  side, own NAudio output (master volume applies), never queued behind radar or world cues. Config `[Reactive radar]
  WallBump` (on) and `WallBumpVolume`; glossary entry `WallBump`.
- **Footsteps** (`FootstepMute`, `[Reactive radar] MuteFootstepsAtWalls`, on). While pressing (stalled with contact)
  each `FootstepRaycastTrigger` under the local visuals has its `emitter` set to null and restored when the push ends,
  on suspension and on teardown (only if the game has not assigned one since); re-applied every poll, so a form change
  is covered. No hook. Native evidence (build `GameAssembly.dll` SHA-256 `d5f9fb45…6567`, Capstone, RVAs from
  `MethodAddressToToken.db`): `AnimatorMessagePropagator.Step` 0x15A4D90 calls each trigger's `Step` with no `enabled`
  check; `FootstepRaycastTrigger.Step` 0x15CA6D0 returns when `HandleStepRaycast` (0x15C9EE0) misses, then calls
  `emitter.FootstepTrigger` only when `emitter` is non-null (test 0x15CA922, call 0x15CA99C); surface and boots/armour
  sounds (`PlayFootstepSound` 0x15CB4D0, `TryPlayExtraSound` 0x15CB770/0x15D17A0) are all behind that call; footprint
  visuals (`actorVisuals.Step`) still run. `FootstepRuntimePlayerLinker.Awake` (0x15CAA30) is the only runtime writer of
  `emitter`. Rejected: disabling components (never checked), `raycastDistance` 0 (a zero-length cast from inside a
  collider may still hit), the emitter's override fields (the game rewrites them on area overrides and scene loads).
  Side effect if a trigger starts or is destroyed while detached: the game logs "No Footstep Sound Emitter assigned!"
  and skips its override-event subscription. Unverified: whether the player also carries a collider-driven
  `FootstepTrigger` (0x15CB9C0, own `emitter`); the log line `footsteps silenced: N foot triggers, M collider triggers`
  shows it. Fallback if needed: a prefix on `FootstepRaycastTrigger.Step`, whose first 16 bytes hold no relative branch.
- Log: `[Wall bump] push read from the Client|Actor state`, `[Wall bump] bump pan=… clearance=…`,
  `[Wall bump] footsteps silenced: …`.

Tests: Navigation 877 checks (walking up to a wall, stopping, sliding, held contact, corner turn, release and poll
gaps, the clearance ray, the sound). Not heard: check online and offline that a straight push into a wall thuds once,
sliding along it does not, an enemy blocking the way does not, how late the thud feels (stopped time plus buffer),
and that footsteps stop while pushing and come back on the first real step (offline and online, after a form change).

## Review of outside proposals and the 2026-10-10 log (2026-10-11)

The user felt navigation was sometimes inconsistent and brought four suggestions from another assistant: a fan of
about 7 rays per direction (±30°) merged into one reading; open only when a body-wide, connected gap exists; follow
the boundary instead of depth jumps; optional physics checks for obstacles the NavMesh lacks. Nothing below is
implemented.

Log of 2026-10-10 evening (66 active minutes, times interpolated from Unity's timestamped lines, so ±0.3 s):
2,726 cues (~41/min), 55 % knocks. 40 % of breaths were followed by a knock in the same direction within 1.5 s;
224 open/blocked/open (or reverse) triples within 2 s, 32 within 1 s, runs of 4-6 alternations, Left worst. Not all
are wrong: passing a doorway is a legitimate breath then knock. Delivery is fine (`max_wait_ms` median 0, p90 183,
max 367), resets are only `Initial` (66) and `PollGap` (2), no audio faults. Heartbeat depths: 217 of 780 windows
have at least one direction under 0.1 m and 169 have two or more (`12,12,0,0`; `0,12,0,12`): the player is pressed
against a wall or corner (two zeros at once is a diagonal wall under the isometric camera). A single ray that starts on
the NavMesh edge and runs along an irregular wall is the likeliest source of the flips; cue lines carry no depth, so
this is inferred.

Assessment:
1. **Several rays per direction**: the useful part. One ray samples a line, not the body. But an angular fan from one
   origin widens each direction into the diagonals and, from a point on the wall's edge, every ray grazes the same
   wall. Better: three parallel rays across the body width, an origin moved off the edge when pressed against it, and
   a state change only when most rays agree. D2R's eight-ray and diagonal experiments were rejected by ear; this keeps
   four directions.
2. **Width and continuity**: mostly present. The NavMesh is baked for the agent's footprint, so a clear ray is
   passable; the continuation branches (2026-10-09) already reject niches. Requiring several adjacent rays clear is
   the new part, and it is item 1.
3. **Boundary following**: present in reduced form (edge rule, wall following after a silent retreat). A wall that
   curves away gradually stays silent on purpose: the 2026-10-08 report was false "open" on stepping away from a wall.
   Changing it risks that regression.
4. **Physics checks**: not advised. The player walks on the NavMesh; decoration, enemies and effects have colliders
   that do not block, so false walls would follow. NavMesh obstacles the game does use: `ActorVisuals.obstacle`
   (actors) and `DisableNavMeshObstacleInteraction.navMeshObstacle` (interactable barriers). Whether these carve the
   NavMesh is unverified; a closed barrier that does not carve would be the one real gap, checkable without physics.

Proposed next step if the user agrees: depth and agreement in each cue's log line first, then the three-ray reading.


## Radar trace (2026-10-11)

The user asked for a better diagnosis before any radar change: play a while, then change what the log shows. Added
lines (numbers invariant, `t=` is `Time.unscaledTimeAsDouble`, the radar's own clock):

- `Reactive radar event: t= dir= state= [flip=] pos=x,z edge= depth=… cont=…` for every generated cue: the last 8
  polls (0.4 s) of that direction, oldest first, depth and continuation flags (`-` = unknown). Until 11:04 the line
  also carried the shadow readings (`shadow=`, then `origin=` and `lanes=`). `flip=` gives the seconds since the opposite cue in the same direction when under 1.5 s.
  `edge=` is the distance from the origin to the nearest NavMesh edge (`NavMesh.FindClosestEdgeFilter`).
- `Reactive radar cue: t= direction= state=` as before, with the time: what was played.
- The five-second status ends with `edge= trace: events= flips= pressed_polls=a/b` (polls with the origin within the
  agent radius of an edge; the edge query is `NavMesh.FindClosestEdgeFilter`, one per poll).

**Shadow readings (removed 11:04, see the second trace)**: `origin` is the live ray from an origin moved off a
touching wall; `lanes` adds two parallel lanes (since 09:49; at 09:30 only `lanes` existed, logged as `shadow=`). The
live rays, but from an origin moved off a touching wall (to one body radius from the nearest edge, away from it; on the edge itself, away from the live rays that read
under a body radius), three parallel lanes per direction across the body width (centre, ±body radius; a lane whose
start lies beyond a wall reads 0), merged by their median, depths measured along the axis from the player's origin,
continuation checked at the median lane's hit. One `RadarEngine` per variant runs with the same settings. About 26
extra NavMesh queries per 50 ms poll. Remove it, or make it the live reading, once a session's trace has been
analysed.

How to read a session: flips with `edge` near 0 and a `depth` series jumping between near 0 and far point to the
grazing ray; dips below the 2.1 m knock for a few polls while `edge` is large point to small obstacles; values
hovering around 2.1 or 3.0 to threshold chatter; a steady depth between 3 and 6 m with `cont` toggling to the
continuation branches. Compare `live_flips` with `shadow_flips`, and check that the shadow still breathes at the
doorways the live reading found.

Tests: Navigation 895 checks after the shadow's removal (trace window and flips, edge distance, heartbeat and event
lines).

### First trace (2026-10-11 09:45)

The user tested other changes and walked little: 14 live events, 3 live flips, 12 shadow (`lanes`) events, 1 shadow
flip, 145 of 1,425 polls pressed against an edge. Two of the three live flips are one grazing episode: Left at
t=181.04-181.39 with `edge` 0.00-0.16 read `11.61,0.58,0.03,0.00,10.87,10.92,10.79,0.75,0.35,0.06` (Blocked, Open
0.17 s later, Blocked 0.18 s later) while the shadow series fell smoothly `11.62 … 9.25`. The third flip (Up, 0.95 s)
was shared by the shadow, at a wall approached head-on (depth falling 4.9 → 1.5). The shadow also kept the real
opening at t=175 (Up, 0.04 → 12 m) and the blocks at walls. Too few events to change the live reading: the shadow
was split to learn whether moving the origin alone suffices (the three lanes may hide passages narrower than two body
radii, the user's 2026-10-09 concern).
Wall bump in the same log: `push read from the Client state`, three bumps (pan 0.99, -0.95, -0.23), each with
`footsteps silenced: 1 foot triggers, 0 collider triggers`; the user heard the bump and the footsteps stop.

### Second trace and decision (2026-10-11 11:00)

The user played a little longer (online again) and reports the bump and the silent footsteps "help a lot". 3.75
active minutes: 235 live events (63 a minute), 95 flips, 27 % of polls pressed against an edge, running at about
6 m/s (median between events under 0.5 s apart). Flips by variant: live 95, `origin` 90, `lanes` 91; `origin`
reproduced 81 of the 95 live flips, `lanes` 75.

What the flips are: the player covered a median 2.4 m between the two cues of a flip (0.6 s). Breath after a knock
(58): the obstacle passed was a median 2.3 m long (17 under 1.35 m); knock after a breath (38): the gap was a median
3.1 m long (8 under 1.35 m). These are real pillars, props, wall segments and gaps passed at running speed, which the
radar is meant to report. 34 flips involve a ray grazing the wall the player touched (`edge` under 0.15 and the depth
jumping between under 0.3 and over 3 m); `origin` removed 12 of them but also lost real openings (14 of 101 live
openings unmatched, e.g. the right wall ending at t=93.91, because the nearest edge pushed the origin toward another
wall); `lanes` lost 20 of 101.

Decision: no change to the live reading. Neither variant removes most flips, and both lose real openings; the
grazing artifact now matters less because the bump tells a player who is against a wall. The shadow and its second
engine are removed (≈26 NavMesh queries per poll saved); the event lines, flip marks and edge distance stay. If the
radar feels too busy, the data supports one optional filter to discuss with the user: a breath only after the
opening has lasted 1.5 body widths (1.35 m, ≈0.2 s at running speed), which would drop the 8 short gaps and delay
every breath by that distance; knocks would stay immediate.

## Wall tones (2026-10-11)

User request: an option in the mod menu to use wall tones instead of (or with) the radar, working like Grimdark
(`https://github.com/ahicks92/grimdark`, `src/screens/in_game.cpp`, `src/audio.cpp`), but softer while the distance
still sounds clearly different. The existing `[Navigation audio] ContinuousWallSonar` (default off) and `SonarVolume`
keep their keys; the old tones (500/180/300 Hz, the game's `NavMeshHelper` from the raw player position, offline
`PlayerFinder` only, a device always open) are replaced.

- **Grimdark's design** (read from its source): four always-running loops on screen axes (its camera yaw is fixed),
  ahead ≈1265 Hz centre, sides ≈856 Hz hard left/right (equal-power pan), behind ≈432 Hz centre +3 dB; distance only
  in volume, `(1 - d/range)²`, silent at the range (10 units there); updated every frame; loops about -20 dBFS RMS;
  its probe simulates the game's own walking, earlier the farthest of several parallel NavMesh lanes.
- **Here** (`WallToneSynth`, `WallSonar`): the same pitches, pans, trim and curve; the range is the radar's probe reach
  (12 m for a 0.9 m body); each direction takes the farthest of three lanes (centre and one body radius to each side,
  `RadarWorldProbe.LaneDepth`; a lane whose start lies beyond a wall is skipped), so a wall brushed or a corner beside
  the way does not sound like a wall ahead. Polled at the radar's 50 ms with a 25 ms glide per sample (no clicks), on
  the radar's validated origin, camera axes and NavMesh filter, online and offline. Softer: 0.07 peak at a touching
  wall, about 6 dB under Grimdark's loops; a light second and third harmonic keep soft tones audible. Silent in menus,
  pauses, conversations and unfocused, and when the player or NavMesh is unavailable; the output opens only once the
  tones are first enabled and retries every 5 s.
- Mod menu: the page and option read "Wall tones"; the details say they are an alternative or companion and that the
  radar can be turned off on its own page. Glossary previews play each direction at a touching wall.
- Log: `[Wall tones] playing`, `[Wall tones] silent: <reason>`, `[Wall tones] audio unavailable: …`.

Tests: RadarAudio 71 (curve, pitch order, pans, trim, glide in and out, levels, preview), Navigation 901 (lanes).
Not heard: the level against game audio, and whether the high tone ahead is pleasant at length.

## Warm wall sounds (2026-10-11)

User report: the wall tones were "very, very strident, ultra annoying"; softer and warmer, not necessarily tones,
like the Grim Dawn mod. Grimdark's loops (both banks in `assets/audio/walltones/`, from wotr-access, same author) were
measured: none is a tone. Each is mono band-limited noise, hundreds of partials across a band, a Rayleigh envelope
(20 ms loudness spread 0.14-0.24), about -20 to -23 dBFS RMS:

| Bank | Ahead | Sides | Behind | Note |
|---|---|---|---|---|
| 1 (warm) | 780-1015 Hz, centroid 945 | 430-650 Hz, 528 | 160-340 Hz, 266 | Grimdark keeps it vendored, unused |
| 2 (bright) | 860-1380 Hz, 1237 | 620-970 Hz, 840 | 300-550 Hz, 428 | Grimdark's live bank |

Our 2026-10-11 11:18 version took bank 2's pitches as sines with a second and third harmonic (1265 Hz plus 2.5 and
3.8 kHz), which is where the harshness came from. The loops themselves were not copied: wotr-access's licence is not
published.

- **Now** (`WallToneSynth`): per direction, deterministic white noise through two RBJ band-pass stages (Q 1.8, a band
  about half an octave wide) and a one-pole low-pass at 1.8 kHz, scaled to unit RMS from the chain's impulse energy.
  Centres 800 / 450 / 250 Hz (bank 1 lowered about 15 percent, as the user asked for warmer); behind +3 dB as in
  Grimdark; equal-power pans, distance curve, lanes, poll and 25 ms glide unchanged.
- Measured from the synth: centroids 845 / 483 / 271 Hz, 84 percent of the ahead band's energy within 500-1000 Hz,
  nothing above 2 kHz, loudness spread 0.16-0.25 (as bank 1). 0.04 RMS at a touching wall (about -28 dBFS, the old
  tones' RMS); with no energy at 2.5-4 kHz it sounds much softer at the same RMS.
- Descriptions in the ten catalogs say "a soft rush of air" (`rumor de aire`), not "tone"; the option keeps its name.

Tests: RadarAudio 73 (contact level, pans, band order, ahead below 1.2 kHz, loudness spread, glide, levels, preview).
Not heard: whether the level against game audio and the 250 Hz band behind suit the user.
