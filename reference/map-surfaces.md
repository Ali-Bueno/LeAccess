# Current player surface descriptions

Disabled by default: the user wants concrete place announcements while exploring,
not generic ground-material labels. This adapter is only polled when
`[Map sections] AnnounceSurfaceChanges` is explicitly enabled. Its verified data
and implementation remain available for that optional use.

`MapSurfaceProbe.TryRead(MapSectionWorldSnapshot, out string textKey)` returns a
verified floor category; `Reset` clears local wrapper caches and diagnostics.
The caller owns player/death/focus/menu/loading gates and announcement priority.
There are no hooks, sound playback calls, input changes or game-field writes.

## Evidence

- Twelve authored `FootstepMaterial` objects in installed `PermaLoad.bundle`
  provide exact names and FMOD GUIDs: snow, wood, mud, ice, sand, concrete,
  water heavy/light, grass, gravel, metal, dirt. The two water events share a
  category. Form/equipment objects Werebear, Spriggan and Armour are excluded.
- [footstep-surfaces.md](footstep-surfaces.md) records all twelve four-integer
  GUIDs and asset path IDs. The independent test fixture
  `tests/LeAccess.MapSurfaceProbe.Tests/SurfaceAssetEvidence.json` preserves
  the bounded extraction's names, path IDs, GUIDs and authored event paths.
- `last epoch code/FootstepMaterial.cs:15`: `soundEvent`;
  installed `FmodShared.dll`: `FMODUnity.EventReference.Guid` and four signed
  `FMOD.GUID.Data1..Data4` fields. Its reference uses `Aliases=FmodGame` to avoid
  the interop assembly's duplicate System nullable attributes.
- `FootstepSoundEmitter.cs:67/82`: active surface buffers;
  `:125/138/151/166/181/194`: ignore flag and override materials/volumes.
- `FootstepRaycastTrigger.cs:90/105/134/149/176/191`: actor visuals, emitter,
  feet, native range, last collider and last holder; static mask at `:120`.
- Native `HandleStepRaycast`, RVA `0x15B5340`, adds exactly `(0, 0.5, 0)` to
  the foot origin and casts down using its mask/range and Ignore triggers.
  The mod reproduces the read-only raycast rather than calling this method,
  which writes the game's last collider/holder fields.
- Native `TerrainFootstepSoundHolder.GetSurfaceSound`, RVA `0x15BF8A0`, samples
  terrain alpha and fills caller buffers without playback or holder state writes.
  Collider getter RVA `0x15AF9C0` likewise fills caller buffers.

## Guards and limits

Only one active player-owned trigger, its actual owned living actor visuals and
emitter are accepted. Cached ownership is rechecked. Feet, collider and holder
must remain active in the current loaded scene. A fresh raycast must identify
the same nearest holder-bearing physical collider; equal-distance alternatives
are ambiguous. This excludes stale cached contact after a teleport.

The known collider/terrain getter resamples the current foot position into local
two-slot buffers cleared before reuse. Its dominant positive finite category
must match the emitter's applicable active surface/override category. Unknown
dominant GUIDs, competing ties, zero/nonfinite weights and ignored surfaces
remain silent. Overrides are read explicitly when enabled unless the native
holder ignores them; mismatches remain silent because override area ownership
cannot be proven from the emitter flag alone.

The fresh getter comparison also catches changed blends after moving within a
single large terrain collider. No renderer names, textures, decorative object
names or scene dependency lists are treated as current floor evidence.
Categories describe authored footstep surfaces; they do not establish bridges,
room functions, indoor status or material physics. Conservative unknowns can
miss surfaces with custom holders, missing feet, stale buffers or differing
unverified overrides. Runtime player hierarchy and terrain behavior still need
gameplay verification.

Validation: source-linked isolated tests cover all fifteen extracted GUIDs,
fresh contact, stale terrain blends, overrides, dominance, native ray arguments,
ownership/lifetime, local buffer reuse, diagnostics and reset. Main no-deploy
build passes; the game has remained closed.
