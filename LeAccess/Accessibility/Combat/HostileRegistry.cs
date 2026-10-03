using System;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

// Walks the game's hostile registry for the current mode; see reference/nearest-enemy.md.
internal static class HostileRegistry
{
    internal static string LastFailure { get; private set; }

    internal static bool IsLivingFoe(ActorVisuals enemy, Alignment own)
    {
        if (enemy.dead || enemy.isInvalidTarget()) return false;
        var alignment = enemy.getAlignment();
        return IsAlive(alignment) && own.IsFoe(alignment);
    }

    internal static bool IsLivingFoe(Actor enemy, Alignment own)
    {
        if (enemy.isInvalidTarget()) return false;
        var alignment = enemy.GetAlignment();
        return IsAlive(alignment) && own.IsFoe(alignment);
    }

    // Visits available foe-group entries except the player; false when the registry is unavailable.
    internal static bool Visit(ActorVisuals player, Action<ActorVisuals, Alignment> visit)
    {
        LastFailure = null;
        var manager = ActorManager.instance;
        var scene = SceneManager.GetActiveScene();
        if (!IsAlive(manager) || !NearestEnemyProbe.IsAvailable(player, scene)) return false;
        var groups = manager.visuals;
        var alignment = player.getAlignment();
        if (groups == null || groups.WasCollected || !IsAlive(alignment)) return false;
        foreach (var group in groups)
        {
            if (group == null || group.WasCollected || !IsAlive(group.alignment) ||
                !alignment.IsFoe(group.alignment)) continue;
            var enemies = group.visuals;
            if (enemies == null || enemies.WasCollected) continue;
            for (int index = 0; index < enemies.Count; index++)
            {
                try
                {
                    var enemy = enemies[index];
                    if (!NearestEnemyProbe.IsAvailable(enemy, scene) || enemy.Pointer == player.Pointer) continue;
                    visit(enemy, alignment);
                }
                catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
            }
        }
        return true;
    }

    internal static bool Visit(Actor player, Action<Actor, Alignment> visit)
    {
        LastFailure = null;
        var manager = ActorManager.instance;
        var scene = SceneManager.GetActiveScene();
        if (!IsAlive(manager) || !NearestEnemyProbe.IsAvailable(player, scene)) return false;
        var groups = manager.actors;
        var alignment = player.GetAlignment();
        if (groups is null || groups.WasCollected || !IsAlive(alignment)) return false;
        foreach (var group in groups)
        {
            if (group is null || group.WasCollected || !IsAlive(group.alignment) ||
                !alignment.IsFoe(group.alignment)) continue;
            var enemies = group.actors;
            if (enemies is null || enemies.WasCollected) continue;
            for (int index = 0; index < enemies.Count; index++)
            {
                try
                {
                    var enemy = enemies[index];
                    if (!NearestEnemyProbe.IsAvailable(enemy, scene) || enemy.Pointer == player.Pointer) continue;
                    visit(enemy, alignment);
                }
                catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
            }
        }
        return true;
    }
}
