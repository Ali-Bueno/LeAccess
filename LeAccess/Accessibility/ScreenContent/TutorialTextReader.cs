using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.I18n;
using Rewired;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Action = EpochInputManager.InputActions;

namespace LeAccess.Accessibility.ScreenContent;

internal static class TutorialTextReader
{
    internal static string Read(TutorialManager tutorial)
    {
        if (!IsAlive(tutorial) || !IsAlive(tutorial.tutorialTooltip)) return null;
        var root = tutorial.tutorialTooltip.gameObject;
        if (!UiVisibility.IsVisible(root)) return null;
        string content = ScreenContentReader.Read(root, tutorial.titleTMP, tutorial.bodyTMP);
        if (string.IsNullOrWhiteSpace(content)) return null;
        var fragments = new List<string> { content };
        var notice = tutorial._tutorialNotice;
        if (notice is not null && !notice.WasCollected && ReInput.isReady)
        {
            string key = notice.textLocalizationKey;
            foreach (var action in ActionsFor(key))
            {
                string buttons = ReadAction(action);
                if (!string.IsNullOrWhiteSpace(buttons))
                    fragments.Add(ModText.Format("tutorial.control", buttons));
                else if (EpochInputManager.IsControllerActive && IsMenu(action))
                {
                    buttons = ReadAction(Action.ControllerMenuToggle);
                    if (!string.IsNullOrWhiteSpace(buttons))
                        fragments.Add(ModText.Format("tutorial.controller_menu", buttons));
                }
            }
        }
        // Some tutorial versions supply a dedicated prompt beside their body text.
        foreach (var prompt in root.GetComponentsInChildren<MultiButtonsActionPrompt>(false))
            if (IsAlive(prompt) && prompt.isActiveAndEnabled && prompt.IsPromptActive())
                fragments.Add(InputBindingText.ReadPrompt(prompt));
        return ScreenContentTracker.Combine(fragments);
    }

    private static string ReadAction(Action id)
    {
        try
        {
            var mapping = ReInput.mapping;
            var action = mapping is not null && !mapping.WasCollected ? mapping.GetAction((int)id) : null;
            return action is not null && !action.WasCollected ? InputBindingText.ReadAction(action.name) : null;
        }
        catch (Exception) { return null; }
    }

    private static bool IsMenu(Action action) => action is Action.Inventory or Action.SkillMenu or
        Action.PasiveGrid or Action.QuestLog or Action.CraftingMenu or Action.LootFilter or Action.SocialPanel;

    private static IEnumerable<Action> ActionsFor(string key)
    {
        // These stable notice keys come from the installed MasterTutorialList.
        switch (key)
        {
            case "Tutorials_HealthPotions_Text": yield return Action.UsePotion; break;
            case "Tutorials_Evade_Text": yield return Action.DodgeRoll; break;
            case "Tutorials_Portal_Text": yield return Action.UsePortal; break;
            case "Tutorials_NewPassivePoint_Text": yield return Action.PasiveGrid; break;
            case "Tutorials_NewSpecializationSlot_Text":
            case "Tutorials_NewSkillPoint_Text": yield return Action.SkillMenu; break;
            case "Tutorials_Quests_Text": yield return Action.QuestLog; break;
            case "Tutorials_Items_Text": yield return Action.Inventory; break;
            case "Tutorials_Crafting_Text": yield return Action.CraftingMenu; break;
            case "Tutorials_LootFilter_Text": yield return Action.LootFilter; break;
            case "Tutorials_PartyUp_Text": yield return Action.SocialPanel; break;
            case "Tutorials_NewSkillUnlocked_Text":
            case "Tutorials_SwappingSkills_Text":
                if (EpochInputManager.IsControllerActive) yield return Action.ControllerMenuToggle;
                break;
            case "Tutorials_UsingAbilities_Text":
                yield return Action.Ability1; yield return Action.Ability2; yield return Action.Ability3;
                yield return Action.Ability4; yield return Action.Ability5;
                break;
        }
    }
}
