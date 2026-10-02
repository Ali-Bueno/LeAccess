using System;
using System.Diagnostics;
using System.Threading;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

// Consumption means copied into NAudio's buffer, not confirmed audible output.
public readonly record struct RadarAudioDiagnostics(bool OutputInitialized, PlaybackState PlaybackState,
    long SubmittedCues, long SubmittedSamples, long ConsumedSamples, int PendingSamples,
    long CanceledSamples, long ReadCalls, int Stops = 0, int Recoveries = 0, string LastFailure = null,
    double BufferedMilliseconds = 0, long CallbackStalls = 0, long CueCallbackStalls = 0,
    double MaxCallbackGapMilliseconds = 0)
{
    public override string ToString() =>
        $"initialized={OutputInitialized} stream={PlaybackState} submitted={SubmittedCues} cues/{SubmittedSamples} samples " +
        $"consumed={ConsumedSamples} pending={PendingSamples} canceled={CanceledSamples} reads={ReadCalls} " +
        $"stops={Stops} recoveries={Recoveries} failure={LastFailure ?? "none"} " +
        FormattableString.Invariant($"buffer_ms={BufferedMilliseconds:F1} callback_stalls={CallbackStalls} cue_callback_stalls={CueCallbackStalls} max_callback_gap_ms={MaxCallbackGapMilliseconds:F1}");
}

public sealed class RadarAudioOutput : IDisposable
{
    private const int PlaybackLatencyMilliseconds = 50;
    private readonly object _outputGate = new();
    private readonly CueSampleProvider _samples = new();
    private readonly Func<IWavePlayer> _createOutput;
    private readonly string _description;
    private IWavePlayer _waveOut;
    private bool _completionConfirmed;
    private Exception _lastPlaybackException;
    private int _stops;
    private int _recoveries;
    private bool _disposed;

    public bool IsPlaying => !_disposed && _samples.IsPlaying;
    public bool IsRunning => !_disposed && _waveOut?.PlaybackState == PlaybackState.Playing;
    public RadarAudioDiagnostics Diagnostics
    {
        get
        {
            lock (_outputGate)
                return _samples.GetDiagnostics(!_disposed && _waveOut != null,
                    _disposed ? PlaybackState.Stopped : _waveOut?.PlaybackState ?? PlaybackState.Stopped)
                    with { Stops = _stops, Recoveries = _recoveries, LastFailure = FailureDescription() };
        }
    }

    public RadarAudioOutput() : this("Reactive radar") { }

    public RadarAudioOutput(string description, int playbackLatencyMilliseconds = PlaybackLatencyMilliseconds)
        : this(CreateOutputFactory(playbackLatencyMilliseconds), description) { }

    private static Func<IWavePlayer> CreateOutputFactory(int latency)
    {
        if (latency <= 0) throw new ArgumentOutOfRangeException(nameof(latency));
        return () => new WaveOutEvent { DesiredLatency = latency };
    }

    internal RadarAudioOutput(Func<IWavePlayer> createOutput, string description = "Reactive radar")
    {
        _createOutput = createOutput ?? throw new ArgumentNullException(nameof(createOutput));
        _description = description;
        InitializeOutput();
    }

    private void InitializeOutput()
    {
        _waveOut = _createOutput();
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        try
        {
            _waveOut.Init(_samples);
            double bufferedMilliseconds = PlaybackLatencyMilliseconds;
            if (_waveOut is WaveOutEvent wave)
            {
                int bytes = _samples.WaveFormat.ConvertLatencyToByteSize(
                    (wave.DesiredLatency + wave.NumberOfBuffers - 1) / wave.NumberOfBuffers);
                bufferedMilliseconds = 1000.0 * bytes * wave.NumberOfBuffers / _samples.WaveFormat.AverageBytesPerSecond;
            }
            _samples.ConfigureBuffering(bufferedMilliseconds);
            _completionConfirmed = false;
            _waveOut.Play();
        }
        catch
        {
            ReleaseOutput();
            throw;
        }
    }

    // Called by the main thread at the caller's existing audio retry interval.
    public bool TryRecover(out string status)
    {
        lock (_outputGate)
        {
            status = null;
            if (_disposed) return false;
            if (_waveOut?.PlaybackState == PlaybackState.Playing) return true;
            if (!_completionConfirmed) return false;
            string reason = FailureDescription();
            _samples.Silence();
            try
            {
                ReleaseOutput();
                InitializeOutput();
                _recoveries++;
                status = $"{_description} audio restarted after {reason}";
                return true;
            }
            catch (Exception ex)
            {
                _lastPlaybackException = ex;
                _completionConfirmed = true;
                status = $"{_description} audio restart failed: {FailureDescription()}";
                return false;
            }
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs stopped)
    {
        lock (_outputGate)
        {
            if (_disposed || !ReferenceEquals(sender, _waveOut)) return;
            _lastPlaybackException = stopped.Exception;
            _stops++;
            // NAudio raises this only after DoPlayback has returned and its final state was set.
            _completionConfirmed = true;
        }
    }

    private string FailureDescription() => _lastPlaybackException != null
        ? $"{_lastPlaybackException.GetType().Name}: {_lastPlaybackException.Message}"
        : _stops == 0 ? null : "stream ended without an error";

    private void ReleaseOutput()
    {
        var output = _waveOut;
        _waveOut = null;
        if (output == null) return;
        output.PlaybackStopped -= OnPlaybackStopped;
        output.Dispose();
    }

    public void Play(float[] stereoSamples)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RadarAudioOutput));
        if (stereoSamples == null)
            throw new ArgumentNullException(nameof(stereoSamples));
        if (stereoSamples.Length % RadarCueSynthesizer.Channels != 0)
            throw new ArgumentException("Samples must contain complete stereo frames.", nameof(stereoSamples));
        _samples.Play(stereoSamples);
    }

    public void Silence()
    {
        if (_disposed)
            return;
        _samples.Silence();
    }

    public void Dispose()
    {
        lock (_outputGate)
        {
            if (_disposed) return;
            _disposed = true;
            _samples.Silence();
            ReleaseOutput();
        }
    }

    private sealed class CueSampleProvider : ISampleProvider
    {
        private readonly object _gate = new();
        private float[] _cue = Array.Empty<float>();
        private int _offset;
        private long _submittedCues;
        private long _submittedSamples;
        private long _consumedSamples;
        private long _canceledSamples;
        private long _readCalls;
        private readonly Func<long> _timestamp;
        private bool _hasReadTimestamp;
        private long _lastReadTimestamp, _maxCallbackGap, _callbackStalls, _cueCallbackStalls;
        private double _bufferedMilliseconds = PlaybackLatencyMilliseconds;

        public CueSampleProvider() : this(Stopwatch.GetTimestamp) { }
        public CueSampleProvider(Func<long> timestamp) => _timestamp = timestamp;

        public void ConfigureBuffering(double milliseconds)
        {
            lock (_gate)
            {
                _bufferedMilliseconds = milliseconds;
                _hasReadTimestamp = false;
            }
        }

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(
            RadarCueSynthesizer.SampleRate, RadarCueSynthesizer.Channels);

        public bool IsPlaying
        {
            get { lock (_gate) return _offset < _cue.Length; }
        }

        public RadarAudioDiagnostics GetDiagnostics(bool initialized, PlaybackState playbackState)
        {
            lock (_gate)
                return new RadarAudioDiagnostics(initialized, playbackState, _submittedCues,
                    _submittedSamples, _consumedSamples, _cue.Length - _offset, _canceledSamples, _readCalls,
                    BufferedMilliseconds: _bufferedMilliseconds, CallbackStalls: _callbackStalls,
                    CueCallbackStalls: _cueCallbackStalls, MaxCallbackGapMilliseconds: 1000.0 * _maxCallbackGap / Stopwatch.Frequency);
        }

        public void Play(float[] stereoSamples)
        {
            lock (_gate)
            {
                if (_offset < _cue.Length)
                    throw new InvalidOperationException("Wait for the current radar cue to finish.");
                _cue = (float[])stereoSamples.Clone();
                _offset = 0;
                Interlocked.Increment(ref _submittedCues);
                Interlocked.Add(ref _submittedSamples, _cue.Length);
            }
        }

        public void Silence()
        {
            lock (_gate)
            {
                Interlocked.Add(ref _canceledSamples, _cue.Length - _offset);
                _cue = Array.Empty<float>();
                _offset = 0;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (_gate)
            {
                Interlocked.Increment(ref _readCalls);
                long timestamp = _timestamp();
                if (_hasReadTimestamp)
                {
                    long gap = timestamp - _lastReadTimestamp;
                    _maxCallbackGap = Math.Max(_maxCallbackGap, gap);
                    // A callback later than all queued audio is a stall risk, not proof of a device underrun.
                    if (1000.0 * gap / Stopwatch.Frequency > _bufferedMilliseconds)
                    {
                        _callbackStalls++;
                        if (_offset < _cue.Length) _cueCallbackStalls++;
                    }
                }
                _lastReadTimestamp = timestamp;
                _hasReadTimestamp = true;
                int available = Math.Min(count, _cue.Length - _offset);
                if (available > 0)
                {
                    // NAudio may supply a byte-backed WaveBuffer view: Array.Copy/Clear use its byte[] runtime type.
                    for (int index = 0; index < available; index++)
                        buffer[offset + index] = _cue[_offset + index];
                    _offset += available;
                    Interlocked.Add(ref _consumedSamples, available);
                }
                for (int index = available; index < count; index++)
                    buffer[offset + index] = 0;
            }
            return count;
        }
    }
}
