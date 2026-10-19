# Changelog

What changed in LeAccess, day by day, newest first. Each heading gives the day and the version that first shipped
those changes. It is written from the commit history; internal notes, status updates and merges are left out.

## 19 October 2026 — version 0.2.5

New:

- The season's Rage of Morditas event is accessible. Starting the blood crystal says the game's text and what it never
  shows: how many monsters to kill before time runs out (20) and the time left. Kills are counted ("12 of 20 kills"),
  time warnings come at 15 and 5 seconds, then charges. In the gauntlet the mod says how far from the spot you must stay,
  the enemies left, when you leave the circle (with the way back and the abandon countdown) and when you are back, then
  the reward or the failure. The mod button with Square adds the event's status after life and mana.
- Each time the game draws its blood trails toward monsters, a drip sounds from the nearest one, quieter when it is far
  (Sounds → Rage of Morditas: on/off, volume, Listen). The trails are also the scanner's first category while the hunt
  lasts, and the beacon can follow one.

Fixed:

- In maps with minimap rooms, the scanner now also lists "unexplored area N": edges of the explored map where no room
  covers the ground. Before, once the nearby rooms were visited, a quest beyond explored ground had no way to try.

## 17 October 2026 — version 0.2.4

New:

- The settings menu is split into five sections: Sounds, Exploration, Announcements, Menus and screens, and General.
  Every sound page has its volume and options together, then "Listen" entries that play each of its sounds. The low
  life sound has its own page under Sounds.
- Enemy, NPC, object and beacon sounds that repeat have a repeat speed (50 to 300 percent) to make them repeat faster
  or slower. Continuous loops are not affected.

Fixed:

- The character sheet reads life, mana, their regeneration and movement speed, which were never found before, online
  or offline. It reads in sections like the screen: main stats, resistances, additional defences, then the open tab's
  stats in groups (speeds, critical chances, critical multipliers…). Right stick up goes to the next section, down
  back to the previous one; opening the sheet reads the main stats.

## 13 October 2026, evening — version 0.2.4

New:

- When there is no known route to something in the scanner, usually a quest in ground you have not explored, the
  scanner names the best way toward it: the edge of the explored map that leads that way, how to walk there, and how
  far the target is from there. A "ways to …" category lists the other ways. The beacon can follow them, tells you when
  a way was a dead end and when the route to the target becomes known.
- What you explore is remembered for all your characters, offline and online. Maps explored before this version are
  merged automatically.

## 13 October 2026 — version 0.2.3

Fixed:

- First start of the game with a new account: the terms now explain that Accept stays disabled until the text has been
  scrolled to the end, name the button that jumps there, and say when Accept becomes available.
- The display name and email screens read their title, explanation and any error. Their fields work like the character
  name: Cross opens the screen keyboard, you can type on it or with the physical keyboard, and Circle keeps what you
  typed. Disabled Submit and Accept buttons say so.
- After opening Settings from the pause menu while choosing a class, closing it brings you back to the class list.

## 12 October 2026 — version 0.2.2

Fixed:

- The game closed (crashed to the desktop) when a player from your friends list came near you in an online game. It
  happened while the mod read that friend's character name, which it now reads in a safe way.

## 11 October 2026, afternoon — version 0.2.1

New:

- At startup the mod says its name and version ("LeAccess version 0.2.1 loaded"), so you know whether you have the
  latest.

Fixed:

- After closing the mod menu, the controller could stop moving the character until the game was restarted. It happened
  on any pad when the keyboard was used while the menu was open (Ctrl+F1, the arrows, Alt+Tab). The controller now keeps
  working.
- Circle in the mod menu no longer closes the game's inventory or another panel open behind it.

## 11 October 2026 — version 0.2.0

New:

- **Mod settings menu** with every option, opened anywhere with L1 while Create or L3 is held (Ctrl+F1 on the
  keyboard). It includes a glossary that plays each of the mod's sounds, and a volume and distance for each kind of
  world sound. While it is open the game gets no controller input: no dodges, menu presses, vibration or clicks on the
  landing screen.
- **Update check**: when the game starts, the mod says if a newer version is published on GitHub. You can turn it off
  on the Updates page of the mod menu.
- **Wall tones**, off by default: an alternative or companion to the radar in the style of Grimdark. A soft rush of air
  per direction grows louder as a wall comes closer: high ahead, low behind, mid in each ear for the sides.
- **Wall bump**: a thud from the side you push when you walk into a wall and stop, and your footsteps go quiet while
  you keep pushing.
- **The scanner lists every interactable object**: Rage of Morditas crystals and rewards, Unstable Rifts, Beacons, the
  Monolith forge, Timeline catalysts, Omen mirrors, and objects that open menus (arena, dungeons, Observatory,
  factions, gamblers, vendors). The Nemesis and these objects are listed as soon as they are on screen. Objects the mod
  cannot name yet are listed under a generic name.
- **Directions follow the explored route** ("260 steps by route, head down"), and a detour can be read stretch by
  stretch with its climbs and descents (route legs, four by default). Goals and arrival need a point you can reach.
  Visited minimap rooms count as explored ground.
- **Breakables** have their own scanner category.
- **Mod button layout**: Square says life, mana, level and experience; Triangle says gold and progress; Circle
  repeats the scanner entry.
- **Life and mana**: separate options for each. Life can come first: it cuts any other speech, and nothing cuts it for
  2 seconds. The steps are configurable. There is an optional Diablo IV low-life warning and heartbeat.
- **Shrines**: their name and effect are said after you use one.

Changed and fixed:

- Pickup lines (gold, potions, the targeted item) cut each other and give way to life, mana and combat.
- A lone dialogue response is repeated on a direction push only when it is the only response.
- In the inventory, left and right follow the list order.

## 10 October 2026, evening — version 0.2.0

From the first report by a player outside the project:

- The character name field echoes each typed character.
- A lone dialogue response is read again when you push a direction. A new option silences what NPCs say, for players
  who hear the game's voices.
- Screen-reader keys such as Ctrl no longer skip the intro movie.
- "Entering…" and "Portaling to…" transition pop-ups are read.
- Character sheet: attributes, resistances and defences are read on opening and with the details input.
- Details requests no longer repeat the list position, and list positions can be turned off. Skill and passive
  details include the status-effect explanations, and skills give their level and progress.
- The inventory moves by row and column over the items as they lie in the grid.
- Scanner: no false "higher level". Towns are fully routable. Shrines and gold piles are listed when on screen.
  Loot, landmarks and enemies can be heard up to the edge of the screen.

## 10 October 2026, morning — version 0.1.0 (first public test release)

- The world map says where the active main quest is.
- Language and region dropdowns name their options. Mod speech follows a language change in the game's settings
  at once.
- On controllers other than DualSense and DualShock 4, L3 is the mod button.
- The README is rewritten as player documentation, with a direct download link.

## 9 October 2026 — version 0.1.0

- Mod speech in all ten game languages: French, German, Brazilian Portuguese, Polish, Russian, Korean, Simplified
  Chinese and Japanese were added.
- World map:
  - The list holds everything the map shows: markers, icons, and End of Time islands with their blessings.
  - With the quests hidden, the D-pad walks place to place, region by region. Eras are spoken on LB/RB.
  - The epoch mastery panel (Knowledge of Orobyss) is read.
- Radar openings are dynamic: a gap past a wall's edge into space that goes on, from 3 metres.
- Rift-beast caves: the scanner reads only the cave, and its entrance and exits are named.
- Audio levels:
  - Rune, glyph and shard sounds are louder, and each loot kind keeps a voice in a pile.
  - Shrines are recognised more reliably.
  - Equipment sounds are quieter and the radar is louder.
- An online sync no longer re-reads the focused inventory item.

## 8 October 2026 — version 0.1.0

- Exploration is saved online too. The inventory no longer jumps to the last item. Crafting materials, gifting, the
  game guide and stash tabs are read.
- Scanner:
  - It lists every enemy on screen, and "corpse" becomes "last death".
  - It moved to Create + right stick, and minion orders are spoken.
  - Targets on another floor say so, and areas name their era.
- Gold pickups are spoken with their amount.
- Endgame panels are read: dungeon and arena tiers, votes, blessings, echo rewards, Sanctuary and Monolith tiers.
  Faction, vendor, item-service, time beast, loot-filter and mastery-choice panels too. Also the Weaver tree, the
  Forgotten Knights timelines, Observatory prophecies and lenses, Bazaar listings, the stall and listing form, the
  Ladder, the Morditas ritual buttons and the cosmetics panel.
- Level-ups and new points are announced, with a status report on Create+Circle or Ctrl+L.
- The physical keyboard types into the screen keyboard from the first key.
- Offline equipped skills are read again. Other players say their level and a friend's character name.
- Skill tree and passive nodes say what they require, their route and what they lead to.
- Quest updates speak online again, and a co-op partner is no longer named over and over.
- Sounds:
  - Ground loot sounds by rarity and type.
  - Shrines, time rifts and the Nemesis get their own sounds and names.
  - Notes and lore records have their own sound.
- Rare and champion enemies are treated as elites. Equipped idols can be reached and are read.
- Radar: an opening means a passage you can walk into.

## 6 October 2026 — version 0.1.0

- Held tooltip modes, the forge from the inventory and the Social panel are read.
- Co-op: your own character is read after joining another player, and the party overview is read.

## 5 October 2026 — version 0.1.0

- Skills are modified through their trees, and passive trees follow the same rules. The mastery banner is a list.
- Gold is said with Create+Triangle or Ctrl+G, when the inventory or a vendor opens, and after trades.

## 4 October 2026 — version 0.1.0

- Exploration is saved per character, and NPCs from every loaded part of a map are listed.
- The scanner gains unexplored areas, categories in the style of the Diablo 2 mod, new-place finds and room sounds.
- List navigation for passive trees, skill trees, the backpack and merchants, and grid navigation for the Skills panel
  and trees.
- Fixes: silence after map travel, passive trees, focus after dropping an item, item comparison, cut-off NPC lines.
- Packaged for game version 1.5.1.2.

## 3 October 2026 — version 0.1.0

- The exploration scanner, the Create button menu and the explored-route beacon replace automatic orientation.
- Sounds:
  - Enemy presence cues in the style of the Diablo 2 mod.
  - World objects play continuous loops, and portals, waypoints, exits and the stash can be heard from afar.
  - Gold, potions, tomes, portals and exits use Last Epoch's own recordings.
- Boss and miniboss life is narrated. There is an on-demand enemy scan. Potion charges are announced. The vitals
  report lists harmful effects and ailments.
- Reading:
  - Controller button names are spoken in the player's language.
  - Changes to settings values are spoken.
  - Character tiles, character-sheet stat lines, and world-map waypoints and regions are read.
- Pop-ups are read once on opening, buttons that show only an icon are named, and the offline Play button is read
  again.
- Fixes for crafting controls and timed notices. Speech that the screen reader rejected is recovered.
- Less repetition: mana changes, and names on range edges and after alt-tab.

## 2 October 2026 — version 0.1.0

- Port to Last Epoch 1.5 on Steam (BepInEx 6 IL2CPP), with wider coverage: more screens read, character name
  editing, skill readiness and map feedback.

## 31 January 2026 — version 0.1.0

- Class descriptions are read on submit in mastery selection.

## 30 January 2026 — version 0.1.0

- Menu reading in layers. State changes of special toggles are read.
- The class selection carousel and mastery and class descriptions are read.

## 29 January 2026 — version 0.1.0

- First version: menu reading with labels, values and descriptions.
