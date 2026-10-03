# Screen-reader connection lifecycle

The existing centralized Tolk transport now recovers when a reader starts or
restarts after LeAccess initialization. Availability queries probe at most once
per second using Unity's unscaled clock; missing native libraries also retry at
that cadence. A loaded Tolk driver is retained across reader restarts, rather
than repeatedly loading/unloading the native library. Repeated missing-reader
and transport-failure diagnostics are suppressed.

`TrySpeak(text, interrupt = true)` returns true only when native `Tolk.Output`
accepts speech or braille output. Existing public void `Speak` delegates to it.
`SpokeThisFrame` is marked only for accepted output; a later failure in the same
frame does not erase an earlier accepted output.
Rejection or exceptions suspend availability until the next probe. Shutdown
clears detection/frame state and disables automatic probes until an explicit
`Initialize`. Existing `Speak`, `SpeakQueued`, `Silence`, `Braille`, availability
and status interfaces remain compatible.

## Contract evidence

Targeted ILSpy inspection of local `references/tolk/TolkDotNet.dll`, type
`DavyKager.Tolk`, confirms `Output` returns a native boolean and exposes
`IsLoaded` and `DetectScreenReader`. The corresponding upstream
[native detection/output implementation](https://github.com/dkager/tolk/blob/master/src/Tolk.cpp)
checks whether its active driver remains active and searches again when needed.
The [NVDA driver](https://github.com/dkager/tolk/blob/master/src/ScreenReaderDriverNVDA.cpp)
checks the running NVDA instance and controller result, and its output succeeds
when speech or braille is accepted. A successful return therefore establishes
accepted accessibility output, not audible playback completion.

The [Tolk contract](https://github.com/dkager/tolk#design) requires callers to
manage its thread/COM use; existing calls remain on the game thread, with no
background reconnection worker. Braille/speech-status unsupported results do
not establish disconnection by themselves.

## Validation and limits

The source-linked `LeAccess.ScreenReader.Tests` suite substitutes a managed
Tolk stub and refuses to run if an NVDA client DLL is present beside its test
executable. No native reader, speech, device or game is opened. Twelve failure
cases reproduced before the lifecycle fix; all 41 lifecycle/acceptance checks now pass, including
late startup, restart, exact probe deadlines, rejected output, exception recovery,
bounded/deduplicated retries and shutdown/reinitialization. Production builds
with `SkipPluginDeployment=true`, zero warnings/errors.

Live NVDA restart/listening remains pending. The transport has no replay queue.
Inventory, skill and passive services now prepare their current candidate without
marking it spoken; acknowledgement and successful-output logging follow only a
true `TrySpeak` result. Rejected/unavailable output remains pending while the
selection is still owned. A failed manual request retains only focus-bound intent,
and rebuilds current details after recovery. Automatic retries yield to accepted
output in the current frame. Selection/owner changes, hidden or collected controls,
unready native state and lifecycle reset discard that intent; there is no invented
time-to-live for an otherwise live selected control.

Source-linked service tests first reproduced lost output after rejection. The
current suites pass **219 inventory**, **189 skill** and **107 passive** checks,
adding 12 acceptance/retry checks to each suite. Coverage includes fresh manual
details, unavailable output after an accepted brief, same-frame priority and lost
ownership without stale replay. Tests use managed sinks and do not open native
speech, audio, devices or the game.

The same explicit acknowledgement now applies to visible modal/tutorial text,
dialogue turns, cinematic/video subtitles, nearby NPC barks and notifications,
including the current Morditas countdown occurrence. Their source ownership,
settling and submission expiry remain in the individual readers; see
[announcement lifecycle](announcement-lifecycle.md) and [NPC subtitles](npc-subtitles.md).
Quest-journal reading now uses the same focus-bound contract. Automatic quest
and dynamic-objective updates commit their shared pace/fairness only on accepted
output; see [quest updates](quest-updates.md). [Vitals](vitals-api.md) keep a
silent first/session baseline and retry current resource crossings. [Orientation](automatic-orientation.md)
does not acknowledge a target or unlock its speech-dependent pulse after rejection;
manual direction reports advance progress comparison only when accepted.
Gameplay manual reports remain explicit button requests, without delayed replay.
Other stateful menu, zone, map and nearby-entity readers still need acceptance-path
review. This does not establish universal recovery across the whole mod.
