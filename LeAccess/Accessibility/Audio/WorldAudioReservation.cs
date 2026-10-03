using System;

namespace LeAccess.Accessibility.Audio;

internal enum WorldAudioSource { Terrain, Npc, Object, Guidance, Enemy }

// Main-thread arbitration includes queued device audio, not just provider consumption.
internal static class WorldAudioReservation
{
    private static readonly long[] Waiting = new long[Enum.GetValues<WorldAudioSource>().Length];
    private static long _sequence;
    private static double _until;
    private static double _lastTime;

    internal static bool TryBegin(WorldAudioSource source, double now, double duration, double bufferMilliseconds)
    {
        if (!double.IsFinite(now) || !double.IsFinite(duration) || duration <= 0 ||
            !double.IsFinite(bufferMilliseconds) || bufferMilliseconds < 0) return false;
        if (now < _lastTime) Reset();
        _lastTime = now;
        int index = (int)source;
        if (Waiting[index] == 0) Waiting[index] = ++_sequence;
        if (now < _until) return false;
        foreach (long order in Waiting)
            if (order != 0 && order < Waiting[index]) return false;
        Waiting[index] = 0;
        _until = now + duration + bufferMilliseconds / 1000;
        return true;
    }

    internal static void Cancel(WorldAudioSource source) => Waiting[(int)source] = 0;

    internal static void Reset()
    {
        Array.Clear(Waiting, 0, Waiting.Length);
        _sequence = 0;
        _until = _lastTime = 0;
    }
}
