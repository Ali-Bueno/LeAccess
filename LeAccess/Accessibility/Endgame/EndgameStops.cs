using LeAccess.Accessibility.Market;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Picks the stop reader for the control that has focus inside an endgame panel.</summary>
internal static class EndgameStops
{
    internal static bool TryRead(Panel panel, GameObject selected, out EndgameStop stop)
    {
        stop = default;
        var observatory = panel.TryCast<ObservatoryPanel>();
        if (IsAlive(observatory)) return ObservatoryReader.TryRead(observatory, selected, out stop);
        if (!IsAlive(selected)) return false;
        if (IsAlive(panel.TryCast<LadderPanel>())) return LadderReader.TryRead(selected, out stop);
        var morditas = panel.TryCast<RageOfMorditasRewardPanel>();
        if (IsAlive(morditas)) return MorditasReader.TryRead(morditas, selected, out stop);
        var blessing = panel.TryCast<ChooseBlessingPanel>();
        if (IsAlive(blessing)) return BlessingReader.Owns(selected) && BlessingReader.TryRead(blessing, selected, out stop);
        return TierTabReader.TryRead(panel, selected, out stop) || ChoiceReader.TryRead(panel, selected, out stop);
    }

    /// <summary>True when <see cref="TryRead"/> speaks this control, so the generic announcer stays silent.</summary>
    internal static bool Owns(Panel panel, GameObject element)
    {
        if (!element.transform.IsChildOf(panel.transform)) return false;
        var observatory = panel.TryCast<ObservatoryPanel>();
        if (IsAlive(observatory)) return ObservatoryReader.Owns(observatory, element);
        if (IsAlive(panel.TryCast<LadderPanel>())) return LadderReader.Owns(element);
        var morditas = panel.TryCast<RageOfMorditasRewardPanel>();
        if (IsAlive(morditas)) return MorditasReader.Owns(morditas, element);
        if (IsAlive(panel.TryCast<ChooseBlessingPanel>())) return BlessingReader.Owns(element);
        return IsAlive(panel.TryCast<SanctuaryOfEterraPanel>()) || IsAlive(panel.TryCast<ChooseEchoRewardPanel>()) ||
            TierTabReader.Owns(panel, element);
    }
}
