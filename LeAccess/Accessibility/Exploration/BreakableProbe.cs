using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.I18n;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Vector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Exploration;

// Barrels, crates and pots: the actors the game files under Alignment.ID.Barrel in ActorManager. See reference/exploration-mode.md.
internal static class BreakableProbe
{
    private const float MinimumArrival = 1.5f;
    private static string _logged;

    internal static string LastFailure { get; private set; }

    internal static int Add(bool online, Func<Vector3, float, ExplorationCategory, bool> canSee, List<ExplorationEntry> entries)
    {
        LastFailure = null;
        var manager = ActorManager.instance;
        var scene = SceneManager.GetActiveScene();
        if (!IsAlive(manager) || !scene.IsValid() || !scene.isLoaded) return 0;
        string generic = ModText.Get("exploration.breakable");
        int standing = 0, listed = 0;
        if (online)
        {
            var groups = manager.visuals;
            if (groups is null || groups.WasCollected) return 0;
            foreach (var group in groups)
            {
                if (group is null || group.WasCollected || !IsAlive(group.alignment) || group.alignment.id != Alignment.ID.Barrel) continue;
                var props = group.visuals;
                if (props is null || props.WasCollected) continue;
                for (int index = 0; index < props.Count; index++)
                {
                    try
                    {
                        var prop = props[index];
                        if (!NearestEnemyProbe.IsAvailable(prop, scene) || prop.dead || prop.isInvalidTarget()) continue;
                        standing++;
                        var position = GuidanceRouteProbe.Convert(prop.transform.position);
                        if (!BreakableSelection.Listed(true, canSee(position, 0, ExplorationCategory.Breakable))) continue;
                        entries.Add(Entry(prop.GetInstanceID(), NearestEnemyProbe.ReadName(prop.BaseDisplayInformation), generic, position));
                        listed++;
                    }
                    catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
                }
            }
        }
        else
        {
            var groups = manager.actors;
            if (groups is null || groups.WasCollected) return 0;
            foreach (var group in groups)
            {
                if (group is null || group.WasCollected || !IsAlive(group.alignment) || group.alignment.id != Alignment.ID.Barrel) continue;
                var props = group.actors;
                if (props is null || props.WasCollected) continue;
                for (int index = 0; index < props.Count; index++)
                {
                    try
                    {
                        var prop = props[index];
                        if (!NearestEnemyProbe.IsAvailable(prop, scene) || prop.isInvalidTarget()) continue;
                        standing++;
                        var position = GuidanceRouteProbe.Convert(prop.position());
                        if (!BreakableSelection.Listed(true, canSee(position, 0, ExplorationCategory.Breakable))) continue;
                        entries.Add(Entry(prop.GetInstanceID(), NearestEnemyProbe.ReadOfflineName(prop), generic, position));
                        listed++;
                    }
                    catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
                }
            }
        }
        string line = $"{listed} (standing {standing})";
        if (line != _logged)
        {
            _logged = line;
            Plugin.Log.LogInfo($"[Exploration] breakables: {line}");
        }
        return listed;
    }

    private static ExplorationEntry Entry(int id, string gameName, string generic, Vector3 position) =>
        new($"breakable:{id}", BreakableSelection.Name(gameName, generic), ExplorationCategory.Breakable, position, MinimumArrival);

    internal static void Reset() => _logged = null;
}
