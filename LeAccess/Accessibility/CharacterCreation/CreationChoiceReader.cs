using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Speaks the state of the new-character checkboxes, game modes and realm banner, and announces its changes once.</summary>
internal static class CreationChoiceReader
{
    // Exact bindings in the installed CharacterSelection prefab; see reference/creation-menu-reading.md.
    private const string GameModesName = "Game Modes";
    private const string HardcoreName = "Hardcore";
    private const string StandardName = "Standard";
    private const string ChangeRealmName = "changeRealmButton";
    private const string CycleTitleName = "CycleName";
    private const string LegacyTitleName = "Name";

    private readonly record struct Choice(GameObject Root, string Text, string State, string Change);

    private static GameObject _tracked;
    private static string _trackedState;
    private static string _lastFailure;

    internal static bool TryRead(GameObject go, out string text)
    {
        text = null;
        try
        {
            if (!TryReadChoice(go, out var choice)) return false;
            _tracked = choice.Root;
            _trackedState = choice.State;
            text = choice.Text;
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>Speaks the new state once when the focused choice changes after a press.</summary>
    internal static void Poll(Func<GameObject, bool> isFocused)
    {
        if (_tracked is null) return;
        try
        {
            if (!IsAlive(_tracked) || !TryReadChoice(_tracked, out var choice)) { Reset(); return; }
            if (choice.State == _trackedState || !isFocused(_tracked)) return;
            bool changed = _trackedState is not null && choice.State is not null;
            _trackedState = choice.State;
            if (changed) ScreenReader.Speak(choice.Change, true);
        }
        catch (Exception ex) { Reset(); ReportFailure(ex); }
    }

    internal static void Reset() { _tracked = null; _trackedState = null; }

    private static bool TryReadChoice(GameObject go, out Choice choice)
    {
        choice = default;
        if (!IsAlive(go)) return false;
        var button = go.GetComponentInParent<Button>();
        if (!IsAlive(button) || !button.isActiveAndEnabled || !UiVisibility.IsVisible(button.gameObject)) return false;
        var slots = LocalCharacterSlots.instance;
        if (!IsAlive(slots) || !button.transform.IsChildOf(slots.transform)) return false;
        string label = ReadOwnLabel(button.transform);
        if (string.IsNullOrWhiteSpace(label)) return false;
        var root = button.gameObject;
        if (IsCheckmarkOwner(button, slots.soloChallengeCheckmark)) return Checkbox(root, label, slots.isSoloChallenge, out choice);
        if (IsCheckmarkOwner(button, slots.soloCharacterChallengeCheckmark))
            return Checkbox(root, label, slots.isSoloCharacterChallenge, out choice);
        var parent = button.transform.parent;
        bool hardcoreButton = root.name == HardcoreName;
        if (IsAlive(parent) && parent.gameObject.name == GameModesName && (hardcoreButton || root.name == StandardName))
            return Mode(root, parent, label, hardcoreButton == slots.isHardcore, slots.isHardcore, out choice);
        return root.name == ChangeRealmName && TryRealm(root, label, out choice);
    }

    // The checkmark is a typed reference owned by the checkbox's button.
    private static bool IsCheckmarkOwner(Button button, GameObject checkmark)
    {
        if (!IsAlive(checkmark)) return false;
        var owner = checkmark.transform.parent;
        return IsAlive(owner) && owner.Pointer == button.transform.Pointer;
    }

    private static bool Checkbox(GameObject root, string label, bool isChecked, out Choice choice)
    {
        string state = ModText.Get(isChecked ? "creation.checked" : "creation.not_checked");
        choice = new(root, ModText.Format("creation.checkbox", label, state), isChecked.ToString(), state);
        return true;
    }

    // Both mode buttons flip the same toggle, so a change names the mode now selected.
    private static bool Mode(GameObject root, Transform modes, string label, bool isSelected, bool isHardcore, out Choice choice)
    {
        string text = ModText.Format("creation.choice", label,
            ModText.Get(isSelected ? "creation.selected" : "creation.not_selected"));
        var activeButton = isSelected ? null : modes.Find(isHardcore ? HardcoreName : StandardName);
        string active = isSelected ? label : IsAlive(activeButton) ? ReadOwnLabel(activeButton) : null;
        string change = string.IsNullOrWhiteSpace(active) ? text
            : ModText.Format("creation.choice", active, ModText.Get("creation.selected"));
        choice = new(root, text, isSelected.ToString(), change);
        return true;
    }

    // The visible banner's own typed GameObject names the chosen realm; its title is the game's localized text.
    private static bool TryRealm(GameObject root, string label, out Choice choice)
    {
        choice = default;
        var character = CharacterSelect.instance;
        if (!IsAlive(character)) return false;
        var banner = root.transform.parent;
        string titleName;
        if (IsBanner(banner, character.CharacterCreationRealm_Cycle)) titleName = CycleTitleName;
        else if (IsBanner(banner, character.CharacterCreationRealm_Legacy)) titleName = LegacyTitleName;
        else return false;
        var titleTransform = banner.Find(titleName);
        var title = IsAlive(titleTransform) ? titleTransform.GetComponent<TMP_Text>() : null;
        string realm = TextUtils.IsVisibleLabel(title) ? TextUtils.CleanText(title.text) : null;
        string text = string.IsNullOrWhiteSpace(realm) ? label : ModText.Format("creation.realm_current", label, realm);
        choice = new(root, text, string.IsNullOrWhiteSpace(realm) ? null : realm, text);
        return true;
    }

    private static bool IsBanner(Transform candidate, GameObject banner) =>
        IsAlive(candidate) && IsAlive(banner) && candidate.Pointer == banner.transform.Pointer;

    private static string ReadOwnLabel(Transform control)
    {
        string label = null;
        for (int i = 0; i < control.childCount; i++)
        {
            var text = control.GetChild(i).GetComponent<TMP_Text>();
            if (!TextUtils.IsVisibleLabel(text) || string.IsNullOrWhiteSpace(TextUtils.CleanText(text.text))) continue;
            if (label != null) return null;
            label = TextUtils.CleanText(text.text);
        }
        return label;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Creation choice feedback unavailable: {ex}");
    }
}
