# Claude handoff — 2026-10-03

The user requested this handoff because Codex was nearing its limit. Continue
the broad accessibility/bug audit; it is **not complete**. Read `STATUS.md`, then
`PRINCIPLES.md`, before changing code. Older API descriptions in `CLAUDE.md` and
`AGENTS.md` need checking against the current ignored `last epoch code/` tree.

## Verified starting point

- Latest implementation: **`89b9ece`**, pushed to `origin/main` and installed.
  The worktree was clean before this documentation handoff. No partial code
  change or running test process needs rescuing; the next investigation was
  stopped before edits.
- Steam build **25690669**, Unity **6000.4.8f1**, BepInEx **6.0.0-be.788**.
  Current interop/native maps were regenerated offline. See
  [Steam port evidence](reference/steam-port.md); do not use old RVA maps.
- All **51 C# suites / 58,040 checks**, **24** missing-audio-resource checks and
  **20** Python extraction tests pass. Native audit: **60 declarations / 59
  targets**, with 46 safety regressions.
- Installed `LeAccess.dll` SHA-256:
  `DF7E5CF8A58D9AFD6EABA6A907A78F9C9E72D7F2CD742BFF6B8EAC90865740FD`.
- Previous plugin/configuration copy:
  `C:\Users\ali-b\Documents\LeAccess backups\pre-encounter-guidance-6ed6f31-20261003`.
  Earlier interop backup is recorded in `STATUS.md`.
- **No game or audio device was launched.** Online startup, controller behavior
  and listening on this build remain unverified. Host tests are not proof that
  every live screen or encounter works.

## Most recent implemented work

`6ed6f31` fixed rejected-output recovery for inventory, skills, passives, visible
screen/dialogue/subtitle content and notices. Rune/glyph grids and flyouts now
read the native selected material's localized name and visible quantity.

`89b9ece` extends recovery to quest journal, automatic quest/dynamic updates,
vitals and orientation. Shared speech pacing/fairness advances only after
accepted output. Manual forge inspection adds the visible preview, current
potential or exact unavailable reason. It excludes unresolved sprite fragments.

The same commit adds native Morditas return-arrow guidance. This is a distinct
`GuidanceTargetKind.EncounterReturn`, temporarily superseding normal markers.
Automatic guidance requires a verified path; manual guidance retains an explicit
bearing fallback. The early urgent pass follows the abandonment notice, shares
the normal half-second route clock, and bypasses the six-second narration gap
only for the first stable return caption. It preserves input/foreground gates.
See [hazard evidence](reference/hazard-sources.md) and
[automatic orientation](reference/automatic-orientation.md).

**Native trap:** arrow `Init` is inlined in the game's async creation body.
Discovery therefore observes Unity's actual `FixedUpdate`, not `Init`.
The stored player transform belongs to the exact local `ActorSync`. Online
ownership uses reciprocal current visuals without requiring a server gameplay
actor. Sync components can live in loaded additive scenes; the player and arrow
presentation/target must still belong to the captured world.

## Next work, in order

1. **Finish output-acceptance coverage in general menus.** Inspect
   `ElementAnnouncer`, `SettingsFocusAnnouncer`, `ButtonPromptAnnouncer`, direct
   toggle/slider speech in `HarmonyPatches`, and manual-details dispatch in
   `MenuAccessibility`. Several callers still use void `Speak`; reproduce lost
   focus/state/details before changing them. Also review controller menus, class
   carousel and character-name feedback. Preserve typed-input behavior and
   native control mappings. Coordinate edits to `ElementAnnouncer` if delegating.
2. **Map/zone recovery.** Start at `MapSectionAccessibility.Update`,
   `MapSectionState`, `SceneryPresenceState`, `ZoneAnnouncer` and
   `ZoneAnnouncementState`. Separate actual observation/departure from accepted
   narration. A failed tent-entry announcement must not manufacture a later
   “left tent” report; an uncertain physical query must not imply departure.
   Refresh location and session before retrying. Keep map overviews and ground
   descriptions disabled by default, as the user requested.
3. **Nearby entity recovery.** Review `GroundLootAccessibility`, `NpcSounds`,
   `NpcNameAnnouncementState`, `NearbyPlayerAccessibility`,
   `LandmarkAccessibility`, `CombatAccessibility` and its nearest-enemy state.
   Retry only the currently valid nearby/selected source, retaining range,
   visibility, ownership, cooldown and audio arbitration rules.
4. **Remaining filter/forge coverage.** Advanced affix conditions, combined
   tiers, sealed-affix selection and editing feedback still need inspection.
   Unresolved forge icons, cost ranges and critical-success presentation are
   also unmapped. Use current native methods and installed prefab labels; do not
   guess hierarchy, indexes, enum values or item models. Current evidence and
   limits are in [crafting reading](reference/crafting-reading.md).
5. **Broader gameplay/runtime audit.** Visual attack telegraphs still lack a
   verified unified source. Do not infer dangerous ground, hostility, safe
   direction or impact time from effect names. The return arrow is an encounter
   instruction, not a damage detector. Continue the remaining source audit and
   preserve the live checks listed in `STATUS.md` for the user's next session.

## Recovery contract and workflow

`ScreenReader.TrySpeak` means Tolk accepted speech **or braille**, not playback
completion. Existing void `Speak` remains compatible. Current migrated readers
prepare a candidate without consuming it, call `TrySpeak`, then acknowledge the
exact current candidate on success. Failed output must not spend a speech turn
or unlock a pulse that depends on announcing its target. Shared state APIs keep
default acknowledgement for unmigrated callers. Do not indiscriminately change
every call or add a global replay queue.

Menu manual intent is bound to the current owned selection and rebuilds fresh
details after recovery. Gameplay manual reports stay explicit button requests;
they are not replayed later. Vitals keep a silent first/session baseline and
recover current band crossings, not old critical values. Existing submitted NPC
subtitle expiry remains unchanged. See [transport/recovery scope](reference/screen-reader-lifecycle.md).

- **Do not launch the game or audio** without a new user request. The user tests
  independently. Preserve online/offline choice, movement, network state,
  personal configuration and translations. Do not add dependencies or silently
  replace the established centralized Tolk transport during this audit.
- Development build: `dotnet build LeAccess/LeAccess.csproj --no-restore -p:SkipPluginDeployment=true`.
  Normal `dotnet build LeAccess/LeAccess.csproj --no-restore` deploys to the game;
  check that the game is stopped first, preserve the old plugin, then verify
  installed/built hashes and the personal-file hashes.
- The user explicitly requested **STATUS updates and GitHub pushes after
  verified fixes/improvements**. Commit/push to `main` is authorized. No new ZIP
  or release was requested; the existing friend's ZIP is old and unchanged.
- Full runner and latest logs:
  `%TEMP%\LeAccess-encounter-guidance-release-25690669-20261003\run-tests.ps1`
  and `tests\final-results.json` beneath that directory. The final result file
  includes the later cadence regression rerun. Personal-file baselines:
  `%TEMP%\LeAccess-audit-20261003\personal-config-hashes.json`.
- Native/asset scratch evidence:
  `%TEMP%\LeAccess-encounter-orientation-25690669-20261003`,
  `%TEMP%\LeAccess-hazards-25690669-20261003`, and
  `%TEMP%\LeAccess-specialized-controls-20261003`.
  These are investigation helpers, not runtime inputs or files to commit.

The broad goal remains unfinished. Do not equate passing host tests with a
bug-free mod or mark all live workflows as validated.
