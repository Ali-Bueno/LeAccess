using LeAccess.Accessibility.Exploration;
using static Program;

internal static class KeyTests
{
    internal static void Run()
    {
        Check(ExplorationKey.Compose(false, "Fiona", 4, null) == "offline-Fiona-4", "offline key keeps name and class");
        Check(ExplorationKey.Compose(false, "Fiona", null, "acct") == null, "offline without a class has no key");
        Check(ExplorationKey.Compose(false, "", 4, "acct") == null, "offline never uses the account name");
        Check(ExplorationKey.Compose(true, "yarkidius", 1, "yarkey") == "online-yarkidius-1", "online key with name and class");
        Check(ExplorationKey.Compose(true, "yarkidius", null, "yarkey") == "online-yarkidius", "online key without a class");
        Check(ExplorationKey.Compose(true, null, 1, "yarkey") == "online-account-yarkey-1", "online without a name uses the account and class");
        Check(ExplorationKey.Compose(true, " ", null, "yarkey") == "online-account-yarkey", "blank online name uses the account");
        Check(ExplorationKey.Compose(true, "", 1, "") == null, "online without any identity has no key");
        Check(ExplorationKey.Compose(true, "a b/c", 2, null) == "online-a_b_c-2", "invalid file-name characters become underscores");
        Check(ExplorationKey.Sanitize(new string('x', 100)).Length == 64, "long names are cut");
        Check(ExplorationKey.Sanitize("Ñandú☃") == "Ñandú☃", "Unicode letters are kept");
    }
}
