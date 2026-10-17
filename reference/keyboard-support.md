# Keyboard support (investigation, 2026-10-17)

User request: players want to play with the keyboard (menus, scanner, mod settings) without changing the game's own
settings. Static analysis of the interop stubs and earlier notes only; nothing below has been tried in game.

Two groups ask (user, 2026-10-17): players with no pad at all, and players who do not want the controller interface
and want the game's own keyboard shortcuts (inventory, character, ability keys, their own rebinds).

## Why the keyboard cannot drive the mod today

- The game reads input through Rewired. Controller mode needs a gamepad connected and assigned, and no key, mouse
  button or mouse movement that frame ([mode rule](runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04)).
  Any other key switches to keyboard/mouse mode, which clears menu focus. Without a pad, controller mode never starts,
  and the mod's focus reading depends on it.
- Controller menus are driven by per-screen maps (`UiNavigationMap`, `InventoryMap`, ...; active one
  `ControllerManager.CurrentMap`) whose `ProcessInput` coroutines read the pad through Rewired's gamepad template and
  call `NavigationManager.HandleNavigateInput(Direction)`, `HandleSubmitInput(SubmitType)`, `HandleCancelInput()` and
  `HandleContextNavigationInput(Direction)`. Keyboard maps never reach the template reads ([menu](mod-menu.md)).
- The game's WASD option is Gameplay > Control Schema `KeyboardMovement` (`LastEpochSettings.IsKeyboardMovement`);
  it stays in keyboard/mouse mode, abilities aim at the cursor (`EpochInputManager.HandleAbilityInput`) and menus are
  pointer-driven, so neither focus reading nor controller auto-targeting applies.

## Current mod keys

Ctrl+F1 menu (configurable), arrows/Enter/Escape inside it, Ctrl+Numpad 8/2/4/6/5 scanner, Ctrl+B/G/L lock, gold,
status, R vitals report (`[Vitals] ReportKey`). All read with `UnityEngine.Input`, not consumed, so the game hears them
too. No key for menu details (right stick up) or minion commands. Untested: whether these keys drop controller mode
while a pad is in use, since they are not screen-reader keys.

## The game reads pads through XInput only (2026-10-17)

`Player.log` on this machine: "Rewired Windows Gaming Input support is not available ... Attempting to fallback to
XInput ... XInput initialized." The x64 install ships no `Rewired_WindowsGamingInput.dll` (only
`Plugins/ARM64` has one; `Plugins/x86_64` has `Rewired_DirectInput.dll`), so every x64 player should get the same
fallback. Rewired then loads one of `Xinput1_1`..`Xinput1_4`/`Xinput9_1_0.dll` (metadata strings) and calls
`XInputGetState`, `XInputGetCapabilities` and `XInputSetState` (GameAssembly strings). A pad can therefore be offered
inside the game process, with no driver: a native detour of those exports in the loaded XInput DLL (BepInEx's own
detour support, `dobby.dll`/MonoMod in `BepInEx/core`, no new dependency) reports a connected pad in a free slot and
fills its state from the keyboard. Unverified: that Rewired creates and the game assigns a joystick from XInput calls
alone, how Steam Input's own XInput hook interacts, and that `ControllerShouldBeActive` accepts it while keys are held
(the mod's guard would have to count the pad's keys as its own).

## Options

1. **Virtual pad fed from the keyboard** (full play; the only option for players without a pad, user 2026-10-17): first
   try the in-process XInput pad above; failing that, the mod creates a virtual Xbox 360 or DualShock 4 pad through the
   ViGEmBus driver and writes its state from keys; the game then behaves exactly as with a pad (controller mode, every
   map, auto-targeting). The game already ran correctly with a ViGEm Xbox 360 pad in test sessions
   ([while open](mod-menu.md#while-open)). Needs a separate driver install with admin rights, the client library as a
   new dependency, the game's keyboard maps switched off at runtime while it is active (as `InputBlock` does), typing
   and NVDA keys passed through, and a key to hand the keyboard back. ViGEmBus is no longer developed upstream.
2. **Navigation injection without a driver**: keys call the `NavigationManager` handlers directly. Still needs a real
   pad for controller mode and misses map-specific buttons (inventory L2/R2, tabs, hold actions), so it only helps
   pad owners.
3. **Mod-only shortcuts**: keys for menu details and minion commands, and mod keys counted as screen-reader keys so they
   never drop controller mode. Cheap, useful with any of the above.
4. **Game shortcuts over controller mode** (for players who want the game's keys; recommended with 1): the game stays
   in controller mode (real or in-process pad) and `ControllerModeGuard` keeps it for every key while the option is on,
   so the game's keyboard bindings act while menus keep their focus and the mod keeps reading them. The mod claims only
   the keys it needs for menu navigation and movement. Unverified, check first: whether `ButtonInputListener` and
   Rewired keyboard actions act while `IsControllerActive` (the stubs have no bodies: disassembly, or a test build with
   the guard widened, played with a real pad), and whether ability keys then aim like the pad (auto-target) or at the
   cursor. Where the game ignores a key in controller mode, the mod would do that key's action itself.

Rejected: real keyboard/mouse mode. Menus there are pointer-driven with no focus, so every screen's navigation and the
aiming of abilities would have to be rebuilt by the mod.

Open questions for an in-game test: whether a pad created at runtime is assigned without restarting; which pad wins
with a real one also connected (and whether the screen keyboard closes on the switch); Steam Input duplicating it;
whether the game's keyboard hotkeys stay live in controller mode.
