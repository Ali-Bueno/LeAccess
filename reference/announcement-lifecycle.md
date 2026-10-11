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

## Generic modal windows

Focused panels found only through the `IModal` cast (loot-filter rule editor, pickers, stash configuration)
read once on open: the single `IModalTemplate`'s `_title`/`_message` (`ConfirmationModalBase` fills them from
`ConfirmationModalOpenData.Title/Message`), then visible text outside any `Selectable` or `ScrollRect`.
Lists and control captions belong to focus navigation. While open, only a changed template header/body is
reread, without interrupting; child modals and alt-tab do not repeat it; hiding rearms it. A delivered
generic modal no longer withholds passive notifications. Explicit warning/alert/dungeon panels are unchanged.

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

## Accepted output and recovery

Previously these readers acknowledged text when it settled, before checking
whether the output transport accepted it. Rejected unchanged content therefore
stayed silent after recovery. Owned screen-content, dialogue, rendered subtitle
and notification readers now use `ScreenReader.TrySpeak` and acknowledge only
accepted output. Acceptance covers speech or braille transport; it does not
prove playback or audibility.

`ScreenContentTracker.Observe(..., acknowledge: false)` offers the settled
candidate without marking it delivered. `Acknowledge(id, text)` verifies that
the same source still has that settled current candidate. Existing consumers
retain the default automatic acknowledgement until migrated explicitly.
Notifications acknowledge their collected source IDs only after acceptance of
the combined current batch. Countdowns additionally validate the collected
native coroutine identity.

A rejected attempt leaves current owned content pending. Each later update
rereads visibility, text, identity and priority before retrying; hidden, changed,
expired and superseded sources discard the old attempt. There is no global
replay queue. Submitted NPC lines retain their existing bounded pending tracker
and original ten-second expiry deadline; rejection does not renew that lifetime.
Submitted occurrence identities are not reused on tracker reset, so accepting
an old attempt cannot acknowledge a newer identical line.
Previously accepted submitted/rendered duplicates keep their deduplication.

Validation: dialogue **73**, screen content **211**, notifications **115**, and
visible subtitles **166** checks pass. The main no-deployment build has zero
warnings/errors. Audible timing, actual countdown restart delivery online, and
interaction with sustained live world narration still require listening.

## Pickup lines give way (2026-10-11, user report)

User: picking things up made the reader queue everything ("25 de oro. 30 de oro. Poción 1 de 3. Maza …"), late enough
to delay combat and life warnings. Pickup lines are the gold gain (`GoldPickupAnnouncer`), a potion count on its own
(`VitalsAccessibility`, no life or mana crossing in the same line) and the ground item the controller targets
(`GroundLootAccessibility`). They go through `ScreenReader.TrySpeakPickup`: a pickup line spoken within
`PickupSpeechSeconds` (4 s) of the previous one interrupts it instead of queueing. Life and mana crossings, the
nearest enemy and boss/elite life use `TrySpeakOverPickup`, which interrupts a pickup line still in that window and
otherwise keeps its own interrupt flag. Any other accepted speech closes the window, so a pickup never cuts it and is
never cut for it. Tests: `LeAccess.ScreenReader.Tests` (8 checks). Unverified live: whether 4 s fits long item names.
