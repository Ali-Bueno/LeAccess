using LeAccess.Accessibility.Combat;
using UnityEngine;

internal static class NearestEnemyProbeTests
{
    internal static void Run(Action<bool, string> check)
    {
        var friendly = new Alignment { Id = 100 };
        var hostile = new Alignment { Id = 200 };
        friendly.Foe = hostile;
        var player = Enemy(1, "Player", friendly, 0);

        void Registry(params ActorVisuals[] enemies) => ActorManager.instance = new ActorManager
        {
            visuals = new NativeList<ActorManager.ActorVisualsList>(Group(hostile, enemies))
        };
        string Find() => NearestEnemyProbe.FindName(player, 10);

        var nearest = Enemy(2, "Lobo", hostile, 2);
        var farther = Enemy(3, "Oso", hostile, 7);
        farther.SelectedByAim = true;
        farther.CausedDamage = true;
        Registry(farther, nearest);
        check(Find() == "Lobo", "Nearest enemy wins independently of aim and damage source");
        check(farther.BaseDisplayInformation.Reads == 0, "Probe only reads the selected nearest enemy name");

        var closer = Enemy(4, "Excluded", hostile, 1);
        closer.dead = true;
        Registry(closer, nearest);
        check(Find() == "Lobo", "A dead nearest registry entry cannot hide the next living enemy");
        closer.dead = false;
        closer.InvalidTarget = true;
        check(Find() == "Lobo", "An invalid nearest target cannot mask a valid enemy");
        closer.InvalidTarget = false;
        closer.ThrowOnAlignment = true;
        check(Find() == "Lobo" && NearestEnemyProbe.LastFailure.Contains("broken visuals"),
            "Broken visual entry is diagnosed without masking the valid nearest neighbor");
        closer.ThrowOnAlignment = false;
        closer.isActiveAndEnabled = false;
        check(Find() == "Lobo", "Inactive enemy visuals are excluded");
        closer.isActiveAndEnabled = true;
        closer.Alignment = friendly;
        check(Find() == "Lobo", "Friendly visual in a stale hostile group is filtered");
        closer.Alignment = hostile;
        closer.WasCollected = true;
        check(Find() == "Lobo", "Collected visuals are rejected before Unity null or native access");
        closer.WasCollected = false;
        closer.Destroyed = true;
        check(Find() == "Lobo", "Destroyed Unity visual is excluded");
        closer.Destroyed = false;
        Registry(null, player, nearest);
        check(Find() == "Lobo", "Null entries and the local player are excluded");

        Registry(nearest, farther);
        nearest.BaseDisplayInformation.Name = null;
        check(Find() == null, "Missing nearest name never substitutes a farther enemy");
        nearest.BaseDisplayInformation.WasCollected = true;
        check(Find() == null, "Collected nearest display never substitutes a farther enemy");
        nearest.BaseDisplayInformation = null;
        check(Find() == null, "Missing nearest display never substitutes a farther enemy");
        nearest.BaseDisplayInformation = new BaseDisplayInformation { Name = "Lobo" };

        var tie = Enemy(5, "Tie", hostile, -2);
        Registry(tie, nearest);
        check(Find() == "Lobo", "Equal-distance tie prefers stable lower instance ID");
        Registry(nearest, tie);
        check(Find() == "Lobo", "Registry order changes do not change an equal-distance winner");
        nearest.transform.position = new Vector3(0, 3, 0);
        Registry(nearest, tie);
        check(Find() == "Tie", "Selection uses full Euclidean center distance");

        Registry(Enemy(6, "Outside", hostile, 11));
        check(Find() == null, "Enemy outside the supplied game targeting range is excluded");
        Registry(Enemy(6, "Boundary", hostile, 10));
        check(Find() == "Boundary", "Enemy exactly at the supplied range boundary is eligible");
        Registry(Enemy(6, "Invalid position", hostile, float.NaN), farther);
        check(Find() == "Oso", "Nonfinite nearest position cannot corrupt valid selection");

        var badGroup = Group(hostile, closer);
        badGroup.WasCollected = true;
        ActorManager.instance.visuals = new NativeList<ActorManager.ActorVisualsList>(
            null, badGroup, Group(friendly, closer), Group(hostile, farther));
        check(Find() == "Oso", "Null, collected and friendly groups are skipped");
        badGroup.WasCollected = false;
        badGroup.visuals.WasCollected = true;
        check(Find() == "Oso", "Collected enemy collections are skipped before indexing");
        ActorManager.instance.visuals.WasCollected = true;
        check(Find() == null, "Collected registry is ignored before enumeration");
        ActorManager.instance.WasCollected = true;
        check(Find() == null, "Collected registry owner is ignored before Unity null checks");
        ActorManager.instance = null;
        check(Find() == null, "Absent registry has no nearest enemy");

        foreach (float invalidRange in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.MaxValue })
        {
            var selection = new NearestEnemySelection(invalidRange);
            check(!selection.Consider(1, 0), "Invalid or overflowing game range cannot select enemies");
        }
        var validSelection = new NearestEnemySelection(10);
        check(!validSelection.Consider(1, -1), "Negative squared distance is rejected");
        check(!validSelection.Consider(1, float.PositiveInfinity), "Infinite distance is rejected");
        check(validSelection.Consider(5, 4), "First valid distance is selected");
        check(!validSelection.Consider(6, 4), "Larger ID loses an equal-distance tie");
        check(validSelection.Consider(4, 4), "Smaller ID wins an equal-distance tie");
        check(!validSelection.Consider(4, 4), "Duplicate identity does not replace itself");
    }

    private static ActorVisuals Enemy(int id, string name, Alignment alignment, float x) => new()
    {
        Id = id,
        Alignment = alignment,
        transform = new Transform { position = new Vector3(x, 0, 0) },
        BaseDisplayInformation = new BaseDisplayInformation { Name = name }
    };

    private static ActorManager.ActorVisualsList Group(Alignment alignment, params ActorVisuals[] enemies) => new()
    {
        alignment = alignment,
        visuals = new NativeList<ActorVisuals>(enemies)
    };
}
