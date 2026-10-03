using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Settings;

internal static class SettingsChallengeReader
{
    internal static string Read(SettingsPanelTabNavigable owner, bool includeDetails = false)
    {
        var parts = new List<UiReadingOrder.Fragment>();
        foreach (var challenge in owner.GetComponentsInChildren<CharacterChallengeSetting>(false))
        {
            if (!IsAlive(challenge) || !challenge.isActiveAndEnabled || !IsAlive(challenge.navigable) ||
                challenge.navigable.Pointer != owner.Pointer || !UiVisibility.IsVisible(challenge.gameObject)) continue;
            bool enabled = OwnedVisible(challenge.challengeEnabledObj, challenge.gameObject);
            bool disabled = OwnedVisible(challenge.challengeDisabledObj, challenge.gameObject);
            if (enabled == disabled) continue;
            string name = SettingsFocusReader.ReadLabel(challenge.challengeNameText, challenge.gameObject);
            string state = enabled
                ? SettingsFocusReader.ReadLabel(challenge.challengeEnabledDescription, challenge.challengeEnabledObj, !includeDetails)
                : SettingsFocusReader.ReadUniqueLabel(challenge.challengeDisabledObj, !includeDetails);
            if (name != null && state != null) parts.Add(new(challenge.challengeNameText, $"{name}: {state}"));
        }
        return ScreenContentTracker.Combine(UiReadingOrder.Read(parts));
    }

    private static bool OwnedVisible(GameObject value, GameObject owner) => IsAlive(value) &&
        value.transform.IsChildOf(owner.transform) && UiVisibility.IsVisible(value);
}
