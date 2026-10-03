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
        if (TryGetAgent(world, out var agent))
        {
            float offset = agent.baseOffset;
            float climb = NavMesh.GetSettingsByID(agent.agentTypeID).agentClimb;
            if (float.IsFinite(offset) && float.IsFinite(climb) && climb >= 0)
                allowance = Math.Max(allowance, Math.Abs(offset) + climb);
        }
        return allowance;
    }

    internal static bool TryGetAgent(MapSectionWorldSnapshot world, out NavMeshAgent agent)
    {
        if (TryGetQueryAgent(world, out agent) && agent.isActiveAndEnabled) return true;
        agent = null;
        return false;
    }

    internal static bool TryGetQueryAgent(MapSectionWorldSnapshot world, out NavMeshAgent agent)
    {
        agent = null;
        var player = PlayerFinder.player;
        if (!IsAlive(player) || !player.activeInHierarchy || (int)player.scene.handle != world.SceneHandle) return false;
        bool owned = IsAlive(_agent) && IsAlive(_agent.gameObject) && _agent.gameObject.activeInHierarchy &&
            (int)_agent.gameObject.scene.handle == world.SceneHandle && _agent.transform.IsChildOf(player.transform);
        if (_session != world.Session || !owned)
        {
            _session = world.Session;
            _agent = IsAlive(player) ? player.GetComponentInChildren<NavMeshAgent>() : null;
        }
        // Native movement can disable the component; its owned query settings remain usable.
        if (IsAlive(player) && IsAlive(_agent) && _agent.gameObject.activeInHierarchy &&
            _agent.transform.IsChildOf(player.transform) && (int)_agent.gameObject.scene.handle == world.SceneHandle)
        {
            agent = _agent;
            return true;
        }
        return false;
    }

    internal static void Reset() { _agent = null; _session = 0; }
}
