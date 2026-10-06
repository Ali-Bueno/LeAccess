using System;
using System.Collections.Generic;
using LE.UI.Generic.UnityUI;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Social;

/// <summary>The Social panel's pop-ups: the three username windows, the player options menu and the party-invite prompt.</summary>
/// <remarks>See reference/social-reading.md#windows.</remarks>
internal static class SocialWindows
{
    internal enum Kind { Username, Options, Invite }

    private static ConfirmationPanel _prompt;
    private static IntPtr _searchedUi;

    /// <summary>The pop-up that contains this element, if any.</summary>
    internal static bool TryGet(GameObject element, out GameObject window, out Kind kind)
    {
        window = null;
        kind = default;
        if (!IsAlive(element)) return false;
        var social = element.GetComponentInParent<FriendPostRequestsUI>();
        if (IsAlive(social))
            foreach (var (candidate, _) in Fields(social))
                if (IsAlive(candidate) && element.transform.IsChildOf(candidate.transform))
                { window = candidate; kind = Kind.Username; return true; }
        var menu = element.GetComponentInParent<RightClickMenu>();
        if (IsAlive(menu)) { window = menu.gameObject; kind = Kind.Options; return true; }
        var prompt = FindPrompt();
        if (IsAlive(prompt) && element.transform.IsChildOf(prompt.transform))
        { window = prompt.gameObject; kind = Kind.Invite; return true; }
        return false;
    }

    /// <summary>What the pop-up says on opening: its title and explanation, the options' player, or the invitation.</summary>
    internal static string Intro(GameObject window, Kind kind) => kind switch
    {
        Kind.Options => SocialText.Options(Label(Title(window.GetComponent<RightClickMenu>()))),
        Kind.Invite => PromptText(window.GetComponent<ConfirmationPanel>()),
        _ => UsernameIntro(window)
    };

    private static TMP_Text Title(RightClickMenu menu) => IsAlive(menu) ? menu.title : null;

    /// <summary>The username windows, by the Social panel's own references.</summary>
    internal static (GameObject Window, SocialField Field)[] Fields(FriendPostRequestsUI social) => new[]
    {
        (social.partyInviteMenu, SocialField.PartyInvite), (social.addFriendMenu, SocialField.AddFriend),
        (social.blockUserMenu, SocialField.Block)
    };

    /// <summary>A username window's field, through the component that submits it.</summary>
    internal static TMP_InputField FieldOf(GameObject window, SocialField field)
    {
        if (!IsAlive(window)) return null;
        switch (field)
        {
            case SocialField.PartyInvite:
                var invite = window.GetComponent<StringInputPanel>();
                return IsAlive(invite) ? invite.StringInputText : null;
            case SocialField.AddFriend:
                var add = window.GetComponentInChildren<AddFriendInputField>(true);
                return IsAlive(add) ? add.inputField : null;
            default:
                var block = window.GetComponentInChildren<BlockPlayerInputField>(true);
                return IsAlive(block) ? block.inputField : null;
        }
    }

    // Labels outside buttons and outside the field's own text and placeholder: the title and explanation.
    private static string UsernameIntro(GameObject window)
    {
        var input = window.GetComponentInChildren<TMP_InputField>();
        var fragments = new List<UiReadingOrder.Fragment>();
        foreach (var label in window.GetComponentsInChildren<TMP_Text>())
        {
            if (IsAlive(label.GetComponentInParent<Button>()) || IsAlive(input) &&
                (Same(label, input.textComponent) || Same(label, input.placeholder))) continue;
            string text = Label(label);
            if (text != null) fragments.Add(new UiReadingOrder.Fragment(label, text));
        }
        return SocialText.Intro(UiReadingOrder.Read(fragments), usernameWindow: true);
    }

    /// <summary>The username field that contains this element, if any.</summary>
    internal static bool TryGetField(GameObject element, out TMP_InputField input, out SocialField field)
    {
        input = null;
        field = default;
        var social = IsAlive(element) ? element.GetComponentInParent<FriendPostRequestsUI>() : null;
        if (!IsAlive(social)) return false;
        foreach (var (window, kind) in Fields(social))
        {
            if (!IsAlive(window) || !element.transform.IsChildOf(window.transform)) continue;
            var candidate = FieldOf(window, kind);
            if (!IsAlive(candidate) || !element.transform.IsChildOf(candidate.transform)) return false;
            input = candidate;
            field = kind;
            return true;
        }
        return false;
    }

    /// <summary>The on-screen keyboard names a username field with the mod's label, which carries the format.</summary>
    internal static string KeyboardLabel(ScreenKeyboardManager keyboard)
    {
        try
        {
            var editor = IsAlive(keyboard) ? keyboard._selectedInputField : null;
            var go = editor is null || editor.WasCollected ? null : editor.gameObject;
            return TryGetField(go, out _, out var field) ? SocialText.FieldLabel(field) : null;
        }
        catch (Exception) { return null; }
    }

    /// <summary>The party-invite prompt: the only confirmation panel under the HUD's player cards (UIBase.PartyOverviewController).</summary>
    internal static ConfirmationPanel FindPrompt()
    {
        if (IsAlive(_prompt)) return _prompt;
        _prompt = null;
        if (!UIBase.instanceExists) return null;
        var ui = UIBase.instance;
        if (!IsAlive(ui) || ui.Pointer == _searchedUi) return null;
        var cards = ui.PartyOverviewController;
        if (!IsAlive(cards)) return null;
        _searchedUi = ui.Pointer;
        var prompt = cards.GetComponentInChildren<ConfirmationPanel>(true);
        _prompt = IsAlive(prompt) ? prompt : null;
        return _prompt;
    }

    internal static string PromptText(ConfirmationPanel prompt) => !IsAlive(prompt) ? null :
        SocialText.Invite(Label(prompt.HeaderText), Label(prompt.BodyText), Caption(prompt.ConfirmButton),
            Caption(prompt.DeclineButton));

    internal static string Caption(Button button) =>
        IsAlive(button) ? TextUtils.GetTextFromHierarchy(button.gameObject) : null;

    internal static string Label(TMP_Text label)
    {
        if (!TextUtils.IsVisibleLabel(label)) return null;
        string text = TextUtils.CleanText(label.text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    internal static bool Same(UnityEngine.Object first, UnityEngine.Object second) =>
        IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;

    internal static void Reset()
    {
        _prompt = null;
        _searchedUi = IntPtr.Zero;
    }
}
