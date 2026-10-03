# Screen-reader connection lifecycle

The existing centralized Tolk transport now recovers when a reader starts or
restarts after LeAccess initialization. Availability queries probe at most once
per second using Unity's unscaled clock; missing native libraries also retry at
that cadence. A loaded Tolk driver is retained across reader restarts, rather
than repeatedly loading/unloading the native library. Repeated missing-reader
and transport-failure diagnostics are suppressed.

`Speak` marks `SpokeThisFrame` only when native `Tolk.Output` accepts the output.
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
cases reproduced before the fix; all 34 lifecycle checks now pass, including
late startup, restart, exact probe deadlines, rejected output, exception recovery,
bounded/deduplicated retries and shutdown/reinitialization. Production builds
with `SkipPluginDeployment=true`, zero warnings/errors.

Live NVDA restart/listening remains pending. A failed announcement is not replayed
after reconnecting: foreground ownership belongs to its feature reader, and
replaying saved text could announce a selection or world state that has changed.
