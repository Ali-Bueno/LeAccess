# Settings focus and row labels — Steam 1.5.1

Inspected 2026-10-02 against installed build 25672295 without launching the game.
The session ending 19:40:15 UTC reaches Gameplay, Graphics, Sound and Social
settings, but `[Element]` mainly records dropdown/toggle controls. The user's
report of value-only speech and a silent tab has two separate native structures.

## Row ownership

Current `PermaLoad.bundle` places each settings title in the row's sibling
`Input Labels/Label`; the selected dropdown's own `Label` is only its value.
Examples under `Settings Panel/Content/Graphics Basic/viewport/GraphicsOptions`:

| Row | Typed setting component | Declared row owner / label |
|---|---:|---|
| Shadow Quality | `EnumSettingUI` 863976713528546392 | `_settingContainer` GO 2686893703631393415; title TMP 339848121705246391 |
| Master Quality | `EnumSettingUI` 3516898873017974175 | Its exact row, title TMP 878758532352213919 |
| Monitor | `StringDropdownSettingUI` 3713444865684217775 | Its exact row, title TMP 4946017766628510867 |
| Screen Resolution | `StringDropdownSettingUI` 1196434466420004589 | Its exact row, title TMP 6265524291420677845 |

Shadow Quality's title localizes through table `UI`, key
`Settings_Panel_Graphics_Advanced_Dropdown_ShadowQuality_Title`; sibling
`Input Labels/Sublabel` TMP 1711527339809697852 has the explanation key ending
`_Explanation`. The dropdown template contains additional inactive controls and
option text: counting those as separate current controls can incorrectly reject
the declared one-row container.

Sound's `Slider - Master/Slider Input` contains `Slider` 7872361821270831823,
`SliderInput` 7816439765113286854 and `NumericSettingUI` 8980000023172487381.
Its child `Number Float Input` separately contains `TMP_InputField`
4828405761118149756. Native selectable index 1 is the slider rectangle
5427127435563879211; index 2 is that editor rectangle 4928343740590981198.
Thus an arbitrary descendant input field does not establish that the editor,
rather than the enclosing slider, currently owns focus.

## Authoritative focus and tabs

`SettingsPanelTabNavigable` implements native navigation through a MonoBehaviour,
not `PrioritizedNavigable`. The small read-only `SettingsFocusReader` captures
the exact current `NavigationManager.currentlySelected` owner and returns
`SettingsFocusSnapshot(OwnerId, TabId, Selected, TabName, TabSummary)`.

Native `FocusSelectable` RVA `0x1064C80` indexes `_selectables` with
`_currentlySelectedIdx` and passes that RectTransform to `ControllerFocusCursor`.
`UpdateSelectableComponents` RVA `0x1066510` populates the parallel cache using
direct `Component.TryGetComponent` calls, never descendants. The reader requires
matching array lengths, valid bounds, live objects, exact shared GameObject and
visible ownership within the current tab. It accepts non-Selectable components
without synthesizing navigation. A missing or late cache yields `Selected=null`
and can recover on the next read. `OnEnable` RVA `0x1065E30` resets the index and
registers this navigable independently of whether any control can be focused.

The installed `SettingsPanelTabUIController` 4685189516411315978 has
`m_Enabled=0` and an empty `tabContents` list. Its referenced `SettingsUIManager`
1229932938368442466 owns the five actual content objects. Native `Select` RVA
`0x17E3130` dispatches element IDs 0–4 to Gameplay, Graphics, Sound, Social and
Challenges respectively. The reader follows that verified mapping, requires the
exact current manager/content association, and reads the selected owned tab's
visible localized TMP title. It does not require the data-holding controller's
enabled flag. Hidden, stale, foreign and duplicate tab associations fail closed.

## Challenges without selectable controls

Challenges navigable 7821522917384828048 contains only two selectable rectangles:
8071061902075662141 and 7635214100180158796, both Disable buttons. Native
`CharacterChallengeSetting.OnEnable` RVA `0x1593410` toggles its declared enabled
state, disabled state and abandon-button objects according to the current
challenge. Both buttons can therefore be absent while the tab's state text is
visible. This is a concrete explanation for a content-bearing tab with no focus
announcement, although the log does not identify which tab the user meant.

`SettingsChallengeReader` reads only active settings whose `navigable` matches
that exact owner. It pairs the visible owned `challengeNameText` with the first
line of the sole visible enabled/disabled state. The state's longer explanation
after a rich-text break or newline is omitted from the brief navigation summary.
Both states visible, both hidden, foreign labels and ambiguous disabled text are
rejected. No challenge query, setting change or navigation method is invoked.

Full filtered asset records are in
`%TEMP%/LeAccess-settings-25672295-20261002/permaload-settings.json`; native
excerpts are beside it. The source-linked `LeAccess.SettingsFocus.Tests` suite
passes **63 checks**, including late caches, non-Selectable focus, exact tab
ownership, the disabled controller, tab-only snapshots and visible challenge
states. Main build with `SkipPluginDeployment=true` has zero warnings/errors.
The helper contains no speech or hooks; the menu integration owns announcement
timing. In-game listening, including which previously silent tab was affected,
remains to be confirmed.

## Menu integration and values

`TextDetector` follows EnumSettingUI and StringDropdownSettingUI's declared
row as well as the previous setting types. An inactive dropdown template no
longer prevents finding the label beside the control. Descriptions remain manual.

`SettingsFocusAnnouncer` combines a changed tab's title/brief summary with its
first control in one announcement; unchanged context stays silent. Read-only
polling refreshes selected values even when no cursor event occurs. The existing
toggle callback yields to this exact settings selection to avoid double speech.
Modal dialogue, loading and lost focus suppress settings and renew context on
return. Right-stick-up reads current descriptions; Challenges also exposes its
complete visible explanations manually when neither Disable button is active.

For sound settings, a SliderInput owns both its Slider and a NumberFloatInput
editor, and each has a distinct controller index. Focusing the whole slider
must not be interpreted as editing its descendant TMP_InputField. The displayed
numeric text supplies its native percent/decimal formatting; it must belong to
the slider's explicit number-input reference. The editor's own focus also retains
the setting name. No value setter or navigation method is called.

Final focused validation: 63 settings-source checks, 73 menu lifecycle/reading
checks and 79 shared input-binding checks. Production build is clean. No new hook
or dependency; audible behavior still needs the next user test.
