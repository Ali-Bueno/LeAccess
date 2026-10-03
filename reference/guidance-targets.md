# Objective marker and exit sources

The orientation reader follows the game's current minimap registry. It does not
search the scene, expose all quest assets, reveal fog or infer a correct quest
exit from geographical proximity. Input and spoken route feedback are described
in [objective orientation](objective-orientation.md).
Current build 25690669 also supplies a distinct, guarded Morditas return target
from its native world arrow; [ownership and current native evidence](hazard-sources.md)
are separate from the minimap sources below.

## Native evidence: Steam 1.5.1, build 25672295

The following inspections were recorded against build 25672295. Wrapper line
numbers and native addresses describe that earlier build; they are evidence,
not production offsets or addresses for the current hotfix.

- `LE.UI.Minimap/MinimapIconManager.cs:330/390`: `Instance.icons` holds explicit
  `IconInfo.icon` / `IconInfo.ui` pairs. `DMM.DMMapIcon.OnEnable` RVA `0x18E8020`
  registers the icon through `MinimapIconManager.Add`; `OnDisable` RVA
  `0x18E7F50` removes it. The reader still validates native lifetime and activity.
- `MinimapIconManager.LateUpdate` RVA `0x1F40F90` reads each icon's
  `transform.position` at `0x1F416A8–0x1F416C4`, uses X/Z for projection, and
  switches the image/edge arrow according to distance. The arrow points to the
  same world marker. `Add` RVA `0x1F40A40` instantiates each UI under the manager's
  transform. The minimap transform's world rectangle supplies shader clipping.
- `SetMapIconColourFromObjectiveCondition.cs:28/43/58` explicitly links
  `objectiveCondition`, `icon` and `arrowIcon`. `Start` RVA `0x17CC4C0` follows
  `objectiveCondition.objective.quest.mainLineQuest` when coloring both icons.
  `getReferences` RVA `0x17CC910` searches owned child icons and distinguishes
  them by `useDirectionIndicator`.
- `TrackedObjectiveCondition.CheckConditionReady` RVA `0x186E0A0` resolves the
  objective's local quest list and objective state. For an Active objective it
  additionally calls `StatefulQuestList.UIIsQuestTracked` at `0x186E30C`.
  The reader uses these same read-only model queries; it does not call the
  condition method, which can lazily initialize component references.
- `WorldObjectClickListener.CanInteract` RVA `0x18A1440–0x18A1533` checks native
  availability flags and component enabled state. It does not read positions or
  compare interaction distance, so using it for a distant exit does not impose
  an accidental near-player range limit.

Serialized `scene_Z12_unity.bundle` confirms concrete ownership. The
`Pulse Controller to Burning Forest` component, path ID 17290, references
objective condition 16417 and icons 16395/16407. The two icons share the
`Area Example` child. `Pulse Controller Old Trail` 17291 and
`Pulse Controller Inspect Messgenger` 17292 have equivalent explicit pairs.
These object names are diagnostic evidence, never player-facing names or a
selection heuristic. Their actual quest/objective references provide the names.

Current `PermaLoad.bundle` also verifies the UI ownership, rather than relying
on a presumed hierarchy. `MinimapIconManager` 7228636774030545665 is at
`Minimap/Minimap/SquareMinimap/DMMap Canvas/Icons`; its explicit minimap
2286239388219952295 is the sibling `Map (1)`. Both share the `DMMap Canvas`.
Its prefab `MinimapIconUI` 5304648495402893660 owns MainImage
5187470622558461872 on its own `Minimap Icon` object and ArrowImage
8165068688371125164 on its `Arrow` child. The prefab has no nested canvas, so
native instantiation under the manager gives both images that same canvas.

Reproduction scratch: `%TEMP%/LeAccess-navigation-targets-20261002/` contains the
current method-address map and bounded native disassembly. Existing scene
extraction evidence is under `%TEMP%/LeAccess-scene-assets-20261002/`.

## Selection and visibility

Each registered icon must be active in the player's current scene and have a
finite world position. Its paired UI must be under the current manager, and
its MainImage or ArrowImage must belong to that exact UI and the minimap canvas. Enabled canvas/image
state, sprite availability, renderer culling, image/renderer alpha, inherited
canvas groups and intersection with the minimap clipping rectangle are checked.
Native `Il2CppStructArray<Vector3>` buffers preserve the output of
`RectTransform.GetWorldCorners`; managed arrays would silently lose it through
interop conversion.

There is no verified general point-discovered query. The reader follows actual
active marker UI, including a displayed edge arrow. It does not claim to have
sampled the fog texture or reconstructed discovery state. Arbitrary hidden
objects and exits without a displayed owned marker are not targets.

Quest markers require the explicitly linked objective to be Active in the local
quest list, its visible quest to be Active and UI-tracked, and matching
quest/step/local-model ownership. Forced-untracked quests are rejected.
`StatefulQuest.trackStatus == FORCE_TRACKED` is the highest priority, followed
by a main quest and then other tracked quests. The parent service chooses among
eligible markers; each announcement names the actual quest and objective.
Names come from `Localization.Quests.GetQuestName` and `GetObjectiveActive`,
with authored text fallback.

Circle/arrow pairs deduplicate by their explicit color-controller owner ID.
Their position is the world marker center, not an exact quest completion point.
`ArrivalRadius` is zero and proximity never claims the objective is complete.
There may be more than one valid marker for the same objective.

## Exit fallback and limits

When no eligible quest marker exists, the reader can offer an explicitly named
zone/dungeon exit. The registered icon must belong to an active
`DMMapWorldIcon.MmIcon`; that owner must belong to a live
`WorldObjectClickListener` with an exact owned hover tag. Only
`ZONE_TRANSITION` and `DUNGEON_TRANSITION` tags qualify. Their typed interaction
metadata supplies `sceneName`, localized through
`SceneList.TryGetNonDefaultSceneDetails`. As in landmark reading, disabled
metadata is permitted while the listener/icon are live and eligible.

The exit position and arrival distance come from the real listener and its
`interactionRange`. An exit is labeled as an exit, with no assertion that it is
the correct quest route. Generic door/stairs icons, same-scene transitions,
unnamed exits, unsupported generated-dungeon icon ownership and arbitrary
personal/return portals are excluded. A quest's `Objective.hostScene` does not
establish which of several local exits leads to it; no multi-zone graph was
verified.

## Validation

`tests/LeAccess.GuidanceTargets.Tests` links the production readers and passes
**90 checks**: local quest/step ownership, active/tracked state, pin/main-quest
priority, localized names, circle/arrow deduplication, exact UI ownership,
minimap clipping, hidden/culling/alpha states, finite positions, native exit
availability, inactive metadata and named exit fallback. Missing native data
or failures are distinguished from a successfully inspected empty registry.
An invalid individual entry does not discard other valid candidates.

These checks run with host doubles and no game process. First-use discovery,
marker coverage and hearing the guidance during gameplay still need the user's
listening test.
