using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;

internal static partial class Program
{
    private static void SelectionElites()
    {
        var rare = EnemyClass.Rare;
        var pack = new[] { At(1, 0, 1), At(2, 0, 2), At(3, 0, 3) };
        var withElite = Select(pack.Append(At(9, 0, 8, enemyClass: rare)).ToArray());
        Check(withElite.Count == EnemyCueSelector.MaxStreams, "An elite and a pack never exceed three streams.");
        Check(withElite[0].Id == 9 && withElite[0].Kind == EnemyCueKind.Elite && withElite[0].Slot == EnemyCueSelector.BossSlot + 1,
            "A far rare enemy leads the ordinary streams with the elite cue.");
        Check(withElite.Skip(1).Select(cue => cue.Id).SequenceEqual(new[] { 1, 2 }) &&
            withElite.Skip(1).All(cue => cue.Kind == EnemyCueKind.Ordinary), "The nearest ordinary enemies fill the rest.");

        var champion = Select(new[] { At(1, 0, 4, enemyClass: EnemyClass.Champion) });
        Check(champion.Count == 1 && champion[0].Kind == EnemyCueKind.Elite, "A champion also gets the elite cue.");
        Check(Select(new[] { At(1, 0, 4, enemyClass: EnemyClass.Magic) })[0].Kind == EnemyCueKind.Ordinary,
            "A magic enemy stays ordinary.");

        var both = Select(new[] { At(1, 0, 2, enemyClass: rare), At(2, 0, 6, boss: true), At(3, 0, 1) });
        Check(both.Select(cue => cue.Kind).SequenceEqual(new[] { EnemyCueKind.Boss, EnemyCueKind.Elite, EnemyCueKind.Ordinary }) &&
            both.Select(cue => cue.Slot).SequenceEqual(new[] { 0, 1, 2 }), "A boss, an elite and an ordinary enemy each keep a stream.");

        var twoElites = Select(new[] { At(1, 0, 5, enemyClass: rare), At(2, 0, 3, enemyClass: rare), At(3, 0, 4) });
        Check(twoElites.Select(cue => (cue.Id, cue.Kind)).SequenceEqual(new[]
            { (2, EnemyCueKind.Elite), (3, EnemyCueKind.Ordinary), (1, EnemyCueKind.Ordinary) }),
            "Only the nearest elite has the elite stream; the others are heard as ordinary enemies.");

        var hidden = Select(new[] { At(1, 0, 2, enemyClass: rare), At(2, 0, 6, enemyClass: rare) },
            sight: contact => contact.Id == 1 ? TerrainSight.Blocked : TerrainSight.Clear);
        Check(hidden.Count == 1 && hidden[0].Id == 2 && hidden[0].Kind == EnemyCueKind.Elite,
            "An elite behind a wall yields the elite stream to the nearest one in sight.");
        Check(Select(new[] { At(1, 0, 2, enemyClass: rare) }, sight: _ => TerrainSight.OffMesh).Count == 0,
            "An elite off the walkable surface stays silent, unlike a boss.");

        var disabled = Select(new[] { At(1, 0, 2, enemyClass: rare) }, elites: false);
        Check(disabled.Count == 1 && disabled[0].Kind == EnemyCueKind.Ordinary, "With elites disabled they growl.");
        var noOrdinary = Select(new[] { At(1, 0, 2, enemyClass: rare), At(2, 0, 1) }, ordinary: false);
        Check(noOrdinary.Count == 1 && noOrdinary[0].Kind == EnemyCueKind.Elite, "Only the elite sounds when ordinary is off.");
    }
}
