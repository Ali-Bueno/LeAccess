using System;
using BepInEx.Configuration;
using EHG.Multiplayer;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Zones;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.Skills;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.SceneManagement;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

public static class CombatAccessibility
{
    private const string ConfigSection = "Combat feedback";
    // Five observations per second keep proximity feedback responsive without scanning every frame.
    private const double DefaultPollSeconds = 0.2;
    private const string DefaultAnnouncementFormat = "{0}";
    private const double DiagnosticIntervalSeconds = 5;
    private static readonly NearestEnemyAnnouncementState Announcements = new();
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<double> _speechInterval;
    private static ConfigEntry<double> _pollInterval;
    private static ConfigEntry<string> _announcementFormat;
    private static double _nextPoll;
    private static long _session;
    private static string _lastFailure;
    private static string _lastStatus;
    private static double _nextDiagnostic;

    public static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "AnnounceNearestEnemy", true,
            "Announce the nearest living hostile within the game's skill targeting range, without requiring damage.");
        _speechInterval = config.Bind(ConfigSection, "SpeechIntervalSeconds",
            NearestEnemyAnnouncementState.DefaultSpeechIntervalSeconds,
            "Minimum time between announcements when the nearest enemy name changes.");
        _pollInterval = config.Bind(ConfigSection, "NearestEnemyPollSeconds", DefaultPollSeconds,
            "Time between checks of the game's enemy registry.");
        _announcementFormat = config.Bind(ConfigSection, "NearestEnemyAnnouncementFormat", DefaultAnnouncementFormat,
            "Spoken nearest enemy message; {0} is replaced by the localized name.");
    }

    public static void Update(bool otherContentActive = false)
    {
        if (_enabled == null) return;
        try
        {
            double now = Time.unscaledTimeAsDouble;
            if (!_enabled.Value || NavigationAudioState.IsSuspended)
            {
                Reset();
                ReportStatus(_enabled.Value ? NavigationAudioState.SuspensionReason : "disabled in configuration", now);
                return;
            }

            if (otherContentActive || !GameplayInputState.OwnsInput())
            {
                Reset();
                ReportStatus("gameplay input not active", now);
                return;
            }

            bool online = GameplayEnvironment.IsOnlineClient;
            var visuals = online ? LocalPlayerSource.Visuals : null;
            var actor = online ? null : PlayerFinder.playerActorComponent;
            UnityEngine.Object player = online ? visuals : actor;
            var scene = SceneManager.GetActiveScene();
            if (online ? !NearestEnemyProbe.IsAvailable(visuals, scene) || visuals.dead
                : !NearestEnemyProbe.IsAvailable(actor, scene) || actor.isDying())
            {
                Reset();
                ReportStatus(online ? "online player unavailable or dead" : "offline player unavailable or dead", now);
                return;
            }

            long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
            if (session != _session)
            {
                Reset();
                _session = session;
            }

            if (now < _nextPoll) return;
            _nextPoll = now + PositiveOrDefault(_pollInterval.Value, DefaultPollSeconds);
            string name = null;
            string observation = "noncombat zone";
            if (!SceneList.IsNonCombatZone(scene.name))
            {
                float range = ControllerTargetingParadigm._SkillTargetMaxDistance;
                if (!float.IsFinite(range) || range <= 0)
                {
                    Announcements.Reset();
                    ReportFailure("game skill targeting range unavailable");
                    return;
                }
                name = online ? NearestEnemyProbe.FindName(visuals, range) : NearestEnemyProbe.FindName(actor, range);
                observation = $"{NearestEnemyProbe.LastStatus} range={range:F2} nearest={name ?? "none"}";
                if (NearestEnemyProbe.LastFailure is not null) ReportFailure(NearestEnemyProbe.LastFailure);
            }

            Announcements.SpeechIntervalSeconds = PositiveOrDefault(_speechInterval.Value,
                NearestEnemyAnnouncementState.DefaultSpeechIntervalSeconds);
            string blocked = SpeechBlockReason();
            ReportStatus($"{observation} speech={blocked ?? "ready"}", now);
            // Surface ownership and same-frame speech are reliable even when the backend stays "speaking".
            name = EnemyClassifier.Decorate(name, NearestEnemyProbe.LastClass, ModText.Format);
            if (Announcements.TryAnnounce(name, now, blocked is null, out string nearest))
            {
                string format = _announcementFormat.Value;
                if (string.IsNullOrWhiteSpace(format) || !format.Contains("{0}")) format = DefaultAnnouncementFormat;
                ScreenReader.TrySpeakOverPickup(format.Replace("{0}", nearest), false);
                Plugin.Log.LogInfo($"[NearestEnemy] {nearest}");
            }
        }
        catch (Exception ex)
        {
            Announcements.Reset();
            ReportFailure(ex.ToString());
        }
    }

    private static string SpeechBlockReason() => !ScreenReader.IsAvailable() ? "reader unavailable"
        : ScreenContentAnnouncer.HasActiveContent ? "screen content owns focus"
        : DialogueAnnouncer.HasActiveContent ? "dialogue owns focus"
        : InventoryAccessibility.HasActiveContent ? "inventory owns focus"
        : SkillAccessibility.HasActiveContent ? "skills own focus"
        : ControllerMenuAccessibility.HasActiveContent ? "controller menu owns focus"
        : ZoneAnnouncer.HasPendingContent ? "zone pending"
        : GroundLootAccessibility.HasPendingContent ? "ground item pending"
        : NpcSounds.HasPendingName ? "NPC name pending"
        : ScreenReader.SpokeThisFrame ? "another announcement this frame" : null;

    private static void ReportStatus(string status, double now)
    {
        if (_lastStatus == status || now < _nextDiagnostic) return;
        _lastStatus = status;
        _nextDiagnostic = now + DiagnosticIntervalSeconds;
        Plugin.Log.LogInfo($"[NearestEnemy status] {status}");
    }

    private static double PositiveOrDefault(double value, double fallback) =>
        double.IsFinite(value) && value > 0 ? value : fallback;

    private static void ReportFailure(string message)
    {
        if (_lastFailure == message) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Nearest enemy feedback unavailable: {message}");
    }

    private static void Reset()
    {
        Announcements.Reset();
        _nextPoll = 0;
        _session = 0;
    }

    public static void Cleanup()
    {
        Reset();
        _enabled = null;
        _lastFailure = null;
        _lastStatus = null;
        _nextDiagnostic = 0;
    }
}
