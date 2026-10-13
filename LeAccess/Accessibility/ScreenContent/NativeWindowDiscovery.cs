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
        var panel = foreground.TryCast<Panel>();
        GenericModalText modal = null;
        if (IsAlive(panel) && !IsExplicitPanel(panel))
        {
            Panels.CostModalReader.TryCreate(panel, out var skip, out var extra);
            modal = new GenericModalText(root, skip, extra);
        }
        else if (Login.LoginPrompts.TryFind(root, out var form) && form.Panel.Pointer == foreground.Pointer)
        {
            // The form's field reader speaks errors while the screen keyboard owns focus; the window reads them otherwise.
            modal = new GenericModalText(root, form.IsError, form.ReadError);
            Plugin.Log.LogInfo($"[Login] form: {root.name}");
        }
        var endgame = IsAlive(panel) ? Endgame.EndgameWindows.Reader(panel) : null;
        ScreenContentAnnouncer.Track(root, retainWhenHidden: true, separateWindow: true,
            readText: endgame ?? (modal is null ? null : modal.ReadOpening), ownsWindow: () => OwnsWindow(foreground),
            readUpdate: endgame ?? (modal is null ? null : modal.ReadHeader));
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
        if (!IsAlive(owner) && Login.LoginPrompts.TryFind(element, out var form)) owner = form.Panel;
        return IsVisibleOwner(owner) ? owner : null;
    }

    private static bool IsContentPanel(Panel panel)
    {
        var modal = panel.TryCast<IModal>();
        return modal is not null && !modal.WasCollected || IsExplicitPanel(panel);
    }

    // Explicit warning, alert and dungeon panels read their whole visible content; other IModal windows do not.
    private static bool IsExplicitPanel(Panel panel) =>
        IsAlive(panel.TryCast<WarningConfirmationModal>()) || IsAlive(panel.TryCast<AlertModal>()) ||
        IsAlive(panel.TryCast<LightlessArborEntrancePanel>()) || IsAlive(panel.TryCast<SoulfireBastionEntrancePanel>()) ||
        IsAlive(panel.TryCast<TemporalSanctumEntrancePanel>()) || IsAlive(panel.TryCast<DungeonFloorExitPanel>()) ||
        IsAlive(panel.TryCast<BossConfirmationPanel>()) || Endgame.EndgameWindows.Covers(panel);

    private static bool IsVisibleOwner(Behaviour owner) =>
        IsAlive(owner) && owner.isActiveAndEnabled && UiVisibility.IsVisible(owner.gameObject);
}
