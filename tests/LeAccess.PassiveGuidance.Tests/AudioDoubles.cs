namespace LeAccess.Accessibility.Audio;

internal enum NaturalCueKind { UnexploredRoom }

internal static class NaturalCueBank
{
    internal static float Pan, Forward, Gain;
    internal static bool Missing;
    internal static string LastFailure => Missing ? "Missing route sample" : null;
    internal static double Duration(NaturalCueKind kind) => Missing ? 0 : 0.35;
    internal static float[] Build(NaturalCueKind kind, float pan, float forward, float gain)
    { Pan = pan; Forward = forward; Gain = gain; return new float[2]; }
}

internal sealed class RadarAudioOutput : IDisposable
{
    internal static bool FailCreation, Running = true, Playing, Recoverable = true;
    internal static int Creations, Plays, Silences, Recoveries, Disposals;
    internal bool IsPlaying => Playing;
    internal bool IsRunning => Running;
    internal RadarAudioOutput(string name, int buffer)
    { Creations++; if (FailCreation) throw new Exception("Device unavailable"); }
    internal bool TryRecover(out string status)
    { Recoveries++; status = "Device recovery"; return Running = Recoverable; }
    internal void Play(float[] samples) => Plays++;
    internal void Silence() => Silences++;
    public void Dispose() => Disposals++;
    internal static void Reset()
    {
        FailCreation = Playing = false;
        Running = Recoverable = true;
        Creations = Plays = Silences = Recoveries = Disposals = 0;
    }
}
