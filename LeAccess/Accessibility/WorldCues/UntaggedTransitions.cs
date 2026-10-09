using LE.PCG;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

// Scene changes the game shows without a hover tag: generated cave entrances and exits, and the cave lobby's way out.
// See reference/exploration-mode.md#generated-caves-2026-10-09.
internal static class UntaggedTransitions
{
    /// <summary>The scene the listener's untagged transition leads to, or null.</summary>
    internal static string ReadDestination(WorldObjectClickListener listener)
    {
        if (!IsAlive(listener)) return null;
        var spawn = listener.GetComponentInParent<UsePlayerSpawnInteraction>();
        if (Available(spawn))
        {
            string scene = spawn.ToScene;
            if (string.IsNullOrWhiteSpace(scene)) scene = spawn.sceneName;
            return string.IsNullOrWhiteSpace(scene) ? null : scene;
        }
        var lobby = listener.GetComponentInParent<CaveLobbyExitInteraction>();
        if (!Available(lobby) || !LevelUtility.TryGetLevel(lobby.gameObject.scene, out var level) || !IsAlive(level))
            return null;
        // The lobby exit returns the player to the scene the cave was generated from.
        var parent = level.ParentScene;
        return parent.IsValid() && !string.IsNullOrWhiteSpace(parent.name) ? parent.name : null;
    }

    /// <summary>The game's localized name for a scene, or null when it has none.</summary>
    internal static string SceneName(string scene) =>
        !string.IsNullOrWhiteSpace(scene) && SceneList.TryGetNonDefaultSceneDetails(scene, out var details) &&
        details is not null && !details.WasCollected ? TextUtils.CleanText(details.LocalizedName) : null;

    private static bool Available(Behaviour component) => IsAlive(component) &&
        component.isActiveAndEnabled && IsAlive(component.gameObject) && component.gameObject.activeInHierarchy;
}
