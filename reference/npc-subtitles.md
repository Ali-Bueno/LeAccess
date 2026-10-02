# Visible NPC subtitles — Steam 1.5.0.1

## Ambient speech and the offline NPC channel (2026-10-02)

The later installed `BepInEx/LogOutput.log`, last written 11:06:42 local,
records the complete Burning Corpses description at line 755, Grael's dialogue
at 1016–1033, and ambient lines such as “Fuego... por todas partes...” at 1669
and “No es seguro volver.” at 1676. This confirms delivery through some existing
rendered subtitle paths; it does not establish complete NPC/player coverage.

Native inspection found another speech route that has **no floating label**:
`VoiceLineHandlerBase.TryPlayLineInternal` (RVA `0x2835890`) plays its selected
voice, checks native proximity, localizes text at `0x2835C57` and actor name at
`0x2835CB7`, then submits an NPC message through `SystemMessageService`.
`NpcBarkInChat.Interaction` (`0x187A920`) also submits localized NPC text.
`ActorBarkHandler.BarkImpl` (`0x27742E0`) publishes both a floating bark and that
message, so the two paths must be deduplicated.

`LE.Chat.Common.ChannelCategory.NpcDialogue` identifies this authored channel.
Crucially, `LE.Chat.FullOfflineChatSystem.SubmitNpcMessage` is the shared empty
native body at `0x3D2F40` (`ret 0`): polling chat labels cannot recover these lines
in full offline mode. The reader now observes a postfix on the unique upstream
`LE.Services.SystemMessageService.SubmitNpcMessage(string content, string sender)`
at `0x1C9AB00`. It consumes the game's already localized submission, never raw
voice assets, ordinary player chat, or hidden conversation database entries.
This additional path intentionally does not require a visible chat window.
The shared empty offline method is **not hooked**.

The callback captures only cleaned managed strings. A locked queue drains on the
existing subtitle update, uses noninterrupting centralized speech, and yields to
cinematic/current speech. Interactive dialogue and modal content discard queued
ambient lines; focus loss/loading/reset clear them. `SubmittedSubtitleTracker`
uses explicit accessibility backlog limits of 16 pending lines and ten seconds,
so a burst cannot build an unbounded or stale narration queue. These are mod
scheduling limits, not inferred game durations. Duplicate submissions and the
same floating/cinematic occurrence are suppressed in either delivery order.
Later repeated lines can speak again. New-source delivery logs
`[DialogueNpcMessage]`; rendered sources retain `[Dialogue]`.

Authored player narrative is included when the game sends it through this NPC
channel or one of the existing subtitle surfaces. This does **not** transcribe
every player vocalization: `PlayerVoiceEventManager.EventVoiceLine` carries FMOD
events without subtitle text; its `play` native body (`0x15B3B00`) invokes voice
playback without publishing an NPC line. Combat/error barks with no authored
text remain outside subtitle reading.

The source-linked suite passes **69 checks** (38 prior + 31 new), including
offline/no-UI delivery, localization, priority, lifecycle, empty content, bounded
backlog, expiration, repeated speech and both duplicate orders. The installed
native audit passes 31 regressions and **51 declarations / 50 targets**, including
the new hook. Listening to the added path and player narrative still requires
the user's next session; no game was launched for this work.

## Burning corpses examination follow-up (2026-10-02)

The user confirms that **Cadáveres ardientes** is named on approach, but its
examination description remained silent. The session ending 10:27:34 records
`[NPC name]` at lines 470/482/494/500/516. Dialogue attaches successfully, but
lines 504/507/510 report `visible=False`, including `responses=True` at 507.
This rules out failed UI discovery and identifies a still-unread displayed body.
The log is preserved in `%TEMP%/LeAccess-offline-stall-20261002/` as
`BepInEx-inspection-102734.log`.

Installed Z22's Burning Corpses listener 15147 and relay 15270 point to child
trigger 14991, external conversation **658**, `Burning Corpses Inspect`. Node 1
is the NPC description from actor 311; node 2 is the player's ellipsis response,
with empty dialogue text. There is no custom UI or conversation setting override.
Its standard response menu retains the NPC statement in a separate subtitle
reminder, which the reader previously omitted.

`PermaLoad.bundle`'s `TextMeshProDialogueUI` has these distinct rendered sources:

| Field | Serialized source |
| --- | --- |
| `dialogue.npcSubtitle.line` | 2473897409730845878, ordinary NPC line |
| `dialogue.responseMenu.subtitleReminder.line` | 4507152227624626032, NPC Subtitle Reminder Line TextMeshPro |
| Shared `portraitName` | 8206214548290545209 |
| Outer `dialogue.panel` | 8262438308549710308 |

Both `responseMenu.panel` and `subtitleReminder.panel` are **null** in the
installed prefab. The reminder and response layout belong to the outer Dialogue
Panel. Native `AbstractUIResponseMenuControls.ShowResponses` (RVA `0x620AB0`)
calls the reminder's `ShowSubtitle(subtitle)` at `0x620AF6`, before populating
response buttons. `TextMeshProSubtitleControls.SetActive` (`0x1AEF780`) activates
the owned line at `0x1AEF874`; a separate reminder panel is unnecessary.

The reader now prefers the visible reminder while the native response menu is
visible, using its actual label and the outer panel's visibility. Null response
panels also use that same owned outer panel for focus ownership. Hidden, culled,
transparent, collected, closed and ended sources remain silent. Moving one
subtitle between ordinary and reminder controls preserves its delivered identity,
so the same statement is not repeated. Existing pending-focus handling queues the
initial response after the body; the body reader never enumerates response text.
Both installed labels have a `TextMeshProTypewriterEffect`. The reminder also
pairs its visible prefix with the current native localized full line. A later
unrelated response state cannot replace or reset that retained statement.
The bounded diagnostic now identifies reminder selection and the label's failed
visibility gate, without logging hidden text. No hook was added.

The source-linked dialogue suite now passes **63 checks**, including this exact
null-panel layout, reminder-only body, visibility and lifecycle gates, opening
without a subtitle hook, pending cancellation, and ordinary/reminder transitions
with typewriter prefixes and subsequent unrelated model states.
Main build passes without warnings/errors. Reading during an actual examination
remains to be verified by the user; no game was launched.

## Earlier ambient subtitle coverage

The reported campaign session had no `[Dialogue]` entries. The previous reader
covered only `TextMeshProDialogueUI`, leaving the game's floating NPC barks and
`CinematicSubtitlePanel` outside its scope. This change reads their actual
displayed localized labels without enabling subtitles or consuming raw voice
signals.

`VisibleSubtitleAnnouncer` polls the known cinematic singleton and keeps a small
registry populated by postfixes on the two `FloatingBark.Bark` overloads. The
`FloatingBark.Hide` postfix removes that occurrence. No scene scans are used.
Two matching observations settle text; expired, collected, disabled or invisible
labels are discarded. Label opacity, culling, hierarchy visibility and cinematic
canvas opacity must permit rendering. A cinematic also requires `IsActive`.
The floating source requires positive finite native `secondsLeft`, so old text
cannot wait indefinitely after its display expires.

The reader yields to interactive dialogue, modal screen content and the caller's
current speech priority. Cinematic labels precede floating barks; at most one
line is queued per update. These sources do not own focus or gameplay input.
Only pending undelivered visible content participates in ordinary speech
priority, and focus loss/loading/reset clears tracked barks. Existing dialogue
response reading remains separate and unchanged. Delivered lines log
`[Dialogue]`; output uses the central `ScreenReader`, without interruption.

## Native evidence

Installed `GameAssembly.dll` and generated interop expose:

| Method | RVA | Observed behavior |
|---|---|---|
| `FloatingBark.Bark(string,string)` | `0x173A070` | Creates its UI if absent, checks `IsPlayerVisible`, writes `FloatingUI.textComponent`, assigns display time. |
| `FloatingBark.Bark(Subtitle)` | `0x173A1E0` | Same visibility gate; calls `LocalizationDialogue.GetActorName` at `0x173A301` and `GetContent` at `0x173A37D`, writes the rendered label at `0x173A3D0`. |
| `FloatingBark.Hide` | `0x173A740` | Hides the owned floating UI. |
| `FloatingBark.OnUpdate` | `0x173A850` | Advances the native lifetime and updates floating UI visibility. |
| `CinematicSubtitlePanel.OnVoiceLine` | `0x2873960` | Requires `IsActive`, resolves localized content into `_subtitleText`, updates target opacity. |
| `CinematicSubtitlePanel.Show` | `0x2873AB0` | Activates/subscribes the panel. |

The mod never calls these setters, `Show`, or localization/voice callbacks.
Hooks inspect only `__instance` after native execution and ignore text arguments.
The three new native targets have unique bodies in the installed address map;
the full hook safety audit passes 31 regression checks and 48 declarations across
47 native targets, including unrelated existing hooks.

Native reproduction uses `MethodAddressToToken.db` with installed interop metadata
to resolve the RVAs, then Capstone disassembly of the installed DLL. Existing
scratch helpers are in `%TEMP%/LeAccess-landmark-runtime-20261002/` (`disasm.py`,
`map.json`); inspect the methods above, not synthetic decompiled method bodies.

## Validation and limits

`dotnet run --project tests/LeAccess.VisibleSubtitles.Tests` passes 38 source-linked
checks covering displayed Spanish text, hidden/disabled/cull gates, cinematic
activation and alpha, changed deferred content, expiry, same-occurrence dedup,
rearming, multiple barks, modal/dialogue priority and lifecycle cleanup. Existing
dialogue tests pass 23 checks. The main no-deploy build has zero warnings/errors.

No game was launched and no listening result is claimed. This reads only the
listed rendered sources; voice lines without visible subtitles remain silent.
Real campaign playback must confirm NPC/cinematic delivery with subtitles both
enabled and disabled, including scene transitions and simultaneous narration.

## Interactive NPC conversation follow-up

The later reported session (log last written 09:46:33) reaches native
`conversation active` states at lines 515, 528 and 534 but contains no delivered
`[Dialogue]` lines. This establishes that interaction reaches a conversation,
not which previous rendering gate rejected its text.

Actual `PermaLoad.bundle` contains `DialogueSystemController` path ID
`7888734422435858511` and `TextMeshProDialogueUI` path ID
`8560887158985721276`. Its controller enables NPC subtitles both during lines
and with responses. The owned NPC subtitle has **null panel** and TMP line path
ID `2473897409730845878`; the conversation's outer panel remains its visibility
boundary. This is the existing PixelCrushers UI, not a newly invented LE.Dialog
panel. `LE.Dialog.LEConversationTrigger` is a conversation trigger.

`DialogueAnnouncer.Update` now discovers the authoritative UI through the raw
lowercase `PixelCrushers.DialogueSystem.DialogueManager.instance` field and
`currentDialogueUI`, guarded by its current `conversationController.IsActive`.
It does not call the uppercase singleton getter, `FindOrCreateInstance`, or
setters. It reads current subtitle controls, or the same UI's rendered NPC
subtitle when responses retain it. Native UI replacement/end discards the old
source; the outer conversation panel must remain visible. A `HideSubtitle`
callback does not clear a statement that the active native conversation still
renders. The selected responses retain their existing focus reader, without
enumerating all choices.

Native `StartConversationInteraction.Interaction` RVA `0x18842B0` closes
appropriate UI, obtains the player and sends the conversation interaction at
`0x18844A3`. `TextMeshProDialogueUI.ShowSubtitle` RVA `0x1AE65F0` assigns the
chosen owned subtitle controls at `0x1AE673A` before delegating its display.
Interop exposes the raw manager field at `DialogueManager.cs:160`, the raw
current UI at `DialogueSystemController.cs:1203`, and owned NPC subtitle at
`TextMeshProDialogueControls.cs:127`. Reading these sources requires no new
hook. Bounded changed-only `[DialogueStatus]` logs distinguish attachment,
availability, subtitle visibility, responses and modal priority on the next
session, at most once per five seconds.

Interactive tests now pass 41 checks, including discovery without show hooks,
the installed null-panel structure, retained statements after hide callbacks,
response focus ownership, current text changes, hidden outer panels, native
end/replacement and discarded deferred content. Actual listening remains
unverified; the log does not prove which hook or visibility condition failed in
the previous build. The fix removes hook-only discovery and preserves the
installed retained-text behavior instead of relaxing visibility.

The generic focus reader also defers an interrupting response announcement when
the dialogue body has just spoken in the same frame. Its existing pending-focus
flush queues that initial response without cutting the body. Later deliberate
response navigation still interrupts. Three source-linked menu regressions cover
this ordering, bringing that suite to 44 checks.

Discovery also reads `ConversationController.state.subtitle.formattedText` only
when paired with the active UI's current subtitle controls and a visible matching
prefix. Native `TextMeshProSubtitleControls.ShowSubtitle` (`0x1AEFB70`) calls
`LocalizationDialogue.GetContent` at `0x1AEFC53` and writes the localized content
back into that subtitle's formatted text at `0x1AEFC76`, before display. Thus the
current model carries the game's rendered localization rather than requiring the
mod to translate raw database text. Different-language/nonmatching text is never
substituted for the label. A new native subtitle identity clears the old full
line, preventing consecutive same-speaker prefix lines from reusing prior text.
Retained NPC fallback text is not paired with a later player-response state.
Tests include two-frame stable typewriter prefixes, consecutive shared prefixes,
localized rendered text differing from raw English and retained response text.
