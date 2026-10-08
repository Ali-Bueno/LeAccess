using System.Collections.Generic;
using LeAccess.Accessibility.Vitals;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

// Registry walk is HostileRegistry; see reference/boss-life.md for the health sources.
internal static class BossLifeProbe
{
    internal static string LastFailure => HostileRegistry.LastFailure;

    // A sighted player follows a boss's bar while the boss is on screen; without a camera all are audible.
    internal static bool OnScreen(Camera camera, Vector3 position)
    {
        if (camera == null) return true;
        var viewport = camera.WorldToViewportPoint(position);
        return viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
    }

    // Elites are rare and champion enemies; they are narrated like bosses, one at a time (BossLifeState).
    private static bool Narrated(EnemyClass enemyClass, bool elites) =>
        enemyClass == EnemyClass.Boss || elites && EnemyClassifier.IsElite(enemyClass);

    internal static List<BossSample> Read(ActorVisuals player, Camera camera, bool elites = true)
    {
        var samples = new List<BossSample>();
        var origin = IsAlive(player) ? player.transform.position : default;
        bool walked = HostileRegistry.Visit(player, (enemy, _) =>
        {
            var enemyClass = EnemyClassProbe.Read(enemy);
            if (!Narrated(enemyClass, elites)) return;
            string name = NearestEnemyProbe.ReadName(enemy.BaseDisplayInformation);
            if (string.IsNullOrWhiteSpace(name)) return;
            // IOverheadBar.HealthNormalized is the 0..1 fill of the boss bar.
            var bar = enemy.TryCast<IOverheadBar>();
            if (bar == null) return;
            int percent = VitalsAnnouncementState.Percent(bar.HealthNormalized, 1);
            samples.Add(new BossSample(enemy.GetInstanceID(), name, percent, enemy.dead,
                OnScreen(camera, enemy.transform.position), EnemyClassifier.IsElite(enemyClass),
                (enemy.transform.position - origin).sqrMagnitude));
        });
        return walked ? samples : null;
    }

    internal static List<BossSample> Read(Actor player, Camera camera, bool elites = true)
    {
        var samples = new List<BossSample>();
        var origin = IsAlive(player) ? player.position() : default;
        bool walked = HostileRegistry.Visit(player, (enemy, _) =>
        {
            var enemyClass = EnemyClassProbe.Read(enemy);
            if (!Narrated(enemyClass, elites)) return;
            var health = enemy.health;
            if (!IsAlive(health)) return;
            string name = NearestEnemyProbe.ReadOfflineName(enemy);
            if (string.IsNullOrWhiteSpace(name)) return;
            float current = health.currentHealth;
            int percent = VitalsAnnouncementState.Percent(current, health.maxHealth);
            samples.Add(new BossSample(enemy.GetInstanceID(), name, percent, current <= 0, OnScreen(camera, enemy.position()),
                EnemyClassifier.IsElite(enemyClass), (enemy.position() - origin).sqrMagnitude));
        });
        return walked ? samples : null;
    }
}
