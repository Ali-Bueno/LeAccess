using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

internal readonly record struct GroundLootSnapshot(
    int LabelId, uint DropId, long ItemPointer, string Name);

internal static class GroundLootProbe
{
    internal static bool TryCapture(out GroundLootSnapshot snapshot)
    {
        snapshot = default;
        if (!IsAlive(PlayerFinder.player) || !PlayerFinder.player.activeInHierarchy || !GameplayInputState.OwnsInput())
            return false;

        var label = FocusedLabel();
        if (!IsAlive(label) || !label.isActiveAndEnabled || !UiVisibility.IsVisible(label.gameObject))
            return false;
        var text = label.itemText;
        if (!IsAlive(text) || !text.isActiveAndEnabled || text.alpha <= 0 ||
            !UiVisibility.IsVisible(text.gameObject) || IsAlive(text.canvasRenderer) && text.canvasRenderer.cull)
            return false;
        string name = TextUtils.CleanText(text.text);
        if (string.IsNullOrWhiteSpace(name)) return false;

        uint dropId = 0;
        long itemPointer = 0;
        var ground = label.TryCast<GroundItemLabel>();
        if (IsAlive(ground))
        {
            var visuals = ground.visuals;
            if (!IsAlive(visuals) || !visuals.isActiveAndEnabled) return false;
            var data = visuals.itemData;
            if (data is null || data.WasCollected) return false;
            dropId = visuals.id;
            itemPointer = data.Pointer.ToInt64();
        }

        // Automatic ground focus stays brief; full item properties belong to deliberate inventory inspection.
        snapshot = new(label.GetInstanceID(), dropId, itemPointer, name);
        return true;
    }

    private static PickupableGroundLabel FocusedLabel()
    {
        if (EpochInputManager.IsControllerActive)
        {
            var manager = EpochInputManager.instance;
            if (!IsAlive(manager) || !manager.isActiveAndEnabled) return null;
            var targeting = manager.InteractableTargeting;
            if (targeting is null || targeting.WasCollected) return null;
            var target = targeting.ActiveTarget;
            if (target is null || target.WasCollected || target.type != Targetable.TargetType.GroundLabel)
                return null;
            var item = target.item;
            return IsAlive(item) ? item.TryCast<PickupableGroundLabel>() : null;
        }

        var highlighted = TooltipItem.highlightedTooltipItem;
        return IsAlive(highlighted) && highlighted.isActiveAndEnabled &&
            highlighted.slotType == TooltipItemManager.SlotType.GROUND
            ? highlighted.GetComponentInParent<PickupableGroundLabel>() : null;
    }

}
