# Enemy class (normal, magic, rare, champion, boss)

Added 2026-10-08 after a fight with "Gilgamesh el Corrupto" (scene A60): it ranked as an ordinary
enemy everywhere because the mod only asked `isBossOrMiniBoss()`.

## Evidence

- The log shows `[NearestEnemy] Gilgamesh el Corrupto` and `[Enemy cues] ... selected=Ordinary#...`
  with no `[Boss life]` line, so `isBossOrMiniBoss()` was false: not a boss or miniboss.
- Last Epoch names rare monsters from a prefix/suffix scheme ("<name> the <epithet>"), which "Gilgamesh
  el Corrupto" matches, and the fight was long. Most likely a Rare. Unverified: no dump of the live
  enemy's class exists yet.

## Source

`BaseDisplayInformation` (the component the name is already read from: online
`ActorVisuals.BaseDisplayInformation`, offline `Actor.GetComponent<BaseDisplayInformation>()` or the
linked visuals) exposes:

- `actorClass` (`DisplayActorClass { Normal, Magic, Rare, Boss }`), the class the game colours the name by;
  `ActorDisplayInformation.ReceivedInitialize(displayRarity, ...)` sets it from the server value, so it
  should also hold for online enemies.
- `IsChampion()` (virtual; `ActorDisplayInformation` overrides it).

Without a display, `ActorData.isChampion` and `rareStatus == Always` are the fallback. `ActorData.isBossOrMiniBoss()`
and `Actor.isBossOrMiniboss()` still decide bosses. `EnemyClassProbe` combines them; `EnemyClassifier.Classify`
orders boss > champion > rare > magic > normal.

## Behaviour

| Feature | Rare / champion | Magic | Boss |
|---|---|---|---|
| Nearest-enemy and scanner speech | name plus class word ("Gilgamesh el Corrupto, raro") | name plus "mágico" | unchanged (scanner says "jefe") |
| Enemy cue | elite cue, see [enemy-cues.md](enemy-cues.md) | ordinary growl | boss swoop |
| Life narration | yes, nearest on-screen elite only, see [boss-life.md](boss-life.md) | no | yes |

Words live in the en/es catalogs (`enemy.class.*`, `enemy.named_class`), not read from the game.

## Unverified

- That `actorClass`/`IsChampion()` hold the live values for Gilgamesh and for online enemies; the interop
  shows the members but the game was not run. If rares still sound ordinary, log `actorClass`,
  `IsChampion()`, `rareStatus` and `isChampion` for the nearest enemy.
- The game's own localized rarity words were not located; the catalogs supply them.
- Named uniques (as opposed to rares) were not identified; they would report as Normal or Rare.
- `IOverheadBar.HealthNormalized` on non-boss online enemies (a non-boss may have no overhead bar; then
  the elite is simply not narrated). Offline reads `health.currentHealth`.
