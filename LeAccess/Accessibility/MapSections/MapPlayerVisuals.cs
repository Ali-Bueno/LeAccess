using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class MapPlayerVisuals
{
    internal static bool TryResolve(GameObject player, int scene, out ActorVisuals visuals)
    {
        visuals = null;
        bool linked = false;
        var actor = PlayerFinder.playerActorComponent;
        if (IsAlive(actor))
        {
            if (!Available(actor, scene) || !actor.transform.IsChildOf(player.transform))
            { MapPlaceDiagnostics.Report("actor-owner-mismatch", "cached local Actor does not belong to the current player"); return false; }
            var sync = actor.actorSync;
            if (IsAlive(sync))
            {
                if (!Available(sync, scene) || !Same(sync.gameplayActor, actor))
                { MapPlaceDiagnostics.Report("sync-owner-mismatch", "cached ActorSync does not point back to the current local Actor"); return false; }
                visuals = sync.actorVisuals;
                linked = true;
            }
        }
        if (!linked)
        {
            visuals = PlayerFinder.playerActorVisuals;
            if (!Available(visuals, scene) || !visuals.isActiveAndEnabled || !visuals.transform.IsChildOf(player.transform))
            { MapPlaceDiagnostics.Report("visual-owner-unavailable", "neither cached ActorSync ownership nor player-owned ActorVisuals is available"); return false; }
        }
        if (!Available(visuals, scene) || !visuals.isActiveAndEnabled || visuals.dead)
        {
            string detail = IsAlive(visuals)
                ? $"enabled={visuals.isActiveAndEnabled}, dead={visuals.dead}, scene={(int)visuals.gameObject.scene.handle}, expectedScene={scene}"
                : "owned ActorVisuals is absent or destroyed";
            MapPlaceDiagnostics.Report("visual-inactive", detail);
            return false;
        }
        return true;
    }

    private static bool Same(UnityEngine.Object a, UnityEngine.Object b) => IsAlive(a) && IsAlive(b) && a.Pointer == b.Pointer;
    private static bool Available(Component component, int scene) => IsAlive(component) &&
        IsAlive(component.gameObject) && component.gameObject.activeInHierarchy && (int)component.gameObject.scene.handle == scene;
}
