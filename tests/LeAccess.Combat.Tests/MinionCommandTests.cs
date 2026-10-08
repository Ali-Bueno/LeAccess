using LeAccess.Accessibility.Combat;
using LeAccess.I18n;

// The native re-arm flag clearing means a command fired; the scanner's own hold never counts.
internal static class MinionCommandTests
{
    internal static void Run(Action<bool, string> check)
    {
        var state = new MinionCommandState();
        check(!state.Observe(false, false), "No command before the stick ever rested");
        state.Observe(true, false);
        check(state.Observe(false, false), "Armed then cleared without Create is a fired command");
        check(!state.Observe(false, false), "A command is reported once");
        state.Observe(true, false);
        check(!state.Observe(false, true), "The scanner holding the flag down is not a command");
        check(!state.Observe(false, false), "Releasing Create with the stick away from rest is not a command");
        state.Observe(true, false);
        check(state.Observe(false, false), "After the stick rests again a new flick is a command");
        state.Observe(true, false); state.Reset();
        check(!state.Observe(false, false), "A menu or keyboard reset forgets the armed flag");

        check(MinionCommandState.Describe(0, "Zángano") == null, "Without minions nothing is said");
        check(MinionCommandState.Describe(2, "Zángano") == ModText.Format("minions.command_attack", "Zángano"),
            "A command with a target names the enemy");
        check(MinionCommandState.Describe(1, " ") == ModText.Get("minions.command_move"), "A command without a target is a move");
    }
}
