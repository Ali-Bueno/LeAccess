# Natural world sounds

The mod now embeds short excerpts from the user's D2R sound collection. Gold,
equipment and verified world services use these recordings with the existing
camera-relative stereo placement. Rearward sounds have a lower pitch. The
existing radar wall/opening sounds and NPC wooden taps remain unchanged.
The original bright synthesized coin/equipment cues and the user's preference
are preserved in [sound design](sound-design.md), including playable WAVs.
Those are listening references; the D2R recordings remain active.

## Meaning and availability

| Audible meaning in Last Epoch | D2R recording | Duration |
|---|---|---|
| Gold pile | `loot/gold.ogg` | 300 ms |
| Equipment allowed by the native loot filter | `loot/normal equipment.ogg` | 300 ms |
| Verified loot chest | `loot/chest.ogg` | 240 ms |
| Stash | `loot/stash.ogg` | 240 ms |
| Portal or waypoint | `exploration/portal.ogg` | 300 ms |
| Zone or dungeon transition | `exploration/zone transition.ogg` | 300 ms |
| Automatic route direction | `exploration/unexplored room.ogg` | 350 ms |

`discovered.ogg` (450 ms) and `inside unexplored room.ogg` (350 ms) are also
available in the shared bank; inclusion alone does not trigger them. The route
sound communicates the next verified direction. It does not assert that a room
is unexplored. See [automatic orientation](automatic-orientation.md) for route
selection, cadence and speech.

`WorldCueIdentityReader` selects sounds from native types rather than display
names. The existing `WorldServiceReader` proves stash/waypoint identity. Portals
must own this listener through `mouseListenerObj` and permit player interaction.
Zone/dungeon transitions require the listener's own typed hover tag and a
nonempty same-scene destination; its metadata may be inactive. Chest recognition
uses exact `ChestSync`, `MonolithChestInteraction`, `TombRewardChestInteraction`,
`CaveTreasureChest` or unopened `VaultsOfUncertainFateChest` components on the
listener's object. Other chest layouts can retain the generic cue.

An unclassified interactable keeps the existing rising prompt. The chest
recording never acts as a generic label for every object, and the equipment cue
does not imply a rarity. The existing registry, native availability, interaction
range, scene, loot-filter and NPC exclusion checks still determine eligibility.
Existing `[World object sounds]` `Enabled` and `Volume` settings govern playback.
NPC/radar settings remain independent. Menus, dialogue, scene changes, pickup,
target disappearance and plugin cleanup retain their cancellation behavior.

## Rendering and failure behavior

`NaturalCueBank.Build(kind, pan, forward, gain)` returns a new interleaved stereo
float buffer at 48 kHz. `Duration(kind)` derives its reservation from the decoded
frame count. PCM is loaded lazily and cached, bounded to one second per asset,
with strict mono/PCM16/sample-rate/chunk validation. The excerpts peak-normalize
to the existing world cue level, use equal-power panning and short edge fades,
and retain the same duration when rear pitch changes. Invalid gain is silent;
invalid directions center safely. Callers cannot mutate the cached source.

The selected excerpts contain audible source signal from their first 100 ms;
measured source RMS exceeded 0.005 full scale before normalization. Their full
source recordings can run for 10–15 seconds, so playing them whole would delay
other navigation information. The bounded excerpts occupy 240–450 ms. World
object excerpts plus their 100 ms device buffer fit inside the existing 450 ms
minimum onset interval. Shared arbitration accounts for actual duration and
buffering, so objects, NPCs, terrain and automatic direction cues take turns.

Missing/corrupt recordings return an empty result without throwing into the
game loop. World cues retain their previous synthesized fallback and report a
deduplicated warning. Playback uses the existing NAudio output and recovery
logic; no native hook or runtime dependency was added.

## Source and reproduction

Source repository: `D:\code\c++\diablo 2 access project\d2r-access`.
Its last sound-asset commit at import was
`6928fd2c874ef6e3a8cd9f618a103f20e2aeb51a`. The user explicitly authorized reuse
in this task. The D2R project's `cmake/GroundItems.cmake` describes the recordings
as user-supplied and identifies the chest cue as coming from the author's
DevilutionX accessibility fork (`audio/chest.ogg`). This records the provenance
available locally; it does not independently establish an original license for
each recording.

The source repository's MIT notice is retained at
`LeAccess/Sounds/D2R-LICENSE.txt` and embedded in the plugin. Original relative
paths, original SHA-256 hashes, excerpt durations and generated asset SHA-256
hashes are recorded in `LeAccess/Sounds/provenance.json`. The D2R source files
were not changed. No external publication was performed.

To reproduce the embedded assets with the already installed FFmpeg command:

```powershell
python tools/import_d2r_audio.py 'D:\code\c++\diablo 2 access project\d2r-access'
```

This maintenance command converts the excerpts to mono, 48 kHz PCM16, removes
metadata and writes their manifest. Ordinary `dotnet build` and installed
playback require neither FFmpeg nor the D2R repository: the nine WAV excerpts
are source assets embedded directly into the mod DLL.

## Verification

The natural-audio suite passes **143 checks** with resources and **24 checks**
with the bank deliberately omitted. Coverage includes all nine recordings,
exact durations, finite/limited samples, audible content, edge fades,
equal-power stereo, rear pitch, invalid inputs, cache isolation, malformed WAV
chunks and audible/directional synthesized fallback. The world-cue suite passes
**149 checks**, including native sound identity, rejection of foreign/inactive
objects, generic fallback, shared arbitration and existing playback lifecycle.

```powershell
dotnet run --project tests/LeAccess.NaturalAudio.Tests/LeAccess.NaturalAudio.Tests.csproj
dotnet run --project tests/LeAccess.NaturalAudio.Tests/LeAccess.NaturalAudio.Tests.csproj -p:OmitNaturalAudioResources=true -- --missing
dotnet run --project tests/LeAccess.WorldCues.Tests/LeAccess.WorldCues.Tests.csproj
```

Main compilation passes with zero warnings/errors. These are source, sample and
state tests; no game or audio device was opened. Listening remains necessary to
judge timbre, level balance, chest coverage and whether simultaneous landmarks
remain easy to distinguish while moving. Approach coins/equipment, then a
verified stash/waypoint/exit; compare left/right/behind and open a menu during a
cue. Verify pickup or leaving range stops that object's repetition.
