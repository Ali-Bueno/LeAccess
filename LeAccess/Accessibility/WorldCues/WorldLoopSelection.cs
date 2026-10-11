using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.WorldCues;

/// <summary>An eligible object with a loop: Right is the camera-relative pan, Front its metres ahead (negative behind), OnScreen whether the camera sees it.</summary>
internal readonly record struct LoopCandidate(int Id, NaturalCueKind Kind, float Distance, float Right, float Front,
    bool OnScreen = false);

// D2RAccess ground_items.cpp Tick/Spatialize: which loops sound and how loud, in Last Epoch metres.
internal static class WorldLoopSelection
{
    // D2RAccess MaxVoices and MaxContainerVoices: exits claim voices first, then containers, then loot,
    // so a pile of loot cannot silence the way out.
    internal const int MaxVoices = 8;
    internal const int MaxChestVoices = 3;
    // D2RAccess LoopVolume: near-source gain for peak-normalized recordings.
    internal const float NearGain = 0.4f;
    // D2RAccess MixGainLimit: headroom when nearby loops peak together.
    internal const float MixGainLimit = 0.5f;
    // Share of NearGain an on-screen loot loop keeps at the edge of the range (about -20 dB): quiet, never silent.
    internal const float ScreenEdgeShare = 0.1f;
    // D2RAccess RearPitch, blended across one subtile; here across one metre behind the player.
    internal const float RearPitch = 0.94f;
    internal const float RearBlendMetres = 1f;

    private static readonly HashSet<NaturalCueKind> Kinds = new();

    internal static bool IsLandmark(NaturalCueKind kind) => WorldCueReach.IsLandmark(kind);

    private static bool IsLoot(NaturalCueKind kind) => !IsLandmark(kind) && kind != NaturalCueKind.Chest;

    /// <summary>Fills voices from candidates (sorted in place, nearest first) for one audible radius.</summary>
    internal static void Select(List<LoopCandidate> candidates, float range, float volume, List<LoopVoice> voices)
    {
        voices.Clear();
        if (!(range > 0) || !(volume > 0)) return;
        candidates.Sort((a, b) => a.Distance != b.Distance ? a.Distance.CompareTo(b.Distance) : a.Id.CompareTo(b.Id));
        float total = 0;
        Emit(candidates, c => IsLandmark(c.Kind), MaxVoices, range, voices, ref total);
        Emit(candidates, c => c.Kind == NaturalCueKind.Chest, MaxChestVoices, range, voices, ref total);
        // The nearest of each loot kind first, so a pile of gold or gear cannot hide a rune, glyph or shard.
        Kinds.Clear();
        Emit(candidates, c => IsLoot(c.Kind) && Kinds.Add(c.Kind), MaxVoices, range, voices, ref total);
        Emit(candidates, c => IsLoot(c.Kind) && !voices.Exists(v => v.Id == c.Id), MaxVoices, range, voices, ref total);
        float scale = Math.Min(1, volume) * (total > MixGainLimit ? MixGainLimit / total : 1);
        for (int index = 0; index < voices.Count; index++)
            voices[index] = voices[index] with { Left = voices[index].Left * scale, Right = voices[index].Right * scale };
    }

    /// <summary>D2RAccess distance law: gain falls with the square of the remaining proximity.</summary>
    internal static float Gain(float distance, float range)
    {
        if (!float.IsFinite(distance) || !(range > 0)) return 0;
        float proximity = Math.Clamp(1 - distance / range, 0, 1);
        return NearGain * proximity * proximity;
    }

    /// <summary>Landmarks fade linearly to WorldCueReach.FarGain at the range edge; loot on screen never drops below ScreenEdgeShare.</summary>
    internal static float Gain(LoopCandidate candidate, float range)
    {
        if (!float.IsFinite(candidate.Distance) || !(range > 0) || candidate.Distance > range) return 0;
        if (IsLandmark(candidate.Kind))
            return NearGain * (WorldCueReach.FarGain + (1 - WorldCueReach.FarGain) * (1 - candidate.Distance / range));
        float gain = Gain(candidate.Distance, range);
        return candidate.OnScreen ? Math.Max(gain, NearGain * ScreenEdgeShare) : gain;
    }

    internal static float Pitch(float front) =>
        float.IsFinite(front) ? 1 + (RearPitch - 1) * Math.Clamp(-front / RearBlendMetres, 0, 1) : 1;

    private static void Emit(List<LoopCandidate> candidates, Func<LoopCandidate, bool> share, int limit,
        float range, List<LoopVoice> voices, ref float total)
    {
        int emitted = 0;
        foreach (var candidate in candidates)
        {
            if (voices.Count >= MaxVoices || emitted >= limit) return;
            if (!share(candidate)) continue;
            float gain = Gain(candidate, range);
            if (gain <= 0) continue;
            // Equal-power stereo, as the existing cue placement.
            float right = float.IsFinite(candidate.Right) ? Math.Clamp(candidate.Right, -1, 1) : 0;
            voices.Add(new LoopVoice(candidate.Id, candidate.Kind, gain * MathF.Sqrt((1 - right) / 2),
                gain * MathF.Sqrt((1 + right) / 2), Pitch(candidate.Front)));
            total += gain;
            emitted++;
        }
    }
}
