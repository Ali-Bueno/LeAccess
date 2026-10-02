# Verified footstep surface identifiers

Read-only extraction from installed `PermaLoad.bundle` on 2026-10-02.
MonoScript `FootstepMaterial` path ID: `5456190108186159963` in `monoscripts.bundle`.
The table contains actual ScriptableObject names and FMOD event GUID fields;
these are a whitelist, not renderer-name guesses.

| Asset name | Asset path ID | FMOD GUID: Data1, Data2, Data3, Data4 |
|---|---:|---|
| Snow | 2407956786127121379 | `887160494, 1076243030, 1257745840, 1069063323` |
| Wood | 4224608582414540689 | `-1599689582, 1211331962, 1259039645, 693955642` |
| Mud | 4909982931930653279 | `-1774636125, 1078812808, -1665080694, 1776409609` |
| Ice | 6229388724561835359 | `-921771577, 1145676817, -27846493, -1889599482` |
| Sand | 6418464174638514564 | `-1651031364, 1263018041, 1950930355, 680759971` |
| Concrete | 6481628078800709198 | `-1232125423, 1174426010, -830846533, 148521210` |
| Water_Heavy | 7267416408089382036 | `875980879, 1198802404, -949527367, -1243303349` |
| Grass | 7618841358611768670 | `1422184698, 1315540948, 1827935151, 1868116228` |
| Gravel | 8091279170748383977 | `-1694723251, 1234617459, 998535565, -59455498` |
| Metal | 8576247495394533386 | `1156031969, 1198785631, 1008576413, 1037585669` |
| Water_Light | 9028734250022286606 | `1352783415, 1321329726, -1257089116, -471361175` |
| Dirt | 9167230981946177424 | `-657236694, 1163284407, 873296268, 1716095648` |

All surface event paths have prefix `event:/Combat/John's/Footsteps/sfx_footsteps_`
followed by the lowercase asset name (including underscore for water variants).
Excluded assets: `Werebear`, `Spriggan`, `Armour`; these describe form/equipment,
not a floor. Water light/heavy may share a localized water category; Concrete
must retain its evidenced meaning rather than be silently relabeled stone.

## Runtime sources

- `last epoch code/FootstepMaterial.cs:15`: `soundEvent`.
- Installed `FmodShared.dll`: `FMODUnity.EventReference.Guid`, `.Path`, `.IsNull`;
  `FMOD.GUID.Data1` through `.Data4` are signed 32-bit integers.
- `FootstepSoundEmitter.cs:67` / `:82`: `activeMaterials` / `activeVolumes`;
  `:97`: `extraSound` is a separate equipment/form layer.
- `FootstepSoundEmitter.cs:304`: `FootstepTrigger(Vector3, FootstepSoundHolder)`.
  Native RVA `0x15B6440` updates active surface arrays through holder/override
  processing. A passive postfix can observe a real player footfall without
  changing sound/input; owner identity and menu/loading/death gates remain needed.
- `FootstepRaycastTrigger.cs:176` / `:191`: `lastCollider` / `lastHolder`;
  `TerrainFootstepSoundHolder.cs:98`: actual terrain blend `GetSurfaceSound`;
  `ColliderFootstepSoundHolder.cs:94`: collider sound blend.

Do not poll cached active materials as proof of a fresh surface contact: they may
persist while stationary or after a scene change. Require a real owned footfall
or separately verified fresh contact. Ignore nonfinite/zero weights, equipment
extras and unknown GUIDs; blended contacts need an explicit selection policy.
Surface audio categories do not identify bridges, room functions or indoor status.

Extraction scratch: `%TEMP%/LeAccess-footstep-audit-20261002/probe.py` and
`permaload-footsteps.json` (15 objects). Only PermaLoad was read; no broad bundle
census or game launch. No production surface reader has been implemented.
