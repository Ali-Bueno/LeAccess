# Player map contact coverage

Inspected installed Steam 1.5.0.1 assets/native code on 2026-10-02 with the game
closed. These findings support [runtime map places](runtime-map-places.md).
They prove authored prefab capability, not successful live contact detection.

## Verified player visuals

Each of these eight exact visual prefabs contains **one enabled
FootstepRaycastTrigger and two local Transform feet references**. Both feet
resolve into active descendants of that prefab. All five class references come
from the installed PermaLoad `CharacterClass.visualsPrefabSoftRef`; form
references come from the corresponding installed transformation
`Ability.visualSoftRef` (`isTransform = 1`).

| Visual | Installed asset bundle | Trigger path ID | Ray distance |
|---|---|---:|---:|
| Acolyte Visuals Prefab V3 | assets_9aebd6f269cced81.bundle | 1967482266023920279 | 3 m |
| Mage Visuals Prefab V02 | assets_8c83fdfd4e923b72.bundle | 1301766487493555015 | 3 m |
| Primalist Visuals Prefab V3 | assets_f9f9608808d62c5c.bundle | 6194693273299438708 | 3 m |
| Rogue Visuals Prefab_V4 | assets_70b35c1caa304fda.bundle | 2936989293236323096 | 3 m |
| Sentinel Visuals Prefab V3 | assets_7d84073e2642c6af.bundle | 8732913525608713261 | 3 m |
| v_WerebearForm | assets_a3a7d73a3b97542a.bundle | 6372917578518483765 | 5 m |
| v_SprigganForm | assets_2dba72d6753c2a81.bundle | 1441612943633826905 | 5 m |
| v_SwarmbladeForm | assets_62460686359ee4ea.bundle | 1678886330862147230 | 5 m |

The serialized visual prefabs do not contain their enclosing ActorVisuals
component. Trigger ownership is established at runtime; prefab inspection must
not substitute for the adapter's current player/visual hierarchy checks.
The fields are exposed by `last epoch code/FootstepRaycastTrigger.cs:134`
(`feet`) and `:149` (`raycastDistance`). Native `Awake` is RVA `0x15B4FC0`.

## Shared online and offline creation

Native direct-call inspection found both routes calling
`ActorVisuals.CreateClassVisuals` (RVA `0x2799400`):

- `PlayerActorSync.ReceiveSyncCharacterInfoProvider` state machine, call at
  `0x2803CD3` (state-machine entry `0x2803980`); wrapper at
  `last epoch code/PlayerActorSync.cs:12932`.
- `CharacterDataTracker.SetupCharacter` state machine, call at `0x286A7DC`
  (entry `0x286A4D0`); wrapper at `CharacterDataTracker.cs:811`.

The shared creation state machine (`0x27AA2C0`) reads CharacterClassList at
`0x27AA42C`, then instantiates the class soft reference at `0x27AA4C3`.
This establishes a common authored visual source for network character sync
and offline setup. Live hierarchy attachment, form transitions and contact
results remain unverified without gameplay.

## Reproduction and limits

Scratch scripts/evidence are in `%TEMP%/LeAccess-player-footsteps-20261002/`:
`perma_probe.py`, `classbundles.json`, `formresolve.py`, `classfeet.py`,
`formfeet.py`, `footpaths.py`, `class-footstep-evidence.json`,
`form-footstep-evidence.json`, and `feet-hierarchies.json`.
They use the existing UnityPy environment at
`%TEMP%/LeAccess-scene-assets-20261002/venv/Scripts/python.exe` and its cached
`scripts.json` mapping to inspect only the relevant installed bundles.

`LEAssetBundles/Catalog.bin` is a ZIP archive containing `data`, beginning
with `LABC` and version 4. Native Deserializer/PartitionedStringList inspection
identified its string-part table (UInt16 count and byte-length UTF-8 parts)
and partition table (UInt24 counts, UInt16 part indices). Class/form soft GUIDs
were matched as their two little-endian UInt64 values in the catalog's
GUID-to-bundle records; the following UInt16 identifies the bundle. Resolved
bundles were independently verified by loading their actual prefab roots.
Native lookup/disassembly helpers and outputs are retained in the same scratch
directory (`Native.csproj`, `Program.cs`, `disasm.py`, `catalog-native.txt`,
`classvisual-movenext.txt`, `callers.py`). Native method RVAs came from the
installed `BepInEx/interop/MethodAddressToToken.db`; no native hooks were added.

No missing trigger was found among these eight verified visuals. Their 3 m / 5 m
audio ray ranges alone are not proof that the player stands on a nearby lower
floor: contact classification still needs current elevation/ownership guards.
No reliable owned, retained ground Collider alternative was found:
`RPGCharacterController.cs:3452` exposes only `isGrounded`, while
`MovementFromAbility.cs:595` and `UsingAbilityPlayer.cs:706` expose shared static
hit buffers. Those buffers must not be reused as player contact state.
An active owned NavMeshAgent can support a bounded ground probe using actual
actor/agent allowances; `isOnNavMesh` itself only establishes navigation binding,
and off-mesh-link state is an additional rejection condition. That probe is a
conservative fallback, not an authoritative retained physical-contact API.
