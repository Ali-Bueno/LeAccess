using EHG.Multiplayer;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Players;

// PlayerFinder's online caches are lazy and cleared on re-find; see reference/multiplayer-local-player.md.
internal static class LocalPlayerSource
{
    /// <summary>The local player's root object; online, the owner of <c>LocalPlayer.instance</c>.</summary>
    internal static GameObject Player
    {
        get
        {
            var local = OnlineLocal();
            return local is null ? PlayerFinder.player : local.gameObject;
        }
    }

    /// <summary>The local visual rig; online, the rig of LocalPlayer's own sync when it links back to that sync.</summary>
    internal static ActorVisuals Visuals
    {
        get
        {
            var local = OnlineLocal();
            if (local is null) return PlayerFinder.playerActorVisuals;
            var sync = local.actorSync;
            if (!IsAlive(sync)) return null;
            var visuals = sync.actorVisuals;
            return IsAlive(visuals) && IsAlive(visuals.actorSync) && visuals.actorSync.Pointer == sync.Pointer ? visuals : null;
        }
    }

    /// <summary>True when the online LocalPlayer component supplies the identity instead of PlayerFinder.</summary>
    internal static bool FromLocalPlayer => OnlineLocal() is not null;

    // LocalPlayer.Awake stores itself; only the tagged local root carries that component.
    internal static LocalPlayer OnlineLocal()
    {
        if (!GameplayEnvironment.IsOnlineClient) return null;
        var local = LocalPlayer.instance;
        return IsAlive(local) ? local : null;
    }
}
