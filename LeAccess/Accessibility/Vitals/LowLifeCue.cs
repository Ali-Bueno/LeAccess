using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using UnityEngine;

namespace LeAccess.Accessibility.Vitals;

/// <summary>Optional Diablo IV low-life sound: its start once when life drops under a percent, its heartbeat while it
/// stays there (reference/vitals-api.md#low-life-sound-2026-10-11).</summary>
internal static class LowLifeCue
{
    private const string ConfigSection = "Vitals";
    // The radar's listening-tested device buffer.
    private const int BufferMilliseconds = 100;
    // About half a heartbeat: the loop neither clicks in nor stops mid-beat.
    private const double GlideSeconds = 0.3;
    private const double AudioRetrySeconds = 5;
    // Diablo IV warns at about 40 percent life (user, 2026-10-11).
    private const int DefaultBelow = 40;
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _below, _volume;
    private static LowLifeMixer _mixer;
    private static LoopAudioOutput _output;
    private static double _nextRetry;
    private static bool _low;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "LowLifeSound", false,
            "Play the Diablo IV low-life sound when life drops under LowLifeBelow, and its heartbeat while it stays there.");
        _below = config.Bind(ConfigSection, "LowLifeBelow", DefaultBelow, new ConfigDescription(
            "Life percent under which the low-life sound plays.", new AcceptableValueRange<int>(5, 95)));
        _volume = config.Bind(ConfigSection, "LowLifeVolume", 100, new ConfigDescription(
            "Percent of the low-life sound's volume.", new AcceptableValueRange<int>(0, 100)));
    }

    /// <summary>Whether the cue needs life readings even with spoken announcements off.</summary>
    internal static bool Wanted => _enabled?.Value == true && _volume.Value > 0;

    /// <summary>Each frame: the latest life percent, or -1 when unknown, which fades the heartbeat out.</summary>
    internal static void Update(int healthPercent)
    {
        if (_enabled == null) return;
        try
        {
            bool low = Wanted && LowLifeMixer.IsLow(healthPercent, _below.Value);
            if (low != _low)
            {
                Plugin.Log.LogInfo(low ? $"[Low life] on at {healthPercent}%" : $"[Low life] off at {healthPercent}%");
                _low = low;
            }
            if (!low && _mixer?.LoopAudible != true) return;
            if (!EnsureOutput()) return;
            _mixer.Set(low, _volume.Value / 100f);
        }
        catch (Exception ex)
        {
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"[Low life] unavailable: {ex}");
        }
    }

    internal static void Cleanup()
    {
        _output?.Dispose();
        _output = null;
        _mixer = null;
        _low = false;
    }

    private static bool EnsureOutput()
    {
        double now = Time.unscaledTimeAsDouble;
        if (_output != null)
        {
            if (_output.IsRunning) return true;
            if (now < _nextRetry) return false;
            _nextRetry = now + AudioRetrySeconds;
            bool recovered = _output.TryRecover(out string status);
            if (status != null) Plugin.Log.LogInfo($"[Low life] {status}");
            return recovered;
        }
        if (now < _nextRetry) return false;
        _nextRetry = now + AudioRetrySeconds;
        if (LowLifeClips.Start.Length == 0 && LowLifeClips.Loop.Length == 0)
        {
            Plugin.Log.LogWarning($"[Low life] sounds unavailable: {LowLifeClips.LastFailure}");
            return false;
        }
        _mixer = new LowLifeMixer(LowLifeClips.Start, LowLifeClips.Loop, GlideSeconds, () => SoundLevels.Master);
        _output = new LoopAudioOutput("Low life", BufferMilliseconds, _mixer);
        return true;
    }
}
