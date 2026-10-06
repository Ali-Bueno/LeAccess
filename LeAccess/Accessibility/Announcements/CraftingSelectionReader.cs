using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class CraftingSelectionReader
{
    internal static bool TryReadSelected(INavigable navigation, out GameObject selected, out bool declared)
    {
        selected = null;
        declared = false;
        try
        {
            if (navigation is null || navigation.WasCollected) return false;
            var flyout = navigation.TryCast<CraftingMaterialSelectorPanel>();
            var materials = navigation.TryCast<CraftingRunesAndGlyphsPanel>();
            var forge = navigation.TryCast<CraftingSlotManager>();
            declared = IsAlive(flyout) || IsAlive(materials) || IsAlive(forge);
            if (!declared) return false;
            var owner = navigation.TryCast<Behaviour>();
            if (!IsAlive(owner) || !owner.isActiveAndEnabled || !UiVisibility.IsVisible(owner.gameObject)) return false;
            RectTransform rect;
            if (IsAlive(flyout))
            {
                var controls = flyout._selectables;
                var rects = flyout._selectableRectTs;
                int index = flyout._selectedButtonId;
                if (controls is null || controls.WasCollected || rects is null || rects.WasCollected ||
                    index < 0 || index >= controls.Count || index >= rects.Count) return false;
                var value = controls[index];
                var control = IsAlive(value) ? value.TryCast<Component>() : null;
                rect = rects[index];
                if (!IsAlive(control) || !IsAlive(rect) || !Within(control.gameObject, owner.gameObject) ||
                    !Within(rect.gameObject, control.gameObject)) return false;
                var behaviour = control.TryCast<Behaviour>();
                if (IsAlive(behaviour) && !behaviour.isActiveAndEnabled) return false;
            }
            else if (IsAlive(materials))
            {
                var material = ReadCell(materials._selectables, materials._selectedRowIndex, materials._selectedColumnIndex)?.TryCast<MaterialContainerUI>();
                if (!IsAlive(material) || !material.isActiveAndEnabled || !Within(material.gameObject, owner.gameObject)) return false;
                rect = material.rectTransform;
                if (!IsAlive(rect) || !Within(rect.gameObject, material.gameObject)) return false;
            }
            else
            {
                // FocusSelectable focuses activeNavigationElements (RectTransforms); selectables holds the cells' components.
                var index = forge.currentlySelectedIdx;
                rect = ReadCell(forge.activeNavigationElements, index.x, index.y)?.TryCast<RectTransform>();
            }
            if (!IsAlive(rect) || !Within(rect.gameObject, owner.gameObject)) return false;
            selected = rect.gameObject;
            return true;
        }
        catch (System.Exception) { selected = null; return false; }
    }

    private static Component ReadCell(Il2CppObjectBase raw, int first, int second)
    {
        if (raw is null || raw.WasCollected) return null;
        var cells = raw.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2 || first < 0 || second < 0 ||
            first >= cells.GetLength(0) || second >= cells.GetLength(1)) return null;
        var cell = cells.GetValue(first, second);
        return cell is not null && !cell.WasCollected ? cell.TryCast<Component>() : null;
    }

    private static bool Within(GameObject element, GameObject root) =>
        UiVisibility.IsVisible(element) && IsAlive(root) && element.transform.IsChildOf(root.transform);
}
