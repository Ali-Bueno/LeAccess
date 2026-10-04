# Exploration mode (proposal, 2026-10-03)

Status: design research only; nothing implemented. Requested by the user after
learning that Last Epoch gives sighted players no route line to quests (only the
minimap icon and its edge arrow, see
[automatic orientation](automatic-orientation.md#live-report-silent-for-a-whole-session-2026-10-03-2118-log)).
The user's rule: do not compute help the game does not give sighted players;
build an exploration mode like the D2R mod's instead.

## What the D2R mod does (`D:\code\c++\diablo 2 access project\d2r-access`)

There is no separate mode toggle: "exploration" is the default fairness policy of
the right-stick scanner plus the beacon (`docs/entity-scan.md`, `docs/beacon.md`,
`docs/decisions.md`). "Full assistance" is opt-in.

- Scanner: right stick up/down picks a category (empty ones skipped, name spoken),
  left/right steps through its entries nearest first: name, straight-line distance,
  one of eight screen directions. No walking steps.
- Categories: exits, portals, waypoints, NPCs, stash, shrines, other objects, quest
  objects, unexplored rooms, enemies (live only), players, corpses. Loot and
  ordinary chests excluded (they have their own loops).
- Discovered only: drawn on the automap, stood in, or seen in range and line of
  sight. Found places persist per map. First discovery speaks "new exit/place" with
  a one-shot sound.
- Beacon: L3 locks the last spoken entry (same entry again cancels, another entry
  retargets). After 600 ms a 620 Hz ping repeats, 160-1200 ms by remaining route,
  panned toward the next route leg, aimed ahead to turn corners early. Routes only
  through explored ground ("helps you return, not discover"); otherwise refused.
  Arrival releases the lock. Silent but kept in menus/dialogue.
- Lessons: R3 was bound by the game, so the lock moved to L3; diagonal radar rays
  were an "overload of cues" and were reverted; an automatic beacon to the nearest
  frontier was rejected as too close to routed navigation; the mod's own
  visited/seen memory proved more reliable than the native automap.

## Last Epoch input facts

- The game uses Rewired through `EpochInputManager` (`rewiredPlayer`); action ids in
  `EpochInputManager.InputActions` (Ability1-5, UsePotion 8, UsePortal 9,
  DodgeRoll 192, Map 14, QuestLog 41...). Bindings live in serialized Rewired user
  data, not on disk; `%USERPROFILE%\AppData\LocalLow\Eleventh Hour Games\Last Epoch\`
  holds no binding file.
- Default D-pad and L3/R3 gameplay bindings are **unverified**. Glyph helpers
  (`DPadAllButtonName`, `DPadUpDownButtonName`) and `SwapAcitonBarSkills` suggest the
  D-pad is used somewhere. Settle it with a read-only runtime dump of the joystick's
  Rewired action element maps, or the user's own knowledge.
- The right stick aims: `ControllerTargeting.RightStickTarget`,
  `RightStickHitTarget(Vector3)`, `GetMostViableTarget(..., rightStickInput)`.
- Mod controller reads today (read-only, game still receives them): right stick up
  flick (`MenuDetailInput.cs:26`, menu details and F7 enemy scan), right stick down
  flick (`MenuDetailInput.cs:27`, F6 orientation), Back/View (`VitalsInput.cs:41`,
  vitals report). Reads use the Rewired gamepad template, so custom remaps are not seen.

## Proposed Last Epoch mapping (awaiting the user)

- Scanner on the D-pad if it is free in gameplay (the right stick already aims);
  keyboard equivalents as in D2R. Lock button to be chosen after the binding dump.
- Categories: exits, waypoints, portals, NPCs/merchants, stash, shrines
  (`ShrinesManager.allShrines`), quest markers, enemies, players. Quest markers may
  be listed with straight-line distance and direction even when unexplored, because
  the minimap shows them to sighted players; they must not require the minimap to be
  shown.
- Discovered: the mod's own visited/seen memory, or currently on the native minimap.
- Beacon: open question for the user: straight bearing only (like the minimap
  arrow), or D2R's route through explored ground only.
- Existing route-computing guidance (automatic orientation, F6) conflicts with the
  rule and would be turned off by default or removed.
