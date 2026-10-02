using System.Reflection;
using System.Diagnostics;
using LeAccess.Accessibility.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        try
        {
            AdapterReadsRealByteBuffer();
            FragmentedCuesRemainContinuous();
            CallbackTimingSeparatesStallsFromPlayback();
            RecoveryWaitsForPlaybackCompletion();
            FailedReopenCanRetry();
            Console.WriteLine($"Radar audio tests passed: {_checks} checks; no audio device opened.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void AdapterReadsRealByteBuffer()
    {
        var providerType = typeof(RadarAudioOutput).GetNestedType("CueSampleProvider", BindingFlags.NonPublic);
        var provider = (ISampleProvider)Activator.CreateInstance(providerType, nonPublic: true);
        var adapter = new SampleToWaveProvider(provider);
        float[] cue = { 0.1f, -0.2f, 0.3f, -0.4f };
        providerType.GetMethod("Play").Invoke(provider, new object[] { cue });
        byte[] bytes = Enumerable.Repeat((byte)0x7F, 40).ToArray();
        Check(adapter.Read(bytes, 8, 24) == 24, "The actual NAudio adapter fills the requested byte count.");
        float[] actual = Enumerable.Range(0, 6).Select(index => BitConverter.ToSingle(bytes, 8 + index * sizeof(float))).ToArray();
        Check(actual.SequenceEqual(cue.Concat(new[] { 0f, 0f })),
            "Cue samples and all silent padding survive NAudio's byte-backed float view.");
        Check(bytes.Take(8).Concat(bytes.Skip(32)).All(value => value == 0x7F),
            "The real adapter respects byte offsets and leaves surrounding data unchanged.");
        Array.Fill(bytes, (byte)0x7F);
        adapter.Read(bytes, 8, 24);
        Check(bytes.Skip(8).Take(24).All(value => value == 0),
            "Idle output clears every byte of every requested float sample.");
        Check(bytes.Take(8).Concat(bytes.Skip(32)).All(value => value == 0x7F),
            "Idle output also preserves bytes outside the requested range.");
    }

    private static void FragmentedCuesRemainContinuous()
    {
        var providerType = typeof(RadarAudioOutput).GetNestedType("CueSampleProvider", BindingFlags.NonPublic);
        foreach (var state in new[] { LeAccess.Accessibility.Navigation.RadarState.Blocked,
            LeAccess.Accessibility.Navigation.RadarState.Open })
        {
            var provider = (ISampleProvider)Activator.CreateInstance(providerType, nonPublic: true);
            var adapter = new SampleToWaveProvider(provider);
            float[] cue = RadarCueSynthesizer.Synthesize(state, LeAccess.Accessibility.Navigation.RadarDirection.Up, 1);
            providerType.GetMethod("Play").Invoke(provider, new object[] { cue });
            int samplesPerRead = RadarCueSynthesizer.SampleRate * RadarCueSynthesizer.Channels * 25 / 1000;
            var actual = new List<float>();
            while (actual.Count < cue.Length + samplesPerRead)
            {
                byte[] bytes = Enumerable.Repeat((byte)0x7F, samplesPerRead * sizeof(float)).ToArray();
                Check(adapter.Read(bytes, 0, bytes.Length) == bytes.Length, "Each 25 ms callback fills its entire device buffer.");
                actual.AddRange(Enumerable.Range(0, samplesPerRead).Select(index => BitConverter.ToSingle(bytes, index * sizeof(float))));
            }
            Check(actual.Take(cue.Length).SequenceEqual(cue),
                "A real radar cue crosses the current 25 ms device-buffer boundaries without gaps, repetition or truncation.");
            Check(actual.Skip(cue.Length).All(sample => sample == 0),
                "Silence starts only after the complete cue has been copied.");
        }
    }

    private static void CallbackTimingSeparatesStallsFromPlayback()
    {
        var providerType = typeof(RadarAudioOutput).GetNestedType("CueSampleProvider", BindingFlags.NonPublic);
        long now = 0;
        var provider = (ISampleProvider)Activator.CreateInstance(providerType, new object[] { (Func<long>)(() => now) });
        var adapter = new SampleToWaveProvider(provider);
        byte[] bytes = new byte[16];
        var configure = providerType.GetMethod("ConfigureBuffering");
        var play = providerType.GetMethod("Play");
        RadarAudioDiagnostics Status() => (RadarAudioDiagnostics)providerType.GetMethod("GetDiagnostics")
            .Invoke(provider, new object[] { true, PlaybackState.Playing });
        void ReadAfter(double seconds)
        {
            now += (long)(seconds * Stopwatch.Frequency);
            adapter.Read(bytes, 0, bytes.Length);
        }

        configure.Invoke(provider, new object[] { 100.0 });
        ReadAfter(0);
        ReadAfter(0.05);
        ReadAfter(0.10);
        Check(Status().CallbackStalls == 0 && Status().BufferedMilliseconds == 100,
            "Callbacks within the configured queued duration are not misreported as stalled.");
        ReadAfter(0.11);
        Check(Status().CallbackStalls == 1 && Status().CueCallbackStalls == 0,
            "An idle callback gap longer than all queued audio is distinguished from an active cue gap.");
        play.Invoke(provider, new object[] { new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f } });
        ReadAfter(0.11);
        Check(Status().CallbackStalls == 2 && Status().CueCallbackStalls == 1 &&
            Status().ConsumedSamples == 4 && Status().PendingSamples == 2,
            "A late callback during a cue is counted while its samples still play unchanged.");
        Check(Math.Abs(Status().MaxCallbackGapMilliseconds - 110) < 0.001,
            "The largest observed callback gap uses the monotonic clock's actual frequency.");
        configure.Invoke(provider, new object[] { 100.0 });
        ReadAfter(2);
        Check(Status().CallbackStalls == 2 && Status().ConsumedSamples == 6,
            "Reinitialization excludes the deliberate device-recovery pause from callback stall counters.");
        Check(Status().ToString().Contains("callback_stalls=2") && Status().ToString().Contains("cue_callback_stalls=1"),
            "Heartbeat diagnostics report scheduling evidence without calling it a proven underrun.");
        configure.Invoke(provider, new object[] { 50.0 });
        ReadAfter(0);
        ReadAfter(0.06);
        Check(Status().CallbackStalls == 3 && Status().BufferedMilliseconds == 50,
            "The stall threshold follows the configured output buffer rather than a guessed fixed cutoff.");
    }

    private static void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(description);
    }

    private static void RecoveryWaitsForPlaybackCompletion()
    {
        var first = new FakeOutput();
        var second = new FakeOutput();
        var devices = new Queue<IWavePlayer>(new IWavePlayer[] { first, second });
        using var output = new RadarAudioOutput(() => devices.Dequeue());
        output.Play(new float[] { 0.1f, 0.2f });
        Check(output.IsRunning && output.TryRecover(out string idleStatus) && idleStatus == null,
            "Healthy playback is neither reopened nor reported as failed.");
        first.MarkStopped();
        Check(!output.TryRecover(out _) && !first.Disposed && devices.Count == 1,
            "Stopped state alone never permits replacement while the old worker may still be exiting.");
        first.CompleteStop(new InvalidOperationException("device disconnected"));
        Check(output.Diagnostics.Stops == 1 && output.Diagnostics.LastFailure.Contains("device disconnected"),
            "The actual playback failure is retained for main-thread diagnostics.");
        Check(output.TryRecover(out string recovered) && recovered.Contains("device disconnected"),
            "Confirmed worker completion permits controlled reopening with the original failure reported.");
        Check(first.Disposed && second.PlaybackState == PlaybackState.Playing && devices.Count == 0,
            "Recovery disposes the completed stream and starts exactly one replacement.");
        Check(output.Diagnostics.Recoveries == 1 && output.Diagnostics.PendingSamples == 0 &&
            output.Diagnostics.CanceledSamples == 2, "Recovery discards obsolete pending cues without losing diagnostic accounting.");
        output.Play(new float[] { 0.3f, 0.4f });
        byte[] bytes = second.Read(16);
        Check(BitConverter.ToSingle(bytes, 0) == 0.3f && BitConverter.ToSingle(bytes, 4) == 0.4f &&
            bytes.Skip(8).All(value => value == 0), "A recovered stream consumes a new cue through the actual NAudio byte adapter.");
        Check(output.Diagnostics.ConsumedSamples == 2, "Recovered audio consumption advances the existing counters.");
        output.Silence();
        Check(first.StopCalls == 0 && second.StopCalls == 0 && second.PlayCalls == 1,
            "Silencing and recovery never introduce a Stop/Play loop.");
        output.Dispose();
        Check(!output.TryRecover(out _) && second.Disposed, "Disposal prevents subsequent automatic recovery.");
    }

    private static void FailedReopenCanRetry()
    {
        var first = new FakeOutput();
        var failed = new FakeOutput { InitFailure = new InvalidOperationException("no output device") };
        var third = new FakeOutput();
        var devices = new Queue<IWavePlayer>(new IWavePlayer[] { first, failed, third });
        using var output = new RadarAudioOutput(() => devices.Dequeue());
        first.MarkStopped();
        first.CompleteStop(new InvalidOperationException("device removed"));
        Check(!output.TryRecover(out string failure) && failure.Contains("no output device"),
            "A failed reopen exposes its reason rather than treating an unusable output as ready.");
        Check(failed.Disposed && !output.Diagnostics.OutputInitialized && devices.Count == 1,
            "Failed initialization releases its device and performs only one attempt per caller request.");
        Check(output.TryRecover(out _) && third.PlayCalls == 1 && output.Diagnostics.Recoveries == 1,
            "The caller's later retry can recover after a temporarily unavailable device returns.");
    }

    private sealed class FakeOutput : IWavePlayer
    {
        private IWaveProvider _provider;
        public bool Disposed;
        public int StopCalls;
        public int PlayCalls;
        public Exception InitFailure;
        public float Volume { get; set; }
        public PlaybackState PlaybackState { get; private set; }
        public WaveFormat OutputWaveFormat => _provider.WaveFormat;
        public event EventHandler<StoppedEventArgs> PlaybackStopped;
        public void Init(IWaveProvider provider)
        {
            if (InitFailure != null) throw InitFailure;
            _provider = provider;
        }
        public void Play() { PlayCalls++; PlaybackState = PlaybackState.Playing; }
        public void Pause() => PlaybackState = PlaybackState.Paused;
        public void Stop() { StopCalls++; MarkStopped(); }
        public void MarkStopped() => PlaybackState = PlaybackState.Stopped;
        public void CompleteStop(Exception exception) => PlaybackStopped?.Invoke(this, new StoppedEventArgs(exception));
        public void Dispose() { Disposed = true; MarkStopped(); }
        public byte[] Read(int byteCount)
        {
            var bytes = new byte[byteCount];
            _provider.Read(bytes, 0, byteCount);
            return bytes;
        }
    }
}
