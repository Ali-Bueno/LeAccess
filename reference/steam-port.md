# Steam 1.5 port — 2026-10-01

## Steam hotfix and persistent repair

The user's subsequent Steam launch updated build 25646802 to **25663618**.
Its 22:14 log shows regeneration followed by the same duplicate `<>O` failure;
no plugins loaded. The earlier manual repair did not survive regeneration.

`LeAccess.InteropRepair` is now built with the main mod and deployed into
`BepInEx/patchers/LeAccess.InteropRepair`. It uses BepInEx and the .NET metadata
reader already present in the host; no extra runtime library is shipped.
The patcher constructor runs after IL2CPP generation and before Cecil opens
interop files. `Initialize` would be too late because those read handles prevent
atomic replacement. The repair preserves tokens, row order and heap data, changing
only orphan visibility and its namespace index; valid nested types are untouched.
It checks only generated `UnityEngine*.dll` files in BepInEx's resolved interop
directory. Valid files are not rewritten, and automatic game-update detection stays enabled.

The first real test through `steam.exe -applaunch 899770 --offline` automatically
repaired 16 rows in five regenerated assemblies, then loaded LeAccess and detected NVDA.
The current 43 Harmony declarations / 42 native targets still pass metadata safety checks.
The game reports version **1.5.0.1** and proceeds to AddDisplayName; subsequent real
controller focus/keyboard events reach the mod. No ToS panel appears in this session.
The user confirmed speech works. A subsequent launch needed no further repairs.
No terms acceptance or account name was supplied by the agent.

Main DLL SHA256: `6969A975F5197E9951D90834AC2CE7388E3439D7F7DAAF28C38F6CFEBF081B33`.
Preloader SHA256: `80BED23112B91BBEC7F5870D922C083B13785C264F3E296470624DD5E4976D51`.

The decompiled reference was refreshed again from this hotfix: 8,584 C# files plus
the isolated ``DataStoreList`2`` IL. `last epoch code/SOURCE.txt` records the current
input hashes and the previous reference backup in task scratch.

## Text entry investigation

The user reports losing the name/email editor with the controller screen keyboard;
physical typing succeeded for the name but the email editor still misbehaved.
A temporary read-only plugin confirmed `_enabled` becomes false, the selected
editor becomes null, and navigation returns from `ScreenKeyboardNavigable` to
`UnityDrivenNavigable:LoginUI`. This is an actual close, not just narration.

Native analysis of build 25663618 found this possible chain:

- `EpochInputManager.ControllerShouldBeActive` rejects mouse buttons, mouse movement
  in the current frame, and held/released physical keyboard keys.
- `IsControllerActive=false` changes the active device and clears navigation.
- `ScreenKeyboardManager.OnInputDeviceChange` disables the screen keyboard when
  controller mode is inactive. Its `ShouldBeEnabled` predicate also requires that mode.
- A selects a keyboard key, RT submits, and B cancels. `SubmitEvent` passes a local
  event to the editor; it does not inject global keyboard/mouse activity.

This chain is a hypothesis until a device transition is recorded during failure.
Skipping only the keyboard's `Disable` would leave global selection clearing intact.
Do not repeatedly reselect the editor or globally lock controller mode.

With explicit user permission, the game was restarted with device type/name added
to the probe. It recorded field identity, focus, navigation and text length, not
entered text. The temporary `BepInEx/plugins/LeAccessTaskProbe/Probe.dll` has since
been removed. No production keyboard patch has been installed. Diagnostic source/logs are under
`%TEMP%/LeAccess-steam-hotfix-20261001`.

The next user attempt succeeded at entering a character name with the controller.
Start followed by Continue then left character-selection navigation empty; see
[the separate pause focus investigation](pause-focus.md). No global keyboard/device
mode patch was needed to obtain that successful typing attempt.

The following records describe the initial port before this Steam hotfix.

## Installation and recovery

Target: `D:\games\steam\steamapps\common\Last Epoch`, Steam build 25646802,
Unity 6000.4.8f1, metadata 39. The game reports release 1.5. The old installation
uses Unity 6000.0.42f1/metadata 31 and BepInEx be.738. Its cache was not reused.

Before launching, shared saves, filters, graphics settings, version.txt and the
previous LeAccess configuration were backed up under
`C:\Users\ali-b\Documents\LeAccess backups\steam-migration-20261001-182231`.
Both installations share the LocalLow save directory. No old game files were removed.

Installed official [BepInEx IL2CPP win-x64 be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip),
commit 5b766a3, with pinned Cpp2IL development 1452 and Il2CppInterop 1.5.3.
Archive SHA256: `F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A`.
The project pins the same loader package and defaults to the Steam path.

Fresh generation produced 214 interop assemblies. Startup exposed duplicate orphan
`<>O` types in Unity 6.4 assemblies. The shared tool at
`D:\code\modding projects\templates\bepinex-interop-fixer` repaired 16 orphan rows
in five assemblies (CoreModule 8, Marshalling 1, Physics2D 4, Physics 1, UIElements 2).
It preserves TypeDef/MethodDef row tokens and the native-method map. A second dry run
found zero remaining repairs; loader-only startup then completed normally.

Task scratch `%TEMP%\LeAccess-steam-port-20261001` holds original generated-assembly
backups and diagnostic logs; these are not dependencies. Temporary `steam_appid.txt`
used during generation was removed.
Automatic approval review rejected recursive temporary-directory cleanup as blocked
by policy. The prior reference backup, local decompiler tool and probe source remain
in task scratch; the probe DLL is absent from the game's plugins.

## Speech and launch behavior

The old installation had Tolk and NVDA client DLLs beside its EXE. Plugin-only
deployment initially loaded Tolk but detected no reader, even though NVDA was running.
Direct controller testing returned success; preloading the bundled client let Tolk
detect NVDA. `ScreenReader` now explicitly loads the existing client by absolute
plugin-local path before Tolk, then frees its handle after Tolk shutdown.
No new speech transport or dependency was introduced.
The final startup confirms `Tolk initialized. Detected: NVDA` and one complete
`[ScreenContent]` announcement of the visible Spanish terms document.

Season 5 moved offline access to the initial game menu. Launch normally and choose
Play Offline; do not invent a replacement argument for the old BAT's `--offline`.
The new `LandingZonePanel` contains a legacy-offline-argument notice, matching the
[official 1.5 notes](https://lastepoch.com/1-5/patchnotes/).
The legacy flag can still parse; our `Offline Mode: False` startup alone does not
prove the parser removed it. The new `Last Epoch Accessibility.bat` launches normally.

First-run ToS 1.5.0 requires user acceptance and blocks character loading. A pending
question asks who will accept. No acceptance, character creation/deletion, point
allocation or purchase has been performed.

## Replaced decompiled reference

`last epoch code/` was replaced, not merged, from fresh LE.dll plus LE.UI.Controls.dll
and DialogueSystem.dll: 8,584 C# files. These are generated interop wrappers and metadata,
not recovered native algorithm bodies. ILSpy 11.0 and 11.1 both failed to render generic
`LE.Services.DataStoreList`; its IL is preserved in `LE.Services/DataStoreList.il`.
Supporting assemblies are under `_assemblies/`; provenance is in `SOURCE.txt`.

## Compatibility and accessibility changes

- SceneHandle requires explicit integer conversion when composing session IDs.
- Ground targeting uses `EpochInputManager.instance.InteractableTargeting.ActiveTarget`.
- Skills use `SkillsPanelManager.CurrentlyOpenTree`, `UITooltipAbility.skillDefinition`
  and `PromptsInfoProvider.Instance`; actual unlock requirements remain separate from type.
- Item tooltips come from `UIBase.instance.TooltipSystem`. V2 ownership checks current
  item, opener, bound info, active rectangle and readiness. Ordered primary sections,
  including off-viewport expanded stats, are read; comparisons and hints are excluded.
- Removed obsolete virtual-mouse, custom-toggle and missing inherited submit/disable hooks.
  Parameterless prompt update aliases another native body and remains unhooked.
- New settings use `LE.Settings` containers. Text lookup rejects hidden, culled and
  neighboring-control text. Navigation interrupts; initial modal focus follows its body.
  Unrelated panel initialization/closure cannot reset current speech.
- Ordinary `PrioritizedNavigable.ActiveSelectable` changes are polled where cursor
  hooks do not fire. Typed controller, inventory and skill readers retain ownership.
- Notifications follow the actual displayed widget. ToS reads its actual title and
  selected localized document; the hooks do not scroll or accept it.
- QuestPing removed. See [radar/world audio details](navigation-radar.md).

## Validation boundaries

All production Harmony declarations are checked against fresh metadata for existence,
overload/argument agreement and shared native bodies: 43 declarations/42 distinct targets,
26 regression assertions. Startup reaches the original's ToS panel without a crash.
The game logs an `EpochMasteryUI.ShouldBeActive` initialization exception but continues
to Login; this is separate from mod stack traces.

A temporary read-only probe inspected the inactive real SkillsPanelManager:
`selectables` is `UnityEngine.Object[,]`, rank 2, dimensions 11×4, and native GetValue
works at startup. Cells are empty before player loading, so this does not validate
real selected skills. The probe was removed from delivered plugins.

The earlier failed skill update produced only a NullReferenceException message in the
old log. The new reader logs full stacks. Current 63 host checks cover selection,
ownership, binding and speech ordering; they cannot establish gameplay success.
After ToS acceptance, inspect `[Skill]`/`Skill feedback unavailable` while navigating
the grid, specialization slots/nodes and actual assigned cast controls.

Source-linked tests use doubles for Unity state or managed audio buffers. They do
not prove audible quality, through-geometry detection, gold timing before auto-pickup
or all prefab coverage. Verify rapid navigation, complete item stats, popups, zone/NPC
names and each cue in original gameplay.

Normal builds deploy. `-p:SkipPluginDeployment=true` prevents copying during runtime
tests; Audio.Tests now passes this through its main-project reference automatically.

Final verification: all 18 test projects pass, totaling 1,188 checks; production build
has zero warnings/errors. Build and installed plugin share SHA256
`7857493518A08E6B33FBA04B9E64C081F50DCF16D6504997CC9463068ABF98FF`.
The final test game process was closed on the ToS screen after collecting evidence.
