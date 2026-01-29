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

## Current Implementation

### Working
- Screen reader initialization (Tolk/NVDA)
- Panel open/close announcements
- ControllerFocusCursor.Show patch - announces when highlight moves
- MenuAccessibility monitors ActiveSelectable changes
- Virtual mouse disabled for discrete navigation

### Known Issues
- Some options only read the value (on/off/medium) without the option name
  - Need to traverse up the hierarchy to find the label
  - Settings controls often have label as sibling, not child
- Popups (like offline mode confirmation) may not be fully read

## TODO
- [ ] Fix option labels - find label from parent/sibling elements
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
