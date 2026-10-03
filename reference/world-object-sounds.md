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
  `[World object sounds] LandmarkRange` metres (default 12, 0-30; 0 restores the
  native radius). Volume is full inside the native radius and fades linearly to
  `FarGain` (35 percent) at the edge. Native `CanInteract()` reads no position,
  so a distant exit still passes it.
- Other interactables keep the native radius; a nearer one inside its radius
  still takes the category's turn.
- A target that was sounding keeps its turn `ExitMargin` (0.5 m) past its edge;
  re-entry needs the real edge, so the boundary cannot chop it.

Identity is read only for interactables between the native and landmark radii.
The world-cue suite passes 169 checks; listening is pending.

## Continuous sonar lifecycle follow-up

The separate optional continuous wall sonar now yields whenever native gameplay
input ownership is lost, including an inventory/menu opened without pausing an
online game. A failed native player query also clears the prior looping tones
instead of retaining their last volumes. New valid gameplay probes restore the
sound after either condition. The source-linked `LeAccess.RadarAudio.Tests`
suite passes **48 checks**, including both reproduced failures and recovery;
its sonar cases inject channels without opening an audio device. Continuous
sonar remains opt-in and its existing sounds are unchanged.
