using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Where the stick is pushing the local player, read from the game's own thumbstick movement state.</summary>
internal static class MovementIntent
{
    internal enum Source { None, Client, Actor }

    /// <summary>The world direction of the current stick push; false while the player is not walking by stick.</summary>
    internal static bool TryGetPush(Vector3 cameraRight, Vector3 cameraForward, out Vector3 push, out Source source)
    {
        // Online the client state machine walks the player; offline the actor's own one may instead.
        push = ClientPush(cameraRight, cameraForward);
        source = Source.Client;
        if (push.sqrMagnitude > Mathf.Epsilon) return true;
        push = ActorPush(cameraRight, cameraForward);
        source = Source.Actor;
        if (push.sqrMagnitude > Mathf.Epsilon) return true;
        source = Source.None;
        return false;
    }

    private static Vector3 ClientPush(Vector3 cameraRight, Vector3 cameraForward)
    {
        var local = LocalPlayer.instance;
        var controller = IsAlive(local) && local.isActiveAndEnabled ? local.clientStateController : null;
        var state = IsAlive(controller) ? controller.currentState : null;
        var stick = IsAlive(state) ? state.TryCast<ClientThumbstickMovingPlayer>() : null;
        return stick is null ? default : Direction(stick.input, stick.WorldMovementDir, cameraRight, cameraForward);
    }

    private static Vector3 ActorPush(Vector3 cameraRight, Vector3 cameraForward)
    {
        var actor = PlayerFinder.playerActorComponent;
        var controller = IsAlive(actor) ? actor.stateController : null;
        var state = IsAlive(controller) ? controller.currentState : null;
        var stick = IsAlive(state) ? state.TryCast<MovingPlayerThumbstick>() : null;
        return stick is null ? default : Direction(stick.input, stick.WorldMovementDir, cameraRight, cameraForward);
    }

    // The state's world direction when it has one; otherwise the stick on the camera's ground axes, as the screen shows it.
    private static Vector3 Direction(Vector2 input, Vector3 world, Vector3 cameraRight, Vector3 cameraForward)
    {
        if (input.sqrMagnitude <= Mathf.Epsilon) return default;
        world.y = 0f;
        return world.sqrMagnitude > Mathf.Epsilon ? world : cameraRight * input.x + cameraForward * input.y;
    }
}
