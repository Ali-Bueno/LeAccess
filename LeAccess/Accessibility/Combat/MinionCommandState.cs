using LeAccess.I18n;

namespace LeAccess.Accessibility.Combat;

/// <summary>Detects a native controller minion command: the game clears its re-arm flag when one fires.</summary>
/// <remarks>See reference/minion-commands.md.</remarks>
internal sealed class MinionCommandState
{
    private bool _armed, _wasBlocked;

    /// <param name="blocked">True while the mod itself holds the flag down (Create held for the scanner).</param>
    internal bool Observe(bool armed, bool blocked)
    {
        bool fired = _armed && !armed && !blocked && !_wasBlocked;
        _armed = armed;
        _wasBlocked = blocked;
        return fired;
    }

    internal void Reset() { _armed = false; _wasBlocked = false; }

    /// <summary>"Minions: attack X" or "Minions: move"; nothing without minions.</summary>
    internal static string Describe(int minions, string target) => minions <= 0 ? null
        : string.IsNullOrWhiteSpace(target) ? ModText.Get("minions.command_move")
        : ModText.Format("minions.command_attack", target);
}
