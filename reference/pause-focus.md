# Character creation focus after Continue

## Observed failure

On Steam 1.5.0.1 / build 25663618, the user successfully entered five characters
with the screen keyboard, then opened Start and chose Continue. The read-only
probe recorded the name editor yielding to `MainMenuPanel`, then
`NavigationManager.currentlySelected` becoming null after Continue while controller
mode stayed active. Reopening Start produced normal focus and speech again.
This was lost navigation, not a disconnected screen reader. The user closed the
game normally afterwards; no gameplay character had loaded yet.

The diagnostic log is retained in task scratch as
`LeAccess-steam-hotfix-20261001/pause-resume-focus-failure.log`.

## Native cause

Analysis used the current `GameAssembly.dll` and matching interop metadata:

- `MainMenuPanel.OnResumeClicked` (RVA `0x206DD60`) only closes its panel.
- `PanelSystem.ClosePanelInternal` (`0x205AC90`) clears focus after
  `MainMenuPanel.OnClose`. A restoration in `OnClose` would be overwritten.
- Automatic restoration is restricted to modal panels. MainMenuPanel derives
  from Panel, so adding a dictionary entry alone would not activate that path.
- Shared `OnPanelOpen<T>` (`0x8D6E50`) closes exclusive panels and activates the
  new object before calling `OnOpen`; it selects the new panel afterwards.
- `PanelSystem.RestoreFocus` (`0x205D900`) registers and selects an existing owner
  through native APIs, using `NavigationManager.invalidIndex` rather than forcing
  the first option. `ControllerMapsManager.UpdateMap` then selects the appropriate
  navigation map. Without a priority owner, the default map does not navigate UI.

The correction is scoped to returning to the same character-selection screen.
It must never restore the closed screen keyboard itself, overwrite a new native
selection, submit text, or select a hidden or destroyed screen.

## Implemented correction and validation

`PauseMenuFocusRecovery` observes only the current character-selection context.
The typed MainMenuPanel opening hook binds the exact native owner/control to that
pause instance. For a screen keyboard, the owner is resolved from its previous
navigable ID and checked against the original editor; the keyboard is not saved
as the return target. A last observed owner covers native focus clearing before
the opening callback.

After a successful, non-preloading close of that same panel, one attempt runs in
the following frame, after panel cleanup and any new opening request. It requires
the same character screen/state/scene, an empty native selection, controller mode,
valid visible objects, no remaining panels and no pending panel opening. It uses
native RestoreFocus and UpdateMap, preserving the prior selectable when valid.
There is no repeated forced selection and no text submission or modification.

The isolated source-linked suite passes 134 checks, including keyboard ownership,
native pointer identity, nested/failed closes, scene/device/lifecycle cancellation,
and preservation of a selection the game already restored. Menu reading (29),
menu/zone (34) and native patch auditing (31; 45 declarations / 44 targets) also pass.
The audit now treats Harmony's managed `__state` separately from native arguments,
while retaining Prefix/Postfix type agreement checks.

Main build and Steam deployment succeeded without warnings/errors. The installed
DLL hash is recorded in STATUS.md. A fresh Steam launch loaded the plugin and all
Harmony patches and detected NVDA without mod errors. Confirmation of the actual
Start / Continue sequence was subsequently confirmed working by the user, who
then entered gameplay. The temporary read-only probe has now been removed.
