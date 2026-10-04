using LeAccess.Accessibility.Exploration;
using static Program;

internal static class SynthTests
{
    private const int Rate = BeaconCueSynthesizer.SampleRate, Channels = BeaconCueSynthesizer.Channels;

    internal static void Run()
    {
        int note = (int)(BeaconCueSynthesizer.NoteSeconds * Rate), gap = (int)(BeaconCueSynthesizer.GapSeconds * Rate);
        var ping = BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, 1, 1);
        Check(ping.Length == note * Channels, $"ping is one 80 ms note ({ping.Length})");
        Check(BeaconCueSynthesizer.Synthesize(BeaconSound.Locked, 0, 1, 1).Length == (2 * note + gap) * Channels, "lock is two notes");
        Check(BeaconCueSynthesizer.Synthesize(BeaconSound.Cleared, 0, 1, 1).Length == (2 * note + gap) * Channels, "cancel is two notes");
        Check(BeaconCueSynthesizer.Synthesize(BeaconSound.Arrived, 0, 1, 1).Length == (3 * note + 2 * gap) * Channels, "arrival is three notes");
        foreach (BeaconSound sound in Enum.GetValues<BeaconSound>())
            Near(BeaconCueSynthesizer.Synthesize(sound, 0, 1, 1).Length / (double)(Rate * Channels),
                BeaconCueSynthesizer.Duration(sound), 1e-9, $"{sound} duration");

        float peak = Peak(ping);
        Check(peak > 0.05f && peak <= BeaconCueSynthesizer.Level * 1.15 + 1e-6, $"ping peak ({peak})");
        Check(Peak(BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, 1, 2)) == peak, "gain is clamped to full");
        Check(Peak(BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, 1, 0.5f)) < peak, "volume lowers the ping");
        Check(Peak(BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, 1, 0)) == 0, "zero volume is silent");
        Check(Peak(BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, 1, float.NaN)) == 0, "unknown volume is silent");
        var odd = BeaconCueSynthesizer.Synthesize(BeaconSound.Arrived, float.NaN, float.NaN, 1);
        Check(odd.All(float.IsFinite) && Peak(odd) > 0, "undefined bearing plays centred without NaN");

        var right = BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 1, 1, 1);
        Check(Peak(right, 0) == 0 && Peak(right, 1) > 0, "full right pan");
        var left = BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, -1, 1, 1);
        Check(Peak(left, 1) == 0 && Peak(left, 0) > 0, "full left pan");
        var half = BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0.5f, 1, 1);
        Check(Peak(half, 1) > Peak(half, 0) && Peak(half, 0) > 0, "partial pan leans right");
        Near(Peak(ping, 0), Peak(ping, 1), 1e-6, "centred ping is balanced");

        double front = Crossings(ping), rear = Crossings(BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, -1, 1));
        Near(BeaconCueSynthesizer.RearPitch, rear / front, 0.03, "a target behind lowers the pitch");
        double side = Crossings(BeaconCueSynthesizer.Synthesize(BeaconSound.Ping, 0, 0, 1));
        Check(side == front, "a target to the side keeps the front pitch");

        var click = BeaconCueSynthesizer.Click(1);
        Check(click.Length == (int)(BeaconCueSynthesizer.ClickSeconds * Rate) * Channels, "click is 20 ms");
        Check(Peak(click) > 0 && Peak(click) < peak && click.All(float.IsFinite), "click is quieter than the ping");
        Check(Peak(BeaconCueSynthesizer.Click(0)) == 0, "silent click at zero volume");
    }

    private static float Peak(float[] samples, int channel = -1)
    {
        float peak = 0;
        for (int index = 0; index < samples.Length; index++)
            if (channel < 0 || index % Channels == channel) peak = Math.Max(peak, Math.Abs(samples[index]));
        return peak;
    }

    private static double Crossings(float[] samples)
    {
        int count = 0;
        float previous = 0;
        for (int index = 0; index < samples.Length; index += Channels)
        {
            float sample = samples[index];
            if (sample == 0) continue;
            if (previous != 0 && Math.Sign(sample) != Math.Sign(previous)) count++;
            previous = sample;
        }
        return count;
    }
}
