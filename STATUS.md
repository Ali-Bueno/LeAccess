# STATUS — Last Epoch

**Updated:** 2026-10-03. Further fixes cover stash/shop selection, forge and filter
labels, repeated dialogue turns, tutorial priority and encounter countdowns.
All 51 host suites pass; the rebuilt plugin is installed for Steam **25690669**.
Startup, online play and listening on this build still need in-game validation.

## Current installation

- **Game:** Steam build **25690669**, Unity **6000.4.8f1**, IL2CPP metadata **39**.
- **Path:** `D:\games\steam\steamapps\common\Last Epoch`.
- **Loader:** BepInEx **6.0.0-be.788**, Il2CppInterop **1.5.3**; automatic updates remain enabled.
- **Interop:** regenerated offline from the installed binary/metadata, 214 DLLs and two native databases installed and byte-verified. The existing preloader repaired the same 16 Unity orphan rows. Native audit: 46 regressions, 59 declarations / 58 targets.
- **Build:** `dotnet build LeAccess/LeAccess.csproj`; use `-p:SkipPluginDeployment=true` while the game is running.
- **Speech:** centralized Tolk/NVDA, now with bounded reconnection; see [connection lifecycle](reference/screen-reader-lifecycle.md).
- **Maps:** 750 fixed sections across 58 layouts and three tent canopies independently re-extracted for the new build. [Provenance](reference/map-section-build-25690669.json).
- **Backup:** previous interop and plugin in `C:\Users\ali-b\Documents\LeAccess backups\steam-build-25690669-20261003`. Previous installation and personal configuration retained.
- **Previous plugin:** commit `322f3c5` installation preserved in `C:\Users\ali-b\Documents\LeAccess backups\pre-menu-audit-322f3c5-20261003`.
- **Launch:** normally through Steam; online/offline remains the game's choice. No game or audio device was opened by this audit.

## Next checks and remaining scope

**The user tests independently; do not launch the game without a new request.**
First check startup/NVDA reconnect, stash/shop tab changes, forge affix/shard
controls, filter bounds, dialogue/tutorial handoffs and the Morditas abandonment
notice. Then check equipped items, passives, dungeon objectives, online speech,
route cadence/turns, world sounds and tent entry/departure.

The [menu audit](reference/menu-visibility.md) still leaves complete vendor/stash,
crafting and loot-filter workflows unverified. Current fixes preserve input,
player movement, network state and existing controller mappings. Visual-only
hazards without a verified text/state source remain a coverage gap. Arbitrary
kitchens, taverns, palace rooms and street names are not mapped; map overview and
ground-material speech remain disabled by default per the user's preference.
Compilation and host tests are evidence for their tested logic, not proof that
every screen or gameplay encounter has been heard successfully.
Next source investigations: rune/glyph selectors, forge preview and unavailable
reasons, advanced filter conditions and editing feedback; see [verified control
scope](reference/crafting-reading.md). [Hazard sources](reference/hazard-sources.md)
distinguish the verified abandonment countdown from the still-unread return arrow
and visual attack telegraphs. Recovery of still-visible content after rejected
speech output also remains to be reviewed across readers.

The older friend's ZIP remains local and unchanged; it does not include the new
fixes. Source updates are to be pushed to GitHub after each verified batch, as
requested. No release or new ZIP has been requested.

## Feature status

| Area | Current evidence / remaining work |
|---|---|
| Steam port | Earlier build 25672295 logged online creation, connection and InGame. Current hotfix compiles and passes native audit; startup/online listening pending. |
| Native hook safety | 46 checks; fresh build 25690669, 59 declarations / 58 distinct native targets validated against installed game. |
| Reader recovery | Late/restarted NVDA is detected with bounded retries; failed output does not claim a speech frame. 34 checks. [Evidence/limits](reference/screen-reader-lifecycle.md); listening pending. |
| Early online/offline selection | Empty native nullable no longer throws during readiness polling or erases the first click; observation failures retain intent, dispatch remains once-only. 56 checks; fresh startup listening pending. |
| Menus/focus | Native mediator callbacks follow FocusedPanel; background/faded controls rejected. 231 menu + 63 settings-source checks. [Audit and limits](reference/menu-visibility.md); listening pending. |
| Mod localization | Automatic game language, en/es catalogs, override and per-key fallback; 372 checks. |
| Return from Start | Character creation owner/control restored after pause closes; 134 checks. User confirms it works. |
| Internal panel chatter | Removed; background Forge/Skill Tree opening or closing no longer speaks object names. |
| Tutorials/popups/terms | Hidden/faded modal content releases focus; passive tutorials now wait for dialogue and prior speech. Actual-service interaction tests preserve blocking modal priority. 200 checks. [Evidence](reference/announcement-lifecycle.md). |
| Dungeon/encounter instructions | Focused native dungeon/modal windows and visible echo/Morditas objectives read. 200 screen-content + 78 dynamic checks. [Windows](reference/dungeon-warning-reading.md), [dynamic sources/limits](reference/dynamic-objectives.md); listening pending. |
| Visible subtitles | Distinct identical dialogue turns now read; duplicate callbacks remain silent. Ambient barks retain visible text/range/source guards. 68 dialogue + 143 subtitle checks. Inspected Rogue opening narrative remains audio-only. |
| Hints/notifications | Button-first action hints and equip rejection notices retained. Active abandonment countdown reads once per restart ahead of ambient narration; menus/typing retain priority. 79 binding/prompt + 103 notification checks. [Evidence](reference/announcement-lifecycle.md). |
| Controller menu/zones | Announces opening with neutral stick, then selected localized option; fully faded wheels/sectors cannot speak retained models. 40 checks. Listening pending. |
| Classes/masteries/realm | Season/legacy labels; descriptions manual. Disabled retained carousel and background mastery rejected. 102 checks; listening pending. |
| Skills/assigned buttons | Pending references in other slots no longer hide verified assignments; readiness can recover within the first opening. Learned/unlock state retained. 177 checks; listening pending. |
| Passive nodes | Native builder supplies name/description; initially empty names retry at a bounded rate instead of remaining cached and silent. 95 checks; listening pending. |
| Quest journal | State follows title; active objectives named explicitly. Prior completed/failed objectives are labeled as this quest's history and read only manually, including UI fallback. 124 checks; listening pending. |
| Automatic quest updates | Shared pacing/fair turns with dynamic objectives; deferred or unavailable quests release menu-notice priority. 100 checks. [Evidence](reference/quest-updates.md). Listening pending. |
| Character name editing | User confirms the latest typing/return fix works. Circle keeps the name; physical entry and owned device transitions retain focus. Eight hooks, 160 checks. |
| Inventory/merchants | Active stash/shop contents replace stale hover data; separate buyback headers recognized. [Equipped slots](reference/equipped-items.md) and final selection guards retained. 207 service + 111 model checks. [Evidence](reference/merchant-reading.md); listening pending. |
| Forge/filter controls | Typed add/upgrade affix and shard reading; six potential/affix-count ranges now include their property heading. 53 new regressions within the 231 menu checks. [Scope and limits](reference/crafting-reading.md); listening pending. |
| Ground loot | New targeting API, brief current-item names; 67 checks. Listening pending. |
| Radar | Initial/resumed wall memory prevents false openings on retreat; owned online visuals supply body radius. 447 navigation checks; sound/control vocabulary unchanged. Listening pending. |
| Objective orientation | Right-stick-down/F6: visible tracked objective or named exit, verified path corner or explicit bearing-only fallback, distance/progress. 147 service/route + 91 target checks. [Controls and limits](reference/objective-orientation.md). Listening pending. |
| Automatic orientation | Natural pulses and target/turn/proximity speech; verified routes only. Ineligible queued pulses release their audio turn after target changes. 353 checks. [Details](reference/automatic-orientation.md). Listening pending. |
| NPC/world sounds | Natural D2R recordings for loot and typed chest/stash/portal/transition identities; generic/NPC/radar cues retained. 153 world + 167 sample/fallback checks. [Details](reference/natural-world-sounds.md). Listening pending. |
| Nearby NPC names | Cadáveres ardientes name confirmed by user and latest log. Now includes native tooltip subtitles and verified merchant role. 109 probe, 109 lifecycle and 59 audio checks; new service coverage needs listening. |
| Nearby player names | Nearest other player's public displayed name inside the native player interaction radius; 29 checks. Online listening pending. |
| Quest ping | Removed as requested. Optional continuous sonar now also mutes in non-pausing menus and after failed queries. 48 radar/audio checks. |
| Nearest enemy | Owned visual display/ActorData; 156 checks. Latest log announces Reptador chamuscado and Ojo de Rahyeh offline. Audible results still need confirmation. |
| Life/mana | D2R percentage bands, critical priority, R/Back/View manual report, offline/online sources and typed-input gates; 379 checks. Online hierarchy, controller mapping and listening pending. |
| Nearby landmarks | Inactive destination metadata can name active forest/camp entrances; listener/range/destination guards remain. Verified non-NPC services no longer require a hover tag. 86 probe + 54 lifecycle checks; listening pending. |
| Spatial map sections | 750 guarded sections across 58 layouts, independently re-extracted against installed build 25690669; 16 Monolith layouts included. Entry overview defaults off. Listening pending. |
| Live map places | Failed approach queries cannot imply tent departure; inactive query owners are rediscovered. Three verified canopies and shared online visual ownership retained. 264 runtime + 346 state + 161 service + 152 world checks. [Details](reference/runtime-map-places.md). Listening pending. |
| Generated rooms | Native rooms and tagged passages, entrance/exit and design connections; 66 adapter checks. Edges/ambiguous floors omitted; listening pending. |
| Ground descriptions | Optional, disabled by default. Shared online visual ownership, closest physical contact, same-floor and multi-foot consistency checks. 86 adapter checks. [Details](reference/map-surfaces.md). |
| Other gaps | Full stash/vendor/crafting coverage needs controller checks; transitions without WorldObjectClickListener; audio-only player lines lack authored text. |

## Validation and references

- `dotnet build LeAccess/LeAccess.csproj --no-restore`: zero warnings/errors; deployed with the game stopped.
- All **51 C# suites / 57,690 checks** passed, plus **24** missing-audio-resource checks and **20** Python map-extractor tests. This batch adds 168 checks. Native safety includes the current loader fingerprint and **59 declarations / 58 targets**.
- Built and installed plugin SHA-256 match: `CB65BE07FFA29C96730353FBC7DA71029E791D5D159F3B634D3D64986BEA8415`.
- Both installed configuration files and both personal translation catalogs retain their original hashes. No game or audio device launched; host tests do not establish live UI visibility or audible output.

Current-build regeneration and native audit: [Steam port notes](reference/steam-port.md).
Newly covered native windows: [dungeon/warning reading](reference/dungeon-warning-reading.md).
Previous validation records and detailed listening checklist were moved to
[validation history](reference/validation-history-20261003.md); their counts and
binary hashes describe earlier builds. Setup and controls remain in [README.md](README.md).
