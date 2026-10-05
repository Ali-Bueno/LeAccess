# STATUS — Last Epoch

**Updated:** 2026-10-05 night (Claude session): the inventory and every gold vendor (shop, gambler, faction
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
All 58 host suites pass; the rebuilt plugin is installed for Steam **25690669**.
Startup, online play and listening on this build still need in-game validation.

## Current installation

- **Game:** Steam build **25711439** (version 1.5.1.2) since 2026-10-04 15:18, Unity **6000.4.8f1**, IL2CPP metadata **39**. BepInEx regenerated interop at 16:28 on the user's launch; the repair patcher fixed the same 16 rows in five assemblies.
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
| Native hook safety | 46 checks; build 25690669, 72 declarations / 71 distinct native targets; two new hooks (2026-10-04 afternoon: `SkillsPanelManager.HandleNavigateInput` prefix, `EpochInputManager.ControllerShouldBeActive` postfix) first relative branch +0x13/+0x15. Audit on 25711439 after regeneration: 46 checks, 75 declarations / 74 native targets. Hooks whose original runs must not start with a relative branch: the passive navigate prefix and three tiny bodies now replay the native code instead ([detour crash](reference/runtime-feedback.md#passive-navigation-crash-detoured-short-branch-prologue-2026-10-04)). |
| Reader recovery | Bounded reconnection; covered readers acknowledge accepted output and retry current content. 41 transport checks plus service regressions. [Scope/limits](reference/screen-reader-lifecycle.md); listening pending. |
| Early online/offline selection | Empty native nullable no longer throws during readiness polling or erases the first click; observation failures retain intent, dispatch remains once-only. 56 checks; fresh startup listening pending. |
| Menus/focus | Screen-reader keys (Ctrl, Shift, Alt, Insert, CapsLock, Windows) keep controller mode, so menus no longer lose focus and reset ([cause](reference/runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04)). Settings sliders/toggles speak only the new value while focus stays; headings no longer repeat after dropdowns; popups never speak raw object names; controller button names localized; zero-height labels that render (main-menu "Partida sin conexión") are read again. Native mediator callbacks follow FocusedPanel; typed forge/material selections and background/faded guards. 307 menu + 63 settings-source checks. [Audit and limits](reference/menu-visibility.md); listening pending. |
| Mod localization | Automatic game language, en/es catalogs, override and per-key fallback; 376 checks. |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Generic modals (loot-filter editor, pickers, stash setup) read title/message/static text once on open; scrolling and control captions stay silent; quick notices speak inside them. Hidden/faded modals release focus; tutorials yield to dialogue. Rejected output retains current content and modal priority. 232 screen-content + 118 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Dungeon/encounter instructions | Native dungeon/modal and visible echo/Morditas text; dynamic recovery preserves pacing. 211 screen-content + 90 dynamic checks. [Windows](reference/dungeon-warning-reading.md), [sources/limits](reference/dynamic-objectives.md); listening pending. |
| Visible subtitles | Distinct dialogue turns and current rendered/submitted lines recover after rejection; stale or expired content is discarded. The game's automatic response/continue focus waits for the NPC line, also when the response menu replaces the continue button (2026-10-04); the continue button reads "Continuar" ([details](reference/npc-subtitles.md)). 86 dialogue + 166 subtitle checks. Inspected Rogue opening narrative remains audio-only. |
| Hints/notifications | Button-first hints retained. Countdown reads once per restart with menu/typing priority; rejected batch output keeps current sources pending. 79 binding/prompt + 115 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option; fully faded wheels/sectors cannot speak retained models. 40 checks. Listening pending. |
| Classes/masteries/realm | Character-select tiles read name, level/mastery, cycle and hardcore tag; character-sheet stat lines read name and value ([sheet](reference/character-sheet.md)). Season/legacy labels; descriptions manual. Disabled retained carousel and background mastery rejected. 102 checks; listening pending. |
| Skills/assigned buttons | Verified assignments and learned/unlock state; rejected briefs/details retry the current owned selection. The overview is a grid: slots, base class and each mastery as rows, row name on entry, "N de M", no wrap; empty/locked slots speak ([overview](reference/skill-tree-navigation.md#skills-overview-grid-2026-10-04)). Specialization trees are a grid by branch ("Rama N"); opening focuses the tree's own root ([tree navigation](reference/skill-tree-navigation.md)); Up from the root reaches the tree's buttons, and specialising, spending/removing points and their state are spoken ([modifying](reference/skill-tree-navigation.md#modifying-a-skill-with-a-controller-2026-10-04-static-analysis)). Square carries the highlighted skill into the chosen slot's list ([assignment](reference/skill-assignment.md#square-carries-the-highlighted-skill-2026-10-04)). 263 checks; listening pending. |
| Passive nodes | Native name/description; overview on opening, tree name/points before a new tree's first node, unmet thresholds, skill strip. The controller panel (`PassivePanelManager`) is covered too; the grid is navigated by column ("Columna N, requiere X puntos", "N de M" in the column), leaving only from the first column; points left, opened columns and Cross refusals are spoken; the banner is a list ([banner](reference/passive-reading.md#mastery-banner-as-a-list-2026-10-05), [controller panel](reference/passive-reading.md#controller-panel-reading-order-and-tree-switching-2026-10-04), [skill-tree rules](reference/passive-reading.md#reading-like-the-skill-trees-2026-10-05)). 193 checks; listening pending. |
| World map/waypoints | Map selection reads location, level, "here", locked and quest markers; a quest's tweened location queues after its brief. Cross says "Viajando a X" or why travel is impossible instead of silence ([travel](reference/waypoint-map.md#cross-on-the-map-travel-2026-10-04)). 35 checks; listening pending. |
| Quest journal | Current state/objectives and manual history retained; rejected briefs/details retry only while selection remains owned. 136 checks; listening pending. |
| Automatic quest updates | Failed output preserves current pending changes, shared pacing and fair turns with dynamic objectives. 116 checks. [Evidence](reference/quest-updates.md). Listening pending. |
| Character name editing | User confirms the latest typing/return fix works. Circle keeps the name; physical entry and owned device transitions retain focus. Eight hooks, 160 checks. |
| Inventory/merchants | Current stash/shop/buyback and [equipped selections](reference/equipped-items.md); rejected briefs/details retry while owned. After a drop, focus moves to the nearest item instead of the first slot ([drop focus](reference/inventory-drop-focus.md)); holding compare reads the equipped item, differences and changed lines ([comparison](reference/item-comparison.md)). 236 service + 111 model checks. Listening pending. Backpack and merchant stock/buyback move item by item with "N de M", rows by Down/Up, no wrap ([navigation](reference/inventory-drop-focus.md#controller-navigation-2026-10-04)). |
| Forge/filter controls | Typed selections, filter-range labels and manual visible preview/potential/unavailable reason. 118 regressions within 296 menu checks. [Scope/limits](reference/crafting-reading.md); listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | Wall memory, owned body radius as before. 2026-10-04: terrain cues never wait behind NPC/enemy/object/beacon sounds (`max_wait_ms` in the status); openings toward a listed unexplored area end in a D2R double pip (`UnexploredMark`). 504 navigation checks. [Details](reference/navigation-radar.md#latency-and-the-unexplored-mark-2026-10-04); listening pending. |
| Objective orientation | Removed 2026-10-03 (F6/right stick down); replaced by the exploration scanner. Target/route classes kept for reuse. [Notes](reference/objective-orientation.md) |
| Automatic orientation | Removed 2026-10-03; replaced by the exploration scanner. [Notes](reference/automatic-orientation.md) |
| NPC/world sounds | Natural recordings for loot and typed chest/stash/portal/transition identities: Last Epoch's own for gold, potions, tomes, portals/waypoints and exits (user choice), D2R for equipment, chests and the stash ([sound design](reference/sound-design.md)); generic/NPC/radar cues retained. Gold, equipment, potions, tomes, chests, the stash, portals, waypoints and exits now play whole recordings as continuous loops (up to 8, from 12 m, `Loops`/`LoopRange`), replacing the 0.3 s excerpts that sputtered ([loops](reference/world-object-sounds.md)). 153 world + 167 sample/fallback checks. [Details](reference/natural-world-sounds.md). Listening pending. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. Now includes native tooltip subtitles and verified merchant role. The same NPC re-entering range within 5 s stays silent. 109 probe, 110 lifecycle and 59 audio checks; new service coverage needs listening. |
| Nearby player names | Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Optional continuous sonar now also mutes in non-pausing menus and after failed queries. 48 radar/audio checks. |
| Nearest enemy | Owned visual display/ActorData; 156 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Enemy cues | D2R port, on by default: ordinary enemies growl, nearest boss/miniboss swoops; max three streams in targeting range and walkable line of sight, faster when closer, camera-relative pan, shared audio queue with radar/NPC/world sounds; off-mesh bosses still sound, brief absences keep cadence. 141 checks; listening pending. [Details](reference/enemy-cues.md) |
| Exploration scanner | D2R port, right stick in gameplay; Create+Square vitals, Create+Cross beacon through explored ground. Since 2026-10-04 the D2R categories in D2R order (exit, portal, waypoint, unexplored room, room, quest, NPC, player, corpse/death place, stash, enemy, treasure, shrine, object, item), D2R speech, "the radar has logged a new exit/place" with chime, minimap rooms as "sala N"/"sala inexplorada N" with the D2R room loop and entry cue. Saved per character (`Exploration/*.bin`). Exploration 357, catalog 45, input 27 checks. [Design](reference/exploration-mode.md), [D2R categories](reference/exploration-mode.md#d2r-categories-2026-10-04). Offline scanner/beacon confirmed 2026-10-04; categories, sounds and online listening pending. |
| Enemy scan | F7/right-stick trigger removed 2026-10-03; probe, report and hostile registry (20 checks) kept as the scanner's enemies category. [Details](reference/enemy-scan.md) |
| Boss life | D2R port: bosses/minibosses read "Name, 74 percent" on band falls, death once as 0; on-screen bosses only, one step below 10 percent; first sighting silent, queued speech. 31 checks. Online bar scale (`IOverheadBar.HealthNormalized`) and live bosses unverified. [Details](reference/boss-life.md) |
| Life/mana | Falls read the band; one-band rises (regeneration between casts) stay silent, ending the 62/75 mana spam; low mana never interrupts and below 10 percent speaks only its first step and empty. [Rules](reference/vitals-api.md). Potion charges speak on use/pickup and in the R / Create+Square report (D2R belt port; Create alone no longer reports); the report also lists HUD debuffs; Create+Triangle / Ctrl+G reads gold ([gold](reference/vitals-api.md#gold-2026-10-05)). 513 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Inactive destination metadata can name active forest/camp entrances; listener/range/destination guards remain. Verified non-NPC services no longer require a hover tag. 86 probe + 54 lifecycle checks; listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, independently re-extracted against installed build 25690669; 16 Monolith layouts included. Entry overview defaults off. Listening pending. |
| Live map places | Same-name places no longer repeat on footprint-edge flicker or adjacent pieces (5 s gap; alt-tab keeps memory). Failed approach queries cannot imply tent departure. Three verified canopies and shared online visual ownership retained. 264 runtime + 357 state + 164 service + 152 world checks. [Details](reference/runtime-map-places.md). Listening pending. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default. Shared online visual ownership, closest physical contact, same-floor and multi-foot consistency checks. 86 adapter checks. [Details](reference/map-surfaces.md). |
| Other gaps | Full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener; audio-only player lines lack authored text. |

## Validation and references

- 2026-10-05 13:45: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261005-134547.zip` (repo root, ignored by git), SHA-256 `EA128FC6D28E1FE20F12F9A01AF59B52E3261838FD160AFD341BC76B618453E5`, 264 files, game 1.5.1.2 / build 25711439; readme now lists skill modification, passive/banner rules and gold. Its `LeAccess.dll` (`3BDD2F412E0580F96ECE101923A4C31F1DED8B22FBBD31F5745B4EE49F6EB3BF`) is the installed copy. Package tests: 242 checks. Rebuilds of the same source can differ in bytes, so the package build's DLL was installed rather than the earlier one.
- Previous friend ZIP: `LeAccess-Steam-1.5.1-prueba-20261004-170437.zip`.
- All 60 test invocations pass, PatchSafety included (Skills 233, Passives 160, Localization 610, VitalsInput 31 checks among them).
- User confirms in game: the grids and screen-reader keys work; the log shows `[Controller mode] kept ... (RightShift)`.

Current-build regeneration and native audit: [Steam port notes](reference/steam-port.md).
Newly covered native windows: [dungeon/warning reading](reference/dungeon-warning-reading.md).
Previous validation records and detailed listening checklist were moved to
[validation history](reference/validation-history-20261003.md); their counts and
binary hashes describe earlier builds. Setup and controls remain in [README.md](README.md).
