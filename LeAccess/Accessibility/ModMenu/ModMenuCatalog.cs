using System.Collections.Generic;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>One config section shown as a page; Id names its title key.</summary>
internal readonly record struct SectionSpec(string Section, string Id, SettingSpec[] Options);

/// <summary>The order, units and steps of every mod option in the settings menu (reference/mod-menu.md).</summary>
internal static class ModMenuCatalog
{
    internal const string AudioSection = "Audio";
    internal const string WorldVolumesSection = "World object sound volumes";
    internal const string WorldRangesSection = "World object sound ranges";
    internal const string WorldSection = "World object sounds";

    internal static readonly SettingSpec MasterVolume = new("MasterVolume", "master_volume", SettingUnit.Percent, 5);
    internal static readonly SettingSpec KindRange = new("", null, SettingUnit.Metres, 1);
    internal static readonly SettingSpec GroupRange = new("LoopRange", "world_loop_range", SettingUnit.Metres, 1,
        ZeroText: "menu.value.game_range");

    /// <summary>Sections reached through the sound pages instead of a page of their own.</summary>
    internal static readonly HashSet<string> SoundPageSections = new() { AudioSection, WorldVolumesSection, WorldRangesSection };

    internal static readonly string[] Languages = { "auto", "en", "es", "fr", "de", "pt", "pl", "ru", "ko", "zh", "ja" };

    internal static readonly SectionSpec[] Sections =
    {
        new("Reactive radar", "radar", new SettingSpec[]
        {
            new("Enabled", "radar_enabled"),
            new("Volume", "radar_volume", SettingUnit.Percent, 5),
            new("UnexploredMark", "radar_unexplored_mark"),
            new("OpenDepthInBodyWidths", "radar_open_depth", SettingUnit.BodyWidths, 1),
            new("AudioBufferMilliseconds", "radar_buffer", SettingUnit.Milliseconds, 10, Restart: true),
        }),
        new("Navigation audio", "sonar", new SettingSpec[]
        {
            new("ContinuousWallSonar", "sonar_continuous"),
            new("SonarVolume", "sonar_volume", SettingUnit.Percent, 5),
        }),
        new(WorldSection, "world", new SettingSpec[]
        {
            new("Enabled", "world_enabled"),
            new("Volume", "world_volume", SettingUnit.Percent, 5),
            new("Loops", "world_loops"),
            GroupRange,
        }),
        new("Enemy cues", "enemies", new SettingSpec[]
        {
            new("Enabled", "enemies_enabled"),
            new("Volume", "enemies_volume", SettingUnit.Percent, 5),
            new("Bosses", "enemies_bosses"),
            new("BossVolume", "enemies_boss_volume", SettingUnit.Percent, 10),
            new("Elites", "enemies_elites"),
            new("EliteVolume", "enemies_elite_volume", SettingUnit.Percent, 10),
            new("Ordinary", "enemies_ordinary"),
            new("OrdinaryVolume", "enemies_ordinary_volume", SettingUnit.Percent, 10),
        }),
        new("NPC sounds", "npcs", new SettingSpec[]
        {
            new("Enabled", "npcs_enabled"),
            new("Volume", "npcs_volume", SettingUnit.Percent, 5),
            new("AnnounceNames", "npcs_names"),
            new("InteractionRangeMultiplier", "npcs_range", SettingUnit.Times, 0.25, 0.5, 4),
        }),
        new("Exploration", "exploration", new SettingSpec[]
        {
            new("Enabled", "exploration_enabled"),
            new("SeenRange", "exploration_seen_range", SettingUnit.Metres, 5),
            new("RoomSounds", "exploration_room_sounds"),
            new("Discoveries", "exploration_discoveries"),
            new("ControllerScanner", "exploration_scanner"),
            new("ModifierMenu", "exploration_modifier_menu"),
            new("LeftStickOnOtherPads", "exploration_left_stick"),
        }),
        new("Exploration beacon", "beacon", new SettingSpec[]
        {
            new("Volume", "beacon_volume", SettingUnit.Percent, 5),
            new("ExploreRadius", "beacon_explore_radius", SettingUnit.Metres, 1),
            new("TownFullyExplored", "beacon_town"),
            new("AudioBufferMilliseconds", "beacon_buffer", SettingUnit.Milliseconds, 10, Restart: true),
        }),
        new("Landmarks", "landmarks", new SettingSpec[] { new("AnnounceNearbyNames", "landmarks_names") }),
        new("Map sections", "map_sections", new SettingSpec[]
        {
            new("AnnounceSections", "map_sections_announce"),
            new("DescribeMapOnEntry", "map_sections_describe"),
            new("AnnounceSurfaceChanges", "map_sections_surfaces"),
        }),
        new("Combat feedback", "combat", new SettingSpec[]
        {
            new("AnnounceBossLife", "combat_boss_life"),
            new("AnnounceEliteLife", "combat_elite_life"),
            new("AnnounceNearestEnemy", "combat_nearest"),
            new("SpeechIntervalSeconds", "combat_interval", SettingUnit.Seconds, 0.5, 0, 10),
            new("NearestEnemyPollSeconds", "combat_poll", SettingUnit.Seconds, 0.1, 0.1, 2),
            new("NearestEnemyAnnouncementFormat", "combat_format"),
        }),
        new("Vitals", "vitals", new SettingSpec[]
        {
            new("AutomaticAnnouncements", "vitals_automatic"),
            new("AnnouncePotionCharges", "vitals_potions"),
            new("AnnounceGoldPickups", "vitals_gold"),
            new("ReportKey", "vitals_report_key"),
            new("ReportWithControllerView", "vitals_controller_view"),
        }),
        new("Quests", "quests", new SettingSpec[]
        {
            new("AutomaticUpdates", "quests_automatic"),
            new("MinimumUpdateIntervalSeconds", "quests_interval", SettingUnit.Seconds, 0.5, 0, 10),
        }),
        new("Progression", "progression", new SettingSpec[] { new("AnnounceChanges", "progression_changes") }),
        new("Dialogue", "dialogue", new SettingSpec[] { new("SpeakNpcLines", "dialogue_npc_lines") }),
        new("Menus", "menus", new SettingSpec[] { new("AnnouncePositions", "menus_positions") }),
        new("Tooltips", "tooltips", new SettingSpec[] { new("ReadKeywordExplanations", "tooltips_keywords") }),
        new("Character sheet", "character_sheet", new SettingSpec[] { new("ReadSummaryOnOpen", "character_sheet_summary") }),
        new("Localization", "language", new SettingSpec[] { new("Language", "language", Choices: Languages) }),
        new("Mod menu", "mod_menu", new SettingSpec[]
        {
            new("ControllerShortcut", "mod_menu_controller"),
            new("OpenKey", "mod_menu_key"),
        }),
        new("Diagnostics", "diagnostics", new SettingSpec[] { new("ControllerInputLog", "diagnostics_controller_log") }),
    };
}
