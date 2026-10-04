using System;
using LE.UI.PanelSystem;
using LeAccess.Accessibility.Quests;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Resolves the open world map through the registered navigable, never a scene search.</summary>
internal static class OpenWorldMap
{
    private static string _lastFailure;

    /// <summary>Polls the open map's selection; a failure resets it so the rest of the frame still runs.</summary>
    internal static void AnnounceSelection()
    {
        try { if (MapSelectionAnnouncer.Update(Find(), QuestAccessibility.MapMovePending)) QuestAccessibility.TakeMapMove(); }
        catch (Exception ex)
        {
            MapSelectionAnnouncer.Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"World map selection unavailable: {ex}");
        }
    }

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
