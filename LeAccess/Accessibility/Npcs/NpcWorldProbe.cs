using System;
using LeAccess.Accessibility.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Npcs;

internal readonly record struct NpcCueTarget(
    long Session, int InstanceId, string Name, float Distance, float Range, float Pan, float Forward,
    string NameSource = null);

internal sealed class NpcWorldProbe
{
    private Camera _camera;
    private long _session;
    internal string LastFailure { get; private set; }

    internal bool TryCapture(float rangeMultiplier, out NpcCueTarget target)
    {
        target = default;
        LastFailure = null;
        try
        {
            if (!float.IsFinite(rangeMultiplier) || rangeMultiplier <= 0 ||
                NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput()) return false;
            var player = PlayerFinder.player;
            if (!IsAlive(player) || !player.activeInHierarchy || !IsAlive(player.transform)) return false;
            var visuals = PlayerFinder.playerActorVisuals;
            if (visuals is not null && (!IsAlive(visuals) || !visuals.isActiveAndEnabled || visuals.dead)) return false;

            long session = ((long)(int)SceneManager.GetActiveScene().handle << 32) | (uint)player.GetInstanceID();
            if (session != _session) { Reset(); _session = session; }
            if (!IsAlive(_camera) || !_camera.isActiveAndEnabled) _camera = Camera.main;
            if (!IsAlive(_camera) || !_camera.isActiveAndEnabled || !IsAlive(_camera.transform)) return false;
            var forward = _camera.transform.forward;
            var right = _camera.transform.right;
            if (!NormalizeXZ(ref forward) || !NormalizeXZ(ref right)) return false;

            var listeners = WorldObjectClickListener.allListener;
            if (listeners is null || listeners.WasCollected) return false;
            Vector3 position = player.transform.position;
            WorldObjectClickListener nearest = null;
            Vector3 nearestOffset = default;
            float nearestDistanceSquared = float.PositiveInfinity;
            float nearestRange = 0;
            int nearestId = 0;
            int count = listeners.Count;
            for (int index = 0; index < count; index++)
            {
                try
                {
                    var listener = listeners[index];
                    if (!IsAlive(listener) || !listener.isActiveAndEnabled ||
                        !IsAlive(listener.gameObject) || !listener.gameObject.activeInHierarchy ||
                        !IsAlive(listener.transform)) continue;
                    float range = listener.interactionRange * rangeMultiplier;
                    if (!float.IsFinite(range) || range <= 0) continue;
                    Vector3 offset = listener.transform.position - position;
                    float distanceSquared = offset.sqrMagnitude;
                    if (!float.IsFinite(distanceSquared) || distanceSquared > (double)range * range) continue;
                    int id = listener.GetInstanceID();
                    if (nearest is not null && (distanceSquared > nearestDistanceSquared ||
                        distanceSquared == nearestDistanceSquared && id >= nearestId)) continue;
                    if (!IsNpc(listener) || !listener.CanInteract()) continue;
                    nearest = listener;
                    nearestOffset = offset;
                    nearestDistanceSquared = distanceSquared;
                    nearestRange = range;
                    nearestId = id;
                }
                catch (Exception ex) { LastFailure ??= Describe(ex); }
            }
            if (!IsAlive(nearest)) return false;
            float distance = MathF.Sqrt(nearestDistanceSquared);
            Vector3 bearing = nearestOffset;
            float pan = 0, ahead = 0;
            if (NormalizeXZ(ref bearing))
            {
                pan = Math.Clamp(Vector3.Dot(bearing, right), -1f, 1f);
                ahead = Math.Clamp(Vector3.Dot(bearing, forward), -1f, 1f);
            }
            string name = ReadName(nearest, out string nameSource);
            target = new(session, nearestId, name, distance, nearestRange, pan, ahead, nameSource);
            return true;
        }
        catch (Exception ex) { LastFailure = Describe(ex); return false; }
    }

    internal static bool IsNpc(WorldObjectClickListener listener)
    {
        return WorldServiceReader.IsNpc(listener);
    }

    private string ReadName(WorldObjectClickListener listener, out string source)
    {
        source = null;
        try
        {
            return NpcNameReader.Read(listener, out source);
        }
        catch (Exception ex) { LastFailure ??= Describe(ex); return null; }
    }

    private static bool NormalizeXZ(ref Vector3 vector)
    {
        double squared = (double)vector.x * vector.x + (double)vector.z * vector.z;
        if (!double.IsFinite(squared) || squared <= 0) return false;
        double length = Math.Sqrt(squared);
        vector = new Vector3((float)(vector.x / length), 0, (float)(vector.z / length));
        return true;
    }

    private static string Describe(Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

    internal void Reset() { _camera = null; _session = 0; LastFailure = null; }
}
