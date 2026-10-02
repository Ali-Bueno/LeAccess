using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Reads a mastery definition without borrowing adjacent tooltip text.</summary>
public static class MasteryReader
{
    public static bool TryGetMasteryInfo(GameObject go, out string name, out string description)
    {
        name = description = null;
        if (!IsAlive(go)) return false;
        var button = go.GetComponentInParent<MasteryButton>();
        if (!IsAlive(button)) return false;
        var tooltip = button.GetComponent<MasteryButtonTooltip>();
        // OpenTooltip displays this title directly; it is not a localization key.
        if (IsAlive(tooltip)) name = TextUtils.CleanText(tooltip.masteryName);
        var panel = button.GetComponentInParent<MasteryPanel>();
        if (IsAlive(panel) && panel.masteryButtons is { WasCollected: false } buttons &&
            panel.masteryButtonTMPs is { WasCollected: false } labels)
        {
            for (int i = 0; i < buttons.Length && i < labels.Length; i++)
                if (IsAlive(buttons[i]) && button.transform.IsChildOf(buttons[i].transform) &&
                    TextUtils.IsVisibleLabel(labels[i])) name = TextUtils.CleanText(labels[i].text);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            string label = null;
            foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var owner = IsAlive(text) ? text.GetComponentInParent<MasteryButton>() : null;
                if (!TextUtils.IsVisibleLabel(text) || !IsAlive(owner) || owner.Pointer != button.Pointer ||
                    IsAlive(button.levelTMP) && button.levelTMP.Pointer == text.Pointer ||
                    IsAlive(button.masteredTMP) && text.transform.IsChildOf(button.masteredTMP.transform) ||
                    IsAlive(text.GetComponentInParent<MultiButtonsActionPrompt>()) ||
                    IsAlive(text.GetComponentInParent<InputDependantPrompt>()) ||
                    IsAlive(text.GetComponentInParent<InputCharacterForAction>())) continue;
                string candidate = TextUtils.CleanText(text.text);
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (label != null && label != candidate) { label = null; break; }
                label = candidate;
            }
            name = label;
        }
        description = ReadDefinition(button);
        return !string.IsNullOrWhiteSpace(name);
    }

    internal static string ReadDefinition(MasteryButton button)
    {
        var parts = new List<string> { Localize(button.classDescriptionLocalizationKey, button.classDescription) };
        AppendBonuses(parts, button.bonusDescriptionLocalizationKey, button.passiveBonuses);
        return ScreenContentTracker.Combine(parts);
    }

    internal static string ReadDefinition(PassivePanelData.MasteryData data)
    {
        var parts = new List<string> { Localize(data.descriptionLocalizationKey, data.description) };
        AppendBonuses(parts, data.bonusLocalizationKeys, data.bonusFallbackText);
        return ScreenContentTracker.Combine(parts);
    }

    internal static string LocalizedName(string name, bool mastery)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var common = Localization.Common;
        return TextUtils.CleanText(common is null || common.WasCollected ? name :
            mastery ? common.GetMastery(name) : common.GetClass(name));
    }

    private static string Localize(string key, string fallback) => TextUtils.CleanText(
        string.IsNullOrWhiteSpace(key) ? fallback : Localization.GetText(key, fallback ?? ""));

    private static void AppendBonuses(List<string> parts, Il2CppSystem.Collections.Generic.List<string> keys,
        Il2CppSystem.Collections.Generic.List<string> fallback)
    {
        int keyCount = keys is null || keys.WasCollected ? 0 : keys.Count;
        int fallbackCount = fallback is null || fallback.WasCollected ? 0 : fallback.Count;
        for (int i = 0; i < System.Math.Max(keyCount, fallbackCount); i++)
            parts.Add(Localize(i < keyCount ? keys[i] : null, i < fallbackCount ? fallback[i] : null));
    }
}
