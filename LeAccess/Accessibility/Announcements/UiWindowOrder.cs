using System.Collections.Generic;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class UiWindowOrder
{
    internal static bool TryCompare(GameObject first, GameObject second, out int order)
    {
        order = 0;
        if (!IsAlive(first) || !IsAlive(second)) return false;
        if (first.GetInstanceID() == second.GetInstanceID()) return true;
        var a = SortingCanvas(first);
        var b = SortingCanvas(second);
        if (!IsAlive(a) || !IsAlive(b)) return false;
        if (a.GetInstanceID() != b.GetInstanceID())
        {
            // Overlay canvases have an engine-computed draw order; other camera/depth combinations stay unknown.
            if (a.renderMode != RenderMode.ScreenSpaceOverlay || b.renderMode != RenderMode.ScreenSpaceOverlay) return false;
            order = a.renderOrder.CompareTo(b.renderOrder);
            if (order != 0) return true;
            order = SortingLayer.GetLayerValueFromID(a.sortingLayerID).CompareTo(SortingLayer.GetLayerValueFromID(b.sortingLayerID));
            if (order == 0) order = a.sortingOrder.CompareTo(b.sortingOrder);
            if (order != 0) return true;
        }
        var ancestors = new HashSet<int>();
        for (var t = first.transform; IsAlive(t); t = t.parent) ancestors.Add(t.GetInstanceID());
        var common = second.transform;
        while (IsAlive(common) && !ancestors.Contains(common.GetInstanceID())) common = common.parent;
        if (!IsAlive(common)) return false;
        if (common.GetInstanceID() == first.transform.GetInstanceID()) { order = -1; return true; }
        if (common.GetInstanceID() == second.transform.GetInstanceID()) { order = 1; return true; }
        var left = first.transform;
        var right = second.transform;
        while (left.parent.GetInstanceID() != common.GetInstanceID()) left = left.parent;
        while (right.parent.GetInstanceID() != common.GetInstanceID()) right = right.parent;
        order = left.GetSiblingIndex().CompareTo(right.GetSiblingIndex());
        return order != 0;
    }

    private static Canvas SortingCanvas(GameObject root)
    {
        var canvas = root.GetComponentInParent<Canvas>();
        while (IsAlive(canvas) && !canvas.overrideSorting)
        {
            var parent = canvas.transform.parent;
            var outer = IsAlive(parent) ? parent.GetComponentInParent<Canvas>() : null;
            if (!IsAlive(outer)) break;
            canvas = outer;
        }
        return canvas;
    }
}
