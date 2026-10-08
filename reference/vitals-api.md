# Local-player vitals API — Steam 1.5.0.1

Build 25663618; implemented through the existing speech sink, no game launch,
new dependencies or native hooks.

## Behavior and controls

Automatic reading is enabled by default. Like the local D2R Access implementation
(`src/vitals.h`, `vitals.cpp`, `vitals_service.cpp`), it samples every 100 ms and
tracks percentage bands at 75, 50, 40, 30, 20, 10, 5, 4, 3, 2 and 1, plus zero.
The first observation of each resource is silent. Falling into a lower band
reads the current whole percentage, with life before mana when both change.
Boundaries follow D2R: 100 to 75 is silent; 75 to 74 reads 74.

Unlike D2R, a rise of only one band is silent and keeps the spoken band, so
falling back into it is silent too; a rise of two or more bands reads the
current percentage. Without this, each cast or regeneration cycle across 75 or
50 spoke twice (a 2026-10-02 session logged ~700 mana readings in an hour,
mostly 62/75 pairs). Example: 100 → 62 reads 62; 75, 62, 76 stay silent; 45
reads 45; 51 is silent; 75 reads 75. An explicit report re-baselines both.
Life crossings down to 10 percent or less interrupt speech; other crossings queue.
Mana never interrupts, and below 10 percent it speaks only its first step and
empty (0): running dry is routine for casters, and the same session logged ~120
interrupting mana readings from 5 down to 0. A recovery to 10 or more re-arms it.
Ordinary chip damage within a band is silent. Zero health remains eligible.

During gameplay, press unmodified **R** or controller **Back/View** to read both
percentages and potion charges immediately, including when automatic reading is disabled. Holding
the control does not repeat. The shortcut is ignored while typing, using the
screen keyboard or navigating menus. A View press that opens a native menu in the
same frame as leaving gameplay can still report; it is never deferred until
the menu closes. The mod passively observes these controls, so any native binding
also runs. Controller hardware/template behavior still needs a live test.

## Health potion charges

Ported from D2R's belt reader. The report appends "Potions N of M" from
`HealthPotionCharges.currentCharges`/`maxCharges`: offline `Actor.healthPotion`,
online `LocalPlayer.healthPotion`, accepted only on the same player hierarchy
and scene (the component need not be enabled). With automatic reading on, a
changed count (use or pickup) queues the same text, joined to any vitals
crossing in that sample; the first readable count and each new session are a
silent baseline, and rejected output retries with the current count.
`AnnouncePotionCharges = false` silences the automatic part only.

## Harmful effects (report only)

The report ends with "Effects: Poison 3, Chill": the HUD debuff row
(`BuffsUIManager.instance.debuffs`, active `BuffUIEntry` objects) with each
name (`Ailment.getLocalizedName(false)` when the entry is an ailment, else
`UIBuff.getName()`) and stacks above one. The HUD row is the local player's on
both online and offline clients. Effects are never announced automatically,
since ailments start and stop constantly in combat (D2R only diff-gates poison).
Live check pending: localized names for non-ailment debuffs.

## Gold (2026-10-05)

User request (for a friend): a way to hear the character's gold. **Create+Triangle** (in the
Create menu, game bindings paused) or **Ctrl+G** speaks "Oro: N" alone, interrupting; gameplay
only, never while typing (same gate as the report). Source: `GoldTracker.value`, the tracker the
game's `UIGoldText` counters display, from `ItemContainersManager.Instance.goldTracker`, else
online `LocalPlayer.goldTracker`, else offline `Actor.goldTracker`; none readable → "Oro no
disponible". The user confirmed it works.

Inventory and shops (same day, user request): opening the inventory (`UIBase.IsInventoryOpen`) or
a shop (`IsShopOpen`, also over an open inventory) leads the first item read with "Oro: N"; while a
shop is open, a gold change (purchase, sale, buyback) leads the next brief with the new amount.
The notice waits until a brief carrying it is accepted, then ends when the selection moves
(`InventoryGoldNotice`, tracked every frame so closing behind a modal resets it). Every vendor
whose container tracks gold counts as a shop: `IsShopOpen`, `IsGamblingOpen`, `IsSoulGamblingOpen`,
`IsDungeonKeysTraderPanelOpen`, `IsFactionGamblingCoFOpen`, `IsFactionGamblingMGOpen`,
`IsFactionTheWeaverVendorPanelOpen`, `IsHunterVendorPanelOpen`, `IsTimeglassVendorPanelOpen`,
`IsBazaarPanelOpen` (checked only while the inventory or shop is open; the notice still needs an
item brief, so a vendor without item containers stays silent). Inventory suite 269 checks;
listening pending.

## Gold pickups (2026-10-08)

User request (for a friend): say the gold of each pickup, "3 de oro", "50 de oro". `GoldPickupAnnouncer`
polls the same `GoldTracker.value` as the gold command every frame and speaks each rise, queued
(`gold.pickup`). The first reading, a different tracker (`Pointer`: another character, player or
session) and an unreadable counter are silent baselines; falls (spending, dropping) are ignored; while a
gold vendor is open (`InventoryAccessibility.VendorOpen`) rises are left to the shop's own total notice.
A gain whose speech is rejected adds up with the next one. Quest gold rewards are rises too and are
spoken the same way. `[Vitals] AnnounceGoldPickups` turns it off. Vitals 101 checks; listening pending.

Settings are created on the next launch in `BepInEx/config/LeAccess.cfg`:

```ini
[Vitals]
AutomaticAnnouncements = true
ReportKey = R
ReportWithControllerView = true
AnnouncePotionCharges = true
```

Use `None` for no keyboard shortcut, or disable controller reporting separately.
Manual unavailable-resource messages and percentages use the English/Spanish
`vitals.*` catalog entries. Negative mana reads zero percent; overcharged mana
reads 100 percent. Exact amounts, ward and rage amounts are not part of this
percentage reader. If rage replaces mana, manual speech states the replacement
and automatic mana speech stops until normal mana returns.

Loading, loss of application focus and player/session loss clear the baseline.
Reader unavailability keeps sampling and validating the live player/session;
only accepted output commits a changed band. Missing individual resources retain
their independent baseline.
Automatic reading continues through non-pausing menus so critical losses remain
audible during online play. Diagnostics record actual speech as `[Vitals]` and
deduplicate API/input failures. All delivery uses the existing centralized sink.

The 2026-10-03 acceptance audit on build **25690669** reproduced two lost-report
paths: a rejected output consumed its band change, and reader unavailability
cleared the baseline before reconnect. Automatic speech now compares each fresh
sample against its acknowledged band and commits only after `TrySpeak` accepts
the output. Initial and replacement-session observations remain silent. Critical
losses keep interrupting priority after rejection; recovery into an ordinary band
uses current ordinary wording and priority. Returning to the acknowledged band
before recovery discards the earlier crossing without replay.

No snapshot text or manual edge is queued. Rejected manual reports do not commit
an automatic crossing; automatic mode may subsequently read the current band.
With automatic mode disabled, a later explicit button press is required. The
100 ms automatic sample interval remains active after rejection. Acceptance
means speech/braille transport acceptance and does not establish audibility.

## Authoritative player data

`VitalsProbe` reads offline `PlayerFinder.playerActorComponent.health/mana`
(`Actor.cs:1814`, `:1919`). Online it uses
`PlayerFinder.getLocalPlayerInMultiplayer()` (`PlayerFinder.cs:749`) and
`LocalPlayer.playerHealth/playerMana` (`LocalPlayer.cs:608`, `:623`). It requires
the current visuals' `actorSync` to equal the local player's `actorSync` and the
typed `PlayerActorSync.localPlayer` to point back to that player. This prevents
combining a retained local-player object with a different current visual actor.

Player, local-player and resource components must be live/enabled in the current
loaded scene. Resource components must belong to the current player's hierarchy;
offline health additionally validates its `actor` owner. Session identity combines
scene handle and current player instance ID. Death is not rejected: zero health
remains readable until the player is disabled or despawned. Missing health or mana
produces NaN for that resource without suppressing the other. The parent service
owns foreground/loading/input gates; the probe owns source identity/lifetime.

Values are raw `BaseHealth.currentHealth` float / `maxHealth` int
(`BaseHealth.cs:466`, `:453`) and `BaseMana.currentMana` float / `maxMana` int
(`BaseMana.cs:369`, `:356`). Native `PlayerActorSync.ReceiveSyncHealth`
RVA `0x27D9320` writes the replicated unsigned health to `PlayerHealth`;
`ReceiveSyncMana` RVA `0x27D9D80` sign-extends the replicated signed short and
writes it to `PlayerMana`. Maximum sync routines at `0x27D9E20` and `0x27D9EC0`
update the corresponding maximum fields. Online visuals' byte health ratios are
not suitable for exact values. No native offsets are embedded in the probe.

Negative mana is valid and retained. Overcharged mana may exceed its nominal
maximum: `UIGlobeMana.OnUpdateTick` RVA `0x1810170` expands its bar capacity and
uses a separate negative bar for mana debt. `BaseMana.manaReplacedByRage()`
RVA `0x128E9C0` checks whether active replacement sources are positive. When true,
the snapshot marks `ManaReplaced` and returns NaN mana rather than calling rage
mana. Ward remains separate and is not added to health.

HUD numerical rounding is not applied to raw snapshots or percentage policy.
For any later exact displayed-value command, `UIGlobeHealth.OnUpdateTick`
RVA `0x180F230` displays positive health below one as one; `UIBar` formatting's
native rounding helper `0x3ECF60` rounds to nearest with ties to even.

## Passive controller View input

Existing `Rewired_Core.dll` supplies a hardware-template path without platform
P/Invoke, additional libraries or guessed button numbers:
`EpochInputManager.instance.rewiredPlayer.controllers.Joysticks` identifies the
assigned joysticks; `Controller.GetTemplate<Rewired.IGamepadTemplate>()` obtains
the normalized gamepad template; `IGamepadTemplate.back.justPressed` reports
the physical Back/View edge. `IGamepadTemplate.back` is in the current reference
(`Rewired/IGamepadTemplate.cs:364`), and installed interop exposes
`IControllerTemplateButton.justPressed` plus `Controller.GetTemplate<T>()`.
The assigned joystick list is `Player.ControllerHelper.Joysticks`, verified in
installed interop. Iterate only assigned controllers, guard native wrappers,
and ignore devices without a gamepad template. Reading this edge is passive;
do not consume the input or change the game's map bindings.

## Validation

Four source-linked suites pass **423 checks**: state/percentage **285**, probe
**35**, input **36** and service **67**. Coverage includes strict thresholds and rounding,
independent missing resources, online identity, stale scenes, death, signed mana,
rage replacement, passive key edges, typing/menu guards, automatic-off manual
reports, speech ordering, rejection/reconnect with fresh values, critical priority,
partial resource acceptance and lifecycle resets. These use host doubles for game
objects and the speech sink, with the real translation catalogs in service tests.
These acceptance changes were not deployed; installed-build history remains in
`STATUS.md`. Custom catalog preservation remains part of deployment.
The current no-deployment main build passes with zero warnings/errors.

Listening/runtime validation remains pending, including online player hierarchy,
View hardware mapping, negative mana and transformation mode. The game stayed
closed. Tomorrow, compare spoken percentages with the game's actual resources,
try R and View repeatedly/held, type R in a field, cross bands in both directions,
and change character/zone to confirm a fresh silent baseline.
