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
