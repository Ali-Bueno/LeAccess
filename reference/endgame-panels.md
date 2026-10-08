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
| Nemesis | fixed (small) | Window text (title, lore). Buttons have captions and `NemesisTooltip` (generic layers); item slots read as inventory slots. |
| Nemesis replacement, Start quest echo | fine | `IModal`: template title/message read once by `GenericModalText`; the Harbinger Crest checkbox is a `Toggle` (state patch). Swap slot reads as an inventory slot. |
| Observatory | fixed (limited) | Window text only (region, lens, target, charges, locked). Slot contents are runtime-filled by `ProphecySlotEditUI`; the lens/reward pickers are `ModalPickerInput` modals. No dedicated stop reader. |
| Ladder | fixed (limited) | Window text excludes rows and lists. Rows (`LadderRowPlayerElement`), dropdowns and tabs use the generic reading; rank/name/score composition not verified. |
| Soothe Morditas | fixed | `IModal`; anger text sits next to a non-interactable slider, so the reader adds `_progressSliderText` ("n / 100") to the window text and rereads it when it changes. Buttons use `_sootheButtonTexts` through generic reading. |
| Rage of Morditas reward | fixed (limited) | Window text (title, info, kills, charges, description); item slots as inventory slots. The Ritual of Ice/Blood and Claim buttons have no caption TMP in the prefab (probably images): only generic reading and their tooltips, unverified. |

## Limits and first in-game checks

1. Tier tab wording and numbering (dungeon and arena), and that the tier description still rereads when the tab changes.
2. Blessing options: name/tooltip arrives, locked options 4 and 5 explain the lock, step 2 says current / new.
3. Monolith: stick over an echo speaks tooltip then state; `_echoTooltipOpen` must be true while the stick rests on it.
4. Sanctuary and echo reward: option, "chosen" after Cross, confirm becoming available.
5. Boss vote and Pinnacle voting: roster once, then one short line per change; no repeats from the countdown.
6. Item slots (key, nemesis, rage reward) speak after the window text, not over it.
