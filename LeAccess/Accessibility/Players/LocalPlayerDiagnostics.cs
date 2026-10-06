using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Players;

internal static class LocalPlayerDiagnostics
{
    private static (int Scene, int Player, int Visuals) _last;

    // Logs once per scene/root/rig change which local player the shared source resolved.
    internal static void Update()
    {
        try
        {
            var scene = SceneManager.GetActiveScene();
            var player = LocalPlayerSource.Player;
            if (!scene.IsValid() || !scene.isLoaded || !IsAlive(player)) return;
            var visuals = LocalPlayerSource.Visuals;
            var key = ((int)scene.handle, player.GetInstanceID(), IsAlive(visuals) ? visuals.GetInstanceID() : 0);
            if (key == _last) return;
            _last = key;
            Plugin.Log.LogInfo($"[LocalPlayer] scene={scene.name} root={player.name}#{key.Item2} " +
                $"rig={(key.Item3 == 0 ? "none" : $"{visuals.gameObject.name}#{key.Item3}")} " +
                $"source={(LocalPlayerSource.FromLocalPlayer ? "LocalPlayer.instance" : "PlayerFinder")} " +
                $"finder_player={Compare(PlayerFinder.player, player)} finder_rig={Compare(PlayerFinder.playerActorVisuals, visuals)}");
        }
        catch (System.Exception ex) { Plugin.Log.LogWarning($"[LocalPlayer] diagnostics failed: {ex.Message}"); }
    }

    internal static void Reset() => _last = default;

    private static string Compare(UnityEngine.Object cached, UnityEngine.Object resolved) =>
        !IsAlive(cached) ? "missing" : IsAlive(resolved) && cached.Pointer == resolved.Pointer ? "same" : "other";
}
