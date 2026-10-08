using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

// Reads an enemy's class from the game's display information; see reference/enemy-class.md.
internal static class EnemyClassProbe
{
    internal static EnemyClass Read(BaseDisplayInformation display, ActorData data, bool boss)
    {
        bool bossData = boss || IsAlive(data) && data.isBossOrMiniBoss();
        if (IsAlive(display))
        {
            var shown = display.actorClass;
            return EnemyClassifier.Classify(bossData || shown == DisplayActorClass.Boss, display.IsChampion(),
                shown == DisplayActorClass.Rare, shown == DisplayActorClass.Magic);
        }
        // Without display information only the actor data can tell.
        bool alive = IsAlive(data);
        return EnemyClassifier.Classify(bossData, alive && data.isChampion,
            alive && data.rareStatus == ActorData.RareStatus.Always, false);
    }

    internal static EnemyClass Read(ActorVisuals enemy) => Read(enemy.BaseDisplayInformation, enemy.Data, false);

    internal static EnemyClass Read(Actor enemy)
    {
        var display = enemy.GetComponent<BaseDisplayInformation>();
        if (!IsAlive(display))
        {
            // Offline simulation actors may keep their display on the linked visuals.
            var sync = enemy.actorSync;
            var visuals = IsAlive(sync) ? sync.actorVisuals : null;
            display = IsAlive(visuals) ? visuals.BaseDisplayInformation : null;
        }
        return Read(display, enemy.data, enemy.isBossOrMiniboss());
    }
}
