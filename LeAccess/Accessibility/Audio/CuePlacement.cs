using System;

namespace LeAccess.Accessibility.Audio;

// Camera-relative placement shared by world cues: equal-power stereo and a lower pitch toward screen-down.
internal readonly struct CuePlacement
{
    // D2RAccess entity_cue_synth.cpp: a major-third drop marks the rear without changing cue identity.
    internal const double RearPitch = 0.80;

    internal CuePlacement(float pan, float forward)
    {
        double rightAxis = float.IsFinite(pan) ? Math.Clamp(pan, -1f, 1f) : 0;
        double frontAxis = float.IsFinite(forward) ? Math.Clamp(forward, -1f, 1f) : 1;
        Pitch = 1 + (RearPitch - 1) * Math.Clamp(-frontAxis, 0, 1);
        Left = Math.Sqrt((1 - rightAxis) / 2);
        Right = Math.Sqrt((1 + rightAxis) / 2);
    }

    internal double Left { get; }
    internal double Right { get; }
    internal double Pitch { get; }

    // Up to twice the reference level so a boosted sound kind can exceed 100%.
    internal const float MaxGain = 2f;

    internal static double Gain(float gain) => float.IsFinite(gain) ? Math.Clamp(gain, 0f, MaxGain) : 0;
}
