using System.Collections.Generic;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class MapTransformResolver
{
    internal static Transform Find(IEnumerable<GameObject> roots, string path)
    {
        var parts = path.Split('/');
        Transform target = null;
        foreach (var root in roots)
            if (IsAlive(root) && root.name == parts[0])
            { if (IsAlive(target)) return null; target = root.transform; }
        if (!IsAlive(target)) return null;
        for (int depth = 1; depth < parts.Length; depth++)
        {
            Transform child = null;
            for (int index = 0; index < target.childCount; index++)
            {
                var candidate = target.GetChild(index);
                if (!IsAlive(candidate) || candidate.name != parts[depth]) continue;
                if (IsAlive(child)) return null;
                child = candidate;
            }
            if (!IsAlive(child)) return null;
            target = child;
        }
        return target;
    }

    internal static bool Matches(Transform target, string path, int sceneHandle)
    {
        if (!IsAlive(target) || !target.gameObject.activeInHierarchy ||
            (int)target.gameObject.scene.handle != sceneHandle) return false;
        var parts = path.Split('/');
        var ancestor = target;
        for (int depth = parts.Length - 1; depth >= 0; depth--)
        {
            if (!IsAlive(ancestor) || ancestor.name != parts[depth]) return false;
            ancestor = ancestor.parent;
        }
        return !IsAlive(ancestor);
    }
}
