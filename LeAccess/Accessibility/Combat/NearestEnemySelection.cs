using System;

namespace LeAccess.Accessibility.Combat;

internal struct NearestEnemySelection
{
    private readonly float _rangeSquared;
    private float _nearestDistanceSquared;
    private int _nearestId;
    private bool _hasCandidate;

    internal NearestEnemySelection(float range)
    {
        _rangeSquared = float.IsFinite(range) && range > 0 ? range * range : float.NaN;
        _nearestDistanceSquared = float.PositiveInfinity;
        _nearestId = 0;
        _hasCandidate = false;
    }

    internal static bool Contains(float rangeSquared, float distanceSquared) =>
        float.IsFinite(rangeSquared) && float.IsFinite(distanceSquared) &&
        distanceSquared >= 0 && distanceSquared <= rangeSquared;

    internal bool Consider(int id, float distanceSquared)
    {
        if (!Contains(_rangeSquared, distanceSquared)) return false;
        if (_hasCandidate && (distanceSquared > _nearestDistanceSquared ||
            (distanceSquared == _nearestDistanceSquared && id >= _nearestId))) return false;

        _nearestDistanceSquared = distanceSquared;
        _nearestId = id;
        _hasCandidate = true;
        return true;
    }
}
