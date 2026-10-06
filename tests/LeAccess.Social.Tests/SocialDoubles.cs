// Game types the Social readers use, with only the members they read.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class FriendPostRequestsUI : MonoBehaviour
{
    internal Toggle onlineFriendToggle, offlineFriendToggle, friendRequestToggle, sentRequestToggle, blockedToggle;
    internal GameObject onlinePlayersPanel, offlinePlayersPanel, friendRequestsPanel, sentRequestsPanel, blockedPlayersPanel;
    internal GameObject addFriendMenu, partyInviteMenu, blockUserMenu;
    internal TextMeshProUGUI socialTitle;
}
internal sealed class SocialPanelPlayerRef : MonoBehaviour
{
    internal TextMeshProUGUI socialPlayerName, socialCharacterName, socialCharacterLevel, socialCharacterLocation, socialStatusMsg;
}
internal sealed class SocialPanelPlayerSelectableAdapter : Selectable { }
internal sealed class TabUIController : MonoBehaviour { internal int SelectedTabId; }
internal sealed class TabUIElement : Selectable { internal TabUIController controller; internal int elementID; }
internal sealed class ChangePlayerStatus : MonoBehaviour { internal TMP_LocalizedDropdown statusDropdown; }
internal sealed class AddFriendInputField : MonoBehaviour { internal TMP_InputField inputField; }
internal sealed class BlockPlayerInputField : MonoBehaviour { internal TMP_InputField inputField; }
internal sealed class RightClickMenu : MonoBehaviour { internal TextMeshProUGUI title; }
internal class InfoForTooltip : MonoBehaviour { }
internal sealed class GeneralTooltop : InfoForTooltip { internal string bodyKey, body; }
internal sealed class RightClickBtnRef : MonoBehaviour
{
    internal Button btnFunction;
    internal TextMeshProUGUI buttonText;
    internal GeneralTooltop tooltip;
}
internal sealed class UIBase : MonoBehaviour
{
    internal static bool instanceExists;
    internal static UIBase instance;
    internal GameObject PartyOverviewController;
}
internal sealed class CommonLocalization : NativeObject
{
    internal string GetClass(string value) => Localization.GetText("class:" + value, value);
}
internal static class Localization
{
    internal static CommonLocalization Common = new();
    internal static readonly Dictionary<string, string> Texts = new();
    internal static string GetText(string key, string fallback = "") => Texts.TryGetValue(key, out string text) ? text : fallback;
    internal static bool TryGetText(string key, out string value) => Texts.TryGetValue(key, out value);
}
internal interface INavigable { }
internal class Navigable : MonoBehaviour, INavigable { internal Selectable ActiveSelectable; }
internal static class NavigationManager { internal static NativeObject currentlySelected; }
internal sealed class KeyboardEditor : NativeObject { internal GameObject gameObject; }
internal sealed class ScreenKeyboardManager : MonoBehaviour { internal KeyboardEditor _selectedInputField; }
namespace LE.UI.Generic.UnityUI
{
    internal sealed class ConfirmationPanel : MonoBehaviour
    {
        internal TMP_Text HeaderText, BodyText;
        internal Button ConfirmButton, DeclineButton;
    }
    internal sealed class StringInputPanel : MonoBehaviour { internal TMP_InputField StringInputText; }
    internal sealed class RegionSelectionDropdown : TMP_Dropdown { }
    internal sealed class RegionSelectionHelper : MonoBehaviour { internal TMP_Text textRegion, textPing; }
}
namespace LE.UI.Social.UnityUI
{
    internal sealed class SocialPartyPanel : MonoBehaviour { internal TMP_Text regionCategoryLabel; }
    internal sealed class SocialPartyMemberPanel : MonoBehaviour
    {
        internal TMP_Text accountNameText, statusText, locationText, characterNameText, characterLevelText;
        internal GameObject leaderIcon;
    }
}
namespace LE.Services.Models
{
    internal sealed class UserIdentity { internal string Username; }
    internal sealed class ClassSlot { internal bool HasValue; internal CharacterClassID Value; }
    internal enum CharacterClassID : byte { Primalist, Mage, Sentinel, Acolyte, Rogue }
    internal sealed class PartyMember_Full
    {
        internal UserIdentity Identity;
        internal bool IsLeader;
        internal ClassSlot characterClass;
    }
}
namespace LE.UI.Party.UnityUI
{
    internal sealed class PartyOverviewSelectableAdapter : Selectable { }
    internal sealed class PartyOverviewMemberPanel : MonoBehaviour
    {
        internal TMP_Text accountNameText;
        internal GameObject leaderTransform;
        internal Button requestPortalButton;
        internal Image healthBar, wardBar;
        internal bool isInDeadState;
        internal LE.Services.Models.PartyMember_Full partyMember;
        internal PlayerActorSync playerActorSync;
    }
}
internal sealed class PlayerActorSync : MonoBehaviour { }
