using UnityEngine;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal enum GuidanceTargetKind { Quest, Exit, EncounterReturn }

internal readonly record struct GuidanceTarget(int Id, string Name, Vector3 Position, float ArrivalRadius, GuidanceTargetKind Kind)
{
    internal int Priority { get; init; }
}
