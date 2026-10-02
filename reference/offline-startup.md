# Early online/offline selection during startup

Implemented for installed Steam 1.5.0.1 / build 25663618 on 2026-10-02.

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
  state; `_activeTransition.HasValue` indicates an outstanding transition.
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

- `dotnet run --project tests/LeAccess.LoginTransition.Tests`: **49 checks**,
  source-linking the guard and both production prefixes. Covers the observed
  startup sequence, both mode choices, deduplication, replacement/cancellation,
  lifecycle, focus/loading delays, English/Spanish speech and native failures.
- Full installed native hook audit: **31 regression checks**, **50 declarations
  / 49 native targets**. Both new click targets have unique native bodies and
  supported signatures.
- Main build with `-p:SkipPluginDeployment=true`: zero warnings/errors.

No game was launched for validation. Fresh listening must confirm an early
offline press says it is loading, then continues once into character selection.
`[StartupMode]` logs distinguish deferral, replay, cancellation and failures
without per-frame output.
