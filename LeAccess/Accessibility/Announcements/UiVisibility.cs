using UnityEngine;

namespace LeAccess.Accessibility.Announcements;

internal static class UiVisibility
{
    internal static bool IsVisible(GameObject element)
    {
        try
        {
            return element is not null && !element.WasCollected && element != null && HasVisibleHierarchy(element);
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static bool HasVisibleHierarchy(GameObject element)
    {
        if (!element.activeInHierarchy) return false;

        bool checkGroups = true;
        for (var current = element.transform; current != null; current = current.parent)
        {
            var canvas = current.GetComponent<Canvas>();
            if (canvas != null && !canvas.isActiveAndEnabled) return false;
            if (!checkGroups) continue;

            bool ignoreParents = false;
            foreach (var group in current.GetComponents<CanvasGroup>())
            {
                if (group == null || !group.isActiveAndEnabled) continue;
                if (group.alpha <= 0) return false;
                ignoreParents |= group.ignoreParentGroups;
            }
            if (ignoreParents) checkGroups = false;
        }

        return true;
    }
}
