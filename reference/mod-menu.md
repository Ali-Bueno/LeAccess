# Mod settings menu and sound glossary

Requested 2026-10-11 (a player, relayed by the user): every mod option in an in-game menu, a glossary of the audio
cues, group and individual volumes and distances for every object with a sound, opened with Create + L1 or L3 + L1
from the main menu or gameplay alike.

## Opening

- Controller: L1 pressed while Create (PlayStation pads), L3 (every pad) or the mod button (Back/View on other pads
  when `Exploration/LeftStickOnOtherPads` is off) is held. The same chord closes it. Keyboard: Ctrl + `Mod menu/OpenKey`
  (F1 by default).
- While a chord modifier is held, the game's own bindings of L1 on that pad are switched off, so the chord does not
  also drink a potion (gameplay L1) or change an era (world map). The pad's L1 element id comes from the template:
  `leftShoulder1.source` (`IControllerTemplateButtonSource`) `.target.elementIdentifierId`; the interop has no usable
  `GetElementTargets` on `IGamepadTemplate`. They come back once L1 and the modifier are up (`ShoulderGuard`).

## While open

- The game gets no input: every enabled Rewired controller map (keyboard, mouse, each joystick) of every player,
  the system player included, is turned off, re-checked each frame, and turned back on once every pad button, key and
  mouse button is up, so the closing press is not delivered to the game as a new press.
  `Player.isPlaying = false` was tried first and blocks nothing (user report 2026-10-11: Cross activated game menus,
  Circle dodged; log showed `game input paused`). In Rewired it only governs joystick auto-assignment.
- Maps are remembered by player, controller type and id, category and layout, and on closing the live map with that
  identity is turned on, not the object turned off. A change of controller mode or active pad makes
  `EpochInputManager.set_IsControllerActive` load the pad's maps again (`LoadControllerData`), and Rewired's
  replacement copies the off state of the map it replaces; turning the old objects back on left the pad's 14 maps off
  for the rest of the session (tester log 2026-10-11, XInput pad: the player never moved again after the first close,
  the next opens counted 10 maps instead of 24). Static analysis, build 25816319, `set_IsControllerActive` 0x10c9860
  +0x39d.
- While the block is up the controller mode stays as it was: `ControllerModeGuard` answers `ControllerShouldBeActive`
  with the current mode, so Ctrl+F1, the arrows or Alt+Tab do not switch to keyboard and the pad does not switch back,
  and nothing reloads the maps. A change of active pad still reloads them; the identity restore covers it.
- The game's own menus read the pad through the gamepad template in their `IControllerMap.ProcessInput` coroutine
  (`UiNavigationMap`, `InventoryMap`, `MainMenuMap`, `PriorityUiNavigationMap`), so Rewired maps do not reach them.
  Each loop of those coroutines first compares `ControllerManager.instance.CurrentMap` (+0x48) with itself and ends
  when it differs, before reading the pad (Capstone: `InventoryMap` 0x10398b8, `UiNavigationMap` 0x103f662). While
  open the mod sets `CurrentMap` to `ControllerMapsManager.defaultMap` (its coroutine yields once and reads no
  buttons) each frame, and on closing calls `ControllerMapsManager.UpdateMap()`; with `defaultMap` current, `SetMap`
  does not return early, so the map of the open screen starts again and `SetRewiredMap` reapplies the game's map rules.
- Before 2026-10-11 afternoon the mod kept `NavigationManager.currentlySelected` null instead. Move and submit then do
  nothing, but `NavigationManager.HandleCancelInput` with no selection calls `UIBase.CloseAll` (0x1080c90 +0x72), so
  Circle in the mod menu closed the inventory or any open panel (user report). The empty priority-navigable list used
  against the landing screen's `PriorityUiNavigationMap` is gone too: that map's coroutine ends like the others.
- Buttons with a `ButtonInputListener` (the landing screen's Play) answer their own `key` through Unity's input, pad
  buttons included, past all of the above (user report: Cross still pressed Play with the selection held). Every
  enabled listener registered in `ButtonInputListenerManager._inputListeners` is disabled while open and re-enabled
  with the rest.
- Panels with a hold action make the navigation map start `ControllerButtonHoldManager.StartHoldingButton` on Cross;
  `UpdateButtonsHold` then vibrates the pad (`ControllerHapticsManager.Play`) and fires the hold feedback (user report:
  vibration and a cursor sound, nothing activated). While open, a hold in progress is reset each frame
  (`ResetHoldingButton`, which also stops the haptics). Call targets checked with Capstone.
- The existing `LandingZonePanel.OnPlay*Clicked` prefix (`LoginTransitionGuard.MayExecute`) refuses both while
  `InputBlock.Blocking` (open, or closing with a button still down): `[Mod menu] landing click ignored: online|offline`
  (Cross once reached Play with every other block in place). Failures name their stage
  (`Mod menu input block unavailable (stage: …)`).
- The current `EventSystem` also stops sending navigation (`sendNavigationEvents = false`), restored the same way, and
  its selected object is put back on closing: the landing screen reads the pad through Unity's own UI input module
  (it moved to "Play offline" and pressed it with 24 Rewired maps off; user report).
- The game keeps running (it is not a pause); the world sounds keep playing, so volume and distance changes are heard
  live.
- Other mod speech is held back (dropped, not queued) so the menu is not talked over; the menu speaks through
  `ScreenReader`'s owned path.
- The mod reads the pad through the gamepad template, which reads the hardware and ignores controller maps.

## Controls

| Pad | Keyboard | Action |
|---|---|---|
| D-pad / left stick up, down | Up, Down | Previous / next entry (held: repeats) |
| D-pad / left stick left, right | Left, Right | Lower / raise the value |
| Cross | Enter, Space | Toggle, open a page, play a sound, capture a key |
| Circle | Escape, Backspace | Back; on the first page closes |
| Triangle | Delete | Default value |
| Square | P | Play the entry's sound |
| Right stick up | I | Details: description, default, range |
| Options | | Close |

## Pages

Since 2026-10-17 (user request: "split it into sections, Sounds with volume, loop and preview inside") the first page
holds five groups and Close (`ModMenuCatalog.Groups`, title `menu.group.<id>`), each a page of section pages:
- **Sounds**: master volume, wall radar, wall tones, object sounds, enemy sounds, NPC sounds, beacon, low life
  (`[Vitals] LowLife*`, a second page of the Vitals section), Rage of Morditas (blood trail drip,
  [details](rage-of-morditas.md#blood-trails)).
- **Exploration**: scanner and exploration, points of interest, map zones.
- **Announcements**: combat speech, life and mana, quests, level and progress, dialogue.
- **Menus and screens**: menus, tooltips, character sheet.
- **General**: language, mod menu, updates, diagnostics.

A sound page lists its options (volume, repeat speed, toggles) and then "Listen: <sound>" for every glossary sound whose
volume one of its options sets (`PreviewItem`; Cross or Square plays it, details say what it means). The object sounds
page ends with **Volume of each object** (each kind's own volume; Cross plays it, left/right change it) and **Sound
distances** (each kind's own distance; 0 follows `World object sounds/LoopRange`). `World object sound volumes` and
`World object sound ranges` appear only there.
- Section pages list every bound option of the section; a section split over two pages lists unknown options on its first. Labels and details are `ModText` keys
  (`menu.option.<id>`, `menu.option.<id>.details`); an option missing from the catalog still appears under its key name
  and is logged.

## Values

Booleans toggle. Numbers step within their `AcceptableValueRange` (or the catalog's bounds) and clamp. Strings with a
choice list cycle (Language: `auto` and the ten catalogs, by their own names). Key codes are captured: Cross, then a
key; Escape cancels. Free text (`NearestEnemyAnnouncementFormat`) is read-only; Triangle restores it. Setting a value
writes `LeAccess.cfg` at once (`SaveOnConfigSet`). Options read live take effect at once; audio buffer sizes on the next
output start (their details say so).

## Audio contract (`SoundLevels`, `SoundGlossary`, `SoundPreview`)

- `[Audio] MasterVolume` 0-100 (100): scales every mod sound at its output (`RadarAudioOutput`, `LoopAudioOutput`,
  `WallSonar`), live. Speech is not affected.
- `[World object sound volumes] <Kind>` 0-200 (100) and `[World object sound ranges] <Kind>` 0-30 m (0 = group
  distance), for each world kind: Gold, Equipment, MagicEquipment, RareEquipment, LegendaryEquipment, Potion, Tome,
  Rune, Glyph, AffixShard, QuestItem, Chest, Stash, Portal, ZoneExit, Shrine, Nemesis, UnexploredRoom. Volume applies to
  the loop and the repeated cue of that kind.
- `[Enemy cues] BossVolume`, `EliteVolume`, `OrdinaryVolume` 0-200 (100); `[Navigation audio] SonarVolume` 0-100 (100);
  Discovery and RoomEntered have their own `[World object sound volumes]` keys (no range).
- `RepeatSpeed` 50-300 (100) in `[Enemy cues]`, `[NPC sounds]`, `[World object sounds]` and `[Exploration beacon]`
  (user request 2026-10-17): divides the designed interval of the cues that repeat instead of looping
  (`SoundLevels.RepeatSpeed`): enemy and NPC pulses (`EntityCueCadence`, 0.15-0.45 s by distance), the gap between
  pulsing object cues (`WorldCueState`, 0.45 s; loops are unchanged) and the beacon ping (`BeaconState`, 0.16-1.2 s).
  Enemy pulses cannot go below the tracker's 0.1 s poll, and a cue never starts before the previous one ends.
- `SoundGlossary.Entries`: id, name and details keys (`sound.<id>`, `sound.<id>.details`) and the config entry that sets
  its volume. `SoundPreview.Play(id)` plays the sound centred at master x group x own level, at most about 2 s.

## Code

`LeAccess/Accessibility/ModMenu/`: `MenuModel` (navigator, items; pure), `SettingItem` (stepping and units; pure),
`ModMenuCatalog` (order, units, steps; pure), `ModMenuPages` (config adapter, pages, options missing from the catalog),
`ModMenuSoundPages` (listen entries, object volumes and distances), `ModMenuInput` and `ModMenuInputGate` (chord, commands, `ShoulderGuard`,
`InputBlock`), `ModMenu` (open/close, speech). Host suite `tests/LeAccess.ModMenu.Tests` also checks that every
`.Bind(section, "Key")` in the source is in the catalog with both text keys.

Log lines: `[Mod menu] opened`, `closed; other announcements held back: N`, `game input paused: N controller maps
off, UI navigation off: N, game controller map held: True, button listeners off: N` / `resumed: N of M controller maps on`
(N below M: a controller left while open), `game controller map chosen again` (only when a panel map was held), `maps turned back on by the game while open: N` (should not appear), `game button holds cancelled while open: N`,
`L1 bindings held back while a chord modifier is down: N`, `option without menu text: Section/Key`.

Verified live (2026-10-11): the L1 guard (9 and 10 L1 bindings held back on a DualSense, no potion). Verified live
(2026-10-11 afternoon, virtual XInput pad through ViGEm, online, Lagoon): open with Ctrl+F1, arrows, D-pad and stick in
the menu, Ctrl+F1 to close, then walk. 0.2.0 left the player still (position unchanged) and the next open counted 25
maps instead of 39. With the fix the player walks and closing logs `resumed: 24 of 24`. Mod menu over the inventory,
Cross into a page, Circle back, Circle to close: 0.2.0 closed the inventory; with the fix it stays open and its cursor
moves. On the landing screen Cross and Circle in the menu did not press Play, and the D-pad navigated after closing. The reporting tester
confirmed their own pad works with 0.2.1 (2026-10-11). Unverified: that
turning the maps off silences every game input path (anything reading `UnityEngine.Input` directly is not blocked),
the L1 guard on an Xbox pad, and the preview levels.
