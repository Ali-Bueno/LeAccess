# STATUS — Last Epoch

**Updated:** 2026-10-08 (Claude session, user away): physical keyboard types from the first key once Cross opens
the screen keyboard, Enter/Escape leave the field ([details](reference/character-name-reading.md));
unread menus covered: [endgame panels](reference/endgame-panels.md) and [faction/vendor/service panels](reference/faction-vendor-panels.md);
the scanner lists every enemy on screen and "cadáver" is now "última muerte" ([details](reference/exploration-mode.md));
gold pickups are spoken ("50 de oro", [details](reference/vitals-api.md#gold-pickups-2026-10-08)); level ups, new
points and Create+Circle / Ctrl+L status ([details](reference/progression.md)). Later the same day: offline skills read
"Equipada" and bar slots again (a gate on `ComboAbilityList.HasInitialised`, set only after an ability is used;
[details](reference/skill-assignment.md)); Weaver tree, Forgotten Knights timelines, Observatory prophecies, Bazaar
listings, Ladder rows and Morditas ritual buttons are read. Afternoon: dropdowns everywhere read "N de M" and
selected state and say a new value once; Bazaar my stall and listing form; Observatory lens effects; other players
say level (and a friend's character name) in the scanner and on approach ([details](reference/nearby-player-names.md)).
Not installed yet (the user was playing); all unheard; 61 host suites pass.
Earlier, 2026-10-07 evening: game build 25764382. Online exploration memory now saves
(key had no character identity online); inventory no longer jumps to the last item (hidden pointer hover);
crafting materials, "Regalar objeto" gifting, the game guide article and stash tabs are read
([inventory](reference/inventory-drop-focus.md), [crafting](reference/crafting-reading.md),
[gift](reference/social-reading.md), [guide](reference/guide-reading.md), [exploration](reference/exploration-mode.md)).
All unheard. Earlier 2026-10-07: co-op works again after restarts. Online skills now say
Equipada/No equipada and action-bar slots read their skill (the online root has no `Actor`;
[details](reference/skill-assignment.md#online-equipped-slots-2026-10-07-build-25711439)). New-character
checkboxes, Normal/Difícil and the realm banner speak their state; either mode button flips the mode
([details](reference/creation-menu-reading.md)).
Earlier, 2026-10-06: first co-op session worked (invite, accept, portal). Fixed: the
joining player's mod losing its own character ([details](reference/multiplayer-local-player.md)) and the
"Grupo" party overview reading raw names ([details](reference/social-reading.md)); listening pending.
Earlier, 2026-10-05 latest: L3/R3 were still silent: the game draws its older
tooltip, now read too, with a log line per tooltip mode ([details](reference/item-comparison.md#older-tooltip-2026-10-05)).
The forge opens with focus on the inventory: it is now announced there with "L2 entra en la forja", and
Square's move or refusal is spoken ([details](reference/crafting-reading.md#forge-from-the-inventory-2026-10-05)); listening pending.
Earlier, 2026-10-05 late: L3/R3/both tooltip modes ([details](reference/item-comparison.md#controller-modes-2026-10-05));
forge grid reader ([forge](reference/crafting-reading.md#controller-forge-2026-10-05)).
Earlier, 2026-10-05 night: the inventory and every gold vendor (shop, gambler, faction
and other vendors) say the gold on opening and after a purchase or sale ([details](reference/vitals-api.md#gold-2026-10-05)); listening pending. Create+Triangle /
Ctrl+G reads the gold in gameplay (**user confirms it works**).
Earlier, 2026-10-05 evening: the passive mastery banner is a list (base class, then
masteries, "N de M"), says which tree is shown or that Cross opens it, and no longer speaks twice
([details](reference/passive-reading.md#mastery-banner-as-a-list-2026-10-05)); **user confirms it works**.
Earlier, 2026-10-05: passive trees follow the skill-tree rules — exits only
from the first column, column labels with their point threshold, quiet open nodes, points left and
opened columns after a point, refusals on Cross ([details](reference/passive-reading.md#reading-like-the-skill-trees-2026-10-05));
**user confirms it works**.
Earlier, 2026-10-05: skills can now be modified by ear — Up from a tree's root
reaches Especializar/Respec, trees speak specialised state, unspent points and point removal mode,
and slot selection is announced ([details](reference/skill-tree-navigation.md#modifying-a-skill-with-a-controller-2026-10-04-static-analysis));
**user confirms it works**; despecialising (Respec → Olvidar especialización) not yet heard.
Earlier, 2026-10-04 afternoon: Ctrl/Shift/Insert no longer drop controller mode
and menu focus; the Skills panel, passive trees and specialization trees navigate as grids (row,
column or branch named on entry, "N de M" inside it). **User confirms it works perfectly** (game 1.5.1.2);
new friend ZIP built; see [WHATS_NEXT.md](WHATS_NEXT.md).
Earlier that day: list navigation with positions for passive trees,
skill trees and the backpack; a navigation-hook crash fixed and three similar hooks made safe;
before that, the exploration scanner now has the D2R categories,
speech, new-place announcements and room sounds; earlier that night, unexplored rooms from the
game's minimap, radar cues no longer waiting behind other sounds and the opening mark; see
[WHATS_NEXT.md](WHATS_NEXT.md).
Earlier, 2026-10-04: menu fixes from the user's report — map travel
speech, passive trees/mastery overview and skill strip, inventory focus after drops,
equipment comparison, NPC lines no longer cut by the response menu. Earlier, 2026-10-03: Spam fixes (vitals, places/NPCs, generic
modals, settings), D2R ports (potions, effects, boss life), enemy scan and audio cues, world-map
waypoints, character tiles and localized button names; see [WHATS_NEXT.md](WHATS_NEXT.md).
All 61 host suites pass (2026-10-08); the rebuilt plugin is installed for Steam **25764382**.
Startup, online play and listening on this build still need in-game validation.

## Current installation

- **Game:** Steam build **25764382** since 2026-10-07 14:16 (the server updated first, hence that morning's `SHA MISMATCH`); before it 25711439 (1.5.1.2) since 2026-10-04 15:18. RVAs in older reference docs belong to their stated build. Then: Unity **6000.4.8f1**, IL2CPP metadata **39**. BepInEx regenerated interop at 16:28 on the user's launch; the repair patcher fixed the same 16 rows in five assemblies.
- **Path:** `D:\games\steam\steamapps\common\Last Epoch`.
- **Loader:** BepInEx **6.0.0-be.788**, Il2CppInterop **1.5.3**; automatic updates remain enabled.
- **Interop:** regenerated offline from the installed binary/metadata, 214 DLLs and two native databases installed and byte-verified. The existing preloader repaired the same 16 Unity orphan rows. Native audit: 46 regressions, 60 declarations / 59 targets.
- **Build:** `dotnet build LeAccess/LeAccess.csproj`; use `-p:SkipPluginDeployment=true` while the game is running.
- **Speech:** centralized Tolk/NVDA, now with bounded reconnection; see [connection lifecycle](reference/screen-reader-lifecycle.md).
- **Maps:** 750 fixed sections across 58 layouts and three tent canopies independently re-extracted for the new build. [Provenance](reference/map-section-build-25690669.json).
- **Backup:** previous interop and plugin in `C:\Users\ali-b\Documents\LeAccess backups\steam-build-25690669-20261003`. Previous installation and personal configuration retained.
- **Previous plugin:** commit `8eae87e` installation and config preserved in `C:\Users\ali-b\Documents\LeAccess backups\pre-claude-audit-8eae87e-20261003`.
- **Launch:** normally through Steam; online/offline remains the game's choice. No game or audio device was opened by this audit.

## Next checks and remaining scope

**Handoff:** [WHATS_NEXT.md](WHATS_NEXT.md) lists this session's changes, the first
live checks for them, open work and the user's working rules.

**The user tests independently; do not launch the game without a new request.**
First check startup/NVDA reconnect during quest/vitals changes, forge manual
preview/unavailable reasons, and leaving/re-entering an active Morditas gauntlet
with its return arrow and abandonment notice. Also check rune/glyph selection,
stash/shop tabs, filter bounds and dialogue/tutorial handoffs.
Then check equipped items, passives, dungeon objectives, online speech,
route cadence/turns, world sounds and tent entry/departure.

The [menu audit](reference/menu-visibility.md) still leaves complete vendor/stash,
crafting and loot-filter workflows unverified. Current fixes preserve input,
player movement, network state and existing controller mappings. Visual-only
hazards without a verified text/state source remain a coverage gap. Arbitrary
kitchens, taverns, palace rooms and street names are not mapped; map overview and
ground-material speech remain disabled by default per the user's preference.
Compilation and host tests are evidence for their tested logic, not proof that
every screen or gameplay encounter has been heard successfully.
Next source investigations: advanced filter conditions/editing, unresolved forge
icons/cost ranges and critical-success presentation; see [verified control scope](reference/crafting-reading.md).
[Hazard sources](reference/hazard-sources.md) distinguish abandonment/return
guidance from still-unmapped visual attack telegraphs. Rejected-output recovery
now also covers quests, vitals and orientation; remaining general menu, zone,
map and nearby-entity readers still need review.

The older friend's ZIP remains local and unchanged; it does not include the new
fixes. Source updates are to be pushed to GitHub after each verified batch, as
requested. No release or new ZIP has been requested.

## Feature status

| Area | Current evidence / remaining work |
|---|---|
| Steam port | Earlier build 25672295 logged online creation, connection and InGame. Current hotfix compiles and passes native audit; startup/online listening pending. |
| Native hook safety | 2026-10-08: 81 declarations / 80 native targets; new `ButtonInputListener.CanBePressed` postfix (keyboard shortcuts blocked while typing; first branch a 32-bit call at +0xB). 46 checks; build 25690669, 72 declarations / 71 distinct native targets; two new hooks (2026-10-04 afternoon: `SkillsPanelManager.HandleNavigateInput` prefix, `EpochInputManager.ControllerShouldBeActive` postfix) first relative branch +0x13/+0x15. Audit on 25711439 after regeneration: 46 checks, 75 declarations / 74 native targets. On 25764382 (2026-10-07): 46 checks, 80 declarations / 79 native targets; new `ItemContainerUI.set_IsHovered` prefix, first relative branch +0x17. Hooks whose original runs must not start with a relative branch: the passive navigate prefix and three tiny bodies now replay the native code instead ([detour crash](reference/runtime-feedback.md#passive-navigation-crash-detoured-short-branch-prologue-2026-10-04)). |
| Reader recovery | Bounded reconnection; covered readers acknowledge accepted output and retry current content. 41 transport checks plus service regressions. [Scope/limits](reference/screen-reader-lifecycle.md); listening pending. |
| Early online/offline selection | Empty native nullable no longer throws during readiness polling or erases the first click; observation failures retain intent, dispatch remains once-only. 56 checks; fresh startup listening pending. |
| Menus/focus | 2026-10-08: endgame panels (dungeon/arena tiers and key slots, Aberroth, boss votes, Sanctuary, echo rewards, blessings, Monolith echoes, Nemesis, Morditas) and faction/vendor panels (faction choice, rank rewards, gamblers, special vendors with prices, item-service slots, time beast, loot-filter rules, mastery choice, bazaar costs); later: Weaver tree, Forgotten Knights timelines, Observatory prophecies, Bazaar listings/pages/results, Ladder rows, Morditas ritual buttons; afternoon: dropdown options "N de M"/selected and new value once (generic `DropdownReader`), Bazaar my stall and listing form, lens effects; not covered: listing-form slot focus, remote players' class ([endgame](reference/endgame-panels.md), [factions/vendors](reference/faction-vendor-panels.md)). Screen-reader keys (Ctrl, Shift, Alt, Insert, CapsLock, Windows) keep controller mode, so menus no longer lose focus and reset ([cause](reference/runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04)). Settings sliders/toggles speak only the new value while focus stays; headings no longer repeat after dropdowns; popups never speak raw object names; controller button names localized; zero-height labels that render (main-menu "Partida sin conexión") are read again. Native mediator callbacks follow FocusedPanel; typed forge/material selections and background/faded guards. 307 menu + 63 settings-source checks. [Audit and limits](reference/menu-visibility.md); listening pending. |
| Mod localization | Automatic game language, en/es catalogs, override and per-key fallback; 376 checks. |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Generic modals (loot-filter editor, pickers, stash setup) read title/message/static text once on open; scrolling and control captions stay silent; quick notices speak inside them. Hidden/faded modals release focus; tutorials yield to dialogue. Rejected output retains current content and modal priority. 232 screen-content + 118 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Dungeon/encounter instructions | Native dungeon/modal and visible echo/Morditas text; dynamic recovery preserves pacing. 211 screen-content + 90 dynamic checks. [Windows](reference/dungeon-warning-reading.md), [sources/limits](reference/dynamic-objectives.md); listening pending. |
| Visible subtitles | Distinct dialogue turns and current rendered/submitted lines recover after rejection; stale or expired content is discarded. The game's automatic response/continue focus waits for the NPC line, also when the response menu replaces the continue button (2026-10-04); the continue button reads "Continuar" ([details](reference/npc-subtitles.md)). 86 dialogue + 166 subtitle checks. Inspected Rogue opening narrative remains audio-only. |
| Hints/notifications | Button-first hints retained. Countdown reads once per restart with menu/typing priority; rejected batch output keeps current sources pending. 79 binding/prompt + 115 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option; fully faded wheels/sectors cannot speak retained models. 40 checks. Listening pending. |
| Classes/masteries/realm | Character-select tiles read name, level/mastery, cycle and hardcore tag; character-sheet stat lines read name and value ([sheet](reference/character-sheet.md)). Season/legacy labels; descriptions manual. Disabled retained carousel and background mastery rejected. 102 checks; listening pending. |
| Skills/assigned buttons | 2026-10-08: offline equipped status and bar slots fixed (read the action bar's own `AbilityBarIcon.playerAbilityList`, no `HasInitialised` gate; log `[Skill binding] source: …`); Skills 274. The user reports it seems to work (same day). Verified assignments and learned/unlock state; rejected briefs/details retry the current owned selection. The overview is a grid: slots, base class and each mastery as rows, row name on entry, "N de M", no wrap; empty/locked slots speak ([overview](reference/skill-tree-navigation.md#skills-overview-grid-2026-10-04)). Specialization trees are a grid by branch ("Rama N"); opening focuses the tree's own root ([tree navigation](reference/skill-tree-navigation.md)); Up from the root reaches the tree's buttons, and specialising, spending/removing points and their state are spoken ([modifying](reference/skill-tree-navigation.md#modifying-a-skill-with-a-controller-2026-10-04-static-analysis)). Square carries the highlighted skill into the chosen slot's list ([assignment](reference/skill-assignment.md#square-carries-the-highlighted-skill-2026-10-04)). 263 checks; listening pending. |
| Passive nodes | Native name/description; overview on opening, tree name/points before a new tree's first node, unmet thresholds, skill strip. The controller panel (`PassivePanelManager`) is covered too; the grid is navigated by column ("Columna N, requiere X puntos", "N de M" in the column), leaving only from the first column; points left, opened columns and Cross refusals are spoken; the banner is a list ([banner](reference/passive-reading.md#mastery-banner-as-a-list-2026-10-05), [controller panel](reference/passive-reading.md#controller-panel-reading-order-and-tree-switching-2026-10-04), [skill-tree rules](reference/passive-reading.md#reading-like-the-skill-trees-2026-10-05)). 193 checks; listening pending. |
| World map/waypoints | Map selection reads location, level, "here", locked and quest markers; a quest's tweened location queues after its brief. Cross says "Viajando a X" or why travel is impossible instead of silence ([travel](reference/waypoint-map.md#cross-on-the-map-travel-2026-10-04)). 35 checks; listening pending. |
| Quest journal | Current state/objectives and manual history retained; rejected briefs/details retry only while selection remains owned. 136 checks; listening pending. |
| Automatic quest updates | Failed output preserves current pending changes, shared pacing and fair turns with dynamic objectives. 116 checks. [Evidence](reference/quest-updates.md). Listening pending. |
| Social panel | Dedicated reader (rows, sections with counts, tabs, own username, region, username windows, player options, invitation prompt), social chat results and typing echo; 58 checks. [Details](reference/social-reading.md); listening pending. |
| Character name editing | 2026-10-08: with the screen keyboard open, physical keys type from the first press (controller mode kept, text through the keyboard's own `SubmitEvent`), Enter submits, Escape keeps the text like Circle; name and Social username fields; 198 checks, listening pending. Earlier: user confirms the latest typing/return fix works. Circle keeps the name; physical entry and owned device transitions retain focus. Eight hooks, 160 checks. |
| Inventory/merchants | Current stash/shop/buyback and [equipped selections](reference/equipped-items.md); rejected briefs/details retry while owned. After a drop, focus moves to the nearest item instead of the first slot ([drop focus](reference/inventory-drop-focus.md)); holding compare reads the equipped item, differences and changed lines ([comparison](reference/item-comparison.md)). 236 service + 111 model checks. Listening pending. Backpack and merchant stock/buyback move item by item with "N de M", rows by Down/Up, no wrap ([navigation](reference/inventory-drop-focus.md#controller-navigation-2026-10-04)). |
| Forge/filter controls | Typed selections, filter-range labels and manual visible preview/potential/unavailable reason. 118 regressions within 296 menu checks. [Scope/limits](reference/crafting-reading.md); listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | Wall memory, owned body radius as before. 2026-10-04: terrain cues never wait behind NPC/enemy/object/beacon sounds (`max_wait_ms` in the status); openings toward a listed unexplored area end in a D2R double pip (`UnexploredMark`). 504 navigation checks. [Details](reference/navigation-radar.md#latency-and-the-unexplored-mark-2026-10-04); listening pending. |
| Objective orientation | Removed 2026-10-03 (F6/right stick down); replaced by the exploration scanner. Target/route classes kept for reuse. [Notes](reference/objective-orientation.md) |
| Automatic orientation | Removed 2026-10-03; replaced by the exploration scanner. [Notes](reference/automatic-orientation.md) |
| NPC/world sounds | Natural recordings for loot and typed chest/stash/portal/transition identities: Last Epoch's own for gold, potions, tomes, portals/waypoints and exits (user choice), D2R for equipment, chests and the stash ([sound design](reference/sound-design.md)); generic/NPC/radar cues retained. Gold, equipment, potions, tomes, chests, the stash, portals, waypoints and exits now play whole recordings as continuous loops (up to 8, from 12 m, `Loops`/`LoopRange`), replacing the 0.3 s excerpts that sputtered ([loops](reference/world-object-sounds.md)). 153 world + 167 sample/fallback checks. [Details](reference/natural-world-sounds.md). Listening pending. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. Now includes native tooltip subtitles and verified merchant role. The same NPC re-entering range within 5 s stays silent. 109 probe, 110 lifecycle and 59 audio checks; new service coverage needs listening. |
| Nearby player names | 2026-10-08: username, friend's character name (friends cache only) and level (`clientCharacterInfoProvider`), also on scanner entries; Players 51. Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Optional continuous sonar now also mutes in non-pausing menus and after failed queries. 48 radar/audio checks. |
| Nearest enemy | Owned visual display/ActorData; 156 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Enemy cues | D2R port, on by default: ordinary enemies growl, nearest boss/miniboss swoops; max three streams in targeting range and walkable line of sight, faster when closer, camera-relative pan, shared audio queue with radar/NPC/world sounds; off-mesh bosses still sound, brief absences keep cadence. 141 checks; listening pending. [Details](reference/enemy-cues.md) |
| Exploration scanner | 2026-10-08: enemies category lists every living enemy on screen (before: skill-targeting range only); corpse category renamed "última muerte", a destination only. D2R port, right stick in gameplay; Create+Square vitals, Create+Cross beacon through explored ground. Since 2026-10-04 the D2R categories in D2R order (exit, portal, waypoint, unexplored room, room, quest, NPC, player, corpse/death place, stash, enemy, treasure, shrine, object, item), D2R speech, "the radar has logged a new exit/place" with chime, minimap rooms as "sala N"/"sala inexplorada N" with the D2R room loop and entry cue. Saved per character (`Exploration/*.bin`). Exploration 357, catalog 45, input 27 checks. [Design](reference/exploration-mode.md), [D2R categories](reference/exploration-mode.md#d2r-categories-2026-10-04). Offline scanner/beacon confirmed 2026-10-04; categories, sounds and online listening pending. |
| Enemy scan | F7/right-stick trigger removed 2026-10-03; probe, report and hostile registry (20 checks) kept as the scanner's enemies category. [Details](reference/enemy-scan.md) |
| Boss life | D2R port: bosses/minibosses read "Name, 74 percent" on band falls, death once as 0; on-screen bosses only, one step below 10 percent; first sighting silent, queued speech. 31 checks. Online bar scale (`IOverheadBar.HealthNormalized`) and live bosses unverified. [Details](reference/boss-life.md) |
| Progression | 2026-10-08: "Nivel N" on level up, new passive points, skills that can take a point, new skill unlock; Create+Circle / Ctrl+L status (level, XP percent, points, zone). Polled, online via `LocalPlayer`. 60 checks; listening pending. [Details](reference/progression.md) |
| Life/mana | Gold pickups and rewards say their amount ("50 de oro"), silent at vendors (2026-10-08, `[Vitals] AnnounceGoldPickups`). Falls read the band; one-band rises (regeneration between casts) stay silent, ending the 62/75 mana spam; low mana never interrupts and below 10 percent speaks only its first step and empty. [Rules](reference/vitals-api.md). Potion charges speak on use/pickup and in the R / Create+Square report (D2R belt port; Create alone no longer reports); the report also lists HUD debuffs; Create+Triangle / Ctrl+G reads gold ([gold](reference/vitals-api.md#gold-2026-10-05)). 513 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Inactive destination metadata can name active forest/camp entrances; listener/range/destination guards remain. Verified non-NPC services no longer require a hover tag. 86 probe + 54 lifecycle checks; listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, independently re-extracted against installed build 25690669; 16 Monolith layouts included. Entry overview defaults off. Listening pending. |
| Live map places | Same-name places no longer repeat on footprint-edge flicker or adjacent pieces (5 s gap; alt-tab keeps memory). Failed approach queries cannot imply tent departure. Three verified canopies and shared online visual ownership retained. 264 runtime + 357 state + 164 service + 152 world checks. [Details](reference/runtime-map-places.md). Listening pending. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default. Shared online visual ownership, closest physical contact, same-floor and multi-foot consistency checks. 86 adapter checks. [Details](reference/map-surfaces.md). |
| Other gaps | Full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener; audio-only player lines lack authored text. |

## Validation and references

- 2026-10-08 10:25: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-102546.zip` (repo root, ignored by git), SHA-256 `9D05028F9624162F5DB0440EE0CB8CE9950BA88F07F45A60B61996848FC6DD92`, 264 files; adds dropdown reading, Bazaar stall/listing, lens effects and other players' level/character name. Its `LeAccess.dll` starts `1AACEBEA26CC77B0`; NOT installed (the game was running): install with a normal build once the game is closed. 61 host suites and package 242 pass (MenuReading 535, Localization 1104, Players 51, Social 74).
- 2026-10-08 09:35: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-093520.zip` (repo root, ignored by git), SHA-256 `5A620F95718E893642293DE4B1C1EF9D42B4FA3CFA7036BB75E12CA524C5192B`, 264 files; adds the offline skill status fix and the Weaver tree, timelines, Observatory, Bazaar, Ladder and Morditas readers. Its `LeAccess.dll` (`F0683BD858E3086F…`) is the installed copy. 61 host suites and package 242 pass (MenuReading 497, Localization 1050, Skills 274).
- 2026-10-08 07:57: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-075719.zip` (repo root, ignored by git), SHA-256 `1AD7B8B24B50A5E8FF5C4373FFA0B0047A4C5C6613F4EE86ECFFCBE33C6DF64B`, 264 files, game build 25764382; adds physical typing in the screen keyboard, endgame and faction/vendor panel readers, scanner enemies on screen and "última muerte", gold pickups and progression; readme lists them. Its `LeAccess.dll` (`D63F19694E7BC2D9…`) is the installed copy. All 61 host suites and package 242 pass (MenuReading 423, Localization 974, CharacterName 198, Inventory 317, Progression 60, Vitals 101, PatchSafety 46 with 81 declarations).
- 2026-10-07 17:14: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261007-171427.zip` (repo root, ignored by git), SHA-256 `1708B9E3A6C144EEA4D030EEE342D99C79F82D15BCF5E1F2563A70AFA25920AB`, 264 files, game build 25764382; adds online exploration saving, inventory jump fix, materials, gifting, guide and stash readers; readme lists them. Inventory 317, MenuReading 336, Social 74, Forge 51, Localization 884, PatchSafety 46, Exploration 368, Skills 268, Classes 143, package 242. `tests/LeAccess.ItemTooltipModel.Tests` did not compile since 85bcaa7 (missing `CraftingMainUI` double); fixed 2026-10-08.
- 2026-10-07 12:30: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261007-123005.zip` (repo root, ignored by git), SHA-256 `F5F9CB869628C4833B2B23990D9C9D061485C1F94D3289800F63FEF0EBC2A68A`, 264 files; adds online skill equip status and action-bar slot reading. Skills 268.
- 2026-10-07 11:41: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261007-114140.zip` (repo root, ignored by git), SHA-256 `D45EC138BD7B55779F921BFC7B9BBDF89AEC1735133D8EA566198D180D280DC3`, 264 files; adds the creation choice states and stops counting the Friends loading placeholder as a player; readme explains the mode buttons and grouping rules. Package tests 242.
- 2026-10-06 16:08: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261006-160806.zip` (repo root, ignored by git), SHA-256 `26C92028BBEED2032FFEF7850E572ED8394ABFF3964CC1F0E722ED11AC9F8333`, 264 files; adds the co-op local-player fix and the party overview reader.
- 2026-10-06 15:30: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261006-153055.zip` (repo root, ignored by git), SHA-256 `A4CD7F82736D30722B10405A277A9596105E0259957D2CE3F550073A00BB8A1D`, 264 files, game 1.5.1.2 / build 25711439; adds L3/R3 comparison (older tooltip), forge from the inventory and the Social reader; readme lists them.
- 2026-10-05 13:45: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261005-134547.zip` (repo root, ignored by git), SHA-256 `EA128FC6D28E1FE20F12F9A01AF59B52E3261838FD160AFD341BC76B618453E5`, 264 files, game 1.5.1.2 / build 25711439; readme now lists skill modification, passive/banner rules and gold. Its `LeAccess.dll` (`3BDD2F412E0580F96ECE101923A4C31F1DED8B22FBBD31F5745B4EE49F6EB3BF`) is the installed copy. Package tests: 242 checks. Rebuilds of the same source can differ in bytes, so the package build's DLL was installed rather than the earlier one.
- Previous friend ZIP: `LeAccess-Steam-1.5.1-prueba-20261004-170437.zip`.
- All 60 test invocations pass, PatchSafety included (Skills 233, Passives 160, Localization 610, VitalsInput 31 checks among them).
- User confirms in game: the grids and screen-reader keys work; the log shows `[Controller mode] kept ... (RightShift)`.

Current-build regeneration and native audit: [Steam port notes](reference/steam-port.md).
Newly covered native windows: [dungeon/warning reading](reference/dungeon-warning-reading.md).
Previous validation records and detailed listening checklist were moved to
[validation history](reference/validation-history-20261003.md); their counts and
binary hashes describe earlier builds. Setup and controls remain in [README.md](README.md).
