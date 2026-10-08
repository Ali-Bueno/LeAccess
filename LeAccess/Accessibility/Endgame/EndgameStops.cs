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
        var blessing = panel.TryCast<ChooseBlessingPanel>();
        if (IsAlive(blessing)) return BlessingReader.Owns(selected) && BlessingReader.TryRead(blessing, selected, out stop);
        return TierTabReader.TryRead(panel, selected, out stop) || ChoiceReader.TryRead(panel, selected, out stop);
    }

    /// <summary>True when <see cref="TryRead"/> speaks this control, so the generic announcer stays silent.</summary>
    internal static bool Owns(Panel panel, GameObject element)
    {
        if (!element.transform.IsChildOf(panel.transform)) return false;
        if (IsAlive(panel.TryCast<ChooseBlessingPanel>())) return BlessingReader.Owns(element);
        return IsAlive(panel.TryCast<SanctuaryOfEterraPanel>()) || IsAlive(panel.TryCast<ChooseEchoRewardPanel>()) ||
            TierTabReader.Owns(panel, element);
    }
}
