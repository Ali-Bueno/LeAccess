using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.Players;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Zones;
using LeAccess.Accessibility.Skills;
using LeAccess.Accessibility.WorldCues;
using LeAccess.Accessibility.Passives;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.Vitals;
using LeAccess.Accessibility.Landmarks;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Login;
using LeAccess.Accessibility.Settings;
using LeAccess.I18n;
using UnityEngine;

namespace LeAccess.Accessibility;

/// <summary>
/// Main component for menu accessibility.
/// </summary>
public class MenuAccessibility : MonoBehaviour
{
    private int _lastPolledSelection;
    private string _focusFailure;

    void Awake()
    {
        Plugin.Log.LogInfo("MenuAccessibility component initialized");
    }

    void LateUpdate()
    {
        if (GameLocale.Refresh())
        {
            _lastPolledSelection = 0;
            ElementAnnouncer.ResetFocus();
        }
        LoginTransitionGuard.Update();
        PauseMenuFocusRecovery.Update();
        ScreenContentAnnouncer.Update();
        DialogueAnnouncer.Update();
        ControllerMenuAccessibility.Update();
        bool readDetails = MenuDetailInput.ReadRequested();
        PassiveAccessibility.Update();
        QuestAccessibility.Update();
        SkillAccessibility.Update(readDetails);
        ClassCarouselHandler.CheckPendingClassAnnouncement();
        if (SettingsFocusAnnouncer.Update()) _lastPolledSelection = 0;
        else PollSelectedControl();
        ElementAnnouncer.FlushPendingFocus();
        CharacterNameAccessibility.Update();
        InventoryAccessibility.Update(readDetails);
        if (readDetails && !SkillAccessibility.HasActiveContent && !InventoryAccessibility.HasActiveContent)
            ReadSelectedDetails();
        ZoneAnnouncer.Update();
        VisibleSubtitleAnnouncer.Update(ScreenReader.SpokeThisFrame || ZoneAnnouncer.HasPendingContent ||
            InventoryAccessibility.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
            SkillAccessibility.HasActiveContent || PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent);
        MapSectionAccessibility.Update(ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
            InventoryAccessibility.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
            SkillAccessibility.HasActiveContent || PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent,
            ZoneAnnouncer.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        GroundLootAccessibility.Update(ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        NpcSounds.Update(ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        NearbyPlayerAccessibility.Update(ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent ||
            VisibleSubtitleAnnouncer.HasPendingContent || GroundLootAccessibility.HasPendingContent || NpcSounds.HasPendingName);
        WorldCueSounds.Update();
        LandmarkAccessibility.Update(ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
            InventoryAccessibility.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
            SkillAccessibility.HasActiveContent || PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent,
            ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent ||
            GroundLootAccessibility.HasPendingContent || NpcSounds.HasPendingName || NearbyPlayerAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        NotificationAnnouncer.Update(ScreenReader.SpokeThisFrame || DialogueAnnouncer.HasActiveContent || InventoryAccessibility.HasPendingContent ||
            PassiveAccessibility.HasPendingContent || QuestAccessibility.HasPendingContent || CharacterNameAccessibility.HasPendingContent ||
            ControllerMenuAccessibility.HasActiveContent || SkillAccessibility.HasPendingContent || ClassCarouselHandler.HasPendingContent || ZoneAnnouncer.HasPendingContent ||
            GroundLootAccessibility.HasPendingContent || NpcSounds.HasPendingName || NearbyPlayerAccessibility.HasPendingContent || LandmarkAccessibility.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        ButtonPromptAnnouncer.Update(InventoryAccessibility.HasPendingContent || GroundLootAccessibility.HasPendingContent ||
            PassiveAccessibility.HasPendingContent || QuestAccessibility.HasPendingContent || CharacterNameAccessibility.HasPendingContent ||
            ControllerMenuAccessibility.HasPendingContent || SkillAccessibility.HasPendingContent || ClassCarouselHandler.HasPendingContent ||
            ZoneAnnouncer.HasPendingContent || NpcSounds.HasPendingName || NearbyPlayerAccessibility.HasPendingContent || LandmarkAccessibility.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        CombatAccessibility.Update(PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent || VisibleSubtitleAnnouncer.HasPendingContent);
        VitalsAccessibility.Update(ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
            InventoryAccessibility.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
            SkillAccessibility.HasActiveContent || PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent);
    }

    private static void ReadSelectedDetails()
    {
        try
        {
            if (PassiveAccessibility.ReadCurrentDetails() || QuestAccessibility.ReadCurrentDetails() ||
                SettingsFocusAnnouncer.ReadCurrentDetails()) return;
            if (ClassCarouselHandler.TryReadDetails(out string text))
            { ScreenReader.Speak(text, true); return; }
            var navigation = NavigationManager.currentlySelected;
            var prioritized = navigation is null || navigation.WasCollected ? null : navigation.TryCast<PrioritizedNavigable>();
            if (!NavigationObjectLifetime.IsAlive(prioritized) || !prioritized.isActiveAndEnabled) return;
            var selected = prioritized.ActiveSelectable;
            if (!NavigationObjectLifetime.IsAlive(selected) || !selected.isActiveAndEnabled) return;
            if (ElementAnnouncer.TryReadDetails(selected.gameObject, out text)) ScreenReader.Speak(text, true);
        }
        catch (System.Exception ex) { Plugin.Log.LogWarning($"Menu details unavailable: {ex.Message}"); }
    }

    // Some current menus change ActiveSelectable without showing the controller
    // cursor. Read that same game-owned focus; never search the scene for a label.
    private void PollSelectedControl()
    {
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible)
            { _lastPolledSelection = 0; return; }
            var navigation = NavigationManager.currentlySelected;
            var prioritized = navigation is null || navigation.WasCollected
                ? null : navigation.TryCast<PrioritizedNavigable>();
            if (!NavigationObjectLifetime.IsAlive(prioritized) || !prioritized.isActiveAndEnabled)
            { _lastPolledSelection = 0; return; }
            var selected = prioritized.ActiveSelectable;
            if (!NavigationObjectLifetime.IsAlive(selected) || !selected.isActiveAndEnabled ||
                !UiVisibility.IsVisible(selected.gameObject))
            { _lastPolledSelection = 0; return; }
            int id = selected.GetInstanceID();
            if (id == _lastPolledSelection) return;
            _lastPolledSelection = id;
            ElementAnnouncer.AnnounceElement(selected.gameObject, interrupt: true);
        }
        catch (System.Exception ex)
        {
            if (_focusFailure == ex.Message) return;
            _focusFailure = ex.Message;
            Plugin.Log.LogWarning($"Menu focus unavailable: {ex}");
        }
    }

    void Update()
    {
        NavigationAudioState.Refresh();
        ReactiveRadar.Update();
        WallSonar.Update();
    }

    /// <summary>
    /// Force announce a specific text.
    /// </summary>
    public static void Announce(string text, bool interrupt = true)
    {
        ScreenReader.Speak(text, interrupt);
    }
}
