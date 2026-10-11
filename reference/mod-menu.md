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
  the system player included, is turned off, re-checked each frame, and only those maps are turned back on once every
  pad button, key and mouse button is up, so the closing press is not delivered to the game as a new press.
  `Player.isPlaying = false` was tried first and blocks nothing (user report 2026-10-11: Cross activated game menus,
  Circle dodged; log showed `game input paused`). In Rewired it only governs joystick auto-assignment.
- The game's own menus read the pad through the gamepad template (`UiNavigationMap`/`MainMenuMap`/
  `PriorityUiNavigationMap.ProcessInput` call `Controller.GetTemplate` once and keep it), so Rewired maps do not
  reach them. They hand every move, submit and cancel to `NavigationManager.Handle*Input`, which acts only on the static
  `NavigationManager.currentlySelected` (statics + 0x10; null returns at once, checked with Capstone, build 25816319).
  While open the menu keeps that static null and gives the last selection back once nothing is held. No hook: all
  four handlers start with a short `jne` within 16 bytes.
- Buttons with a `ButtonInputListener` (the landing screen's Play) answer their own `key` through Unity's input, pad
  buttons included, past all of the above (user report: Cross still pressed Play with the selection held). Every
  enabled listener registered in `ButtonInputListenerManager._inputListeners` is disabled while open and re-enabled
  with the rest.
- Panels with a hold action make the navigation map start `ControllerButtonHoldManager.StartHoldingButton` on Cross
  before the selection is asked; `UpdateButtonsHold` then vibrates the pad (`ControllerHapticsManager.Play`) and fires
  the hold feedback (user report: vibration and a cursor sound, nothing activated). While open, a hold in progress is
  reset each frame (`ResetHoldingButton`, which also stops the haptics). Call targets checked with Capstone.
- Even so, Cross on the landing screen still reached Play (log: every block in place, then
  `Application state changed to CharacterSelect`); the path is unknown. The existing `LandingZonePanel.OnPlay*Clicked`
  prefix (`LoginTransitionGuard.MayExecute`) now refuses both while `InputBlock.Blocking` (open, or closing with a
  button still down): `[Mod menu] landing click ignored: online|offline`. The event system's selected object is put
  back on closing, since the landing focus still moved to Play offline. Failures name their stage
  (`Mod menu input block unavailable (stage: …)`).
- With the selection held, the landing focus still moved and Cross still reached Play 50 times: `PriorityUiNavigationMap`
  re-selects `NavigationManager.GetPriorityNavigable()` every frame before `ProcessDpadInput` (Capstone, MoveNext
  +0x177 → +0x24a → +0x259). That getter walks `registeredPrioritizedNavigables` (instance + 0x28). While open the
  manager gets an empty list; on closing the original comes back with anything registered meanwhile, minus destroyed
  navigables. With no priority navigable the map calls `ControllerMapsManager.UpdateMap` (+0x182 → +0x202) and ends,
  so on closing the mod calls `UpdateMap` itself; without it every command stayed dead after closing (user report).
- The current `EventSystem` also stops sending navigation (`sendNavigationEvents = false`), restored the same way: with
  24 Rewired maps off, the landing screen still moved to "Play offline" and pressed it (user report, same day), so
  that screen reads the pad through Unity's own UI input module.
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

The first page: master volume (an option), Sounds, Sound distances, then one page per config section in the order of
`ModMenuCatalog.Sections`, then Close. `World object sound volumes` and `World object sound ranges` appear only through Sounds and Sound distances.

- **Sounds** (glossary and volumes): every distinct sound the mod plays. Cross or Square plays it centred at its
  configured level; left/right change that sound's volume (its own, or its group's where it has no own volume); details
  say what it means.
- **Sound distances**: the group distance (`World object sounds/LoopRange`) and each world object kind's own distance;
  0 follows the group distance.
- Section pages list every bound option of the section. Labels and details are `ModText` keys
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
- `SoundGlossary.Entries`: id, name and details keys (`sound.<id>`, `sound.<id>.details`) and the config entry that sets
  its volume. `SoundPreview.Play(id)` plays the sound centred at master x group x own level, at most about 2 s.

## Code

`LeAccess/Accessibility/ModMenu/`: `MenuModel` (navigator, items; pure), `SettingItem` (stepping and units; pure),
`ModMenuCatalog` (order, units, steps; pure), `ModMenuPages` (config adapter, pages, options missing from the catalog),
`ModMenuSoundPages` (glossary and distances), `ModMenuInput` and `ModMenuInputGate` (chord, commands, `ShoulderGuard`,
`InputBlock`), `ModMenu` (open/close, speech). Host suite `tests/LeAccess.ModMenu.Tests` also checks that every
`.Bind(section, "Key")` in the source is in the catalog with both text keys.

Log lines: `[Mod menu] opened`, `closed; other announcements held back: N`, `game input paused: N controller maps
off, UI navigation off: N, menu selection held: True, priority list held: True, button listeners off: N` / `resumed: N controller maps on`, `maps turned back on by the game while open: N` (should not appear), `game button holds cancelled while open: N`,
`L1 bindings held back while a chord modifier is down: N`, `option without menu text: Section/Key`.

Verified live (2026-10-11): the L1 guard (9 and 10 L1 bindings held back on a DualSense, no potion). Unverified: that
turning the maps off silences every game input path (anything reading `UnityEngine.Input` directly is not blocked),
the L1 guard on an Xbox pad, and the preview levels.
