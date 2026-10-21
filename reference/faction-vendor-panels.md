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

2026-10-21 (user report: the tiles never said which evolution was chosen; log: no "elegida" ever spoken). The tile's
state came from `tile.GetComponent<Toggle>()`, but the panel keeps its toggles in `_evolutionToggles`, beside
`_evolutionOptions` in the same order (`GetSelectedEvolutionOption`/`ChooseOption` take that index), so the tile's own
object may hold none (**I**, from the interop members). Now: the state comes from `_evolutionToggles[index]` (fallback:
a toggle under or above the tile); an evolution toggle turning on speaks "<header>, chosen" wherever focus is, and its
other changes stay silent (`ListItemReader.TryReadEvolutionChoice`, from `ElementAnnouncer.HandleToggleValueChange`);
the confirm button (`_confirmEvolutionButton`) adds the chosen evolution's header (`_headerText`) or "no evolution
chosen". Unheard.

## Bazaar purchase confirmation

`BazaarPurchaseConfirmationModal` shows gold, favor and token amounts as bare numbers. `CostModalReader` (through
`GenericModalText`'s new skip/extra arguments) skips those labels and speaks "Coste: <gold>. <favor>. <n> fichas del
Gremio de Mercaderes" for the visible wrappers, and again when the checkbox changes them (header update path).

## Bazaar market

Code: `Market/BazaarReader.cs` (event path in `ElementAnnouncer`, results polled from `PanelControlReader.Update`); `PanelKind.Bazaar`
gives the panel an introduction (title and static text). Online data: offline or missing fields read nothing.

- **Listing card** (`ListingUI`): name, type, level, price (gold and/or favor holders that are shown), place among the drawn
  cards. The seller is not shown by the game. With the details key: implicits, affixes, forging/legendary potential, weaver's
  will, corrupted, listed-at text.
- **Pagination** (`PaginationButton`): "Página N de M [actual]", previous and next buttons named.
- **Results**: once per drawn page ("37 anuncios, página 2 de 4") or "Sin resultados" (`QueryItemsResponse`,
  `IsNoResultPanelActive`), not while the loading indicator is up.
- **Dropdowns** (stall type, sort, rarity, class, corrupted, primordial, T6/T7 counts, price-lowering type/frequency) are plain
  `TMP_Dropdown`s. `BazaarControlReader` says "Title: value"; the title is the sibling `FilterTitle` label, and the stall type and
  sort dropdowns (title object inactive in the prefab) are named from `BazaarUI.stallTypeDropdown` / `FilterUI.sortingSelection`.
  Expanded options are generic (`Announcements/DropdownReader.cs`): text, "seleccionado" for the chosen one, "N de M" among the
  active toggles of the list. A value chosen while the list closes is said once by `DropdownValueWatcher` (0.3 s settle) unless the
  focus announcement already spoke it (`NoteSpoken`); every dropdown that goes through the generic path benefits.
- **Fields**: `InputSmall` pairs under one `FilterTitle` read "Rango de nivel, mínimo: 5" / "máximo" (or "vacío"); single fields
  (`NumberInput`) add the `_suffix` unit; the multi-selection openers (affixes, uniques, item type, subtype) read title plus the
  opener's own label text. Typing uses the existing screen-keyboard handling.
- **My stall** (`BazaarStallReader`): the listed / sold buttons read as tabs with the selected one; `UI_STATE` changes are said
  once with the count ("Mi puesto, anuncios activos: 3 anuncios, página 1 de 1" or "Tu puesto no tiene anuncios"); own
  listings (`ItemListingData.IsOwnedByRequestingUser`) add the state from `ItemListingData.State` (a la venta / vendido /
  oro cobrado / retirado; the game has no "expired") and the action from the card's button (`buttonText`, Unlist or Redeem);
  the sold-items indicator reads "Vendidos: N" plus its View Stall button; icon-only Back and Clear buttons are named.
- **List an item** (`BazaarListReader`, `ListItemUI`): placing an item says its name and details once, then the suggested price
  once it loads (polled, `ListItemDetails`/`PriceSuggestionUI`); an empty slot says so. The asking price field reads
  "Precio de venta: 500 de oro" plus the game's tax summary; the price-lowering checkbox, type, amount, frequency and floor read
  as titled controls; the confirm button reads its caption, favor cost and fee/lowering summaries (disabled marked); inventory
  and stash source buttons are named. Gold is the only currency (no selector exists in the prefab).

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
| BazaarPanel | new reader (listings, pagination, results, titled filters and dropdowns, my stall, list-an-item form) |
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
6. Bazaar: a search speaks the result line, listing cards read name, type and price, pages read their number; each filter
   dropdown says title and value, its list says N de M and the chosen one, choosing says the new value once; my stall tabs,
   own listing state and action; list-an-item: item, suggested price, price field, confirm button.
