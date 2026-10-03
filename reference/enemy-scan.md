# Enemy scan

On-demand report of nearby hostiles during gameplay: F7 or right stick up from neutral.

## Output

- Count of living hostile enemies within `ControllerTargetingParadigm._SkillTargetMaxDistance`
  (the range the nearest-enemy announcement uses; 3D distance), then the 3 nearest
  (`EnemyScanReport.MaxListed`): `Name, [jefe,] direction, unas N unidades`.
- Direction uses `GuidanceGeometry.Direction` against `Camera.main` forward/right, like orientation (F6).
  Units are the rounded horizontal distance.
- Bosses and minibosses are tagged: `Actor.isBossOrMiniboss()` offline,
  `ActorVisuals.Data.isBossOrMiniBoss()` online.
- Nothing in range: "Sin enemigos cerca". Unreadable names fall back to "Enemigo desconocido".
- Speech interrupts; the text is logged as `[Enemy scan] <text>`.

## Structure

- `Combat/HostileRegistry.cs`: shared walk of `ActorManager.visuals` (online) / `.actors` (offline) over foe
  groups with availability checks. Used by `NearestEnemyProbe`, `BossLifeProbe` and `EnemyScanProbe`.
  The nearest-enemy `examined` diagnostic now counts available entries only.
- `Combat/EnemyScanProbe.cs`: collects `EnemyContact` records (game-facing).
- `Combat/EnemyScanReport.cs`: pure sorting, limiting and wording (host-tested).
- `Combat/EnemyScanAccessibility.cs`: input, gating and speech. Gates match orientation: focused, no loading
  screen, no text entry, gameplay owns input, navigation audio not suspended, no other world content.
- `MenuDetailInput.ReadRequested()` (right stick up) is read once per frame in `MenuAccessibility` and passed in;
  `MenuDetailInput.KeyPressed` is the shared modifier-free key check (also used by orientation and vitals).

## Config

`[Enemy scan]`: `Enabled = true`, `ReportKey = F7`, `RightStickUp = true`.

## Tests

`tests/LeAccess.EnemyScan.Tests` (ordering, limit and true total, boss tag, sectors, rounding, none text,
Spanish, non-finite positions). `LeAccess.Combat.Tests` covers the registry walk through the probes.

## Right-stick note

The game also reads the right stick for manual targeting (`ControllerTargeting.RightStickHitTarget`, `EpochInputManager.RightStickInput`), so the up flick that requests a scan may also nudge the game's target toward that direction, exactly like the existing down flick for orientation. Both need a deliberate push from neutral. Set `[Enemy scan] RightStickUp = false` (and `[Orientation] RightStickDown = false`) if this interferes; F7/F6 remain.
