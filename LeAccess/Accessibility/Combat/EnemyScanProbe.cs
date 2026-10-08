using System.Collections.Generic;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Players;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

internal static class EnemyScanProbe
{
    internal static string LastFailure => HostileRegistry.LastFailure;

    // Collects living hostiles within range, or anywhere in the camera's view when one is given;
    // false when the player or registry is unavailable.
    internal static bool TryRead(bool online, float range, List<EnemyContact> contacts, out System.Numerics.Vector3 origin,
        bool readNames = true, Camera view = null)
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
                if (!HostileRegistry.IsLivingFoe(enemy, own) || !Included(enemy.transform.position, position, rangeSquared, view)) return;
                contacts.Add(new EnemyContact(enemy.GetInstanceID(),
                    readNames ? NearestEnemyProbe.ReadName(enemy.BaseDisplayInformation) : null,
                    EnemyClassProbe.Read(enemy), GuidanceRouteProbe.Convert(enemy.transform.position)));
            });
        }
        var actor = PlayerFinder.playerActorComponent;
        if (!IsAlive(actor)) return false;
        var actorPosition = actor.position();
        origin = GuidanceRouteProbe.Convert(actorPosition);
        return HostileRegistry.Visit(actor, (enemy, own) =>
        {
            if (!HostileRegistry.IsLivingFoe(enemy, own) || !Included(enemy.position(), actorPosition, rangeSquared, view)) return;
            contacts.Add(new EnemyContact(enemy.GetInstanceID(), readNames ? NearestEnemyProbe.ReadOfflineName(enemy) : null,
                EnemyClassProbe.Read(enemy), GuidanceRouteProbe.Convert(enemy.position())));
        });
    }

    private static bool Included(UnityEngine.Vector3 enemy, UnityEngine.Vector3 player, float rangeSquared, Camera view) =>
        NearestEnemySelection.Contains(rangeSquared, (enemy - player).sqrMagnitude) ||
        (view != null && BossLifeProbe.OnScreen(view, enemy));
}
