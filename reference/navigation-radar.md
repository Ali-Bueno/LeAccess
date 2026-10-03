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
| Reactive radar | OpenDepthInBodyWidths | 3.3333333 | Straight walkable depth needed to confirm an opening |
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

`RadarWorldProbe` uses `PlayerFinder.player`, the local player's
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

`WorldAudioReservation` coordinates terrain, NPC and world-object outputs using
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
