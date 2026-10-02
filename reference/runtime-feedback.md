# Runtime feedback investigation — 2026-10-01

## Evidence

The installed BepInEx log from the reported session contains `[ButtonPrompt]`
messages for the forge, ground tooltips and inventory actions during login and
character loading. `MultiButtonsActionPrompt.UpdatePrompt(bool)` previously
spoke immediately and queued each message in the screen reader. Active objects
alone did not establish that a hint belonged to the current screen.

Windows recorded crashes at 13:35:46 and 14:02:44 local time. Both dumps remain
under `%LOCALAPPDATA%/CrashDumps/Last Epoch.exe.17656.dmp` and `.34124.dmp`.
The later log includes the full radar exception stack:
`NavMeshQueryFilter.get_agentTypeID` → `NavMesh.SamplePosition` →
`RadarWorldProbe.TryCapture`. The radar was enabled with volume 100.

Native CDB analysis of both dumps identifies the same original access violation
on the Unity main thread: `GameAssembly+0x2a8df0`, inside
`il2cpp_gchandle_get_target`, with a null handle and a read at address `0x20`.
Both stacks contain the filter getter, filtered `SamplePosition`, the radar
probe/update and `MenuAccessibility.Update`. This traces these crashes to the
radar query with high confidence. The Windows event's `coreclr+0x1d1fdd`
(version 6.0.722.32202) is secondary fatal exception handling.

Decompiling the installed `BepInEx/interop/UnityEngine.AIModule.dll` confirms
invalid restored IL in the `NavMeshQueryFilter` overloads: they pass the address
of a managed class reference into property access expecting an object. The
filter was originally a Unity struct, but its generated interop type is a class.
This explains the misleading collected-object exception; cache resets alone
cannot fix it. The primitive `SamplePositionFilter` and `RaycastFilter` paths
pass the agent type and area mask directly and avoid that malformed wrapper.

The quest ping's `CalculatePath(..., int areaMask, NavMeshPath)` and `path.corners`
use native invocation wrappers in this installation, without the malformed
filter-object body. The dumps do not identify them or an audio thread as the
source of either reported crash.

The subsequent session log (last written 14:20 local time) reaches the radar's
ready state after Z12 loading without another radar exception. Quest-ping updates
continue during movement. The user confirms crashes stopped, but reports hearing
no radar cues. That build logged neither geometry nor cue consumption, so it does
not establish whether range, repeated baselines or output caused the silence.

To reproduce the native analysis, open the dumps with installed WinDbg/CDB and
Microsoft symbols. For `.34124`, use `.exr 0000008f361adbf0` and
`.cxr 0000008f361ad700`; for `.17656`, use `.exr 00000065c76ed9f0` and
`.cxr 00000065c76ed500`. Then `r`, `dps @rsp L600`,
`u GameAssembly!il2cpp_gchandle_get_target L12` and `u @rip-30 @rip+30` show the
original context and export jump (`+0x292f80` to `+0x2a8dc0`). These addresses
are dump-specific evidence, never implementation offsets.

## Changes

- Button hints are observed after UI updates and must remain visible for two
  frames. Checks include the label, canvas, inherited canvas-group opacity,
  active navigable and gameplay context. Global HUD hints cannot speak at login
  merely because their objects were initialized.
- Current hints wait for speech to become idle. Hidden or superseded hints are
  discarded before sending anything to the screen reader; matching hints are
  grouped and deduplicated. Rich-text markup is removed.
- Element focus ignores hidden UI and loading. Panel messages require a real
  open/closed transition, rather than every invocation of the panel methods.
- Tutorial and modal readers now capture visible localized title/body/action text
  after the display lifecycle, using two matching snapshots before speech. They
  own focus while active, preventing background controls from interrupting the
  body. Selection changes inside an announced modal remain available. Populated
  content does not depend on the backend reporting `IsSpeaking` correctly.
- Controller, quick, over-chat, location and gameplay-holder notifications are
  sampled from their actual visible widgets and spoken without interruption.
  Hidden/superseded notices expire locally. Explicit new deliveries rearm reused
  controller/quick/chat/location widgets and `currentGenericNotification`;
  nested roots collapse to their visible parent, preventing duplicate body text.
  Generic gameplay-holder child structure still needs runtime validation.
- Unchanged focus text and HUD hints no longer repeat merely because callbacks
  recur or child navigables change. Panel closing no longer interrupts speech.
- Dialogue reads the visible speaker and current localized subtitle, once per
  change. `Subtitle.formattedText.text` supplies the complete line when a visible
  typewriter prefix is still animating. Tutorials/modals take priority. Responses
  use scoped focus speech, with the initial selection queued after the body and
  deliberate navigation allowed to interrupt. Conversation end/loading clears
  stale dialogue. No choices are selected or advanced automatically.
- Radar suspension/failure clears its cached world references. Collected
  wrappers are checked before Unity null comparisons. Navigation audio also
  waits while the game's loading screen is visible.
- Radar queries now use `SamplePositionFilter`/`RaycastFilter` with the actual
  agent type and area mask. Do not reintroduce the broken filter-object overloads.
  Earlier host-double tests modeled valid filter methods and could not detect
  this installed-interop defect; the revised host exposes only primitive calls
  and checks both filter values on the origin sample and all four rays.
- Warning range now scales from `Actor.getRadius()` (never below agent radius),
  rather than only the potentially tiny helper NavMesh agent. Small camera drift
  no longer resets candidate transitions. Both correct adapter mismatches with
  D2R; their contribution to the latest silence is not yet proven in-game.
- Radar diagnostics report actual body/agent radii, depths, state/reset counts
  and cue generation/submission every five seconds. Audio diagnostics count real
  provider reads, submitted/consumed/canceled samples and stream state. Buffer
  consumption is evidence of processing, not confirmation of audible playback.
- Audio suspension clears managed sample providers and leaves the output
  running silently. NAudio `Stop()` followed immediately by `Play()` could
  overlap playback workers; that separate race has been removed from radar,
  quest ping and wall sonar. Ping trigger/read/silence share a lock so an audio
  read cannot overwrite a concurrent silence request. Device buffers may finish
  their already-submitted audio before silence; cleanup still stops/disposes.
- Combat feedback reads the nearest living hostile from the game's maintained
  `ActorManager.visuals` registry. It filters inactive, dead, invalid and friendly
  targets before comparing distances. No damage event or selected target is
  required. It speaks the localized name when it changes, with bounded frequency.

## Startup regression after the screen-content changes

The 14:42:09 dump `Last Epoch.exe.17572.dmp` identifies a separate regression
introduced with the popup reader. `BasePopupNavigable.OnSelected(Vector2Int)`
shares an empty native body with unrelated methods, including construction paths.
Harmony detoured that shared body, so creating `MenuAccessibility` incorrectly
entered `PopupContentShownPatch.Postfix` and `ScreenContentAnnouncer.TrackPopup`.
Its `Component.GetComponentInParent` call then used an invalid component receiver.

The dump stack contains `Plugin.Load` → `GameObject.AddComponent` → the
`ReversePInvoke(Int64, Vector2Int, Int64)` hook → popup postfix → `TrackPopup` →
`GetComponentInParent` → `UnityPlayer+0xa6d76`. Original exception/context records
are `00000054ac0ed370` / `00000054ac0ece80`; the fault reads `[rcx+0x20]` with
`rcx=0x10000007fff`. The coreclr fatal-handler exception is secondary. This is
unrelated to radar queries or audio playback.

Removed that indexed hook, plus two other aliased targets found by auditing the
new patches: `BossConfirmationPanel.OnUnlock` shares hide/deactivation code with
unrelated objects, and `ConfirmationPanel.IsActive` shares setters with login,
social and store panels. Real parameterless popup selection and concrete
confirmation/boss lifecycle hooks remain. The "mod loaded" speech now follows
successful component creation instead of preceding patch installation.

`tests/LeAccess.PatchSafety.Tests` resolves the 30 remaining new hook declarations
against installed metadata and checks their cached native reference ranges for
aliases. All pass; the three removed targets remain negative regression fixtures.
This supplements signature checks without loading native game code. It uses the
installed .NET 10 SDK's metadata tooling only in the test project; the mod stays
on .NET 6 with no new runtime dependency. Run with `dotnet run --project
tests/LeAccess.PatchSafety.Tests`; optionally pass the game directory after `--`.
Missing scan metadata is not proof of a unique native body, so actual runtime
validation remains necessary.

Real startup validation at 14:47:40 passed the previous failure point: the log
records `MenuAccessibility component initialized`, `LeAccess loaded successfully`,
the visible controller notice, and navigation in `LoginUI`. The session later
records submissions on `ButtonExitGame`; no newer crash dump was created. Direct
executable launch produced a separate service ownership error, so this verifies
startup/menu operation rather than online login or gameplay. Continue gameplay
tests through the user's normal launcher.
The user subsequently clarified that this is `Last Epoch Launcher.bat` in the
game directory; its command is `start "" "Last Epoch.exe" --offline`. Use that
launcher for subsequent runtime tests rather than the bare executable.

## Character-load regression: native argument marshaling

The later `Last Epoch.exe.33532.dmp` (15:53:16) reports an unhandled managed
`COMException`, HRESULT `0x80004005`, rather than a radar access violation.
The stack stops in `InterfaceMarshaler.ConvertToManaged` while entering a
reverse-P/Invoke callback, before any mod postfix executes. The native caller
at `GameAssembly` address `0x7ffa0af80745` calls `0x7ffa0af80220`, the entry detoured
to `0x7ff9e6bc3a24` in that session's BepInEx log. These are dump-specific addresses.

`IdolTutorialTooltipManager.AttemptTooltip(Il2CppObjectBase)` is the only current
hook with the exact interop base class as an argument. Installed
`TrampolineHelpers.NativeType` and Harmony's argument conversion recognize
**subclasses** of `Il2CppObjectBase`, but not that base type itself. The generated
callback consequently invokes CLR interface/COM marshaling on a game pointer.
A try/catch in the postfix cannot prevent this failure. Native alias checks
alone do not detect it; argument ABI checks are also required.

The fix replaces this hook with parameterless `Awake` registration and watches
only that instance's known tooltip root. Hidden watched roots remain registered,
clear obsolete speech and rearm on reopening. No global scene search or bridge
library replacement is needed. Ordinary tutorials retain their specific hooks.
The patch-safety suite now checks exact-base argument/return types (including
by-ref types) alongside native aliases and rejects four historical unsafe hooks.
All 30 current target signatures pass. The reader's 47 checks cover retained
hidden roots, close/reopen, visibility priority and collection without rescanning
the scene.

The next normal-launcher session (16:06:34, PID 40280) passed actual offline
character loading at 16:07:18 and continued gameplay. The log records the visible
movement tutorial, `Moverse. Usa el joystick izquierdo del mando para moverte.`,
through the screen-content reader. This verifies the previous character-load
failure point; it does not validate every tutorial or popup. The game was left
running while the following radar defects were investigated.

## Radar silence captured during offline gameplay

The same session provided two independent causes of silence:

- The body radius printed as 0.500, with agent radius 0.100, but tiny float
  differences replaced `RadarSettings` on many polls. Status windows recorded
  14–67 `Initial` resets in roughly 85 polls, repeatedly discarding confirmation.
  `Mathf.Approximately` now preserves settings identity for insignificant body
  size differences. Real body-size and configured depth changes still rebuild
  settings. A regression first reproduced the failure and now confirms that
  movement produces cues despite neighboring float values; navigation checks
  increased to 344.
- The output initially processed 1049 silent reads, then stopped on the first
  cue with zero consumed cue samples. Further cues were submitted but the read
  counter stayed frozen. NAudio's `SampleToWaveProvider` passes a `WaveBuffer`
  float view whose actual managed array type is `byte[]`. `Array.Copy` from a
  genuine `float[]` therefore fails, and `Array.Clear` also counts the wrong
  element size. The provider now writes individual float samples, including
  silence, through the typed buffer view. The real NAudio adapter reproduced
  `ArrayTypeMismatchException` before the fix and now passes cue/offset/zero-fill
  checks. Playback-stop diagnostics retain the exception and allow replacement
  only after the stop-completion event, with the existing five-second retry
  interval. Eighteen adapter/recovery checks pass without opening a device.

## Combat configuration and limits

`Combat feedback` in the existing BepInEx configuration exposes
`AnnounceNearestEnemy` (true), `SpeechIntervalSeconds` (2),
`NearestEnemyPollSeconds` (0.2), and `NearestEnemyAnnouncementFormat` (`{0}`).
Only the localized name is spoken by default. Timing values are accessibility
preferences. Old attacker/encounter/message-format keys are no longer read;
there are no damage subscriptions or attacker announcements.

Distance is the Euclidean distance between actor positions, limited by the
runtime `ControllerTargetingParadigm._SkillTargetMaxDistance`; equal distances
use instance ID as a stable tie-break. There is no verified shared player
in-combat flag: this defines a nearby combat neighborhood, and names can speak
on approaching hostiles before either side deals damage. Noncombat zones,
loading, pause, dialogue, focus loss and a dead/missing player suppress speech.

The probe uses the maintained offline Actor or online ActorVisuals registry,
without network damage attribution. The latest offline session found nearby
living hostiles but failed to resolve their names; [the naming correction and
runtime evidence](nearest-enemy.md) now cover separate visual displays and
ActorData localization. Audible delivery still needs user verification.
The nearest enemy must have a localized display name; missing names stay silent
instead of substituting a farther enemy. Same-name enemies do not cause repeated
speech when they exchange places. No automatic aiming, movement or attacks occur.

Verified interop declarations under `last epoch code/`: `ActorManager.cs:2600`
and its `ActorVisualsList.visuals:446` (registry), `ActorVisuals.cs:1581,4015,3328`
(dead/invalid/alignment), `ControllerTargetingParadigm.cs:39` (runtime range),
`SceneList.cs:1339` (noncombat zones), and `BaseDisplayInformation.cs:564` (name).

## Inventory and ground loot

The user then confirmed enemy speech but reported unreadable inventory and
ground items. Both now have dedicated readers without additional native hooks.

Inventory selection comes from the active `ItemContainerUI.currentlySelectedItem`
and `currentFocusSlot`. Generic ancestor-label extraction yields to this reader
within the selected container. A stable item selection reads the game's completed
primary tooltip: localized name/type, visible stats and requirements. Hidden
labels and the separate comparison content are excluded. The tooltip must match
the selected item and originate inside its container. If no matching tooltip is
available, the fallback is `ItemDataUnpacked.FullName` and
`LocalisedBaseTypeName`; it never borrows another item's stats. Empty slots clear
the previous item and announce their state. Changed data in the same selected
entry can be read again; unchanged contents do not repeat.

Ground loot follows `ControllerManager.instance.targeting.activeInteractableTarget`
when its `Targetable` type is `GroundLabel`, or the mouse's
`TooltipItem.highlightedTooltipItem` for a ground tooltip. It now reads only the
visible localized `PickupableGroundLabel.itemText`. Label/drop/item
identity protects against reused labels. Lost, hidden, collected or picked-up
targets clear pending speech; inventory, other menus, loading and dialogue
suppress retained gameplay targets. It does not enumerate nearby drops.

Inventory uses `Items/ItemTooltipReader`; both readers use the existing central
speech transport. Inventory owns foreground speech over passive notifications/combat;
pending item content precedes button hints. Ground selection precedes passive
speech while its current text settles. Neither changes movement, selection,
pickup or equipment. Actual inventory navigation and ground targeting still
need a listening pass in the updated game build.

The inventory suite passes 32 checks and the ground-loot suite 61. They cover
matching item/opener identity, live tooltip initialization, stable changes,
empty slots, comparison isolation, pooled drops, pickup/loss, visibility and
menu ownership. These use the production readers with host doubles and do not
establish the game's actual focus hierarchy at runtime.

The next user report and log confirmed excessive automatic ground readings:
the same drop spoke its name, then a complete tooltip including stats, forging
potential and action prompts. Ground focus now stays name-only even when that
tooltip appears, changes or closes. Full properties remain in inventory. This
also removes whole-tooltip traversal from every ground-focus frame. Pending
zone speech defers loot; only the latest still-present item's name survives.

## Controller wheel, zones and nearby NPC names

The controller menu does not use the normal Selectable focus route.
`ControllerMenuAccessibility` follows the active `ControllerMenuMap.controllerMenu`,
its `FocusedRadialMenu` and actual selected sector. It reads the sector's visible
localized option or assigned localization key, settles changes, handles both
wheels and suppresses generic underlying focus. It uses no new native hooks.

The prior log submitted `Bosque en llamas. Era Divina: -12 a. E.` through a
passive notification, but the user did not hear zone narration. `ZoneAnnouncer`
now reads `SceneList.TryGetNonDefaultSceneDetails` / `SceneDetails.LocalizedName`
once after each scene loads and the player is available. Zone speech interrupts
passive backlog, yields to actual UI/dialogue and precedes loot/NPC/hints.
Optional year/chapter comes only from a visible location notification captured
in the same scene with the exact current localized title. Location hooks route
to this reader rather than speaking a second generic notification. Obsolete
pending zones are replaced; already-read scenes are not traversed every frame.
The menu/zone suite passes 34 checks and notifications pass 47.

NPC cues worked, but recorded names were empty. Name resolution now includes
the selected NPC's owned hierarchy and explicit relay/conversant references,
using actual localized tooltip, display and dialogue-name data. Names announce
once on approach or change, independently of sound volume, after higher priority
text. NPC tests pass 108 lifecycle/name checks and 44 probe/name-source checks.
Actual NPC name recovery still needs runtime confirmation. Radar stutter evidence
and its buffering mitigation are in [navigation notes](navigation-radar.md).

The integrated follow-up build passed with zero warnings/errors and matches the
installed DLL (SHA-256
`DD8BD83F06CC75AE57E589A6093451CFAC876059A3269FBDA80C0856365B3922`).
The normal BAT launch at 17:46:47 (PID 40844) completed plugin startup with no
new crash dump. Saved config confirms `AnnounceNames=true` and radar
`AudioBufferMilliseconds=100`. The user subsequently confirmed controller-menu
reading and improved radar audio. Complete elimination of cuts and zone/NPC
speech still need checking. The captured session recorded zero callback stalls
or output stops; this does not prove that every audible interruption is gone.

## Skill identity and assigned controls

The next user report is specifically the Skills screen opened from the controller
wheel: only `Más información` was spoken, with no ability name or cast button.
The captured log confirms `[ControllerMenu] Habilidades` followed by repeated
`[ButtonPrompt] Más información` and no ordinary `[Element]` focus events.
Skills use a custom navigable, so the generic Selectable reader missed them.

`SkillSelectionReader` reads `SkillsPanelManager.selectables` through the
interop `Il2CppSystem.Array` API, checks rank/bounds, and selects
`[currentlySelectedIdx.x, currentlySelectedIdx.y]`. The installed native
`FocusSelectable` and `SlotIsEmpty(int x, int y)` bodies confirmed this coordinate
order; no array offsets are used by the mod. This works with the tooltip closed.
The selected icon supplies its ability; localized ability data supplies its name
and description. A visible tooltip contributes details only when its owner,
ability, visibility and completed initialization match the selected skill.
Ordinary active Selectables cover the assignment grid and hotbar.

`SkillAccessibility` settles the current selection, discards obsolete pending
speech on navigation, and announces name, assigned controls and description in
that order. Foreground popups/dialogue keep priority; passive hints wait for the
skill name. Bindings refresh immediately on selection and at most five times
per second while stationary to catch remaps without traversing glyphs per frame.
Closing or temporarily hiding details acknowledges the already-spoken selection
without interrupting it with the shorter fallback. Changed visible content still
speaks, and a changed control/name uses fresh content rather than retained stats.

Assigned controls come from the matching live `AbilityBarIcon` and its own
`InputCharacterForAction.actionName` or its owned typed prompt components.
Rewired's actual action metadata verifies
that it is one of the game's `InputActions.Ability1` through `Ability5` actions;
`PromptsInfoProvider` supplies the configured button names. No slot-to-button
table or default controller layout is guessed. The existing game assembly
`Rewired_Core.dll` and `Sirenix.Serialization.dll` are compile-only references,
never copied into the plugin. Unknown bindings are omitted rather than guessed;
actual hotbar glyph coverage still requires an in-game listening pass.

To validate with the controller, open **Habilidades** from the wheel and move
between skills without opening **Más información**. Hear the localized name,
assigned cast control (when equipped) and description. Open/close extra details,
move quickly to another skill, and change an assignment; confirm no old skill
or shortened fallback interrupts the current selection. Also check the ability
assignment grid and moving from a specialization slot into its tree and back.

Validation: the source-linked Skills suite passes **63 checks**, including actual
reader, binding and announcement code with game-object doubles. Existing hint
and combat suites pass 19 and 67 checks. The main build has zero warnings/errors;
the deployed DLL matches SHA-256
`8C5D84190EFADCAE8B10F371E16E901ADD80B9F7266559765508D95B9F67324C`.
The normal offline BAT startup at 18:08:17 (PID 20176) loaded the plugin and patches
successfully. Skill navigation and audible controller labels remain unverified
in the real game; automated checks do not establish that prefab coverage.

## Runtime validation still needed

Build/deployment succeeds. Navigation passes 345 checks, including the primitive
filter arguments, footprint scaling, camera drift and lifecycle recovery. Combat
passes 67 checks. Managed audio providers pass 1024 assertions (mostly repeated cancellation stress checks),
without opening an audio device. Inspection of the compiled mod's IL confirms
calls to `SamplePositionFilter` and `RaycastFilter` and no filter-object route.
Run each project under `tests/` with `dotnet run`; these checks do not substitute
for running the corrected mod in the game.
Screen-content tests pass 47 checks, dialogue tests 23, notification tests 47,
and hint-tracker tests 19. They cover visibility, ownership, stale content,
overlapping notification roots, re-delivery and unchanged navigation hints.
The new patch safety suite checks 30 current hook signatures across screen,
dialogue and notification readers and rejects known shared native bodies.
The existing `DialogueSystem.dll` is referenced with
`Private=false`; it is supplied by the game's interop environment, not deployed
as a new dependency.

The integrated build at 16:24:32 succeeded with zero warnings/errors and was
verified identical to the installed DLL (SHA-256
`02BA1BB2C2F04D634D94B5E4C8847E288B8CEE34714BEC0C8CF01F371D7D65D1`).
This includes the radius-jitter fix, NAudio adapter/recovery fix, and inventory/
ground-loot readers. The earlier 16:06 gameplay session used the previous DLL.

The normal BAT launch at 16:25:04 (PID 30504) loaded NVDA, installed all patches,
and passed offline character loading again. During player movement the updated
radar processed five cues / 69,120 samples in full, stayed `Playing`, and recorded
zero output stops or recoveries. Five-second gameplay windows now show no repeated
initial resets. This directly validates both runtime failure paths; the listener
still needs to confirm audible output. The movement tutorial also spoke again.

1. Start the game in Spanish: the login menu should not announce forge or
   inventory hints. Open relevant menus and confirm their visible hints remain
   available after the focused control is read.
2. Load the character, approach a wall, then pass an opening. The reactive radar
   intentionally stays quiet for its initial baseline and unchanged terrain.
3. Change zones and repeat; pause/dialogue/loading should suspend navigation
   audio, and subsequent movement should restore it without restarting.
4. Approach two enemy types before taking damage, then move so the nearest
   changes. Confirm the closest living hostile is named even if a farther enemy
   hits the player or is selected. Kill the closest and confirm the next living
   enemy is read. Check no repetition or stale speech after pause/zone changes;
   repeat online to validate the visual registry there.
5. Repeat the reported Z12 load/movement scenario with both radar and quest ping
   enabled, then pause/resume and change zones. Confirm the radar reaches its
   ready state without a filter-getter exception. Preserve a new dump if any
   crash recurs. The user has confirmed no crashes in the subsequent session.
6. Open an actual tutorial and a confirmation window: read the visible title,
   complete body and actions once. Move between actions, close/reopen, and verify
   background focus/hints do not cut the text. Test a quest/location/controller
   notification and confirm hidden or expired messages never replay later.
7. Speak to an NPC: hear the visible speaker and full subtitle once, then the
   initially selected response. Navigate responses and close the conversation;
   verify no old lines replay after returning to gameplay.
8. Open inventory with the controller, move between items and empty slots, and
   read selected-item stats without comparison-item or previous-item text.
   Change a selected item's contents and close/reopen inventory.
9. Target a ground drop with the controller, switch to another, pick it up, and
   open a menu while a drop is targeted. Repeat with mouse hover. Confirm names
   follow only the current visible target and no stale pickup repeats.

## D2R reading behavior transferred

The comparison used the local `d2r-access` sources: `game_text_reader.cpp` and
`plugin.cpp` for foreground ownership; `ui_text.cpp` for coherent body/selection
readouts; `ui_notifications.cpp` for visible changes; and `ui_message_monitor.cpp`
for modal population and noninterrupting closes. Its reader relies on UI ownership
and backend queue semantics, not `IsSpeaking` or estimated speech-duration timers.
The LE port preserves its existing centralized transport and uses game-specific
display hooks, not D2R widget offsets or timing constants. No new runtime package
or native library is introduced.
