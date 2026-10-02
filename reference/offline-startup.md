# Early online/offline selection during startup

Initial fix implemented for Steam 1.5.0.1 / build 25663618 on 2026-10-02.
The one-click follow-up below uses installed 1.5.1 / build 25672295.

## One-click follow-up, 15:23 session

`BepInEx-152324.log` retains the failure after the first fix: the offline click
is deferred at line 464, the game reaches `Login` at line 488, and the guard
logs `NullReferenceException` at line 490. There is no `resuming` message before
that exception. The old `Update` catch discarded the pending click; a later
explicit press reached CharacterSelect at line 529.

The generated `_activeTransition` getter copies the native `Nullable<UniTask>`,
passes it through `il2cpp_value_box`, and constructs a managed Nullable wrapper
from the returned pointer. Installed native `il2cpp_value_box` (export RVA
`0x2F1780`, implementation `0x31D6A0`) returns zero when the nullable is empty
(`0x31D6DE`–`0x31D6E5`). Constructing the wrapper from zero fails precisely when
startup finishes. The earlier mock retained a wrapper with `HasValue = false`,
so it missed the real nullable-boxing boundary.

`LoginTransitionState` instead resolves the `_activeTransition` field once using
Il2CppInterop metadata after static class initialization. It calls the supplied
`il2cpp_field_get_value_object(field, IntPtr.Zero)` API and tests the returned
pointer directly: zero means no active transition. Current native export
`0x33FDA0`, implementation `0x2F4060`, reads the static field and calls the same
box implementation at `0x2F4191`. Production uses no hardcoded addresses, field
offsets, unsafe memory reads, Nullable wrappers or new hooks.

A failed readiness/ownership observation now preserves the pending request and
logs its exception once, including the stack. A positively observed owner or
application-state change still discards it. The request is cleared before calling
the original native handler, so handler failures are never retried as new actions.
Persistent ownership and visibility restrictions otherwise remain unchanged.

## Observed failure

The first failed test survives in Unity's `Player-prev.log`, last written at
10:24:42 local time. It was preserved before another launch at
`%TEMP%/LeAccess-offline-stall-20261002/Player-prev-102442.log`.

| UTC time | Evidence |
| --- | --- |
| 13:23:19 | Application enters `SystemLoading` (line 59). |
| 13:23:44 | Login scene is loaded (line 1388); application startup is still in progress. |
| 13:23:46 | The user's offline choice initializes offline networking (line 1463). |
| 13:23:47 | `InvalidOperationException: Cannot transition to CharacterSelect while transition to SystemLoading is in progress` (line 1482). |
| 13:23:51 | CharacterSelectScene preload finishes, then application enters `Login` (lines 1558, 1601). No successful CharacterSelect transition follows. |

The exception stack identifies `LandingZonePanel.AdvancePlayerToCharacterSelect`
→ `LoginController.OnEnterState` → `ClientStateManager.Transition`. This is a
native state-transition failure, not sufficient evidence of a screen-reader
failure or complete process freeze. The process later logs normal quitting.

The successful next session enters `Login` at 13:26:33, switches offline at
13:26:38 and enters `CharacterSelect` at 13:26:39. Both sessions contain the
separate `EpochMasteryUI.ShouldBeActive` null-reference error, so that error
does not explain the distinguishing failure.

## Native contract and fix

Installed native inspection confirms:

- `LandingZonePanel.OnPlayOfflineClicked`, RVA `0x1F98780`, invokes
  `AdvancePlayerToCharacterSelect(false)` and forgets its native async task.
- `OnPlayOnlineClicked`, `0x1F987B0`, invokes the same flow with `true`.
- The advance state machine, `0x1FA6B10`, disables both mode buttons before
  its waits. A premature transition can therefore leave an inert landing UI.
- `ClientStateManager.CurrentClientAppStateType` reads the current native
  state; `_activeTransition` is a native nullable task. Read its boxed presence
  through `LoginTransitionState`, not the generated Nullable wrapper.
  `LoginPanelBase.loginController`, its `landingZonePanel`, and
  `FSM.CurrentState == LandingZone` identify the actual UI owner.

`LoginTransitionGuard` prefixes only the two explicit mode-click handlers.
For an owned active landing panel during `SystemLoading`, or `Login` with an
outstanding transition, it retains the user's actual click and announces a
localized loading message once. Repeated clicks are deduplicated; the latest
online/offline choice replaces the pending choice.

The existing `MenuAccessibility.LateUpdate` checks readiness. It resumes the
original handler once after native state is `Login`, the transition has ended,
the application is focused and the loading screen has closed. The exact panel,
controller, FSM, selected button and loaded scene must still match. Hidden,
destroyed, disabled, replaced or collected owners, leaving LandingZone, or
moving into another application state discard the request. Plugin cleanup
also clears it. A ready manual click clears any older pending request.

The pending request is cleared before calling the original handler, preventing
recursion or repeated retries after a native error. Speech failure cannot
release an early action into the known race. No state setter, forced transition,
button activation, timeout, automatic character selection or unrequested login
action is added. Native behavior outside the verified startup scope remains.

## Validation

- `dotnet run --project tests/LeAccess.LoginTransition.Tests`: **56 checks**,
  source-linking the guard and both production prefixes. Covers the observed
  startup sequence, both mode choices, deduplication, replacement/cancellation,
  lifecycle, focus/loading delays, English/Spanish speech and native failures.
  The current native-field reader is also source-linked. Follow-up regressions
  reproduce empty-nullable boxing, check metadata caching, recover a transient
  observation failure without another click, and cancel on actual owner change.
- Full installed native hook audit: **31 regression checks**, **59 declarations
  / 58 native targets**. Both click targets have unique native bodies and
  supported signatures.
- Main build with `-p:SkipPluginDeployment=true`: zero warnings/errors.

The follow-up main build passes with zero warnings/errors and deployment skipped.
No game was launched for validation. Fresh listening must confirm an early
offline press says it is loading, then continues once into character selection.
`[StartupMode]` logs distinguish deferral, replay, cancellation and failures
without per-frame output.
