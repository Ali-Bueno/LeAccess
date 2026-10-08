# Progression feedback (2026-10-08)

Code: `Accessibility/Progression/` (`ProgressionState` pure logic, `ProgressionProbe` IL2CPP reads,
`ProgressionAccessibility` polling and the report). Tests: `tests/LeAccess.Progression.Tests`.
No Harmony hook; the probe polls every 0.25 s. Listening and in-game validation pending.

## Sources (interop in `last epoch code/`)

| Datum | Source | Online / offline |
|---|---|---|
| Level, experience, threshold | `ExperienceTracker.CurrentLevel` / `CurrentExperience` / `NextLevelExperience` (`ExperienceTracker.cs:406-455`); `maxLevel` static (`:110`) | Online `LocalPlayer.instance.experienceTracker` (`LocalPlayer.cs:563`), offline `PlayerFinder.playerExperienceTracker` (`PlayerFinder.cs:409`), fallback `getExperienceTracker()` (`:877`). The local root has no `Actor`, so `Actor.experienceTracker` is not used ([why](multiplayer-local-player.md)). |
| Unspent passive points | `LocalTreeData.passiveTree.getUnspentPoints()` (`LocalTreeData.cs:725-800`, nested `PassiveTreeData`); tree from `tracker.localTreeData` (`ExperienceTracker.cs:153`), then `LocalPlayer.localTreeData` (`:713`) or `PlayerFinder.getLocalTreeData()` | Both |
| Passive fallback | `LevelUpPassivesButton.instance.gameObject.activeSelf` (`LevelUpPassivesButton.cs`, static `instance`, `activate()`/`deactivate()`), used only when the tree cannot be read | HUD singleton, both |
| Skills that can take a point | `LevelUpSkillsButton.instance.abilitiesWithLevelUpButtons` (`LevelUpSkillsButton.cs`), name via `Localization.GetText(Conventions.GetAbilityNameKey(ability.abilityName), abilityName)` | HUD singleton, both |
| New skill unlocked | `LevelUpSkillsButton.instance.newUnlockButton.activeSelf` | HUD singleton, both |
| Zone | `SceneList.TryGetNonDefaultSceneDetails(scene.name).LocalizedName`, as `ZoneAnnouncer` | Both |

XP thresholds are the game's own (`NextLevelExperience`, `maxLevel`); there is no level table in the mod.

## What is spoken

Automatic (config `Progression/AnnounceChanges`, default on), queued and never interrupting, only while
gameplay owns input (held back, not dropped, while a menu or dialogue is open):

- level rises: "Nivel 12" (several level-ups before it is spoken keep the latest);
- unspent passive points rise: "Punto de pasiva disponible" / "3 puntos de pasiva disponibles";
- a skill appears in the HUD level-up list: "Tormenta creciente puede subir de nivel";
- the HUD new-unlock button appears: "Nueva habilidad disponible" (the button carries no skill name).

The first read of a session (tracker identity changes on a new character or load) is a silent baseline; reads reset
on the loading screen, focus loss or an unreadable tracker. Spending points or a skill leaving the list is silent and
withdraws queued speech that became stale.

Report: Create+Circle (controller) or Ctrl+L (keyboard): level, percent toward the next level (or "Nivel maximo"),
unspent passive points, skills that can level up, new-skill flag, zone. The Create modifier already pauses every game
button binding, so Circle does not also trigger the game's action.
Ctrl+L: `L` has no mod or NVDA use, and the other mod commands (Ctrl+G, Ctrl+B) use the same Ctrl-only pattern; the game's
own default key for `L` is unverified (plain `L` is never read, only Ctrl+L with no Alt/Shift).

## Limits and unverified points

- The percentage assumes `CurrentExperience / NextLevelExperience` is progress inside the level (the XP bar's shape, `UIExperienceBar` keeps
  `previousCurrentExperience`/`previousNextLevelExperience`). The report logs `xp current/next`; confirm in game.
- Skill XP (ability levels) is not announced; only the HUD's "can take a point" list.
- `getUnspentPoints()` online and the HUD buttons' `activeSelf` semantics are unverified; a failing read falls back to the HUD button.
