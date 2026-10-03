# Merchant item reading

The October 2, 15:23 log showed merchant item names at lines 2093 onward, but
manual inspection of `Dados de marfil` and `Lanza corta` returned unavailable
details. The model fallback recognized only inventory, equipped and stash
containers without visible tooltip parameters. It omitted the ordinary shop.

`MerchantItemReader.ReadContext(item, container)` reads purchase, repurchase or
sale price and native affordability. It requires the exact selected item in a live,
visible container and an open, visible `ShopPanel`. Purchases and repurchases must
match the panel's typed `_shop` or `_buyback` reference and `ContainerType.SHOP`.
The player's inventory and equipped containers expose sale prices only while that
shop is open. Stash and unrelated merchant types cannot inherit this context.

The model fallback now obtains `SlotType.SHOP` for those exact shop/buyback owners,
so manual inspection can build properties without a rendered tooltip. Price
reading calls only `ItemTooltipContentBuilder.GetVendorInfo`, not the full tooltip
builder. It preserves each native `ItemTooltipPriceInfo.ToSearchableString()` and
`CanAfford`, then rechecks selection and shop context. Exceptions or stale/collected
data return no partial price. It never buys, sells, refreshes stock or changes tabs.
Manual model output also appends the current price; unidentified items retain the
price while hidden properties remain omitted.

## Current native evidence

The game updated during user testing to Steam build **25672295**. Evidence below
uses the regenerated 15:05 interop map and that installed `GameAssembly.dll`, not
the earlier build's addresses. Runtime readers use named APIs and enum values.

- `TooltipSystem.OnItemHovered` (`0x2013A00`) dispatches `ContainerType.SHOP`
  through `0x2013C29`; after its specialized vendor checks, the ordinary shop path
  selects `SlotType.SHOP` at `0x2013D77`. Shop and buyback use this same slot.
- `ItemTooltipContentBuilder.GetVendorInfo` (`0x1FD41A0`) selects its gold purchase
  branch for that slot at `0x1FD46AA`. It reads the item's native price, calls
  `GoldTracker.CanAfford` at `0x1FD4708`, and constructs presentation data. The
  sale branch at `0x1FD4A05` uses the item's sale price and marks `IsForPurchase`
  false. No transaction call occurs in this inspected path.
- `ItemTooltipPriceInfo.ToSearchableString` (`0x200A2B0`) formats its value with
  native `ToCurrencyTypeString` (`0x2009ED0`), preserving game-localized currencies.

## Installed shop and tabs

The current `PanelSystemAssetsProvider` in `PermaLoad.bundle` points to the ordinary
shop prefab in `assets_0528f981cb90b4c2.bundle` (container GUID
`113cddea06265714caf731d275f051f4`). `ShopPanel` path ID `6889737796574501008`
owns `BaseShopContainerUI` shop `7667684799135877494` and buyback
`5463880559623891240`. Both contain typed `shopBtn` (`6434578509329221581`),
`buyBackBtn` (`98913146007139333`) and search-input references.

The shop tab's TMP `2609507848705698559` uses localization table `UI`, key
`Shop_Panel_Title`; buyback TMP `9010671146596428493` uses
`Shop_Panel_BuyBack_Button`. Search's placeholder uses `SearchBox_Placeholder`.
These provide typed ownership and native localization for header reading; the
internal English GameObject names are not localized labels.
`MerchantMenuReader.TryReadTab` matches only those exact owned buttons, resolves
their native localization keys, and retains a visible TMP or catalog fallback.
It permits the buyback header while the shop item grid itself is disabled, but
rejects hidden buttons, closed panels and collected owners.

Current native and asset scratch evidence lives in
`%TEMP%/LeAccess-vendor-native-20261002`. Its `map.json` and `scripts.json` were
regenerated against build 25672295; older scratch maps must not be reused against
the updated binaries or bundles.

## Validation

The source-linked item model suite passes **92 checks**, including **29 merchant
item checks** and **13 merchant tab checks**, for shop/buyback/sale prices, native currency and affordability, full
manual properties, unidentified items, packed/unpacked identity, hidden or foreign
owners, missing/collected data, exceptions, and selection/panel changes during
lookup. The production no-deploy build passes without warnings/errors. The game
was not launched; live merchant navigation and listening remain user tests.

## Stash and merchant ownership audit — build 25690669

The follow-up reproduced two source-level gaps. Tab labels only matched buttons
on `ShopPanel._shop`, although `_buyback` owns a separate `BaseShopContainerUI`
with its own shop/buyback buttons. Both exact typed owners are now considered
independently; an absent, collected or differently typed shop grid no longer hides
the live buyback header. Visible button/panel and native localization guards remain.

Stash and shop focus formerly trusted `currentlySelectedItem` even after its
backing tab or stock changed. `InventorySelectionReader.TryGetEntry` now requires
a live backing `Container`, and validates stash/shop hovered items against
`Container.GetEntryAt(hovered.Position)`. The stored entry must retain the selected
native pointer or nonempty serialized item bytes. Missing, collected or different stored data is unavailable;
it cannot supply names, actions, prices or properties from the previous tab/stock.
Matching packed/unpacked identities retain ownership. The existing final
selection checks also catch stock/tab changes during model construction.

Fresh build 25690669 interop confirms `ItemContainerEntry.Position`
(`last epoch code/ItemContainerEntry.cs:155`) and the read-only `GetEntryAt` API.
Native inspection using the matching staged address map found
`StashItemContainerUI.get_Container` (`0x1464690`) returning the holder's
`CurrentContainer`; `TabbedItemContainer.GetEntryAt` (`0x147D970`) reads its active
tab index and delegates to the current `ItemContainer.GetEntryAt`. The holder
also forwards to its active category (`0x145B010`). These are provenance addresses
only; production code uses named APIs and item positions without offsets or scans.

An independent native review confirmed shop and buyback inherit
`ItemContainer.GetEntryAt` (`0x13EEDC0`) through `TradingContainerBase`; this method
matches entry position plus item bounds, so the entry's top-left `Position` is a
valid lookup coordinate even when controller focus lies in another covered cell.
`ItemData.IdEquals` (`0x1201210`) delegates the `id` arrays to
`ByteBufferExtensions.ByteArrayEquals` (`0x1B27360`), which accepts null/null and
empty/empty. Distinct wrappers now need live nonempty IDs; exact native pointers
remain valid when IDs are unavailable. These IDs are serialized item content,
not globally unique instance identifiers: identical content can match, but the
reader always uses the currently stored entry for its actual data and context.

The source-linked suites pass **207 inventory checks** and **111 item model
checks**, adding 38 checks for retained stock/tab items, backing-container
lifetimes, packed identity, native item coordinates, construction-time changes
and separately owned buyback headers, including missing-ID rejection and exact
pointer acceptance. Production builds with
`SkipPluginDeployment=true`, zero warnings/errors. No game/audio launch or
transaction/navigation mutation was performed. Live stash folders, tabs, search,
transfer actions and merchant controller/listening still require user validation.
