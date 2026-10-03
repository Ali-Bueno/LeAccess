# Validation history through the early 2026-10-03 audit

This is historical evidence moved from STATUS.md when Steam build 25690669 was
validated. Counts, binaries and planned listening checks below belong to their
recorded build; current installation and outstanding work are in [STATUS.md](../STATUS.md).

**Latest validation:** all 49 standalone C# suites passed, totaling 57,214 checks
(including 51,203 catalog checks), plus 24 missing-audio fallback checks and 20
Python navigation-decoder tests. The installed native audit still validates
59 declarations / 58 targets. Main build/deploy has zero warnings/errors;
installed and built hashes match above, and personal configuration is unchanged.
The [menu audit](menu-visibility.md) records coverage and runtime gaps;
[equipped-item evidence](equipped-items.md) explains the Head/Hands fix.
The [sound design reference](sound-design.md) preserves the original
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
scheduling. [Automatic orientation](automatic-orientation.md) follows
only verified routes and never moves the character. [Tent presence](runtime-map-places.md#exact-tent-presence-and-departure-2026-10-02)
uses extracted canopy contours; merchant proximity does not establish a shop
interior. The user's configuration and the older friend ZIP remain unchanged.
No new native hooks or runtime dependencies; no game was opened.

**Navigation/quest follow-up:** 436 radar, 147 orientation geometry/route/service,
90 native target-reader, 78 automatic quest-update, 124 journal, 330 localization,
79 input-binding, 73 menu, 44 radar-audio and 31 native-safety checks pass.
The hook audit still covers 59 declarations / 58 targets; no hooks or runtime
dependencies were added. Main build/deploy is clean and hashes match above.
See [orientation](objective-orientation.md), [quest updates](quest-updates.md)
and [radar corrections](navigation-radar.md). The old quest ping remains
removed. A displayed marker is not proof of an exact completion point or that an
unlinked exit leads to the current quest. No game was opened.

**Previous listening follow-up:** 73 menu, 63 settings-source, 79 shared binding,
171 skill, 207 runtime-place, 150 map-service, 66 generated-room and 323 section-state
checks pass. Map-data validation remains 51,203 checks; native safety passes
31 checks / 59 declarations / 58 targets. No new native hooks.
See [settings](settings-reading.md), [first skill opening](skill-assignment.md),
[camp follow-up](runtime-map-places.md) and [test packaging](test-package.md).
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
unchanged. No game launched. See [manual controls and evidence](skill-inventory-buttons.md).

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
[Per-scene provenance](map-section-build-25672295.json) records scene,
navigation, script and output hashes for all 58 layouts.
[World-service evidence](world-services.md) verifies the camp waypoint
and Isfarger merchant. The reader also supports the current typed stash/forge
interactions, but their presence in Z32 has not been established; the obsolete
stash menu enum is explicitly rejected. No tavern interior was established.
The Rogue opening story trigger inspected in Z12 carries FMOD audio without text;
see [subtitle source limits](npc-subtitles.md). Movie caption listening
and [manual item properties](item-tooltip-model.md) still need user checks.
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

- [Steam migration and launch/validation details](steam-port.md)
- [Early mode selection and startup transition guard](offline-startup.md)
- [Focus recovery after Start / Continue](pause-focus.md)
- [First Steam gameplay feedback and fixes](steam-gameplay.md)
- [Radar and world sounds](navigation-radar.md)
- [Earlier runtime reports and D2R behavior](runtime-feedback.md)
- [Nearby NPC speaker-name sources](npc-names.md)
- [Nearby other-player names](nearby-player-names.md)
- [Mod localization and translator instructions](localization.md)
- [Additional menu reading audit](menu-reading-audit.md)
- [Additional tooltip reading audit](tooltip-reading-audit.md)
- [Creation class/mastery descriptions and installed prefab](creation-menu-reading.md)
- [Manual item details from native presentation data](item-tooltip-model.md)
- [Merchant prices, properties and localized tabs](merchant-reading.md)
- [Skill primary identity, assigned buttons and learning requirements](skill-assignment.md)
- [Skill tooltip implementation](skill-details.md)
- [Passive node implementation and native evidence](passive-reading.md)
- [Quest journal implementation and native evidence](quest-reading.md)
- [Character name validation speech](character-name-reading.md)
- [Life/mana behavior, controls and native API evidence](vitals-api.md)
- [Nearby landmark reading and finer-map investigation](map-landmarks.md)
- [Spatial section behavior and map-authoring workflow](map-sections.md)
- [Installed bridge geometry, navigation heights and Spanish identity](map-section-data.md)
- [Static map generator, accepted coverage and rejection rules](map-section-generation.md)
- [Complete installed scene inventory and remaining gaps](map-coverage-audit.md)
- [Scene-independent live physical place reader](runtime-map-places.md)
- [Verified player class/form contact capability](player-map-contact.md)
- [Installed navigation format and offline extraction tests](map-navigation-format.md)
- [Generated rooms and native layout connections](pcg-map-sections.md)
- [Current ground reader and verified surface identities](map-surfaces.md)

Preserve existing uncommitted work and the previous installation. User will test the remaining gameplay features later; do not launch the game without a new request.
