using System;
using UnityEngine.AI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class MapPlayerFloor
{
    private static NavMeshAgent _agent;
    private static long _session;

    internal static double HeightAllowance(MapSectionWorldSnapshot world)
    {
        double allowance = double.IsFinite(world.BodyRadius) ? Math.Max(0, world.BodyRadius) : 0;
        var player = PlayerFinder.player;
        bool owned = IsAlive(player) && IsAlive(_agent) && _agent.transform.IsChildOf(player.transform);
        if (_session != world.Session || !owned)
        {
            _session = world.Session;
            _agent = IsAlive(player) ? player.GetComponentInChildren<NavMeshAgent>() : null;
        }
        if (IsAlive(player) && IsAlive(_agent) && _agent.isActiveAndEnabled &&
            _agent.transform.IsChildOf(player.transform) && (int)_agent.gameObject.scene.handle == world.SceneHandle)
        {
            float offset = _agent.baseOffset;
            float climb = NavMesh.GetSettingsByID(_agent.agentTypeID).agentClimb;
            if (float.IsFinite(offset) && float.IsFinite(climb) && climb >= 0)
                allowance = Math.Max(allowance, Math.Abs(offset) + climb);
        }
        return allowance;
    }

    internal static void Reset() { _agent = null; _session = 0; }
}
