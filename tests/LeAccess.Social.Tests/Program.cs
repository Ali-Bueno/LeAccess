using LE.UI.Generic.UnityUI;
using LE.Services.Models;
using LE.UI.Party.UnityUI;
using LE.UI.Social.UnityUI;
using LeAccess;
using LeAccess.Accessibility;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Social;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

int checks = 0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
ModText.SetLanguage("es");
string T(string key) => ModText.Get(key);
string F(string key, params object[] args) => ModText.Format(key, args);
string Join(params string[] parts) => ScreenContentTracker.Combine(parts);
string Read(GameObject element) => SocialReader.TryRead(element, out string text) ? text : null;

// Wording that does not need the panel.
Check(SocialText.TrimDecoration("● Ausente") == "Ausente" && SocialText.TrimDecoration("En línea") == "En línea",
    "The status option's coloured dot is dropped");
Check(SocialText.Edit("", "B") == "B" && SocialText.Edit("B", "Be") == "e" && SocialText.Edit("Bea", "Be") == F("social.erased", "a"),
    "Typing speaks the new characters and erasing the removed ones");
Check(SocialText.Edit("Bea", "Zed") == "Zed" && SocialText.Edit("Bea", "") == T("social.field_empty") &&
    SocialText.Edit("Bea", "Bea") == null, "A replaced, cleared or unchanged value");
Check(SocialText.Region("Región de grupo", null, "25ms") == null && SocialText.Status(null, "Ana#1234") == F("social.username", "Ana#1234"),
    "A missing region or status value leaves the generic reading or just the username");
Check(SocialText.Invite("Bea#5678", "te ha invitado a un grupo.", "Aceptar", "Rechazar") ==
    "Bea#5678 te ha invitado a un grupo. Aceptar o Rechazar", "The invitation reads as one sentence, then its buttons");

// System chat lines, matched by the game's localized templates.
Localization.Texts["Chat_SuccessMessage_InviteSend"] = "ha recibido la invitación a tu grupo.";
Localization.Texts["Chat_SuccessMessage_FriendRequest"] = "se ha añadido a tu lista de amigos.";
Localization.Texts["Chat_ErrorMessage_FriendRequest"] = "Fallada al enviar solicitud de amistad";
Localization.Texts["Chat_ErrorMessage_Block"] = "No se puede bloquear al jugador {0}";
Localization.Texts["Chat_Message_SetPartyRegion"] = "{0} establece la Región del Grupo en: {1}";
string Localize(string key) => Localization.TryGetText(key, out string text) ? text : null;
Check(SocialChatLines.IsSocial("<color=#64A4EF>Bea#5678</color> ha recibido la invitación a tu grupo.", Localize),
    "An invitation sent: name, then the game's text");
Check(SocialChatLines.IsSocial("Bea#5678 se ha añadido a tu lista de amigos.", Localize) &&
    SocialChatLines.IsSocial("Fallada al enviar solicitud de amistad", Localize), "Friend added and friend request failed");
Check(SocialChatLines.IsSocial("No se puede bloquear al jugador Bea#5678", Localize) &&
    SocialChatLines.IsSocial("Ana establece la Región del Grupo en: Europa", Localize), "Templates with placeholders");
Check(!SocialChatLines.IsSocial("Bienvenido a Last Epoch", Localize) && !SocialChatLines.IsSocial("", Localize) &&
    !SocialChatLines.IsSocial("ha recibido la invitación a tu grupo. y más", Localize), "Other system lines are not social");

// The Social panel, built like the game's prefab.
TextMeshProUGUI AddLabel(GameObject owner, string text, bool active = true)
{
    var go = owner.Child("Label");
    go.Active = active;
    return go.Add(new TextMeshProUGUI { text = text });
}
var root = new GameObject { name = "Social" };
var social = root.Add(new FriendPostRequestsUI());
social.socialTitle = AddLabel(root.Child("StatusDropdown"), "Ana#1234");
(Toggle Toggle, GameObject Panel, GameObject Header) Section(string caption, bool expanded)
{
    var holder = root.Child("Section");
    var header = holder.Child("Header");
    header.Add(new Button());
    AddLabel(header, caption);
    var toggle = header.Child("Toggle").Add(new Toggle { isOn = expanded });
    return (toggle, holder.Child("Panel"), header);
}
GameObject Row((Toggle Toggle, GameObject Panel, GameObject Header) section, string name, string character, string location,
    bool loading = false)
{
    var row = section.Panel.Child("SocialPanelIndividualPlayer(Clone)");
    row.Active = section.Toggle.isOn;
    row.Add(new SocialPanelPlayerSelectableAdapter());
    var refs = row.Add(new SocialPanelPlayerRef());
    var loaded = row.Child("Loaded");
    loaded.Active = !loading;
    refs.socialPlayerName = AddLabel(loaded, name);
    refs.socialCharacterName = character == null ? null : AddLabel(loaded, character);
    refs.socialCharacterLevel = AddLabel(loaded, "84", active: false);
    refs.socialCharacterLocation = location == null ? null : AddLabel(loaded, location);
    refs.socialStatusMsg = AddLabel(loaded, "Buscando grupo", active: false);
    return row;
}
var requests = Section("Peticiones de amistad", true);
var online = Section("Amigos en línea", true);
var offline = Section("Sin conexión", false);
var blocked = Section("Jugadores bloqueados", true);
(social.friendRequestToggle, social.friendRequestsPanel) = (requests.Toggle, requests.Panel);
(social.onlineFriendToggle, social.onlinePlayersPanel) = (online.Toggle, online.Panel);
(social.offlineFriendToggle, social.offlinePlayersPanel) = (offline.Toggle, offline.Panel);
(social.blockedToggle, social.blockedPlayersPanel) = (blocked.Toggle, blocked.Panel);
var bea = Row(online, "Bea#5678", "Pj - Nivel 84", "Fin de los tiempos");
Row(online, "Carla#9999", "Druida - Nivel 12", "Ciudad");
var loadingRow = Row(online, "", null, null, loading: true);
Row(offline, "Dani#2222", null, null);
AddLabel(blocked.Panel.Child("SocialPanelEmpty"), "ningún jugador bloqueado");
var request = Row(requests, "Eva#3333", null, null);
var accept = request.Child("AcceptButton");
accept.Add(new Button());
AddLabel(accept, "Aceptar");
var deny = request.Child("DenyButton");
deny.Add(new Button());

Check(Read(bea) == Join("Bea#5678", "Pj - Nivel 84", "Fin de los tiempos", "Amigos en línea"),
    "A friend row: name, character, location, then its section: " + Read(bea));
var card = bea.Child("PlayerCard");
Check(SocialReader.Canonical(card) == bea && SocialReader.Canonical(bea) == bea && Read(card) == Read(bea),
    "The row's card is the same stop as its row");
Check(Read(loadingRow) == Join(T("social.loading"), "Amigos en línea"), "A loading row says so instead of an object name");
Check(Read(online.Header) == "Amigos en línea. Expandida. 3 jugadores", "An expanded header with its row count: " + Read(online.Header));
Check(Read(offline.Header) == Join("Sin conexión", T("social.collapsed"), T("social.players_one")),
    "A collapsed header still counts its hidden rows");
Check(Read(blocked.Header) == Join("Jugadores bloqueados", T("social.expanded"), T("social.players_none")),
    "The game's empty-list placeholder is not a player");
Check(Read(online.Toggle.gameObject) == Read(online.Header), "The header's own toggle reads like the header");
Check(SocialReader.TryReadToggleState(online.Toggle, false, out string state) &&
    state == Join(T("social.collapsed"), F("social.players_many", 3)), "Toggling a section says collapsed and its count");
Check(!SocialReader.TryReadToggleState(new GameObject().Add(new Toggle()), true, out _), "Other toggles keep on and off");
Check(Read(request) == Join("Eva#3333", "Peticiones de amistad"), "A friend request row");
Check(Read(accept) == F("social.button", "Aceptar") && Read(deny) == "Rechazar solicitud, botón",
    "The request's accept button and its textless deny button");

var tabs = root.Child("DetailedTabs Holder").Add(new TabUIController { SelectedTabId = 0 });
TabUIElement Tab(string caption, int id)
{
    var go = tabs.gameObject.Child("Tab");
    AddLabel(go, caption);
    return go.Add(new TabUIElement { controller = tabs, elementID = id });
}
var friendsTab = Tab("amigos", 0);
var partyTab = Tab("grupo", 1);
Check(Read(friendsTab.gameObject) == "amigos, pestaña seleccionada" && Read(partyTab.gameObject) == F("social.tab", "grupo"),
    "Tabs say they are tabs and which one is selected");

var statusGo = root.Child("SocialPanelStatusDropdown");
var dropdown = statusGo.Add(new TMP_LocalizedDropdown());
dropdown.captionText = AddLabel(statusGo, "<size=25><color=yellow>●</color></size><voffset=3> Ausente </voffset>");
statusGo.Add(new ChangePlayerStatus { statusDropdown = dropdown });
Check(Read(statusGo) == "Estado: Ausente. Tu nombre de usuario: Ana#1234",
    "The status gives the user's own username to share: " + Read(statusGo));

var partyGo = root.Child("Party Panel");
var party = partyGo.Add(new SocialPartyPanel());
party.regionCategoryLabel = AddLabel(partyGo, "Región de grupo");
var selector = partyGo.Child("RegionDropdownSelector");
selector.Add(new RegionSelectionDropdown());
var textfield = selector.Child("Textfield");
var region = selector.Add(new RegionSelectionHelper { textRegion = AddLabel(textfield, "Sudamérica"), textPing = AddLabel(textfield, "25ms") });
Check(Read(selector) == "Región de grupo: Sudamérica, ping 25ms", "The region names its label, region and ping");
Check(SocialReader.Canonical(textfield) == selector, "The highlighted text field is the same stop as its dropdown");
region.textPing.gameObject.Active = false;
Check(Read(selector) == "Región de grupo: Sudamérica", "A hidden ping is left out");

var memberGo = partyGo.Child("SocialPartyMember");
memberGo.Add(new SocialPanelPlayerSelectableAdapter());
var leaderIcon = memberGo.Child("Leader");
var member = memberGo.Add(new SocialPartyMemberPanel
{
    accountNameText = AddLabel(memberGo, "Ana#1234"), characterNameText = AddLabel(memberGo, "Pj - Nivel 90"),
    characterLevelText = AddLabel(memberGo, "90", active: false), locationText = AddLabel(memberGo, "Ciudad"),
    statusText = AddLabel(memberGo, "", active: false), leaderIcon = leaderIcon
});
var leave = memberGo.Child("LeavePartyButton");
leave.Add(new Button());
AddLabel(leave, "Salir");
Check(Read(memberGo) == Join("Ana#1234", "Pj - Nivel 90", "Ciudad", T("social.leader")), "A party member and the leader mark");
leaderIcon.Active = false;
Check(Read(memberGo) == Join("Ana#1234", "Pj - Nivel 90", "Ciudad"), "A member who is not the leader");
Check(Read(leave) == null, "The member's own buttons keep the generic reading");

// The party overview panel (radial menu "Grupo").
Localization.Texts["class:Sentinel"] = "Centinela";
var overview = new GameObject { name = "Party Members" };
var pcard = overview.Child("Member 1");
pcard.Add(new PartyOverviewSelectableAdapter());
var cardClass = pcard.Child("Class");
var portalGo = pcard.Child("PortalToPlayer");
var portalButton = portalGo.Add(new Button());
var cardLeader = pcard.Child("PartyLeader");
var healthBar = pcard.Child("HealthBar").Add(new Image { fillAmount = 0.8f });
var wardBar = pcard.Child("WardBar").Add(new Image());
var partyCard = pcard.Add(new PartyOverviewMemberPanel
{
    accountNameText = AddLabel(pcard, "Bea#5678"), leaderTransform = cardLeader, requestPortalButton = portalButton,
    healthBar = healthBar, wardBar = wardBar, playerActorSync = pcard.Add(new PlayerActorSync()),
    partyMember = new() { Identity = new() { Username = "Bea#5678" }, characterClass = new() { HasValue = true, Value = CharacterClassID.Sentinel } }
});
Check(Read(pcard) == Join("Bea#5678", "Centinela", T("party.leader"),
    F("party.health", 80), T("party.same_zone")) && Read(pcard) == "Bea#5678. Centinela. líder. vida 80 por ciento. en tu zona",
    "A member in the same zone: name, class, leader, life, zone");
Check(Read(cardClass) == Read(pcard) && SocialReader.Canonical(cardClass).Pointer == pcard.Pointer,
    "The cursor's Class target is the member's own stop");
wardBar.fillAmount = 0.25f;
cardLeader.Active = false;
Check(Read(pcard) == "Bea#5678. Centinela. vida 80 por ciento. barrera 25 por ciento. en tu zona", "Ward only when present, no leader mark");
partyCard.isInDeadState = true;
Check(Read(pcard) == "Bea#5678. Centinela. muerto", "A dead member");
partyCard.isInDeadState = false;
partyCard.playerActorSync.Destroyed = true;
partyCard.accountNameText.text = "";
Check(Read(pcard) == "Bea#5678. Centinela. en otra zona", "A member in another scene; the name falls back to the identity");
Check(Read(portalGo) == "Portal a Bea#5678, botón", "The portal button names the member");
portalButton.interactable = false;
Check(Read(portalGo) == "Portal a Bea#5678 (No disponible)", "An unavailable portal says so");
Check(PartyText.Percent(1.4f) == 100 && PartyText.Percent(-1f) == 0 && PartyText.Percent(0.495f) == 50 &&
    PartyText.Portal(null, true) == "Portal al jugador, botón", "Percent clamps and rounds; a nameless portal");

// The three username windows.
(GameObject Window, TMP_InputField Field, GameObject Submit, GameObject Close) Window(string title, string button, string explanation = null)
{
    var window = root.Child("Window");
    window.Active = false;
    var panel = window.Child("Panel");
    var close = panel.Child("Close_Button");
    close.Add(new Button());
    AddLabel(close, "X", active: false);
    AddLabel(panel, title);
    var fieldGo = panel.Child("Input Field");
    var field = fieldGo.Add(new TMP_InputField());
    if (explanation != null) AddLabel(fieldGo, explanation);
    var area = fieldGo.Child("Text Area");
    field.placeholder = AddLabel(area, "Nombre de usuario...");
    field.textComponent = AddLabel(area, "​");
    var submit = panel.Child("SubmitButton");
    submit.Add(new Button());
    AddLabel(submit, button);
    return (window, field, submit, close);
}
var invite = Window("Enviar invitación", "Enviar invitación");
invite.Window.Add(new StringInputPanel { StringInputText = invite.Field });
var addFriend = Window("Añadir amigo", "Añadir amigo", "añade a un amigo introduciendo su nombre de usuario");
addFriend.Field.gameObject.Add(new AddFriendInputField { inputField = addFriend.Field });
var block = Window("Bloquear jugador", "Bloquear jugador");
block.Field.gameObject.Add(new BlockPlayerInputField { inputField = block.Field });
(social.partyInviteMenu, social.addFriendMenu, social.blockUserMenu) = (invite.Window, addFriend.Window, block.Window);
invite.Window.Active = true;
Check(Read(invite.Field.gameObject) == Join(T("social.field_invite"), T("control.text_field"), T("social.field_empty"),
    T("social.field_hint")), "The invite field: label with format, role, empty value and how it is typed");
invite.Field.text = "Bea#5678";
Check(Read(invite.Field.gameObject).Contains("Bea#5678") && Read(addFriend.Field.gameObject).StartsWith(T("social.field_add_friend")) &&
    Read(block.Field.gameObject).StartsWith(T("social.field_block")), "Each field names its purpose and reads its value");
Check(Read(invite.Submit) == "Enviar invitación, botón" && Read(invite.Close) == F("social.button", T("social.close")),
    "The window's buttons say they are buttons; the textless one is Close");
addFriend.Window.Active = true;
Check(SocialWindows.Intro(addFriend.Window, SocialWindows.Kind.Username) ==
    Join("Añadir amigo", "añade a un amigo introduciendo su nombre de usuario", T("social.format_hint")),
    "A window's introduction: title, the game's explanation and the username format");
addFriend.Window.Active = false;
var keyboard = new GameObject().Add(new ScreenKeyboardManager { _selectedInputField = new KeyboardEditor { gameObject = invite.Field.gameObject } });
Check(SocialWindows.KeyboardLabel(keyboard) == T("social.field_invite"), "The on-screen keyboard names the username field");
keyboard._selectedInputField.gameObject = new GameObject();
Check(SocialWindows.KeyboardLabel(keyboard) == null, "Other fields keep the game's placeholder");

// Player options menu.
var menuGo = new GameObject { name = "SocialRightClickMenuAlt" };
var menu = menuGo.Add(new RightClickMenu { title = AddLabel(menuGo, "Bea#5678") });
GameObject Option(string caption, bool available)
{
    var go = menuGo.Child("rightClickMenuBtn");
    var button = go.Add(new Button { interactable = available });
    var tooltip = go.Add(new GeneralTooltop { Enabled = !available, bodyKey = available ? null : "Portal_Unavailable" });
    go.Add(new RightClickBtnRef { btnFunction = button, buttonText = AddLabel(go, caption), tooltip = tooltip });
    return go;
}
var inviteOption = Option("Invitar al grupo", true);
var portalOption = Option("Portal al jugador", false);
Localization.Texts["Portal_Unavailable"] = "El jugador no está en una zona a la que puedas viajar.";
Check(Read(inviteOption) == "Invitar al grupo", "An available option reads its caption");
Check(Read(portalOption) == Join(F("control.disabled", "Portal al jugador"), "El jugador no está en una zona a la que puedas viajar."),
    "An unavailable option says so and gives the game's reason");

// Introductions lead the first announcement inside a newly opened pop-up.
SocialAccessibility.Reset();
SocialReader.TryRead(bea, out _);
void Frame() { Time.frameCount++; ScreenReader.SpokeThisFrame = false; SocialAccessibility.Update(); }
Check(SocialAccessibility.LeadFor(invite.Submit) == Join("Enviar invitación", T("social.format_hint")),
    "Opening the invite window leads with its title and the username format");
Check(SocialAccessibility.LeadFor(invite.Field.gameObject) != null, "An introduction not yet spoken is offered again");
SocialAccessibility.LeadSpoken(invite.Field.gameObject);
Check(SocialAccessibility.LeadFor(invite.Submit) == null, "Once spoken, moving inside the window is not introduced again");
Check(SocialAccessibility.LeadFor(bea) == null && SocialAccessibility.LeadFor(new GameObject()) == null,
    "Elements outside pop-ups have no introduction");
invite.Window.Active = false; Frame();
invite.Window.Active = true; Frame();
Check(SocialAccessibility.LeadFor(invite.Submit) != null, "Reopening the window introduces it again");
Check(SocialAccessibility.LeadFor(inviteOption) == "Opciones de Bea#5678", "The options menu names its player");

// Social results from the chat.
ScreenReader.Messages.Clear();
SocialAccessibility.SystemMessageSubmitted("<color=#64A4EF>Bea#5678</color> ha recibido la invitación a tu grupo.");
SocialAccessibility.SystemMessageSubmitted("Bienvenido a Last Epoch");
Frame();
Check(ScreenReader.Messages.Count == 0, "A result waits a frame for the focus the game moves");
Frame();
Check(ScreenReader.Messages.Single() == ("Bea#5678 ha recibido la invitación a tu grupo.", false),
    "Then speaks on its own, after current speech; other system lines stay silent");
Check(Plugin.Log.Information.Contains("[Social] Bea#5678 ha recibido la invitación a tu grupo."), "Results are logged");
ScreenReader.Messages.Clear();
SocialAccessibility.SystemMessageSubmitted("Bea#5678 se ha añadido a tu lista de amigos.");
Frame();
string lead = SocialAccessibility.LeadFor(bea);
SocialAccessibility.LeadSpoken(bea);
Frame(); Frame();
Check(lead == "Bea#5678 se ha añadido a tu lista de amigos." && ScreenReader.Messages.Count == 0,
    "A result leads the next announcement in the panel instead, once");

// The unknown username: only the game's log line, while the invite window stays open.
SocialLog.Install();
void Log(string text) => BepInEx.Logging.Logger.Listeners.Single().LogEvent(null, new BepInEx.Logging.LogEventArgs { Data = text });
Log("User service error: GENERIC - Not found");
Frame(); Frame();
Check(ScreenReader.Messages.Single().Text == T("social.user_not_found"), "A user-service error with the invite window open");
ScreenReader.Messages.Clear();
invite.Window.Active = false;
Log("User service error: GENERIC - Not found");
Frame(); Frame();
Log("[Forge Craft] Result");
invite.Window.Active = true;
Frame(); Frame();
Check(ScreenReader.Messages.Count == 0, "Without the invite window, or for other log lines, nothing is said");

// Typing echo in the open window's field.
invite.Field.text = "";
Frame();
ScreenReader.Messages.Clear();
foreach (string value in new[] { "B", "Be", "B", "Zed", "" }) { invite.Field.text = value; Frame(); }
Check(ScreenReader.Messages.Select(message => message.Text).SequenceEqual(new[] { "B", "e", F("social.erased", "e"), "Zed", T("social.field_empty") }) &&
    ScreenReader.Messages.All(message => message.Interrupt), "Typed and erased characters are echoed as they change");
invite.Window.Active = false; Frame();
invite.Field.text = "Old#0001";
invite.Window.Active = true;
ScreenReader.Messages.Clear();
Frame(); Frame();
Check(ScreenReader.Messages.Count == 0, "Opening a window takes its field's value silently");
invite.Window.Active = false; Frame();

// The party-invite prompt under the HUD's player cards.
var cards = new GameObject { name = "Player Cards Panel" };
UIBase.instance = cards.Add(new UIBase { PartyOverviewController = cards });
UIBase.instanceExists = true;
var promptGo = cards.Child("Party Invite Confirmation Prompt");
promptGo.Active = false;
var acceptInvite = promptGo.Child("AcceptPartyInviteButton");
var declineInvite = promptGo.Child("DeclinePartyInviteButton");
promptGo.Add(new ConfirmationPanel
{
    HeaderText = AddLabel(promptGo, "Bea#5678"), BodyText = AddLabel(promptGo, "te ha invitado a un grupo."),
    ConfirmButton = acceptInvite.Add(new Button()), DeclineButton = declineInvite.Add(new Button())
});
AddLabel(acceptInvite, "Aceptar");
AddLabel(declineInvite, "Rechazar");
var navigable = new GameObject { name = "Party Members" }.Add(new Navigable());
navigable.ActiveSelectable = navigable.gameObject.Child("Member 1").Add(new Button());
NavigationManager.currentlySelected = navigable;
ScreenReader.Messages.Clear();
Frame();
promptGo.Active = true;
Frame();
Check(ScreenReader.Messages.Count == 0, "The prompt's text settles for a frame");
Frame();
Check(ScreenReader.Messages.Single() == ("Bea#5678 te ha invitado a un grupo. Aceptar o Rechazar", false),
    "The invitation and its buttons are spoken once it appears");
Check(Plugin.Log.Information.Last().Contains("navigable=Party Members, selected=Member 1"),
    "The log records which navigable holds focus when the prompt appears");
Frame(); Frame();
Check(ScreenReader.Messages.Count == 1, "The prompt speaks once per opening");
promptGo.Active = false; Frame();
promptGo.Active = true;
ScreenReader.Messages.Clear();
Check(SocialAccessibility.LeadFor(acceptInvite) == "Bea#5678 te ha invitado a un grupo. Aceptar o Rechazar",
    "If the game focuses its buttons, the invitation leads them");
SocialAccessibility.LeadSpoken(acceptInvite);
Frame(); Frame();
Check(ScreenReader.Messages.Count == 0, "and is not repeated on its own");

SocialLog.Uninstall();
Check(BepInEx.Logging.Logger.Listeners.Count == 0, "Unloading removes the log listener");
Check(Plugin.Log.Warnings.Count == 0, "No failures were reported: " + string.Join(" | ", Plugin.Log.Warnings));
Console.WriteLine($"Social tests passed: {checks} checks.");
