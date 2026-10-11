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

- The game gets no input: the Rewired player stops playing (`Player.isPlaying = false`). It plays again only once
  every pad button, key and mouse button is up, so the closing press is not delivered to the game as a new press.
- The game keeps running (it is not a pause); the world sounds keep playing, so volume and distance changes are heard
  live.
- Other mod speech is held back (dropped, not queued) so the menu is not talked over; the menu speaks through
  `ScreenReader`'s owned path.
- The mod reads the pad through the gamepad template, which ignores `isPlaying`.

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

Log lines: `[Mod menu] opened`, `closed; other announcements held back: N`, `game input paused` / `resumed`,
`L1 bindings held back while a chord modifier is down: N`, `option without menu text: Section/Key`.

Unverified live (2026-10-11): that `Player.isPlaying = false` silences every game input path (menus read Rewired
through `EpochInputManager`; anything reading `UnityEngine.Input` directly is not blocked), the L1 element id from the
template source on DualSense and Xbox pads, and the preview levels.
