using System;
using System.Collections.Generic;
using EHG.Multiplayer;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Combat;

// Main-thread snapshot of the enemy cue streams; see reference/enemy-cues.md.
internal sealed class EnemyCueProbe
{
    // The radar's probe supplies the validated player, camera axes and NavMesh line of sight.
    private readonly RadarWorldProbe _world = new();
    private readonly EnemyCueSelector _selector = new();
    private readonly List<EnemyContact> _contacts = new();
    private readonly Func<EnemyContact, TerrainSight> _sight;

    internal EnemyCueProbe() => _sight = contact =>
        _world.Sight(new UnityEngine.Vector3(contact.Position.X, contact.Position.Y, contact.Position.Z));

    internal List<EnemyCue> Selected { get; } = new();
    internal long Session { get; private set; }
    internal int InRange => _contacts.Count;
    internal string UnavailableReason { get; private set; }
    internal string LastFailure => HostileRegistry.LastFailure;

    internal bool TryCapture(bool bosses, bool ordinary, bool elites)
    {
        Selected.Clear();
        _contacts.Clear();
        if (!_world.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot, probeWalls: false))
            return Unavailable(_world.UnavailableReason ?? "world unavailable");
        // The same range as the nearest-enemy announcement and the enemy scan.
        float range = ControllerTargetingParadigm._SkillTargetMaxDistance;
        if (!float.IsFinite(range) || range <= 0) return Unavailable("game skill targeting range unavailable");
        if (!EnemyScanProbe.TryRead(GameplayEnvironment.IsOnlineClient, range, _contacts, out var origin, readNames: false,
                view: UnityEngine.Camera.main))
            return Unavailable("hostile registry unavailable");
        var up = snapshot.Axes[(int)RadarDirection.Up];
        var right = snapshot.Axes[(int)RadarDirection.Right];
        _selector.Select(_contacts, origin, new NVector3(up.X, 0, up.Y), new NVector3(right.X, 0, right.Y),
            range, bosses, ordinary, _sight, Selected, elites, static kind => SoundLevels.Enemy(kind) > 0);
        Session = snapshot.Session;
        UnavailableReason = null;
        return true;
    }

    private bool Unavailable(string reason)
    {
        Session = 0;
        UnavailableReason = reason;
        return false;
    }

    internal void Reset()
    {
        _world.Reset();
        Selected.Clear();
        _contacts.Clear();
        Session = 0;
    }
}
