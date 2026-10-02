# Nearby world services

Statically verified on 2026-10-02 against Steam **1.5.1 / build 25672295**,
Unity 6000.4.8f1. The game was not launched. Current interop provenance is in
`last epoch code/SOURCE.txt`. Native addresses below are installed RVAs.

| Input | SHA256 |
|---|---|
| GameAssembly.dll | `502E32F1F31BC1979AC6C387FAEF1266A62AD29F5B573E6D9B51CB577A72F5F8` |
| scene_Z32_unity.bundle | `5C125D510C14B457B8E172BB018A97D388440E6791F91353458207A30C85D3EF` |

## NPC labels and merchant role

`NpcNameTooltip.OnPointerEnter` (`0x174A920`) localizes and displays both
`TopTextKey/TopText` and `BottomTextKey/BottomText`, with localization calls at
`0x174A9B7` and `0x174A9FB`. The reader previously omitted the bottom label.
It now includes that authored label and removes identical repeats.

Z32 first-visit Isfarger's GO 12584 has the click listener and
`RelayNPCInteraction` 50086. The relay explicitly references `ConditionHandler`
49491 on child GO 9087. That child is active, the handler is enabled, and
`OpenShop` 49492 is disabled. All five authored NPC phases share this pattern.
`RelayNPCInteraction.OnNPCInteraction` (`0x187FF80`) iterates its explicit list
and invokes active GameObjects' handlers. `OpenShop.OnEnable` (`0x1191150`)
calls `UIBase.openShop` (`0x1800350`), then disables only its own Behaviour
(`0x11911DC`). Its enabled flag therefore cannot determine merchant identity.

`WorldServiceReader` examines only the exact listener GameObject and the owned,
active handlers explicitly referenced by its live relay. OpenShop there proves
the merchant role; no display name, scene ID, hierarchy name or actor ID is
used to guess it. Unrelated descendants and foreign listeners are excluded.
Conflicting roles across live relay branches are left unresolved.

## Service objects without hover labels

Z32 GO 12714 has `OpenMenuInteraction` 49753 (`menu=Map`) and
`WorldObjectClickListener` 49828 (`interactionRange=2`, null hover-tag reference).
Child GO 12534 has `EnableWaypointInteraction` 50030 and `WaypointVFX` 50136,
without another click listener. `EnableWaypointInteraction.EnableWaypoint`
(`0x1864240`) calls `WaypointManager` and its native enable operation. Requiring
a hover tag caused the old landmark reader to discard this waypoint.

| Label | Required native evidence |
|---|---|
| Merchant | Owned `OpenShop`; its self-disabled state is permitted |
| Stash | Active `OpenStash` on the exact interaction GameObject |
| Forge | Active `OpenMenuInteraction.CraftingPanel` |
| Waypoint | Active `OpenMenuInteraction.Map` plus an owned descendant `EnableWaypointInteraction` |

Map alone is insufficient to claim a waypoint. Its activation component may
be on an inactive child after discovery: that component supplies identity,
while the map action and listener must remain active. Ownership lookup includes
inactive ancestors, so nested foreign listeners still fail.

`OpenMenuInteraction.Interaction` (`0x187C010`) calls `UIBase.openMap` at
`0x187C11F` and `UIBase.openCraftingPanel` at `0x187C172`. Do not use its Stash
enum as evidence: jump-table entry 1 at `0x187CA90` points to `0x187C6DE`, which
just returns in this build. The separate `OpenStash.OnUse` (`0x1191500`) calls
`UIBase.openStash` (`0x18005C0`) at `0x1191578`.

Landmarks retain the current player/scene checks, native listener range,
`CanInteract()`, nearest selection and announcement deduplication, and now
accept proven services without a hover tag. NPC services are excluded from
landmark speech and appended to the existing NPC name, e.g. `Isfarger.
Mercader`. No interaction is invoked, component enabled, menu opened or native
hook added by these readers.

## Verification and limits

Source-linked tests pass **101 NPC probe checks** and **86 landmark probe
checks**, preserving all prior 85/66 cases. Added coverage includes localized
bottom labels, duplicate roles, Isfarger's exact relay metadata, disabled and
inactive components, foreign owners, collected wrappers, conflicting roles,
generic maps, inactive waypoint metadata, the obsolete Stash enum, current
stash/forge actions, range, native availability, stale scenes and NPC duplication.
The no-deploy plugin build passes with zero warnings/errors.

Z32's stash comes from a runtime customization slot whose default `chest_slot`
and `chest_slot (1)` roots are initially empty. This task did not resolve that
instantiated prefab; stash coverage still needs a runtime check. No functioning
forge or tavern was established in Z32. The reader supports the proven types
where instantiated; it does not manufacture services from scenery. Verify
Isfarger, the waypoint, the spawned stash and any actual forge in a fresh run.
Landmark diagnostics expose the `service.*` candidate key.

Reproduction uses current `map.json`, `disasm.py` and `scripts.json` under
`%TEMP%/LeAccess-vendor-native-20261002`, plus the Z32 setup/hierarchy resolver
in `%TEMP%/LeAccess-camp-20261002/probe.py`. Asset IDs above are investigation
evidence only; none are runtime constants.
