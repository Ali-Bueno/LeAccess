# Rage of Morditas (seasonal event)

Added 2026-10-19 for the tester report: "it doesn't say anything when I start the blood crystal thing, and I fail".
Code: `Accessibility/Morditas/` (`MorditasAccessibility` loop, `MorditasEventState` speech logic, `MorditasProbe`
reads, `BloodTrails`, `BloodTrailCue`, `MorditasSpeechOwner`), `Audio/BloodTrailSynthesizer.cs`,
`Patches/MorditasPatches.cs`. Related: [reward panel](endgame-panels.md#rage-of-morditas-reward),
[return arrow](hazard-sources.md), [scanner](exploration-mode.md), [dynamic objectives](dynamic-objectives.md),
[notification lifecycle](announcement-lifecycle.md).

Provenance: **V** = verified in the installed Steam 1.5 `GameAssembly.dll` disassembly (Capstone over the
`MethodAddressToToken.db` map) or in the game data (`PermaLoad.bundle`, UI string table); **I** = inferred.

## The mechanic

| Phase (`RageOfMorditasLogic.State`) | What happens |
|---|---|
| NotStarted | The Blood Crystal stands in the world; clicking it (`RageOfMorditasInteractableObject.OnClicked`) starts the hunt. No place to stand (V). |
| Hunt | Kill any hostile monster anywhere. Timer starts at `HuntInitialDuration` (25 s campaign, 20 s Monolith); kills add time (champion +10 s, diminishing) (V). Aggro and call-to-arms ranges grow (×3 campaign, ×5 Monolith) (V). Time passes faster as the hunt goes on (`HuntTimerEscalationCurve`) and 3× after the last charge (V). |
| Hunt ends | When time passed reaches the total (`FixedUpdate_Hunt` compares `+0x100` with `+0x104`) (V): fewer kills than the first charge threshold → Finished, failed; otherwise → PrepareGauntlet (V). |
| PrepareGauntlet | 3 s (`PreGauntletDuration`); Morditas appears (a visual, not an actor) (V). |
| Gauntlet | A 22 m circle (`GauntletAreaRadius`) centred where the hunt ended (`GauntletLocation`). Waves of "bloodcrafted" copies of the hunt's monster types, 4 per wave (6 in Monolith), spawn 3–14 m (15) from the centre (V). Total to defeat = the hunt's kill count: online clients get `OnGauntletKillCountUpdate(GauntletKillCount, HuntKillCount)` and the widget shows total − killed (V). |
| Leaving | Every player outside the circle for `GauntletOutOfZoneTimerDuration` (4 s) in a row → fail; stepping back resets (V). |
| ReadyForReward | All waves defeated; "Interact with the Blood Ice crystal" (the scanner lists the reward crystal, `WorldCues/InteractableRole.cs`). Then the reward panel (`Market/MorditasReader.cs`). |
| Finished | `Success` true or false. The owner dying or leaving with no other living player during Hunt, PrepareGauntlet or Gauntlet also fails (V). |

The centre crystal casts "Gauntlet Crystal Freeze Nova" every 10 s after a 1 s warning
(`ShowCrystalAbilityPrecastVisuals`); its target is unknown, so it is not announced.

Blood trails (V): every `BloodTrailFireInterval` (3 s) the server picks up to `MaxBloodTrails` (3) monsters between
`BloodTrailMinDistance` (10 m) and `BloodTrailMaxDistance` (300 m) from the player and sends their positions to that
player (`SendMorditasBloodTrails` → `ReceiveMorditasBloodTrails` → `HuntVisualController.HandleBloodTrails`). It is the
game's only "where to go" cue.

### Game data (PermaLoad.bundle, 2026-10-19)

`RageOfMorditasData` (`RageOfMorditasData.Instance`): `HuntKillThresholdsForBloodrageCharges` [20, 50, 90, 140, 200],
`HuntKillThresholdsForPlayerBuffs` [20, 50, 90, 140], `HuntRemainingDurationWarning` 15, `HuntRemainingDurationCritical` 5,
`HuntTimerForLowTimerVoiceLine` 10, `HuntTimerSpeedMultiplierAfterLastBloodrageCharge` 3, `BloodTrailFireInterval` 3,
`MaxBloodTrails` 3, `BloodTrailMinDistance` 10, `BloodTrailMaxDistance` 300, `GauntletOutOfZoneTimerDuration` 4,
`GauntletFailsafeDuration` 30, `GauntletFailsafeMonsterLimit` 5. Encounter configs (`sync.GetEncounterConfig()`):
Campaign `HuntInitialDuration` 25, `GauntletEnemiesPerWave` 4, `MaxNbOfSpawnedMonsters` 16; Monolith 1–4 20, 6, 30;
all `TimeAddedToHuntDurationOnChampionKill` 10, `PreGauntletDuration` 3, `GauntletAreaRadius` 22,
`CrystalFreezeAbilityInterval` 10, `CrystalFreezeAbilityPrecastDuration` 1.

The mod reads every value it speaks at runtime; none is a literal in code.

- Requirement: the first charge threshold. `GetBloodrageChargeCount` and `FixedUpdate_Hunt` both read the list at
  `RageOfMorditasData+0xD0` (V); the hunt's pass/fail compares the kill count with a System.Core LINQ call over it,
  First or equivalent (I; both lists start at 20). `Notifications.MorditasNotification_EventStarted(int killsRequired)`
  receives the first buff threshold (`+0xC8`) and never shows it: the localized description has no placeholder.
- Time left = `HuntTimeTotal − HuntTimePassed` from the network object (V for the end condition; I that the timer widget
  shows the same).
- Warnings: the timer widget warns at 15 s and 5 s; the mod uses the data's `HuntRemainingDuration*` values.

### Game texts (UI table)

| Key (`RageOfMorditas_…`) | English / Spanish |
|---|---|
| `Name` | Rage of Morditas / Furia de Morditas |
| `Hunt_EventStarted_Title`, `_Description` | Event Started / Evento empezado; Defeat as many monsters as you can. / Derrota a todos los monstruos que puedas. |
| `Hunt_Objective_HuntStarted` | same as the start description |
| `Hunt_RitualStart_Title`, `_Description` | Defeat bloodcrafted enemies; Morditas is using blood magic to revive your enemies. Defeat them to coagulate them into rewards. |
| `Hunt_Objective_PrepareGauntlet` | Defeat all the bloodcrafted monsters. / Derrota a todos los monstruos forjados en sangre. |
| `Hunt_Objective_ReadyForReward` | Interact with the Blood Ice crystal. / Interactúa con el cristal de Hielo sangriento. |
| `Hunt_BloodrageChargeGained_Title`, `_Description` | Bloodrage Charge Gained; You've earned a Bloodrage Charge |
| `Hunt_BuffGained_Title`, `_Description` | Bloodrage Buff Gained; Gained {0}! |
| `Hunt_EventFailed_Title`, `_Description` | Event Failed / Evento fallido; Morditas is not impressed with your performance. |
| `Encounter_AbandonTitle` (no prefix) | Abandoning Event in {0}s / Abandonando en: {0} |

## What the mod says

One speaker, `MorditasAccessibility`, polled every 100 ms. Speech goes through `ScreenReader.TrySpeakOverPickup`
only with gameplay access (`QuestAnnouncementEligibility.HasGameplayAccess`: no menu, typing, dialogue, pause or
loading); the outcome also speaks over the death screen (not while typing), since the encounter may be gone after the
respawn. Nothing counts as said until the reader accepts it, so held-back speech comes later with the latest state.

| When | Speech (English) |
|---|---|
| Hunt starts | game start title and description + "Defeat 20 monsters before time runs out; kills add time. 25 seconds left." |
| Kills | "12 of 20 kills", coalesced (below) |
| First threshold | "20 kills, enough: the next phase starts when time runs out." + game charge title + "Next charge at 50 kills." |
| Later charges | game charge title + next threshold; counts toward it, less often |
| Time | "15 seconds left", "5 seconds left" when time falls through the game's warnings; the critical one interrupts before the requirement is met. A warning re-arms only when time left climbs `RearmSeconds` (2 s) above it |
| PrepareGauntlet | game ritual title and description |
| Gauntlet starts | game gauntlet objective + "Stay within 22 steps of this spot." + "24 enemies left" |
| Enemies | "N enemies left", coalesced |
| Leaving | "Outside the circle. Centre: 25 steps, down and left." + when everyone is out "Return within 4 seconds or the event is abandoned." (interrupts); "Back inside the circle." |
| ReadyForReward | game reward objective |
| Finished | "Rage of Morditas complete." or the game's failure title and description (interrupts) |

Steps are the scanner's (body width, `ExplorationCatalog.Steps`); directions are the guidance directions.
A first reading mid-encounter (late join, scene return) announces the current phase; one before the start or after
the end is a silent baseline. The game's buff pop-up ("Bloodrage Buff Gained") stays with the notification reader.

### Pacing

Mod choices (`MorditasEventState`), not game data: a count speaks once it has rested `SettleSeconds` (0.6 s: an area
attack's kills land over several frames) or after `MaxWaitSeconds` (2.5 s) of steady kills, and not sooner than
`HuntInterval` (2 s) toward the requirement, `ChargeInterval` (6 s) toward later charges or `GauntletInterval` (3 s)
for enemies left. Milestones (requirement, charges, phases, leaving) are immediate.

### One speaker

`MorditasSpeechOwner` is claimed in the first hook call of an encounter (before the game's pop-up can settle) and kept
for the scene, so late pop-ups stay silent too:

- `NotificationAnnouncer` skips roots holding `RageOfMorditasOutOfGauntletNotification` or
  `RageOfMorditasHuntProgressNotification` (timer and enemies-left widget) and any text containing the start, failure,
  ritual or charge description.
- `DynamicObjectiveProbe` skips the Morditas quest tracker.
- If no hook ever fires, nothing is claimed and both readers behave as before (`EncounterCountdownReader` included).

On demand: the vitals report (Create+Square / R) adds "Rage of Morditas. 12 of 20 kills. 9 seconds left" (or enemies
left, outside, reward) after life and mana. No new binding.

## Blood trails

**Scanner.** Category "blood trail" (`ExplorationCategory.BloodTrail = 17`), first in the order: it exists only during
the hunt, whose goal is reaching monsters. Entries "blood trail N" (keys `bloodtrail:N`), numbered nearest first at
each refresh, gone when the hunt ends. Arrival radius = `BloodTrailMinDistance`: the game stops trailing a monster that
close. A beacon lock (Create+Cross) follows its own number while drawn, else the nearest trail under the locked key
(`BloodTrails.TryFollow` in `ExplorationAccessibility.LockedTarget`); no trails left ends it as "target lost".

**Drip.** Each game refresh queues one drip from the nearest target, placed when it plays (camera pan, lower pitch
behind, `CuePlacement`). Level falls linearly from full at the minimum trail distance to `FarGain` (35 %, mod choice)
at the maximum. It waits its turn (`WorldAudioSource.Trail`) until the next refresh is due, then is dropped; silent in
menus, dialogue, pause and loading. Sound: `BloodTrailSynthesizer`, a 160 ms sine gliding 320→960 Hz in 50 ms with a
45 ms decay, like a falling drop; no asset. Every bundled WAV already names another object, and the beacon ping (flat
620 Hz blip), NPC tap and enemy growls are all different shapes. Settings: `[Rage of Morditas] BloodTrailSound` (on),
`BloodTrailVolume` (100); mod menu Sounds → Rage of Morditas with "Listen"; glossary id `BloodTrail`.

## Circle pulse

User request (2026-10-21, tester log): in the final part the circle and the crystal needed a sound that is a little
louder, tells front from back better and repeats faster. Before, nothing marked the gauntlet's centre, and the reward
crystal had the generic shrine recording, whose only rear cue is a 6 % pitch drop (`WorldLoopSelection.RearPitch`).

- **Source.** Gauntlet: the circle's centre (`GauntletLocation`). ReadyForReward: the reward crystal's click listener
  (`RageOfMorditasRewardVisualController.InteractableClickListener`, found in `WorldObjectClickListener.allListener` at
  most once a second, `MorditasProbe.TryRewardCrystal`), else the centre. Silent from Finished on, in menus (the reward
  panel), dialogue, pause and loading.
- **Sound.** `Audio/MorditasCircleSynthesizer.cs`: a 140 ms bell at 440 Hz with inharmonic partials (1, 2.76, 5.4),
  every 0.5 s (`MorditasCircleCue.PulseSeconds`, mod choice). Behind it drops a perfect fifth and loses its overtones
  (dull), fully from 60° either side of straight behind; equal-power pan. Peak 0.3, above the drip's 0.2. Level falls
  linearly from full at the centre to 50 % at the radius and stays there outside, so the way back is audible.
- **No double sound.** While it sounds the crystal, the world loops skip that listener (`WorldCueProbe.SoundedElsewhere`, set by `MorditasAccessibility`,
  checked in `WorldCueProbe.Consider`).
- **Settings.** `[Rage of Morditas] CircleSound` (on), `CircleVolume` (100); mod menu Sounds → Rage of Morditas;
  glossary id `MorditasCircle` with "Listen". Reservation source `WorldAudioSource.Circle`.
- Tests (`LeAccess.Morditas.Tests`, 108 checks): length, silence at zero, louder than the drip, pan, rear lower and
  without overtones, rear share, distance level. Unheard in game.

## Hooks

All read-only postfixes; the patch audit (`LeAccess.PatchSafety.Tests`) confirms each target exists with a native body
of its own (Steam 1.5: 89 declarations / 88 targets). Native RVAs, current build only:

| Target | RVA | Use |
|---|---|---|
| `RageOfMorditasHuntVisualController.OnStateUpdate` | 15229C0 | first sight of the encounter (`Init` calls it, not inlined) |
| `RageOfMorditasGauntletVisualController.OnStateUpdate` | 151E220 | same, for a client arriving in the gauntlet |
| `…GauntletVisualController.OnInGauntletStatusChanged(bool status)` | 151E0E0 | local player inside (true) or out |
| `…GauntletVisualController.OnAllPlayersOutOfGauntletChanged(bool)` | 151DC90 | abandonment countdown running |
| `RageOfMorditasHuntVisualController.HandleBloodTrails(IReadOnlyList<Vector3>)` | 1520DF0 | trail targets; the `List<Vector3>` is copied at once |

Polled state: `RageOfMorditasSync.networkObject` (`RageOfMorditasNetObject`: `State` byte at +0x51, `Success`,
`HuntKillCount` +0x64, `HuntTimePassed`, `HuntTimeTotal`, `GauntletKillCount` +0x70, `GauntletLocation`) and
`IsLocalPlayerInEncounterScene`; the same on host and online clients. `RageOfMorditasLogic.runningInstance` is
server-only and not used. Never patch `Notifications.MorditasNotification_BuffGained`: its native body is an empty stub
shared with about 4,095 methods. The audit now resolves generic callback types (`IReadOnlyList<Vector3>`); that hook
is the mod's first with an interface-typed argument, so its patch class has a Harmony `Cleanup` that logs
`[Morditas] blood trail hook unavailable` and lets the rest of the mod load if the detour cannot be built. The mod
references `LE.Networking.Core.dll` (game interop, compile only) for the network object's base type.

The timer, enemies-left and leaving widgets are created with `NotificationSystem.CreateImmediateNotification` (callers
at 17847C4, 178369B, 1784A58; V), not through `ShowOnChannel`, which is why nothing was read before.

## Log

`[Morditas] tracking encounter N`, `encounter N: thresholds=[…] warning=… radius=… (S steps) trails=…`,
`state Hunt success=… kills=… time_left=… gauntlet_kills=…`, `circle inside=… all_out=…`,
`blood trails 3, nearest 42.0 m`, every spoken line, `encounter no longer followed`, failures as
`[Morditas] … unavailable`. The scanner status line has `trails=N`.

## Tests

`LeAccess.Morditas.Tests` (108 checks): phases, coalescing, warnings with re-arm, requirement and charges, circle
in/out and countdown, baselines, status, trail order, entries, follow, placement, level, ownership, drip, circle pulse.
Also: `ExplorationCatalog.Tests` (order, trail walk), `Notifications.Tests` and `DynamicObjectives.Tests` (deference
and fallback), `Vitals.Tests` (report), `PatchSafety.Tests`.

## Open live checks

1. Start a hunt: start speech once (no second copy from the pop-up), requirement and time; plugin log has
   "Harmony patches applied" (the generic-parameter hook loads).
2. Kill packs with area skills: one count per burst; requirement and charge lines; warnings at 15 and 5 s.
3. Blood trails: the drip every ~3 s from the right side and quieter far; scanner category first; beacon lock follows.
4. Gauntlet: circle size in steps, enemies left matches the widget, leaving and returning, the 4 s countdown, failure
   by leaving; online client and party member (non-owner) too.
5. Reward and success line; failure by time with fewer than 20 kills.

## Reward panel slots (2026-10-21)

Tester report: after the event the "Recompensas del ritual" slots spoke "Item Container UI 0..4" (log: focus on
`ItemContainerUI` under `RageOfMortidas_RewardPanelRewardUI`; neither the inventory reader nor any endgame reader claimed it).
Code: `Market/MorditasRewardSlots.cs` (slots), `Market/MorditasReader.cs` (buttons, dispatch), `EndgameText.RewardSlot`.
Provenance: **I** = inferred from the interop member names (`last epoch code/`), none verified in game yet.

| Class | Members used |
|---|---|
| `RageOfMorditasRewardPanel` | `ItemRewards` (list of slots; inactive ones are skipped for "n of m"), `ClaimButton`, `RitualOfBloodButton`, `RitualOfIceButton`, `_description` (status line: "select an item", "not enough Bloodrage"), `_bloodrageChargesRoot` (charges label and number) |
| `RageOfMorditasRewardPanelRewardUI` | `ItemContainerUI`, `IsSelected` (ritual target), `IsSacrificedForBlood` / `IsSacrificedForFrost`, `CanBeUpgradedByRitual` |
| `ItemContainerUI` | `Container.GetEntryAt(0,0)` (one-slot `RageOfMorditasItemContainer`) then `entry.data` |

- Slot line: item (`ItemIdentityReader`, the inventory's name plus base type, extracted from `InventoryAccessibility`), then
  "marked as target", "sacrificed for <game's ritual name>", "can be upgraded by a ritual" (I), then the place among slots.
  The stop key holds item and state, so selecting a target or a sacrifice is spoken again. The details key reads the game's own
  item tooltip (`ItemTooltipReader.TryReadForItem`), as in the inventory.
- `InventoryAccessibility.GetFocusedContainer` leaves these containers to the Morditas reader, so nothing is spoken twice.
- Claim button: caption, then the charges (`_bloodrageChargesRoot` text); any button that is not interactable adds `_description`,
  the panel's own reason. Charge changes are also spoken by the window-text reader, which rereads the panel when it changes.
- Mod strings: `morditas.reward_target`, `morditas.reward_sacrificed`, `morditas.reward_upgradable` (ten catalogs).
- Unverified in game: that focus on a slot reaches the Morditas reader (the log shows the generic announcer, so the focused
  object is a child of the slot), the exact meaning of `CanBeUpgradedByRitual`, and the claim cost wording.
