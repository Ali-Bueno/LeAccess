# STATUS — Last Epoch

**Updated:** 2026-10-03. Added guarded Morditas return orientation and manual
forge previews/reasons. Quest, vitals and guidance recovery now acknowledge
accepted output before consuming state or pacing.
All 51 host suites pass; the rebuilt plugin is installed for Steam **25690669**.
Startup, online play and listening on this build still need in-game validation.

## Current installation

- **Game:** Steam build **25690669**, Unity **6000.4.8f1**, IL2CPP metadata **39**.
- **Path:** `D:\games\steam\steamapps\common\Last Epoch`.
- **Loader:** BepInEx **6.0.0-be.788**, Il2CppInterop **1.5.3**; automatic updates remain enabled.
- **Interop:** regenerated offline from the installed binary/metadata, 214 DLLs and two native databases installed and byte-verified. The existing preloader repaired the same 16 Unity orphan rows. Native audit: 46 regressions, 60 declarations / 59 targets.
- **Build:** `dotnet build LeAccess/LeAccess.csproj`; use `-p:SkipPluginDeployment=true` while the game is running.
- **Speech:** centralized Tolk/NVDA, now with bounded reconnection; see [connection lifecycle](reference/screen-reader-lifecycle.md).
- **Maps:** 750 fixed sections across 58 layouts and three tent canopies independently re-extracted for the new build. [Provenance](reference/map-section-build-25690669.json).
- **Backup:** previous interop and plugin in `C:\Users\ali-b\Documents\LeAccess backups\steam-build-25690669-20261003`. Previous installation and personal configuration retained.
- **Previous plugin:** commit `6ed6f31` installation preserved in `C:\Users\ali-b\Documents\LeAccess backups\pre-encounter-guidance-6ed6f31-20261003`.
- **Launch:** normally through Steam; online/offline remains the game's choice. No game or audio device was opened by this audit.

## Next checks and remaining scope

**Claude handoff:** [WHATS_NEXT.md](WHATS_NEXT.md) records the verified starting
point, next investigations and working constraints requested by the user.

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
| Native hook safety | 46 checks; fresh build 25690669, 60 declarations / 59 distinct native targets including the return-arrow callback. |
| Reader recovery | Bounded reconnection; covered readers acknowledge accepted output and retry current content. 41 transport checks plus service regressions. [Scope/limits](reference/screen-reader-lifecycle.md); listening pending. |
| Early online/offline selection | Empty native nullable no longer throws during readiness polling or erases the first click; observation failures retain intent, dispatch remains once-only. 56 checks; fresh startup listening pending. |
| Menus/focus | Native mediator callbacks follow FocusedPanel; typed forge/material selections and background/faded guards. 296 menu + 63 settings-source checks. [Audit and limits](reference/menu-visibility.md); listening pending. |
| Mod localization | Automatic game language, en/es catalogs, override and per-key fallback; 376 checks. |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Hidden/faded modals release focus; tutorials yield to dialogue. Rejected output retains current content and modal priority. 211 checks. [Evidence](reference/announcement-lifecycle.md). |
| Dungeon/encounter instructions | Native dungeon/modal and visible echo/Morditas text; dynamic recovery preserves pacing. 211 screen-content + 90 dynamic checks. [Windows](reference/dungeon-warning-reading.md), [sources/limits](reference/dynamic-objectives.md); listening pending. |
| Visible subtitles | Distinct dialogue turns and current rendered/submitted lines recover after rejection; stale or expired content is discarded. 73 dialogue + 166 subtitle checks. Inspected Rogue opening narrative remains audio-only. |
| Hints/notifications | Button-first hints retained. Countdown reads once per restart with menu/typing priority; rejected batch output keeps current sources pending. 79 binding/prompt + 115 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option; fully faded wheels/sectors cannot speak retained models. 40 checks. Listening pending. |
| Classes/masteries/realm | Season/legacy labels; descriptions manual. Disabled retained carousel and background mastery rejected. 102 checks; listening pending. |
| Skills/assigned buttons | Verified assignments and learned/unlock state; rejected briefs/details retry the current owned selection. 189 checks; listening pending. |
| Passive nodes | Native name/description; empty names retry at a bounded rate, rejected details rebuild while selection remains owned. 107 checks; listening pending. |
| Quest journal | Current state/objectives and manual history retained; rejected briefs/details retry only while selection remains owned. 136 checks; listening pending. |
| Automatic quest updates | Failed output preserves current pending changes, shared pacing and fair turns with dynamic objectives. 116 checks. [Evidence](reference/quest-updates.md). Listening pending. |
| Character name editing | User confirms the latest typing/return fix works. Circle keeps the name; physical entry and owned device transitions retain focus. Eight hooks, 160 checks. |
| Inventory/merchants | Current stash/shop/buyback and [equipped selections](reference/equipped-items.md); rejected briefs/details retry while owned. 219 service + 111 model checks. [Evidence](reference/merchant-reading.md); listening pending. |
| Forge/filter controls | Typed selections, filter-range labels and manual visible preview/potential/unavailable reason. 118 regressions within 296 menu checks. [Scope/limits](reference/crafting-reading.md); listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | Initial/resumed wall memory prevents false openings on retreat; owned online visuals supply body radius. 447 navigation checks; sound/control vocabulary unchanged. Listening pending. |
| Objective orientation | F6/right-stick-down: native encounter return, tracked objective or named exit; verified path or explicit bearing. Progress follows accepted reports. 157 service/route + 148 target checks. [Limits](reference/objective-orientation.md); listening pending. |
| Automatic orientation | Verified routes and accepted-output pacing; early Morditas return guidance respects countdown/input priority. 398 checks. [Details](reference/automatic-orientation.md); listening pending. |
| NPC/world sounds | Natural D2R recordings for loot and typed chest/stash/portal/transition identities; generic/NPC/radar cues retained. 153 world + 167 sample/fallback checks. [Details](reference/natural-world-sounds.md). Listening pending. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. Now includes native tooltip subtitles and verified merchant role. The same NPC re-entering range within 5 s stays silent. 109 probe, 110 lifecycle and 59 audio checks; new service coverage needs listening. |
| Nearby player names | Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Optional continuous sonar now also mutes in non-pausing menus and after failed queries. 48 radar/audio checks. |
| Nearest enemy | Owned visual display/ActorData; 156 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Enemy scan | F7/right stick up: living hostile count in skill-targeting range, three nearest with camera-relative direction and distance, bosses tagged. Shared hostile registry walk (nearest enemy, boss life, scan). 20 checks; listening pending. [Details](reference/enemy-scan.md) |
| Boss life | D2R port: bosses/minibosses read "Name, 74 percent" on band falls, death once as 0; first sighting silent, queued speech. 20 checks. Online bar scale (`IOverheadBar.HealthNormalized`) and live bosses unverified. [Details](reference/boss-life.md) |
| Life/mana | Falls read the band; one-band rises (regeneration between casts) stay silent, ending the 62/75 mana spam. [Rules](reference/vitals-api.md). Potion charges speak on use/pickup and in the R/Back/View report (D2R belt port). 493 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Inactive destination metadata can name active forest/camp entrances; listener/range/destination guards remain. Verified non-NPC services no longer require a hover tag. 86 probe + 54 lifecycle checks; listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, independently re-extracted against installed build 25690669; 16 Monolith layouts included. Entry overview defaults off. Listening pending. |
| Live map places | Same-name places no longer repeat on footprint-edge flicker or adjacent pieces (5 s gap; alt-tab keeps memory). Failed approach queries cannot imply tent departure. Three verified canopies and shared online visual ownership retained. 264 runtime + 357 state + 164 service + 152 world checks. [Details](reference/runtime-map-places.md). Listening pending. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default. Shared online visual ownership, closest physical contact, same-floor and multi-foot consistency checks. 86 adapter checks. [Details](reference/map-surfaces.md). |
| Other gaps | Full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener; audio-only player lines lack authored text. |

## Validation and references

- `dotnet build LeAccess/LeAccess.csproj --no-restore`: zero warnings/errors; deployed with the game stopped.
- All **51 C# suites / 58,040 checks** passed, plus **24** missing-audio-resource checks and **20** Python map-extractor tests. This batch adds 219 checks. Native safety includes the current loader fingerprint and **60 declarations / 59 targets**.
- Built and installed plugin SHA-256 match: `DF7E5CF8A58D9AFD6EABA6A907A78F9C9E72D7F2CD742BFF6B8EAC90865740FD`.
- Both installed configuration files and both personal translation catalogs retain their original hashes. No game or audio device launched; host tests do not establish live UI visibility or audible output.

Current-build regeneration and native audit: [Steam port notes](reference/steam-port.md).
Newly covered native windows: [dungeon/warning reading](reference/dungeon-warning-reading.md).
Previous validation records and detailed listening checklist were moved to
[validation history](reference/validation-history-20261003.md); their counts and
binary hashes describe earlier builds. Setup and controls remain in [README.md](README.md).
