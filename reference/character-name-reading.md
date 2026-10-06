# Character name editing and validation

**User confirmation (2026-10-02):** the latest fix works in the reported name-entry
workflow, including retaining the name when returning with Circle. The user will
describe additional menu issues separately. Broader device/online combinations
have not been individually confirmed.

## Explicit screen-keyboard activation (2026-10-02)

Since 2026-10-06 the same guard covers the Social panel's username fields
([social](social-reading.md)); listening pending there.

`CharacterNameKeyboard` now requires an explicit click or submit on the declared
character-name field before the game's screen keyboard takes focus. Simply
entering the name/mastery screen or navigating over the field leaves the screen
keyboard closed. The guard covers `CharacterSelect.newCharacterNameInputField`
and `LocalCharacterSlots.inputTextTMP`; other editors keep their native behavior.
Mouse editing bypasses the screen-keyboard adapter entirely. Controller Back
retains the entered name through native keyboard Submit; it does not advance the
creation form, synthesize input or lock device mode.

Native evidence from installed build 25663618:

- `ScreenKeyboardManager.Update` (`0x10826D0`) obtains the selected EventSystem
  object, sets `SelectedGameObject`, then evaluates `ShouldBeEnabled`.
- The selected-object setter (`0x10830C0`) detects an input field. Its subsequent
  `SelectedInputField` setter (`0x1083390`) **registers keyboard navigation before
  the enabled predicate runs**. Preventing only the predicate would leave that
  focus registration active. The new setter prefix stops this path for an
  unrequested character-name field.
- `ShouldBeEnabled` (`0x1082BE0`) requires the native controller mode and an active
  editor; it does not require a click. Its new postfix preserves those native
  conditions while checking character-name editing ownership.
- Primary `Navigable.HandleSubmitInput` (`0x1055FB0`, dispatch at `0x1056220`)
  iterates `_clickHandlerCache`, declared as `List<IPointerClickHandler>` in
  `Navigable.cs:603`. It constructs a `PointerEventData` and dispatches a click.
  The installed `TMP_InputField.OnPointerClick` (`0x3E72D60`) accepts left clicks
  and calls `ActivateInputField` (`0x3E68320`). That method sets the activation
  flag without calling `Disable` or the screen-keyboard predicate. The click
  prefix therefore authorizes editing before the next keyboard update.
- The separate `TMP_InputField.OnSubmit` (`0x3E73A30`) also authorizes explicit
  Unity submit events. Cancel/commit's native `Disable` (`0x1081260`) clears the
  authorization. Moving away, hiding the field, unloading or native device-mode
  deactivation also rearms the click requirement. Focus within the keyboard's
  own buttons/substitute editor retains the same editing session.

All **eight hooks** pass the installed native-address/signature/alias audit.
The source-linked name suite now passes **160 checks**: 38 validation checks and
122 keyboard checks, including the close and typing sequences below. Main build is
clean; no game was started.

## Circle cancellation and returning focus (follow-up, 2026-10-02)

The user first reported a broken return from the screen keyboard, then confirmed
improvement but incomplete names and text disappearing on Back. The 12:24 log
confirms one completed focus return and controller key submissions. It does not
record entered text, so individual lost-character triggers remain unproven live.
Native inspection established two lifecycle holes that the initial tests missed:

- `Update` sets `_enabled=false` after a rejected predicate without clearing the
  selected editor or unregistering keyboard navigation. `Disable` immediately
  returns when that flag is already false. The guard now runs the full native
  close before disabling an active session. A stale disabled session can finish
  cleanup only when keyboard navigation or its visible animation proves ownership.
  An inactive adapter used for physical typing retains the native early return.
- `Navigable.TrySelect` (`0x1057470`) calls `set_ActiveSelectable` (`0x1057600`),
  which returns immediately for the same control (`0x10576E2`). The saved field
  can therefore remain without EventSystem focus. After native restoration, the
  close postfix selects that exact field and repairs its EventSystem focus even
  when the navigable already remembers it. It requires the actual saved owner,
  a live visible input within it, and no foreign navigation/EventSystem focus.

Controller cancel follows `ControllerScreenKeyboardMap.<ProcessInput>d__9`
(`0x1040EE4`) into `OnFunctionPress(Cancel=4)`. The inline branch (`0x1081B77`)
restores `_originalText` and calls `Disable` at `0x1081BCB`; it does **not** call
the standalone `OnCancel` method. The existing Disable hook covers both routes.
Authorization is revoked before native close callbacks and again after them.
Following the user's report of erased names, the new `OnFunctionPress` prefix
converts Cancel to Submit only for the declared editable name field. The separate
`OnCancel` callback routes the same field through native Submit. This prevents
the original-text restoration before Disable; patching Disable alone is too late.
Native Submit commits the editor and closes the keyboard without invoking
`CharacterNameSubmit` or character creation. Other editors still revert on Cancel.
The installed name owner uses navigable ID 110, the keyboard 63; code uses the
runtime saved ID and matching object ownership rather than hardcoded IDs.

Regression doubles now reproduce native early return, close ordering and
same-control selection no-op. Cases cover normal/stale cancel, commit, callbacks,
reopening, lost focus/loading, foreign owners, unrelated editors and physical mode.
The adjacent pause-focus and menu-reading suites also pass (134 and 44 checks).
Native safety now passes 31 checks, 59 declarations / 58 distinct targets.

Regression workflow: open explicitly, type, press Circle, navigate the form,
then open again. Circle must keep the entered name and require a fresh click to reopen.
`[CharacterKeyboard] closed; returned to the name field` logs successful focus
restoration without recording entered names. Mastery ordering was not changed.

## Physical typing and Enter

The mouse-click postfix explicitly focuses and activates the actual TMP input.
The keyboard-manager setter now rejects the name field in physical mode, before
it can register keyboard navigation and cause its disabled predicate to close the
physical editor. A mouse click never authorizes automatic later controller opening.
The installed
field is TMP SingleLine (`m_LineType=0`), with empty `m_OnSubmit` and `m_OnEndEdit`
listeners. Enter finishes editing and keeps the name; it does not advance the form
or create a character. TMP `KeyPressed` returns Finish (`0x3E6D638`), and
`OnUpdateSelected` sends submit and deactivates the editor (`0x3E73C9E`).
`NameCharacterSubmitButton` separately calls `CharacterNameSubmit` (`0x286C1F0`)
to advance to character details. The final Create button performs creation.

Controller-key reading now preserves authorization across temporary null
EventSystem selection while the exact keyboard navigation owns the editor. Its
declared navigation root also owns controls outside the visual keyboard child.
This matters because native `OnKeyPress` checks `ShouldBeEnabled` for every letter;
the earlier guard could revoke the edit at that point. Foreign controls still
revoke permission. Native `SubmitEvent` (`0x1082300`) processes name characters in
the original TMP, then copies original text to the substitute. The substitute is
not an independent text buffer to save back. Name inputs are not deferred editors.

The device-mode setter now captures only an explicitly activated, owned name
editor before changing from controller to physical mode. Native
`set_IsControllerActive(false)` clears navigation at `0x10C74DB`, then invokes
`OnDeviceChange` at `0x10C7383`. The keyboard subscriber calls Disable and clears
EventSystem. Restoring at `set_ActiveController` would be too early. The new outer
setter postfix restores physical editing after those operations, provided the
keyboard is disabled, its adapter is null, and no foreign menu/focus took ownership.

TMP `ActivateInputField` (`0x3E68320`) only sets a pending activation flag.
`ActivateInputFieldInternal` (`0x3E67E30`) enables editing synchronously. The
postfix temporarily suppresses select-all, activates, clears pending activation,
restores the native caret/string positions and updates the label, then restores
the select-all preference. Native `ForceCursor` checks for the released adapter
after its yield (`0x108406C`) and exits, so it cannot later rewind physical typing.
No key is replayed. The tests cover input processed after the setter; the complete
Unity event ordering is not proven by those tests. The user's subsequent live
confirmation establishes that the latest fix works in their reported workflow.
The existing login/email device-transition issue is outside this name-only scope.

`Heathen.ScreenKeyboard.dll` is an existing game interop reference with
`Private=false`; no new runtime DLL is distributed. Diagnostics report focus
transfers without logging entered names.

## Validation-error speech

Implemented for Steam 1.5.0.1 on 2026-10-02, without starting the game. The
validation reader itself adds no native hooks. `CharacterNameAccessibility` polls the currently owned form after
ordinary focus speech, so an error does not immediately get cut off by its field.

The authoritative sources in `last epoch code/` are:

- `CharacterSelect.cs:1960`: `newCharacterNameInputField`; `:1975`:
  `newCharacterError`.
- `LocalCharacterSlots.cs:925`: `inputTextTMP`; `:940`: `errorTextTMP`.
- `CharacterNameInputValidator.cs:50`: `errorUI`; `:65`: its matching `input`.
- `ScreenKeyboardManager.cs:487`: `_selectedGameObject`; `:517`: `_enabled`;
  `:680`: the existing `Instance` singleton.

The owner, input and error must be live and visible. Navigation must belong to
the form or contain its input; a pause-menu component is excluded. If the screen
keyboard is active, it must be editing that same field. Popup/dialogue/radial
ownership, loading or lost application focus suppress errors in background forms.
Validators are discovered only under the declared character owner when that
owner/input changes, then cached; only validators whose input pointer matches
the active field contribute. Explicit typed label references remain valid across
separate canvases, without guessing ownership from nearby text.

The two-observation tracker speaks a changed visible error once, retains only
the latest pending text, and combines identical direct/validator errors once.
Clearing an error rearms a later occurrence. Closing the form releases the cache.
Messages use the game's actual localized text. No guessed validity rule, typed
character echo, submission, character creation, or input-mode change is introduced.

The source-linked `LeAccess.CharacterName.Tests` suite passes **38 checks** for
direct/validator/local-slot sources, ownership, hidden/stale components, duplicate
errors, corrections, rapid updates, modal/keyboard/pause gates and reopening.
Main build succeeds against the installed interop. Controller editing and actual
error-widget visibility still require tomorrow's listening test; this reader
does not claim to fix all previously reported name-entry interaction failures.
