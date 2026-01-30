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
    ├── Patches/
    │   └── HarmonyPatches.cs   # All Harmony patches (UIPanel, Toggle, Focus cursor, etc.)
    ├── Announcements/
    │   ├── ElementAnnouncer.cs # Main announcement logic (AnnounceElement, BuildAnnouncementText)
    │   ├── TextDetector.cs     # Text detection strategies (FindLabel*, TryGet*)
    │   └── TextClassifier.cs   # Text classification (Label/Value/Description)
    ├── CharacterCreation/
    │   └── ClassCarouselHandler.cs  # Class selection carousel polling
    └── Utils/
        └── TextUtils.cs        # Text cleaning and utility methods
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

### Character Creation Classes
- `CharacterCreationSelection` - Selectable for class selection, extends Unity's `Selectable`
  - `characterClass` (CharacterClass) - Contains class info
  - `characterContext` - `NewCharacter` or `ExistingCharacter`
  - `positionState` - HIDDEN, IN_LINE, SELECTED, SINGLE_DISPLAY
- `CharacterClass` - ScriptableObject with class data
  - `className` (string) - Display name of the class
  - `classID` (CharacterClassID) - Enum value
  - `masteries` - Available masteries for the class
- `CharacterClassID` - Enum: Primalist, Mage, Sentinel, Acolyte, Rogue
- `CharacterData` (LE.Data namespace) - Character save data
  - `CharacterName` - Character's name
  - `Level` - Character level
  - `CharacterClass` - Class ID as int
- `SimpleCharacterInfoProvider` - Provides classID, level, subClassID

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
- **Class selection carousel**: Announces class name when navigating (Sentinel, Rogue, Mage, Primalist, Acolyte)
  - Uses ListNavigable polling, not ControllerFocusCursor
  - Currently reads English class names only (no localization)

### Menu Reading Strategy (Announcements/)
The menu reading uses a multi-layer approach (see `ElementAnnouncer.cs` and `TextDetector.cs`):

1. **Layer 1 - InfoForTooltip**: Check for tooltip component, call `SetText()` to get `nameText` (label) and `mainText` (description)
2. **Layer 2 - GraphicsOption**: Check for `ToggleOption`/`SliderOption` components which have `displayName`
3. **Layer 3 - FindLabelInImmediateContainer**: Search siblings in immediate parent for label/description
4. **Layer 3.5 - FindLabelAndDescriptionAggressive**: Use `FindSingleOptionContainer` to find the option's container (has exactly 1 control) and search there
5. **Layer 4 - FindAnyTextInParents**: Last resort search for any text in parent hierarchy
6. **Layer 4.5 - Toggle search**: If label found but no value, search parents for Toggle
7. **Layer 5 - GameObject name**: Use cleaned GameObject name as fallback

### Text Classification (TextClassifier.cs)
- **Values**: Short universal words (on, off, yes, no, auto, low, medium, high, etc.)
- **Descriptions**: Detected by punctuation only (". ", ", ", "; ", ends with ".")
- **Labels**: Everything else that doesn't end with ".", "!" or "?"

### Character Creation Reading Strategy
The character creation reading is handled before other layers:

1. **TryGetCharacterCreationText**: Check for `CharacterCreationSelection` component
   - If found, get `characterClass.className` for the class name
   - Check `characterContext` to determine if new or existing character
   - For existing characters, attempt to find name/level from text elements
2. **TryGetExistingCharacterFromHierarchy**: Fallback search for character slot containers
   - Looks for parent containers named "character", "slot", or "card"
   - Collects all text elements and combines them

### Class Selection Carousel (IMPORTANT)
The class carousel in character creation does NOT use ControllerFocusCursor for navigation.

**Key Discovery**: The carousel is managed by a `ListNavigable` named **"Select A Class Navigable"**
- Uses `_lastSelectedIndex` to track which class is currently selected
- Has 5 children (one per class): Sentinel, Rogue, Mage, Primalist, Acolyte
- The `CharacterCreationSelection` objects' positions/scales do NOT change during navigation
- Must use periodic polling in `MenuAccessibility.Update()` to detect selection changes

**Implementation** (in `ClassCarouselHandler.CheckPendingClassAnnouncement`):
1. Check if `CharacterSelect.instance.currentState == ClassSelection`
2. Find the ListNavigable with CharacterCreationSelection children
3. Get `_lastSelectedIndex` to find the selected class
4. Read class name from `characterClass.className` (English only, no localization field found)
5. Announce when selection changes

### Key Discoveries
- Option containers typically have exactly 1 control (Toggle/Slider/Dropdown)
- Section containers have multiple controls - used to stop searching
- Scroll containers (`content`, `viewport`) should stop hierarchy traversal
- Labels are usually siblings BEFORE the control, descriptions AFTER
- Some options use nested structures where control is not direct child

## Recently Fixed Bugs

### Bug 1: State not updating on toggle (FIXED)
- **Solution**: Added `Toggle.isOn` setter Harmony patch that detects state changes and re-announces "On"/"Off"
- **Implementation**: `HarmonyPatches.ToggleIsOnSetterPatch` tracks previous states and announces when focused toggle changes

### Bug 2: Color picker options read incorrectly (FIXED)
- **Solution**: Detect `ColorPickerManager` and `RuleColorPicker` components before treating as toggle
- **Implementation**: Elements with color picker components show "Color selector" instead of "On"/"Off"

### Bug 3: Some toggles not reading state (FIXED)
- **Solution**: Improved toggle search using `FindSingleOptionContainer` and extended parent search to 6 levels
- **Implementation**: Also filters out toggles that belong to color pickers

### Bug 4: Custom toggle components not announcing state (PARTIAL FIX)
- **Issue**: Options like "Move to Attack" and "Hide unknown social requests" use custom toggle components (`ActiveWhileMoveToAttackEnabled`, `ActiveWhileFriendAddNotificationEnabled`) instead of Unity's Toggle
- **Solution**: Track these components when focused and detect state changes on subsequent focus events
- **Implementation**: State change detection in `ElementAnnouncer.AnnounceElement` compares current vs tracked state and announces "On"/"Off"
- **Note**: Works but timing can be inconsistent - state change is detected when focus cursor re-triggers

## Custom Toggle Components
These game-specific components don't use Unity's Toggle system:
- `ActiveWhileMoveToAttackEnabled` - has `moveAttackEnabled` bool field
- `ActiveWhileFriendAddNotificationEnabled` - has `hideUnknownSocialRequests` bool field
- `ChatToggleField` - has `IsActive` property (used in chat settings)

## TODO
- [x] **Character creation menu accessibility** - Class selection carousel working
  - [x] Class selection carousel (uses ListNavigable polling)
  - [ ] Localized class names (currently English only)
  - [ ] Existing character selection (name, level, class from CharacterData)
  - [ ] Character name input field accessibility
  - [ ] Mastery selection when creating character
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
