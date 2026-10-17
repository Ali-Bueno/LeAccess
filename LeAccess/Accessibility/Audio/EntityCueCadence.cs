namespace LeAccess.Accessibility.Audio;

// D2RAccess entity_cues.h Settings and entity_cue_service.cpp PollMs, shared by NPC and enemy pulses.
internal static class EntityCueCadence
{
    internal const double PollSeconds = 0.1;
    internal const double NearIntervalSeconds = 0.15;
    internal const double FarIntervalSeconds = 0.45;
    internal const double GapSeconds = 0.1;

    // Shrinks linearly from the range edge to contact; speed divides it. Callers validate distance and range.
    internal static double Interval(float distance, float range, float speed = 1) =>
        (NearIntervalSeconds + (FarIntervalSeconds - NearIntervalSeconds) * distance / range) / speed;
}
