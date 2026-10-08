# Minion commands on the controller right stick

Static analysis of `GameAssembly.dll` (build 25764382, Capstone, read-only). RVAs are evidence only.

## How the game does it

- `EpochInputManager.Update` (0x10C6680) reads the gamepad right stick every frame into `RightStickInput`
  (camera-rotated, 0x10C7AA8) and sets `hitVector` to player position + stick direction x 10 (0x10C7BB7).
- `canControllerMinionCommand` is set when stick magnitude is below 0.2 (0x10C7BD1). The command fires when the
  controller is active, the flag is set, magnitude is above 0.2 and no panel blocks skills (0x10C81C0-0x10C8237);
  the flag is then cleared. So one command per flick away from rest; not continuous, not on release, not R3.
  Chat focus skips the step (0x10C8184). A separate path issues the same call on Rewired action 40
  (`MinnionAttackCommand`, 0x10C8272).
- `hitTransform` is the transform of `ControllerTargeting.RightStickTarget` (the enemy in the stick direction, via
  `PrepareTargets` 0x10C58F0 / `RightStickHitTarget` 0x102F8F0), null if there is none.
- `EpochInputManager.HandleMinionCommand` (0x10C4000): online it passes the `ActorVisuals` of the target (or null) and
  the point to `ClientUsingAbilityState.HandleMinionCommand` (0x1628CF0) and shows a move marker; offline it uses
  `MarkPreferredTargetForMinions.ProcessMinionCommand` (0x25368E0).
- The NullReferenceException comes from one of three null reads in 0x1628CF0 (own `actorVisuals`, its `alignment`,
  `PlayerSync.local`); which one is unconfirmed. It sends nothing, aborts the rest of `Update` for that frame and
  leaves the flag armed, so it repeats on the next frame (pairs about 17 ms apart in the log).
- `MethodAddressToToken.db` tokens are the interop assembly's own, not the `GetIl2CppMethodByToken` constants in the stubs.

## What the mod does

While Create is held, `ExplorationInput` sets `canControllerMinionCommand = false` every frame, so the scanner flick
no longer issues a minion command (no marker, no exception). Without Create the stick is untouched. The log line
`[Exploration input] game minion command held back while Create is down` appears once per hold when the flag was
armed. The action-40 button path is not blocked (no controller button is bound to it in the verified layout).

## Not done

No spoken feedback for minion commands. A Harmony hook on `EpochInputManager.HandleMinionCommand` takes a `Transform`
argument (PatchSafety rejects exact `Il2CppObjectBase` signatures) and the original throws often online, so a postfix
would not reliably run. A polling alternative: watch the flag clear and read `RightStickTarget`.

## Check in game

1. Hold Create and flick the right stick near an enemy: no exception in the log, the scanner speaks.
2. Release Create and flick the stick with minions: they attack or move; note whether the exception still appears.
3. Note whether the held-back log line appears.
