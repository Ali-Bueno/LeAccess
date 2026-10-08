# Sound direction and original bright loot cues

## Last Epoch recordings (user choice, 2026-10-03)

The user found Last Epoch's own sounds more "magical, crystalline" than the D2R
recordings and chose, after auditioning candidates extracted from the game's
FMOD banks (`Last Epoch_Data/StreamingAssets/*.bank`, unencrypted FSB5 Vorbis
with sample names):

| Mod sound | Last Epoch sample | Bank | Length |
|---|---|---|---|
| Gold | `sfx_ui_general_goldsmall` | `UI` | 0.60 s one-shot |
| Potion | `sfx_ui_potion` | `Environmental` | 0.61 s one-shot |
| Tome (experience/favor) | `sfx_ui_inventory_glyph` | `UI` | 0.67 s one-shot |
| Portal or waypoint | `sfx_soundevent_timerift_loop1` | `Ingame_Events` | 17.0 s loop |
| Zone/dungeon exit | `sfx_enviro_waypoint_loop_v2` | `Environmental` | 14.25 s loop |

Shrines and the Nemesis altar (2026-10-08) use the game's own `Environmental` loops
`sfx_enviro_shrine_nature_main_loop` and `sfx_ui_nemesis_corpse_loop` (not auditioned by the user yet; other shrine
loops: `rainbow_main`, `phoenix`; `sfx_ui_nemesis_corpse_rift_loop`).

None of the equipment-rarity or chest candidates convinced the user
(`sfx_lootfilter_itemdrop_01`-`12`, `sfx_item_tier_*_drop`,
`sfx_item_uniquedrop_v2`, `sfx_item_exalteddrop`, `vouf_chest_*`,
`monolithchest_loop`). Equipment, chests and the stash keep the D2R recordings,
and so do the route and discovery sounds.

The exit sample is probably what the game itself plays at waypoints. If exits
and waypoints are confused in play, that is the first suspect.

One-shots shorter than a second repeat once a second, as the D2R stash already
did. At equal 0.9 peak, their loudest 400 ms measured 11.4 (gold), 6.2 (potion)
and 8.9 dB (tome) under the D2R loops they replace; the portal and exit loops
were within 2 dB. A boost into a limiter (D2R's scroll-click remedy) gained at
most 1.7 dB here. The one-shots therefore take the full headroom of the loop
mix instead: peak `1 / MixGainLimit` (+6.9 dB over 0.9), so even every voice
peaking together stays within full scale. Gold and tomes may still sound
quieter than equipment; check that by listening.

`tools/import_audio.py` decodes the samples with vgmstream-cli (a development
tool only; FFmpeg cannot decode FSB5 Vorbis) and records each bank's SHA-256 in
`provenance.json`. The assets are embedded like the D2R recordings.

Audition copies (not in the repository): every sample of a bank was dumped with
`vgmstream-cli -S 0 -o "<dir>/?n.wav" <bank>` into
`D:\code\unity and such\LastEpoch_sonidos\` (`UI/`, `Ingame_Events/`, a `loot/`
subset and `candidatos/`, the per-category audition set). Loot drop/pickup
sounds are mostly in `UI.bank`, chest landings in `Ingame_Events.bank`.
`Master Bank.strings.bank` (event paths) is compressed, so a plain text search
finds nothing in it. The user still wants better equipment-rarity and chest sounds.

## Original bright cues

### User preference, earlier on 2026-10-03

The user liked the original coin and equipment cues: their bright timbre suited
Last Epoch. The desired future sound keeps that brightness and clear identity,
with a more realistic physical texture. This is a design reference, not a request
to replace the current D2R recordings yet. Preserve the existing radar
knocks/breaths as well. (Later the same day the user replaced some of them with
Last Epoch recordings; see above.)

### Preserved listening references

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
