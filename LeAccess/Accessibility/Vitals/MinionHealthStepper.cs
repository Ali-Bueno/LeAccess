using System.Collections.Generic;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Vitals;

/// <summary>One minion card of the game's HUD: a single minion with its life, or a group card with only a count.</summary>
internal readonly record struct MinionCardInfo(string Name, int Percent, int Count)
{
    internal bool IsGroup => Count > 0;
}

/// <summary>Steps through the minion cards one press at a time, like D2R's companion stepper.</summary>
/// <remarks>See reference/minion-health.md.</remarks>
internal sealed class MinionHealthStepper
{
    // D2R's companion stepper (minion_report.h RestartMs) starts over after this pause.
    internal const double RestartSeconds = 10;

    private int _next;
    private double _lastPress = double.NegativeInfinity;

    /// <summary>The index of the card to read for a press at <paramref name="now"/>; -1 without cards.</summary>
    internal int Step(int count, double now)
    {
        if (now - _lastPress >= RestartSeconds) _next = 0;
        _lastPress = now;
        if (count <= 0) { _next = 0; return -1; }
        if (_next >= count) _next = 0;
        return _next++;
    }

    internal void Reset()
    {
        _next = 0;
        _lastPress = double.NegativeInfinity;
    }

    /// <summary>"Skeleton 2: life 80 percent" or "Wraiths: 5"; repeated names are numbered in card order.</summary>
    internal static string Describe(IReadOnlyList<MinionCardInfo> cards, int index)
    {
        var card = cards[index];
        string name = card.Name;
        int same = 0, number = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].Name != name) continue;
            same++;
            if (i == index) number = same;
        }
        if (same > 1) name = ModText.Format("minions.numbered", name, number);
        return card.IsGroup ? ModText.Format("minions.count", name, card.Count) : ModText.Format("minions.life", name, card.Percent);
    }
}
