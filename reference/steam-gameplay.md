# First gameplay feedback on Steam 1.5.0.1

The user confirmed Start / Continue recovery and reached InGame in Camino ancestral
on build 25663618. They reported silent controller menus, incomplete class/mastery
descriptions, missing nearest-enemy names and missing ground-object sounds. They
will test later and explicitly asked that the agent not open the game again.

The original gameplay log is preserved in task scratch as
`LeAccess-steam-hotfix-20261001/first-gameplay-failures.log`. It records DefaultMap,
active radar audio, a short ControllerMenuMap transition, localized ground loot,
and movement/health-potion tutorials. Combat and world-object features were enabled
in LeAccess.cfg, but there were no world-cue emission entries. Unity MaterialList
errors at the end occurred during shutdown; they are not evidence of a mod crash.

## Shared tutorial ownership

TutorialManager's anchored callouts were tracked as modal content. Every consumer
of ScreenContentAnnouncer.HasActiveContent therefore suppressed menu/world/combat
feedback while such a callout remained visible, although gameplay continued.
TrackTutorial now marks these sources as nonblocking. Their full localized text
is still announced once; real confirmation panels retain input ownership and
priority over a newer tutorial. The screen-content suite covers this distinction.

## Controller menu

The reader previously required a selected radial sector before saying anything.
It now announces opening even when the stick is neutral. If a sector is already
selected, opening and its localized option are combined; subsequent movement
interrupts with the new option. Holding the same selection remains silent.

Native inspection confirms FocusedRadialMenu selects the primary/secondary widget
from the game's own flag (LE token 100687669, RVA 0x1074C70). Input and menu-opening
behavior are unchanged; there is no forced selection or synthesized button press.

## Classes and masteries

ClassSelectionReader follows ListNavigable's actual selected index in the class
carousel, the selected MasteryButton, or the new passive panel's matching
MasteryTabButton/data index. MasteryReader reads the selected definition's localized
description and bonus keys with their corresponding fallback text. Class feedback
settles in LateUpdate, interrupts previous speech on navigation, and excludes
prompts, level labels, mastery state labels and unrelated neighboring paragraphs.

The tooltip's masteryName is preserved as displayed. Native
MasteryButtonTooltipManager.OpenTooltip (RVA 0x17DCDC0) sends it directly to the
title TMP; translating it a second time could change the visible title.
Common.GetClass/GetMastery (RVAs 0x193BE00/0x193BE70) remain appropriate for raw
definition names: they pass the original input as the localization fallback.
The class suite has 47 checks, including preserving an already localized title.

Carousel descriptions require a MasteryBanner for the selected class ID. The
presence and hierarchy of the old ClassTabPaladin/VoidKnight/Base objects have not
been verified in the original installation: their generic visible labels remain
readable, but typed description coverage needs the user's later test. No internal
object name or longest ancestor paragraph is used to fabricate a description.

## Nearest enemy

The reader used the online ActorVisuals registry in offline play. It now follows
GameplayEnvironment.IsOnlineClient: offline uses PlayerFinder.playerActorComponent
and ActorManager's actor registry; online retains the visuals registry. Native
GetTargetableActors confirms this distinction (RVA 0x277B690). Both paths select
the nearest living hostile inside the game's skill-targeting distance, then read
its localized display name. They do not require damage or select the damage source.

Foreground input ownership, pending UI speech and announcement spacing still gate
speech. A persistent backend IsSpeaking flag no longer suppresses it indefinitely.
Actual emissions log [NearestEnemy]; bounded status logs identify the registry,
range, nearby candidates and any speech blocker. The combat suite has 121 checks.

## Coins, equipment and interactables

Loot now uses targetingConfiguration.itemsRange, matching native targeting, rather
than each listener's shorter interaction radius. Coins need positive value, not a
visible UI label. Equipment respects the game's HIDE loot-filter result without
requiring its label to be rendered. Interactables no longer require requiresClick;
native OnEnable registers automatic interactables too, and CanInteract checks
availability without imposing another distance limit.

Coin, equipment and interactable categories retain distinct directional sounds.
Each category tracks its own repeat eligibility; an always-nearest coin cannot
starve another category. A world snapshot with no remaining eligible objects no
longer cancels the queued sound after automatic pickup. The world-cue suite has
89 checks, including the real probe/state/lifecycle paths.

Doors and transitions are covered when they expose WorldObjectClickListener.
Pure ColliderEnter/WorldAreaEnter triggers have no verified coverage. No speculative
scene scans or hooks were added. [World cue status] reports blockers and candidate
counts at a limited rate; [World cue] records actual playback requests.

## Validation boundary

All changes in this batch require the user's later gameplay/listening test. Host
tests and native API checks establish behavior against the modeled cases, not
audible success in a new play session. The temporary focus-probe plugin has been
removed from the game; normal mod diagnostics will remain available in its log.
All 21 suites passed (1,664 checks); the final main build/deployment completed with
zero warnings/errors. Installed and built DLL hashes match (recorded in STATUS.md).
Process checks before and after deployment confirmed the game was closed.

Native-analysis scratch remains at `%TEMP%/LeAccess-class-native-20261002`.
Automatic review rejected its cleanup with "blocked by policy"; no deletion was
retried. This scratch is outside the repository and installed game.
