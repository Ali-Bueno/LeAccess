using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Resolves the open world map through the registered navigable, never a scene search.</summary>
internal static class OpenWorldMap
{
    internal static MapPanel Find()
    {
        var manager = NavigationManager.instance;
        if (!IsAlive(manager) || !manager.GetNavigableFromID(NavigableID.WORLD_MAP_PANEL, out var navigable) ||
            navigable is null || navigable.WasCollected) return null;
        var component = navigable.TryCast<Behaviour>();
        if (!IsAlive(component)) return null;
        var panel = component.GetComponentInParent<WorldMapPanel>();
        return IsAlive(panel) ? panel._mapPanel : component.GetComponentInParent<MapPanel>();
    }
}
