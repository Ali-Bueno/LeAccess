# Recurring nearby world-object sounds

The October 2 offline log exposed two concrete failures. `WorldCueState` retained
each category's played target forever; its previous tests explicitly required
unchanged targets never to repeat. Gold registries also contained live piles
(`registry=2/0/1`, `unavailable=2`) rejected by the common click-listener gate.
Installed configuration had world sounds enabled at volume 100; this was not a
disabled setting or missing gold registry.

The scheduler now repeats while candidates remain in range. A global 0.45-second
minimum interval retains the existing D2R-derived burst bound. The least recently
played available category gets the next turn; preferred/nearer targets break
initial ties. Coins, equipment and interactables therefore cycle without a
persistent preferred pile starving the others. Existing stereo bearings, front/
rear pitch, category rhythms and shared audio-source reservation remain intact.
Pickup, range exit, scene loss and menu suspension stop pending target audio.
Playback uses the persistent output; repeated beeps do not emit per-beep logs.

Loot uses its native visual registry, current scene, active object, live data and
the game's `targetingConfiguration.itemsRange`. Gold requires a positive value.
Equipment still requires the authoritative label's filter outcome to differ from
`RuleOutcome.HIDE`. Neither requires an optional `WorldObjectClickListener` to be
present, enabled, or clickable. Ordinary interactables retain `CanInteract()`.

## Current installed native evidence

- `GroundGoldVisuals.initialise`, RVA `0x11D3670`, stores value/id/label and adds
  the visual to its native `all` registry. It does not initialize a click listener.
  `beforePooling`, RVA `0x11D35E0`, clears value/id/label and removes that entry.
- `GroundItemVisuals.initialise`, RVA `0x11EDFD0`, stores data/label and registers
  the visual. `beforePooling`, RVA `0x11EDE50`, clears those references. The
  source wrappers expose these fields in `last epoch code/GroundItemVisuals.cs`.
- `GroundItemLabel.ShowLabel`, RVA `0x11D5D40`, hides filtered labels when
  `lastFilterResult == HIDE`; its `useFilter=false` path can show them anyway.
  A spoken temporary label alone does not prove that its filter outcome is SHOW.
- `KeyBasedInteractableTargeting.UpdateTargets`, RVA `0x1890A70`, compares label
  world-position distance against the native item targeting range. No guessed
  scene-wide radius or object discovery scan was added.

The offline equipment log reported `filtered=1` alongside a spoken shield. The
old counter conflated missing data, non-equipment and missing labels with HIDE.
Diagnostics now separate `missingData`, `nonEquipment`, `missingLabel` and real
filter rejection; the exact cause requires the next listening log. Hidden loot
has not silently been made audible contrary to the game's filter.

The source-linked world-cue suite passes **112 checks**, including absent/disabled
loot listeners, four complete fair category cycles, current-scene ownership,
pickup with another category remaining, and bounded logging. The main no-deploy
build passes with zero warnings/errors. The game was not launched; audible loop
quality and the remaining equipment rejection require user testing.

For the next test, approach an unfiltered dropped item and gold without picking
them up; verify repetition and directional movement. Approach a dungeon entrance,
then leave its native interaction radius. Open a real menu and collect the loot;
both actions should stop that target's sound. Compare the bounded diagnostics if
a nearby visible item remains silent.

## Landmark reach (live report, 2026-10-03)

The user reported portal sounds that "stop or stutter". The 20:43 log shows the
camp waypoint (`sound=Portal`) at its native `interactionRange=2.00`: five cues,
then `outOfRange=3` once the player stepped away, and at the edge each poll
alternated in and out, silencing the cue in progress.

`WorldCueReach` follows D2RAccess `ground_item_service.cpp`, where exits,
waypoints and the stash are loop-only landmarks audible well beyond reach:

- Portals, waypoints, zone transitions and the stash sound up to
  `[World object sounds] LoopRange` metres (default 12, 0-30; 0 restores the
  native radius; first released as `LandmarkRange`). Volume is full inside the native radius and fades linearly to
  `FarGain` (35 percent) at the edge. Native `CanInteract()` reads no position,
  so a distant exit still passes it.
- Other interactables keep the native radius; a nearer one inside its radius
  still takes the category's turn.
- A target that was sounding keeps its turn `ExitMargin` (0.5 m) past its edge;
  re-entry needs the real edge, so the boundary cannot chop it.

Identity is read only for interactables between the native and landmark radii.
The world-cue suite passes 169 checks; listening is pending.

## Continuous loops (D2R mixer port, 2026-10-03)

Second live report: the loop sounds still "sputter, as if the audio were not
processed well". The log showed no device fault (`callback_stalls=0`, 70 ms
maximum gap); the sounds themselves were the cause. The import tool (now `tools/import_audio.py`)
had cut the first 0.24-0.3 s of D2R loop recordings that run 10-15 s and the
scheduler repeated that fragment with edge fades and gaps, interleaved with radar
and NPC cues through `WorldAudioReservation`.

The mixer is now ported from D2RAccess `item_audio.cpp` / `ground_items.cpp`:

- `Sounds/Loops/*.wav` hold the whole recordings (gold, normal equipment, chest,
  stash, portal, zone transition; 48 kHz mono PCM16, ~5 MB). `WorldLoopBank`
  peak-normalizes them to 0.9 with D2R's trims (chest +4 dB, stash -4 dB, stash
  padded to repeat once a second).
- `WorldLoopMixer` is one continuous `ISampleProvider` (`LoopAudioOutput`,
  100 ms `WaveOutEvent`). Each voice keeps its loop position across updates,
  interpolates for pitch, and glides gains/pitch with a 30 ms time constant
  (D2R: 0.25 smoothing per 20 ms, about 80 ms); absent voices fade out before
  release, so pickup or range exit never clicks. It does not use the
  reservation queue: loops are ambient, as in D2R.
- `WorldLoopSelection`: up to 8 voices, exits/waypoints/stash first, then at most
  3 chests, then the nearest loot. Gain `0.4 * (1 - d/range)^2`, summed scalar
  gain limited to 0.5, equal-power pan, pitch 0.94 blended across the first
  metre behind the player. `Volume` scales the result.
- `WorldCueProbe` sends every eligible object whose kind has a loaded loop to
  the mixer within `LoopRange`, and never also to the repeated cue. Unclassified
  interactables keep the native-range repeated cue; a kind whose loop failed to
  load falls back to its cue. `Loops=false` restores the old behaviour.

Potions and tomes (follow-up report "las pociones no sonaron"): they were never
read. They live in their own native registries like gold:
`GroundPotionVisuals.all`, `GroundXPTomeVisuals.all`, `GroundFavorTomeVisuals.all`
(`last epoch code/GroundPotionVisuals.cs` etc., each with `worldObjectClickListener`).
They now loop `loot/potion.ogg` and `loot/scroll.ogg` (the scroll click gets
D2R's 24 dB boost and 0.9 limiter at import) within `LoopRange`, sharing the loot
voices. They have no repeated-cue fallback: with `Loops=false` they stay silent,
as before. Their listeners are excluded from generic interactables.

Later on 2026-10-03 gold, potions, tomes, portals/waypoints and exits switched to
Last Epoch recordings chosen by the user ([sound design](sound-design.md#last-epoch-recordings-user-choice-2026-10-03)).
Every recording under a second now repeats once a second; the three one-shots
take the mix's full headroom. Equipment, chests and the stash keep D2R.

Tests: `LeAccess.WorldLoops.Tests` and world cues cover decoding, levels, seamless position, fades, release, clamping and pitch. Listening is pending.

## Loot by rarity and type, and more interactables (2026-10-08)

`GroundLootClassifier` maps the game's item predicates to a loop; the same mixer, range and loot filter as
equipment apply (a hidden label stays silent; a kind whose loop failed to load is silent, never a borrowed
equipment cue). Unknown interactables keep the repeated cue at the native radius: widening it would make every
door, lever and lore object beep from `LoopRange`.

| Game item or object | Loop | Source |
|---|---|---|
| Normal equipment | `equipment` | D2R `normal equipment` |
| Magic equipment | `magic_equipment` | D2R `magic equipment` |
| Rare or exalted equipment | `rare_equipment` | D2R `rare equipment` |
| Unique, set, legendary | `legendary_equipment` | D2R `legendary equipment` |
| Idols | by rarity, as equipment | |
| Rune | `rune` | D2R `rune` |
| Glyph | `gem` | D2R `gem` |
| Affix shard | `scroll` | D2R `scroll` |
| Key, lens, woven echo, other loot | `quest_item` | D2R `quest item` |
| Shrine (`Shrine`, `ShrineSync`) | `shrine` | LE `Environmental` `sfx_enviro_shrine_nature_main_loop` (28 s) |
| Nemesis (`NemesisEncounterInteractableObject`) | `nemesis` | LE `Environmental` `sfx_ui_nemesis_corpse_loop` (22 s) |
| Time rift, Monolith/Shattered Road portal (`TimeRiftInteraction`, `ActivateTimeRift`, `TimeBeastRift`, `MonolithPortal`, `ShatteredRoadPortalInteractable`) | `portal` | LE time-rift loop (unchanged) |
| One-shot cache | `chest` | D2R `chest` |
| Generated cave entrance/exits without a tag (`UsePlayerSpawnInteraction`, `CaveLobbyExitInteraction`) | `zone_transition` | exit loop; see [generated caves](exploration-mode.md#generated-caves-2026-10-09) |

`InteractableRoles.Read` finds these components on the listener or its ancestors. `Shrine` is the shrine's
activated effect and `ShrineSync` its network object, so neither need sit above the clickable listener; shrines are
also recognized by `ShrineVisualsCreator` above the listener and by the objects the scene's `ShrinesManager` placed
(`GetAllShrineObjects`, listener inside or above one, looked up again every 5 s). `ShrineObjects` holds that list for
sounds, landmarks and the scanner (before 2026-10-09 only the scanner had it).

**Voices per kind (2026-10-09).** In the loot pass of `WorldLoopSelection`, the nearest object of each loot kind takes
a voice before the rest fill by distance: in the 2026-10-09 log, 14-15 loop candidates competed for 8 voices and runes,
glyphs and shards got one only once they were the nearest objects. Shrines and the Nemesis are
landmarks (voices first, `LoopRange`). Names: `InteractableRoles.Name` takes the hover tag's text, resolved through
`Localization.GetText` when it has a `localizationKey`, and only then the mod's `exploration.shrine`,
`exploration.time_rift`, `exploration.nemesis`. The nearby-landmark announcement and the scanner use it, so key-only
tags (log: `LOCALIZATION_KEY` without a portal controller) are named. Unverified without the game: that the rift and
Nemesis components sit on or above the listener, and the speech text of their tags.

## Fallback cues are cut from the loops

The root `Sounds/*.wav` excerpts duplicated the loops. Only `discovery.wav` and `inside_unexplored_room.wav` (no
loop twin) remain; `NaturalCueBank` cuts the repeated-cue fallback from the first `GoldSeconds`, `EquipmentSeconds`,
`ContainerSeconds`, `TransitionSeconds` or `RouteSeconds` of the loop at load time (the old file lengths).

## Continuous sonar lifecycle follow-up

The separate optional continuous wall sonar now yields whenever native gameplay
input ownership is lost, including an inventory/menu opened without pausing an
online game. A failed native player query also clears the prior looping tones
instead of retaining their last volumes. New valid gameplay probes restore the
sound after either condition. The source-linked `LeAccess.RadarAudio.Tests`
suite passes **48 checks**, including both reproduced failures and recovery;
its sonar cases inject channels without opening an audio device. Continuous
sonar remains opt-in and its existing sounds are unchanged.

## Loop mixer accumulation (live reports, 2026-10-03)

Since the mixer port the user heard the loops "spotereado", then with the Last
Epoch portal loop "como si el sonido se reprodujese varias veces encima del
mismo", louder than the game and not clean like D2RAccess. The log always showed
one steady voice and the same file sounded clean outside the game.

Cause: `WorldLoopMixer.Read` zeroed its output with `Array.Clear`. NAudio's
`SampleToWaveProvider` passes a byte array viewed as `float[]` (`WaveBuffer`);
`Array.Clear` uses the runtime `byte[]` type and cleared only a quarter of the
request, so each fill summed the loop onto the audio left in the reused device
buffer. Copies stacked one buffer apart and grew until the full-scale clamp.
`RadarAudioOutput` already documented this trap; the port missed it. A loopback
recording of the real output on the user's PC showed RMS 0.17-0.48 rising for a
voice that should give 0.019; after the fix a 2 s window matches the loop at
exactly its 0.150 gain with -42 dB residual. The loop suite now drives the real
NAudio adapter with a stale buffer (it fails on the old clear).

Two changes made while chasing this were kept; both were verified in that
recording and follow D2RAccess:

- `LoopAudioOutput` opens event-driven shared-mode `WasapiOut` (NAudio.Wasapi,
  already shipped; D2RAccess's XAudio2 also renders through WASAPI), falling back
  to `WaveOutEvent`. The heartbeat reports `backend` and `max_read_gap_ms`
  (about 11 ms on the user's PC).
- Gains and pitch glide with a one-poll time constant (0.1 s) instead of 30 ms,
  so they keep moving between the 0.1 s target updates instead of stepping at
  10 Hz (D2RAccess updates every 20 ms with 0.25 smoothing).

A detour that kept one stereo channel of the Last Epoch loops was reverted: the
summed channels were never the cause.
