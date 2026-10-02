# LeAccess

LeAccess is a screen-reader accessibility mod for **Last Epoch**, built as a
BepInEx 6 IL2CPP plugin for Windows. The current target is Steam **1.5.0.1**,
build **25663618**, with Unity **6000.4.8f1**.

See [STATUS.md](STATUS.md) for confirmed behavior, pending listening checks and
the latest installed build. Technical findings and reproduction notes live in
[reference/](reference/).

## Accessibility features

- Menu focus, settings, character classes, mastery descriptions and notifications.
- Skill and passive details, equipped-skill buttons, inventory actions and quest
  journal reading.
- Visible dialogue, examination descriptions, NPC subtitles and cinematic text.
- Nearby NPC, enemy, loot, destination and other-player names.
- Health/mana feedback and directional navigation and world-object audio cues.
- Local map sections, supported physical places and generated-room information.
- English and Spanish mod speech, following the game language, with editable
  translation catalogs and per-key fallback.

The startup guard retains an early online/offline button press until the game's
loading transition has finished. Menu reading preserves native control mappings;
HUD button hints remain silent during exploration.

In inventory and skill menus, push the **right stick up** to reread the selected
object or skill's details. Return the stick to neutral before repeating the
gesture. Skill rereading also refreshes the current displayed combat statistics.
Item inspection can use the game's tooltip formatter when the visual tooltip is
unavailable, including modifiers, lore and requirements.
Character-name screen keyboards require an explicit click or submit on the name
field; moving focus to it does not start editing.

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
