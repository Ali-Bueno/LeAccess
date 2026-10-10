# Natural world sounds

> **Origin (user, 2026-10-10):** the recordings imported from the unreleased Diablo 2 mod (called "D2R" below) were
> taken from Diablo IV. They are to be replaced over time; gold, potions, tomes, portals, waypoints, exits, shrines,
> the Nemesis and time rifts already use Last Epoch's own sounds ([sound design](sound-design.md)).

The mod now embeds short excerpts from the user's D2R sound collection and from
Last Epoch's own FMOD banks ([user choice](sound-design.md#last-epoch-recordings-user-choice-2026-10-03)),
and the whole recordings for the continuous world loop mixer
([continuous loops](world-object-sounds.md#continuous-loops-d2r-mixer-port-2026-10-03)).
With loops on, gold, equipment, chests, the stash, portals and transitions use
the loops; the excerpts below remain the fallback and the route sound. Gold,
equipment and verified world services use these recordings with the existing
camera-relative stereo placement. Rearward sounds have a lower pitch. The
existing radar wall/opening sounds and NPC wooden taps remain unchanged.
The original bright synthesized coin/equipment cues and the user's preference
are preserved in [sound design](sound-design.md), including playable WAVs.
Those are listening references, not active sounds.

## Meaning and availability

Every duration below is the first part of the matching `Sounds/Loops/` recording, cut when the fallback cue loads
(no separate files; see [loops](world-object-sounds.md#fallback-cues-are-cut-from-the-loops)). Rarity, rune, gem,
scroll, quest-item, shrine and Nemesis loops: [mapping](world-object-sounds.md#loot-by-rarity-and-type-and-more-interactables-2026-10-08).

| Audible meaning in Last Epoch | Recording (D2R unless marked LE) | Duration |
|---|---|---|
| Gold pile | LE `UI.bank` `sfx_ui_general_goldsmall` | 300 ms |
| Equipment allowed by the native loot filter | `loot/normal equipment.ogg` | 300 ms |
| Verified loot chest | `loot/chest.ogg` | 240 ms |
| Stash | `loot/stash.ogg` | 240 ms |
| Portal or waypoint | LE `Ingame_Events.bank` `sfx_soundevent_timerift_loop1` | 300 ms |
| Zone or dungeon transition | LE `Environmental.bank` `sfx_enviro_waypoint_loop_v2` | 300 ms |
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

Source repository (the author's unreleased Diablo 2 mod): `D:\code\c++\diablo 2 access project\d2r-access`.
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
hashes are recorded in `LeAccess/Sounds/provenance.json`; a Last Epoch source is
`<bank path>#<sample name>` with the whole bank's hash. The D2R source files
were not changed. No external publication was performed.

To reproduce the embedded assets with FFmpeg and vgmstream-cli
(<https://github.com/vgmstream/vgmstream/releases>, r2117 used):

```powershell
python tools/import_audio.py 'D:\code\c++\diablo 2 access project\d2r-access' 'D:\games\steam\steamapps\common\Last Epoch' --vgmstream <path>\vgmstream-cli.exe
```

This maintenance command converts the excerpts to mono, 48 kHz PCM16, removes
metadata and writes their manifest. It finds Last Epoch samples by name in the
FSB5 name table and checks vgmstream reports the same name. Rerunning it
reproduces the D2R assets byte for byte. Ordinary `dotnet build` and installed
playback require none of these tools or sources: the nine WAV excerpts and the
loops are embedded directly into the mod DLL.

## Verification

The natural-audio suite passes **143 checks** with resources and **24 checks**
with the bank deliberately omitted. Coverage includes all nine recordings,
exact durations, finite/limited samples, audible content, edge fades,
equal-power stereo, rear pitch, invalid inputs, cache isolation, malformed WAV
chunks and audible/directional synthesized fallback. The world-cue suite passes
**149 checks**, including native sound identity, rejection of foreign/inactive
objects, generic fallback, shared arbitration and existing playback lifecycle.

The camera-lifetime follow-up adds four checks (153 world-cue checks total).
Reacquiring `Camera.main` now verifies that the returned camera is enabled;
previously, reacquiring the same disabled cached camera could still authorize
directional world cues. Reactivating it or replacing it with an enabled camera
restores detection. No game or audio device was opened for these checks.

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

## Notes, lore records and paintings (2026-10-08)

User: notes, messages and scrolls sounded like NPCs; they should use the tome sound. They are conversation objects:
the same `RelayNPCInteraction`/`LEConversationTrigger` as NPCs, so `WorldServiceReader.IsNpc` took them and the
log named them as NPCs ("Nota", "Nota ajada", "Registros del primer anciano", "El registro de Erza", "Pintura
ancestral", "El fragmento"). Scene survey (UnityPy over `scene_{A10,A40,A60,EoT}_unity.bundle` with
`monoscripts.bundle`): every NPC and examinable body has a `SkinnedMeshRenderer` under its click listener; notes
("Note", "Records of the First Elder", "Lore Coll Placeholder") have none. Exceptions seen: EoT's "Fertile Soil" and
"Lost Spriggan" easter egg also have no skinned model.

`WorldServiceReader.IsReadable`: an NPC interaction, no skinned model under the listener (nor under its relay's or its
`NpcNameTooltip` owner's root, for collider-child layouts), and no service. Readables get the tome loop
(`WorldCueIdentityReader`), no NPC cue (`NpcWorldProbe.IsNpc` is `IsCharacterNpc`), the scanner's object category,
and the nearby-landmark announcement by their dialogue name ("Cerca: Nota", status `readable`). Positive model checks
are cached per listener; a model that loads later turns the object into an NPC. NpcProbe 112, LandmarkProbe 101,
WorldCues 246 checks. Listening pending.
