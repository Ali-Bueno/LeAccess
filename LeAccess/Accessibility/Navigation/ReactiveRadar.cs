using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using UnityEngine;

namespace LeAccess.Accessibility.Navigation;

public static class ReactiveRadar
{
    private const string ConfigSection = "Reactive radar";
    private const float MinimumOpenDepthInBodyWidths = 2f;
    private const float MaximumOpenDepthInBodyWidths = 20f;
    // Device startup retries are spaced out to avoid doing audio initialization every frame.
    private const double AudioRetrySeconds = 5;
    // Retain the listening-tested 100 ms buffer; bounds span the original value and NAudio's default.
    private const int DefaultAudioBufferMilliseconds = 100;
    private const int MinimumAudioBufferMilliseconds = 50;
    private const int MaximumAudioBufferMilliseconds = 300;
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _volume;
    private static ConfigEntry<float> _openDepth;
    private static ConfigEntry<int> _audioBuffer;
    private static ConfigEntry<bool> _unexploredMark;
    private static readonly RadarWorldProbe World = new();
    private static readonly RadarCueQueue Queue = new();
    private static readonly RadarDiagnostics Diagnostics = new();
    private static readonly RadarTrace Trace = new();
    // Openings, walls, then marked (unexplored) openings, one per direction.
    private static readonly float[][] Cues = new float[RadarEngine.DirectionCount * 3][];
    private static System.Numerics.Vector2 _position;
    private static System.Numerics.Vector2[] _axes;
    private static RadarEngine _engine;
    private static RadarAudioOutput _audio;
    private static double _nextAudioRetry;
    private static string _lastAudioFailure;
    private static bool _hasSnapshot;
    private static string _lastFailure;
    private static string _lastUnavailableReason;
    private static int _cueVolume = -1;
    private static double _nextProbe;
    private static double _nextCue;

    public static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Announce changes between obstacles and open ground with directional sounds.");
        _volume = config.Bind(ConfigSection, "Volume", 100,
            new ConfigDescription("Percent of the Diablo 2 radar cue volume.", new AcceptableValueRange<int>(0, 100)));
        _openDepth = config.Bind(ConfigSection, "OpenDepthInBodyWidths", RadarSettings.DefaultOpenDepthInBodyWidths,
            new ConfigDescription("Shortest walkable depth, in player body widths, that can sound open when the space goes on beyond it.",
                new AcceptableValueRange<float>(MinimumOpenDepthInBodyWidths, MaximumOpenDepthInBodyWidths)));
        _audioBuffer = config.Bind(ConfigSection, "AudioBufferMilliseconds", DefaultAudioBufferMilliseconds,
            new ConfigDescription("Audio buffer duration. Larger values give playback more tolerance for scheduling delays. Restart the game after changing this.",
                new AcceptableValueRange<int>(MinimumAudioBufferMilliseconds, MaximumAudioBufferMilliseconds)));
        _unexploredMark = config.Bind(ConfigSection, "UnexploredMark", true,
            "End an opening cue with a double pip when the exploration scanner lists an unexplored area that way.");
        Plugin.Log.LogInfo("Reactive radar configured (screen cardinals, NavMesh terrain)");
        _lastUnavailableReason = "initializing";
    }

    public static void Update()
    {
        if (_enabled == null) return;
        try
        {
            string suspensionReason = !_enabled.Value ? "disabled in configuration"
                : _volume.Value == 0 ? "volume is zero" : NavigationAudioState.SuspensionReason;
            if (suspensionReason == null && !GameplayInputState.OwnsInput())
                suspensionReason = "gameplay input not active";
            if (suspensionReason != null)
            {
                Suspend(suspensionReason);
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            if (now >= _nextProbe)
            {
                _nextProbe = now + RadarSettings.PollSeconds;
                if (!World.TryCapture(_openDepth.Value, out var snapshot))
                {
                    Suspend(World.UnavailableReason);
                    return;
                }

                if (_engine == null || !ReferenceEquals(_engine.Settings, snapshot.Settings))
                {
                    ResetPlayback();
                    _engine = new RadarEngine(snapshot.Settings);
                }

                ulong revision = _engine.Revision;
                var events = _engine.Tick(snapshot.Position, snapshot.Axes, snapshot.Readings, now, snapshot.Session);
                Diagnostics.Observe(_engine, _engine.Revision != revision);
                if (_engine.Revision != revision) Trace.ClearReadings();
                Trace.Record(snapshot.Readings, snapshot.EdgeDistance, snapshot.AgentRadius);
                LogEvents(events, snapshot, now);
                if (_engine.Revision != revision)
                {
                    Queue.Clear();
                    if (_hasSnapshot) _audio?.Silence();
                    _nextCue = now;
                }
                _position = snapshot.Position;
                _axes = snapshot.Axes;
                Queue.Retain(_engine.States);
                Queue.Enqueue(events);
                Diagnostics.Pending(now, Queue.PendingCount > 0);
                string status = Diagnostics.Report(now, snapshot, _engine, Queue.PendingCount,
                    static () => _audio == null ? "not initialized" : _audio.Diagnostics.ToString());
                if (status != null) Plugin.Log.LogInfo(FormattableString.Invariant($"{status} edge={snapshot.EdgeDistance:0.00} {Trace.Summary()}"));
                _hasSnapshot = true;
                _lastFailure = null;
                if (_lastUnavailableReason != null)
                {
                    Plugin.Log.LogInfo("Reactive radar ready; silent navigation baseline established");
                    _lastUnavailableReason = null;
                }
            }

            if (!_hasSnapshot || now < _nextCue || !EnsureAudio(now) || _audio.IsPlaying) return;
            if (!Queue.TryPeek(out var direction, out var state))
            {
                WorldAudioReservation.Cancel(WorldAudioSource.Terrain);
                return;
            }
            EnsureCues();
            bool marked = state == RadarState.Open && _unexploredMark.Value &&
                RadarUnexploredMarks.Marked(direction, _position, _axes, now);
            var cue = Cues[CueIndex(state, direction, marked)];
            double duration = (double)cue.Length / (RadarCueSynthesizer.SampleRate * RadarCueSynthesizer.Channels);
            if (!WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, now, duration, _audioBuffer.Value)) return;
            Queue.TryTake(out _, out _);
            _audio.Play(cue);
            Diagnostics.Submitted(now, Queue.PendingCount > 0);
            Plugin.Log.LogInfo(FormattableString.Invariant($"Reactive radar cue: t={now:0.00} direction={direction} state={state}{(marked ? " unexplored" : "")}"));
            _nextCue = now + (double)cue.Length / (RadarCueSynthesizer.SampleRate * RadarCueSynthesizer.Channels)
                + RadarCueSynthesizer.CueGapSeconds;
        }
        catch (Exception ex)
        {
            Suspend("probe error");
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Reactive radar unavailable: {ex}");
        }
    }

    // Each generated cue with its recent readings.
    private static void LogEvents(RadarState[] events, RadarWorldSnapshot snapshot, double now)
    {
        for (int i = 0; i < events.Length; i++)
            if (events[i] != RadarState.Unknown)
                Plugin.Log.LogInfo(Trace.Event(i, events[i], now, snapshot.Position, snapshot.EdgeDistance));
    }

    private static bool EnsureAudio(double now)
    {
        if (_audio?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Terrain);
        if (now < _nextAudioRetry) return false;
        try
        {
            if (_audio != null)
            {
                _nextAudioRetry = now + AudioRetrySeconds;
                bool recovered = _audio.TryRecover(out string status);
                if (status != null && status != _lastAudioFailure)
                {
                    if (recovered) Plugin.Log.LogInfo(status);
                    else Plugin.Log.LogWarning(status);
                }
                _lastAudioFailure = recovered ? null : status;
                return recovered;
            }
            _audio = new RadarAudioOutput("Reactive radar", _audioBuffer.Value);
            _lastAudioFailure = null;
            return true;
        }
        catch (Exception ex)
        {
            _nextAudioRetry = now + AudioRetrySeconds;
            if (_lastAudioFailure != ex.Message)
                Plugin.Log.LogWarning($"Reactive radar audio initialization failed: {ex.Message}");
            _lastAudioFailure = ex.Message;
            return false;
        }
    }

    private static void EnsureCues()
    {
        if (_cueVolume == _volume.Value) return;
        _cueVolume = _volume.Value;
        foreach (var state in new[] { RadarState.Open, RadarState.Blocked })
            foreach (RadarDirection direction in Enum.GetValues(typeof(RadarDirection)))
                Cues[CueIndex(state, direction)] = RadarCueSynthesizer.Synthesize(state, direction, _cueVolume / 100f);
        foreach (RadarDirection direction in Enum.GetValues(typeof(RadarDirection)))
            Cues[CueIndex(RadarState.Open, direction, true)] =
                RadarCueSynthesizer.Synthesize(RadarState.Open, direction, _cueVolume / 100f, unexplored: true);
    }

    private static int CueIndex(RadarState state, RadarDirection direction, bool marked = false) =>
        (marked ? 2 * RadarEngine.DirectionCount : state == RadarState.Blocked ? RadarEngine.DirectionCount : 0) + (int)direction;

    private static void Suspend(string reason = null)
    {
        if (reason != null && _lastUnavailableReason != reason)
        {
            Plugin.Log.LogInfo($"Reactive radar suspended: {reason}");
            _lastUnavailableReason = reason;
        }
        World.Reset();
        ResetPlayback();
    }

    private static void ResetPlayback()
    {
        WorldAudioReservation.Cancel(WorldAudioSource.Terrain);
        Queue.Clear();
        _engine?.Reset();
        if (_hasSnapshot) _audio?.Silence();
        _hasSnapshot = false;
        _nextCue = 0;
    }

    public static void Cleanup()
    {
        Suspend();
        _audio?.Dispose();
        _audio = null;
        _nextAudioRetry = 0;
        _lastAudioFailure = null;
        _lastUnavailableReason = null;
        _lastFailure = null;
        _engine = null;
        Trace.Reset();
        _enabled = null;
        _unexploredMark = null;
        _axes = null;
        _cueVolume = -1;
        _nextProbe = 0;
        Diagnostics.Reset();
        World.Reset();
        NavigationAudioState.Reset();
    }
}
