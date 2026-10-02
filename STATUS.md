# STATUS — Last Epoch

**Updated:** 2026-10-02. The user confirms Cadáveres ardientes is named, but its examination body was silent. The response-menu reminder is now read. A separate early offline click failed during native startup; mode selection now waits for native readiness. Both fixes are deployed; listening remains required. Keep the game closed.

**Repository snapshot:** Steam port, accessibility readers, en/es catalogs,
runtime map data, extraction tools, tests and investigation references are
included together. [README.md](README.md) documents setup and build requirements.
User-confirmed behavior and unverified follow-up fixes remain distinguished below.

## Current installation

- **Game:** Last Epoch **1.5.0.1**, Steam build **25663618** (updated since initial port), Unity **6000.4.8f1**, IL2CPP metadata **39**.
- **Target:** `D:\games\steam\steamapps\common\Last Epoch`.
- **Loader:** **BepInEx 6.0.0-be.788**, Il2CppInterop **1.5.3**; `LeAccess.InteropRepair` now repairs Unity 6.4 orphan types automatically before loading interop.
- **Build/deploy:** `dotnet build LeAccess/LeAccess.csproj`; targets Steam by default. Add `-p:SkipPluginDeployment=true` while the game is running.
- **Speech:** existing centralized Tolk/NVDA; bundled NVDA client now loaded explicitly from the plugin directory.
- **Reference:** `last epoch code/` refreshed again for **1.5.0.1 / build 25663618**; see `SOURCE.txt` and [migration notes](reference/steam-port.md).
- **Launcher:** `Last Epoch Accessibility.bat` in the original game directory. Choose offline inside the new game menu.
- Previous `D:\games\Last Epoch` installation retained. Save/config backup: `C:\Users\ali-b\Documents\LeAccess backups\steam-migration-20261001-182231`.

## Next step / blocker

**Next check:** examine Cadáveres ardientes and hear its complete description before the response. The [owned dialogue reminder](reference/npc-subtitles.md) was missing from the reader. Also test an early "Jugar sin conexión" press: the [startup guard](reference/offline-startup.md) retains that choice until loading ends, instead of entering the recorded invalid transition. NPC approach names are now user-confirmed. **Do not open the game: the user tests independently.**

English/Spanish mod speech now follows the game's language, with editable catalogs
and English fallback. [Language testing](reference/localization.md) is pending.
The audited readers are now implemented: [passive nodes](reference/passive-reading.md),
[quest journal](reference/quest-reading.md), [name errors](reference/character-name-reading.md)
and [skill details](reference/skill-details.md). Native inspection confirmed passive
array indexing and quest ownership. All still need controller/listening validation.

[Life/mana reading](reference/vitals-api.md) now follows D2R's percentage bands,
with automatic loss/recovery announcements and critical interruption. Unmodified
R or controller Back/View reports both during gameplay. Settings are under
`[Vitals]`; automatic reading defaults on. No game was launched to test it.

[Nearby landmarks](reference/map-landmarks.md) now read authored signs and known
zone/dungeon destinations within interaction range. A separate
[spatial section reader](reference/map-sections.md) now combines the Camino
ancestral pilot with an extracted catalog for **57 additional layouts**, including
16 Monolith layouts with complete source/collider guards. There are **750 fixed
sections** in total. [Live place detection](reference/runtime-map-places.md) adds
bridges, stairs, docks, cave floors, tunnels, corridors, courtyards and supported
named rooms without a scene whitelist. All [eight class/form rigs](reference/player-map-contact.md)
have the required native feet. [Generated levels](reference/pcg-map-sections.md)
expose actual rooms, hallway tags, entrance/exit flags and layout connections.
The [complete scene audit](reference/map-coverage-audit.md) distinguishes evidence
from name-only candidates. Map-entry summaries and
[current-ground descriptions](reference/map-surfaces.md) are now optional and
disabled by default, per the user's preference for specific places while walking.
Kitchens, taverns, arbitrary palace rooms and street names remain unmapped.
Inspected intersection owners lacked floor geometry; their names alone cannot
establish local junction coverage. Connection counts do not prove open passages.

## Feature status

| Area | Current evidence / remaining work |
|---|---|
| Steam port | Loader/NVDA and InGame in Camino ancestral verified. Feature validation remains incomplete. |
| Native hook safety | 31 checks; 50 declarations / 49 distinct native targets validated against original. |
| Early online/offline selection | User's mode click deferred until native Login is ready; observed SystemLoading transition failure preserved in logs. 49 checks; fresh startup listening pending. |
| Menus/focus | Owned labels/settings and focus polling; initial dialogue response no longer interrupts same-frame body. 44 checks. |
| Mod localization | Automatic game language, en/es catalogs, override and per-key fallback; 216 checks including startup waiting speech. |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Current bindings added to identified tutorial notices; passive tutorials do not own gameplay input. 71 checks. |
| Visible subtitles | Owned response reminder now covers examination descriptions with null panels and typewriter effects. 63 dialogue + 38 ambient/cinematic checks; listening pending. |
| Hints/notifications | Gameplay HUD bar silent; menu actions deduplicated across device changes. 76 binding/prompt and 27 announcement checks; notification checks previously 47. |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option with interruption; 38 checks. Listening pending. |
| Classes/masteries | Current typed selection, localized descriptions/bonuses and interruption on navigation. 47 checks. Original ClassTab hierarchy and carousel description availability still need runtime verification. |
| Skills/assigned buttons | Descriptions user-confirmed; equipped status/current buttons included. First late tooltip appends only new details without interrupting. 100 checks; listening pending. |
| Passive nodes | Native-verified cell selection, localized name/points/details/requirements; 54 checks. Controller/listening pending. |
| Quest journal | Selected title/chapter, completion/pin states, owned objectives/rewards; 77 checks. Controller/listening pending. |
| Character name errors | Owned visible creation/local-slot validation messages, deduplication and keyboard/modal gates; 38 checks. Editing behavior still needs testing. |
| Inventory | Owned modern tooltip includes visible usable actions with current buttons/hold/overrides; 64 checks. Listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | Consistent sampled NavMesh origin, gameplay/death gates, 100 ms buffering retained; 357 navigation checks. Listening pending. |
| NPC/world sounds | Repeating gold/equipment/interactable cues; removed invalid loot click-listener requirement; 112 checks. Equipment filter rejection now distinguished in logs. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. 85 probe, 109 lifecycle and 59 audio checks; other NPC coverage still needs listening. |
| Nearby player names | Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Continuous sonar remains opt-in. |
| Nearest enemy | Owned visual display/ActorData; 136 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Life/mana | D2R percentage bands, critical priority, R/Back/View manual report, offline/online sources and typed-input gates; 379 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Missing listener reference now resolves exact same-object tag, as installed Z12/Z22 exits require; 95 checks. Listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, including 16 Monolith layouts. Entry overview defaults off. Listening pending. |
| Live map places | Latest Z22 log confirms rig ownership and floor contact, but visited Plane/Terrain names lack specific place semantics. 136 checks; broader named-place coverage remains unverified. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default per user request. Verified material research retained; 61 adapter checks. |
| Other gaps | Remaining character input interaction failures; full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener. |

## References

**Pre-publication validation:** all **42 standalone C# suites pass**, totaling
**54,861 checks** (including 51,088 catalog checks), with zero failures. The native
audit passes 31 regressions and validates 50 declarations / 49 targets against
the installed game. All 20 Python navigation-extraction tests also pass.
The full mod builds without warnings/errors with deployment
disabled. No game or audio device was opened for this validation. Individual
feature results and remaining listening limits are recorded in the linked references.

Build/deploy completed with zero warnings/errors. Installed and built DLL hashes
match; existing config hash stayed unchanged. The game remained closed. The
10:27 user log confirms the corpse name and attached dialogue UI with an unread
response reminder; the earlier failed startup survives in the preserved Unity
Player-prev log. These two fixes have not yet been heard in a live session.
Broader live map coverage and multiplayer proximity also remain unverified.
Installed main DLL SHA256: `2AEF1C43D3EF567746800E345D3446701938C683EBCE2375B7F22BBEF80275CC`.

Follow-up listening: examination body and early offline selection first, then
skill repetition, NVDA keyboard commands while using the controller, nearby
player names online, repeated world cues, menu bindings and map places. Detailed
checks and remaining coverage gaps are in each feature's linked reference.
Check R and Back/View, health/mana loss and recovery, zero health, online play
and a mana-replacing transformation. Life is separate from ward; negative mana
reports zero percent. Confirm shortcuts stay silent while typing/in menus.
Approach a named sign or exit, leave/re-enter range and open/close a menu nearby.
Confirm “Cerca” uses the correct destination and does not imply entering a room.
In Camino ancestral, cross both wooden bridges in both directions; expect
“Puente de madera 1/2” once on each entry and silence across collider seams.
Check `[Map section]` for anchor/layout mismatch if a bridge remains silent.
Check stairs/walkways on other mapped scenes and no chatter between adjoining pieces.
Check live docks, tunnels, corridors, cave floors and supported room floors;
try an echo, a sloped stair and a Druid form. A lower floor must not name the place.
Native tagged connectors should say "Pasillo" while crossing them.
Expect local place names while walking, with no map summary or ground-material labels.
In a generated cave/tomb, compare
room number, entrance/exit and connection count; a connection can still be blocked.
`[Map sections]` keeps `AnnounceSections=true`; `DescribeMapOnEntry=false` and
`AnnounceSurfaceChanges=false` match the requested behavior.

- [Steam migration and launch/validation details](reference/steam-port.md)
- [Early mode selection and startup transition guard](reference/offline-startup.md)
- [Focus recovery after Start / Continue](reference/pause-focus.md)
- [First Steam gameplay feedback and fixes](reference/steam-gameplay.md)
- [Radar and world sounds](reference/navigation-radar.md)
- [Earlier runtime reports and D2R behavior](reference/runtime-feedback.md)
- [Nearby NPC speaker-name sources](reference/npc-names.md)
- [Nearby other-player names](reference/nearby-player-names.md)
- [Mod localization and translator instructions](reference/localization.md)
- [Additional menu reading audit](reference/menu-reading-audit.md)
- [Additional tooltip reading audit](reference/tooltip-reading-audit.md)
- [Skill tooltip implementation](reference/skill-details.md)
- [Passive node implementation and native evidence](reference/passive-reading.md)
- [Quest journal implementation and native evidence](reference/quest-reading.md)
- [Character name validation speech](reference/character-name-reading.md)
- [Life/mana behavior, controls and native API evidence](reference/vitals-api.md)
- [Nearby landmark reading and finer-map investigation](reference/map-landmarks.md)
- [Spatial section behavior and map-authoring workflow](reference/map-sections.md)
- [Installed bridge geometry, navigation heights and Spanish identity](reference/map-section-data.md)
- [Static map generator, accepted coverage and rejection rules](reference/map-section-generation.md)
- [Complete installed scene inventory and remaining gaps](reference/map-coverage-audit.md)
- [Scene-independent live physical place reader](reference/runtime-map-places.md)
- [Verified player class/form contact capability](reference/player-map-contact.md)
- [Installed navigation format and offline extraction tests](reference/map-navigation-format.md)
- [Generated rooms and native layout connections](reference/pcg-map-sections.md)
- [Current ground reader and verified surface identities](reference/map-surfaces.md)

Preserve existing uncommitted work and the previous installation. User will test the remaining gameplay features later; do not launch the game without a new request.
