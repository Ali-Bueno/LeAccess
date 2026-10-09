# Reactive navigation radar

LeAccess adapts the reactive radar in `D:\code\c++\diablo 2 access project\d2r-access`.
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
| Reactive radar | Volume | 100 | Percentage of the original radar cue level |
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
