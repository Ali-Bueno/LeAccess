using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Settings;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class UiFocusOwnership
{
    internal static bool HasForegroundAccess(GameObject element)
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || !UiVisibility.IsVisible(element)) return false;
        if (ScreenContentAnnouncer.HasActiveContent) return ScreenContentAnnouncer.OwnsFocus(element);
        return !DialogueAnnouncer.HasActiveContent || DialogueAnnouncer.OwnsFocus(element);
    }

    internal static bool Allows(GameObject element)
    {
        try
        {
            if (!HasForegroundAccess(element) || !UiVisibility.HasVisiblePresentation(element)) return false;
            var navigation = NavigationManager.currentlySelected;
            if (navigation is null) return true;
            if (navigation.WasCollected) return false;
            if (TryReadSelected(navigation, out var selected, out bool declared)) return IsWithin(element, selected);
            if (declared) return false;
            var component = navigation.TryCast<Component>();
            var behaviour = navigation.TryCast<Behaviour>();
            if (IsAlive(behaviour) && !behaviour.isActiveAndEnabled) return false;
            var mediator = navigation.TryCast<NavigableMediator>();
            if (IsAlive(mediator))
            {
                var panel = mediator.FocusedPanel;
                return IsAlive(panel) && panel.isActiveAndEnabled && IsWithin(element, panel.gameObject);
            }
            return IsAlive(component) && IsWithin(element, component.gameObject);
        }
        catch (System.Exception) { return false; }
    }

    internal static bool TryGetSelected(out GameObject selected)
    {
        selected = null;
        try
        {
            var navigation = NavigationManager.currentlySelected;
            return navigation is not null && !navigation.WasCollected &&
                TryReadSelected(navigation, out selected, out _) && HasForegroundAccess(selected) &&
                UiVisibility.HasVisiblePresentation(selected);
        }
        catch (System.Exception) { selected = null; return false; }
    }

    private static bool TryReadSelected(INavigable navigation, out GameObject selected, out bool declared)
    {
        selected = null;
        declared = false;
        if (SettingsFocusReader.TryRead(out var settings))
        {
            declared = true;
            selected = settings.Selected;
            return UiVisibility.IsVisible(selected);
        }
        var owner = navigation.TryCast<Behaviour>();
        if (!IsAlive(owner) || !owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject))
        { declared = true; return false; }
        if (CraftingSelectionReader.TryReadSelected(navigation, out selected, out bool craftingDeclared))
        { declared = true; return true; }
        if (craftingDeclared) { declared = true; return false; }
        UnityEngine.UI.Selectable control = null;
        var vertical = navigation.TryCast<VerticalNavigable>();
        var search = navigation.TryCast<CraftingMaterialsPanelSearchInputFieldNavigable>();
        var standard = navigation.TryCast<Navigable>();
        if (IsAlive(vertical))
        {
            declared = true;
            var controls = vertical.selectables;
            int index = vertical.currentlySelectedIdx;
            if (controls is null || controls.WasCollected || index < 0 || index >= controls.Length) return false;
            control = controls[index];
        }
        else if (IsAlive(search))
        {
            declared = true;
            control = search.inputField;
        }
        else if (IsAlive(standard))
        {
            control = standard.ActiveSelectable;
            declared = IsAlive(control);
        }
        if (!IsAlive(control) || !control.isActiveAndEnabled || !UiVisibility.IsVisible(control.gameObject)) return false;
        selected = control.gameObject;
        return true;
    }

    private static bool IsWithin(GameObject element, GameObject root) =>
        UiVisibility.IsVisible(root) && element.transform.IsChildOf(root.transform);
}
