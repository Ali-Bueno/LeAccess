# Character name validation speech

Implemented for Steam 1.5.0.1 on 2026-10-02, without starting the game or adding
native hooks. `CharacterNameAccessibility` polls the currently owned form after
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
