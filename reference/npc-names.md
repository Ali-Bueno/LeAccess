# Nearby NPC conversation names

The 1.5.1 service-role and authored bottom-label follow-up is documented in
[world-services.md](world-services.md). The older name-resolution evidence
below remains relevant.

Validated against installed Steam 1.5.0.1 / build 25663618 on 2026-10-02.
The reported Z22 session produces NPC cues with an unavailable name, then enters
a conversation. That log identifies the missing name but cannot associate its
runtime instance ID with a serialized scene object.

## Installed Z22 evidence

`StreamingAssets/LEAssetBundles/scene_Z22_unity.bundle` contains no scene
`NpcNameTooltip`, `BaseDisplayInformation` or `OverrideActorName` components.
Its Grael interaction has this explicit authored chain:

| Source | Serialized identity and relationship |
| --- | --- |
| `SceneObjects/Gameplay/Quests Folder/Grael/Grael` | `WorldObjectClickListener` 15148 and `RelayNPCInteraction` 15269 |
| Relay `otherInteractables` | References `ConditionHandler` 15018 on the direct child named `Grael` |
| That child's conversation trigger | `LEConversationTrigger` 15019: ConversationID 702, `useExternalDb=true`, null `conversant` |
| External database | `PermaLoad.bundle`, object 8545222662296949990, `Z Overhaul Dialogue Database` |
| Conversation 702 | Actor ID 1, Conversant ID 300 |
| Actor 300 | Name `Z_Overhaul_Grael`, `IsPlayer=False`; no Display Name field |

The listener's same-object conversation cache does not discover this child.
The database identifier is also not the native speaker display name. The shipped
shared tables contain no `Actor_Name_Z_Overhaul_Grael` entry. They do contain
`Actor_Name_Grael`, shared ID 150300, whose Spanish text is `Grael`.
The shared `Actors` object is 3077052929033708527 in
`aa/StandaloneWindows64/localization-assets-shared_assets_all.bundle`; its
Spanish table object is 1254783106925213149 in the corresponding Spanish bundle.

## Native name contract

Read-only disassembly of installed `GameAssembly.dll` establishes the chain:

- `LEConversationTrigger.Start`, RVA `0x1C1AD50`, selects the explicit external
  database or the existing dialogue controller's `initialDatabase`, then
  retrieves the configured conversation ID. The reader does not invoke Start.
- `RelayNPCInteraction.OnNPCInteraction`, `0x187F5C0`, follows its explicit
  `otherInteractables` list. The reader follows the same references without
  running interactions or evaluating quest conditions.
- `ConversationStarter.StartConversation`, `0x63FE10`, selects from
  `[conversant, this.transform]`; `Tools.Select`, `0x6351E0`, returns the first
  live transform. Thus the null Grael conversant resolves to the trigger itself.
- `CharacterInfo` constructor, `0x5ED240`, calls
  `OverrideActorName.GetActorName`, `0x601180`, for the resolved transform. That
  native reader resolves overrides and database display names. Its absent-
  override route uses the configured speaker transform's name; the Grael
  trigger is named `Grael`.
- `TextMeshProSubtitleControls.ShowSubtitle`, `0x1AEFB70`, passes this
  `CharacterInfo.Name` to `LocalizationDialogue.GetActorName` at `0x1AEFCFE`.
  It does not pass the database's internal actor identifier.
- `LocalizationDialogue.GetActorName`, `0x125D680`, looks up
  `Actor_Name_` plus that display name. On lookup failure it returns its raw
  input. Proximity requires `Localization.TryGetText` success instead, so an
  internal identifier or untranslated key cannot become spoken text.

## Reader and validation

`NpcConversationNameReader` is the final fallback after existing tooltip,
actor-display and explicit-conversant name components. It reads an owned cached
or same-object trigger, plus triggers on the exact objects referenced by the
NPC relay's live `ConditionHandler` entries. It does not search unrelated
children, siblings, global actor registries or arbitrary object names.

Each trigger must resolve a nonplayer database conversant. The effective native
speaker must be active, owned by this listener or explicitly referenced without
another listener owner, and must not be the local player. Multiple active
branches must agree on database, actor ID and localized name; an unreadable or
conflicting branch leaves the name unavailable. The original sound cue remains.
External databases never silently fall back to the global database. No game
state is written, conversation started, singleton created or new hook added.

`dotnet run --project tests/LeAccess.NpcProbe.Tests
-p:CheckEolTargetFramework=false` passes **85 source-linked checks**. They cover
the actual null-cache/null-conversant Z22 structure with `Z_Overhaul_Grael` in the
database and `Grael` from the native speaker resolver, Spanish lookup, current
language changes, explicit/default speaker selection, player rejection,
foreign and ambiguous branches, missing keys, collected wrappers, errors and
preservation of the existing cue behavior. The main no-deploy build passes
with zero warnings/errors.

No game was launched. Actual approach/listening still requires a fresh run;
the existing NPC diagnostic should report source `dialogue database` when this
fallback succeeds. This source label covers the verified database identity and
native speaker name together, not direct speech of the database identifier.

## Scene lifetime follow-up (2026-10-03)

The proximity probe now requires a valid loaded active scene, a loaded local
player scene and loaded NPC listener scenes. Previously, a retained active NPC
in an unloaded scene could beat a live NPC in the registry and provide stale
speech/audio. Loaded additive and network-owned scenes remain eligible; scene
handles are not required to match the active scene.

The NPC probe suite passes 109 checks, including eight new scene-lifetime
regressions; the existing lifecycle suite passes 109 checks. The main build
with `SkipPluginDeployment=true` has zero warnings/errors. These checks use
source-linked doubles; online approach and listening remain unverified.

## Repeat gap

When the NPC target is lost (range-edge flicker), `NpcNameAnnouncementState.Lost(now)`
keeps the spoken memory. The same NPC re-entering within `RepeatGapSeconds` (5 s) stays
silent; a different NPC, a session change or a longer absence announces normally.
Menu or suspension resets still clear the memory.
