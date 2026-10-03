# Dungeon and warning window reading

Added 2026-10-03. Source-linked tests pass; controller and listening validation
remain pending. No game or audio device was started.

The previous screen-content hooks covered legacy confirmations, tutorial
tooltips and terms, but did not discover the separate dungeon panels or newer
PanelSystem warning/alert windows. A regression that focused the native
Lightless Arbor entry panel reproduced missing speech before the fix.

`NativeWindowDiscovery` now reads the native navigation owner. For
`NavigableMediator` it follows `FocusedPanel`; direct navigation and its actual
`ActiveSelectable` can identify a parent panel or legacy dungeon UI. Discovery
uses no global object search, new hook, player action or network-state write.

Recognized owners are Lightless Arbor, Soulfire Bastion and Temporal Sanctum
entry panels, `DungeonFloorExitPanel`, `BossConfirmationPanel`,
`WarningConfirmationModal`, `AlertModal`, and any focused panel which casts
successfully to the native `IModal` interface. The optional interface cast
extends coverage to native generic confirmation/picker implementations, which
read only once on open ([generic modal rule](announcement-lifecycle.md#generic-modal-windows)); a
failed cast is not treated as a modal. Explicit dungeon and warning types
remain supported independently of that cast.

The existing reader collects actual visible title, description and choice
labels, follows the established visual reading order and excludes hidden
templates and input values. Changed tier/modifier instructions settle before
speech, unchanged content stays silent, and reopening a retained window reads
its current instructions again. Native owner eligibility is checked during
each ownership query, so changing `FocusedPanel` releases an old window before
the next accessibility update. Disabled, inactive, faded and culled windows
cannot own focus. Existing independently registered popups retain their draw
order/opening priority over an underlying dungeon window.

## Member evidence and validation

The replaced `last epoch code/` interop contains these member contracts:

- `NavigableMediator.FocusedPanel` supplies the authoritative focused panel.
- `LE.UI.PanelSystem.IModal` is the native non-generic modal interface.
- `DungeonEnterPanelUI.tierDescription`, `tierInfoPopupTMP` and `dungeonPanel`
  hold its displayed description, optional explanation and presentation group.
- `DungeonFloorExitPanel._dungeonFloorExitPanelUI` and its `modDescription`
  identify the exit instruction widget.
- `BossConfirmationPanel._bossConfirmationPanelUI` and its `title`/`description`
  identify the boss-vote widget.
- `WarningConfirmationModal._warningText` and `AlertModal._modalTemplate`
  expose the displayed warning/template widgets.

These are member/compilation evidence, not proof that the particular installed
native binary has been inspected at those method addresses. No new method
detours were added. The `IModal` test double models a successful native
interface cast; the actual native cast and presentation hierarchy still need
runtime validation.

`LeAccess.ScreenContent.Tests` passes 187 checks, including 93 new checks for
the discovered owners, current instruction changes, identical reopen,
visibility/fade, immediate focus loss, legacy/selected-control ancestry,
collected selected-control wrappers and overlapping popups. Notifications
(50), quest updates (85), quest journal (124) and controller menu/zones (40)
also pass. The main build with `SkipPluginDeployment=true` has zero compiler
warnings/errors.

This does not establish speech for visual-only environmental hazards or
audio-only instructions without authored text. Dungeon/Monolith dynamic HUD
objectives and Morditas encounter widgets now have a separate
[dynamic instruction reader](dynamic-objectives.md) using their actual visible
owned rows. Panel control navigation and audible online dungeon behavior remain
unverified.
