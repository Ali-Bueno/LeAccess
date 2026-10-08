namespace LeAccess.Accessibility.Combat;

internal enum EnemyClass { Normal, Magic, Rare, Champion, Boss }

internal delegate string EnemyScanText(string key, params object[] arguments);

// Pure class rules and wording; the game reads live in EnemyClassProbe (reference/enemy-class.md).
internal static class EnemyClassifier
{
    internal static EnemyClass Classify(bool boss, bool champion, bool rare, bool magic) =>
        boss ? EnemyClass.Boss : champion ? EnemyClass.Champion : rare ? EnemyClass.Rare
        : magic ? EnemyClass.Magic : EnemyClass.Normal;

    // Rare and champion enemies get the elite cue and life narration.
    internal static bool IsElite(EnemyClass enemyClass) => enemyClass is EnemyClass.Rare or EnemyClass.Champion;

    // Bosses keep their own wording elsewhere, so only the other special classes have a word.
    internal static string WordKey(EnemyClass enemyClass) => enemyClass switch
    {
        EnemyClass.Magic => "enemy.class.magic",
        EnemyClass.Rare => "enemy.class.rare",
        EnemyClass.Champion => "enemy.class.champion",
        _ => null
    };

    internal static string Decorate(string name, EnemyClass enemyClass, EnemyScanText text)
    {
        string key = WordKey(enemyClass);
        return key == null || string.IsNullOrWhiteSpace(name) ? name : text("enemy.named_class", name, text(key));
    }
}
