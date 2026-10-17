# Handoff — 2026-10-17: sheet life and mana, menu in groups, repeat speed; keyboard parked

Released as `v0.2.4` ([changelog](CHANGELOG.md)); the sheet sections work (user, 2026-10-17). Still to hear:
- Character sheet (second fix, the first found no life line): opening reads "main stats" (level, life, regeneration,
  mana, mana regeneration, movement speed, attributes); right stick up/down step through resistances, additional
  defences and the open tab's groups, "N of M" each ([sheet](reference/character-sheet.md#sections-and-life-from-the-prefab-2026-10-17-second-report)).
  Log `[Character sheet] section N/M: …`; check the labels, the group sizes, and the other tabs.
- Mod menu: groups Sounds / Exploration / Speech / Reading / General; sound pages hold volume, repeat speed and
  "Listen: …" previews ([pages](reference/mod-menu.md#pages)).
- `RepeatSpeed` 50-300 % for enemy, NPC, world object and beacon pulses.

Keyboard play: parked by the user for a later session. Plan and open checks in
[keyboard-support](reference/keyboard-support.md): in-process XInput pad for players without one, and the game's own
shortcuts kept over controller mode; first step a test build with `ControllerModeGuard` widened to every key.

# Handoff — 2026-10-13 evening: ways toward unexplored targets, exploration shared by all characters

User request: the scanner only said "up right, no known route" for a quest beyond explored ground; a sighted player
plans on the minimap. Decided with the user: only what the minimap shows (no NavMesh path through the fog), explored
ground shared by every character. Done, built and installed, unheard, not released
([ways](reference/exploration-mode.md#ways-toward-a-target-beyond-explored-ground-2026-10-13-user-request),
[saved](reference/exploration-mode.md#saved-exploration)). Live checks:
- First start: `[Exploration saved] loaded merged N character files …`, then `saved key=…` within 30 s and
  `Exploration/shared.bin` present; a second character sees the first one's rooms and routes.
- A quest in unexplored ground (Z62 "Escapa de la cámara de los guardianes" was the original case): the readout adds
  "Mejor de N caminos: sala inexplorada …; desde allí, …"; next category "caminos hacia …"; log `[Exploration] ways …`.
- Lock the quest: "Guiando hacia …, por …"; walk it; watch `[Exploration beacon] way … news=` lines. False "Sin salida"
  right after entering a room would mean the 1.3 s settle is too short (onward rooms seeded late).
- Frame time with the beacon guided on a large saved map (two capped searches per 0.4 s refresh).

# Handoff — 2026-10-13: first login (terms, display name, email) and Settings over class selection

Tester report, no log: could not get through the first-login screens with a pad; after Submit on the display name
nothing worked; Settings from the pause menu over class selection was silent and the class list unreachable after it.
Changes ([first login](reference/first-login.md), [pause](reference/pause-focus.md)), all unheard, released as `v0.2.3`:
- Terms: hint that Accept unlocks at the end of the text (and the jump button's caption), "Accept is now available"
  once, locked Accept says "(Disabled)".
- Display-name and email forms: title, help and errors read; fields open the keyboard only on Cross ("Cross to type"),
  physical typing through the open keyboard, Back keeps the text; locked Submit says "(Disabled)".
- Pause → Settings → back: the class carousel focus is restored once Settings closes (or on Continue).
Live checks need a fresh account (or clear the `TosAcceptedVersion` setting for the terms): log lines
`[ScreenContent]` for the terms, `[Login] form: AddDisplayName` / `AddEmail`, `[CharacterKeyboard]`, and
`[PauseFocus] waiting: … (selection=…)` while Settings is open over class selection — that selection name is the
evidence for why Settings was silent there. Ask the reporting tester for `BepInEx/LogOutput.log`.

# Handoff — 2026-10-11 14:00: dead pad and Circle after the mod menu, version at startup

A tester (XInput pad, also keyboard/NVDA) could not move after closing the mod menu; Circle in the mod menu closed the
game's inventory; they asked for the version at startup. Their log: every close said `game input resumed`, yet the
player never moved again and later opens counted 10 maps instead of 24. Cause and fix in
[mod-menu.md](reference/mod-menu.md#while-open): maps restored by identity, controller mode frozen while blocked,
`CurrentMap` = `defaultMap` while open and `UpdateMap` on closing, no more null selection. Released as `v0.2.1`.
Verified by Claude in game with a virtual Xbox 360 pad (ViGEm, the user away): 0.2.0 reproduced the dead pad (Ctrl+F1,
arrows and D-pad in the menu, close: no movement, next open 25 maps instead of 39) and Circle closing the inventory;
0.2.1 walks after the same steps, keeps the inventory open with a working cursor, and Cross/Circle in the menu on the
landing screen did not press Play ([evidence](reference/mod-menu.md#code)). Confirmed 2026-10-11 22:03 by the tester
who reported it: their own pad works with 0.2.1. Still unheard by a player: the DualSense chord (L1 with Create), the
startup sentence (the dev install keeps old translation files, so
its log warns `Invalid translation mod.loaded … keeping fallback`; a fresh ZIP has the new ones).

# Handoff — 2026-10-11 afternoon: warm wall sounds, update check, every interactable in the scanner

User: the wall tones were "ultra annoying", wanted softer and warmer, like Grimdark; a startup notice when a newer mod
version exists; everything interactable (Nemesis, Morditas, time rifts...) in the scanner. All three done, unheard.
Live checks:
- Wall tones on: Sounds page previews and a walk; judge the level against game audio and whether the low band behind
  (250 Hz) is heard ([design](reference/navigation-radar.md#warm-wall-sounds-2026-10-11)).
- Start the game online: `[Updates] up to date: running 0.2.0, latest release v0.1.0` in the log, nothing spoken. A
  release tagged above 0.2.0 must be spoken after "mod loaded". Next release: tag `v0.2.0` with `<Version>` 0.2.0, or bump
  both ([rule](reference/updates.md#release-rule)).
- Scanner in a monolith echo, a Rage of Morditas event, a Nemesis and End of Time: the new objects are listed by name;
  collect `[Exploration] unnamed interactable: …` lines to name what is still generic
  ([details](reference/exploration-mode.md#every-interactable-in-the-scanner-2026-10-11-user-request)). Weaver altar and
  rune prison are not covered (no listener found).

# Handoff — 2026-10-11: low-life sound

User request: Diablo IV's low-life start sound and heartbeat as an optional cue ([details](reference/vitals-api.md#low-life-sound-2026-10-11)).
Live checks: turn on `Sonido de vida baja` (Vitals page), preview both sounds on the Sounds page, then drop under 40
percent: one warning, then the heartbeat until healed or dead (`[Low life] on at N%` / `off at N%`); judge its loudness
against speech and the radar. Route legs are on by default now (4).

# Handoff — 2026-10-11: life first and life/mana steps

User request: life before any other announcement, and choosable percent steps. Mod menu, Vitals page: `Vida primero`
(on), `Paso de vida` 1/5/10/25 and `Vida de 10 en 10 / 5 en 5 / 1 en 1 por debajo de` a percent (0 = never), the same
for mana ([design](reference/vitals-api.md#configurable-steps-and-life-first-2026-10-11-user-request)). Defaults keep
today's bands. Live checks: with life first, a life line cuts a menu or NPC line and a line right after it waits;
`[Vitals] …` in the log; try life step 5 and 1 below 20 in a fight and judge the amount of speech; check the 2 s
window is long enough at the user's speech rate.

# Handoff — 2026-10-11 11:18: wall tones

User: an option in the mod menu to use wall tones like Grimdark's, softer but with distance still clearly audible, for
players who prefer them to the radar. Done ([design](reference/navigation-radar.md#wall-tones-2026-10-11)): the old
continuous sonar keys now drive Grimdark-style tones; page and option renamed "Wall tones" in ten languages. Live
checks: turn "Wall tones" on (radar off or on), `[Wall tones] playing` in the log; walking along a corridor the side
tones stay steady and the tone ahead rises toward a wall; hugging a wall does not sound like a wall ahead; menus and
pauses silence them (`[Wall tones] silent: …`); the Sounds page previews the three tones. Ask whether the level and
the high tone ahead suit the user.

# Handoff — 2026-10-11: route legs (optional)

The release tester wants the path, not only the line: barriers, level changes, the side an entrance is entered from.
Added `Exploration/RouteLegs` (mod menu, Exploration page, 4 by default, 0 = off): a detour reads its stretches
([design](reference/exploration-mode.md#route-legs-2026-10-11-optional)). Live checks: with the default 4, scan an exit
behind a detour (Burning Forest at Grael; the Fortress Walls Storerooms from the plateau once the pocket is explored):
the stretches should match the walk, the ramp say "going down", the last stretch run along the pocket floor;
`[Exploration] route … corners=N`. Too many short diagonal stretches at doorways would mean the folding needs a
wider threshold. Not yet in any test package (v0.1.0 predates the route work).

# Handoff — 2026-10-11 11:04: radar decision

Second trace analysed ([decision](reference/navigation-radar.md#second-trace-and-decision-2026-10-11-1100)): no radar
irregularity beyond a minor grazing artifact against walls; no change to the live reading; shadow removed, event lines
kept. The user confirmed the wall bump offline too (11:07) and wants the radar left as it is; the 1.35 m opening
confirmation described there stays an idea only, offer it only if the user finds the radar too busy.

# Handoff — 2026-10-11 09:49: first trace, shadow variants

User: wall bump and silent footsteps work online (offline untested); walked little, mostly testing the other session's
changes. The 14-cue trace already shows the grazing ray (live Left 11.6 → 0 → 10.9 → 0 m within 0.35 s, `edge` 0.00,
both shadows smooth) ([findings](reference/navigation-radar.md#first-trace-2026-10-11-0945)). The shadow now logs two
variants: `origin` (the live ray from an origin moved off a touching wall) and `lanes` (three lanes, median).
Next session, same analysis as below plus: does `origin` alone remove the grazing flips, and does `lanes` lose narrow
passages (`lanes` blocked where live and `origin` breathe)? Adopt the smaller sufficient variant. Also check the wall
bump offline (`push read from the Actor state`).

# Handoff — 2026-10-11 09:30: radar trace before any radar change

User: unsure about the radar proposals; asked for the best diagnostics, will play a while, and the radar changes
follow from that log. Installed, nothing audible changed ([format and how to read it](reference/navigation-radar.md#radar-trace-2026-10-11)).
Next session: take the new `LogOutput.log`, split on `Reactive radar event:` / `shadow:` / `cue:` and the status
`trace:` counts; classify each live flip (grazing ray at the edge, small obstacle, threshold chatter, continuation
toggling, real doorway) from its `depth`/`cont`/`edge` series; compare `live_flips` with `shadow_flips` and check the
shadow kept the real doorway breaths. Then change the live reading accordingly and remove `RadarWorldProbe.Shadow.cs`,
the shadow engine in `ReactiveRadar` and the `shadow=` field (keep the event lines if still useful).

# Handoff — 2026-10-11 09:19: wall bump and silent footsteps

User: sometimes the radar feels inconsistent (brought another assistant's four proposals); wants to know when walking
into a wall stops the character, offline and online, by silencing the footsteps or a short bump, since footsteps are
very quiet in places. Done: a thud from the pushed side once per contact, and the footsteps silenced while pushing
without advancing ([bump](reference/navigation-radar.md#wall-bump-2026-10-11)). No new hook. Radar unchanged: the
proposals were assessed against the 2026-10-10 log ([review](reference/navigation-radar.md#review-of-outside-proposals-and-the-2026-10-10-log-2026-10-11));
open question for the user: build the three-ray reading (after adding depth to each cue's log line)?
Live checks, with log lines:
- `[Wall bump] push read from the Client state` online, `… Actor state` offline (if neither appears, the stick
  state is elsewhere and nothing bumps).
- Straight push into a wall: one thud (`[Wall bump] bump pan=… clearance=…`), footsteps stop
  (`footsteps silenced: N foot triggers, M collider triggers`; M > 0 means a second footstep path still sounds) and
  return on the first real step. Sliding along a wall: no thud, footsteps go on. An enemy in the way: no thud
  (`stopped without a wall within …`). A corner: turning the stick to the other side thuds again.
- Mod menu, Sounds page: "Wall bump" plays; the three new radar options are listed.

# Handoff — 2026-10-11: settings menu and a release tester's report

A player asked for an in-game menu with every option, a glossary of the sounds and group or individual volume and
distance for every object with a sound, opened with Create + L1 or L3 + L1 anywhere ([design](reference/mod-menu.md)).
A release tester then reported straight-line tracker directions, the beacon delivering the player above the Fortress
Walls storerooms, asked for one status button and a "repeat tracker" button, fewer mana announcements, breakables and
shrine effects. All done; nothing heard yet. No new hook. Live checks, with log lines:
- Menu: opens from the main menu, a panel and gameplay (`[Mod menu] opened`); the game ignores the pad while open
  (`game input paused` / `resumed` after release); L1 of the chord neither drinks a potion nor changes era
  (`L1 bindings held back … : N`, N > 0 on DualSense and Xbox); no other speech talks over it (`held back: N`).
- Sounds page: each sound plays at its level; Gold at 0 % goes silent in play; a kind's own distance carries further.
- Scanner: Burning Forest at Grael says the route ("… by route, head down"; `[Exploration] route … straight= route=`);
  an unexplored detour says "no known route". Storerooms: no arrival on the plateau (`arrival held … wall|other level`),
  arrival in the pocket below; check Cross works there.
- Create+Circle repeats the entry; Square/Triangle readouts; `[Exploration] breakables: N` (are crates and pots in the
  Barrel group?); `[Shrine] activated: …` and `[Shrine] effect: …`.
Visited minimap rooms now count as explored (user decision): a detour shown on the minimap routes even if never seen
from close up ([details](reference/exploration-mode.md#visited-minimap-rooms-count-as-explored-2026-10-11)); check
`[Exploration beacon] visited rooms seeded: …` and that Burning Forest at Grael gives a route instead of "no known route".
Mod menu input: Cross, Circle and the D-pad must do nothing in the game while it is open, and closing with Circle must
not dodge (`[Mod menu] game input paused: N controller maps off`; `maps turned back on by the game while open` must
not appear). The first version used `Player.isPlaying` and leaked every button; the second left the landing screen's
Unity UI navigation on (check the log line `UI navigation off: 1` and that Down + Cross there does nothing).
Third version (1ac19b6): the menu also empties the game's menu selection, since its menus read the pad through the
template; check `menu selection held: True` and that Down + Cross on the landing screen does nothing, and that the
selection is back after closing. Fourth version: Cross pressed Play through a `ButtonInputListener`; check
`button listeners off: N` (N > 0 on the landing screen) and that Cross there does nothing.
Fifth: Cross in the menu must not vibrate the pad or play a cursor sound (`game button holds cancelled while open: N`
at closing shows the game had started holds).
Sixth: on the landing screen Cross in the menu must log `[Mod menu] landing click ignored: online` and stay there;
tell whether the pad still vibrates (its source is then still unknown).
Seventh: priority list held too; on the landing screen D-pad and Cross in the menu must not move the focus, log
`landing click ignored` or vibrate; after closing, Down/Cross work again in the landing screen.
Eighth: after closing, `[Mod menu] game controller map chosen again` and the landing screen answers Down and Cross. Confirmed by the user on the landing screen; still to check in game panels and gameplay. Inventory: left/right must step 1, 2, 3 … of N with no jump back.
Dialogue with several responses: each push says the new response once.
Pickup lines (gold, a potion count alone, the targeted ground item) now cut each other, and life, mana and combat lines
cut them ([details](reference/announcement-lifecycle.md#pickup-lines-give-way-2026-10-11-user-report)); check that a
burst of gold and loot leaves only the last line and never delays a life warning.
Not done: more enemy health speech (today bosses and elites only).

# Handoff — 2026-10-10 night: first outside player report

A player's list (English game, NVDA, controller), every item addressed; the user asked that options which depend on
taste be optional. New config keys, all default on: `[Dialogue] SpeakNpcLines`, `[Menus] AnnouncePositions`,
`[Tooltips] ReadKeywordExplanations`, `[Character sheet] ReadSummaryOnOpen`, `[Exploration beacon] TownFullyExplored`
(README table updated). No new hook: the intro's `Update`, `EndOrSkip` and `ButtonInputListenerManager.IsAnyKeyDown`
all start with a short `jcc`, checked with Capstone against `MethodAddressToToken.db`. Live checks, with log lines:
- Intro: Ctrl does not show the skip prompt or skip (`[Intro] skip ignored: screen-reader key`); any other key does.
- Messenger's letter: a direction push repeats "Pick up the letter" (`[Dialogue] response repeated on direction push`).
- Name field: typed letters echo; `SpeakNpcLines=false` silences NPC lines but keeps responses.
- Burning Forest → Keeper's Camp and a town portal: `[Zone] transition: …` spoken.
- Character sheet: summary on opening and on right stick up (`[Character sheet] …`; unlabeled fields:
  `no label or value on screen for: …`).
- Passive/skill details: no "N of M"; Volatile Lightning and the Mage Ward node read "More information …"
  (`[Tooltips] keyword text: …`); a specialised tree says level and percent (`[Skill level] …`, check `progress=` is 0–1).
- Backpack: a mixed-height bottom row walks left↔right in order; up/down keep the column.
- Scanner: quest markers no longer "higher level" (`[Exploration] quest marker icon_y=…`); Keeper's Camp routes to
  Leena and the stash (`[Exploration beacon] town scene=… seeded cells=…`); shrines and gold piles listed on screen;
  far loot/enemies audible (`[World loop] … range=` above 12).
Answered without code: the mod is client-only, so party members do not need it (co-op so far tested only with both
players modded).

# Handoff — 2026-10-10: L3 mod button, player README, forum post

User: pads with only Start/Select cannot use the radar (Select opens the controller menu); use a game command that
does not matter, only off the DualSense, and keep L3 in the inventory (compare, forge). L3 (overlay map, visual only)
is now the mod button on non-PlayStation pads, its overlay-map binding off in gameplay only
([details](reference/exploration-mode.md#l3-as-the-mod-button-on-non-playstation-pads-2026-10-10)). README rewritten in
the style of Grimdark for the first public test release; forum post for the audiogames.net playtest room drafted (not
posted). Check with an Xbox pad: L3 + right stick scans, L3 + X/A/Y/B give vitals/beacon/gold/level, L3 in the
inventory compares, View still opens the controller menu, and the log line `L3 is the mod button`.

# Handoff — 2026-10-09: dynamic radar openings

User: 6 m may be too much; there are very narrow passages; make it dynamic. Openings now need (1) an edge passed:
the probe tip jumps a body width away within one poll (receding and diagonal walls are gradual and stay silent;
a followed wall's real end still breathes) and (2) for depths from 3 m (D2 open depth) up to 6 m, the space to go
on: four branch rays (±45°, ±90°) from one body width short of the far wall, any clear for 3 m. 6 m or unblocked
opens by itself. Config key back to `OpenDepthInBodyWidths` ([details](reference/navigation-radar.md#dynamic-openings-2026-10-09)).
Check: narrow side passages and bends breathe; niches, recesses and stepping away from walls do not; tell me which
real openings stay silent.

# Handoff — 2026-10-08 late night: radar passages, other levels, eras

User: the radar says "open" again after stepping a little away from a wall, so they walked back looking for an exit;
the End of Time Gaspar case (on a platform, unreachable without sighted help); announce the era.
- Radar: of ~3,340 Blocked→Open cues today, 3,031 followed the knock within a second or two. One axis projection
  remembered only at confirmation, diagonal walls (LE's camera) and a 3 m "open" made false openings; invalid
  frames wiped memory. Now open = passage (D2 reach ratio, about 6 m), the knock stays at 2.1 m, the wall memory
  follows the wall while it blocks, a followed wall's real end still breathes, invalid frames are skipped; new
  config key `PassageDepthInBodyWidths`; status `invalid=`/`retreats=` ([details](reference/navigation-radar.md#passages-and-wall-following-2026-10-08)).
  Check: walk along walls (straight and diagonal) and away from them (silent), corridor side doorways and ends
  (breath), how rare "open" became; tell me if 6 m is too strict.
- Scanner entries say "en un nivel más alto/más bajo" for targets a floor above/below (baked agent height)
  ([details](reference/exploration-mode.md#targets-on-another-level-2026-10-08)). Check Gaspar at the End of Time.
- Eras: the area announcement adds the game's era name when travel changes the era; the status report too
  ([details](reference/zone-reading.md)).

# Handoff — 2026-10-08 night: idols, elites, scanner on Create + right stick, minion orders

User (later the same evening): no way to find equipped idols; move the radar to Share + right stick because
the right stick gives minion orders, and make those accessible; "Gilgamesh el Corrupto" (A60) sounded ordinary,
took long to kill and had no life narration; keep looking for bugs. Installed after the game closed; all unheard.
- Idols: the native route is Right from the equipment's right column (top row → idol altar, second row → idol
  grid) or Up twice from the backpack's right half; the equipment slot says "Derecha: ídolos"; entering says
  "Ídolos. N equipados. N ranuras libres…"; idols are a list like the backpack; slot states from the game; one new
  prefix on `IdolsContainerUI.HandleNavigateInput` ([details](reference/idols.md)). Check: the hint, the summary,
  moving and leaving the grid, R3 on an idol.
- Elites: `BaseDisplayInformation.actorClass`/`IsChampion()`; "Gilgamesh el Corrupto, raro", a rising elite cue
  for the nearest rare/champion, life narrated for the nearest on-screen elite; an enemy shown as Boss counts as a
  boss even if `isBossOrMiniBoss()` is false ([details](reference/enemy-class.md)). Check Gilgamesh-like enemies.
- Scanner: Create + right stick; the right stick alone is the game's minion command (the scanner used to fire it:
  27 game NREs today); the mod holds the game's flag down while Create is held ([details](reference/minion-commands.md)).
- Minion orders: polled from the game's re-arm flag; "Esbirros: atacar a X"/"Esbirros: mover", silent without
  minions; log `[Minion command] minions=N target=…`. Check with a minion build online and offline.
- Notes ("Nota", "Registros del primer anciano", "Pintura ancestral"…) sounded like NPCs: they share the NPC
  interaction but have no skinned model; now the tome loop, the scanner's object category and "Cerca: Nota"
  ([details](reference/natural-world-sounds.md#notes-lore-records-and-paintings-2026-10-08)). Check a note and that
  NPCs keep their own cue.
- Passive nodes now say what they require ("requiere 1 punto en X, tiene 0" instead of "Requisitos no cumplidos")
  and the details add "Ruta"/"Lleva a", as in the specialization trees ([details](reference/passive-reading.md#node-requirements-2026-10-08)).
  Check "Égida de acero" in the Sentinel tree.
- Bug sweep of the log: remaining Unity errors are the game's own (decals, ability list on load, level customization).

# Handoff — 2026-10-08 evening: loot by rarity, shrines/rifts/Nemesis, re-reads and online quests

User report (away; installed after the game closed, nothing heard): inventory tabs read oddly; time rifts,
shrines and the Nemesis ("Potenciar/Desterrar/Desafío") silent and unnamed; equipment not by rarity like D2R;
runes, glyphs ("gemas"), shards ("fragmentos") without sounds; duplicate WAVs in `Sounds/` and `Sounds/Loops/`;
after the A90→B10 cinematic the radar found only exits; mid-session: after a drop/equip the controller rumbles
and the focused item is read again seconds later, cutting R3 details; can a waypoint be the login spawn.
- Sounds: equipment/idols by rarity (D2R normal/magic/rare+exalted/unique+set+legendary), rune, glyph → D2R gem,
  shard → D2R scroll, keys/lenses/echoes/other → D2R quest item; shrine and Nemesis loops from Last Epoch's
  `Environmental.bank`; rifts/Monolith portals use the rift loop; caches the chest loop. Names come from the hover
  tag through `Localization.GetText` (key-only tags were unnamed), else "santuario/fisura temporal/Némesis".
  Root WAV excerpts removed: the loops-off fallback cuts the loop at load ([mapping](reference/world-object-sounds.md)).
  Check: each rarity and material on the ground, a shrine and a Nemesis from 12 m, a time rift, loudness of
  rune/gem/scroll clicks and the legendary loop; alternative bank samples are listed in the doc.
- Cosmetics tab (raw "Helmet", "Skill MTX", "Class Filter - Acolyte"): own reader ([details](reference/cosmetics-reading.md)).
- Inventory re-read: the key used wrapper pointers that an online sync replaces; now the item id, and a change
  of only "N de M" is silent. The rumble is the game's haptics (its settings) ([details](reference/inventory-drop-focus.md#re-reading-after-an-online-sync-2026-10-08)).
- B10: nothing was stuck; the arrival point is empty and the exit back (10.5 m below) was not counted as seen;
  it is now, and the `[Exploration]` status logs on arrival with `room_gap`, `came_from` and distances ([details](reference/exploration-mode.md)).
- Found in the log: automatic quest updates and dynamic objectives never spoke online (no saved character data
  online) — fixed ([details](reference/quest-updates.md)); the co-op partner was named 478 times — leaving the
  radius erased the memory; now 60 s for players ([details](reference/nearby-player-names.md)).
- Waypoint spawn: the game stores `CharacterData.LastVisitedTownScene`; the previous session ended in A10 and
  the next began in A06 (Ruinas desmoronadas), so login apparently returns to the last town/camp, not a chosen
  waypoint (inferred, method bodies not read). The mod does not change save data.
- Not done: generic unknown interactables still beep only within their ~2.5 m native radius (widening would make
  every door/lever audible from 12 m); the plugin is now ~18 MB with the new loops.

# Handoff — 2026-10-08: quality batch while the user was away

User request: make every unread menu accessible; physical keyboard typing as soon as the screen keyboard
opens, leaving with Enter or the controller; enemies in the radar, no corpse semantics for the last death;
other QoL. Mid-session: gold pickups said aloud (a friend's request); emotes — Last Epoch has none (no
emote/ping/quick-chat class in the interop; only an audio `WeightedClip.emoteIndex`). Four branches merged,
61 host suites pass, installed, committed and pushed, friend ZIP built. Nothing heard in game.
- Keyboard: the first key used to switch the game to keyboard mode, close the screen keyboard and be lost.
  Now controller mode is kept and keys go through the keyboard's own `SubmitEvent`; Enter submits,
  Escape keeps the text; one new hook (`ButtonInputListener.CanBePressed`) stops game shortcuts while
  typing ([details](reference/character-name-reading.md)). Check: Cross on the name field, type at
  once (first letter, Backspace, @, accents), Enter (focus back, d-pad works), Escape, Ctrl/Shift alone;
  Social invite field (letters must not trigger skills/panels). Unverified: whether Enter/Space also
  press the highlighted on-screen key (would add a letter).
- Menus: [endgame](reference/endgame-panels.md) and [faction/vendor/service](reference/faction-vendor-panels.md)
  readers; their docs list the first checks (tier tabs, blessings, Monolith echo tooltip, votes,
  vendor intro/prices, rank rewards, loot-filter rules). Not covered: Weaver tree, prophecies,
  Forgotten Knights timelines, Bazaar market, Ladder rows, Ritual/Claim buttons.
- Scanner: enemies on screen (not only skill reach); "última muerte" replaces "cadáver"
  ([details](reference/exploration-mode.md)).
- Gold pickups "N de oro" ([details](reference/vitals-api.md#gold-pickups-2026-10-08)); progression:
  "Nivel N", points, skills that can level, Create+Circle / Ctrl+L status ([details](reference/progression.md)).
  Check the XP percent against the bar (log `[Progression]`), and online.
- Later the same day: a friend's report — offline, skills lost "Equipada" and bar slots read "Ability Icon1".
  `PlayerAbilityList` sets `HasInitialised` only after an ability is used; the reader no longer waits for it and
  reads the action bar's own list ([details](reference/skill-assignment.md)). Check offline in town before casting;
  log `[Skill binding] source: action bar list`. Also read now: Weaver tree, Forgotten Knights timelines
  ([factions](reference/faction-vendor-panels.md)), Observatory prophecies, Ladder rows, Morditas rituals
  ([endgame](reference/endgame-panels.md)) and Bazaar listings; still generic: Bazaar/Ladder dropdowns, my stall.
- Afternoon (installed after the user closed the game): generic dropdowns read "N de M" and selected state and say a new value
  once (check settings dropdowns do not speak twice); Bazaar my stall, listing form and filters; Observatory lens
  effects; other players' level and friend character name in the scanner and on approach — if the character
  name never appears for the friend, check the friends-cache key ([details](reference/nearby-player-names.md)).
- Evening: specialization tree nodes say what they require ("requiere Martillos alados"; points needed/held when
  unmet) and the details gesture adds "Ruta: …" and "Lleva a: …" ([details](reference/skill-tree-navigation.md#node-connections-2026-10-08)).
  Check Catapulta: it should name Martillos alados and the route Camino firme, Martillos alados.
- Next QoL proposal (not started): D2R attack-reach ping (sound when an enemy in sight enters the main
  skill's reach; d2r-access `docs/attack-reach.md`).

# Handoff — 2026-10-07 evening: exploration saving, inventory jump, materials, gifting, guide

Game build now 25764382 (Steam updated 14:16; the server had updated first). Three parallel fixes, all
unheard; installed; friend ZIP 20261007-171427; not committed.
- Online exploration never saved: the persistence key needed a character name/class that is blank
  online; `ExplorationKey` falls back to tracker, info provider, then account. Check the new
  `[Exploration saved] online identity key=…` line, then `saved key=online-…` and a reload after restart
  ([details](reference/exploration-mode.md)).
- Inventory jumped to the last item: a hidden mouse left the grid hovered and the game refocused the cell
  under it (0,0) on item changes; a `set_IsHovered` prefix keeps it false in controller mode. Check no jump
  after equips/drops/reopen and no `[Inventory drop] focus moved to 0,0` ([details](reference/inventory-drop-focus.md)).
- Materials panel: shard rows (name, type, count), section headings, filter hint, rune/glyph descriptions
  on the details gesture ([details](reference/crafting-reading.md)).
- "Regalar objeto": panel intro, confirm/resonance/gift slot/close, inventory hint (Square to slot, L2 back),
  result line; check the `[Gift] panel open, focus on …` line ([details](reference/social-reading.md)).
- Guide: entry article read after its label, details gesture rereads, headers say desplegado/plegado; forge
  "Guía" may only show a hint. Stash tabs/folders read name, count, state ([details](reference/guide-reading.md),
  [audit](reference/menu-reading-audit.md)). Not covered yet: endgame panels (Monolith, factions, Nemesis…).

# Handoff — 2026-10-07 later: online skills lost "Equipada" and bar slots read "Ability Icon1"

After restarts the co-op party worked. Online, `[Skill binding] player unavailable`: `SkillBindingReader`
took the equipped list from `PlayerFinder.getPlayerActor()`, but the online local root has no `Actor`, so
skills lost Equipada/No equipada and the assignment flow's bar slots (`AbilityIcon1` under
`ActionBarAbility (n)`) fell back to "Ability Icon1". Online it now reads `LocalPlayer.abilityList` with the
HUD's own `getAbilityCount`/`getAbility` ([details](reference/skill-assignment.md#online-equipped-slots-2026-10-07-build-25711439)).
Skills 268, Players 41, Combat 157 pass; installed; friend ZIP 20261007-123005; not committed.
Listening check: Skills menu (Equipada: button / No equipada), assigning a skill (slot names its skill
and button), the `[Skill binding] ready` log line; transformed (werebear) bar unverified.

# Handoff — 2026-10-07: co-op failed again; creation challenge boxes are silent

User and friend (new characters, both "online") each saw the other "sin conexión" and could not group.
User's side, from Player.log versus the working 2026-10-06 session: the character is Swordfish cycle,
Normal, no solo challenge (nothing pressed at creation); "canario69 ha recibido la invitación" came 3
times but no `RtmClientPartyManager … PartyIDChanged` followed (on 10-06 the party ID appeared at once,
the friend joined 6 s later); new today: `Get Online players - Chat system not connected` 0.3 s before
`Connected to chat` at login, and 3 × `SHA MISMATCH` (client 233fb127…, server 8350432…; Steam reports
no pending update). Native analysis ([details](reference/social-reading.md#presence-and-party-internals-2026-10-07-static-build-25711439)):
presence is account chat connection plus a readable character summary, with no region/mode filter; the
invite line is posted without any server reply, the party is the server's decision. The "1 en línea"
the user heard was the panel's own blank loading row, which the mod counted: fixed (Social 67). Advised a
full restart of both games, same realm/mode/no solo, a whisper test, and the friend's log.
Fixed: the creation screen's "Botín en solitario - personaje/cuenta" checkboxes, "Normal/Difícil" and
"Cambiar de reino" spoke only their names. `CreationChoiceReader` now speaks state on focus and the new
state once after a press. Prefab fact: both mode buttons flip the mode, so pressing "Normal" while Normal
is selected makes a hardcore character ([details](reference/creation-menu-reading.md)). Classes 143,
Localization 822, MenuReading 307, PatchSafety 46, Social 66, package 242 pass; installed, friend ZIP
20261007-113113 built, not committed. Listening check: each control on focus and after Cross, realm title.

# Handoff — 2026-10-06 later: first co-op session

User and a friend played online: friend request, party invite (by "canario69", no #number needed),
the invite prompt (Aceptar reached) and Portal to player all worked. Two faults fixed: after the
portal join the friend's mod lost his own character (PlayerFinder's online visuals cache is emptied
on re-find; one shared `LocalPlayerSource` now uses `LocalPlayer.instance`, 13 call sites,
[details](reference/multiplayer-local-player.md)); the radial "Grupo" party overview spoke raw names
("Member 1", "Class", "Portal To Player") and now reads each member (name, class, leader, life,
barrier, dead, zone) and "Portal a X, botón" ([details](reference/social-reading.md)). The username
hint no longer demands "#1234".
First live checks: in co-op after a portal join, the `[LocalPlayer]` log line (`source=LocalPlayer.instance`),
radar `body_radius` about 0.45, scanner, landmarks and enemies back; the Grupo panel. Social 66,
Navigation 508, Combat 157, Players 41 and the other affected suites pass; installed, not committed.

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
