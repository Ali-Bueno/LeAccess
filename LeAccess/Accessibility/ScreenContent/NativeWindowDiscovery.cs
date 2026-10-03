using LE.UI.PanelSystem;
using UnityEngine;
using LeAccess.Accessibility.Announcements;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.ScreenContent;

internal static class NativeWindowDiscovery
{
    internal static void Update()
    {
        var foreground = ReadForeground();
        if (!IsAlive(foreground)) return;
        var root = foreground.gameObject;
        int id = root.GetInstanceID();
        if (ScreenContentAnnouncer.HasWindowOwner(id)) return;
        ScreenContentAnnouncer.Track(root, retainWhenHidden: true, separateWindow: true,
            ownsWindow: () => OwnsWindow(foreground));
    }

    private static Behaviour ReadForeground()
    {
        var navigation = NavigationManager.currentlySelected;
        if (navigation is null || navigation.WasCollected) return null;
        var owner = navigation.TryCast<Behaviour>();
        if (!IsVisibleOwner(owner)) return null;
        var mediator = navigation.TryCast<NavigableMediator>();
        if (IsAlive(mediator))
        {
            var panel = mediator.FocusedPanel;
            return IsVisibleOwner(panel) && IsContentPanel(panel) ? panel : null;
        }
        var foreground = FindOwner(owner.gameObject);
        if (IsAlive(foreground)) return foreground;
        var navigable = navigation.TryCast<Navigable>();
        return IsAlive(navigable) && IsVisibleOwner(navigable.ActiveSelectable)
            ? FindOwner(navigable.ActiveSelectable.gameObject) : null;
    }

    private static bool OwnsWindow(Behaviour expected)
    {
        if (!IsVisibleOwner(expected)) return false;
        var navigation = NavigationManager.currentlySelected;
        if (navigation is null || navigation.WasCollected) return false;
        var owner = navigation.TryCast<Behaviour>();
        if (!IsVisibleOwner(owner)) return false;
        var mediator = navigation.TryCast<NavigableMediator>();
        if (IsAlive(mediator))
        {
            var panel = mediator.FocusedPanel;
            return IsVisibleOwner(panel) && panel.gameObject.GetInstanceID() == expected.gameObject.GetInstanceID();
        }
        if (owner.transform.IsChildOf(expected.transform)) return true;
        var navigable = navigation.TryCast<Navigable>();
        return IsAlive(navigable) && IsVisibleOwner(navigable.ActiveSelectable) &&
            navigable.ActiveSelectable.transform.IsChildOf(expected.transform);
    }

    private static Behaviour FindOwner(GameObject element)
    {
        var panel = element.GetComponentInParent<Panel>();
        if (IsVisibleOwner(panel) && IsContentPanel(panel)) return panel;
        Behaviour owner = element.GetComponentInParent<DungeonEnterPanelUI>();
        if (!IsAlive(owner)) owner = element.GetComponentInParent<DungeonFloorExitPanelUI>();
        if (!IsAlive(owner)) owner = element.GetComponentInParent<LE.UI.Dungeons.BossConfirmationPanelUI>();
        return IsVisibleOwner(owner) ? owner : null;
    }

    private static bool IsContentPanel(Panel panel)
    {
        var modal = panel.TryCast<IModal>();
        return modal is not null && !modal.WasCollected ||
            IsAlive(panel.TryCast<WarningConfirmationModal>()) || IsAlive(panel.TryCast<AlertModal>()) ||
            IsAlive(panel.TryCast<LightlessArborEntrancePanel>()) || IsAlive(panel.TryCast<SoulfireBastionEntrancePanel>()) ||
            IsAlive(panel.TryCast<TemporalSanctumEntrancePanel>()) || IsAlive(panel.TryCast<DungeonFloorExitPanel>()) ||
            IsAlive(panel.TryCast<BossConfirmationPanel>());
    }

    private static bool IsVisibleOwner(Behaviour owner) =>
        IsAlive(owner) && owner.isActiveAndEnabled && UiVisibility.IsVisible(owner.gameObject);
}
