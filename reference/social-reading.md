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
| Username field | "Nombre de usuario para invitar. Campo de texto. Vacío. Cruz para escribir" |
| Window buttons | "Enviar invitación, botón"; textless close: "Cerrar, botón"; request deny: "Rechazar solicitud, botón" |
| Options | lead "Opciones de Bea#5678"; disabled: "Portal al jugador (No disponible). <game reason>" |
| Invitation | "Bea#5678 te ha invitado a un grupo. Aceptar o Rechazar", once per opening |

- Openings: the first announcement inside a newly visible window leads with its own labels
  (title, explanation) and the neutral username hint ("El nombre de usuario de la cuenta de Last Epoch; si tiene número, inclúyelo: Nombre#1234"; the number is optional, "canario69" worked for a friend request and a party invite); options lead with
  "Opciones de …". Introduced windows are forgotten when they hide.
- Results: social chat lines are matched against the game's localized templates (38 keys from the
  UI/Common tables, including the gift's `Notification_Resonance_Spent`; suffix templates after a name, `{n}` templates by pattern), logged `[Social]`,
  and lead the next Social announcement or speak alone one frame later. A "User service error: "
  log line while the invite window is open says "No se encontró ese nombre de usuario".
- Typing: while a username window is open its field is polled; typed characters, erased characters
  ("e borrado") or a replaced value are spoken. `CharacterNameAccessibility` only reads validation
  errors, so it was not reusable. The on-screen keyboard names the field with the mod label.
- The invitation prompt logs `[Social] Party invite prompt: …; focus: navigable=…, selected=…`.

## Party overview (radial menu "Grupo")

Evidence (PersistentUI scene dump, build 25711439): `GUI/Canvas (animated)/Player Cards Panel/Party
Members` is a `PartyOverviewPanel` (navigable `Party Members`) with children `Member 1..4`. Each is a
`PartyOverviewMemberPanel` + `PartyOverviewSelectableAdapter` (the Selectable) whose
`_controllerCursorTarget` is its `Class` child (portrait, no Selectable): the cursor highlights
"Class", so "Member N" and "Class" are one stop (`SocialReader.Canonical` maps Class to the card).
`PortalToPlayer` is a separate `Button` (`requestPortalButton`); `PortalToUnavailable` shows the
game's "Portal unavailable". Fields: `PlayerName` (`accountNameText`, prefab text `EHG_PLAYERNAME`),
`PartyLeader` (`leaderTransform`), `HealthBar`/`WardBar` (`Image` fill), `isInDeadState`,
`playerActorSync` (set only while the member is in the same scene), `partyMember`
(`Identity.Username`, `IsLeader`, `characterClass` as `CharacterClassID`). Logs showed the generic
reader speaking "Member 1", "Class" and "Portal To Player".

Behaviour (`PartyOverviewReader`, wording in `PartyText`, keys `party.*`):

| Stop | Spanish |
|---|---|
| Card, same zone | "Bea#5678. Centinela. líder. vida 80 por ciento. barrera 20 por ciento. en tu zona" |
| Card, dead | "Bea#5678. Centinela. muerto" (same zone); elsewhere: "Bea#5678. Centinela. en otra zona" |
| Portal button | "Portal a Bea#5678, botón"; not interactable: "Portal a Bea#5678 (No disponible)" |

Class names go through `Localization.Common.GetClass(enum name)`; the name falls back to
`partyMember.Identity.Username` when the label still holds the placeholder. Ward is spoken only when non-zero.
Unverified at runtime: what Cross does on a card (Member 2+ carry `SocialRightClickFunctions`, so
possibly the player options), whether `fillAmount` drives the bars, and `IsInteractable` while the
portal is unavailable.

## Presence and party internals (2026-10-07, static, build 25711439)

Investigated after a co-op attempt where both friends saw each other offline and no party formed.
- Online = the friend's account ID is in the chat service's online list (`0x1c37eb0`/`0x1c38970`, account ID
  only) **and** their character summary has a non-empty name (`0x1cb6430`); a missing or unreadable summary
  (`0x1c82d90`) lists them offline. `Get Online players - Chat system not connected` comes from `0x1ca0a10`.
- The client filters presence by no region, realm, mode, solo or language. The own status dropdown is saved
  locally only (`0x26ef480`; the server call `0x1caf7c0` is a no-op), so friends only see online/offline.
- Opening the Friends tab adds one blank loading row to the online section (`0x17bacf0`); after a timer
  (`0x17be130`) it is removed when nobody is online. `CountRows` skips rows with a blank name for this reason.
- Opening Social re-queries presence when enough time has passed (`0x207b740` → `0x26c4c20`), so a failed
  query at login does not stick.
- The invite (`0x139fe50`) posts "X ha recibido la invitación a tu grupo" immediately, without a reply; only
  the backend's `PartyIDChanged` (`0x1cae480`) proves a party. Realm, mode, solo and version mismatches are
  server-side refusals with client error texts (`0x1f3cbe1`). `SHA MISMATCH` (`0x19031c0`) is log-only.

## Item gifting (2026-10-07)

User report: "the gift equipment menu doesn't work". Static analysis of Steam build **25764382** (V = verified
in disassembly or assets, I = inferred); nothing below has been heard.

- V "Regalar objeto" (party options, only for party members) runs `RightClickMenu.GiftItem` @0x26be870:
  `UIBase.openInventory` @0x1803890 (inventory on the Right), then `UIBase.OpenItemGiftingPanel` @0x17fe6f0 →
  `ItemGiftingPanel.OnOpenWithArgs` @0x2074880 → `ItemGiftingPanelUI.OnPanelOpen` @0x17a4d10 (panel in the Center).
  With gifting disabled it first shows "El regalo ha fallado / Regalar no es posible en estos momentos.".
- V The panel takes focus: `ItemGiftingPanel.OnSelected` @0x20748b0 selects its `ItemGiftPopupNavigable`
  (NavigableID 119), whose `OnSelected` @0x10511e0 picks stop 0, Confirm (stop 2 with an item on the cursor).
- V Prefab `assets_2c31067832ca1a75.bundle`, root `Item Gifting`: `_selectables` = Confirm Gift Button,
  ResonanceSlot, giftedItem (`LegendaryContainerUI`, ContainerID 60 ITEM_TO_GIFT), Close_Button. Texts: title
  `Gifts_Gifting_Header` "Regalo de objetos", recipient "Regalo a" + name (an unclosed colour tag), description
  `Gifts_Description` ("Los jugadores solo pueden recibir objetos que hayan caído mientras estaban juntos en la
  misma zona, a menos que se use una Resonancia al regalar."), `Gifts_Confirm`, `Gifts_Gifting_ResonanceRequired`
  (box hidden until needed). The resonance and close stops are icons only.
- V `HandleNavigateInput` @0x1050cb0: Up next stop, Down previous, wrapping; Left/Right nothing. Cross
  (`HandleSubmitInput` @0x1050d50) presses a button, opens the resonance pop-up (`ResonanceFlyout`, rows
  `GiftResonanceUI` with player and amount), picks the slot's item up, or on an empty slot jumps to the inventory.
  Square on the slot returns the item. In the inventory Square moves the item into the slot (`ProcessDropSubmit`
  → quick move); L2 is `TryFocusInDirection(-1)` @0x2066d60, focused location minus one: Right → Center.
- V Rules (`ValidateGiftItem` @0x14121d0): items that dropped with the recipient present, otherwise a gold or
  obsidian resonance; some uniques never. `ConfirmGiftItemButtonPress` @0x17a49f0 sends `MessageConfirmGiftItem`
  only online; no confirmation dialog, the panel stays open. The server answers with notifications ("Regalo
  enviado", "El destinatario tiene que estar en la misma zona.", "El inventario de regalos del destinatario está
  lleno.") and the chat line "Has gastado 1 {0} para regalar un objeto.".
- V The slotted resonance lives only in `<SlottedResonance>k__BackingField`, a native `Nullable<ResonanceType>`
  (`get_SlottedResonance` @0x17a59b0 returns the word at +0x70; cleared to 0 by `ClearSlottedResonance`). The
  interop wrapper throws while it is empty, so the mod reads its two bytes (hasValue, value) through the field's
  runtime offset (layout I, from `Nullable<T>`).

Why it seemed broken (I): the panel opened silently on an unavailable Confirm; nothing read the title, rule,
slot or resonance (all icons or plain labels), and the way to the item (slot → inventory → Square → L2) was
never said.

Mod behaviour (`Gifting/GiftPanelReader`, `GiftAccessibility`, wording `GiftText`, keys `gift.*`):

| Moment | Spanish |
|---|---|
| Opening (first stop) | "Regalo de objetos. Regalo a canario69. Los jugadores solo pueden recibir… Arriba y abajo recorren las opciones." + the stop |
| Confirm | "Confirmar regalo, botón. Objeto: X. 1 de 4"; unavailable: "… (No disponible). Primero pon un objeto en la ranura de regalo" or "Objeto: X. Necesitas resonancias" |
| Resonance stop | "Sin resonancia / Resonancia de oro / Resonancia de obsidiana. Cruz para elegir una resonancia. 2 de 4" |
| Slot | "Ranura de regalo vacía. Cruz va al inventario; allí, Cuadrado trae el objeto. 3 de 4" or "Ranura de regalo: X. Cuadrado lo devuelve al inventario" |
| Close | "Cerrar, botón. 4 de 4" |
| Resonance pop-up row | "Resonancia de oro. <player>. Cantidad: 3" |
| Inventory while gifting | first brief led by "Regalo de objetos abierto: Cuadrado lleva el objeto a la ranura de regalo; L2 vuelve al regalo"; after Square "X listo para regalar. L2 vuelve al regalo" or "No se movió a la ranura de regalo" |

The panel is polled (state changes on the same stop are spoken again); the manual-details gesture repeats the
panel's text and the stop. `[Gift] panel open, focus on <navigable>` logs every time the open panel loses focus,
so a test shows where focus went after "Regalar objeto". Tests: gift wording and the resonance chat line in
`tests/LeAccess.Social.Tests`. Pending listening: focus right after the options menu closes (it deregisters the
party navigable), the result notifications, resonance rows, and an item left in the slot when the panel closes.

## Limits and pending listening

- Whether the windows open with focus on their button or field is unheard.
- Cross, typing (screen keyboard and physical) and Circle on the username fields are unheard.
- The invite failure log line is not specific to "not found" (network errors read the same).
- Which social lines really pass through `SubmitSystemMessage` beyond the three disassembled ones.
- How a controller user reaches the invitation prompt: read the focus log line on the first test.
- Tests: `tests/LeAccess.Social.Tests` (74 checks).
