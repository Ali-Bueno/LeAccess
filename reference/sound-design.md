# Sound direction and original bright loot cues

## User preference, 2026-10-03

The user liked the original coin and equipment cues: their bright timbre suited
Last Epoch. The desired future sound keeps that brightness and clear identity,
with a more realistic physical texture. This is a design reference, not a request
to replace the current D2R recordings yet. **Continue using the D2R sounds for
now.** Preserve the existing radar knocks/breaths as well.

## Preserved listening references

- [Original coin cue](audio/coins-bright-original.wav): two short ascending bright
  chimes, 160 ms total; first 2,100 Hz, then 2,625 Hz.
- [Original equipment cue](audio/equipment-bright-original.wav): a short bright
  metallic strike, 140 ms, with a 1,250 Hz fundamental and partials at 2.76 and
  4.07 times that frequency (relative amplitudes 1, 0.5 and 0.25).

These files contain the exact original synthesized floating-point samples:
48 kHz stereo, front/center position and gain 1. They were exported directly from
`WorldCueSynthesizer.Synthesize` with the natural bank deliberately absent, using
the existing fallback. No microphone, game session or audio device was used.
They are reference assets, not shipped runtime replacements.

Both use equal-power stereo, a peak scale of 0.24 before panning, a 4 ms attack
and 15 ms release. Exponential decay is 22 ms for each coin chime and 32 ms for
equipment. Rear placement lowers pitch continuously to 80 percent. The original
implementation remains the missing-recording fallback in
`LeAccess/Accessibility/Audio/WorldCueSynthesizer.cs`.

Future alternatives should retain recognizable light metallic transients,
short durations, stereo clarity and unobtrusive levels while adding the texture
of real small objects. A coin sound should suggest light coin contact; equipment
should remain a distinct material/metal movement. These are proposed design
directions to compare with the preserved references, not approved new recordings.

Current mappings and source provenance: [natural world sounds](natural-world-sounds.md).
