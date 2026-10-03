# LeAccess - Last Epoch Accessibility Mod

> **Shared accessibility playbook:** this mod follows the shared playbook at `D:\code\modding projects`
> (`PRINCIPLES.md` engineering ground rules + `reference/` library — PRISM integration, audio-navigation,
> UI specs, per-engine screen-reader transport). The file below holds the game-specific rules for Last Epoch.
> Progress and next steps live in this repo's `STATUS.md` — open it first.
> Latest continuation handoff: [WHATS_NEXT.md](WHATS_NEXT.md).

> **Steam 1.5 port:** current loader, API changes and runtime limits are in [STATUS.md](STATUS.md) and [reference/steam-port.md](reference/steam-port.md). Detailed API notes below include the earlier game build; verify against the replaced `last epoch code/` before adding hooks.

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
    ├── Audio/
    │   └── WallSonar.cs        # Directional wall audio cues (NAudio)
    ├── CharacterCreation/
    │   ├── ClassCarouselHandler.cs  # Class selection carousel polling
    │   └── MasteryReader.cs         # Mastery name/description extraction
    └── Utils/
        └── TextUtils.cs        # Text cleaning and utility methods
```

## Building
```bash
cd LeAccess
dotnet build
```
DLL is copied to `D:\games\steam\steamapps\common\Last Epoch\BepInEx\plugins\LeAccess\`

## Game References
- Interop DLLs: `D:\games\steam\steamapps\common\Last Epoch\BepInEx\interop\`
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

### Notification Classes
- `ControllerNotification` - Main notification popup system (title + message)
  - `instance` - Static singleton
  - `title` (TMP_Text) - Notification title
  - `text` (TMP_Text) - Notification message
  - `ShowNotification(Notification)` - Display a notification
  - `EnqueueNotification(Notification)` - Add to queue
  - `Notification` struct: `title`, `message`, `duration`
- `UIQuickNotificationText` - Quick hint/feedback messages (temporary text)
  - `_label` (TMP_Text) - The notification text
  - `Show(string text, Color? color)` - Display quick notification
  - Used for hints like "Press RT to submit"

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

### Mastery Classes
- `MasteryPanel` - Panel showing mastery options
  - `masteryButtons[]` - Array of mastery buttons
  - `masteryButtonTMPs[]` - Array of TextMeshProUGUI for button text
  - `characterClass` - The base class
- `MasteryButton` - Individual mastery button in the panel
  - `classDescription` - Description text for the mastery
  - `classDescriptionLocalizationKey` - For localization
  - `isMasteryClass` - True if mastery, false if base class
  - `passiveBonuses` - List of passive bonuses
- `MasteryButtonTooltip` - Tooltip component on mastery buttons
  - `masteryName` - Name of the mastery
  - `masteryBonuses` - List of bonus descriptions
- `Mastery` - Mastery data object
  - `name` - Internal name
  - `LocalizedName` - Localized display name
  - `localizationKey` - Key for localization
- `ActorClass` - ScriptableObject with class descriptions
  - `className` - Base class name
  - `classDescription` - Base class description
  - `subclasses[]` - Array of ActorSubclass
- `ActorSubclass` - Mastery/subclass data
  - `subClassName` - Mastery name
  - `classDescription` - Mastery description

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
- **ClassTab mastery selection**: Reads mastery names (localized) and descriptions
  - Name read on navigation (from TMP_Text children)
  - Description read on A/X submit (searches parent hierarchy for long text)
- **Popup notifications**: Announces system notifications with title and message
  - `ControllerNotification.ShowNotification` patch - "You are offline", connection messages, etc.
  - `UIQuickNotificationText.Show` patch - Quick hints like "Press RT to submit"
- **Wall sonar audio cues**: Directional tones for nearby walls during gameplay
  - Uses NAudio (NuGet) for audio generation (Unity AudioModule doesn't work for procedural audio in IL2CPP)
  - Physics.Raycast in 4 directions relative to player facing
  - Front wall: 880 Hz (high pitch), center pan
  - Back wall: 220 Hz (low pitch), center pan
  - Left wall: 440 Hz, panned left
  - Right wall: 440 Hz, panned right
  - Volume scales with proximity (closer = louder)
  - Automatically muted when UI panels are open (menus, inventory, settings)

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

### ClassTabs (Mastery Selection)
After selecting a base class, the mastery selection screen shows ClassTabs (e.g., ClassTabPaladin, ClassTabVoidKnight, ClassTabForgeGuard, ClassTabBase).

**Key Discoveries**:
- ClassTabs are Selectables (not Buttons) inside `LeftColClassMenu` parent
- They use `PrioritizedNavigable` for navigation (navigable name: "Name Creation Holder")
- `ListNavigable` does NOT have a `HandleSubmitInput` method - don't try to patch it!
- ClassTabs have localized names via TMP_Text children (e.g., "Paladín", "Caballero del vacío")

**Implementation**:
1. **Name reading** (on navigation): `TryGetClassTabInfo` in ElementAnnouncer.cs
   - Gets localized name from `GetComponentInChildren<TextMeshProUGUI>().text`
   - Fallback: Extract from GameObject name (ClassTabVoidKnight → "Void Knight")
2. **Description reading** (on A/X press): `HandleClassTabSubmit` in ElementAnnouncer.cs
   - Triggered via `PrioritizedNavigable.HandleSubmitInput` patch
   - Searches parent hierarchy for long text (>50 chars containing ".")
   - Descriptions are found at level 1-2 in parent hierarchy

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
  - [x] Class description reading (searches UI for description text)
  - [x] Mastery selection in skill tree (MasteryButton name + description)
  - [x] ClassTab mastery selection (name on navigate, description on A/X submit)
  - [ ] Localized class names (currently English only - but ClassTabs DO have localization!)
  - [ ] Existing character selection (name, level, class from CharacterData)
  - [ ] Character name input field accessibility
- [ ] Inventory item announcements (name, stats, rarity)
- [ ] Skill tree navigation
- [x] Map navigation with audio cues (WallSonar - NAudio directional tones)
- [ ] Combat feedback
- [ ] Quest log reading
- [ ] Configurable verbosity
- [ ] Audio cues for navigation boundaries

## IL2CPP Notes
- Use `.TryCast<T>()` for type casting
- Compare IL2CPP objects by `.Pointer` not reference equality
- Avoid `FindObjectOfType` every frame - cache references
- Use `GetInstanceID()` to track object identity
- **IMPORTANT**: `Selectable.OnSubmit` does NOT exist in IL2CPP - use navigable `HandleSubmitInput` patches instead
- **IMPORTANT**: `ListNavigable` does NOT have `HandleSubmitInput` - it inherits from PrioritizedNavigable but doesn't override it
- When patching methods, check they exist first - a failed patch will prevent the entire plugin from loading

## Screen Reader (Tolk)
- Namespace: `DavyKager`
- Methods: `Tolk.Load()`, `Tolk.Output(text, interrupt)`, `Tolk.Silence()`, `Tolk.DetectScreenReader()`
- Supports: NVDA, JAWS, Window-Eyes, SuperNova, SAPI fallback

## GitHub
Repository: https://github.com/Ali-Bueno/LeAccess
