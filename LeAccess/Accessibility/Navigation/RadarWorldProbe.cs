using System;
using LeAccess.Accessibility.MapSections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using NumericsVector2 = System.Numerics.Vector2;

namespace LeAccess.Accessibility.Navigation;

internal readonly struct RadarWorldSnapshot
{
    public NumericsVector2 Position { get; }
    public NumericsVector2[] Axes { get; }
    public RadarReading[] Readings { get; }
    public RadarSettings Settings { get; }
    public long Session { get; }
    public float AgentRadius { get; }
    public float BodyRadius { get; }
    public int AgentTypeId { get; }
    public int AreaMask { get; }

    public RadarWorldSnapshot(NumericsVector2 position, NumericsVector2[] axes, RadarReading[] readings,
        RadarSettings settings, long session, float agentRadius, float bodyRadius, int agentTypeId, int areaMask)
    {
        Position = position;
        Axes = axes;
        Readings = readings;
        Settings = settings;
        Session = session;
        AgentRadius = agentRadius;
        BodyRadius = bodyRadius;
        AgentTypeId = agentTypeId;
        AreaMask = areaMask;
    }
}

internal sealed class RadarWorldProbe
{
    private NavMeshAgent _agent;
    private Actor _actor;
    private Camera _camera;
    private Quaternion _cameraRotation;
    private bool _hasCameraFrame;
    private long _session;
    private RadarSettings _settings;
    private float _bodyWidth;
    private float _openDepthInBodyWidths;
    private readonly NumericsVector2[] _axes = new NumericsVector2[4];
    private readonly RadarReading[] _readings = new RadarReading[4];
    public string UnavailableReason { get; private set; }

    public bool TryCapture(float openDepthInBodyWidths, out RadarWorldSnapshot snapshot)
    {
        snapshot = default;
        var scene = SceneManager.GetActiveScene();
        var player = PlayerFinder.player;
        if (!scene.IsValid() || !scene.isLoaded || !NavigationObjectLifetime.IsAlive(player)
            || !player.activeInHierarchy || (int)player.scene.handle != (int)scene.handle)
            return Unavailable("local player unavailable");
        var visuals = PlayerFinder.playerActorVisuals;
        var localActor = PlayerFinder.playerActorComponent;
        if ((visuals is not null || NavigationObjectLifetime.IsAlive(localActor) &&
            NavigationObjectLifetime.IsAlive(localActor.actorSync)) &&
            !MapPlayerVisuals.TryResolve(player, (int)scene.handle, out visuals))
            return Unavailable("local player inactive or dead");

        long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
        if (session != _session)
        {
            Reset();
            _session = session;
        }

        if (!Owned(_agent, player))
        {
            var localPlayer = LocalPlayer.instance;
            _agent = NavigationObjectLifetime.IsAlive(localPlayer) ? localPlayer.navMeshAgent : null;
            if (!Owned(_agent, player)) _agent = player.GetComponentInChildren<NavMeshAgent>();
        }
        if (!Owned(_agent, player)) return Unavailable("player NavMesh agent unavailable");
        float agentRadius = _agent.radius;
        if (!float.IsFinite(agentRadius) || agentRadius <= 0f) return Unavailable("player NavMesh radius unavailable");
        if (!Owned(_actor, player)) _actor = PlayerFinder.playerActorComponent;
        float actorRadius = Owned(_actor, player) && _actor.isActiveAndEnabled ? _actor.getRadius() : 0f;
        if ((!float.IsFinite(actorRadius) || actorRadius <= 0f) && visuals is not null)
            actorRadius = visuals.getRadius();
        // D2 measures warning reach in actual body widths; a helper NavMesh agent can be much narrower.
        float bodyRadius = float.IsFinite(actorRadius) && actorRadius > agentRadius ? actorRadius : agentRadius;
        float bodyWidth = bodyRadius * 2f;
        // Actor scale has float jitter; changing the settings identity discards pending radar confirmation.
        if (_settings == null || !Mathf.Approximately(bodyWidth, _bodyWidth)
            || openDepthInBodyWidths != _openDepthInBodyWidths)
        {
            _bodyWidth = bodyWidth;
            _openDepthInBodyWidths = openDepthInBodyWidths;
            _settings = RadarSettings.FromBodyWidth(bodyWidth, openDepthInBodyWidths);
        }

        if (!NavigationObjectLifetime.IsAlive(_camera) || !_camera.isActiveAndEnabled)
        {
            _camera = Camera.main;
            _hasCameraFrame = false;
        }
        if (!NavigationObjectLifetime.IsAlive(_camera) || !_camera.isActiveAndEnabled || !TryUpdateAxes())
            return Unavailable("gameplay camera unavailable");

        // Unity 6's restored filter-object overloads contain invalid IL. See reference/runtime-feedback.md.
        int agentTypeId = _agent.agentTypeID;
        int areaMask = _agent.areaMask;
        Vector3 playerPosition = player.transform.position;
        if (!NavMesh.SamplePositionFilter(playerPosition, out var origin, agentRadius, agentTypeId, areaMask))
            return Unavailable("no walkable NavMesh near player");

        for (int i = 0; i < _axes.Length; i++)
        {
            var direction = new Vector3(_axes[i].X, 0f, _axes[i].Y);
            Vector3 target = origin.position + direction * _settings.ReachDepth;
            bool blocked = NavMesh.RaycastFilter(origin.position, target, out var hit, agentTypeId, areaMask);
            float depth = blocked ? HorizontalDistance(origin.position, hit.position) : _settings.ReachDepth;
            _readings[i] = new RadarReading(depth, float.IsFinite(depth) && depth >= 0f);
        }

        snapshot = new RadarWorldSnapshot(
            new NumericsVector2(origin.position.x, origin.position.z), _axes, _readings, _settings, session,
            agentRadius, bodyRadius, agentTypeId, areaMask);
        UnavailableReason = null;
        return true;
    }

    private bool Unavailable(string reason)
    {
        UnavailableReason = reason;
        return false;
    }

    private static bool Owned(Component component, GameObject player) =>
        NavigationObjectLifetime.IsAlive(component) && NavigationObjectLifetime.IsAlive(component.gameObject)
        && component.gameObject.activeInHierarchy && (int)component.gameObject.scene.handle == (int)player.scene.handle
        && (component.transform.IsChildOf(player.transform) || player.transform.IsChildOf(component.transform));

    private bool TryUpdateAxes()
    {
        var rotation = _camera.transform.rotation;
        if (_hasCameraFrame && rotation == _cameraRotation) return true;

        Vector3 up = _camera.transform.forward;
        up.y = 0f;
        if (up.sqrMagnitude <= Mathf.Epsilon) return false;
        up.Normalize();
        Vector3 rightDirection = _camera.transform.right;
        rightDirection.y = 0f;
        if (rightDirection.sqrMagnitude <= Mathf.Epsilon) return false;
        rightDirection.Normalize();
        var right = new NumericsVector2(rightDirection.x, rightDirection.z);
        var forward = new NumericsVector2(up.x, up.z);
        _axes[(int)RadarDirection.Up] = forward;
        _axes[(int)RadarDirection.Left] = -right;
        _axes[(int)RadarDirection.Right] = right;
        _axes[(int)RadarDirection.Down] = -forward;
        _cameraRotation = rotation;
        _hasCameraFrame = true;
        return true;
    }

    private static float HorizontalDistance(Vector3 from, Vector3 to)
    {
        float dx = to.x - from.x;
        float dz = to.z - from.z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    public void Reset()
    {
        _agent = null;
        _actor = null;
        _camera = null;
        _settings = null;
        _hasCameraFrame = false;
        _session = 0;
    }
}
