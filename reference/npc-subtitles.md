# Visible NPC subtitles — Steam 1.5.1

## Distant camp barks and actual screen visibility — build 25672295

The 15:23 session reads camp chatter through `[Dialogue]` (for example log lines
1898 and 1928), rather than the submitted NPC-message channel. The prior floating
reader checked active hierarchy, alpha and `canvasRenderer.cull`, but none of
these proves that an active floating label lies inside the screen. An offscreen
label could therefore be announced repeatedly as new bark occurrences.

`BarkAudibility` now requires the exact `FloatingBarkUI.bark` owner and label
hierarchy in a loaded scene. An active readable floating label is
eligible when either its actual rectangle overlaps the screen, its speaker is
within the declared `WorldObjectClickListener.interactionRange` from that bark's
own hierarchy, or the bark belongs to the player's hierarchy. Range is physical
3D distance in the player's own scene, finite and positive; no guessed radius, nearest unrelated NPC,
conversation permission or interaction invocation is involved. An NPC without a
reliable listener has only the onscreen route. Existing alpha, cull, hierarchy,
lifetime, priority and loading gates remain.

Screen visibility uses the TMP label's `RectTransform.GetWorldCorners`, the
owning root canvas's render mode/camera, `RectTransformUtility.WorldToScreenPoint`
and the actual camera pixel rectangle or overlay `Screen.width/height`. A partly
visible label remains eligible even when the NPC's center is offscreen or far
away. Behind-camera sources, invalid geometry and missing render-camera evidence
cannot establish visibility. A truly visible label does not depend on the player
reference being ready or sharing its scene, so additive/persistent scene sources
remain readable. Nearby and player-owned speech do not need a camera to pass
their separate eligibility routes.

Current native evidence: `FloatingBarkUI.SetPosition` RVA `0x173A650` and
`FloatingTextLineUI.SetPosition` RVA `0x1742140` project through
`CameraManager.getMainCamera`; `FloatingBark.OnUpdate` RVA `0x173AFF0` supplies
`transform.position + offset`. `FloatingTextLine.IsPlayerVisible` RVA `0x1742650`
only checks a native 25-unit player distance, **not viewport visibility**, and is
not used as an onscreen test. Earlier native addresses in the following historical
sections are from build 25663618.

Rejected current barks do not accumulate pending speech. Their occurrence tracker
is retained so leaving and re-entering the viewport cannot repeat an already
spoken occurrence. A spatially rejected owned label consumes matching queued NPC
submissions and suppresses later duplicates while that bark remains registered;
it cannot be read indirectly through the second channel. Unrelated submissions,
player narrative without floating UI, intro/video and cinematic readers retain
their existing behavior. `[BarkSubtitleStatus]` reports bounded changed counts
for current, onscreen, in-range, player-owned and eligible barks plus speech
priority; it never logs suppressed text.

The source-linked visible-subtitle suite passes **143 checks**, including 49 new
spatial/ownership/dedup regressions. Main `SkipPluginDeployment=true` build passes
with zero warnings/errors. No game was launched. The next listening test should
compare distant offscreen camp speech, approaching the same speaker, a partially
visible distant label, and walking away/back during one still-current line.

## Intro/video subtitles and Rogue narrative follow-up (2026-10-02)

The session ending **13:01:22 local** creates a Rogue, enters Z12, then Z22.
It delivers Grael's interactive dialogue at log lines 1485–1501, but no
`DialogueNpcMessage` or movie subtitle entries. This establishes the remaining
gap, without proving a particular visibility failure in the old cinematic-panel
reader. The log is preserved at
`%TEMP%/LeAccess-cinematic-20261002/BepInEx-130122.log`.

Full-screen intro/video playback has a **different owner** from the previously
covered `CinematicSubtitlePanel`: `CharacterSelectionVideoManager.instance`.
Interop fields at `CharacterSelectionVideoManager.cs:647–778` expose `holder`,
`cv`, `subtitleText`, `currentCinematic`, `currentSubtitleIndex` and `IsPlaying`.
Native `Update` (`0x16FDFA0`) tests video playback, compares video time against
`Cinematic.Subtitle.StartTime`, resolves that entry's localized text, writes
`subtitleText` at `0x16FE146`, activates its object and advances the subtitle
index at `0x16FE187`. This source was entirely absent from the reader.

The reader now polls that exact owner and its currently rendered localized label.
The owner must be playing, the cinematic alive, the subtitle index positive,
and its holder/canvas/label visible. It does not enumerate upcoming subtitles.
Movie identity plus the native subtitle index distinguishes repeated captions
and resets pending text on a different clip. Stop, skip, invisible content and
focus loss discard pending captions. The movie's own rendered lifecycle is read
before the gameplay loading reset, so resetting world sources cannot erase a
currently visible movie every frame. No hook or dependency was added.
Delivered movie lines log `[VideoSubtitle]`. Changed status logs for the movie
and the existing cinematic panel are capped at once per five seconds per source
and contain visibility/priority flags, never hidden subtitle text.

The **Rogue's initial spoken line** has separate, concrete evidence: installed
`scene_Z12_unity.bundle` object `PC_VA_RogueZStart` (2800) has a class condition
for Rogue, a world-area listener and `PlayOneShotSoundInteraction` (17298),
`isVoiceLine=1`, using
`event:/Voice Acting/Rogue/Story/va_rogue_story_farfromslums`. It contains no NPC
message, floating subtitle or cinematic-text component. Native
`PlayOneShotSoundInteraction.play(GameObject)` (`0x187E430`) calls FMOD voice
playback at `0x187E498` without publishing text. This particular authored story
utterance is audio-only at the inspected trigger; the new movie reader cannot
transcribe it. The previous limitation therefore includes some story lines,
not only combat/error vocalizations. No transcript is guessed from the event name.

**94 visible-subtitle checks** now pass, including 25 movie regressions for its
real separate-owner structure, locale text, repeated-caption identity, source
replacement, loading coexistence, skip, deferred-text replacement, priority and
visibility gates. Main no-deploy build passes without warnings/errors. No game
was launched; the newly added movie source still needs the user's listening test.

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

The source-linked dialogue suite now passes **68 checks**, including this exact
null-panel layout, reminder-only body, visibility and lifecycle gates, opening
without a subtitle hook, pending cancellation, and ordinary/reminder transitions
with typewriter prefixes and subsequent unrelated model states.
Changed native subtitle identities also rearm identical speaker/body text,
while duplicate callbacks remain silent. See the
[announcement lifecycle audit](announcement-lifecycle.md) for reproduction and
deferred-turn replacement coverage.
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
