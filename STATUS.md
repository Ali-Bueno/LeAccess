# STATUS — Last Epoch

**Updated:** 2026-10-03. Additional fixes for hidden menus, late passive names, stale item speech, quest-update fairness, audio cancellation and map contacts are installed. All 49 C# suites plus the missing-audio variant pass (57,238 checks, 67 added), as do 20 Python tests. D2R sounds remain active and original bright loot cues archived. Listening remains pending; no game launch. The older friend ZIP stays local.

**Repository snapshot:** Steam port, accessibility readers, en/es catalogs,
runtime map data, extraction tools, tests and investigation references are
included together. [README.md](README.md) documents setup and build requirements.
User-confirmed behavior and unverified follow-up fixes remain distinguished below.

## Current installation

- **Game:** Last Epoch **1.5.1**, Steam build **25672295** (updated at 15:04), Unity **6000.4.8f1**, IL2CPP metadata **39**.
- **Target:** `D:\games\steam\steamapps\common\Last Epoch`.
- **Loader:** **BepInEx 6.0.0-be.788**, Il2CppInterop **1.5.3**; `LeAccess.InteropRepair` now repairs Unity 6.4 orphan types automatically before loading interop.
- **Build/deploy:** `dotnet build LeAccess/LeAccess.csproj`; targets Steam by default. Add `-p:SkipPluginDeployment=true` while the game is running.
- **Installed DLL SHA256:** `10DB5CF61AD94CF0D38C915518F40F89558F57A188423EB0432706F8B995CA99`; build and deployed file match, zero compiler warnings/errors, personal config unchanged.
- **Speech:** existing centralized Tolk/NVDA; bundled NVDA client now loaded explicitly from the plugin directory.
- **Reference:** `last epoch code/` replaced from **1.5.1 / build 25672295** interop: LE 8046 C# files, Controls 11, Dialogue 537 and isolated DataStoreList IL. See `SOURCE.txt` and [migration notes](reference/steam-port.md).
- **Launch:** start the game normally through Steam; a BAT is unnecessary. Choose online or offline inside the game menu.
- Previous `D:\games\Last Epoch` installation retained. Save/config backup: `C:\Users\ali-b\Documents\LeAccess backups\steam-migration-20261001-182231`.

## Next step / blocker

**Next task:** listen to equipped slots (including swapping/removing an item), menu order, scrolling and popup return. Then check automatic route cadence/turns, natural loot/service sounds and entering/leaving a supported camp tent. The [menu audit](reference/menu-visibility.md) lists screen-specific gaps. **Do not open the game: the user tests independently.**

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
sections** in total, re-extracted and guarded for build **25672295**; geometry
matches the previous catalog, whose old build hash prevented loading after the update.
[Live place detection](reference/runtime-map-places.md) adds
bridges, stairs, docks, cave floors, tunnels, corridors, courtyards and supported
named rooms without a scene whitelist. Model footprints also identify nearby
tents, wagons, gateways and the statue of Eterra, with same-floor and reachable
approach checks. Native interactions identify merchants, stash, forge and
waypoints; a generic map-opening action alone does not establish a waypoint.
All [eight class/form rigs](reference/player-map-contact.md)
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
| Steam port | Loader/NVDA verified. Latest native log confirms online character creation, matchmaking connection and InGame; broader online feature validation remains incomplete. |
| Native hook safety | 31 checks; 59 declarations / 58 distinct native targets validated against installed game. |
| Early online/offline selection | Empty native nullable no longer throws during readiness polling or erases the first click; observation failures retain intent, dispatch remains once-only. 56 checks; fresh startup listening pending. |
| Menus/focus | Invisible modal ownership and popup-over-dialogue details corrected; labels/descriptions follow visual order. 168 menu + 63 settings-source checks. [Audit and limits](reference/menu-visibility.md); listening pending. |
| Mod localization | Automatic game language, en/es catalogs, override and per-key fallback; 366 checks. |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Fully faded/culled modal content releases focus; mixed text follows screen positions and known draw order. Native nested modals remain independent. 94 checks. |
| Visible subtitles | Creation/movie captions have their own lifecycle. Ambient bark UI requires actual on-screen text, NPC interaction range or the player's own source; rejected distant lines cannot escape through the submitted-message fallback. 63 dialogue + 143 subtitle checks. Inspected Rogue opening narrative is audio-only. |
| Hints/notifications | Button-first hints; item actions accompany selection instead of separate delayed hints. Native equip rejection notices allowed after brief focus speech. 79 binding/prompt + 50 notification checks. |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option; fully faded wheels/sectors cannot speak retained models. 40 checks. Listening pending. |
| Classes/masteries/realm | Season/legacy labels; descriptions manual. Disabled retained carousel and background mastery rejected. 102 checks; listening pending. |
| Skills/assigned buttons | Pending references in other slots no longer hide verified assignments; readiness can recover within the first opening. Learned/unlock state retained. 171 checks; listening pending. |
| Passive nodes | Native builder supplies name/description; initially empty names retry at a bounded rate instead of remaining cached and silent. 85 checks; listening pending. |
| Quest journal | State follows title; active objectives named explicitly. Prior completed/failed objectives are labeled as this quest's history and read only manually, including UI fallback. 124 checks; listening pending. |
| Automatic quest updates | Progress/completion coalesced through menus/dialogue; fair rotation between lists and FIFO within each list prevent starvation. Configurable pacing. 85 checks. [Evidence](reference/quest-updates.md). Listening pending. |
| Character name editing | User confirms the latest typing/return fix works. Circle keeps the name; physical entry and owned device transitions retain focus. Eight hooks, 160 checks. |
| Inventory/merchants | [Equipped slots](reference/equipped-items.md) read worn items/actions; manual inspection rechecks foreground and selection immediately before speech. 178 service + 102 model checks; listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | Initial/resumed wall memory prevents false openings on retreat; owned online visuals supply body radius. 436 navigation checks; sound/control vocabulary unchanged. Listening pending. |
| Objective orientation | Right-stick-down/F6: visible tracked objective or named exit, verified path corner or explicit bearing-only fallback, distance/progress. 147 service/route + 91 target checks. [Controls and limits](reference/objective-orientation.md). Listening pending. |
| Automatic orientation | Natural pulses and target/turn/proximity speech; verified routes only. Ineligible queued pulses release their audio turn after target changes. 353 checks. [Details](reference/automatic-orientation.md). Listening pending. |
| NPC/world sounds | Natural D2R recordings for loot and typed chest/stash/portal/transition identities; generic/NPC/radar cues retained. 149 world + 167 sample/fallback checks. [Details](reference/natural-world-sounds.md). Listening pending. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. Now includes native tooltip subtitles and verified merchant role. 101 probe, 109 lifecycle and 59 audio checks; new service coverage needs listening. |
| Nearby player names | Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Optional continuous sonar now also mutes in non-pausing menus and after failed queries. 48 radar/audio checks. |
| Nearest enemy | Owned visual display/ActorData; 136 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Life/mana | D2R percentage bands, critical priority, R/Back/View manual report, offline/online sources and typed-input gates; 379 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Inactive destination metadata can name active forest/camp entrances; listener/range/destination guards remain. Verified non-NPC services no longer require a hover tag. 86 probe + 54 lifecycle checks; listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, regenerated against installed 1.5.1 assets; 16 Monolith layouts included. Entry overview defaults off. Listening pending. |
| Live map places | Failed approach queries cannot imply tent departure; inactive query owners are rediscovered. Three verified canopies and shared online visual ownership retained. 260 runtime + 346 state + 161 service + 152 world checks. [Details](reference/runtime-map-places.md). Listening pending. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default. Shared online visual ownership, closest physical contact, same-floor and multi-foot consistency checks. 86 adapter checks. [Details](reference/map-surfaces.md). |
| Other gaps | Full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener; audio-only player lines lack authored text. |

## References

**Latest validation:** all 49 standalone C# suites passed, totaling 57,214 checks
(including 51,203 catalog checks), plus 24 missing-audio fallback checks and 20
Python navigation-decoder tests. The installed native audit still validates
59 declarations / 58 targets. Main build/deploy has zero warnings/errors;
installed and built hashes match above, and personal configuration is unchanged.
The [menu audit](reference/menu-visibility.md) records coverage and runtime gaps;
[equipped-item evidence](reference/equipped-items.md) explains the Head/Hands fix.
The [sound design reference](reference/sound-design.md) preserves the original
coin/equipment WAVs and the preference for bright but more realistic future cues.
D2R remains the active set. No game or audio device was opened for validation.

The additional review reproduced failures before fixes and added 67 checks.
It covers invisible modal/radial ownership, visual description order, popup
details over dialogue, inventory focus loss, late passive names, fair quest
delivery, abandoned audio turns, sonar muting, false tent departures and floor
contact/online rig recovery. See the linked feature references for evidence;
live controller and listening validation remains the next step.

**Natural sounds and hands-free orientation:** nine short D2R recordings are
embedded in the DLL, with typed world-object identities and shared playback
scheduling. [Automatic orientation](reference/automatic-orientation.md) follows
only verified routes and never moves the character. [Tent presence](reference/runtime-map-places.md#exact-tent-presence-and-departure-2026-10-02)
uses extracted canopy contours; merchant proximity does not establish a shop
interior. The user's configuration and the older friend ZIP remain unchanged.
No new native hooks or runtime dependencies; no game was opened.

**Navigation/quest follow-up:** 436 radar, 147 orientation geometry/route/service,
90 native target-reader, 78 automatic quest-update, 124 journal, 330 localization,
79 input-binding, 73 menu, 44 radar-audio and 31 native-safety checks pass.
The hook audit still covers 59 declarations / 58 targets; no hooks or runtime
dependencies were added. Main build/deploy is clean and hashes match above.
See [orientation](reference/objective-orientation.md), [quest updates](reference/quest-updates.md)
and [radar corrections](reference/navigation-radar.md). The old quest ping remains
removed. A displayed marker is not proof of an exact completion point or that an
unlinked exit leads to the current quest. No game was opened.

**Previous listening follow-up:** 73 menu, 63 settings-source, 79 shared binding,
171 skill, 207 runtime-place, 150 map-service, 66 generated-room and 323 section-state
checks pass. Map-data validation remains 51,203 checks; native safety passes
31 checks / 59 declarations / 58 targets. No new native hooks.
See [settings](reference/settings-reading.md), [first skill opening](reference/skill-assignment.md),
[camp follow-up](reference/runtime-map-places.md) and [test packaging](reference/test-package.md).
The 19:40 log confirms native `isOnline: True`, server matchmaking/connection and
InGame. The user subsequently confirmed adding their email through the website;
there is no remaining reported account-registration concern.

**Friend's test ZIP (previous build; excludes the latest sound, orientation and menu changes):** `LeAccess-Steam-1.5.1-prueba-20261002.zip`, kept locally
in the repository root (36,449,013 bytes; 264 files; no BAT). SHA256:
`B238F1B8422734C60D4D8B28993A837273E700DD4EB9BD2FA8D14815F3C26ECC`.
242 packaging checks pass. All 228 official loader files are unchanged, and the
packaged main DLL matches the previous packaged hash below. ZIP CRC and exclusion checks
pass; no game launch/clean-machine boot was performed. Extract beside the game EXE;
first launch can require Internet to generate local interop.
The replacement ZIP is generated directly in the repository root without a BAT.
Future ZIPs also default to the repository root. Per the user's clarification,
ZIPs remain local and are excluded from the GitHub publication.

**Previous pre-publication baseline:** all **42 standalone C# suites passed**, totaling
**54,861 checks** (including 51,088 catalog checks), with zero failures. All 20
Python navigation-extraction tests also passed. That baseline predates this follow-up.

**Keyboard follow-up validation:** **369 checks** across name editing (160),
pause focus (134), menu reading (44) and native safety (31). All 59 declarations /
58 distinct native targets validate. Main build/deploy has zero warnings/errors;
installed and built hashes match, config unchanged. No game was opened.
The earlier menu/subtitle baseline passed 1,461 checks across 20 suites before
this close/focus follow-up.

**Previous menu follow-up:** **1,660 checks across 19 suites**, including classes
(101), skills (123), passives (69), quests (115), inventory (97), subtitles (94),
notifications (50), menu focus (47), bindings (78), and existing keyboard,
dialogue, localization, model and hook regressions. No new native hooks.
Main build/deploy: zero warnings/errors; built/installed hashes match; config
unchanged. No game launched. See [manual controls and evidence](reference/skill-inventory-buttons.md).

The session ending 13:01:22 confirms unsolicited skill tooltip speech, inconsistent
Ráfaga assignment and 15 unprepared inventory action hints. The fixes above target
those sources. The prior equip attempts have no recorded rejection reason; the
game's visible failure notices are now allowed while inventory stays open.

The user then reported continued passive silence, unreadable merchants and false
unassigned skills. The 15:23 log confirms generic passive Sprite focus, Ráfaga
“No equipada”, and unavailable shop details. The current correction fixes the
underlying blank passive model, absent SHOP fallback and explicit primary-ability
relationships. Inventory actions now accompany each item without waiting for a
visual tooltip; full properties remain manual. Latest focused checks: skills 143,
passives 82, inventory 111, item/merchant models 92, bindings 79, menu 48,
localization 280, hook audit 31, interop repair 206 and first-click startup 56. These are source/model
checks, not an audible runtime confirmation.
The same follow-up separates quest history (124 checks) and filters floating
subtitles by their actual screen rectangle or interaction range (143 checks;
63 dialogue and 71 screen-content regressions also pass). D2R's verified model
footprint approach informed the camp scenery reader: 202 runtime-place and 145
map-service checks, plus 66 generated-room and 61 surface regressions. Camp
service/title coverage adds 101 NPC-probe and 86 landmark-probe checks; 54
landmark lifecycle checks remain applicable. No new native hooks were added.
The fixed map catalog was re-extracted from the installed assets and passes
51,203 data checks, including current-build acceptance and old-build rejection.
[Per-scene provenance](reference/map-section-build-25672295.json) records scene,
navigation, script and output hashes for all 58 layouts.
[World-service evidence](reference/world-services.md) verifies the camp waypoint
and Isfarger merchant. The reader also supports the current typed stash/forge
interactions, but their presence in Z32 has not been established; the obsolete
stash menu enum is explicitly rejected. No tavern interior was established.
The Rogue opening story trigger inspected in Z12 carries FMOD audio without text;
see [subtitle source limits](reference/npc-subtitles.md). Movie caption listening
and [manual item properties](reference/item-tooltip-model.md) still need user checks.
Broader live map coverage and multiplayer proximity remain unverified.
Final main build/deploy: zero warnings/errors. Built and installed DLL hashes
match; the user's config is unchanged. The game was not launched.
Previous packaged DLL SHA256: `9D9B3C0687C2592A326CFE062732F3074A7AA778B9342795A37CA85FC4518025`.

Follow-up listening: the remaining menu/manual-detail checks above, then
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
- [Creation class/mastery descriptions and installed prefab](reference/creation-menu-reading.md)
- [Manual item details from native presentation data](reference/item-tooltip-model.md)
- [Merchant prices, properties and localized tabs](reference/merchant-reading.md)
- [Skill primary identity, assigned buttons and learning requirements](reference/skill-assignment.md)
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
