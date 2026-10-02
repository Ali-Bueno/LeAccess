using System;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

internal static class NearestEnemyProbe
{
    internal static string LastStatus { get; private set; }
    internal static string LastFailure { get; private set; }

    internal static string FindName(ActorVisuals player, float range)
    {
        LastStatus = "online visuals unavailable";
        LastFailure = null;
        var manager = ActorManager.instance;
        if (!IsAlive(manager) || !IsAlive(player)) return null;
        var groups = manager.visuals;
        var alignment = player.getAlignment();
        if (groups == null || groups.WasCollected || !IsAlive(alignment)) return null;

        var position = player.transform.position;
        var selection = new NearestEnemySelection(range);
        ActorVisuals nearest = null;
        int examined = 0, living = 0, inRange = 0;

        // The game's maintained registry includes online enemies, without searching the Unity scene.
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
                    examined++;
                    var enemy = enemies[index];
                    if (!IsAlive(enemy) || enemy.Pointer == player.Pointer || !enemy.isActiveAndEnabled ||
                        enemy.dead || enemy.isInvalidTarget()) continue;
                    var enemyAlignment = enemy.getAlignment();
                    if (!IsAlive(enemyAlignment) || !alignment.IsFoe(enemyAlignment)) continue;
                    living++;
                    float distanceSquared = (enemy.transform.position - position).sqrMagnitude;
                    if (float.IsFinite(distanceSquared) && distanceSquared <= range * range) inRange++;
                    if (selection.Consider(enemy.GetInstanceID(), distanceSquared)) nearest = enemy;
                }
                catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
            }
        }

        LastStatus = $"registry=online visuals examined={examined} living={living} in_range={inRange}";
        if (!IsAlive(nearest)) return null;
        return ReadName(nearest.BaseDisplayInformation);
    }

    internal static string FindName(Actor player, float range)
    {
        LastStatus = "offline actors unavailable";
        LastFailure = null;
        var manager = ActorManager.instance;
        if (!IsAlive(manager) || !IsAlive(player)) return null;
        var groups = manager.actors;
        var alignment = player.GetAlignment();
        if (groups is null || groups.WasCollected || !IsAlive(alignment)) return null;

        var position = player.position();
        var selection = new NearestEnemySelection(range);
        Actor nearest = null;
        int examined = 0, living = 0, inRange = 0;

        // ActorManager.GetTargetableActors uses actors offline and visuals on an online client.
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
                    examined++;
                    var enemy = enemies[index];
                    if (!IsAlive(enemy) || enemy.Pointer == player.Pointer || !enemy.isActiveAndEnabled ||
                        enemy.isInvalidTarget()) continue;
                    var enemyAlignment = enemy.GetAlignment();
                    if (!IsAlive(enemyAlignment) || !alignment.IsFoe(enemyAlignment)) continue;
                    living++;
                    float distanceSquared = (enemy.position() - position).sqrMagnitude;
                    if (float.IsFinite(distanceSquared) && distanceSquared <= range * range) inRange++;
                    if (selection.Consider(enemy.GetInstanceID(), distanceSquared)) nearest = enemy;
                }
                catch (Exception ex) { LastFailure = ex.GetType().Name + ": " + ex.Message; }
            }
        }

        LastStatus = $"registry=offline actors examined={examined} living={living} in_range={inRange}";
        return IsAlive(nearest) ? ReadOfflineName(nearest) : null;
    }

    private static string ReadOfflineName(Actor actor)
    {
        string name = CleanDisplay(actor.GetComponent<BaseDisplayInformation>());
        if (!string.IsNullOrWhiteSpace(name)) { LastStatus += " name_source=actor_display"; return name; }
        // Modern offline simulation actors and their visual display live on separate objects.
        var sync = actor.actorSync;
        var linkedActor = IsAlive(sync) ? sync.gameplayActor : null;
        var visuals = IsAlive(linkedActor) && linkedActor.Pointer == actor.Pointer ? sync.actorVisuals : null;
        if (IsAlive(visuals) && visuals.isActiveAndEnabled && !visuals.dead)
        {
            name = CleanDisplay(visuals.BaseDisplayInformation);
            if (!string.IsNullOrWhiteSpace(name)) { LastStatus += " name_source=owned_visuals"; return name; }
        }
        var data = actor.data;
        // The native method falls back to Unity object names when actorName is empty.
        if (IsAlive(data) && !string.IsNullOrWhiteSpace(data.actorName))
        {
            name = TextUtils.CleanText(data.GetLocalizedActorName());
            if (!string.IsNullOrWhiteSpace(name)) { LastStatus += " name_source=actor_data"; return name; }
        }
        LastStatus += $" nearest_name=unavailable visuals={(IsAlive(visuals) ? "present" : "missing")} actor_data={(IsAlive(data) ? "present" : "missing")}";
        return null;
    }

    private static string CleanDisplay(BaseDisplayInformation display)
        => IsAlive(display) ? TextUtils.CleanText(display.GetLocalizedName()) : null;

    private static string ReadName(BaseDisplayInformation display)
    {
        string name = CleanDisplay(display);
        if (string.IsNullOrWhiteSpace(name)) LastStatus += " nearest_name=unavailable";
        return name;
    }
}
