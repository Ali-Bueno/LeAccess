using System.Collections.Generic;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

internal static class EnemyScanProbe
{
    internal static string LastFailure => HostileRegistry.LastFailure;

    // Collects living hostiles within range; false when the player or registry is unavailable.
    internal static bool TryRead(bool online, float range, List<EnemyContact> contacts, out System.Numerics.Vector3 origin,
        bool readNames = true)
    {
        origin = default;
        float rangeSquared = range * range;
        if (online)
        {
            var player = LocalPlayerSource.Visuals;
            if (!IsAlive(player)) return false;
            var position = player.transform.position;
            origin = GuidanceRouteProbe.Convert(position);
            return HostileRegistry.Visit(player, (enemy, own) =>
            {
                if (!HostileRegistry.IsLivingFoe(enemy, own) || !InRange(enemy.transform.position, position, rangeSquared)) return;
                var data = enemy.Data;
                contacts.Add(new EnemyContact(enemy.GetInstanceID(),
                    readNames ? NearestEnemyProbe.ReadName(enemy.BaseDisplayInformation) : null,
                    IsAlive(data) && data.isBossOrMiniBoss(), GuidanceRouteProbe.Convert(enemy.transform.position)));
            });
        }
        var actor = PlayerFinder.playerActorComponent;
        if (!IsAlive(actor)) return false;
        var actorPosition = actor.position();
        origin = GuidanceRouteProbe.Convert(actorPosition);
        return HostileRegistry.Visit(actor, (enemy, own) =>
        {
            if (!HostileRegistry.IsLivingFoe(enemy, own) || !InRange(enemy.position(), actorPosition, rangeSquared)) return;
            contacts.Add(new EnemyContact(enemy.GetInstanceID(), readNames ? NearestEnemyProbe.ReadOfflineName(enemy) : null,
                enemy.isBossOrMiniboss(), GuidanceRouteProbe.Convert(enemy.position())));
        });
    }

    private static bool InRange(UnityEngine.Vector3 enemy, UnityEngine.Vector3 player, float rangeSquared) =>
        NearestEnemySelection.Contains(rangeSquared, (enemy - player).sqrMagnitude);
}
