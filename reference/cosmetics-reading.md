# Cosmetics panel reading

`Cosmetics/CosmeticsReader` is consulted by `ElementAnnouncer` after the stash tab reader and before generic
text; `CosmeticsText` composes the speech (unit tested in `LeAccess.MenuReading.Tests`).

| Control | Component | Speech and source |
| --- | --- | --- |
| Equipment, pet, portal, back slots | `CosmeticItemSlot` | Slot: `Localization.GetText(cosmeticLocalizedKey, cosmeticSlotName)`, else the designer name, else `cosmetics.slot`. Cosmetic: `_storedCosmetic.DisplayName`, or `cosmetics.empty`. `locked` adds `cosmetics.locked`. |
| Skill row (`SkillMTX`) | `CosmeticSkillSlot` (subclass) | `ability.abilityName` through `Conventions.GetAbilityNameKey`, then the cosmetic as above. |
| Class filter | `MtxClassToggle` | `Localization.Common.GetClass(Class.ToString())` plus `Toggled` (`cosmetics.filter_on/off`). |
| Category tab (`MTXTab-*`) | `MTXCategorySelection` | Visible `categoryName` text plus `panels.tab` / `panels.tab_selected` (no position). |

Not covered: `MTXFilterToggle` (store filters) and `ProfileFrame` (already localized by the game).

## Unverified (no live test)

- That `cosmeticLocalizedKey` is a localization key (as its name and the key/fallback pattern suggest) and not display text; the
  fallback to `cosmeticSlotName` covers either case.
- That `Cosmetic.DisplayName` is localized and `_storedCosmetic` is kept current by `SetCosmetic` for every slot kind, including
  the default portal (which may read "empty" when the game shows a default).
- That the focused object is, or sits under, these components (`GetComponentInParent`), for the log names in the 2026-10-08 session.
