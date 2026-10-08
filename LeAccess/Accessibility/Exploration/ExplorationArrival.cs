using System.Numerics;

namespace LeAccess.Accessibility.Exploration;

// Where the player entered the current scene session. The exit back to the scene they left is known from
// there: the player has just come through it. See reference/exploration-mode.md#arrival-and-the-way-back-2026-10-08.
internal sealed class ExplorationArrival
{
    private long _session;
    private Vector3 _point;
    private bool _known;

    // Keeps the first finite position of each scene session.
    internal void Observe(long session, Vector3 player)
    {
        if (_known && session == _session || !ExploredMap.Finite(player)) return;
        _session = session;
        _point = player;
        _known = true;
    }

    // An exit to the scene the player came from, within the seen range of the arrival point.
    internal bool IsWayBack(ExplorationCategory category, string destination, string cameFrom, Vector3 position,
        float seenRange)
    {
        if (!_known || category != ExplorationCategory.Exit || string.IsNullOrEmpty(cameFrom) || destination != cameFrom)
            return false;
        float flat = ExploredMap.Horizontal(position, _point);
        return float.IsFinite(flat) && float.IsFinite(seenRange) && flat <= seenRange;
    }

    internal void Clear()
    {
        _session = 0;
        _point = default;
        _known = false;
    }
}
