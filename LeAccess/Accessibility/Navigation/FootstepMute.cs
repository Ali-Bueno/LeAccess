using System.Collections.Generic;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Players;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Silences the local player's footsteps by detaching each foot trigger's emitter, the one gate
/// <c>FootstepRaycastTrigger.Step</c> checks before any sound (reference/navigation-radar.md#wall-bump-2026-10-11).</summary>
internal static class FootstepMute
{
    private static readonly List<(FootstepRaycastTrigger Trigger, FootstepSoundEmitter Emitter)> Detached = new();
    private static bool _muted;

    /// <summary>Detaches every local foot trigger's emitter, again on each call so a form change is silenced too.</summary>
    internal static void Mute()
    {
        var player = LocalPlayerSource.Player;
        if (!IsAlive(player) || !MapPlayerVisuals.TryResolve(player, (int)SceneManager.GetActiveScene().handle, out var visuals))
            return;
        var triggers = visuals.gameObject.GetComponentsInChildren<FootstepRaycastTrigger>(true);
        if (triggers is null || triggers.WasCollected) return;
        foreach (var trigger in triggers)
        {
            if (!IsAlive(trigger)) continue;
            var emitter = trigger.emitter;
            if (!IsAlive(emitter)) continue;
            Detached.Add((trigger, emitter));
            trigger.emitter = null;
        }
        if (_muted || Detached.Count == 0) return;
        _muted = true;
        // The collider-driven FootstepTrigger path has its own emitter; the log shows whether the player carries one.
        int colliderTriggers = visuals.gameObject.GetComponentsInChildren<FootstepTrigger>(true)?.Length ?? 0;
        Plugin.Log.LogInfo($"[Wall bump] footsteps silenced: {Detached.Count} foot triggers, {colliderTriggers} collider triggers");
    }

    /// <summary>Gives each detached trigger its emitter back, unless the game has assigned one since.</summary>
    internal static void Restore()
    {
        foreach (var (trigger, emitter) in Detached)
            if (IsAlive(trigger) && trigger.emitter is null && IsAlive(emitter)) trigger.emitter = emitter;
        Detached.Clear();
        _muted = false;
    }
}
