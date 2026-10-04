# Enemy cues

Diablo 1-style presence sounds for nearby enemies, so a ranged player can tell
where enemies are before attacking. Ported from the same author's D2R mod
(`D:\code\c++\diablo 2 access project\d2r-access`: `docs/entity-cues.md`,
`src/entity_cues.*`, `src/entity_cue_synth.cpp`, `src/entity_cue_service.cpp`).
There, the user and a friend asked for these sounds after the
[diablo-access](https://github.com/mojsior/diablo-access) model: one shared
monster sound, the three nearest emitters, faster when closer. Enabled by default.

## What the player hears

| Target | Sound | Streams |
|---|---|---|
| Boss or miniboss | Deep resonant falling swoop | One: the nearest boss in sight |
| Any other enemy | Short rough growl | The nearest ordinary enemies in sight, in the streams left |

- At most three enemy streams. Several bosses make one stream; a pack is heard as
  its three nearest members, each on its own, so its spread is audible.
- Pulses repeat faster when closer: the interval shrinks linearly from 450 ms at
  the range edge to 150 ms at contact. Distance changes tempo, not volume.
- Stereo gives screen left/right; screen-down lowers the pitch (to 80 percent).
  The axes are the gameplay camera's, as for the radar, NPC and world sounds.
- Each stream (the boss, nearest, second, third) keeps its own cadence, so the
  nearest enemy changing does not burst. The next pulse uses the newly selected
  enemy's position.

## Eligibility

`EnemyCueProbe` reuses `EnemyScanProbe.TryRead` (names skipped) over
`HostileRegistry`: living foes in the current online/offline registry within
`ControllerTargetingParadigm._SkillTargetMaxDistance` (3D), the same range as the
nearest-enemy announcement and the [enemy scan](enemy-scan.md). Bosses use offline
`Actor.isBossOrMiniboss()` and online `ActorVisuals.Data.isBossOrMiniBoss()`.

Terrain sight is `RadarWorldProbe.Sight`, from a radar probe of its own captured
without the four wall rays (`probeWalls: false`; player, camera axes and the
sampled NavMesh origin are still validated). The enemy position is sampled onto
the player's NavMesh (`SamplePositionFilter`, player agent type and area mask)
within the baked build radius (`NavMesh.GetSettingsByID(type).agentRadius`, at
least the runtime agent radius, which online is a narrow helper) plus the baked
step height `agentClimb` for the surface's vertical offset. The baked edge lies
one build radius inside walls, so an enemy flush against a wall still samples.
`RaycastFilter` from the origin to that sampled point gives Clear or Blocked; a
target that cannot be sampled is OffMesh. Only the nearest candidates pay for a
query (at most the needed three plus any hidden nearer ones).

Policy (`EnemyCueSelector.InSight`): Clear is audible and Blocked never is. An
OffMesh boss stays audible, an OffMesh ordinary enemy does not: an arena boss in
water or on a ledge is still within targeting range and the player can hit it,
and there is a single boss stream, while ordinary off-mesh enemies are often in
unreachable places. This holds even when `Bosses = false` makes the boss growl.

Limits: this is walkable-surface sight, as in D2R. Walk-blocking but see-through
obstacles (fences, chasms, water) hide an ordinary enemy standing on a walkable
surface behind them, and ordinary enemies off the player's NavMesh are silent.
Bearings are direct, not routes.

## Timing and shared audio

- Snapshots (registry walk, rays, selection) run on the main thread every 100 ms
  (`EntityCueCadence.PollSeconds`, D2R `PollMs`).
- Starts are checked every frame against the latest snapshot, so a shared-channel
  turn is not lost to polling. `EnemyCueTracker` plays first cues before repeats;
  among repeats the most overdue fraction wins, so a nearby boss cannot starve the
  others. Enemy starts are at least 100 ms apart (D2R `gapMs`).
- A stream that leaves the selection keeps its cadence; only after an absence of
  the far interval (450 ms) is it new again with first-cue priority. A one-poll
  dropout (a ray flicker, a stepping enemy) therefore cannot jump the queue.
- `WorldAudioReservation` source `Enemy` joins the FIFO shared by radar knocks and
  breaths, NPC taps, world-object sounds and automatic orientation. Each enemy cue
  reserves its 90 ms plus the 100 ms device buffer; it waits rather than overlap,
  and a repeating enemy cannot take the next turn from a waiting source.
- Heard rates: an isolated enemy about every 190 ms nearby and 450 ms at the edge.
  Three nearby enemies with nothing else playing take turns about every 190 ms
  (reservation plus frame), so each repeats about every 570 ms. D2R quoted 200/500
  ms and 300 ms from its separate device and poll-quantized starts.
- Output: its own `RadarAudioOutput("Enemy cues", 100 ms)`, as NPC and world cues
  have, with the same 5 s event-confirmed recovery. It was 50 ms until a live log
  showed 1721 callback stalls in 7740 reads, 159 of them inside the 174 cues played
  (the user heard a "mini sputtering"); the 100 ms radar output logged none (max
  callback gap 67 ms). NPC cues moved from 50 to 100 ms for the same reason. Samples are synthesized on the
  main thread at play time like the other cues; the NAudio callback reads only the
  float buffer, never Unity or IL2CPP objects.

## Suspension and reset

Disabled, volume 0, both classes off, `NavigationAudioState` suspension (focus,
pause, loading, conversation), a non-gameplay input owner (menus), screen content
or dialogue: the current cue is silenced, the pending turn cancelled, cadence
reset and the probe cache dropped. A failed capture (player, camera, NavMesh,
range or registry unavailable) only silences and cancels the pending turn; the
cadence survives so a brief failure in the same session does not make every
stream fire back-to-back on recovery. A radar session change (scene or player)
silences and resets cadence. Registry warnings are de-duplicated until a poll
reports none; exception warnings until a cue plays again. There is no queue: each start comes from the
latest snapshot, and dead or unloaded enemies vanish at the next one.

## Sound design

Both cues are 90 ms, 48 kHz stereo, synthesized in `EnemyCueSynthesizer` with no
assets. Placement (equal-power pan, 0.80 rear pitch) is the shared `CuePlacement`
also used by NPC, world and natural cues.

| Cue | D2R constants |
|---|---|
| Growl (2026-10-02) | 5 harmonics at 1/n, 175 to 125 Hz linear, 36 Hz loudness flutter at depth 0.55, sin^2 swell, level 0.36 |
| Boss swoop (2026-09-22) | Phase-modulated resonance, index 0.65 at ratio 1.61, 480 to 110 Hz quadratic fall, sin^2 swell, 12 % one-pole air at 1500 Hz (xorshift seed 0x6D2B79F5), level 0.29 |

D2R users confused an earlier noisy whoosh with the radar's breath; both enemy
cues are tonal and swell. Zero-crossing rates with the suite's helper (middle
third; first third for the decaying knock and tap): growl about 300/s, boss swoop
400/s; radar knock 500/s (percussive), radar breaths 2,983-6,600/s (filtered
noise), NPC tap 1,893/s.

## Configuration

| `[Enemy cues]` key | Default | Meaning |
|---|---|---|
| Enabled | true | Enemy cues on or off |
| Bosses | true | Bosses get the swoop; when off they growl like other enemies (D2R does the same for its disabled ranged class) |
| Ordinary | true | Growls for ordinary enemies; when off only the boss swoop remains |
| Volume | 100 | Percent of the D2R enemy cue level, like the other cue volumes |

## Code map

- `Combat/EnemyCueSelection.cs`: streams, ranks, slots, bearing (pure).
- `Combat/EnemyCueTracker.cs`: per-stream cadence, gap, fairness, reset (pure).
- `Combat/EnemyCueProbe.cs`: main-thread snapshot. `Combat/EnemyCueSounds.cs`: config, gating, playback.
- `Audio/EnemyCueSynthesizer.cs`; shared `Audio/EntityCueCadence.cs` (also `NpcCueTiming`) and `Audio/CuePlacement.cs`.
- `Navigation/RadarWorldProbe.Sight` and `TryCapture(probeWalls: false)`, `TerrainSight` (`RadarModel.cs`), `GuidanceGeometry.Bearing`.

Log: `[Enemy cues] <status>; in_range=N selected=Boss#id@dist/slot0,... played=N audio=...`
on each suspension change and every 5 s while active; warnings are deduplicated.

## Validation

`dotnet run --project tests/LeAccess.EnemyCues.Tests/LeAccess.EnemyCues.Tests.csproj`
passes 141 checks: cadence, bearing, stream selection and limits, lazy sight
tests, the off-mesh boss policy, range/invalid inputs, class toggles, tracker
cadence, gap, fairness, per-rank continuity across brief and long absences, resets, a 60 fps simulation through the shared reservation,
FIFO arbitration with radar/NPC sources, and synthesis (length, peak 0.046-0.427,
no NaN, silent edges, swell, falling pitch, flutter, rear pitch, distinctness,
equal-power mirrored panning, gain and invalid inputs). The navigation suite has
16 sight checks (463): wall-free capture, sampling radius from baked settings
over a narrow runtime agent, a grazing off-edge enemy, blocked, off-mesh and
reset cases. No game was launched and no audio device opened. The service's
capture-failure pause is not host-tested; the tracker behavior it relies on is.

## Live listening checks

1. An outdoor pack: three growls, placed left/right/behind, faster on approach,
   each stopping when that enemy dies.
2. A boss or miniboss: the swoop leads; compare it with the radar wall knock.
3. Enemies in another room behind a wall stay silent; an enemy walking along the
   same wall ahead stays audible; a boss in water or on a ledge sounds. Note
   ordinary enemies across water, chasms, fences or on ledges.
4. Walk so the nearest enemy changes repeatedly: no bursts.
5. Combat density alongside radar knocks/breaths, NPC taps and loot sounds: no
   overlap, no starved source, loudness against the game's combat audio.
6. Inventory, pause, map, dialogue, zone change and death: immediate silence and
   a clean resume.
7. `Bosses`, `Ordinary` and `Volume`; an online session as well as offline.
