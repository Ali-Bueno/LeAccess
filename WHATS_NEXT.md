# Handoff — 2026-10-03 (Claude session after Codex)

Codex reached its weekly usage limit at `8eae87e`; Claude continued the audit and
feature work the user asked for (bugs, playability, ports from the D2R mod).
Read `STATUS.md`, then `PRINCIPLES.md`, before changing code.

## Verified state

- Latest code: pushed `origin/main`, installed with the game closed. Installed
  `LeAccess.dll` equals the build; personal `Translations/*.json` and
  `config/LeAccess.cfg` kept their hashes. Previous plugin and config backed up to
  `C:\Users\ali-b\Documents\LeAccess backups\pre-claude-audit-8eae87e-20261003`.
- 56 test invocations (54 C# suites, ~58,350 checks, missing-audio and Python
  extractor) pass; native audit unchanged at 60 declarations / 59 targets.
- **No game or audio device was launched.** Everything below is host-tested only.

## Done this session (one commit each, see `git log 8eae87e..`)

- Spam fixes: one-band vitals rises silent (mana 62/75 per cast); places and NPC
  names no longer repeat on range-edge flicker (5 s gap) or alt-tab; generic modals
  (loot-filter editor, pickers) read once on open instead of re-reading on every
  scroll; settings sliders speak only the new value; settings headings no longer
  repeat after dropdowns.
- New: potion charges on use/pickup and in the R/Back report; harmful effects in
  the report (localized ailment names); boss/miniboss life bands (on-screen only);
  enemy presence audio cues (D2R growl/swoop, on by default); character-sheet stat lines;
  enemy scan (F7 / right stick up); character-select tiles; world-map waypoint and
  selection reading; controller button names localized ("Triángulo", "L3").
- Fixes from two independent reviews of the above: icon-only popup buttons keep a
  name, settings no longer rebuild every frame, nearest-enemy status string no longer
  grows, world-map poll failures cannot stop the rest of LateUpdate, enemy-cue sight
  snaps targets onto the NavMesh (off-mesh bosses still sound), brief absences keep
  cue cadence, boss life narrates on-screen bosses only.

## Live report fixes (first user session, 20:43 log)

Installed `13fc9c9` with the game closed; previous plugin/config backed up to
`C:\Users\ali-b\Documents\LeAccess backups\pre-live-fixes-f13f726-20261003`.

- "Partida sin conexión" silent: zero-height TMP label rejected by the size check
  from `7710258`; rendered text bounds now count ([menu-visibility](reference/menu-visibility.md)).
- NPC line cut off by the automatic response/continue focus; that focus now queues,
  and the continue button says "Continuar" ([npc-subtitles](reference/npc-subtitles.md)).
- Portal/waypoint sound stopped beyond 2 m and stuttered at the edge; landmarks now
  sound from 12 m with fading and an edge margin ([world-object-sounds](reference/world-object-sounds.md)).

- Second report: loops still sputtered. Cause: 0.3 s excerpts of 10-15 s D2R loops
  repeated with gaps. Now whole recordings through a continuous D2R-style loop mixer
  (`e411d5d`, installed; backup `pre-loops-e19b556-20261003`). Listen for: seamless
  portal/waypoint hum, gold/equipment loops fading with distance, no clicks on pickup,
  loudness against the game (`[World object sounds] Volume`, `LoopRange`, `Loops`).
  The old `LandmarkRange` key in the cfg is now unused.

Re-check these four first; then the list below.

## First live checks for the user

1. Mana/life speech while casting repeatedly (should be quiet between casts).
2. F7 / right stick up in a fight; does the flick disturb aiming?
3. A boss fight: "Nombre, 74 por ciento" steps, death at 0, online scale.
4. World map from a waypoint: does moving the selection speak location, level,
   "Estás aquí"/"Bloqueado"? (Controller focus path is unverified.)
5. Settings: hold left/right on a volume slider; pick a dropdown option.
6. Loot-filter rule editor and stash tab setup: one opening read, quiet scrolling.
7. Character select: name, level/mastery, cycle, hardcore.
8. R/Back report: potions and "Efectos: …" in Spanish.
9. Enemy cues: growls left/right/behind, boss swoop, silence behind walls and in menus;
   loudness against the game; `[Enemy cues] Volume/Bosses/Ordinary` if needed.

## Next work

- Items in the previous handoff that remain open: output-acceptance coverage in
  general menus, map/zone recovery, nearby-entity recovery, advanced filter/forge
  conditions, visual attack telegraphs (see `reference/hazard-sources.md`).
- D2R ports not yet done: attack-reach ping, ailment change cues (needs a
  spam-safe design), death place. Analysis notes are in this session's commits.
- Settings heading reset relies on `SettingsUIManager` becoming invisible on close;
  confirm live.

## Working rules (from the user)

- After each verified fix/feature: update `STATUS.md`, commit and push to `main`.
- Do not launch the game without a new request; the user tests independently.
- Keep everything in this repository (no external agent memory).
- Dev build: `dotnet build LeAccess/LeAccess.csproj --no-restore -p:SkipPluginDeployment=true`;
  a normal build deploys — check the game is closed, back up the plugin first.
