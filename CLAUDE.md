# LeAccess - Last Epoch Accessibility Mod

## Project Overview
BepInEx 6 IL2CPP plugin that adds accessibility features to Last Epoch for screen reader users.

## Project Structure
```
LeAccess/
├── Plugin.cs                    # Entry point, initializes screen reader and patches
├── LeAccess.csproj             # Project file with references
└── Accessibility/
    ├── ScreenReader.cs         # Tolk wrapper (namespace: DavyKager)
    ├── MenuAccessibility.cs    # Monitors NavigationManager for selection changes
    └── UIPatches.cs            # Harmony patches for UI events
```

## Building
```bash
cd LeAccess
dotnet build
```
DLL is copied to `D:\games\Last Epoch\BepInEx\plugins\LeAccess\`

## Game References
- Interop DLLs: `D:\games\Last Epoch\BepInEx\interop\`
- Main assembly: `LE.dll`, `LE.UI.Controls.dll`
- Decompiled code reference: `last epoch code/` (not in repo)
- Tolk DLLs: `references/tolk/` (not in repo)

## Key Game Classes
- `NavigationManager` - Static manager, `currentlySelected` is the active INavigable
- `PrioritizedNavigable` - Base class with `ActiveSelectable` property
- `VerticalNavigable` - Menu lists, has `selectables[]` and `currentlySelectedIdx`
- `ListNavigable` - Inherits PrioritizedNavigable, has `_selectableChildren` and `_lastSelectedIndex`
- `ControllerFocusCursor` - Visual highlight cursor, `Show()` when highlight moves
- `UIPanel` - Base for UI panels, `Open()`/`Close()` events
- `EpochInputManager` - Input system, `SetActiveVirtualMouse()` to disable virtual cursor
- `InfoForTooltip` - Tooltip component with `SetText()` and `tooltipInfo` (nameText, mainText)
- `ToggleOption` / `SliderOption` - GraphicsOption subclasses with `displayName` property

## Current Implementation

### Working
- Screen reader initialization (Tolk/NVDA)
- Panel open/close announcements
- ControllerFocusCursor.Show patch - announces when highlight moves
- MenuAccessibility monitors ActiveSelectable changes
- Virtual mouse disabled for discrete navigation
- **Menu options reading**: Label, value, and description for most options
- **Dropdown expanded items**: Reads individual items when navigating dropdown lists
- **Buttons**: Read button text directly

### Menu Reading Strategy (UIPatches.cs)
The menu reading uses a multi-layer approach:

1. **Layer 1 - InfoForTooltip**: Check for tooltip component, call `SetText()` to get `nameText` (label) and `mainText` (description)
2. **Layer 2 - GraphicsOption**: Check for `ToggleOption`/`SliderOption` components which have `displayName`
3. **Layer 3 - FindLabelInImmediateContainer**: Search siblings in immediate parent for label/description
4. **Layer 3.5 - FindLabelAndDescriptionAggressive**: Use `FindSingleOptionContainer` to find the option's container (has exactly 1 control) and search there
5. **Layer 4 - FindAnyTextInParents**: Last resort search for any text in parent hierarchy
6. **Layer 4.5 - Toggle search**: If label found but no value, search parents for Toggle
7. **Layer 5 - GameObject name**: Use cleaned GameObject name as fallback

### Text Classification (Language-Independent)
- **Values**: Short universal words (on, off, yes, no, auto, low, medium, high, etc.)
- **Descriptions**: Detected by punctuation only (". ", ", ", "; ", ends with ".")
- **Labels**: Everything else that doesn't end with ".", "!" or "?"

### Key Discoveries
- Option containers typically have exactly 1 control (Toggle/Slider/Dropdown)
- Section containers have multiple controls - used to stop searching
- Scroll containers (`content`, `viewport`) should stop hierarchy traversal
- Labels are usually siblings BEFORE the control, descriptions AFTER
- Some options use nested structures where control is not direct child

## Known Bugs (Pending)

### Bug 1: State not updating on toggle
- **Option**: "Acercamiento automático a los enemigos fuera de alcance"
- **Issue**: Reads state as "Off" but doesn't update when toggled
- **Possible cause**: Reading cached value, need to re-read on interaction

### Bug 2: Color picker options read incorrectly
- **Options**: Color selectors for enemy bars, health, etc.
- **Issue**: Reads as "Select Color: Off" instead of option name
- **Possible cause**: Toggle detected but it's actually a color picker button

### Bug 3: Some toggles not reading state
- **Options**: "Filtro de obscenidades", "Desactivar chat", "Ocultar ventanas emergentes..."
- **Issue**: Reads label and description but not On/Off state
- **Possible cause**: Toggle is in a different location in hierarchy, not found by current search

## TODO
- [ ] Fix toggle state update on interaction (Bug 1)
- [ ] Handle color picker buttons differently from toggles (Bug 2)
- [ ] Improve toggle finding for deeply nested options (Bug 3)
- [ ] Inventory item announcements (name, stats, rarity)
- [ ] Skill tree navigation
- [ ] Map navigation with audio cues
- [ ] Combat feedback
- [ ] Quest log reading
- [ ] Configurable verbosity
- [ ] Audio cues for navigation boundaries

## IL2CPP Notes
- Use `.TryCast<T>()` for type casting
- Compare IL2CPP objects by `.Pointer` not reference equality
- Avoid `FindObjectOfType` every frame - cache references
- Use `GetInstanceID()` to track object identity

## Screen Reader (Tolk)
- Namespace: `DavyKager`
- Methods: `Tolk.Load()`, `Tolk.Output(text, interrupt)`, `Tolk.Silence()`, `Tolk.DetectScreenReader()`
- Supports: NVDA, JAWS, Window-Eyes, SuperNova, SAPI fallback

## GitHub
Repository: https://github.com/Ali-Bueno/LeAccess
