using System;

namespace LeAccess.Accessibility.Audio;

// Stereo buffers for SoundPreview: a centred excerpt of a loop or a steady tone, faded at both ends.
internal static class PreviewClips
{
    internal const int SampleRate = NaturalCueBank.SampleRate, Channels = 2;
    internal const double FadeSeconds = 0.1;
    // Equal-power centre, as the loop mixer places an object straight ahead.
    private static readonly float Centre = MathF.Sqrt(0.5f);

    /// <summary>The first seconds of a mono loop (repeated if shorter) at the given gain, centred.</summary>
    internal static float[] Loop(float[] mono, float gain, double seconds)
    {
        if (mono == null || mono.Length == 0 || !float.IsFinite(gain) || !(gain > 0) || !(seconds > 0))
            return Array.Empty<float>();
        int frames = (int)(seconds * SampleRate);
        var samples = new float[frames * Channels];
        for (int frame = 0; frame < frames; frame++)
        {
            float value = mono[frame % mono.Length] * gain * Fade(frame, frames);
            samples[frame * Channels] = Math.Clamp(value * Centre, -1f, 1f);
            samples[frame * Channels + 1] = Math.Clamp(value * Centre, -1f, 1f);
        }
        return samples;
    }

    /// <summary>A steady sonar-like tone (triangle and sine blend) at the given frequency and amplitude, centred.</summary>
    internal static float[] Tone(double frequency, float amplitude, double seconds)
    {
        if (!(frequency > 0) || !float.IsFinite(amplitude) || !(amplitude > 0) || !(seconds > 0))
            return Array.Empty<float>();
        int frames = (int)(seconds * SampleRate);
        var mono = new float[frames];
        double phase = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            double triangle = 2.0 * Math.Abs(2.0 * phase - 1.0) - 1.0;
            mono[frame] = (float)((triangle * 0.7 + Math.Sin(2 * Math.PI * phase) * 0.3) * amplitude);
            phase += frequency / SampleRate;
            if (phase >= 1) phase -= 1;
        }
        return Loop(mono, 1, seconds);
    }

    private static float Fade(int frame, int frames)
    {
        int fade = (int)(FadeSeconds * SampleRate);
        return Math.Min(1f, Math.Min((float)(frame + 1) / fade, (float)(frames - frame) / fade));
    }
}
