# Social panel reading (2026-10-06)

User goal: play online with a friend by ear: find one's own username to share, invite the friend to a
party or add them as a friend, and accept a party invitation. Static analysis only (build 25711439,
game 1.5.1.2); nothing below has been heard in game yet.

## Structure (prefab and scene evidence)

- Panel prefab `assets_9bbbbadcc6fcebb8.bundle` (`LE.UI.PanelSystem.SocialPanel`). Root `Social` carries
  `FriendPostRequestsUI`, `SocialPanelUI` and the `UnityDrivenNavigable` SOCIAL_PANEL.
  `SocialPanelNavigable` is unused and its methods share a native no-op stub: never patch it.
- Friends tab: a `ListNavigable` (`SocialPanelFriendsPanel`) with five sections. Each section is a
  `Header` button holding a caption and a `Toggle` (expanded = `isOn`) above its panel; the five
  toggle/panel pairs are `FriendPostRequestsUI` fields (`onlineFriendToggle`/`onlinePlayersPanel`, …).
  Collapsing (`toggleOnlineFriends` 0x17c03d0) deactivates every child row but keeps it.
- Rows `SocialPanelIndividualPlayer*(Clone)` carry `SocialPanelPlayerRef` (`socialPlayerName`,
  `socialCharacterName` "Pj - Nivel 84", `socialCharacterLocation`, `socialStatusMsg`) and the
  selectable `SocialPanelPlayerSelectableAdapter`. Offline, request and sent rows fill only the name.
  `SetLoadingState` (0x26e6ea0) swaps `loadingStateObj`/`contentObj`; the request prefab has those
  two references swapped, so "loading" is read as "the name label is not visible".
- Request rows: `AcceptButton` (caption "Aceptar") and `DenyButton` (icon only).
- Party tab: `SocialPanelPartyPanel` (`ListNavigable`) → `SocialPartyPanel` with
  `regionCategoryLabel` ("Región de grupo"/"REGIÓN PERSONAL") and `RegionDropdownSelector`
  (`RegionSelectionDropdown` + `RegionSelectionHelper.textRegion/textPing`; its `DropdownNavigable`
  highlights the child `Textfield`, which caused a second identical announcement). Members are
  `SocialPartyMemberPanel` (`accountNameText`, `characterNameText`, `characterLevelText`,
  `locationText`, `statusText`, `leaderIcon`).
- Status: `SocialPanelStatusDropdown` (`ChangePlayerStatus.statusDropdown`); the user's own username
  is `FriendPostRequestsUI.socialTitle` (`PlayerName`) beside it. Tabs: `TabUIElement`, selected when
  `controller.SelectedTabId == elementID`.

## Windows

- `partyInviteMenu` (`SendPartyInviteWindow`, `StringInputPanel.StringInputText`), `addFriendMenu`
  (`AddFriendInputField.inputField`, explanation label inside the field) and `blockUserMenu`
  (`BlockPlayerInputField.inputField`), all under `Social`. `togglePartyInviteMenu` (0x17c0870) and
  `toggleAddFriendMenu` (0x17bf410) clear the field and toggle the window; Enter is ignored in
  controller mode, the window's button submits.
- Player options: `GUI/Canvas/SocialRightClickMenuAlt` (`RightClickMenu.title` = display name;
  buttons `RightClickBtnRef` built at runtime). `DisableButton` (0x26b6fb0) makes the button
  non-interactable, enables its `GeneralTooltop` and stores the reason key in `bodyKey`.
- Party invitation: `GUI/Canvas (animated)/Player Cards Panel/Party Invite Confirmation Prompt`, the
  only `ConfirmationPanel` under `UIBase.PartyOverviewController` (header = inviter, body
  "te ha invitado a un grupo.", Aceptar/Rechazar). The mod's pop-up hooks never covered it.

## Feedback

- Party/friend results are system chat lines posted through
  `SystemMessageService.SubmitSystemMessage(string)` (0x1c9ec80; ISystemMessageService slot 0),
  e.g. `PlayerSync.SendPartyInvite` (0x139fe50) posts name + " " + `Chat_SuccessMessage_InviteSend`;
  `SendFriendRequestWithID` posts name + `Chat_SuccessMessage_FriendRequest` or
  `Chat_ErrorMessage_FriendRequest`. Prologue: no relative branch in the first 16 bytes (same shape
  as the live `SubmitNpcMessage` hook).
- The invite window (`PartyController.<OnInviteConfirmButtonPressed>d__13.MoveNext` 0x1faf1e0) on a
  failed `TryGetUserIdentityByDisplayName` only calls `Debug.LogError(UserServiceError)`, whose
  `ToString` (0x1d14d50) is "User service error: {Type} - {Message}", and leaves the window open.
  The right-click invite path (`PlayerSync.<SendPartyInvite>d__64`) also posts
  `Chat_ErrorMessage_FailedInvite`.
- Focusing a TMP input field in controller mode opens the on-screen keyboard natively
  (`ScreenKeyboardManager.Update` 0x1082720 → `set_SelectedGameObject` 0x1083110 →
  `ShouldBeEnabled` 0x1082c30). At the user's request (2026-10-06) the character-name guard
  (`CharacterNameKeyboard`) now also covers the three username fields: the keyboard opens only on
  Cross, and the physical-typing transfer applies ([guard](character-name-reading.md)).

## Mod behaviour

`Social/SocialReader` (called from `ElementAnnouncer.BuildAnnouncementText` after the filter-range
reader), `SocialWindows`, `SocialText` (wording), `SocialAccessibility` (polled service),
`SocialChatLines`, `SocialLog`; hook `Patches/SocialPatches.cs`.

| Element | Example (Spanish) |
|---|---|
| Friend row | "Bea#5678. Pj - Nivel 84. Fin de los tiempos. Amigos en línea"; loading: "Cargando. Amigos en línea" |
| Party member | "Ana#1234. Pj - Nivel 90. Ciudad. Líder del grupo" |
| Section header | "Amigos en línea. Expandida. 3 jugadores"; toggling says "Contraída. 3 jugadores" |
| Tab | "amigos, pestaña seleccionada" / "grupo, pestaña" |
| Status | "Estado: Ausente. Tu nombre de usuario: Ana#1234" |
| Region | "Región de grupo: Sudamérica, ping 25ms" (the Textfield maps to the dropdown: one stop) |
| Username field | "Nombre de usuario para invitar, formato Nombre#1234. Campo de texto. Vacío. Cruz para escribir" |
| Window buttons | "Enviar invitación, botón"; textless close: "Cerrar, botón"; request deny: "Rechazar solicitud, botón" |
| Options | lead "Opciones de Bea#5678"; disabled: "Portal al jugador (No disponible). <game reason>" |
| Invitation | "Bea#5678 te ha invitado a un grupo. Aceptar o Rechazar", once per opening |

- Openings: the first announcement inside a newly visible window leads with its own labels
  (title, explanation) and "El nombre de usuario tiene el formato Nombre#1234"; options lead with
  "Opciones de …". Introduced windows are forgotten when they hide.
- Results: social chat lines are matched against the game's localized templates (37 keys from the
  UI/Common tables; suffix templates after a name, `{n}` templates by pattern), logged `[Social]`,
  and lead the next Social announcement or speak alone one frame later. A "User service error: "
  log line while the invite window is open says "No se encontró ese nombre de usuario".
- Typing: while a username window is open its field is polled; typed characters, erased characters
  ("e borrado") or a replaced value are spoken. `CharacterNameAccessibility` only reads validation
  errors, so it was not reusable. The on-screen keyboard names the field with the mod label.
- The invitation prompt logs `[Social] Party invite prompt: …; focus: navigable=…, selected=…`.

## Limits and pending listening

- Whether the windows open with focus on their button or field is unheard.
- Cross, typing (screen keyboard and physical) and Circle on the username fields are unheard.
- The invite failure log line is not specific to "not found" (network errors read the same).
- Which social lines really pass through `SubmitSystemMessage` beyond the three disassembled ones.
- How a controller user reaches the invitation prompt: read the focus log line on the first test.
- Tests: `tests/LeAccess.Social.Tests` (58 checks).
