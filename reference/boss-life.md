# Boss life narration

Ported from the D2R mod, the author's unreleased Diablo 2 mod (`docs/boss-life.md`). Config: `[Combat feedback] AnnounceBossLife = true`.

## Behavior

- Every boss or miniboss in the foe registry is followed separately by `BossLifeState`, one `VitalsAnnouncementState` each (health only).
- First sighting is a silent baseline; a band fall speaks the real floored percent ("Name, 74 percent"); a one-band rise is silent; a larger recovery speaks.
- Bosses crossing in the same poll form one utterance joined with ". ". Output is always queued (`interrupt: false`), so the player's own critical vitals are never cut off.
- Death is spoken once as 0 percent (dead flag or health 0 after a living baseline), then the boss is forgotten. A boss that leaves the registry unseen-dead is forgotten silently and re-baselined if it returns.
- Speech uses `ScreenReader.TrySpeak`; only accepted output is acknowledged, a rejected attempt retries with fresh values.
- Polled every 200 ms while the window is focused. A loading screen, scene change or disabling the option forgets all baselines; a pausing menu keeps them.
- Only bosses inside the main camera viewport speak (a sighted player follows the bar of a boss on screen); off-screen bosses, e.g. a party fight elsewhere in the zone, update their bands silently and end silently.
- Below 10 percent only the first per-point step is spoken, then the death: a fast kill would otherwise queue 5, 4, 3, 2, 1 behind other speech. A recovery to 10 or more re-arms it.
- Log: `[Boss life] <text>`.

## Data sources (`last epoch code/`)

- Registries as `NearestEnemyProbe`: offline `ActorManager.actors` (`Actor`), online client `ActorManager.visuals` (`ActorVisuals`), foes by `alignment.IsFoe`.
- Boss test: offline `Actor.isBossOrMiniboss()` (`Actor.cs`), online `ActorVisuals.Data.isBossOrMiniBoss()` (`ActorData.cs`).
- Offline health: `Actor.health` (`BaseHealth.currentHealth` / `maxHealth`); dead when `currentHealth <= 0`.
- Online health: `IOverheadBar.HealthNormalized` through `ActorVisuals.TryCast<IOverheadBar>()` (the interop class has no public member for the explicit implementation); dead via `ActorVisuals.dead`.
- Names: `NearestEnemyProbe.ReadName` / `ReadOfflineName`.

## Limits and live checks pending

- The 0..1 scale of `HealthNormalized` is inferred from its name; the stubs hold no logic. Verify in an online session that the spoken percent matches the boss bar.
- Not verified in game: that `TryCast<IOverheadBar>()` succeeds on `ActorVisuals`, and that dead bosses stay in the registry for at least one poll (otherwise the 0 percent is skipped).
- Tests: `tests/LeAccess.BossLife.Tests`.

## Elites (2026-10-08)

Rare and champion enemies ([enemy-class.md](enemy-class.md)) are narrated like bosses: same on-screen
rule, bands and first-sighting silence. Online health comes from the same `IOverheadBar.HealthNormalized`
cast; an enemy without that bar is skipped. Anti-spam rule: only the nearest living on-screen elite is
voiced (bosses all speak); the others follow their bands silently and a takeover starts from that silent
baseline. An elite's death is spoken only if it was the last voiced one. Config `AnnounceEliteLife`
(default true). Unverified: the `IOverheadBar` cast on non-boss online enemies.
