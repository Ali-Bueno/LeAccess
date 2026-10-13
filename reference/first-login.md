# First login: terms, display name and email (2026-10-13)

Tester report (new account, pad): "I got past the terms and conditions. Then I needed to put in a screen name but it
wasn't really letting me do that. I restarted the game and then it let me put in my screen name using the controller
input but not the keyboard. Then I hit Submit but after that I couldn't do anything again." Other players could not
accept the terms or enter name and email. No log was available; the evidence below is static.

## Native behaviour (build 25816319, Capstone on `GameAssembly.dll`, read-only)

V = verified in the disassembly or scene data, I = inferred.

### Terms (`LE.Tos.TosPanel`)

- V `ApplyMode` (0x1B3E720): in acceptance mode Close is hidden and Accept is shown with `interactable = false`;
  `get_CanClose` (0x1B3FB80) refuses to close while acceptance is pending, so Back does nothing.
- V `Update` (0x1B3FB10) runs `EvaluateScrollGate` (0x1B3EA10) every frame: Accept unlocks when the text is within
  `_endToleranceLines` of the bottom or fits on screen (`_hasScrolledToEnd`).
- V `OnScrollToEndClicked` (0x1B3F4E0) jumps to the bottom; the next frame unlocks Accept.
- V/I `TosNavigable.Update` (0x106DCE0) scrolls with a pad stick in controller mode only (probably the right stick).
- V Accept stores `TosAcceptedVersion = "1.5.0"` and the panel closes; there is a single document. Clearing that setting
  shows the terms again (exact storage key I).

### Display name (`LE.UI.Login.UnityUI.DisplayNamePromptPanel`, object `AddDisplayName`)

- V scene data: title "Create Account", help "Set a display name for your account…", a 30-character field, Submit,
  and an `errorHeader` (`LoginHeader`, label under `Error`).
- V/I `OnOnEnable` (0x1FA0AF0) selects the field; V `ScreenKeyboardManager.Update` (0x10844D0) opens the screen
  keyboard for any selected text field in controller mode.
- V `OnUpdateTick` (0x1FA0B20): Enter submits when the text is not empty. Submit (0x1FB31D0) disables the button during
  the request; failure shows the error header, success opens the email prompt.

### Email (`EmailPromptPanel`, object `AddEmail`)

- V title "add email address", help text, a 50-character field, Submit and "no thanks"; Submit is disabled below three
  characters but still reachable (explicit navigation).
- V `OnUpdateTick` (0x1FA0F70) fires Submit on Enter only when the field is empty, so Enter never submits (game bug).
- V "no thanks" and a successful submit go to the landing screen.

## Causes in the mod

- Terms: the whole text was read once, Accept was read without its disabled state, and nothing said that the text had
  to reach its end or when Accept unlocked.
- Fields: `CharacterNameKeyboard.IsNameInput` knew only the character-name and Social fields, so none of the typing
  support applied: the keyboard opened by itself on selection, a physical key ended controller mode, the keyboard
  closed and focus was cleared ([chain](steam-port.md#text-entry-investigation)).
- The login panels are `LoginPanelBase`, not game `Panel`s, so `NativeWindowDiscovery` never read their title, help
  or errors: after Submit the email prompt appeared unexplained, with its keyboard open.

## Changes

- `TermsGate` (from `ScreenContentAnnouncer.TrackTerms`): while acceptance is pending and the text has not reached its
  end, the opening says "<Accept> becomes available once the text has been scrolled to the end" and, when the jump
  button is visible, "<its caption> goes straight there", between title and body (game captions). When the gate opens:
  "<Accept> is now available", once, without interrupting.
- `LoginPrompts`: finds either form from any element inside it. `CharacterNameKeyboard` treats both fields like the
  character name (keyboard only on Cross, Back keeps the text, physical typing through the open keyboard, focus back to
  the field), and `CharacterNameAccessibility` reads their error header and drives the typing echo.
- `NativeWindowDiscovery` treats both forms as windows: title and help on opening, later errors through the update
  reader; while the screen keyboard owns focus the field reader speaks errors instead. Log `[Login] form: <object>`.
- `ElementAnnouncer`: the fields add "Cross to type" in controller mode; Accept and Submit say "(Disabled)" while the
  game keeps them locked.

No new hook. Not yet heard in game: the account forms only appear for a new account.

## Open questions for a tester log

- Which control the terms open on, the button captions, and whether locked Accept takes focus.
- Whether each login panel still opens with its field selected (`[Login] form:` then the field reading).
- What a rejected display name says (error header text) and what Back does on each form.
