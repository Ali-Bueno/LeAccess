# LeAccess

LeAccess is a screen-reader accessibility mod for **Last Epoch**, built as a
BepInEx 6 IL2CPP plugin for Windows. The current target is Steam build
**25690669**, with Unity **6000.4.8f1**.

See [STATUS.md](STATUS.md) for confirmed behavior, pending listening checks and
the latest installed build. Technical findings and reproduction notes live in
[reference/](reference/).

## Accessibility features

- Menu focus, settings, character classes, mastery descriptions and notifications.
- Visible dungeon entry/exit, boss confirmation and native modal instructions.
- Skill and passive details, equipped-skill buttons, inventory actions and quest
  journal reading.
- Forge affix actions, shard details, and localized rune/glyph names and quantities.
- Endgame and faction panels: dungeon/arena tiers, Monolith echoes and blessings, echo rewards,
  boss votes ([endgame](reference/endgame-panels.md)); faction choice and ranks, gamblers and special
  vendors, item-service panels, loot-filter rules and mastery choice ([factions/vendors](reference/faction-vendor-panels.md)).
- Visible dialogue, examination descriptions, NPC subtitles and cinematic text.
- Nearby NPC, enemy, loot, destination and other-player names.
- Health/mana/potion-charge feedback and directional navigation and world-object audio cues.
- Boss and miniboss life narration by life steps ([details](reference/boss-life.md)).
- Automatic quest progress updates and spoken/audio orientation toward visible quest markers or named exits.
- Local map sections, supported physical places and generated-room information.
- English and Spanish mod speech, following the game language, with editable
  translation catalogs and per-key fallback.

The startup guard retains an early online/offline button press until the game's
loading transition has finished. Menu reading preserves native control mappings;
HUD button hints remain silent during exploration.

If screen-reader output is rejected, inventory, skill, passive and quest-journal readers retry
the current selection after recovery. Dialogues, visible subtitles, popups and
notices similarly retain current content until output is accepted. Hidden or
replaced content is discarded; a failed manual inspection rebuilds details only
while the same selection remains owned. See [recovery scope and limits](reference/screen-reader-lifecycle.md).
Quest updates, vitals and automatic orientation also retain current information
after rejected output; failed attempts do not consume announcement pacing.

Menu navigation reads brief names and states. Push the **right stick up** to read
the selected tooltip: inventory, skills, passive nodes, class/mastery/realm
choices, quest details and supported ordinary menu descriptions. Return the stick
to neutral before repeating. Skill inspection refreshes current displayed stats.
Quests also announce their current objective and state during navigation.
Earlier completed or failed objectives appear only in manual details, clearly
labeled as that quest's history.
Shared [menu visibility and focus checks](reference/menu-visibility.md) exclude
hidden, faded and clipped labels, and reject retained background selection.
Collected visual text follows screen rows, top to bottom and left to right;
native controller navigation remains unchanged.
Focused native [dungeon and warning windows](reference/dungeon-warning-reading.md)
read their visible instructions and choices, including changes to tier information.
During gameplay, new quests, current-objective changes (including counters) and
completion are announced automatically. Saved quest history is initially silent;
updates wait while menus or conversations own input.
[Dynamic echo and encounter instructions](reference/dynamic-objectives.md)
read their visible objective/modifier text and share pacing and turns with quest
updates. Queued gameplay text yields to native notices while menus own input.
The visible [Morditas abandonment countdown](reference/announcement-lifecycle.md)
speaks its current instruction once per countdown or restart. It takes priority
over ambient narration during gameplay and waits while menus or typing own input.
Items announce their available actions with the button first; merchant items
include purchase, sale or repurchase prices. Skills announce learned state and
their declared level or passive-point requirement.
[Equipped slots](reference/equipped-items.md) announce the localized slot and
actual worn item, including changes made while focus stays on the same slot.
An empty slot is distinguished from temporarily unavailable item data.
Stash and shop reading verifies the item against the current tab or stock before
announcing it. [Forge and filter controls](reference/crafting-reading.md) read the
selected affix action, rendered tier and available shards; right-stick-up includes
visible shard improvement details. Rune/glyph grids and selector flyouts read
the native selected material's localized name and visible quantity, including
empty stacks. Supported potential and affix-count filter
bounds include their property name alongside the minimum or maximum value.
Manual inspection of a selected forge control includes its visible preview and
current forging potential, or the game's visible reason forging is unavailable.
Item inspection can use the game's tooltip formatter when the visual tooltip is
unavailable, including modifiers, lore and requirements.
Character-name screen keyboards require an explicit click or submit on the name
field; moving focus to it does not start editing.
Settings announce the tab and each control's name/value, including sound sliders
and visible challenge states when no button is available. Descriptions remain
available with right-stick-up.

During gameplay the right stick is the [exploration scanner](reference/exploration-mode.md),
a port of the Diablo 2 mod's: push it up or down to change category and left or right to
step through that category's entries, nearest first. The categories and their order are
D2R's: exit, portal, waypoint, unexplored room, room, quest, NPC, player, last death (where you
last died; a destination only, nothing to pick up), stash, enemy (every enemy on screen), treasure (chests), shrine, object (levers, notes, the forge and
other interactables) and item (loot on the ground). A category step says its name and then
its nearest entry; each entry gives its name, distance in steps (one step is the character's
width) and one of eight screen directions. Places are listed once you have seen them; quest
markers, enemies and other players as the game shows them. Rooms are the game's minimap
rooms: "room N" once entered, "unexplored room N" when you have glimpsed it but never
entered (in maps without them, edges where unseen ground continues). As in D2R, the first
sighting of an exit, waypoint or the stash says "the radar has logged a new exit/place" with a
chime, the nearest unentered room hums from its nearest point, and entering one plays a short
cue. Keyboard: Ctrl+Numpad 8/2 and 4/6.
Hold **Create** (Share) for the mod menu: **Square** reads life, mana and potions, **Triangle**
reads your gold (keyboard: Ctrl+G), **Circle** reads your level, experience percentage, unspent passive points, skills that can level up and the zone (keyboard: Ctrl+L), **Cross** starts the beacon toward the selected entry (again cancels). While Create is held the
game's button actions pause, so Square and Cross do not cast or interact. The beacon pings
toward the route through ground you have already explored, faster when closer, and refuses
when no explored route exists (keyboard: Ctrl+B). In menus the right stick keeps reading
details. Without a button,
[enemy cues](reference/enemy-cues.md) growl for up to three
nearby enemies in sight (a deeper swoop for a boss), panned on screen and faster
when closer; `[Enemy cues]` in the configuration turns them off or adjusts them.
Each gold pickup or gold reward is spoken with its amount ("50 de oro"), except at
vendors, which announce the new total ([gold](reference/vitals-api.md#gold-pickups-2026-10-08));
level ups and new passive points or skill points are announced as they happen
([progression](reference/progression.md)).

[Natural world sounds](reference/natural-world-sounds.md) reuse recordings from
the D2R mod for loot and identified world interactions. Short repeating samples
share playback time with each other; radar knocks and breaths never wait behind
them. An opening breath that ends in a quick double pip leads toward an unexplored area. The sounds are embedded in the plugin; no separate audio installation
is required.
The previous bright coin and equipment cues are preserved as playable WAVs in
the [sound design reference](reference/sound-design.md). D2R recordings remain
the active sound set while those originals guide future, more realistic sounds.

Ambient NPC subtitles are read when their text is visible on screen or the NPC
is within interaction range. Nearby world services, wagons, gateways and the
statue of Eterra can be announced separately from the ground beneath the player.
Supported camp tents are named only when the player is inside their verified
canopy contour on the same walkable floor; leaving a tent receives a brief spoken
confirmation. Being near a merchant does not establish a shop interior.

## Requirements and build

- A local Last Epoch installation with the matching generated interop assemblies.
- BepInEx **6.0.0-be.788**, IL2CPP Windows x64, and Il2CppInterop **1.5.3**.
- A .NET SDK capable of building `net6.0`, plus the runtime required by the tests.
- The existing speech dependencies in `references/tolk/`: `TolkDotNet.dll`,
  `Tolk.dll` and `nvdaControllerClient64.dll`. NVDA is the tested screen reader.

The shared [engineering policy](PRINCIPLES.md) targets PRISM. This revision still
uses the centralized Tolk/NVDA adapter; migration to PRISM remains outstanding.
Game assemblies, decompiled game code, third-party DLLs and build outputs are
excluded from this repository.
The [speech connection](reference/screen-reader-lifecycle.md) retries automatically
if a reader starts or restarts after the mod; failed outdated messages are not replayed.

Follow the [Steam port notes](reference/steam-port.md) to set up the loader and
interop generation. The included `LeAccess.InteropRepair` preloader handles the
documented Unity 6.4 generated-type issue and is built with the main plugin.

From the repository root, with the game closed:

```powershell
dotnet build LeAccess/LeAccess.csproj
```

The default game directory is `D:\games\steam\steamapps\common\Last Epoch`.
Override it for another installation:

```powershell
dotnet build LeAccess/LeAccess.csproj -p:GameDir="D:\SteamLibrary\steamapps\common\Last Epoch"
```

A normal build deploys the plugin and its existing dependencies to
`BepInEx/plugins/LeAccess/`, and the repair preloader to
`BepInEx/patchers/LeAccess.InteropRepair/`. To compile without deployment:

```powershell
dotnet build LeAccess/LeAccess.csproj -p:SkipPluginDeployment=true
```

Settings are stored in `BepInEx/config/LeAccess.cfg`. Translation overrides are in
the plugin's `Translations/` directory; builds preserve existing catalogs.
See [localization instructions](reference/localization.md).

A complete copy-to-game-folder test ZIP can be assembled with the pinned loader,
bundled runtime, plugin and repair preloader. See [test packaging](reference/test-package.md).
Its first launch generates matching interop and may download Unity base libraries.
It contains no saved games, account data or personal configuration.
Generated test ZIPs are kept locally in the repository root and excluded from Git.
Extract the package beside `Last Epoch.exe`, open NVDA, and launch through Steam;
no BAT launcher is required.

## Validation

Standalone test projects under [tests/](tests/) exercise state tracking,
ownership, visibility, formatting, navigation geometry and announcement order.
For example:

```powershell
dotnet run --project tests/LeAccess.Dialogue.Tests -p:SkipPluginDeployment=true
dotnet run --project tests/LeAccess.LoginTransition.Tests -p:SkipPluginDeployment=true
```

The [native hook audit](tests/LeAccess.PatchSafety.Tests/) also requires the local
game installation. Some map tests use local game references; consult the linked
feature notes for their fixtures. Host tests do not establish audible behavior
in a running game.

The optional Python map-extraction tools use UnityPy and Shapely with local game
assets. Their decoder tests run with `python tests/MapSectionExtraction/test_navmesh.py`.
These development tools are separate from the plugin's runtime dependencies.

## Known limits

This is an active development snapshot. The latest character-name typing and
return fixes are user-confirmed. Other menu, manual inspection and ambient
subtitle changes still need controller/listening checks.
Speech events with authored text are readable; audio-only character vocalizations
have no transcript. Other-player naming needs live online validation, and complete
stash/vendor/crafting navigation remains an area for testing.

Map descriptions are partial: the catalog covers 750 guarded sections across
58 layouts, with additional runtime readers. This does not establish complete
map coverage or names for every room, street or crossing. Map-entry summaries
and generic ground descriptions default to off. See the
[map coverage audit](reference/map-coverage-audit.md) and
[current feature status](STATUS.md).
