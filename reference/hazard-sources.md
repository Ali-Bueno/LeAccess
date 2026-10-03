# Danger and encounter warning sources

Read-only audit of Steam build **25690669**, 2026-10-03. The binary and metadata
hashes are recorded in [Steam port notes](steam-port.md). Native addresses below
describe this build only; production code does not use them as offsets.

| Source | Meaning and current coverage |
|---|---|
| Vitals | Actual local health, ward and mana changes use the existing critical/band reader. This reports player state, not the shape or timing of an incoming attack. |
| Native warning/modal text | The [window reader](dungeon-warning-reading.md) reads the focused visible warning. A modal's existence is not proof of a nearby environmental hazard. |
| Encounter abandonment | `RageOfMorditasOutOfGauntletNotification` renders a localized countdown when all players have left the gauntlet. The [typed notification reader](announcement-lifecycle.md) owns its text and restart lifecycle. |
| Morditas return arrow | `RageOfMorditasGauntletWarningPointerVisualController` exposes an actual target and local player transform. It is a return-to-encounter indicator; it is not yet an orientation target. |
| Attack telegraphs and dangerous ground | General visual-only damage areas still lack a verified unified player-facing source. Ability object names, visual effects and haptics are insufficient to infer hostility, affected floor, safe direction or time to impact. |

## Countdown evidence

`RageOfMorditasGauntletVisualController.OnAllPlayersOutOfGauntletChanged`
(RVA `1514C40`) forwards the native status and encounter configuration duration
to `Notifications.ToggleRageOfMorditasOutOfGauntletNotification` (`1780B50`).
That method creates the notice through `NotificationSystem` if needed. For an
existing notice, another true status calls `StartCountdown` directly; false
calls `Hide`. Thus the generic channel-delivery hook alone cannot detect every
restart of the same visible widget.

`StartCountdown` (`2077EA0`) assigns a newly allocated coroutine to
`_currentCountdownCoroutine` on each call. Its `MoveNext` (`207E350`) renders an
integer countdown using `Localization.GetTextFormat("Encounter_AbandonTitle",
remaining, fallback)`, with fallback `Abandoning Event in {remaining}s`. It
waits one second, subtracts one, and repeats until hiding the notice. The full
instruction therefore changes every second; it is not an unstable per-frame
decimal label. The existing two-observation content tracker alone would repeat
the full sentence each second.

These facts were resolved from freshly generated `LE.dll` metadata and the
installed `MethodAddressToToken.db`, then checked against `GameAssembly.dll`.
String literals were decoded using the installed LibCpp2IL metadata-39 format:
12-byte section headers and four-byte literal data indices. No networking
method, game method or audio device was executed for this inspection.

## Arrow evidence and remaining validation

`OnInGauntletStatusChanged` (`15150C0`) negates the player's in-zone status
before calling `SetPointerActive`; when the pointer has not loaded yet, it
retains that negated status. Pointer `Init` (`151CC60`) stores its supplied
look-at target and obtains the local player's transform from
`PlayerFinder.getLocalActorSync`. These are useful future orientation sources,
but require live ownership/lifecycle checks and distinct wording from an
ordinary quest marker. The current mod does not infer a safe route from this
arrow or claim that the region outside it deals damage.

Live checks remain: countdown appearance and return/restart while objectives
and loot are updating, online ownership of the return arrow, and actual
attack/ground telegraphs in each dungeon. Automated notification tests establish
delivery, deduplication and ownership logic; they do not prove audible timing.
