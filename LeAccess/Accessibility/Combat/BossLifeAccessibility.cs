using System;
using BepInEx.Configuration;
using EHG.Multiplayer;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.SceneManagement;
using LeAccess.Accessibility.Players;

namespace LeAccess.Accessibility.Combat;

internal static class BossLifeAccessibility
{
    private const string ConfigSection = "Combat feedback";
    // D2R boss_life_service.cpp polls every 200 ms.
    private const double PollSeconds = 0.2;
    private static readonly BossLifeState State = new();
    private static ConfigEntry<bool> _enabled;
    private static double _nextPoll;
    private static int _scene;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "AnnounceBossLife", true,
            "Announce the life of bosses and minibosses as it falls, with the same steps as the player's life.");
        Reset();
    }

    internal static void Update()
    {
        if (_enabled == null) return;
        try
        {
            if (!_enabled.Value) { Reset(); return; }
            // A pausing menu deals no damage, so baselines are kept; a loading screen ends the session.
            if (!Application.isFocused) return;
            if (LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;

            var scene = SceneManager.GetActiveScene();
            if (scene.handle != _scene) { State.Reset(); _scene = scene.handle; }
            var camera = Camera.main;
            if (camera != null && !camera.isActiveAndEnabled) camera = null;
            var samples = GameplayEnvironment.IsOnlineClient ? BossLifeProbe.Read(LocalPlayerSource.Visuals, camera)
                : BossLifeProbe.Read(PlayerFinder.playerActorComponent, camera);
            if (BossLifeProbe.LastFailure is not null) ReportFailure(BossLifeProbe.LastFailure);
            if (samples == null) return;

            var update = State.Observe(samples);
            if (!update.HasContent) return;
            string text = update.Text((name, percent) => ModText.Format("boss.health", name, percent));
            if (!ScreenReader.TrySpeak(text, false)) return;
            State.Acknowledge(update);
            Plugin.Log.LogInfo($"[Boss life] {text}");
        }
        catch (Exception ex)
        {
            Reset();
            ReportFailure(ex.ToString());
        }
    }

    private static void ReportFailure(string message)
    {
        if (_lastFailure == message) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Boss life narration unavailable: {message}");
    }

    private static void Reset()
    {
        State.Reset();
        _nextPoll = 0;
        _scene = 0;
    }

    internal static void Cleanup()
    {
        Reset();
        _enabled = null;
        _lastFailure = null;
    }
}
