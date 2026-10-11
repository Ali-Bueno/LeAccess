using System;
using System.IO;

namespace LeAccess.Accessibility.Audio;

// The Diablo IV low-life start sound and heartbeat loop (tools/import_audio.py), mono 48 kHz, peak-normalized.
internal static class LowLifeClips
{
    internal const int SampleRate = NaturalCueBank.SampleRate;
    // The world loops' peak level (WorldLoopBank), so the heartbeat sits among the mod's other recordings.
    private const float PeakLevel = 0.9f;
    private static readonly Lazy<float[]> StartClip = new(() => Load("LeAccess.Sounds.low_life_start.wav"));
    private static readonly Lazy<float[]> LoopClip = new(() => Load("LeAccess.Sounds.Loops.low_life.wav"));

    internal static string LastFailure { get; private set; }

    /// <summary>Played once on entering low life; empty when it failed to load.</summary>
    internal static float[] Start => StartClip.Value;

    /// <summary>Repeated while life stays low; empty when it failed to load.</summary>
    internal static float[] Loop => LoopClip.Value;

    private static float[] Load(string resource)
    {
        try
        {
            using Stream stream = typeof(LowLifeClips).Assembly.GetManifestResourceStream(resource);
            float[] samples = PcmWaveReader.ReadMono48(stream, NaturalCueBank.MaximumLoopFrames);
            float peak = 0;
            foreach (float sample in samples) peak = Math.Max(peak, Math.Abs(sample));
            if (peak == 0) throw new InvalidDataException("The recording contains only silence.");
            float scale = PeakLevel / peak;
            for (int index = 0; index < samples.Length; index++) samples[index] *= scale;
            return samples;
        }
        catch (Exception ex)
        {
            LastFailure = $"{resource}: {ex.Message}";
            return Array.Empty<float>();
        }
    }
}
