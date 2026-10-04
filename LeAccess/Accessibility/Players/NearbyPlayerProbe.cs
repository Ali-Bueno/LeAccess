using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using EHG.Multiplayer;
using LE.Networking;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Players;

internal readonly record struct NearbyPlayerTarget(long Session, int InstanceId, string Name);

internal readonly record struct OtherPlayer(int InstanceId, string Name, Vector3 Position);

internal static class NearbyPlayerProbe
{
    private static readonly System.Collections.Generic.List<OtherPlayer> Others = new();

    internal static string LastFailure { get; private set; }

    internal static bool TryRead(out NearbyPlayerTarget target)
    {
        target = default;
        LastFailure = null;
        try
        {
            if (!TryLocal(out var scene, out var local, out var localVisuals, out var localSync)) return false;
            var input = EpochInputManager.instance;
            if (!IsAlive(input)) return false;
            var configuration = input.targetingConfiguration;
            if (configuration is null || configuration.WasCollected) return false;
            float range = configuration.playersRange;
            if (!float.IsFinite(range) || range <= 0) return false;
            if (!Collect(local, localVisuals, localSync, Others)) return false;
            Vector3 origin = localVisuals.position();
            double best = double.PositiveInfinity;
            int bestId = int.MaxValue;
            string bestName = null;
            foreach (var other in Others)
            {
                Vector3 offset = other.Position - origin;
                double distance = (double)offset.x * offset.x + (double)offset.y * offset.y + (double)offset.z * offset.z;
                if (!double.IsFinite(distance) || distance > (double)range * range) continue;
                if (distance > best || distance == best && other.InstanceId >= bestId) continue;
                best = distance; bestId = other.InstanceId; bestName = other.Name;
            }
            Others.Clear();
            if (bestName == null) return false;
            long session = ((long)(int)scene.handle << 32) | (uint)local.GetInstanceID();
            target = new(session, bestId, bestName);
            return true;
        }
        catch (Exception ex) { LastFailure ??= ex.Message; return false; }
    }

    // Every other living player in the game with a public name, wherever they are (the exploration scanner).
    internal static bool TryReadAll(System.Collections.Generic.List<OtherPlayer> players)
    {
        players.Clear();
        LastFailure = null;
        try
        {
            return TryLocal(out _, out var local, out var localVisuals, out var localSync) &&
                Collect(local, localVisuals, localSync, players) && players.Count > 0;
        }
        catch (Exception ex) { LastFailure ??= ex.Message; return false; }
    }

    private static bool TryLocal(out Scene scene, out GameObject local, out ActorVisuals localVisuals, out PlayerActorSync localSync)
    {
        scene = default;
        local = null;
        localVisuals = null;
        localSync = null;
        if (!GameplayEnvironment.IsOnlineClient) return false;
        local = PlayerFinder.player;
        var localPlayer = PlayerFinder.getLocalPlayerInMultiplayer();
        localVisuals = PlayerFinder.playerActorVisuals;
        scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || !IsAlive(local) || !local.activeInHierarchy ||
            !local.scene.IsValid() || !local.scene.isLoaded || !IsAlive(local.transform) || !IsAlive(localVisuals) ||
            !localVisuals.isActiveAndEnabled || !IsAlive(localVisuals.gameObject) ||
            !localVisuals.gameObject.scene.IsValid() || !localVisuals.gameObject.scene.isLoaded || !IsAlive(localVisuals.transform) ||
            !IsAlive(localPlayer) || !localPlayer.isActiveAndEnabled || !IsAlive(localPlayer.gameObject) ||
            !localPlayer.gameObject.scene.IsValid() || !localPlayer.gameObject.scene.isLoaded || !IsAlive(localPlayer.actorSync) ||
            !IsAlive(localVisuals.actorSync) || localPlayer.actorSync.Pointer != localVisuals.actorSync.Pointer) return false;
        localSync = localPlayer.actorSync.TryCast<PlayerActorSync>();
        return IsAlive(localSync) && IsAlive(localSync.localPlayer) && localSync.localPlayer.Pointer == localPlayer.Pointer &&
            IsAlive(localSync.actorVisuals) && localSync.actorVisuals.Pointer == localVisuals.Pointer;
    }

    private static bool Collect(GameObject local, ActorVisuals localVisuals, PlayerActorSync localSync,
        System.Collections.Generic.List<OtherPlayer> others)
    {
        others.Clear();
        var players = PlayerStore.GetPlayerActorSyncs();
        if (players is null || players.WasCollected) return false;
        var snapshot = new Il2CppReferenceArray<PlayerActorSync>(players.Count);
        players.CopyTo(snapshot, 0);
        foreach (var sync in snapshot)
        {
            try
            {
                if (!IsAlive(sync) || !sync.isActiveAndEnabled || sync.Pointer == localSync.Pointer) continue;
                var visuals = sync.actorVisuals;
                if (!IsAlive(visuals) || !visuals.isActiveAndEnabled || !visuals.isPlayer ||
                    visuals.Pointer == localVisuals.Pointer || !IsAlive(visuals.actorSync) ||
                    visuals.actorSync.Pointer != sync.Pointer || !IsAlive(visuals.gameObject) ||
                    !visuals.gameObject.activeInHierarchy || !visuals.gameObject.scene.IsValid() || !visuals.gameObject.scene.isLoaded ||
                    !IsAlive(visuals.transform) || visuals.transform.IsChildOf(local.transform)) continue;
                Vector3 position = visuals.position();
                if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) continue;
                string name = ReadName(visuals);
                if (string.IsNullOrWhiteSpace(name)) continue;
                others.Add(new OtherPlayer(visuals.GetInstanceID(), name, position));
            }
            catch (Exception ex) { LastFailure ??= ex.Message; }
        }
        return true;
    }

    private static string ReadName(ActorVisuals visuals)
    {
        // Native FloatingPlayerName.UpdatePlayerName displays this public username, never account IDs.
        var identity = visuals.UserIdentity;
        if (identity is null || identity.WasCollected) return null;
        string name = TextUtils.CleanText(identity.Username);
        if (string.IsNullOrWhiteSpace(name)) return null;
        var floating = visuals.floatingPlayerName;
        if (IsAlive(floating) && IsAlive(floating.identityProvider) && floating.identityProvider.Pointer == visuals.Pointer &&
            floating.hasBeenInitialized && !floating.updateLabelsNextFrame && IsAlive(floating.text) && !floating.IsDefaultText())
        {
            string displayed = TextUtils.CleanText(floating.text.text);
            if (string.Equals(displayed, name, StringComparison.OrdinalIgnoreCase)) return displayed;
        }
        return name;
    }
}
