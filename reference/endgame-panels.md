# Endgame and progression panels

Added 2026-10-08, build 25764382. Host tests only (`EndgameTests`, run in English and Spanish; the MenuReading suite passes 378 checks); no game launch,
so every runtime path below is unverified by listening. Prefab evidence: read-only UnityPy dump of
`LEAssetBundles/assets_*.bundle` (one prefab per panel); API evidence: `last epoch code/`. The user's last log has no
line from these panels, so there was no raw-name speech to grep.

## How they are read

- **Window text** (`Endgame/EndgameWindows.cs` + `NativeWindowDiscovery`): panels that are not `IModal` were never
  discovered as windows, so nothing spoke their title or instructions. They are now tracked like the dungeon panels:
  visible text once on open, later changes without interrupting. Filters keep options, rows and countdowns out.
- **Stops** (`Endgame/EndgameAccessibility.cs`, polled in `MenuAccessibility`): the focused option/echo/tab, only when its
  state matters. `ElementAnnouncer` stays silent for controls a stop reader owns (`EndgameStops.Owns`).
- **Item slots** (keys, nemesis items, rage rewards) speak through `InventoryAccessibility`: its guard against
  screen-content windows now lets slots of this group through (`EndgameWindows.AllowsItemReading`), after the window text.
- Localized game text always comes from the game's own fields; mod words are `endgame.*` keys. No hooks were added.

## Per panel

| Panel | Verdict | Source and what is spoken |
|---|---|---|
| Temporal Sanctum, Soulfire Bastion, Lightless Arbor entrances | fixed (small) | Window text already read. Tier tabs have no text in the prefab (`TabUIElement`, labels runtime), so `TierTabReader` says "Tier N, selected/locked, N of M" from `DungeonEnterPanelUI.tierTabs`; tier number is the tab position (verify in game). Key slot reads as an inventory slot. |
| Aberroth entrance | fixed | Was not discovered. Now window text: title, preamble, locked-for-realm message (`PinnacleEnterPanelUI.messageBlockerText`). Slot reads as inventory slot. |
| Arena entrance | fixed | Same as above plus `ArenaEnterPanelUI.tierTabs` via `TierTabReader`. |
| Boss confirmation, Pinnacle boss voting | fixed | Title/message once; countdown (`TimerObject`) excluded so it does not reread. `VoteRosterReader` reads rows (`playerName` + which of accepted/declined icon is active): roster on open, then only players whose vote changed. |
| Sanctuary of Eterra | new reader | Window text (header, outcome, explainer). `ChoiceReader`: option from `CorruptionOption.optionText`, "chosen" from `chosenOption`, N of M, confirm/close availability. |
| Choose echo reward | new reader | Same `ChoiceReader` over `EchoRewardPanelManager.rewardOptions[].rewardText`, `chosenOption`, confirm. |
| Choose blessing | new reader | Window text (static labels). `BlessingReader`: option item name or the game's visible item tooltip (waits up to 0.6 s for it), lock reason from `lockTooltip` keys through `Localization`, role (current / new) from the manager fields, N of M. |
| Monolith (echo web) | new reader | Window text: title, corruption and gaze text. `MonolithReader`: when `_echoTooltipOpen`, the echo tooltip's visible title, subtitle, description, modifiers and rewards plus completed / out of reach (`EchoWeb.islandCanBeRun`) / reward waiting. Difficulty selection, search and filter use the generic path. |
| Nemesis | new reader | Window text (title, lore). Buttons have captions and `NemesisTooltip` (generic layers); item slots: see [Nemesis item slots](#nemesis-item-slots-2026-10-26). |
| Nemesis replacement, Start quest echo | fine | `IModal`: template title/message read once by `GenericModalText`; the Harbinger Crest checkbox is a `Toggle` (state patch). Swap slot reads as an inventory slot. |
| Observatory | new reader | Slot summary and picker options; see [Observatory](#observatory). |
| Ladder | new reader | Rows, top places, tabs, empty list; see [Ladder](#ladder). |
| Soothe Morditas | fixed | `IModal`; anger text sits next to a non-interactable slider, so the reader adds `_progressSliderText` ("n / 100") to the window text and rereads it when it changes. Buttons use `_sootheButtonTexts` through generic reading. |
| Rage of Morditas reward | fixed | Window text and item slots as before; ritual and claim buttons named, see [below](#rage-of-morditas-reward). |

## Observatory

Code: `Market/ObservatoryReader.cs`, `Market/ProphecyPickerReader.cs`, wording in `Market/MarketText.cs` (`market.*`); no hook.
Prefab: one `ProphecySlotEdit` at a time (region, lens slot, target, reward slot, charge count, locked overlay) between textless
Previous/Next Region buttons; `ObservatoryUI` is the navigable, so the slot is read even when no control has focus.

- **Slot stop** (`EndgameStops`, polled): region (`ProphecyRegion.GetDisplayName`), goal (`ProphecyTargetUI._targetDescription`),
  reward (`ProphecyRewardUI.GetDisplayName`, `GetRewardDescription`), lens (name plus `_lensEffectText`), `CompletedCharges`,
  charge progress percent (`GetChargePercentage`, read as a fraction), `FavorPerCharge`, ready indicator, or the locked text.
  Re-spoken when region, reward, lens, charge count or lock changes. The slot's own labels are excluded from the window text.
- **Region buttons**: "Región siguiente: Sur" (neighbour from `Increment`/`Decrement`; verify they match the buttons).
- **Pickers** (reward and lens `ModalPickerInput` options, event path in `ElementAnnouncer`): name, effect, favor price,
  lock reason (`GetRewardMessageIfLocked`), "actual" from the socket's active indicator, place among sibling sockets. Lens
  options add the effect: the slot's effect label builds it from the common string table key
  `CircleOfFortune_Lens_<LensType index>_Effect` ("+50% velocidad de carga" style), read with `Localization.GetText` (index
  matches the enum order for the first three lenses; verify the others in game).
- Unverified: the percent scale, whether the picker items are the focused objects, and `Increment` direction.

## Ladder

Code: `Market/LadderReader.cs`. Prefab: `LadderRowPlayerElement` (rank, score, `MemberHolder` of `LadderRecordMemberElement`
with level, name, class, dead icon and five skill selectables), `LadderUITopPlaceElement`, `TabUIElement` tabs.

- **Rows**: "Puesto 3, tú, Ana, Pícaro, nivel 92, Puntuación 12.500" from `rankTMP`, member TMPs, `scoreTMP`; party rows list each
  character; `localPlayerBackground` marks the player's own row; a bare-number level gets the word "nivel", other text is as
  the game shows it. Focus on a skill icon keeps its own tooltip reading.
- **Top placements**: rank, account name (`_playerName`), party members or `_characterName`, score.
- **Tabs** (mode and ladder type): caption, selected from `TabUIController.ActiveTabId`, place among visible tabs.
- **Empty list**: "Sin entradas en la clasificación" once when `noEntriesElement` is shown (offline or a filter without entries).
- **Class and mode dropdowns** are `TMP_LocalizedDropdown`s owned by the stop reader: "Clase: Todas" (title from the sibling label,
  else `market.ladder_class` / `market.ladder_mode`); the stop key holds the value, so a new choice is spoken once when the list
  closes. Expanded options use the generic reader (selected, N de M). Online data: any missing field reads nothing.

## Rage of Morditas reward

Code: `Market/MorditasReader.cs`. The three buttons (`RitualOfBloodButton`, `RitualOfIceButton`, `ClaimButton`) have no caption.
Names come from the game's UI strings `RageOfMorditas_BloodyRitual_Name`, `RageOfMorditas_FrozenRitual_Name`, `Reward_Claim`
through `Localization.GetText` ("Ritual sangriento", "Ritual gélido", "Reclamar" in Spanish). A ritual button then adds the
game's `MortidasTooltip` text (description, marked item, Bloodrage cost) once it is visible (waits up to 0.6 s); unavailable
buttons say so.

## Nemesis item slots (2026-10-26)

Player report: the encounter "just says nemesis item with no description of it". No log; same shape as the Morditas
reward slots (an item holder no reader claimed, so the generic announcer spoke its object name).
Code: `Market/NemesisItemSlots.cs`, shared single-slot helpers `Endgame/ItemSlotReader.cs` (also used by Morditas).

| Class | Members used |
|---|---|
| `NemesisPanel` | `_manager` (`NemesisPanelManager`) |
| `NemesisPanelManager` | `itemHolders` (`NemesisItemHolderUI`: `containerID`, `nonSwappableHolder`, `swappableHolder`, `swappedItemHolder`), `swapButtons`, `containers.Containers` (`NemesisItemContainer`, one slot each) |

- The encounter also appears in the campaign (the user meets it at level 30); the reader keys on `NemesisPanel` alone,
  never on the zone or level, like every panel in this file.
- A focused object inside the active layout of a holder is that holder's slot; swap buttons keep their own caption.
- Item: the `ItemContainerUI` under the holder (cell 0,0), else the panel's `NemesisItemContainer` with the holder's
  `containerID`. Spoken as the inventory does (name, base type), then "N of M" over the holders that show an item
  layout. The details key reads the game's item tooltip for that item (as Morditas); without one it repeats the line.
- `InventoryAccessibility` leaves these containers to this reader, so nothing is spoken twice. The swap slot of the
  replacement window (`NemesisSwapSlotContainerUI`, `NEMESIS_SWAP`) is not in a holder and still reads as an inventory slot.
- Unverified: that focus lands inside a holder layout (log `[Endgame] <item>, 1 of 2`); that the holder holds an
  `ItemContainerUI`; whether the game shows the item tooltip on controller focus (details); online.

## Limits and first in-game checks

1. Tier tab wording and numbering (dungeon and arena), and that the tier description still rereads when the tab changes.
2. Blessing options: name/tooltip arrives, locked options 4 and 5 explain the lock, step 2 says current / new.
3. Monolith: stick over an echo speaks tooltip then state; `_echoTooltipOpen` must be true while the stick rests on it.
4. Sanctuary and echo reward: option, "chosen" after Cross, confirm becoming available.
5. Boss vote and Pinnacle voting: roster once, then one short line per change; no repeats from the countdown.
6. Item slots (key, nemesis, rage reward) speak after the window text, not over it; Nemesis slots say the item.
7. Observatory: slot summary on opening and after switching region, picker options, region buttons; Ladder rows and tabs
   (class/mode dropdown title, value, list and new value); Morditas buttons name and tooltip text.
