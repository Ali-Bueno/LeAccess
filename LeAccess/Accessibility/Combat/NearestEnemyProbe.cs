using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

internal static class NearestEnemyProbe
{
    internal static string LastStatus { get; private set; }
    internal static EnemyClass LastClass { get; private set; }
    internal static string LastFailure => HostileRegistry.LastFailure;

    internal static string FindName(ActorVisuals player, float range)
    {
        LastStatus = "online visuals unavailable";
        LastClass = EnemyClass.Normal;
        var selection = new NearestEnemySelection(range);
        ActorVisuals nearest = null;
        int examined = 0, living = 0, inRange = 0;
        float rangeSquared = range * range;
        var position = IsAlive(player) ? player.transform.position : default;

        // The game's maintained registry includes online enemies, without searching the Unity scene.
        bool walked = HostileRegistry.Visit(player, (enemy, own) =>
        {
            examined++;
            if (!HostileRegistry.IsLivingFoe(enemy, own)) return;
            living++;
            float distanceSquared = (enemy.transform.position - position).sqrMagnitude;
            if (NearestEnemySelection.Contains(rangeSquared, distanceSquared)) inRange++;
            if (selection.Consider(enemy.GetInstanceID(), distanceSquared)) nearest = enemy;
        });
        if (!walked) return null;
        LastStatus = $"registry=online visuals examined={examined} living={living} in_range={inRange}";
        if (!IsAlive(nearest)) return null;
        string name = ReadName(nearest.BaseDisplayInformation);
        LastClass = EnemyClassProbe.Read(nearest);
        if (string.IsNullOrWhiteSpace(name)) LastStatus += " nearest_name=unavailable";
        return name;
    }

    internal static string FindName(Actor player, float range)
    {
        LastStatus = "offline actors unavailable";
        LastClass = EnemyClass.Normal;
        var selection = new NearestEnemySelection(range);
        Actor nearest = null;
        int examined = 0, living = 0, inRange = 0;
        float rangeSquared = range * range;
        var position = IsAlive(player) ? player.position() : default;

        // ActorManager.GetTargetableActors uses actors offline and visuals on an online client.
        bool walked = HostileRegistry.Visit(player, (enemy, own) =>
        {
            examined++;
            if (!HostileRegistry.IsLivingFoe(enemy, own)) return;
            living++;
            float distanceSquared = (enemy.position() - position).sqrMagnitude;
            if (NearestEnemySelection.Contains(rangeSquared, distanceSquared)) inRange++;
            if (selection.Consider(enemy.GetInstanceID(), distanceSquared)) nearest = enemy;
        });
        if (!walked) return null;
        LastStatus = $"registry=offline actors examined={examined} living={living} in_range={inRange}";
        if (!IsAlive(nearest)) return null;
        string name = ReadOfflineName(nearest, out string source);
        LastClass = EnemyClassProbe.Read(nearest);
        LastStatus += " " + source;
        return name;
    }

    internal static string ReadOfflineName(Actor actor) => ReadOfflineName(actor, out _);

    // The source is diagnostics only; callers that poll many actors discard it.
    private static string ReadOfflineName(Actor actor, out string source)
    {
        string name = CleanDisplay(actor.GetComponent<BaseDisplayInformation>());
        if (!string.IsNullOrWhiteSpace(name)) { source = "name_source=actor_display"; return name; }
        // Modern offline simulation actors and their visual display live on separate objects.
        var sync = actor.actorSync;
        var linkedActor = IsAlive(sync) ? sync.gameplayActor : null;
        var visuals = IsAlive(linkedActor) && linkedActor.Pointer == actor.Pointer ? sync.actorVisuals : null;
        if (IsAvailable(visuals, actor.gameObject.scene) && !visuals.dead)
        {
            name = CleanDisplay(visuals.BaseDisplayInformation);
            if (!string.IsNullOrWhiteSpace(name)) { source = "name_source=owned_visuals"; return name; }
        }
        var data = actor.data;
        // The native method falls back to Unity object names when actorName is empty.
        if (IsAlive(data) && !string.IsNullOrWhiteSpace(data.actorName))
        {
            name = TextUtils.CleanText(data.GetLocalizedActorName());
            if (!string.IsNullOrWhiteSpace(name)) { source = "name_source=actor_data"; return name; }
        }
        source = $"nearest_name=unavailable visuals={(IsAlive(visuals) ? "present" : "missing")} actor_data={(IsAlive(data) ? "present" : "missing")}";
        return null;
    }

    private static string CleanDisplay(BaseDisplayInformation display)
        => IsAlive(display) ? TextUtils.CleanText(display.GetLocalizedName()) : null;

    internal static bool IsAvailable(Behaviour component, Scene scene) =>
        scene.IsValid() && scene.isLoaded && IsAlive(component) && component.isActiveAndEnabled &&
        IsAlive(component.gameObject) && component.gameObject.activeInHierarchy &&
        component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;

    internal static string ReadName(BaseDisplayInformation display) => CleanDisplay(display);
}
