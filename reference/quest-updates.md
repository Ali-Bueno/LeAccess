# Automatic quest updates

Implemented 2026-10-02 against Steam 1.5.1, build 25672295. Listening validation
is pending; the game was not launched during development.

`QuestUpdateAccessibility` polls the local player's initialized campaign,
Monolith and stash quest holders using `PlayerFinder.getLocalStatefulQuestListProvider`.
The interval matches the existing vitals reader's 100 ms poll. It reads only
`playerVisible` quest assets belonging to the exact local list, with reciprocal
quest/step/objective/stateful-owner checks. No quest state is written and no new
native hooks or dependencies are introduced.

Speech announces new quests, changed steps/current objectives, incremental
progress, completion and failure. Native quest names and current objective text
are retained. Earlier completed/failed objective history is excluded. The
`[Quests] AutomaticUpdates` setting defaults on. `MinimumUpdateIntervalSeconds`
defaults to the existing nearest-enemy reader's two-second speech interval;
rapid counters coalesce locally between deliveries. English and Spanish headings
come from the existing mod catalog.

Two matching observations establish an initially silent baseline and settle
later changes. Saved history, pinning, ordering and translation-only changes do
not trigger speech. Pending changes coalesce per quest, so a counter updated
several times during dialogue reports its latest value afterward. Removed rows
are not interpreted as completed quests. Invalid fields suspend only their own
quest; they cannot silence another quest, and an initially unavailable quest
that later becomes readable establishes its own silent baseline.

Menus, dialogue, typing, loading, lost focus and unavailable speech defer output.
Zone announcements and visible subtitles take priority. Delivery uses the single
project speech adapter without interruption, with a configurable minimum interval.
It does not depend on `IsSpeaking`: a persistently true backend flag has previously
silenced gameplay readers (see [Steam gameplay](steam-gameplay.md)). Source-list replacement silently
seeds previously unseen historical quests while retaining progress comparisons
for known quests. This handles native initial-state synchronization and ordinary
area transitions without replaying the journal. Continuity is scoped to the
local `CharacterData` identity; leaving a named gameplay scene, changing that
identity, disabling the feature or cleaning up the plugin discards pending
speech and starts a fresh baseline. A game reload that reconstructs that
identity is deliberately treated as a fresh session.

## Current native evidence

The installed `MethodAddressToToken.db` was joined with current `LE.dll`
metadata, then `GameAssembly.dll` was inspected read-only with Capstone. These
are evidence addresses for build 25672295, not production offsets:

- `StatefulObjective.GetObjectiveActiveText`, RVA `0x14C7B50`, resolves
  `Localization.Quests.GetObjectiveActive` and, for incremental objectives,
  formats the result with the stateful `progress` and authored objective `limit`.
- `QuestTrackerObjectiveUI.UpdateObjective`, RVA `0x14A9360`, calls that exact
  formatter before setting the HUD text. The accessibility reader uses the
  formatter directly, so it does not depend on opening the journal or whether
  the tracker has instantiated a particular row.
- `StatefulQuest.getState`, RVA `0x14CE030`, reads
  `currentStep.questStateDuringThisStep`. A missing step logs and returns Active;
  it is not evidence of completion. Such malformed entries remain unavailable.
- `StatefulQuestListHolder.initialise`, RVA `0x14C7DD0`, attaches the list and
  marks the holder initialized. `receiveInitialQuestState`, RVA `0x14C8010`,
  creates a new list, initializes it, and synchronously deserializes into it.
  Consequently the holder flag alone cannot distinguish a previous empty list
  from a new initial synchronization; the reader also follows list identity.

Temporary analysis lives under `%TEMP%/LeAccess-quest-updates-20261002`.

## Validation and limits

`dotnet run --project tests/LeAccess.QuestUpdates.Tests/LeAccess.QuestUpdates.Tests.csproj`
passes 100 checks against source-linked state, probe and service code: quiet baseline, settling,
coalescing, duplicate prevention, completion/failure, invalid/foreign ownership,
counter formatting, history omission, lifecycle, source replacement, initial
synchronization, menus/loading/focus/typing gates, priority, speech intervals,
persistent backend busy flags, disabling and en/es speech. The production project
builds cleanly with deployment disabled.

Pending updates have an explicit enqueue order rather than relying on dictionary
enumeration: a rapidly updated quest cannot reuse a removed dictionary slot and
overtake an older pending quest. The service also rotates among the campaign,
Monolith and stash lists after delivery, so recurring campaign counters cannot
starve the other ready lists. Coalescing keeps the latest revision and preserves
the original waiting position; unavailable or still-settling entries do not
block another ready entry. Both starvation paths were reproduced before the fix.

This covers the authoritative local quest lists. Separate dungeon/Monolith and
Morditas widgets use the [dynamic instruction reader](dynamic-objectives.md),
which shares this service's automatic-update setting and speech deadline.
Queued updates retain `HasPendingContent`, but only eligible current gameplay
updates expose `HasPendingAnnouncement` priority. Menus, typing, lost focus,
loading and invalid player/character state release that priority immediately,
including frames between polls, so native menu rejection notices can speak.
Absent/collected providers and unready pending sources also release priority
without deleting known progression. Eligible, settled stateful and dynamic
updates alternate shared speech turns when both streams remain continuously
pending.
Very short state transitions between polls can be
coalesced into their latest settled state. Actual online/offline quest listening
and zone-transition timing remain to be tested by the user.
