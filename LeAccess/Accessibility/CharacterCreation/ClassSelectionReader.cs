using LeAccess.Accessibility.Announcements;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

internal readonly record struct ClassSelection(string Key, GameObject Root, string Name, string Description);

internal static class ClassSelectionReader
{
    private static CharacterSelect _character;
    private static MasteryBanner[] _banners;

    internal static bool TryRead(out ClassSelection selection)
    {
        selection = default;
        var nav = NavigationManager.currentlySelected;
        if (nav is null || nav.WasCollected) return false;
        var owner = nav.TryCast<Component>();
        if (!IsAlive(owner) || !UiVisibility.IsVisible(owner.gameObject)) return false;
        var list = nav.TryCast<ListNavigable>();
        if (IsAlive(list))
        {
            var children = list._selectableChildren;
            int index = list._lastSelectedIndex;
            if (children is null || children.WasCollected || index < 0 || index >= children.Count) return false;
            var selected = children[index];
            return IsAlive(selected) && selected.isActiveAndEnabled && TryReadControl(selected.gameObject, out selection);
        }
        var prioritized = nav.TryCast<PrioritizedNavigable>();
        if (IsAlive(prioritized) && prioritized.isActiveAndEnabled)
        {
            var control = prioritized.ActiveSelectable;
            if (IsAlive(control) && control.isActiveAndEnabled && UiVisibility.IsVisible(control.gameObject))
            {
                if (TryReadControl(control.gameObject, out selection)) return true;
                return false;
            }
        }
        var tree = owner.GetComponentInParent<FullScreenPanelPassiveTree>();
        return IsAlive(tree) && TryReadTree(tree, tree._currentMasteryIndex, out selection);
    }

    internal static bool TryReadControl(GameObject go, out ClassSelection selection)
    {
        selection = default;
        if (!UiVisibility.IsVisible(go)) return false;
        var tab = go.GetComponentInParent<MasteryTabButton>();
        if (IsAlive(tab))
        {
            var panel = tab.GetComponentInParent<FullScreenPanelPassiveTree>();
            return IsAlive(panel) && IsAlive(tab._tabButton) &&
                TryReadTree(panel, tab._tabButton.SelectedIndex, out selection, tab);
        }
        var mastery = go.GetComponentInParent<MasteryButton>();
        if (IsAlive(mastery))
        {
            MasteryReader.TryGetMasteryInfo(go, out var masteryName, out var masteryDescription);
            selection = new($"mastery:{mastery.Pointer}", mastery.gameObject, masteryName, masteryDescription);
            return true;
        }
        var creation = go.GetComponentInParent<CharacterCreationSelection>();
        var character = CharacterSelect.instance;
        if (!IsAlive(creation) || !IsAlive(character) ||
            character.currentState != CharacterSelect.CharacterSelectState.ClassSelection ||
            creation.characterContext != CharacterCreationSelection.CharacterContext.NewCharacter ||
            !IsAlive(creation.characterClass)) return false;
        var definition = creation.characterClass;
        string name = MasteryReader.LocalizedName(definition.className, mastery: false);
        string description = ReadBaseDescription(character, definition.classID);
        selection = new($"class:{creation.Pointer}:{definition.Pointer}", creation.gameObject, name, description);
        return true;
    }

    private static bool TryReadTree(FullScreenPanelPassiveTree tree, int index, out ClassSelection selection,
        MasteryTabButton focused = null)
    {
        selection = default;
        var tabs = tree._masteryTabButtons;
        var data = tree._masteryDataList;
        if (!UiVisibility.IsVisible(tree.gameObject) || tabs is null || tabs.WasCollected ||
            data is null || data.WasCollected || index < 0 || index >= data.Count) return false;
        MasteryTabButton target = null;
        foreach (var tab in tabs)
            if (IsAlive(tab) && IsAlive(tab._tabButton) && tab._tabButton.SelectedIndex == index)
            { target = tab; break; }
        if (!IsAlive(target) || !UiVisibility.IsVisible(target.gameObject) ||
            IsAlive(focused) && focused.Pointer != target.Pointer) return false;
        var entry = data[index];
        if (entry is null || entry.WasCollected) return false;
        string name = MasteryReader.LocalizedName(entry.name, entry.isMastery);
        selection = new($"tree:{tree.Pointer}:{index}", target.gameObject, name, MasteryReader.ReadDefinition(entry));
        return true;
    }

    private static string ReadBaseDescription(CharacterSelect character, CharacterClassID classId)
    {
        if (!IsAlive(_character) || _character.Pointer != character.Pointer)
        {
            _character = character;
            var banners = character.GetComponentsInChildren<MasteryBanner>(true);
            _banners = new MasteryBanner[banners.Length];
            for (int i = 0; i < banners.Length; i++) _banners[i] = banners[i];
        }
        foreach (var banner in _banners)
        {
            if (!IsAlive(banner) || banner.characterClassID != classId ||
                banner.masteryButtons is null || banner.masteryButtons.WasCollected) continue;
            foreach (var button in banner.masteryButtons)
                if (IsAlive(button) && !button.isMasteryClass) return MasteryReader.ReadDefinition(button);
        }
        return null;
    }

    internal static void Reset() { _character = null; _banners = null; }
}
