# Mod speech localization

Implemented and deployed on 2026-10-02 for Steam 1.5.0.1 (English and Spanish).
2026-10-09: catalogs for every other text language the game ships — French, German,
Brazilian Portuguese, Polish, Russian, Korean, Simplified Chinese and Japanese — so
all ten game languages cover every mod-owned speech key. Game labels, names, dialogue,
descriptions and configured enemy announcement text remain supplied by the game or user.
Log messages and configuration identifiers are not speech translations.

## Language selection

The next launch creates this setting in `BepInEx/config/LeAccess.cfg`:

```ini
[Localization]
Language = auto
```

`auto` follows the game's text language. Set a catalog code (`en`, `es`, `fr`, `de`,
`pt`, `pl`, `ru`, `ko`, `zh`, `ja`) to override the mod's own messages; this does not change the game's language. Additional installed
culture codes are accepted. Configuration edits apply on restart or an existing
BepInEx config reload; translation-file edits require restart.

`GameLocale` reads `Localization._initialized` and `Localization.Locale` once
per existing `LateUpdate`, without new hooks or scene searches. Before the game
localization is ready it uses English; a later language change updates the mod
and refreshes normal focused-control speech. Transient API exceptions retain the
last valid language. Standard locale strings are used directly; nonstandard game
names can resolve through `LocaleNames.LanguageKeys` and its named `LanguageKey`
values. All APIs are in the existing `LE.dll` reference.

The game's locale codes are those of its string-table bundles in
`StreamingAssets/aa/StandaloneWindows64/localization-string-tables-<language>(<code>)`:
`en`, `es-es`, `fr`, `de`, `pt`, `pl`, `ru`, `ko`, `zh` and `jp` (the backend's
supporter-pack texts use the same codes, with `es-ES`). `LocaleNames.LanguageKeys`
is consulted first; `jp` is not an ISO code and .NET accepts it as an unknown
culture, so it is mapped to `ja` explicitly. `es-ES` falls back to `es`.

This follows D2R Access's game-text-language selection and English fallback
policy. It uses Last Epoch's direct locale API instead of D2R's translated-text
probe. Actual game locale strings and audible switching still need a live test.

## Translation files

Installed files are beside the plugin:

```text
BepInEx/plugins/LeAccess/Translations/<code>.json   (en, es, fr, de, pt, pl, ru, ko, zh, ja)
```

Source catalogs live in `LeAccess/Translations/`. They are also embedded in the
DLL, so deleting or damaging an external file cannot remove bundled speech.
Build/deployment copies missing catalogs and preserves existing user edits.

To add a language the game does not ship, copy `en.json` to a culture-code filename
such as `it.json` and translate the values. Use UTF-8 JSON; UTF-8 BOM is supported.
Preserve keys and all numbered placeholders (`{0}`, `{1}`), which can change order.
For example:

```json
{
  "control.empty_field": "{0}: vuoto",
  "inventory.empty_slot": "Slot vuoto"
}
```

Regional catalogs fall back to their parent language, then English. A partial
external Spanish file overrides only its entries and retains the bundled Spanish
for other keys. Empty values, wrong JSON types, unknown keys and malformed or
incomplete placeholders are logged and ignored. Repeated lookups do not reread
files or repeat warnings. Unsupported languages use English for missing keys.
Player-entered text is passed as a format argument, never interpreted as a template.

## Adding or changing speech keys

Every key must exist in all ten bundled catalogs: the localization suite fails on
any missing key, unknown key or placeholder mismatch, and checks that every game
language has a catalog. Translate new keys into all of them in the same change.

Use the game's own terminology. `tools/extract_game_glossary.py` writes the game's
short string-table entries (about 52,000 rows) as one TSV with a column per game
locale; search it for the English term (keep the output outside the repository;
it is game data). Speech-specific conventions used in the catalogs:

- PlayStation names as the game's input settings spell them (`Settings_InputPanel_ActionLabel_*`);
  Japanese speaks バツ/マル/シカク/サンカク instead of the game's ×/○/□/△ glyphs, which TTS misreads.
- Text inserted through placeholders is nominative and of unknown gender: Polish and Russian
  use "Label: {0}" or apposition; counts use number-invariant forms ("Punkty: {0}", "Очки: {0}").
  Korean avoids particles directly after a placeholder.
- Portuguese is Brazilian (the game's `pt`: Cross = "Xis", Stash = "Depósito"); Chinese is Simplified.

Spanish was written and reviewed with the user. The other eight catalogs (2026-10-09)
were translated by Claude from English and the reviewed Spanish, with game terms taken
from the glossary; they have not been reviewed by native speakers.

## Coverage and validation

All mod-owned speech goes through `ModText`; translation occurs at the producers,
before the central speech sink, and arbitrary game strings are never used as
translation keys. Coverage history is in the [menu](menu-reading-audit.md) and
[tooltip](tooltip-reading-audit.md) audits.

2026-10-09: the localization suite passes 5,692 checks: every catalog loads without
warnings with complete key parity, each real game locale code (`en`, `es-ES`, `fr`, `de`,
`pt`, `pl`, `ru`, `ko`, `zh`, `jp`) selects its catalog, and the game's `LanguageKeys`
table wins over culture parsing. The package test verifies all ten catalogs in the friend
ZIP. Build/deployment installed the eight new catalogs beside the existing en/es copies.
Unheard: switch the game to French and Japanese, check a toggle, an empty inventory slot
and the controller menu, then return to Spanish.
