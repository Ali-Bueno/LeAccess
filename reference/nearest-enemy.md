# Nearest enemy naming: offline correction

Investigated the installed Steam 1.5.0.1 build on 2026-10-02 without launching
the game. The user reports that offline enemies are still never named; earlier
mock tests did not reproduce the installed simulation/visual separation.

## Runtime evidence

The latest installed `BepInEx/LogOutput.log` records living hostile entries
within the real targeting range, with speech permitted, but no readable name:

- Line 955: `examined=1 living=1 in_range=1 nearest_name=unavailable`.
- Line 1094: `examined=3 living=3 in_range=2 nearest_name=unavailable`.
- Line 1169: `examined=3 living=3 in_range=3 nearest_name=unavailable`.

All report `registry=offline actors`, `range=10,00`, `nearest=none` and
`speech=ready`. This proves that the offline registry and gates worked during
those observations; failure occurred while resolving the selected enemy name.
It does not distinguish a missing direct display component from an empty name.

## Exact selected-actor sources

The former offline path only queried `Actor.GetComponent<BaseDisplayInformation>()`.
The corrected probe keeps that source, then reads these existing game links:

1. `Actor.actorSync` (`last epoch code/Actor.cs:1786`) is a direct cached field.
   `ActorSync.gameplayActor` (`ActorSync.cs:3172`) must be alive and have the exact
   selected Actor pointer before using `ActorSync.actorVisuals` (`:3187`). Its
   current active, living `BaseDisplayInformation` provides localized/custom names.
2. If that display is unavailable, the same `Actor.data` (`Actor.cs:1771`) supplies
   `ActorData.GetLocalizedActorName` (`ActorData.cs:1431`). The raw `actorName`
   must be nonblank before calling it; no technical object-name fallback is spoken.

Native `ActorData.GetLocalizedActorName` RVA `0x2775C60` tests `actorName` at
`0x2775C91`. The nonempty branch passes it to
`LocalizationActors.GetName` at `0x2775D30` (target `0x193B680`). The empty
branch uses Unity object name plus an internal suffix, so the probe excludes it.
Installed PermaLoad examples have authored actor names distinct from asset
names: `FrostSpriggan` → `Dryad Jailer` (path ID 44888106849937902),
`Decaying Spriggan A90 Dungeon` → `Hollow Spriggan`
(323019872934985939), `FrozenWerebear` → `Frozen Druid`
(2466895882444639130). These are source-data checks, not observed encounter names.

The probe deliberately does **not** call `Actor.GetActorSync` (RVA `0x2745D10`):
native inspection shows its missing-cache path can create/setup a sync object.
Reading `actorSync` directly adds no game mutation. Only the already selected
nearest enemy is named; a missing name never substitutes a farther enemy.
Registry selection, polling, cooldown, input gates and speech priority are unchanged.

## Verification and remaining listening check

`dotnet run --project tests/LeAccess.Combat.Tests --no-restore` passes **136 checks**.
Thirteen new service checks reproduce absent direct display with ActorData,
separate custom visual display, pooled mismatched links, collected wrappers,
empty displays, changing game localization, unnamed raw data and missing nearest
translation without borrowing a farther name. The main no-deployment build passes.

Bounded status logs now report `name_source=actor_display`, `owned_visuals` or
`actor_data`; an unavailable name reports whether linked visuals/data were present.
Successful delivery retains `[NearestEnemy] <name>`. The next user session should
confirm a name source and actual audible delivery. Mock tests and native analysis
do not establish that listening result.

Native scratch evidence is in `%TEMP%/LeAccess-player-footsteps-20261002/`:
`enemy-name-native.txt`, `disasm.py`, `map.json`, and `formdefs.json` (installed
PermaLoad source examples). RVAs derive from the installed
`BepInEx/interop/MethodAddressToToken.db`. No dependency, native hook, scene scan,
automatic targeting, movement, attack or game launch was added.

## Class word (2026-10-08)

A magic, rare or champion nearest enemy is spoken as "<name>, <class>" ("Gilgamesh el Corrupto, raro"); bosses
and normal enemies keep the plain name. See [enemy-class.md](enemy-class.md).
