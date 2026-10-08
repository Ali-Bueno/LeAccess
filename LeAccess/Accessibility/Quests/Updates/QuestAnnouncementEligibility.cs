using System;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Players;
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
        var player = LocalPlayerSource.Player;
        if (!scene.isLoaded || !SceneList.TryGetNonDefaultSceneDetails(scene.name, out _) ||
            !IsAlive(player) || !player.activeInHierarchy || player.scene.handle != scene.handle) return false;
        var character = Character();
        if (character == IntPtr.Zero || expectedCharacter != IntPtr.Zero && character != expectedCharacter) return false;
        var input = EpochInputManager.instance;
        var keyboard = ScreenKeyboardManager.Instance;
        return !(IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled);
    }

    /// <summary>Changes with the character: its saved data offline; online, where that data is absent, LocalPlayer.</summary>
    internal static IntPtr Character()
    {
        var data = PlayerFinder.getPlayerData();
        if (data is not null && !data.WasCollected) return data.Pointer;
        var local = LocalPlayerSource.OnlineLocal();
        return local is null ? IntPtr.Zero : local.Pointer;
    }
}
