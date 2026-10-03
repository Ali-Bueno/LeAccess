using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Vitals;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

// Registries as in NearestEnemyProbe; see reference/boss-life.md for the health sources.
internal static class BossLifeProbe
{
    internal static string LastFailure { get; private set; }

    internal static List<BossSample> Read(ActorVisuals player)
    {
        var samples = new List<BossSample>();
        LastFailure = null;
        var manager = ActorManager.instance;
        var scene = SceneManager.GetActiveScene();
        if (!IsAlive(manager) || !NearestEnemyProbe.IsAvailable(player, scene)) return null;
        var groups = manager.visuals;
        var alignment = player.getAlignment();
        if (groups == null || groups.WasCollected || !IsAlive(alignment)) return null;
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
                    var data = enemy.Data;
                    if (!IsAlive(data) || !data.isBossOrMiniBoss()) continue;
                    string name = NearestEnemyProbe.ReadName(enemy.BaseDisplayInformation);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    // IOverheadBar.HealthNormalized is the 0..1 fill of the boss bar.
                    var bar = enemy.TryCast<IOverheadBar>();
                    if (bar == null) continue;
                    int percent = VitalsAnnouncementState.Percent(bar.HealthNormalized, 1);
                    samples.Add(new BossSample(enemy.GetInstanceID(), name, percent, enemy.dead));
                }
                catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
            }
        }
        return samples;
    }

    internal static List<BossSample> Read(Actor player)
    {
        var samples = new List<BossSample>();
        LastFailure = null;
        var manager = ActorManager.instance;
        var scene = SceneManager.GetActiveScene();
        if (!IsAlive(manager) || !NearestEnemyProbe.IsAvailable(player, scene)) return null;
        var groups = manager.actors;
        var alignment = player.GetAlignment();
        if (groups is null || groups.WasCollected || !IsAlive(alignment)) return null;
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
                    if (!NearestEnemyProbe.IsAvailable(enemy, scene) || enemy.Pointer == player.Pointer ||
                        !enemy.isBossOrMiniboss()) continue;
                    var health = enemy.health;
                    if (!IsAlive(health)) continue;
                    string name = NearestEnemyProbe.ReadOfflineName(enemy);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    float current = health.currentHealth;
                    int percent = VitalsAnnouncementState.Percent(current, health.maxHealth);
                    samples.Add(new BossSample(enemy.GetInstanceID(), name, percent, current <= 0));
                }
                catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
            }
        }
        return samples;
    }
}
