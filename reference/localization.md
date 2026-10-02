# Mod speech localization

Implemented and deployed on 2026-10-02 for Steam 1.5.0.1. English and Spanish
cover all current mod-owned speech keys. Game labels, names, dialogue, descriptions
and configured enemy announcement text remain supplied by the game or user.
Log messages and configuration identifiers are not speech translations.

## Language selection

The next launch creates this setting in `BepInEx/config/LeAccess.cfg`:

```ini
[Localization]
Language = auto
```

`auto` follows the game's text language. Set `en` or `es` to override the mod's
own messages; this does not change the game's language. Additional installed
culture codes are accepted. Configuration edits apply on restart or an existing
BepInEx config reload; translation-file edits require restart.

`GameLocale` reads `Localization._initialized` and `Localization.Locale` once
per existing `LateUpdate`, without new hooks or scene searches. Before the game
localization is ready it uses English; a later language change updates the mod
and refreshes normal focused-control speech. Transient API exceptions retain the
last valid language. Standard locale strings are used directly; nonstandard game
names can resolve through `LocaleNames.LanguageKeys` and its named `LanguageKey`
values. All APIs are in the existing `LE.dll` reference.

This follows D2R Access's game-text-language selection and English fallback
policy. It uses Last Epoch's direct locale API instead of D2R's translated-text
probe. Actual game locale strings and audible switching still need a live test.

## Translation files

Installed files are beside the plugin:

```text
BepInEx/plugins/LeAccess/Translations/en.json
BepInEx/plugins/LeAccess/Translations/es.json
```

Source catalogs live in `LeAccess/Translations/`. They are also embedded in the
DLL, so deleting or damaging an external file cannot remove bundled speech.
Build/deployment copies missing catalogs and preserves existing user edits.

To add a language, copy `en.json` to a culture-code filename such as `fr.json`
and translate the values. Use UTF-8 JSON; UTF-8 BOM is supported. Preserve keys
and all numbered placeholders (`{0}`, `{1}`), which can change order. For example:

```json
{
  "control.empty_field": "{0} : vide",
  "inventory.empty_slot": "Emplacement vide"
}
```

Regional catalogs fall back to their parent language, then English. A partial
external Spanish file overrides only its entries and retains the bundled Spanish
for other keys. Empty values, wrong JSON types, unknown keys and malformed or
incomplete placeholders are logged and ignored. Repeated lookups do not reread
files or repeat warnings. Unsupported languages use English for missing keys.
Player-entered text is passed as a format argument, never interpreted as a template.

## Coverage and validation

Localized messages: plugin startup, on/off, color selector, input field label
and empty state, disabled options, screen keyboard, controller menu title,
empty inventory slots, node allocated points/requirement states, quest
completion/pin states, life/mana percentages and availability, and the nearby
landmark prefix and the authored bridge-section names. Translation occurs at these producers, before the existing
central speech sink; arbitrary game strings are never used as translation keys.

The source-linked localization suite now passes **136 checks**, including real
catalog loading and the game/config adapter with host doubles. See `STATUS.md`
for the current integrated validation totals. The main build/deployment
passes with zero warnings/errors, and installed/built DLL hashes match.
Staged deployment also verified preservation of a user-edited English catalog
while copying a missing Spanish catalog. No game or audio device was launched.

Tomorrow: start with Spanish selected, check a toggle, an empty inventory slot
and an empty name field; open the radial menu with a neutral stick. Switch the
game to English and repeat, then return to Spanish. Confirm game-supplied names
remain unchanged by an explicit mod-language override. Broader follow-up coverage
is recorded in [menu](menu-reading-audit.md) and [tooltip](tooltip-reading-audit.md)
audits, with links to the subsequent implementations.
