# LeAccess

A screen-reader accessibility mod for Last Epoch.

Status: first public test release. I have played about 20 hours with it and have a level 23 Paladin. It has been
tested with several characters, online and offline, including co-op with a friend, and the game is quite playable. It
is not finished: some screens are still silent, and you will find rough edges.

What changed recently is in [STATUS.md](STATUS.md); the technical notes behind each feature are in
[reference/](reference/).

## Disclaimer

This is a hobby project, and it is a mod of a closed-source Unity game. Almost all of the code was written by Claude
(Anthropic's AI) under my direction; I design the features, play, test and report what is wrong. Last Epoch updates
itself through Steam, and any update can break part of the mod or all of it until I catch up. I fix things when I have
time, so bugs will not always be fixed quickly.

That said, it works well. It reads the menus, it lets you explore by sound, and you can fight, loot, craft, level up,
travel and play online with other people. Just know what you are getting into.

## What you need

- **Last Epoch for Windows 64-bit, Steam version.** I test on 1.5.1 (Steam build 25816319). Other stores are untested.
- **A screen reader.** NVDA is what I use and test with. Speech goes through Tolk, which also talks to JAWS and a few
  others and falls back to SAPI if no screen reader is running, but those are untested.
- **A controller.** The mod is built around the game's controller mode; the keyboard only has a few mod shortcuts.
  - DualSense (what I use) or DualShock 4: the **Create** (Share) button is the mod button.
  - Xbox and other controllers: their View/Select button opens the game's controller menu, so **L3** (clicking the
    left stick) is the mod button instead. On those pads L3 no longer toggles the game's overlay map during gameplay
    (it is a purely visual map). In menus L3 still does what the game says: comparing items, forging and so on.
- **Internet on the first launch**, so the loader can download some Unity files. You do not need to install .NET; it is
  in the package.

## Installation

Download the latest version here (this link always points to the newest release):
<https://github.com/Ali-Bueno/LeAccess/releases/latest/download/LeAccess.zip>

1. Close Last Epoch.
2. In Steam, right click Last Epoch, then Manage, then Browse local files.
3. Extract **everything** in the ZIP straight into that folder, next to `Last Epoch.exe`. Do not leave it inside an
   extra folder. You should end up with `winhttp.dll`, `doorstop_config.ini` and the `BepInEx` and `dotnet` folders
   next to `Last Epoch.exe`.
4. Start NVDA before the game.
5. Start the game from Steam as usual. There is no launcher or BAT file.

The first launch can take several minutes while the loader (BepInEx) prepares itself. It is silent; let it finish and
do not close the game. Later launches are fast. After a game update the next launch takes longer again for the same
reason.

If you already use BepInEx or other mods, back up their files and `BepInEx/config` before mixing installations.

To turn the mod off, with the game closed, move `BepInEx/plugins/LeAccess` out of the `plugins` folder. To turn off the
loader as well, rename `winhttp.dll` (that also turns off any other BepInEx mods). Your saves are never touched.

## Getting started

Last Epoch is an action RPG in the style of Diablo: you run around, hit things, pick up loot and make your character
stronger. Online and offline both work; the mod keeps whatever you choose on the first screen. Most of what the mod does
happens on its own, by sound and speech, while you play with the game's normal controls:

- The menus speak: the focused element with its value, state and position ("3 of 8"). Push the **right stick up** in a
  menu to hear the details or tooltip of what is focused.
- A wall radar: a knock when a wall comes within about two metres, and a breath when you slip past a wall's edge into
  space that goes on (a doorway, a side passage, the end of a wall). A breath that ends in a quick double pip leads
  toward somewhere you have not explored.
- Things in the world have sounds: loot by rarity, gold, potions, chests, the stash, portals, waypoints, exits, shrines,
  rifts, NPCs (wooden taps) and enemies (growls), all panned and faster or louder as you get closer.
- An exploration scanner: hold the mod button and move the right stick to go through exits, waypoints, rooms, NPCs, enemies, loot and more, nearest first, with distance and direction. Mod
  button and Cross starts a beacon that leads you there through ground you have already explored.
- Automatic speech for the things a sighted player sees at a glance: life and mana when they drop, potion charges, gold
  you pick up, level ups and new points, quest updates, zone and era names, the names of NPCs and players you walk up
  to, boss and elite life, dialogue and subtitles, notifications and minion orders.

The basic flow: walk with the left stick and listen. The radar tells you about walls and openings. When you hear loot,
walk toward it; Cross picks up or interacts with the nearest thing, and the D-pad up/down chooses which nearby thing
Cross uses. Enemies growl as they come into sight; your abilities are on Square, Triangle, R1, L2 and R2, and the
game's controller targeting aims them (I play with the game's auto-targeting). Circle dodges, L1 drinks a potion, R3
opens a portal to town.

When you do not know where to go, hold the mod button and flick the right stick up or down to pick a category (exit,
waypoint, quest, unexplored room...), then left or right to step through it. Each entry says its name, how far it is in
steps and one of eight screen directions. Mod button and Cross locks the beacon on it; follow the pings. The scanner
learns places as you see them and remembers them per character.

The game assumes you know what you are doing, so a few things are worth knowing:

- Each class picks a mastery partway through the campaign. That choice screen is read, and so are the passive and
  skill trees.
- The scanner's "last death" category leads to where you last died; there is no corpse to pick up.
- The controller menu (touchpad on PlayStation, View on Xbox) opens a wheel with the panels: inventory, skills,
  passives, map, forge and the rest. The wheel is read as you move the right stick.
- Holding the mod button alone plays a click and pauses the game's actions on the other buttons, so Square and Cross do
  what the mod says instead of casting or interacting. Let go and they work again.
- The right stick on its own still gives your minions orders, and the mod says them ("Minions: attack X").
- The screen readers' keys (Ctrl, Shift, Alt, Insert, Caps Lock) do not throw the game out of controller mode, so you
  can silence speech without losing your place in a menu.
- Name fields (character name, usernames) open the game's screen keyboard with Cross. While it is open you can type on
  your physical keyboard; Enter accepts and Escape leaves.
- For co-op both characters must be online, in the same realm and mode (Normal or Hard), and neither can be on Solo
  Loot. Your username for invitations is read in the Social panel. Careful: on the character creation screen, pressing
  Normal or Hard always switches to the other mode.
- Last Epoch has a lot of systems. The game's own guide (in the game menu, read by the mod) and online build guides
  are your friends.

## Controls

The game's own bindings must stay at their defaults. The mod reads the standard controller layout and does not follow
remapped buttons.

PlayStation names are used below. On an Xbox pad: Cross is A, Circle is B, Square is X, Triangle is Y, L1/R1 are LB/RB,
L2/R2 are LT/RT, Options is Menu and the touchpad is View. "Mod button" means Create on PlayStation pads and L3 on
everything else.

### Menus

| Button | Description |
|---|---|
| D-pad or left stick | Move the focus. Grids (skills, passives, backpack, merchants) say the row, column or branch when you enter it, then "N of M"; they do not wrap |
| Cross | Activate |
| Circle | Back / close |
| Right stick up | Details of the focused thing: item tooltip, skill or passive node, quest, class, setting description. Return the stick to neutral before repeating |
| Options | Game menu |

### The game's buttons in the world

| Button | Description |
|---|---|
| Left stick | Move |
| Cross | Interact / pick up the nearest thing |
| D-pad up / down | Choose which nearby object Cross uses |
| Square, Triangle, R1, L2, R2 | Abilities 1 to 5 |
| Circle | Dodge |
| L1 | Potion |
| R3 | Town portal |
| L3 | Overlay map (PlayStation pads only; on other pads it is the mod button) |
| D-pad left | Minion AI on/off |
| D-pad right | Dungeon ability |
| Right stick | Minion orders (spoken) |
| Touchpad (View on Xbox) | Controller menu wheel |
| Options | Game menu |

### The mod button (hold it)

| Button | Description |
|---|---|
| Right stick up / down | Previous / next scanner category (says the category and its nearest entry) |
| Right stick left / right | Previous / next entry in the category, nearest first |
| Cross | Start the beacon toward the selected entry; again cancels it. It pings along a route through ground you have explored, faster as you get closer, and refuses if there is no explored route |
| Square | Life, mana, potion charges and debuffs |
| Triangle | Gold |
| Circle | Level, experience percent, unspent passive points, skills that can take a point, and the zone |

The scanner categories, in order: exit, portal, waypoint, unexplored room, room, quest, NPC, player, last death, stash,
enemy (everything on screen), treasure (chests), shrine, object (levers, notes, the forge and other things you can use)
and item (loot on the ground). Rooms are the game's minimap rooms: "room N" once you have been inside, "unexplored room
N" when you have seen it but not entered. A target on another floor says "on a higher level" or "on a lower level".

### Inventory, merchants and the forge

| Button | Description |
|---|---|
| D-pad | Move through the grid as it looks: left and right along the row, up and down along the column ("N of M"). Items say their available actions with the button first |
| Right stick up | Full item tooltip |
| L3 (hold) | Compare with what you have equipped: the equipped item, the differences and changed lines |
| R3 | Explanations of the item's modifiers; L3 then R3 adds the ranges |
| Right, from the equipment's right column | Equipped idols (top row: the altar; second row: the idol grid) |

The inventory and every merchant say your gold when they open and after a purchase or sale. Merchants read prices for
buying, selling and buying back. The forge opens from the controller menu wheel; the focus starts on the inventory:
Square sends an item to the forge, L2 goes into the forge and R2 goes back to the inventory. Forge actions read the
affix, its tier and your shards; right stick up adds the preview and forging potential, or why you cannot forge.

### Skills and passives

- The Skills panel is a grid: the bar slots, the base class and each mastery are rows. Square carries the highlighted
  skill into the slot you choose.
- A skill's tree is a grid by branch. Up from the tree's root reaches Specialise and Respec. Each node says what it
  requires; right stick up gives the route from the start of its branch and what it leads to. Spending or removing
  points, and unspent points, are spoken. A specialised skill's tree says its level and the percent to the next one.
- Right stick up on a skill or passive node also reads the explanations of its status effects and keywords (the
  game's "more information" tooltip); holding R3 reads that tooltip too.
- Passive trees go by column: each column says the points it needs, and you can only leave the grid from the first
  column. The mastery banner above them is a list: up and down move through the trees, Cross opens one.
- The character sheet reads your attributes, resistances and defences when it opens; right stick up repeats them.

### World map

- Square hides or shows the quests. With the quests hidden, D-pad up and down walk everything the map shows, region by
  region, like a list: places (locked ones and ones without a waypoint too), dungeons, factions and rifts. Each says its
  level, whether you are here, whether it is locked and its quests. Cross travels.
- L1 / R1 change era, with its name. Triangle goes back to where you are.
- The first quest entry, Knowledge of Orobyss, reads your epoch mastery progress.

### Keyboard

| Key | Description |
|---|---|
| R | Life, mana and potions (configurable, `Vitals.ReportKey`) |
| Ctrl+G | Gold |
| Ctrl+L | Level and progress |
| Ctrl+Numpad 8 / 2 | Previous / next scanner category |
| Ctrl+Numpad 4 / 6 | Previous / next scanner entry |
| Ctrl+B | Start or cancel the beacon |

## Configuration

Options are in `BepInEx/config/LeAccess.cfg`, created on the first launch. Edit it with the game closed. The most
useful ones:

| Section | Option | What it does |
|---|---|---|
| Localization | `Language` | `auto` follows the game's text language; or force `en`, `es`, `fr`, `de`, `pt`, `pl`, `ru`, `ko`, `zh` or `ja` |
| Exploration | `LeftStickOnOtherPads` | L3 as the mod button on non-PlayStation pads (default on) |
| Reactive radar | `Enabled`, `Volume` | The wall radar |
| Enemy cues | `Enabled`, `Bosses`, `Elites`, `Ordinary`, `Volume` | Enemy sounds |
| World object sounds | `Enabled`, `Volume`, `Loops`, `LoopRange` | Loot, objects, portals, exits |
| NPC sounds | `Enabled`, `AnnounceNames`, `Volume` | NPC taps and names |
| Vitals | `AutomaticAnnouncements`, `AnnouncePotionCharges`, `AnnounceGoldPickups` | Automatic life, mana, potion and gold speech |
| Combat feedback | `AnnounceBossLife`, `AnnounceEliteLife`, `AnnounceNearestEnemy` | Boss, elite and nearest-enemy speech |
| Quests | `AutomaticUpdates` | Quest progress speech |
| Dialogue | `SpeakNpcLines` | NPC lines, barks and cinematic subtitles (default on). Turn it off if you play in English and hear the voices; lines the game does not voice are silenced too |
| Menus | `AnnouncePositions` | "3 of 8" in lists and grids (default on) |
| Tooltips | `ReadKeywordExplanations` | Status-effect and keyword explanations (the "more information" tooltip) in skill and passive details (default on) |
| Character sheet | `ReadSummaryOnOpen` | Attributes, resistances and defences read when the character sheet opens (default on) |
| Exploration beacon | `TownFullyExplored` | In towns the beacon routes to any NPC or the stash without walking there first (default on) |

## Language

The mod's own speech follows the game's text language, and changes when you change it in the game's settings: English,
Spanish, French, German, Brazilian Portuguese, Polish, Russian, Korean, Simplified Chinese and Japanese. Game text
(items, skills, quests) is always the game's own. The Spanish texts are the ones I check by ear; the others use the
game's own terms where possible but have not been reviewed by native speakers, so they may sound odd. Corrections are welcome: the texts are in
`BepInEx/plugins/LeAccess/Translations/`.

## Reporting bugs

Open an issue on GitHub or post in the forum thread. Say what you were doing, and attach `BepInEx/LogOutput.log` from
after you closed the game. Read the log before sharing it: it can contain your user name or folder paths. Never share
saves, account folders or tokens.

## Known limitations

- Some screens and elements are still not read. Tell me which.
- Attack telegraphs and other purely visual warnings (ground effects about to explode, for instance) are not conveyed.
- Character lines that are audio-only have no text to read.
- Place descriptions are partial: many areas name their sections and buildings, but not every room or street.
- The radar is good, not perfect. Odd walls, slopes and big objects sometimes give a wrong knock or a missing breath.
- Other screen readers than NVDA are untested.

# Development

LeAccess is a BepInEx 6 IL2CPP plugin (Unity 6000.4.8f1, IL2CPP metadata 39). Durable engineering rules are in
[PRINCIPLES.md](PRINCIPLES.md); progress in [STATUS.md](STATUS.md) and [WHATS_NEXT.md](WHATS_NEXT.md).

## Requirements

- A local Last Epoch installation with generated interop assemblies.
- BepInEx **6.0.0-be.788**, IL2CPP Windows x64, Il2CppInterop **1.5.3**. Setup and interop generation:
  [Steam port notes](reference/steam-port.md).
- A .NET SDK that builds `net6.0`.
- The speech dependencies in `references/tolk/` (not in the repository): `TolkDotNet.dll`, `Tolk.dll` and
  `nvdaControllerClient64.dll`. The shared policy targets [PRISM](https://github.com/ethindp/prism); this revision still
  uses the centralized Tolk adapter, and the move to PRISM is outstanding.

Game assemblies, decompiled game code, third-party DLLs and build outputs are not in the repository.

## Building

From the repository root, with the game closed:

```powershell
dotnet build LeAccess/LeAccess.csproj
```

This deploys the plugin and its dependencies to `BepInEx/plugins/LeAccess/` and the `LeAccess.InteropRepair` preloader
(which repairs a Unity 6.4 generated-type issue) to `BepInEx/patchers/LeAccess.InteropRepair/`. The default game folder
is `D:\games\steam\steamapps\common\Last Epoch`; override it with `-p:GameDir="..."`. To compile without deploying (for
example while the game is running), add `-p:SkipPluginDeployment=true`.

## Tests

Standalone test projects under [tests/](tests/) cover state tracking, focus ownership, visibility, formatting,
navigation geometry, localization and announcement order without the game:

```powershell
dotnet run --project tests/LeAccess.ExplorationInput.Tests -p:SkipPluginDeployment=true
```

The [native hook audit](tests/LeAccess.PatchSafety.Tests/) needs the local game installation. Host tests prove the
tested logic, not what is heard in a running game. The optional Python map-extraction tools use UnityPy and Shapely
with local game assets; see [map section data](reference/map-section-data.md).

## Test package

`tools/build_test_package.ps1` builds the player ZIP from the pinned official BepInEx archive, the plugin, the repair
preloader, the translations, the Spanish `Leeme-LeAccess.txt`, the licenses and a SHA-256 manifest. It contains no
game files, saves or personal configuration. See [test packaging](reference/test-package.md).

## Credits

Some sounds were taken from Diablo IV. They are placeholders that I plan to replace; several of them (gold, potions,
tomes, portals, waypoints, exits and shrines) already use Last Epoch's own sounds.

Third-party components and their licenses are listed in
[THIRD-PARTY-NOTICES.txt](packaging/test-package/THIRD-PARTY-NOTICES.txt).
