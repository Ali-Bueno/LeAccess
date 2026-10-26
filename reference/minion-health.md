# Minion life (2026-10-26)

User request (a player's suggestion): hear the life of a minion build's summons, as in D2R Access. Built from the interop
stubs only; nothing below has been checked in game yet.

## Behavior

Hold the mod button and press **R1** (keyboard **Ctrl+M**) in gameplay. Each press reads one minion card of the game's
HUD, interrupting: "Skeleton 2: life 80 percent, 2 of 5". A group card (one card for many minions) gives its count
instead, "Summon Wraith: 5". After the last card the next press goes back to the first, and after a 10-second pause the
next press starts at the first again. Without cards: "No minions". Nothing is announced automatically. Same gate as the
gold command: gameplay only, never while typing. While the mod button is held the game's button bindings are paused, so
R1 (an ability) does not also fire.

## D2R design it follows

`d2r-access` (`src/minion_report.h`, `src/vitals_service.cpp` `StepCompanions`): BACK+RT steps one companion per press,
following the game's party portraits; counted types say "Name: N", others "Name: life N percent"; repeated names are
numbered; after `RestartMs` (10 s) it restarts at the first; the list is rebuilt on every press. Its full list on one key
was too long for testers, hence the stepper. Not ported: D2R adds where each companion stands (distance, direction)
and includes the mercenary and party members.

## Sources (`last epoch code/`)

Code: `Vitals/MinionHealthReport.cs` (HUD), `Vitals/MinionHealthStepper.cs` (order, numbering, text; tested in
`tests/LeAccess.Vitals.Tests`).

| Class | Members used |
|---|---|
| `MinionUI` (HUD, found once with `FindObjectOfType` on a press, then cached) | `cards` (`Dictionary<SingleMinionCard, SummonedVisuals>`, keys only), `multiMinionCards` |
| `SingleMinionCard` | `healthBar` (`UIBar.currentFill` / `maxFill`), `minionVisuals` (`ActorVisuals`: `dead`, `BaseDisplayInformation` for the name), `ability` |
| `MultiMinionCard` | `currentMinionCount`, `ability` (name of the summoning skill) |

- Life: the card's own bar as drawn, `currentFill / maxFill`; when the bar has no range, the minion's
  `IOverheadBar.HealthNormalized` (0..1, as [boss life](boss-life.md)).
- Name: the minion's display name (`NearestEnemyProbe.ReadName`), else the summoning skill's localized name
  (`Utils/AbilityNames`), else "Minion".
- Order: card positions on screen, rows top to bottom then left to right. Dead minions are skipped.
- The HUD is the same online and offline, so there is no separate tracker path (`MinionCommandAccessibility.CountMinions`
  still counts from the summon trackers for minion orders).
- Log: `[Minion health] <text> (bar 120/200)` or `(overhead 0.6)` / `(group)`; `[Minion health] no cards (hud=False)`.
- Strings: `minions.life`, `minions.count`, `minions.numbered`, `minions.none`, `minions.unavailable`, `minions.unnamed`.

## Unverified, check first

1. `FindObjectOfType<MinionUI>()` finds the HUD (online and offline), and iterating `cards.Keys` works through interop.
2. `UIBar.currentFill` / `maxFill` hold the minion's life (or the same ratio): compare the spoken percent with a minion
   taking damage; the log line shows the raw pair.
3. Which minions get a group card (`SummonedVisuals.singleCardForAllMinions`) and whether the skill name fits them.
4. Card order on screen and whether cards of dead minions disappear before the next press.
