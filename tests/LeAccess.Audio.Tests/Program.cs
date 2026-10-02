using System.Reflection;
using LeAccess.Accessibility.Audio;
using NAudio.Wave;

internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        try
        {
            RadarCancellation();
            RadarDiagnostics();
            Console.WriteLine($"Audio provider tests passed: {_checks} checks; no audio device opened.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void RadarCancellation()
    {
        var radar = new Provider("LeAccess.Accessibility.Audio.RadarAudioOutput+CueSampleProvider");
        float[] cue = { 0.1f, 0.2f, 0.3f, 0.4f };
        radar.Call("Play", cue);
        cue[0] = 9;
        Check(radar.Property<bool>("IsPlaying"), "Radar reports pending samples.");
        Check(Read(radar, 2).SequenceEqual(new[] { 0.1f, 0.2f }),
            "Queued radar samples are independent of the caller's buffer.");
        radar.Call("Silence");
        Check(!radar.Property<bool>("IsPlaying"), "Suspending radar releases its pending cue.");
        Check(Read(radar, 128).All(sample => sample == 0),
            "Suspended radar continuously supplies silence without restarting the device.");
        radar.Call("Play", new float[] { -0.1f, -0.2f });
        Check(Read(radar, 4).SequenceEqual(new[] { -0.1f, -0.2f, 0f, 0f }),
            "Radar resumes with the new cue and zero fills the remaining buffer.");
        Check(!radar.Property<bool>("IsPlaying"), "Radar completes the resumed cue.");
    }

    private static void RadarDiagnostics()
    {
        var radar = new Provider("LeAccess.Accessibility.Audio.RadarAudioOutput+CueSampleProvider");
        RadarAudioDiagnostics Snapshot() =>
            (RadarAudioDiagnostics)radar.Call("GetDiagnostics", true, PlaybackState.Playing);
        Read(radar, 8);
        var idle = Snapshot();
        Check(idle.ReadCalls == 1 && idle.ConsumedSamples == 0 && idle.SubmittedSamples == 0,
            "Silent callbacks show stream progress without claiming any cue consumption.");

        radar.Call("Play", new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f });
        Read(radar, 2);
        var partial = Snapshot();
        Check(partial.SubmittedCues == 1 && partial.SubmittedSamples == 6,
            "Diagnostics record accepted submissions and their complete sample count.");
        Check(partial.ConsumedSamples == 2 && partial.PendingSamples == 4 && partial.CanceledSamples == 0,
            "Partial playback counts only samples actually copied into the output buffer.");
        try
        {
            radar.Call("Play", new float[] { 0.7f, 0.8f });
            throw new InvalidOperationException("A pending cue must reject replacement.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException) { }
        Check(Snapshot() == partial, "Rejected cue replacement does not advance diagnostic counters.");

        radar.Call("Silence");
        Read(radar, 8);
        var canceled = Snapshot();
        Check(canceled.ConsumedSamples == 2 && canceled.CanceledSamples == 4 && canceled.PendingSamples == 0,
            "Canceled samples remain distinguishable from consumed samples.");
        Check(canceled.ReadCalls == 3, "Silent reads continue advancing the callback counter.");
        radar.Call("Silence");
        Check(Snapshot() == canceled, "Repeated silence does not count a canceled cue twice.");

        radar.Call("Play", new float[] { 0.7f, 0.8f });
        Read(radar, 8);
        var resumed = Snapshot();
        Check(resumed.SubmittedCues == 2 && resumed.SubmittedSamples == 8 && resumed.ConsumedSamples == 4,
            "Counters persist through cancellation and resumed output, excluding zero padding.");
        Check(resumed.SubmittedSamples == resumed.ConsumedSamples + resumed.CanceledSamples + resumed.PendingSamples,
            "Every submitted sample is accounted for as consumed, canceled, or pending.");
        var stopped = (RadarAudioDiagnostics)radar.Call("GetDiagnostics", false, PlaybackState.Stopped);
        Check(!stopped.OutputInitialized && stopped.PlaybackState == PlaybackState.Stopped,
            "The snapshot carries device initialization and stream playback state independently of cue counters.");
        string description = stopped.ToString();
        Check(description.Contains("stream=Stopped") && description.Contains("submitted=2 cues/8 samples") &&
            description.Contains("consumed=4") && description.Contains("pending=0"),
            "Diagnostic text exposes stream state and submission versus consumption for runtime logs.");
    }

    private static float[] Read(Provider provider, int count)
    {
        var samples = new float[count];
        provider.Samples.Read(samples, 0, count);
        return samples;
    }

    private static void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(description);
    }

    // Inspect only managed sample providers; constructing audio outputs would open real hardware.
    private sealed class Provider
    {
        private readonly Type _type;
        private readonly object _instance;
        public ISampleProvider Samples => (ISampleProvider)_instance;

        public Provider(string typeName, params object[] arguments)
        {
            _type = typeof(RadarAudioOutput).Assembly.GetType(typeName, throwOnError: true);
            _instance = Activator.CreateInstance(_type, BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic, binder: null, arguments, culture: null);
        }

        public object Call(string method, params object[] arguments) =>
            _type.GetMethod(method).Invoke(_instance, arguments);

        public T Property<T>(string property) => (T)_type.GetProperty(property).GetValue(_instance);
    }
}
