using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.WorldCues;
using LeAccess.Accessibility.Vitals;
using LeAccess.Accessibility.Diagnostics;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Landmarks;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using System.IO;

namespace LeAccess;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    private Harmony _harmony;
    private static ConfigEntry<bool> _continuousSonar;

    internal static bool ContinuousSonarEnabled => _continuousSonar?.Value == true;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"LeAccess v{MyPluginInfo.PLUGIN_VERSION} loading...");
        ModText.Initialize(Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "Translations"),
            message => Log.LogWarning(message));
        GameLocale.Initialize(Config);
        var positions = Config.Bind("Menus", "AnnouncePositions", true,
            "Speak list positions such as \"3 of 8\" after menu, inventory, map and tree entries.");
        ListPosition.Enabled = positions.Value;
        positions.SettingChanged += (_, _) => ListPosition.Enabled = positions.Value;
        var keywords = Config.Bind("Tooltips", "ReadKeywordExplanations", true,
            "Read the status-effect explanations the game shows only while Right Stick is held, with a details request.");
        KeywordExplanation.Enabled = keywords.Value;
        keywords.SettingChanged += (_, _) => KeywordExplanation.Enabled = keywords.Value;

        // Initialize screen reader
        if (!ScreenReader.Initialize())
        {
            Log.LogWarning("Screen reader unavailable; automatic connection checks remain active.");
        }
        else
        {
            Log.LogInfo($"Screen reader initialized: {ScreenReader.DetectedScreenReader}");
        }

        // Apply Harmony patches
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Log.LogInfo("Harmony patches applied");

        // Initialize audio cues
        _continuousSonar = Config.Bind("Navigation audio", "ContinuousWallSonar", false,
            "Wall tones: a soft, steady rush of air per screen direction that grows louder as a wall comes closer; an alternative or companion to the reactive radar.");
        SoundLevels.Initialize(Config);
        ReactiveRadar.Initialize(Config);
        WallBump.Initialize(Config);
        QuestUpdateAccessibility.Initialize(Config);
        CombatAccessibility.Initialize(Config);
        VitalsAccessibility.Initialize(Config);
        GoldPickupAnnouncer.Initialize(Config);
        var speakNpcLines = Config.Bind("Dialogue", "SpeakNpcLines", true,
            "Speak what NPCs say: conversation lines, barks and cinematic subtitles. Turn off to rely on the game's voices; " +
            "lines without a voice are silenced too, and only English is voiced. Responses, buttons and quest text are always spoken.");
        LeAccess.Accessibility.Dialogue.NpcLines.Use(() => speakNpcLines.Value);
        LeAccess.Accessibility.Progression.ProgressionAccessibility.Initialize(Config);
        BossLifeAccessibility.Initialize(Config);
        LeAccess.Accessibility.CharacterSheet.CharacterSheetSummary.Initialize(Config);
        LandmarkAccessibility.Initialize(Config);
        ShrineEffectAnnouncer.Initialize(Config);
        MapSectionAccessibility.Initialize(Config);
        NpcSounds.Initialize(Config);
        WorldCueSounds.Initialize(Config);
        EnemyCueSounds.Initialize(Config);
        ControllerBindingLog.Initialize(Config);
        ExplorationAccessibility.Initialize(Config);
        LeAccess.Accessibility.ModMenu.ModMenu.Initialize(Config);
        WallSonar.Initialize();
        LeAccess.Accessibility.Forge.ForgeResultLog.Install();
        LeAccess.Accessibility.Social.SocialLog.Install();

        // Register UI component for menu accessibility
        AddComponent<MenuAccessibility>();

        Log.LogInfo("LeAccess loaded successfully!");
        ScreenReader.Speak(ModText.Get("mod.loaded"));
    }

    public override bool Unload()
    {
        LeAccess.Accessibility.ModMenu.ModMenu.Cleanup();
        LeAccess.Accessibility.Login.LoginTransitionGuard.Reset();
        GameLocale.Reset();
        LeAccess.Accessibility.CharacterCreation.PauseMenuFocusRecovery.Reset();
        LeAccess.Accessibility.CharacterCreation.ClassCarouselHandler.Reset();
        LeAccess.Accessibility.CharacterCreation.CreationChoiceReader.Reset();
        LeAccess.Accessibility.CharacterCreation.CharacterNameAccessibility.Reset();
        LeAccess.Accessibility.CharacterCreation.CharacterNameEcho.Reset();
        LeAccess.Accessibility.CharacterCreation.CharacterNameKeyboard.Reset();
        LeAccess.Accessibility.CharacterCreation.ScreenKeyboardTyping.Reset();
        LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.Reset();
        LeAccess.Accessibility.Dialogue.DialogueAnnouncer.Reset();
        LeAccess.Accessibility.Dialogue.VisibleSubtitleAnnouncer.Reset();
        ElementAnnouncer.ResetFocus();
        LeAccess.Accessibility.Inventory.InventoryAccessibility.Reset();
        LeAccess.Accessibility.Items.GroundLootAccessibility.Reset();
        LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.Reset();
        LeAccess.Accessibility.Skills.SkillAccessibility.Reset();
        LeAccess.Accessibility.Forge.ForgeAccessibility.Reset();
        LeAccess.Accessibility.Gifting.GiftAccessibility.Reset();
        LeAccess.Accessibility.Endgame.EndgameAccessibility.Reset();
        LeAccess.Accessibility.Panels.PanelIntro.Reset();
        LeAccess.Accessibility.Forge.ForgeResultLog.Uninstall();
        LeAccess.Accessibility.Social.SocialAccessibility.Reset();
        LeAccess.Accessibility.Social.SocialLog.Uninstall();
        LeAccess.Accessibility.Passives.PassiveAccessibility.Reset();
        LeAccess.Accessibility.Quests.QuestAccessibility.Reset();
        QuestUpdateAccessibility.Cleanup();
        DynamicObjectiveAccessibility.Reset();
        EncounterGuidanceTargets.Reset();
        LeAccess.Accessibility.Zones.ZoneAnnouncer.Reset();
        LeAccess.Accessibility.Zones.Transitions.ZoneTransitionAnnouncer.Reset();
        NotificationAnnouncer.Reset();
        ButtonPromptAnnouncer.Reset();
        CombatAccessibility.Cleanup();
        VitalsAccessibility.Cleanup();
        GoldPickupAnnouncer.Cleanup();
        LeAccess.Accessibility.Progression.ProgressionAccessibility.Cleanup();
        ControllerBindingLog.Cleanup();
        ExplorationAccessibility.Cleanup();
        BossLifeAccessibility.Cleanup();
        LandmarkAccessibility.Cleanup();
        ShrineEffectAnnouncer.Cleanup();
        MapSectionAccessibility.Cleanup();
        NpcSounds.Cleanup();
        LeAccess.Accessibility.Players.NearbyPlayerAccessibility.Reset();
        WorldCueSounds.Cleanup();
        EnemyCueSounds.Cleanup();
        ReactiveRadar.Cleanup();
        WallBump.Cleanup();
        WallSonar.Cleanup();
        SoundPreview.Cleanup();
        WorldAudioReservation.Reset();
        _harmony?.UnpatchSelf();
        ScreenReader.Shutdown();
        return base.Unload();
    }
}
