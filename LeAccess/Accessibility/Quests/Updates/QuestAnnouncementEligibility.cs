using System;
using LeAccess.Accessibility.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class QuestAnnouncementEligibility
{
    internal static bool HasGameplayAccess(bool otherContentActive, IntPtr expectedCharacter)
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || otherContentActive ||
            NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput()) return false;
        var scene = SceneManager.GetActiveScene();
        var player = PlayerFinder.player;
        if (!scene.isLoaded || !SceneList.TryGetNonDefaultSceneDetails(scene.name, out _) ||
            !IsAlive(player) || !player.activeInHierarchy || player.scene.handle != scene.handle) return false;
        var character = PlayerFinder.getPlayerData();
        if (character is null || character.WasCollected ||
            expectedCharacter != IntPtr.Zero && character.Pointer != expectedCharacter) return false;
        var input = EpochInputManager.instance;
        var keyboard = ScreenKeyboardManager.Instance;
        return !(IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled);
    }
}
