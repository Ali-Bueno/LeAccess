# Faction, vendor and item-service panels (build 25764382)

Code: `LeAccess/Accessibility/Panels/` (no Harmony hook; polled from `MenuAccessibility`). Evidence: the
`PanelSystemAssetsProvider` entries of `PermaLoad.bundle` resolved through `Catalog.bin` to each panel's prefab
(read-only UnityPy, game closed), plus the interop members in `last epoch code/`. Nothing here was heard in game yet.

## How it works

- `PanelFamily.Identify` classifies the panel that owns a focused element (`GetComponentInParent<Panel>`), cached per element.
- `PanelIntro.LeadFor` returns the panel's introduction once per opening: it leads the first control read in the panel
  (`ElementAnnouncer.AnnounceElement`) or the first item read (`InventoryAccessibility`, through its panel lead notice).
  Closing or hiding the panel makes the next opening introduce it again.
- The introduction is the panel's own visible text that no control, item slot, list, prompt, tooltip or currency counter
  owns (`PanelStaticText`), plus `Panel.Title` for vendors/gamblers/time beast/loot filter/mastery, plus a kind line.
- `PanelControlReader.TryRead` (one line in `ElementAnnouncer.BuildAnnouncementText`) reads controls of these panels.
- Mod wording is `panels.*` in `en.json`/`es.json`; tests in `tests/LeAccess.MenuReading.Tests/PanelTextTests.cs`.

## Vendors and gamblers

Panels: `GoldGamblingPanel`, `SoulGamblingPanel`, `CircleOfFortuneGamblingPanel`, `MerchantsGuildGamblingPanel`,
`HunterVendorPanel`, `WeaverVendorPanel`, `TimeglassVendorPanel`. Their prefabs share one shape: `TabBar` (Buttons with
`TabbedUITab`), `UpperTabs/RefreshButton` (`TabbedShopItemContainerRefreshButton`, label + `|` + cost), a search box, a
`CloseButton`, a footer of bare currency numbers and stock/buyback containers (`GamblingContainerUI`/`ShopContainerUI`).
Items were already read by the inventory reader; gold is spoken by the gold notice.

- **Currencies**: bare footer numbers are named through the game's own `ItemTooltipPriceInfo(type, amount).ToSearchableString()`
  for `UIAncientBonesText` (ancient bones), `UIFavorText` (Circle of Fortune favor, Merchant's Guild favor, Weaver = memory
  amber), `SoulEmberCounterUI` (soul embers) and the `UIKeyText` of the Timeglass vendor (timeglass fragments). Said in the
  introduction ("Tienes ...") and again, queued, when an amount changes (purchase, sale, refresh; `CurrencyChanges`).
  Limit: the Hunter's five primordial-material counters are `UIKeyText` with a bare sub-type and are not named.
- **Tabs**: "name, pestaña [seleccionada], N de M" from `TabbedUITab.IsActive` and the sibling tabs.
- **Refresh**: the button's visible words and cost without the divider, "(no disponible)" when not interactable.
- **Prices**: `MerchantItemReader.TryGetContext` also accepts these panels' stock (slot types `GAMBLING`, `SOUL_GAMBLING`,
  `FACTION_GAMBLING`, `HUNTER_VENDOR`, `WEAVER_VENDOR`, `TIMEGLASS_VENDOR`) and buyback (`SHOP`) and asks the game's
  `ItemTooltipContentBuilder.GetVendorInfo`; a null answer stays silent. Needs an in-game check that the builder answers for
  these slot types. Sale prices of inventory items while these panels are open are not read.

## Factions

`FactionsPanel`/`ChooseItemFactionPanel` hold `FactionTile`s (Banner, primary "Join ...", secondary "Visit ..." Buttons;
Banner has no text, so it used to read as an object name). Entering a tile speaks its visible name, tagline and feature
text (outside Selectables and tooltips) plus "Rango N, favor M" when aligned; moving between its controls says only the
control, and the textless banner says "Estandarte de la facción".

The four rank panels (`CircleOfFortuneRankPanel`, `MerchantsGuildRankPanel`, `ForgottenKnightsRankPanel`,
`WeaverRankPanel`) are a `FactionRankUI` with 12 textless rank Buttons (`rankButtons`) over `FactionReward` rows
(`rankRewards`: title, description, locked/unlocked frame, next-unlock text). Focusing a rank button speaks "Rango N de M
[, rango actual], title, description, desbloqueado | bloqueado, what unlocks it"; unlocked means `rank <= Faction.Rank`.
The introduction adds "Rango N, favor M" (the game shows bare numbers in tooltip objects). Both UIs keep their own
`GamepadButton`, so `FactionReader.Poll` announces it when it changes while a `FactionUI`/`FactionRankUI` is the current
navigable. Limits: the prophecy grid is not read; the Weaver tree and the Forgotten Knights timelines have their own readers below.

## Weaver tree

Evidence (read-only UnityPy, game closed): the tree is `FactionRankPanelWeaver/FactionWeaverSilkenPath` in the `WeaverRankPanel`
prefab: a `WeaverTreeNavigable` (GridNavigable) over `Weaver Tree Holder/Weaver Tree` (`WeaverTree`) with ~80 `Node_*`
objects (`WeaverTreeNode` + `SkillnodeTooltip`, each with a `PointsAllocated` "0/2" label) and the woven-echo item slots
(`WeaverTreeItemContainerUI`). The full-screen `TWFactionUI` (`_weaverTreeNavigable`) hosts the same classes. Header texts:
`UnspentPoints`, `Unconfirmed Spent Points` (`FactionRankUIWeaver.unconfirmedPointsText`), Memory Amber.

`WeaverTreeReader` (polled from `PanelControlReader.Update`; no hook) acts when `NavigationManager.currentlySelected` is a
`WeaverTreeNavigable`; `ActiveNode` is the game's selected node. It uses the passive-tree model,
`NodeTooltipContentBuilder.GetNodeTooltipInfo(node)` (localized name, effect, requirements), so wording is the game's.

- **Entering a node**: "name, N de M puntos asignados[, requisitos no cumplidos], effect"; the first node after opening adds
  "Árbol del Tejedor, N puntos por gastar" and the game's unconfirmed-points text. Moving is the game's spatial navigation.
- **After Cross/Square on the same node**: only the new points, "requisitos no cumplidos" if it locked, the points left and
  the changed unconfirmed text. The node summary is also what the generic focus path returns for a node (`TryRead`), so
  nothing is read twice. A full or locked node shows in its summary instead of a refusal message (no submit hook).
- **Right stick (details)**: effect and point bonus, requirements met/unmet, and the nodes it connects to
  (`Tree.connections`), clockwise from up, each with its screen direction ("Alfa, arriba; Beta, izquierda").
- `PanelStaticText` ignores labels inside nodes and timeline views so the panel introduction is not 80 "0/2" counters.

Limits: no "N de M" place (navigation is spatial, not a grid); the woven-echo slots use the inventory reader; respec mode
text and the search box stay generic; needs an in-game check that `ActiveNode` follows the stick and `unspentPoints`
drops with unconfirmed points.

## Forgotten Knights timelines

Evidence: `ForgottenKnightsRankPanel/HarbingerOverview`: ten `TimelineHarbinger` (`HarbingerDefeatedView`: `IslandName`,
`Harbinger Name` = `hoverText`, `HarbingerKilled`/`HarbingerNotKilled` boxes with `HarbingerIcon` buttons), a side panel
(required corruption, kills, highest corruption kill) and the Aberroth prompt boxes; `FactionNavigationFK` has the
"Harbingers" / "Rank Rewards" buttons. `FactionRankUITimelines` keeps `focusedTimeline`, `timelinesMap` and `CurrentKey`.

`TimelineReader` (called from `FactionReader.Poll`) owns the selection while `timelinesRoot` is active and the gamepad
focus is `GamepadKey.Timelines`, absent, or inside a view; other buttons keep the old reader. Entering the view says the
game's figures and Aberroth prompt plus "N de 10 líneas temporales con el presagiador derrotado"; each timeline then reads
"island, harbinger, presagiador derrotado | sin derrotar, N de 10". Harbinger names come from the label's localization key
(`Localization.GetText`) because the label is hidden. Limits: if `focusedTimeline` is not the `timelinesMap` key the
view is found from the gamepad button only; the extra-teleport and Aberroth location buttons stay generic.

## Item-service panels

`ExchangeWovenEchoesPanel`, `GlyphOfInsightReplacementPanel`, `RandomizeLegendaryPotentialPanel`, `RerollUniqueAffixesPanel`,
`ShatterSetItemsPanel`, `SwapAttributesPanel`, `IdolEnchantmentPanel` share a Woven Echo template: `Title`, `Intro`,
`CraftingUsesRemainingUI` ("(1/2 uses remain)"), item containers, a Craft Button and a Stash Button. The introduction reads
that text; the Reroll panel's two slots are named by the label beside each ("Sacrifice", "Reroll") through
`ServiceSlotReader`. `DungeonKeysTraderPanel` introduces its NPC name and offer and numbers its three key slots
(`_keyContainers`). `EternityCachePanel` is introduced only (its list slots, dropdown and help icons use the generic reader).

## Time beast and loot filter

`TimeBeastEvolutionPanel`: each `TimeBeastEvolutionTile` (a Toggle) reads title, adaptation, reward and guaranteed loot,
"elegida" and "N de M". `LootFilterPanel`: the rule's edit button reads "Regla N de M", the game's rule description,
"desactivada" when off; the rule's switch reads description and on/off (`Rule.isEnabled`). The rule editor modal and the
other buttons stay generic.

## Bazaar purchase confirmation

`BazaarPurchaseConfirmationModal` shows gold, favor and token amounts as bare numbers. `CostModalReader` (through
`GenericModalText`'s new skip/extra arguments) skips those labels and speaks "Coste: <gold>. <favor>. <n> fichas del
Gremio de Mercaderes" for the visible wrappers, and again when the checkbox changes them (header update path).

## Mastery choice

`ChooseMasteryPanel` is a `MasteryPanel` per class: each mastery card has name, description, passive bonuses, an
"Choose Mastery" Button. Focus reads the card's text (outside Selectables) then the button and its place; the
confirmation step (`UnityDrivenNavigable` with header/message) is read once on entering it.

## Verdicts

| Panel | Verdict |
|---|---|
| FactionsPanel, ChooseItemFactionPanel | new reader |
| Circle of Fortune / Merchant's Guild / Forgotten Knights / Weaver rank panels | new reader (rank rewards, standing); Weaver tree and FK timelines read; prophecies not done |
| FullScreenPanelFactions | Weaver tree and FK timelines read (same classes); tabs and reward rows generic |
| Gold / Soul / CoF / MG gambling, Hunter / Weaver / Timeglass vendors | new reader (currencies, tabs, refresh, prices) |
| BazaarPanel | not done (market search, stall and listing UI) |
| BazaarPurchaseConfirmationModal | fixed (named costs) |
| DungeonKeysTrader, Exchange Woven Echoes, Eternity Cache | introduction and slot names |
| Glyph of Insight, Randomize Legendary Potential, Reroll Unique, Shatter Set, Swap Attributes, Idol Enchantment | introduction and slot captions |
| TimeBeastEvolutionPanel | new reader |
| LootFilterPanel | new reader (rules) |
| CharacterStatsPanel | fine: `CharacterStatLineReader` ([character-sheet.md](character-sheet.md)); category tabs and the level header are generic |
| ChooseMasteryPanel | new reader; ConfirmMasteryModal fine (generic modal) |
| AcceptCostConfirmationModal | fine: "Cost:" and "You Have:" labels precede their numbers; the currency has no name |
| SkillRespecChoiceModal, WarningConfirmationModal | fine: title, message and descriptions are static text of the generic modal reader |

## First in-game checks

1. A gambler or vendor: intro with title and currency, tab/refresh speech, price after the item name, currency after a buy.
2. Faction choice tiles and a rank panel: tile summary on entry, rank rewards with locked/unlocked.
3. Woven Echo panels, dungeon keys trader: introduction before the first slot; Reroll slot names.
4. Weaver tree: node entry, a point spent (points left), stick details with neighbours; FK timelines list and entry intro.
5. Time beast tiles, loot filter rules, mastery choice cards, bazaar purchase confirmation costs.
