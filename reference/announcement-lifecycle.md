# Announcement lifecycle audit, 2026-10-03

Current Steam build: **25690669**. Source-linked checks reproduce the defects
below without running the game or opening an audio device.

## Dialogue turns and passive tutorials

`DialogueAnnouncer.ShowSubtitle` assigned the new native subtitle pointer before
the polling path could detect a changed turn. Consecutive native subtitles with
identical speaker/body therefore shared one spoken-text entry. The hook now
rearms on a changed nonzero native identity. Duplicate callbacks, a late first
hook for an already rendered line, and ordinary/reminder transfers keep their
existing deduplication. A deferred turn replaced by a later turn does not replay.

Passive tutorials previously spoke with interruption while a conversation was
active, despite correctly leaving input ownership with the game. They now settle
behind active dialogue and earlier same-frame speech. The current visible text
is reread until a clear frame; blocking modal bodies retain their priority.
`HasActiveContent` uses `FindForeground`, which explicitly excludes sources
whose `BlocksInput` is false. The tutorial therefore cannot block the dialogue
it is waiting for. The screen-content suite links both actual announcers and
runs the game's service order to check this interaction, latest deferred text,
and blocking-modal release; its dialogue activity is not a writable test double.

## Encounter abandonment countdown

The native encounter countdown is an abandonment instruction. It is not evidence
of damage outside the gauntlet. See [native source evidence](hazard-sources.md)
for the current countdown and restart RVAs.

The generic notification reader previously narrated the whole localized
`Encounter_AbandonTitle` once per second because each integer change settled
successfully. `EncounterCountdownReader` now handles only the rendered
`RageOfMorditasOutOfGauntletNotification._countdownText` authenticated by its live
ancestor owner and exact field identity. It speaks that full displayed caption
once after two observations, then leaves timer ticks silent. Sibling labels keep
their ordinary text-change behavior; they never repeat an outdated timer.

Native `StartCountdown` assigns a fresh `_currentCountdownCoroutine` even when
the same visible widget receives no delivery hook. Its pointer rearms the
caption, including identical restart text. Explicit delivery, hidden root or
label, focus/loading reset and destroyed sources also discard old state. A
deferred caption uses the latest visible timer. Missing or collected coroutine
ownership retains ordinary visible-notification behavior.

`NotificationAnnouncer.UpdateUrgent(foregroundContentActive)` provides an early
pass for this authenticated countdown alone. It respects foreground content,
same-frame speech, modal ownership, loading, focus and reader availability.
Its urgent-only eligibility also requires unsuspended gameplay input and no
active text field or screen keyboard. Native ownership queries fail closed.
Ordinary menu-action notices retain their existing passive delivery while these
gates defer a still-visible countdown.
It may receive a turn while ordinary quest/map/loot narration remains pending.
That authenticated caption interrupts prior passive speech once it wins a clear
frame, so it cannot queue behind an earlier long quest. Ordinary notices retain
noninterrupting delivery.
The normal pass keeps its existing priority and does not mark sibling labels
spoken during the urgent pass. A per-frame observation guard preserves the
two-observation settle when both passes run. Integration belongs between visible
subtitle updates and quest/world narration; later critical vitals retain their
existing priority.

Validation: dialogue **68**, screen content **200**, notifications **103**, and
visible subtitles **143** checks pass. The main no-deployment build has zero
warnings/errors. Audible timing, actual countdown restart delivery online, and
interaction with sustained live world narration still require listening.
