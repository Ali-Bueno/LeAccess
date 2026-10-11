# STATUS — Last Epoch

**Updated:** 2026-10-11: in-game settings menu with every option, a glossary of the mod's 38 sounds and per-kind volume
and distance (L1 while Create or L3 is held, Ctrl+F1; [menu](reference/mod-menu.md),
[levels](reference/world-object-sounds.md)); a release tester's report: scanner readouts and the beacon follow the explored
route ("260 steps by route, head down"), goals and arrival need a reachable point (the Fortress Walls storerooms)
([routes](reference/exploration-mode.md#route-aware-directions-and-reachable-goals-2026-10-11-tester-report)); mod button
Square = life, mana, level and experience, Triangle = gold and progress, Circle repeats the scanner entry; breakables in
the scanner ([layout](reference/exploration-mode.md#2026-10-11-tester-report-mod-button-layout-and-breakables)); separate
life/mana announcement options ([vitals](reference/vitals-api.md)); shrine names and effects ([shrines](reference/map-landmarks.md)).
Installed (game closed). Unheard; checks in [WHATS_NEXT.md](WHATS_NEXT.md).
Earlier, 2026-10-10 night: first report from a player outside the project; every item addressed. Name field echoes
typing ([echo](reference/character-name-reading.md#typing-echo-2026-10-10-user-report)); a lone dialogue response
repeats on a direction push and `[Dialogue] SpeakNpcLines` (default on) can silence voiced NPC speech
([dialogue](reference/npc-subtitles.md#optional-silence-for-npc-speech-2026-10-10-user-report)); Ctrl no longer arms
the intro skip, by polling, no hook ([intro](reference/runtime-feedback.md#intro-skipped-by-screen-reader-keys-2026-10-10-user-report));
"Entering …"/"Portaling to …" spoken ([transitions](reference/zone-reading.md#transition-pop-ups-2026-10-10-user-report));
character sheet summary ([sheet](reference/character-sheet.md#upper-half-summary-2026-10-10-user-report)); details
requests drop the position, `[Menus] AnnouncePositions`, keyword explanations, skill level and progress
([passives](reference/passive-reading.md#details-request-list-positions-and-keyword-text-2026-10-10-user-report),
[skills](reference/skill-tree-navigation.md#details-request-keyword-text-and-skill-level-2026-10-10-user-report));
inventory moves by row and column ([grid](reference/inventory-drop-focus.md#cell-based-grid-navigation-2026-10-10-user-report));
no false "higher level", towns fully routable, shrines and gold piles listed on screen
([scanner](reference/exploration-mode.md#2026-10-10-user-report-levels-towns-shrines-gold)); loot, landmarks and
enemies audible to the screen edge ([sounds](reference/world-object-sounds.md#2026-10-10-user-report-on-screen-objects-are-audible),
[enemies](reference/enemy-cues.md#2026-10-10-user-report-on-screen-enemies)). Installed (game closed). Unheard; checks in [WHATS_NEXT.md](WHATS_NEXT.md).
Earlier, 2026-10-10 morning: on pads other than DualSense/DualShock 4, L3 is the mod button (their Select opens
the controller menu); the overlay map is off on L3 in gameplay only, menus keep L3 ([details](reference/exploration-mode.md#l3-as-the-mod-button-on-non-playstation-pads-2026-10-10)).
README rewritten as player documentation for the first public test release. Installed. Unverified with an Xbox pad.
Earlier, 2026-10-10: world map says where the active main quest is, read live from quest state
([main quest](reference/waypoint-map.md#main-quest-on-the-map-2026-10-10)); login chat language and region and the
Settings language dropdown name their option ([rows](reference/settings-reading.md#untyped-dropdown-rows-language-and-region-2026-10-10-build-25816319));
mod speech follows a Settings language change live (verified natively, [localization](reference/localization.md)). Confirmed by the user.
Earlier, 2026-10-09 late night: equipment loops -3 dB, radar cues +3 dB (user request, combat masked the radar)
([levels](reference/sound-design.md)). Installed. Unheard.
Earlier, 2026-10-09 late night: rune, glyph and shard clicks take the one-shot headroom (were 10-15 dB under
gear) and each loot kind keeps a voice in a pile; shrines recognized by sounds and landmarks through the shrine manager
and visuals creator ([levels](reference/sound-design.md), [voices, shrines](reference/world-object-sounds.md)). Installed. Unheard.
Earlier, 2026-10-09 late night: generated caves (rift-beast nest from a fisura) — the scanner no longer lists the
parent scene's rooms or places; cave entrance and exits are named, listed as exits and sound like exits
([caves](reference/exploration-mode.md#generated-caves-2026-10-09)). Installed after the game closed. Unheard.
Earlier, 2026-10-09 night: mod speech in all ten game languages (French, German, Brazilian Portuguese, Polish,
Russian, Korean, Simplified Chinese, Japanese added with the game's own terms; Japanese `jp` recognized)
([localization](reference/localization.md)). Unheard.
Earlier, 2026-10-09 evening: the world-map list shows everything a sighted player sees: markers, icons, End of Time islands
([whole map](reference/waypoint-map.md#the-whole-map-as-a-sighted-player-sees-it-2026-10-09-user-request)). Unheard.
Earlier, 2026-10-09 afternoon: world-map list includes locked waypoints again (user request); the era name is no
longer cut by the place the map focuses after LB/RB ([map](reference/waypoint-map.md#counts-era-and-region-names-2026-10-09-user-report-2-de-15-with-about-5-places-eras-silent)). Unheard.
Earlier, 2026-10-09 11:27: the focused inventory item is no longer re-read when an online sync rebuilds the grid a few
seconds after a drop or equip ([details](reference/inventory-drop-focus.md#refresh-after-an-online-sync-2026-10-09)). Unheard.
Earlier, 2026-10-09 midday: world-map list counts only unlocked travel points, eras are spoken on LB/RB with
localized names ([counts](reference/waypoint-map.md#counts-era-and-region-names-2026-10-09-user-report-2-de-15-with-about-5-places-eras-silent)). Unheard.
Earlier, 2026-10-09 later: world map — with the quests hidden (Square), D-pad up/down snap from place to place
region by region with "N de M"; era tabs say title and position; hiding/showing quests is spoken
([map](reference/waypoint-map.md#list-navigation-with-the-quests-hidden-2026-10-09)); the epoch mastery panel
(Knowledge of Orobyss entry, timeline points, completion and travel) is read ([panel](reference/waypoint-map.md#epoch-mastery-panel-knowledge-of-orobyss-2026-10-09)). Unheard.
Earlier, 2026-10-09: radar openings are dynamic — past a wall's edge into space that goes on, from 3 m, so narrow
passages count and receding walls or niches do not ([radar](reference/navigation-radar.md#dynamic-openings-2026-10-09)).
Earlier, 2026-10-08 late night:
scanner targets on another floor say so; areas name their era when it changes ([eras](reference/zone-reading.md)). Unheard.
Earlier, 2026-10-08 night: equipped idols reachable and read ([idols](reference/idols.md)); rare/champion
enemies named with their class, elite cue and life narration ([classes](reference/enemy-class.md)); scanner moved to
Create + right stick and minion orders spoken ([minions](reference/minion-commands.md)); notes and lore records use
the tome loop instead of the NPC cue ([notes](reference/natural-world-sounds.md#notes-lore-records-and-paintings-2026-10-08)). All unheard.
Earlier, 2026-10-08 evening (Claude session, user away; installed after the game closed, all unheard):
ground loot sounds by rarity and type, shrines/time rifts/Nemesis get loops and names, duplicate WAVs removed
([sounds](reference/world-object-sounds.md)); cosmetics tab read ([details](reference/cosmetics-reading.md));
the focused inventory item is no longer re-read after an online sync ([details](reference/inventory-drop-focus.md#re-reading-after-an-online-sync-2026-10-08));
automatic quest updates speak online again ([details](reference/quest-updates.md)); the co-op partner is no longer
named on every radius crossing ([details](reference/nearby-player-names.md)); B10 arrival names the exit back
([details](reference/exploration-mode.md)). See [WHATS_NEXT.md](WHATS_NEXT.md).
Earlier that day (Claude session, user away): physical keyboard types from the first key once Cross opens
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
Installed after the user closed the game; all unheard; 61 host suites pass.
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

Source updates are pushed to GitHub after each batch, as requested. Public test
releases go to GitHub Releases with the asset name `LeAccess.zip` (see below).

## Feature status

| Area | Current evidence / remaining work |
|---|---|
| Steam port | Earlier build 25672295 logged online creation, connection and InGame. Current hotfix compiles and passes native audit; startup/online listening pending. |
| Native hook safety | 2026-10-09: 84 declarations / 83 native targets; new `MapPanel.HandleNavigateInput` and `EpochMasteryUI.HandleSubmitInput` prefixes (no relative branch in the prologue). 2026-10-08: 81 declarations / 80 native targets; new `ButtonInputListener.CanBePressed` postfix (keyboard shortcuts blocked while typing; first branch a 32-bit call at +0xB). 46 checks; build 25690669, 72 declarations / 71 distinct native targets; two new hooks (2026-10-04 afternoon: `SkillsPanelManager.HandleNavigateInput` prefix, `EpochInputManager.ControllerShouldBeActive` postfix) first relative branch +0x13/+0x15. Audit on 25711439 after regeneration: 46 checks, 75 declarations / 74 native targets. On 25764382 (2026-10-07): 46 checks, 80 declarations / 79 native targets; new `ItemContainerUI.set_IsHovered` prefix, first relative branch +0x17. Hooks whose original runs must not start with a relative branch: the passive navigate prefix and three tiny bodies now replay the native code instead ([detour crash](reference/runtime-feedback.md#passive-navigation-crash-detoured-short-branch-prologue-2026-10-04)). |
| Reader recovery | Bounded reconnection; covered readers acknowledge accepted output and retry current content. 41 transport checks plus service regressions. [Scope/limits](reference/screen-reader-lifecycle.md); listening pending. |
| Early online/offline selection | Empty native nullable no longer throws during readiness polling or erases the first click; observation failures retain intent, dispatch remains once-only. 56 checks; fresh startup listening pending. |
| Menus/focus | 2026-10-08 evening: inventory cosmetics tab (slots, skill rows, class filters, tabs). 2026-10-08: endgame panels (dungeon/arena tiers and key slots, Aberroth, boss votes, Sanctuary, echo rewards, blessings, Monolith echoes, Nemesis, Morditas) and faction/vendor panels (faction choice, rank rewards, gamblers, special vendors with prices, item-service slots, time beast, loot-filter rules, mastery choice, bazaar costs); later: Weaver tree, Forgotten Knights timelines, Observatory prophecies, Bazaar listings/pages/results, Ladder rows, Morditas ritual buttons; afternoon: dropdown options "N de M"/selected and new value once (generic `DropdownReader`), Bazaar my stall and listing form, lens effects; not covered: listing-form slot focus, remote players' class ([endgame](reference/endgame-panels.md), [factions/vendors](reference/faction-vendor-panels.md)). Screen-reader keys (Ctrl, Shift, Alt, Insert, CapsLock, Windows) keep controller mode, so menus no longer lose focus and reset ([cause](reference/runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04)). Settings sliders/toggles speak only the new value while focus stays; headings no longer repeat after dropdowns; popups never speak raw object names; controller button names localized; zero-height labels that render (main-menu "Partida sin conexión") are read again. Native mediator callbacks follow FocusedPanel; typed forge/material selections and background/faded guards. 307 menu + 63 settings-source checks. [Audit and limits](reference/menu-visibility.md); listening pending. |
| Mod localization | All ten game languages (en, es, fr, de, pt-BR, pl, ru, ko, zh, ja; game `jp` mapped), automatic game language that follows in-game Settings changes live (verified natively), override and per-key fallback; non-Spanish catalogs unreviewed by native speakers ([details](reference/localization.md)); 5,710 checks; live switching from Settings confirmed by the user (2026-10-10). |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Generic modals (loot-filter editor, pickers, stash setup) read title/message/static text once on open; scrolling and control captions stay silent; quick notices speak inside them. Hidden/faded modals release focus; tutorials yield to dialogue. Rejected output retains current content and modal priority. 232 screen-content + 118 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Dungeon/encounter instructions | Native dungeon/modal and visible echo/Morditas text; dynamic recovery preserves pacing. 211 screen-content + 90 dynamic checks. [Windows](reference/dungeon-warning-reading.md), [sources/limits](reference/dynamic-objectives.md); listening pending. |
| Visible subtitles | Distinct dialogue turns and current rendered/submitted lines recover after rejection; stale or expired content is discarded. The game's automatic response/continue focus waits for the NPC line, also when the response menu replaces the continue button (2026-10-04); the continue button reads "Continuar" ([details](reference/npc-subtitles.md)). 86 dialogue + 166 subtitle checks. Inspected Rogue opening narrative remains audio-only. |
| Hints/notifications | Button-first hints retained. Countdown reads once per restart with menu/typing priority; rejected batch output keeps current sources pending. 79 binding/prompt + 115 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option; fully faded wheels/sectors cannot speak retained models. 40 checks. Listening pending. |
| Classes/masteries/realm | Character-select tiles read name, level/mastery, cycle and hardcore tag; character-sheet stat lines read name and value ([sheet](reference/character-sheet.md)). Season/legacy labels; descriptions manual. Disabled retained carousel and background mastery rejected. 102 checks; listening pending. |
| Skills/assigned buttons | 2026-10-08: tree nodes say what they require ("requiere Martillos alados", points when unmet); details add the route and what each leads to ([connections](reference/skill-tree-navigation.md#node-connections-2026-10-08)); the user confirms it works. Offline equipped status and bar slots fixed (read the action bar's own `AbilityBarIcon.playerAbilityList`, no `HasInitialised` gate; log `[Skill binding] source: …`); Skills 274. The user reports it seems to work (same day). Verified assignments and learned/unlock state; rejected briefs/details retry the current owned selection. The overview is a grid: slots, base class and each mastery as rows, row name on entry, "N de M", no wrap; empty/locked slots speak ([overview](reference/skill-tree-navigation.md#skills-overview-grid-2026-10-04)). Specialization trees are a grid by branch ("Rama N"); opening focuses the tree's own root ([tree navigation](reference/skill-tree-navigation.md)); Up from the root reaches the tree's buttons, and specialising, spending/removing points and their state are spoken ([modifying](reference/skill-tree-navigation.md#modifying-a-skill-with-a-controller-2026-10-04-static-analysis)). Square carries the highlighted skill into the chosen slot's list ([assignment](reference/skill-assignment.md#square-carries-the-highlighted-skill-2026-10-04)). 263 checks; listening pending. |
| Passive nodes | 2026-10-08 night: nodes say what they require; details give the route and what they lead to ([requirements](reference/passive-reading.md#node-requirements-2026-10-08)). Native name/description; overview on opening, tree name/points before a new tree's first node, unmet thresholds, skill strip. The controller panel (`PassivePanelManager`) is covered too; the grid is navigated by column ("Columna N, requiere X puntos", "N de M" in the column), leaving only from the first column; points left, opened columns and Cross refusals are spoken; the banner is a list ([banner](reference/passive-reading.md#mastery-banner-as-a-list-2026-10-05), [controller panel](reference/passive-reading.md#controller-panel-reading-order-and-tree-switching-2026-10-04), [skill-tree rules](reference/passive-reading.md#reading-like-the-skill-trees-2026-10-05)). 193 checks; listening pending. |
| World map/waypoints | 2026-10-09 evening: the list holds everything the map shows (markers say "Sin punto de viaje", standalone dungeon/faction/rift icons with their tooltip, islands with name, banners, corruption, harbinger, blessing); Waypoints 75. 2026-10-09 afternoon: locked waypoints back in the list (user request); the place focused after an era switch queues behind the era name; Waypoints 69. 2026-10-09 later: list holds unlocked travel points only (Maj'elka "2 de 15" had 7 no-waypoint markers), islands stepped by identity, era spoken on `CurrentEra` change with the game's localized name, localized region names, `[World map]` log lines; Waypoints 67. Earlier: quests hidden (Square) → D-pad up/down walk the era's places by region (centre + select like the game's recentre, region on entry, "N de M", no wrap); era title + position on LB/RB; quest panel state spoken; Knowledge of Orobyss entry reads the epoch mastery panel (brief, points as details, completion text, "Viajando" on Cross); Waypoints 63, Quests 139; listening pending ([list](reference/waypoint-map.md#list-navigation-with-the-quests-hidden-2026-10-09)). Map selection reads location, level, "here", locked and quest markers; a quest's tweened location queues after its brief. Cross says "Viajando a X" or why travel is impossible instead of silence ([travel](reference/waypoint-map.md#cross-on-the-map-travel-2026-10-04)). 35 checks; listening pending. |
| Quest journal | Current state/objectives and manual history retained; rejected briefs/details retry only while selection remains owned. 136 checks; listening pending. |
| Automatic quest updates | 2026-10-08: never spoke online (no saved character data online); identity now from `LocalPlayer`. Failed output preserves current pending changes, shared pacing and fair turns with dynamic objectives. 116 checks. [Evidence](reference/quest-updates.md). Listening pending. |
| Social panel | Dedicated reader (rows, sections with counts, tabs, own username, region, username windows, player options, invitation prompt), social chat results and typing echo; 58 checks. [Details](reference/social-reading.md); listening pending. |
| Character name editing | 2026-10-08: with the screen keyboard open, physical keys type from the first press (controller mode kept, text through the keyboard's own `SubmitEvent`), Enter submits, Escape keeps the text like Circle; name and Social username fields; 198 checks, listening pending. Earlier: user confirms the latest typing/return fix works. Circle keeps the name; physical entry and owned device transitions retain focus. Eight hooks, 160 checks. |
| Inventory/merchants | 2026-10-09: online sync (`receiveContainerContents` re-adds every item) left the focus empty for a frame and erased the spoken record; the same item regained with nothing said since stays silent (log `[Inventory] same item after a refresh; not repeated`); Inventory 378; the pad's vibration is the game's own. 2026-10-08 night: idol grid and altar reachable and read. 2026-10-08 evening: no re-read after an online sync (item id key; "N de M"-only changes silent). Current stash/shop/buyback and [equipped selections](reference/equipped-items.md); rejected briefs/details retry while owned. After a drop, focus moves to the nearest item instead of the first slot ([drop focus](reference/inventory-drop-focus.md)); holding compare reads the equipped item, differences and changed lines ([comparison](reference/item-comparison.md)). 236 service + 111 model checks. Listening pending. Backpack and merchant stock/buyback move item by item with "N de M", rows by Down/Up, no wrap ([navigation](reference/inventory-drop-focus.md#controller-navigation-2026-10-04)). |
| Forge/filter controls | Typed selections, filter-range labels and manual visible preview/potential/unavailable reason. 118 regressions within 296 menu checks. [Scope/limits](reference/crafting-reading.md); listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | 2026-10-09: dynamic openings — an edge must be passed (receding/diagonal walls stay silent, wall ends breathe) and short gaps (from 3 m) must continue beyond (niches silent, narrow/bending passages audible); 6 m opens by itself; knock 2.1 m; invalid frames skipped; Navigation 834; listening pending ([details](reference/navigation-radar.md#dynamic-openings-2026-10-09)). Wall memory, owned body radius as before. 2026-10-04: terrain cues never wait behind NPC/enemy/object/beacon sounds (`max_wait_ms` in the status); openings toward a listed unexplored area end in a D2R double pip (`UnexploredMark`). 504 navigation checks. [Details](reference/navigation-radar.md#latency-and-the-unexplored-mark-2026-10-04); listening pending. |
| Objective orientation | Removed 2026-10-03 (F6/right stick down); replaced by the exploration scanner. Target/route classes kept for reuse. [Notes](reference/objective-orientation.md) |
| Automatic orientation | Removed 2026-10-03; replaced by the exploration scanner. [Notes](reference/automatic-orientation.md) |
| NPC/world sounds | 2026-10-09 late night: rune/gem/scroll clicks at one-shot level, one voice per loot kind, shrines via `ShrineObjects`/`ShrineVisualsCreator`; WorldCues 261, WorldLoops 176, LandmarkProbe 111. 2026-10-08 evening: loot by rarity (D2R normal/magic/rare/legendary), rune, glyph (gem), shard (scroll), other materials (quest item); shrine and Nemesis loops from Last Epoch banks; rifts use the rift loop; fallback cues cut from the loops (root duplicates removed); listening pending ([mapping](reference/world-object-sounds.md)). Natural recordings for loot and typed chest/stash/portal/transition identities: Last Epoch's own for gold, potions, tomes, portals/waypoints and exits (user choice), D2R for equipment, chests and the stash ([sound design](reference/sound-design.md)); generic/NPC/radar cues retained. Gold, equipment, potions, tomes, chests, the stash, portals, waypoints and exits now play whole recordings as continuous loops (up to 8, from 12 m, `Loops`/`LoopRange`), replacing the 0.3 s excerpts that sputtered ([loops](reference/world-object-sounds.md)). 153 world + 167 sample/fallback checks. [Details](reference/natural-world-sounds.md). Listening pending. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. Now includes native tooltip subtitles and verified merchant role. The same NPC re-entering range within 5 s stays silent. 109 probe, 110 lifecycle and 59 audio checks; new service coverage needs listening. |
| Nearby player names | 2026-10-08 evening: partner named once, again after 60 s away (was 478 times in a session). 2026-10-08: username, friend's character name (friends cache only) and level (`clientCharacterInfoProvider`), also on scanner entries; Players 51. Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Optional continuous sonar now also mutes in non-pausing menus and after failed queries. 48 radar/audio checks. |
| Nearest enemy | Owned visual display/ActorData; 156 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Enemy cues | 2026-10-08 night: rising elite cue for the nearest rare/champion; class words; minion orders spoken. D2R port, on by default: ordinary enemies growl, nearest boss/miniboss swoops; max three streams in targeting range and walkable line of sight, faster when closer, camera-relative pan, shared audio queue with radar/NPC/world sounds; off-mesh bosses still sound, brief absences keep cadence. 141 checks; listening pending. [Details](reference/enemy-cues.md) |
| Exploration scanner | 2026-10-10: L3 is the mod button on non-PlayStation pads (overlay map off on L3 in gameplay; [details](reference/exploration-mode.md#l3-as-the-mod-button-on-non-playstation-pads-2026-10-10)). 2026-10-09 late night: generated caves read only the active scene (rooms, places); untagged cave entrance/exits named ([caves](reference/exploration-mode.md#generated-caves-2026-10-09)). 2026-10-08 night: Create + right stick (the right stick alone is the game's minion command). 2026-10-08: enemies category lists every living enemy on screen (before: skill-targeting range only); corpse category renamed "última muerte", a destination only. D2R port, right stick in gameplay; Create+Square vitals, Create+Cross beacon through explored ground. Since 2026-10-04 the D2R categories in D2R order (exit, portal, waypoint, unexplored room, room, quest, NPC, player, corpse/death place, stash, enemy, treasure, shrine, object, item), D2R speech, "the radar has logged a new exit/place" with chime, minimap rooms as "sala N"/"sala inexplorada N" with the D2R room loop and entry cue. Saved per character (`Exploration/*.bin`). Exploration 357, catalog 45, input 27 checks. [Design](reference/exploration-mode.md), [D2R categories](reference/exploration-mode.md#d2r-categories-2026-10-04). Offline scanner/beacon confirmed 2026-10-04; categories, sounds and online listening pending. |
| Enemy scan | F7/right-stick trigger removed 2026-10-03; probe, report and hostile registry (20 checks) kept as the scanner's enemies category. [Details](reference/enemy-scan.md) |
| Boss life | 2026-10-08 night: nearest on-screen rare/champion narrated too. D2R port: bosses/minibosses read "Name, 74 percent" on band falls, death once as 0; on-screen bosses only, one step below 10 percent; first sighting silent, queued speech. 31 checks. Online bar scale (`IOverheadBar.HealthNormalized`) and live bosses unverified. [Details](reference/boss-life.md) |
| Progression | 2026-10-08: "Nivel N" on level up, new passive points, skills that can take a point, new skill unlock; Create+Circle / Ctrl+L status (level, XP percent, points, zone). Polled, online via `LocalPlayer`. 60 checks; listening pending. [Details](reference/progression.md) |
| Life/mana | Gold pickups and rewards say their amount ("50 de oro"), silent at vendors (2026-10-08, `[Vitals] AnnounceGoldPickups`). Falls read the band; one-band rises (regeneration between casts) stay silent, ending the 62/75 mana spam; low mana never interrupts and below 10 percent speaks only its first step and empty. [Rules](reference/vitals-api.md). Potion charges speak on use/pickup and in the R / Create+Square report (D2R belt port; Create alone no longer reports); the report also lists HUD debuffs; Create+Triangle / Ctrl+G reads gold ([gold](reference/vitals-api.md#gold-2026-10-05)). 513 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Inactive destination metadata can name active forest/camp entrances; listener/range/destination guards remain. Verified non-NPC services no longer require a hover tag. 86 probe + 54 lifecycle checks; listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, independently re-extracted against installed build 25690669; 16 Monolith layouts included. Entry overview defaults off. Listening pending. |
| Live map places | Same-name places no longer repeat on footprint-edge flicker or adjacent pieces (5 s gap; alt-tab keeps memory). Failed approach queries cannot imply tent departure. Three verified canopies and shared online visual ownership retained. 264 runtime + 357 state + 164 service + 152 world checks. [Details](reference/runtime-map-places.md). Listening pending. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default. Shared online visual ownership, closest physical contact, same-floor and multi-foot consistency checks. 86 adapter checks. [Details](reference/map-surfaces.md). |
| Other gaps | Full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener; audio-only player lines lack authored text. |

## Validation and references

- 2026-10-11 08:57: settings menu and tester batch installed (`LeAccess.dll` `ECC6B942930F0B00…`, game closed). All 62 host suites pass (new ModMenu 262; Exploration 425, ExplorationCatalog 91, Localization 8410, WorldLoops 181); PatchSafety 84 declarations / 83 targets, no new hook. No friend ZIP or release.
- 2026-10-10 22:47: player-report batch installed (`LeAccess.dll` `C517B9A5B6820E59…`, game closed; external catalogs kept, new keys are embedded). All 61 host suites pass (Inventory 390, Exploration 397, ExplorationCatalog 64, Skills 291, Passives 203, Dialogue 97, Classes 148, Localization 5750); PatchSafety 84 declarations / 83 targets, no new hook. No friend ZIP or release.
- 2026-10-10: release announced in the audiogames.net Play testing room on the user's account, at their request: <https://forum.audiogames.net/topic/60577/>.
- 2026-10-10: GitHub release `v0.1.0` (first public test release) carries that ZIP as `LeAccess.zip`; every release must keep that asset name so <https://github.com/Ali-Bueno/LeAccess/releases/latest/download/LeAccess.zip> (README, forum post) stays valid, and must not be a prerelease (`latest` skips them).
- 2026-10-10 10:44: test ZIP `LeAccess-Steam-1.5.1-prueba-20261010-104437.zip` (repo root, ignored by git), SHA-256 `9FD982DCB5465DF81840633E41F97C4BAD8201A1530B32F04820CE8EB84199BD`, 272 files, game build 25816319; L3 as the mod button on non-PlayStation pads (Leeme updated), config texts name the Diablo 2 mod as unreleased. Its `LeAccess.dll` (`8CAAF9ABC684825E…`) is the installed copy (game closed). Package 250. Replaces 20261010-101034.
- 2026-10-09 19:37: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261009-193756.zip` (repo root, ignored by git), SHA-256 `11BB3C34349C264A1B13B09FA79B5EEB6276B735A7FADD5FEF10ED980FA08D95`, 272 files; replaces 20261009-175923 with rift-beast caves (scanner reads only the cave, entrance/exits named), louder rune/glyph/shard clicks with a voice per loot kind, shrines recognized by sounds and landmarks, equipment -3 dB and radar +3 dB (Leeme updated). Its `LeAccess.dll` (`5D0013DB69D70EE1…`) and InteropRepair patcher are the installed copies (game closed). All 61 host suites pass; package 250.
- 2026-10-09 17:59: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261009-175923.zip` (repo root, ignored by git), SHA-256 `A9E78FF92A01DEC8266F1561DB41C771D354E3C6052D6E46A071F1C872154424`, 272 files; adds mod speech in all ten game languages (eight new catalogs, Leeme updated). Its `LeAccess.dll` (`DD8E397131F38A2E…`) is the installed copy (game closed). Package 250.
- 2026-10-09 night: ten-language catalogs installed (eight new catalogs copied); Localization 5692, package 250, and the 20 catalog-embedding suites pass.
- 2026-10-09 12:12: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261009-121206.zip` (repo root, ignored by git), SHA-256 `2D28DD062CF401CD76F0D7010396E4FD748941EBE3E6E6AFEC7AFF60286CF7A7`, 264 files; replaces 20261009-071254 with the world-map list (whole map, eras, islands), the epoch mastery panel and the inventory refresh fix (Leeme updated). Its `LeAccess.dll` (`C2A54EF91E45BD96…`) is the installed copy. Package 242.
- 2026-10-09 11:58: whole-map list installed (`LeAccess.dll` `C2A54EF91E45BD96…`, game closed); Waypoints 75, Localization 1198.
- 2026-10-09 11:48: locked waypoints listed again and era name queued before the settled place; installed `LeAccess.dll` `5EAA9C7080481D88…` (game closed); Waypoints 69.
- 2026-10-09 11:27: map list/era fixes and the inventory refresh fix installed (`LeAccess.dll` `E4F0653BC8E25806…`, game closed); DynamicObjectives 90, Inventory 378, ItemTooltipModel 111, MenuReading 546, QuestUpdates 117, Quests 139, ScreenReader 41, Waypoints 67, Localization 1188. No friend ZIP.
- 2026-10-09 09:17: epoch mastery panel installed (`LeAccess.dll` `143E786C8FCF4669…`, game closed); Waypoints 63, Quests 139, MenuReading 546, Localization 1188, PatchSafety 46 (84 declarations / 83 targets). No friend ZIP.
- 2026-10-09 08:03: world-map list navigation installed (`LeAccess.dll` `4390D54C9DAE8476…`, game closed); Waypoints 56, Localization 1182, PatchSafety 46 (83 declarations / 82 targets). No friend ZIP.
- 2026-10-09 07:12: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261009-071254.zip` (repo root, ignored by git), SHA-256 `3753F150F4AEF16C88CC33B44F45CC65515D35E90461F4CEFCB9E5C528541200`, 264 files; replaces 20261008-234721 with dynamic radar openings (Leeme updated). Its `LeAccess.dll` (`B0EFDDE1DDDC159E…`) is the installed copy. All 61 host suites pass (Navigation 834); package 242.
- 2026-10-08 23:47: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-234721.zip` (repo root, ignored by git), SHA-256 `9BFB80424031A4E6E5E4817A6AA655708532A1CFD8DD2DD5946F3A4204867C75`, 264 files; replaces 20261008-232813 and adds the radar passage/wall-following change, scanner levels and eras (Leeme updated). Its `LeAccess.dll` (`BAE58899A5373899…`) is the installed copy. All 61 host suites pass (Navigation 821, ExplorationCatalog 49, MenuZone 46); package 242.
- 2026-10-08 23:28: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-232813.zip` (repo root, ignored by git), SHA-256 `3ED7AEFEE0B88350647D30AEE72FC784847194D0B36D7629DFB3DF6C54D3DF5E`, 264 files, game build 25764382; replaces 20261008-225317 and adds passive node requirements (Leeme updated). Its `LeAccess.dll` (`6DFA5B9D7ADCA5F6…`) is the installed copy. All 61 host suites pass (Passives 196); package 242.
- 2026-10-08 22:53: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-225317.zip` (repo root, ignored by git), SHA-256 `A5F30FE5DFC51317A0F21B56CDC84FF0758332DB9C40845DB220A7D30922D201`, 264 files, game build 25764382; adds everything from the evening and night batches (loot sounds, shrines/rifts/Nemesis, notes as tomes, cosmetics, idols, elites, scanner on Create + right stick, minion orders, online quest updates, inventory re-read and co-op naming fixes); the Leeme lists them. Its `LeAccess.dll` (`760AE8215A2E4F3E…`) is the installed copy. Package 242 checks.
- 2026-10-08 night (notes): all 61 host suites pass (NpcProbe 112, LandmarkProbe 101, WorldCues 246). Installed `LeAccess.dll` SHA-256 `5393EA1007637F9C…` after the game closed.
- 2026-10-08 night: all 61 host suites pass (Inventory 356, Combat 175, EnemyCues 181, BossLife 40, EnemyScan 28, ExplorationInput 45, Localization 1172, PatchSafety 46 with 82 declarations / 81 targets; new prefix `IdolsContainerUI.HandleNavigateInput`). Installed `LeAccess.dll` SHA-256 `F3F0A0FBE7B1FB04…` after the game closed. No friend ZIP.
- 2026-10-08 evening: all 61 host suites pass (Inventory 322, WorldCues 245, WorldLoops 176, NaturalAudio 165, Exploration 383, QuestUpdates 117, DynamicObjectives 90, Players 53, MenuReading 546, Localization 1128, PatchSafety 46 with 81 declarations). Installed `LeAccess.dll` SHA-256 `7FEB48AFFDAE72E1…` (about 18 MB with the new loops), after the game closed. No friend ZIP built.
- 2026-10-08 11:56: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-115642.zip` (repo root, ignored by git), SHA-256 `9A9BD0C1B0DF7B87453C7E6785FB6C622DB7EEE1141D6257119D965B70CCF8BD`, 264 files; adds skill tree node connections. `LeAccess.dll` `E409BBF6FB14CD2F` is the installed copy (installed after the user closed the game). Skills 280, Localization 1114, package 242.
- 2026-10-08 10:25: friend ZIP `LeAccess-Steam-1.5.1-prueba-20261008-102546.zip` (repo root, ignored by git), SHA-256 `9D05028F9624162F5DB0440EE0CB8CE9950BA88F07F45A60B61996848FC6DD92`, 264 files; adds dropdown reading, Bazaar stall/listing, lens effects and other players' level/character name. Its `LeAccess.dll` (`1AACEBEA26CC77B0`) is the installed copy (installed after the user closed the game). 61 host suites and package 242 pass (MenuReading 535, Localization 1104, Players 51, Social 74).
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
