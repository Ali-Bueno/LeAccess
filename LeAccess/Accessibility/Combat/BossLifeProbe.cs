using System.Collections.Generic;
using LeAccess.Accessibility.Vitals;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

// Registry walk is HostileRegistry; see reference/boss-life.md for the health sources.
internal static class BossLifeProbe
{
    internal static string LastFailure => HostileRegistry.LastFailure;

    internal static List<BossSample> Read(ActorVisuals player)
    {
        var samples = new List<BossSample>();
        bool walked = HostileRegistry.Visit(player, (enemy, _) =>
        {
            var data = enemy.Data;
            if (!IsAlive(data) || !data.isBossOrMiniBoss()) return;
            string name = NearestEnemyProbe.ReadName(enemy.BaseDisplayInformation);
            if (string.IsNullOrWhiteSpace(name)) return;
            // IOverheadBar.HealthNormalized is the 0..1 fill of the boss bar.
            var bar = enemy.TryCast<IOverheadBar>();
            if (bar == null) return;
            int percent = VitalsAnnouncementState.Percent(bar.HealthNormalized, 1);
            samples.Add(new BossSample(enemy.GetInstanceID(), name, percent, enemy.dead));
        });
        return walked ? samples : null;
    }

    internal static List<BossSample> Read(Actor player)
    {
        var samples = new List<BossSample>();
        bool walked = HostileRegistry.Visit(player, (enemy, _) =>
        {
            if (!enemy.isBossOrMiniboss()) return;
            var health = enemy.health;
            if (!IsAlive(health)) return;
            string name = NearestEnemyProbe.ReadOfflineName(enemy);
            if (string.IsNullOrWhiteSpace(name)) return;
            float current = health.currentHealth;
            int percent = VitalsAnnouncementState.Percent(current, health.maxHealth);
            samples.Add(new BossSample(enemy.GetInstanceID(), name, percent, current <= 0));
        });
        return walked ? samples : null;
    }
}
