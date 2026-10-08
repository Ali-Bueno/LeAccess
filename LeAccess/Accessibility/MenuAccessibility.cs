using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
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
using LeAccess.Accessibility.Map;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.Vitals;
using LeAccess.Accessibility.Diagnostics;
using LeAccess.Accessibility.Exploration;
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
        ElementAnnouncer.ResetFocusIfUnowned();
        ControllerMenuAccessibility.Update();
        bool readDetails = MenuDetailInput.ReadRequested();
        PassiveAccessibility.Update();
        QuestAccessibility.Update();
        SkillAccessibility.Update(readDetails);
        ClassCarouselHandler.CheckPendingClassAnnouncement();
        CreationChoiceReader.Poll(ElementAnnouncer.IsRelatedToFocusedElement);
        if (SettingsFocusAnnouncer.Update()) _lastPolledSelection = 0;
        else PollSelectedControl();
        ElementAnnouncer.FlushPendingFocus();
        OpenWorldMap.AnnounceSelection();
        CharacterNameAccessibility.Update();
        Social.SocialAccessibility.Update();
        Guide.GuideAccessibility.Update();
        Forge.ForgeInventoryNotices.Observe();
        Gifting.GiftAccessibility.Observe();
        InventoryAccessibility.Update(readDetails);
        Forge.ForgeAccessibility.Update(readDetails);
        Gifting.GiftAccessibility.Update(readDetails);
        Endgame.EndgameAccessibility.Update(readDetails);
        if (readDetails && !SkillAccessibility.HasActiveContent && !InventoryAccessibility.HasActiveContent &&
            !Forge.ForgeAccessibility.HasActiveContent && !Gifting.GiftAccessibility.HasActiveContent &&
            !Endgame.EndgameAccessibility.HasActiveContent)
            ReadSelectedDetails();
        ZoneAnnouncer.Update();
        bool worldContentActive = ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
            InventoryAccessibility.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
            SkillAccessibility.HasActiveContent || PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent;
        // Before VitalsAccessibility: it reads this frame's Create+Square command.
        ExplorationAccessibility.Update(worldContentActive);
        VisibleSubtitleAnnouncer.Update(ScreenReader.SpokeThisFrame || ZoneAnnouncer.HasPendingContent ||
            InventoryAccessibility.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
            SkillAccessibility.HasActiveContent || PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent);
        NotificationAnnouncer.UpdateUrgent(worldContentActive || VisibleSubtitleAnnouncer.HasPendingContent);
        QuestUpdateAccessibility.Update(worldContentActive,
            ZoneAnnouncer.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent);
        DynamicObjectiveAccessibility.Update(worldContentActive,
            ZoneAnnouncer.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || QuestUpdateAccessibility.HasPriorityOverDynamic);
        bool questUpdatesPending = QuestUpdateAccessibility.HasPendingAnnouncement || DynamicObjectiveAccessibility.HasPendingAnnouncement;
        MapSectionAccessibility.Update(worldContentActive,
            ZoneAnnouncer.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || questUpdatesPending);
        GroundLootAccessibility.Update(ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || questUpdatesPending);
        NpcSounds.Update(ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || questUpdatesPending);
        NearbyPlayerAccessibility.Update(ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent ||
            VisibleSubtitleAnnouncer.HasPendingContent || GroundLootAccessibility.HasPendingContent || NpcSounds.HasPendingName || questUpdatesPending);
        WorldCueSounds.Update();
        EnemyCueSounds.Update();
        LandmarkAccessibility.Update(worldContentActive,
            ZoneAnnouncer.HasPendingContent || MapSectionAccessibility.HasPendingContent ||
            GroundLootAccessibility.HasPendingContent || NpcSounds.HasPendingName || NearbyPlayerAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || questUpdatesPending);
        NotificationAnnouncer.Update(ScreenReader.SpokeThisFrame || DialogueAnnouncer.HasActiveContent || InventoryAccessibility.HasPendingContent ||
            PassiveAccessibility.HasPendingContent || QuestAccessibility.HasPendingContent || CharacterNameAccessibility.HasPendingContent ||
            ControllerMenuAccessibility.HasActiveContent || SkillAccessibility.HasPendingContent || ClassCarouselHandler.HasPendingContent || ZoneAnnouncer.HasPendingContent ||
            Guide.GuideAccessibility.HasPendingContent || GroundLootAccessibility.HasPendingContent || NpcSounds.HasPendingName || NearbyPlayerAccessibility.HasPendingContent || LandmarkAccessibility.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || questUpdatesPending);
        ButtonPromptAnnouncer.Update(InventoryAccessibility.HasPendingContent || GroundLootAccessibility.HasPendingContent ||
            PassiveAccessibility.HasPendingContent || QuestAccessibility.HasPendingContent || CharacterNameAccessibility.HasPendingContent ||
            ControllerMenuAccessibility.HasPendingContent || SkillAccessibility.HasPendingContent || ClassCarouselHandler.HasPendingContent ||
            ZoneAnnouncer.HasPendingContent || NpcSounds.HasPendingName || NearbyPlayerAccessibility.HasPendingContent || LandmarkAccessibility.HasPendingContent || MapSectionAccessibility.HasPendingContent || VisibleSubtitleAnnouncer.HasPendingContent || questUpdatesPending);
        CombatAccessibility.Update(PassiveAccessibility.HasActiveContent || QuestAccessibility.HasActiveContent || VisibleSubtitleAnnouncer.HasPendingContent);
        VitalsAccessibility.Update(worldContentActive);
        ControllerBindingLog.Update();
        BossLifeAccessibility.Update();
    }

    private static void ReadSelectedDetails()
    {
        try
        {
            if (PassiveAccessibility.ReadCurrentDetails() || QuestAccessibility.ReadCurrentDetails() ||
                Guide.GuideAccessibility.ReadCurrentDetails() ||
                SettingsFocusAnnouncer.ReadCurrentDetails()) return;
            if (ClassCarouselHandler.TryReadDetails(out string text))
            { ScreenReader.Speak(text, true); return; }
            if (UiFocusOwnership.TryGetSelected(out var selected) &&
                ElementAnnouncer.TryReadDetails(selected, out text)) ScreenReader.Speak(text, true);
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
            if (!UiFocusOwnership.TryGetSelected(out var selected))
            { _lastPolledSelection = 0; return; }
            int id = selected.GetInstanceID();
            if (id == _lastPolledSelection) return;
            _lastPolledSelection = id;
            ElementAnnouncer.AnnounceElement(selected, interrupt: true);
        }
        catch (System.Exception ex)
        {
            if (_focusFailure == ex.Message) return;
            _focusFailure = ex.Message;
            Plugin.Log.LogWarning($"Menu focus unavailable: {ex}");
        }
    }

    // Closing the game skips Plugin.Unload; keep the last 30 s of exploration.
    void OnApplicationQuit()
    {
        try { ExplorationPersistence.Flush(); }
        catch (System.Exception ex) { Plugin.Log.LogWarning($"Exploration save on quit failed: {ex.Message}"); }
    }

    void Update()
    {
        NavigationAudioState.Refresh();
        LocalPlayerDiagnostics.Update();
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
