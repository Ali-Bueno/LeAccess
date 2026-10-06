# Handoff — 2026-10-06: Social panel for online co-op

User: wants to play online with a friend: share their own username, invite the friend to a party or
add them as a friend, accept an invitation. A dedicated Social reader now speaks friend rows (name,
character, location, section; "Cargando"), section headers (expanded/collapsed and player count),
tabs, the status with "Tu nombre de usuario", the region with ping as one stop, party members
(leader), username fields and window buttons; windows open with their title, explanation and the
Nombre#1234 format; player options say "Opciones de …" and why an option is unavailable; the party
invitation is spoken once; social chat results, the silent unknown-username error and typed
characters are spoken ([details](reference/social-reading.md)). At the user's request the username
fields use the character-name keyboard guard: the screen keyboard opens only on Cross.
First live checks: open Social (status and username), walk friends and headers, toggle a section,
party tab and region, Invitar jugador (introduction, field, keyboard, typing echo, a valid and an
invalid name), Añadir amigo, Cross on a friend (options), receive an invitation and read the
`[Social] Party invite prompt … focus:` log line. Social 58, MenuReading 307, Localization 792,
PatchSafety 46 (79 declarations), ScreenContent 234, Notifications 118, CharacterName 166 pass;
installed, not committed.

# Handoff — 2026-10-05 latest: older tooltip and the forge from the inventory

User: L3 still silent, L3+R3 silent, Square "Mover a Forja" did nothing; must one be at a forge?
Native analysis: the game draws its older tooltip (`UITooltipItem`; the V2 one is behind a disabled
feature switch), which the comparison reader ignored; it now reads it, and logs
`[Inventory comparison state]` per tooltip source/mode ([details](reference/item-comparison.md#older-tooltip-2026-10-05)).
The forge opens with focus on the inventory; L2 reaches the grid, R2 returns. The inventory now
announces the forge and the L2 hint on opening, and Square's move ("X en la forja") or refusal
("No se movió a la forja…"); the forge works anywhere once unlocked
([details](reference/crafting-reading.md#forge-from-the-inventory-2026-10-05)).
First live checks: hold L3 on an inventory item (compare), R3 (explanations), L3 then R3 (ranges),
on an item with nothing equipped in its slot; read the comparison state lines in the log. Open the
forge (lead + hint), Square on an item (moved), Square again with the forge full (refusal), L2 into
the grid, R2 back. Inventory 311, Forge 51, MenuReading 307, Localization 742, PatchSafety 46 pass;
installed, not committed.

# Handoff — 2026-10-05 late: L3/R3 tooltip modes and the forge

User: comparison text never read; the game uses L3/R3; review the forge entirely. L3 (compare),
R3 (explanations) and both (ranges) are held tooltip modes; the reader now follows the tooltip mode
instead of on-screen geometry ([details](reference/item-comparison.md#controller-modes-2026-10-05)).
The forge was silent because its reader used the wrong grid; a polled forge reader now speaks every
cell, the opening, details and each craft's result from the game's own result log line
([forge](reference/crafting-reading.md#controller-forge-2026-10-05)).
First live checks: hold L3, R3 and both on an inventory item; open the forge, walk the grid, add a
shard to a free slot, upgrade an affix, forge (result), glyph and rune flyouts, Square to return the
item. Forge 28, Inventory 274, MenuReading 307, Localization 726, PatchSafety 46 pass; installed, not committed.

# Handoff — 2026-10-05 night: gold in the inventory and shops

User: the gold command works; asked for it on opening the inventory or merchants. The first item
read after the inventory or a shop opens is now led by "Oro: N", and a purchase/sale while a shop is
open leads the next brief with the new amount ([details](reference/vitals-api.md#gold-2026-10-05)).
First live checks: open the inventory (gold then item), a merchant (gold then item), buy and sell
(new gold), close and reopen; also the gambler and faction vendors (all gold vendors count as shops).
Inventory 269 pass. Committed and pushed at the user's request; new friend ZIP
`LeAccess-Steam-1.5.1-prueba-20261005-134547.zip` built (readme lists the new features); listening pending.

# Handoff — 2026-10-05 night: gold

User: the banner works; a friend asked for a way to hear the character's gold. Create+Triangle
(controller) or Ctrl+G (keyboard) now speaks "Oro: N" in gameplay, from the game's own gold
tracker ([details](reference/vitals-api.md#gold-2026-10-05)). Merchants/menus not covered.
First live checks: offline and online value matches the inventory counter; Triangle does not
trigger the game's action while Create is held. Vitals 89, VitalsInput 38, ExplorationInput 27,
Localization 662 pass. The user confirms it works.

# Handoff — 2026-10-05 evening: mastery banner

User: the passive rework works; on the specialization tabs the focus "goes half crazy", the reader
said "Tiradora, 0 puntos" then "Tiradora", and it must say when a tree is open or needs Cross. Fixed:
the creation class reader no longer reads the controller banner (double speech); the banner's cross
of diamonds is now a list (Up/Down, "N de M", Right back to the tree, entry on the shown tree); each
diamond says "Árbol mostrado" or "Confirma para abrir este árbol"; masteries read "Bloqueado" until
one is chosen. [Details](reference/passive-reading.md#mastery-banner-as-a-list-2026-10-05).

First live checks: Left from the first column (lands on the shown tree, "Árboles"), Up/Down over all
four, Cross on another tree (it opens with its first node), Left back and Right into the tree.
Passives 193, Classes 113, Skills 263, Localization 658, PatchSafety 46 pass. The user confirms it works.

# Handoff — 2026-10-05 later: passive trees like the skill trees

User: despecialising works ("funciona"); the passive tree "still feels ultra confusing", make it like
the skill tree. Passive trees have no root or branches (columns are gated by tree points), so
columns stay, with the skill tree's rules: exits only from the first column (Up buttons, Down
strip, Left banner), other ends repeat; column label with its threshold or lock; no "Requisitos
cumplidos"; points left and opened columns after a point; Cross refusals (full, threshold,
requirements, no points). Skill trees also gained "Nodo al máximo".
[Details](reference/passive-reading.md#reading-like-the-skill-trees-2026-10-05).

First live checks: open Passives, walk a column to both ends (repeats), Up/Down from the first
column (buttons/strip) and back, Right into a locked column (label with "tienes N"), spend a point
(points left, "Columna N desbloqueada" when crossing), Cross with no points or on a full node.
Passives 181, Skills 263, Localization 654, PatchSafety 46 pass. The user confirms it works.

# Handoff — 2026-10-05: modifying skills through their trees

User question: can the mod change skills ("variants" per ChatGPT)? Static analysis: no separate
variant system; specialization nodes take points and change the skill. A adds a point (no
confirmation), Respec offers "remove points" (A then refunds via "¿Seguro?", the skill loses a level)
or "despecialise"; specialising is A on a free slot, then A on a skill, or the tree's Especializar.
Four gaps fixed: Up from the root now reaches those buttons (our branch grid had trapped it);
trees speak specialised state, unspent points and point removal mode; A on an ignored node says
why; slot selection is announced. Modals were already covered by the generic reader.
[Details](reference/skill-tree-navigation.md#modifying-a-skill-with-a-controller-2026-10-04-static-analysis).

First live checks: on the overview, A on a free slot ("elige una habilidad…"), then a skill
("Confirma para especializarla"); in its tree, the opening notice and unspent points, A on a node
(points and points left), Up to the root and Up again (Especializar/Respec), Respec → quitar puntos
(mode notice on the node, "No se puede quitar"), Terminar de quitar puntos, despecialise. Skills
263, Localization 644, PatchSafety 46, Passives 160, MenuReading 307 pass; installed with the game
closed.

Result (same morning): the user confirms it works (log: specialising, spending, point removal with
"¿Seguro?", "No se puede quitar"). Still unheard: Respec → "Olvidar especialización", which frees
the slot (all points and the skill's experience are lost; a new skill starts at the minimum level).

# Handoff — 2026-10-04 afternoon: grids and controller mode

User report (15:05–15:18 log): the Skills panel d-pad "does not register well", acts outside the
menu and jumps (base class straight to 30/35-point mastery skills); pressing Ctrl breaks movement;
asked for grid-like navigation in the Skills panel and the other trees, the inventory only needs to
stop interfering. Steam updated the game to **25711439** at 15:18, after that session.

- **Controller mode:** any held/released key (Ctrl to silence NVDA) made the game leave controller
  mode and clear menu focus; the next d-pad press rebuilt it at the default. Screen-reader keys now
  keep controller mode ([cause/fix](reference/runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04)).
- **Skills overview:** one row per class (slots, base class, masteries 1–3), Left/Right inside,
  Up/Down between rows at the same place, no wrap, row name on entry, "N de M"; empty/locked slots
  speak ([grid](reference/skill-tree-navigation.md#skills-overview-grid-2026-10-04)).
- **Trees:** passives by column ("Columna N"), specialization trees by branch ("Rama N"), Up/Down
  inside, Left/Right to the neighbour at the same place; edge exits as before for passives.

First live checks: BepInEx regenerates interop on the first launch of 25711439 (the repair patcher
should log its rows); then hold Ctrl in the Skills panel and move (log `[Controller mode] kept`),
walk every row, Up from the slots (search row), A on a skill; passive and skill trees with the new
column/branch speech; Ctrl in the inventory. After regeneration rerun the PatchSafety audit
(it refuses the stale interop now).

Result (same afternoon): the user confirms everything works "a la perfección"; interop regenerated
on launch, PatchSafety passes on 25711439, and a new friend ZIP was built for game 1.5.1.2.

# Handoff — 2026-10-04: passive tree with a controller

User report: switching trees cut the tab's speech; the cursor "skips nodes", no sense of first/last.
Cause: the controller uses the old `PassivePanelManager` panel, so the tree code never ran, and the
native grid search skips nodes. Now: the grid is one list (Down/Up next/previous, Right/Left jump
column, "N de M"), a tree switch speaks "tree, points, role, unspent" plus its first node once, the
banner diamonds and the controller's skill strip are read. Four new hooks on `PassiveTreeNavigable`.
[Details](reference/passive-reading.md#controller-panel-reading-order-and-tree-switching-2026-10-04).

First live checks: open Passives (overview + first node "1 de N"); walk Down to the end and Up back;
Right/Left between columns; LB/RB between trees; Left from the first column to the banner and back;
Up from node 1 (top buttons) and Down from the last (strip) and return. `[PassiveStatus]` lines show
`mastery banner` / `mastery page switching`. 60 test invocations pass; installed with the game
closed; not committed yet.

First listen (10:09): the list worked, then the game crashed on Up from "1 de 29" — the hook let the
game's original `HandleNavigateInput` run, and that function cannot be called through its detour.
Fixed (the original never runs; [cause](reference/runtime-feedback.md#passive-navigation-crash-detoured-short-branch-prologue-2026-10-04));
three older hooks shared the risk and now replay their tiny bodies instead (name-field mouse click,
chat option toggle, loot-filter modal close). Re-check: Up from the first node, Down from the last,
Left from the first column, several times.

Same list navigation added on request for the backpack and the merchant's stock/buyback grids ([inventory](reference/inventory-drop-focus.md#controller-navigation-2026-10-04):
Right/Left item by item, Down/Up by row, no wrap, "N de M") and the skill specialization trees
([skill trees](reference/skill-tree-navigation.md): Down/Up node by node, Right/Left by branch,
opening now focuses the tree's own root). Listen for: the backpack with a held d-pad (game
auto-repeat kept), Up from the top row to the equipment, a skill tree right after opening and after
LB/RB, A on a node at the end of the list (it must spend the point, not click a slot).

After the push (`b469c69`), from the user's next report: the passive skill strip took "not
learned" from tree points although base-class skills unlock by level; it now uses the character's
known skills ([strip](reference/passive-reading.md#strip-learned-state-2026-10-04)). Square in
Skills is the game's "swap action bar skills" and keeps no skill; the mod now carries the
highlighted skill so the chosen slot's list opens on it ([assignment](reference/skill-assignment.md#square-carries-the-highlighted-skill-2026-10-04)).
Listen for: Square on Shurikens, Left/Right on the bar ("Confirma dos veces para asignar…"),
Cross, Cross; the strip on Shurikens should say "Aprendida".

# Handoff — 2026-10-04 morning: D2R scanner categories

User (after the night batch): "I want the same categories as the D2R radar; I asked you to implement
the exploration mode as it is there and you did it halfway." Now ported from d2r-access: the 15
categories in D2R order with D2R names and speech, rooms "sala N"/"sala inexplorada N", the death place
under *cadáver*, other players, chests, shrines, objects (notes no longer count as NPCs) and ground
items, "El radar ha registrado una salida nueva / un lugar nuevo" with the discovery chime, the
unexplored-room loop and entry cue (whole D2R recordings), and D2R's rule of what is saved.
[Details](reference/exploration-mode.md#d2r-categories-2026-10-04). Host-tested only.

First live checks: flick through every category in a town and in Z62; a new exit's announcement; the
room hum getting louder toward an unentered room and the entry cue; die once and find *cadáver*;
`[Exploration]` status fields `players items death source rooms visited_rooms`.

# Handoff — 2026-10-04 night: unexplored areas and radar delivery

User request (before sleeping): improve the radar's precision a bit, and add unexplored rooms
like D2R; in the 00:46 log the Z62 objective stayed ~200 steps away, the beacon refused it
(no explored route) and nothing pointed at new ground. Done host-side, not heard in game:

- Scanner category *Zonas sin explorar* after the exits. Source: the game's minimap persistence
  volumes (`MinimapVolumesManager`, 185 of 360 scenes; Z62 has 61); a room is listed when not
  entered (game status or the mod's own memory) but some of its ground was seen. Scenes without
  volumes use explored-ground edges. Create+Cross guides there; "Zona explorada" on arrival.
  [Details](reference/exploration-mode.md#unexplored-areas-2026-10-04).
- Radar: terrain cues no longer wait behind NPC taps, enemy growls, object cues or beacon pings
  (the precision gap found against current D2R); openings toward a listed area end in D2R's
  double pip (`[Reactive radar] UnexploredMark`). [Details](reference/navigation-radar.md#latency-and-the-unexplored-mark-2026-10-04).

First live checks: in Z62 flick to *Zonas sin explorar*, lock one toward the objective and walk
there; the `[Exploration]` status should show `source=rooms rooms=61`; check `visited_rooms`
right after loading (does the game's saved status count?); a dungeon or Z12 should show
`source=frontier`. Radar: walls near NPCs/enemies sound on time (`max_wait_ms` in the radar
status); do the pips help or clutter? Tuning keys: `UnexploredMark`, `[Exploration beacon]
ExploreRadius` (also sets the pip range, twice it).

# Handoff — 2026-10-04 menu batch

User report (2026-10-04): map quest press sometimes silent / some quests travel; passive
tree unclear plus "Sprite"; inventory focus back to slot one after drops; NPC lines still
sometimes cut by dialogue options; comparison text not read. All five fixed host-side
([map](reference/waypoint-map.md#cross-on-the-map-travel-2026-10-04),
[passives](reference/passive-reading.md#trees-mastery-and-the-skill-unlock-strip-2026-10-04),
[drop focus](reference/inventory-drop-focus.md), [subtitles](reference/npc-subtitles.md),
[comparison](reference/item-comparison.md)); 58 suites pass, six new native hooks.
First live checks: Cross on a quest with/without waypoint; open Passives and switch trees;
move up into the skill strip; drop three items in a row (log line `[Inventory drop]`
identifies the reset source); hold compare on an item; an NPC with a continue line
followed by responses.

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
- Potions were silent (own native registry, never read). Potions and experience/favor
  tomes now loop too (`a290b8a`, installed; backup `pre-potions-6e2790e-20261003`).

- Loot/landmark sounds: gold, potions, tomes, portals/waypoints and exits now use
  Last Epoch recordings the user picked from the game's banks; equipment, chests and the
  stash stay D2R ([sound design](reference/sound-design.md)). Listen for: gold and tome
  loudness against equipment, exits confused with the game's own waypoint hum. Installed
  with the game closed; backup `pre-le-sounds-20b4a8d-20261003`.

- Loops stacked copies of themselves: the mixer's `Array.Clear` cleared only a quarter of
  NAudio's byte-backed buffer, so old audio summed in (also behind the earlier D2R
  "sputter"). Fixed and verified by a loopback recording; output is now WASAPI with
  a D2R-like glide ([details](reference/world-object-sounds.md#loop-mixer-accumulation-live-reports-2026-10-03)).
  User confirmed the loops now work. Enemy and NPC cues still "mini sputtered": their
  50 ms buffers underran (159 stalls inside 174 enemy cues); both now use the radar's
  stall-free 100 ms ([enemy cues](reference/enemy-cues.md)). Listening pending.

Re-check these five first; then the list below.

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

- Exploration mode (scanner, Create menu, explored-route beacon) implemented 2026-10-03,
  not yet heard; first live checks and open items in
  [exploration-mode](reference/exploration-mode.md#implemented-first-version-2026-10-03-not-yet-heard).
- Radar precision proposals: terrain audio priority done 2026-10-04; bump/scrape from stick
  intent and the heading parking sensor still await the user's choice:
  [review](reference/navigation-radar.md#precision-review-and-proposals-2026-10-03).
- Automatic orientation was silent for the user's whole last session, with no log
  reason; diagnose before tuning ([report](reference/automatic-orientation.md#live-report-silent-for-a-whole-session-2026-10-03-2118-log)).
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
- Keep everything in this repository: never write to Claude Code's auto-memory or any
  other agent memory (repeated 2026-10-03). Discoveries and preferences go here.
- Tests must not compare against translated literals: build expected speech from
  `ModText` keys and from the test's own data/labels, so catalogs can change or be
  ported to another localization system (user, 2026-10-04). All suites were converted
  the same day; only the small `ModText` doubles (NpcProbe, Passives, LandmarkProbe,
  GuidanceTargets) still hold template text, and game-provided strings stay literal.
- Navigation and combat aids should work passively, by sound and speech, without
  pressing keys; manual queries (F6/F7) are secondary extras.
- Dev build: `dotnet build LeAccess/LeAccess.csproj --no-restore -p:SkipPluginDeployment=true`;
  a normal build deploys — check the game is closed. No plugin/config backups: git is the
  history (user rule, 2026-10-04).
