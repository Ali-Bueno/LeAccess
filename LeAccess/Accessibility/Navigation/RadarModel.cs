using System;

namespace LeAccess.Accessibility.Navigation;

public enum RadarDirection { Up, Left, Right, Down }
public enum RadarState { Unknown, Open, Blocked }
// Clear or Blocked by the player's NavMesh; OffMesh when the target is not near any walkable surface.
public enum TerrainSight { Unknown, Clear, Blocked, OffMesh }
public enum RadarResetReason { None, Explicit, Initial, SessionChanged, PollGap, TimeReversed, Teleport, CameraTurn, InvalidInput }

public readonly struct RadarReading
{
    public float Depth { get; }
    public bool Available { get; }
    /// <summary>False when the space a short opening leads to closes around its far end: a niche, not a passage.</summary>
    public bool Continues { get; }

    public RadarReading(float depth, bool available, bool continues = true)
    {
        Depth = depth;
        Available = available;
        Continues = continues;
    }
}

public sealed class RadarSettings
{
    // D2 radar timings are navigation tuning, independent of collision units.
    public const double PollSeconds = 0.05;
    public const double MaxGapSeconds = 0.25;
    public const double StoppedSeconds = 0.2;
    public const double StationaryConfirmSeconds = 0.1;
    public const double OpeningIntervalSeconds = 0.5;

    private const float SourceBodyWidth = 3f;
    private const float SourceOpenDepth = 10f;
    private const float SourceReachDepth = 20f;
    public const float DefaultOpenDepthInBodyWidths = SourceOpenDepth / SourceBodyWidth;
    // The probe looks twice D2's reach, so a far wall still counts as the space going on.
    private const float ProbeDepthInPassageDepths = 2f;

    /// <summary>Shortest walkable depth that can be an opening, when the space goes on beyond it.</summary>
    public float OpenDepth { get; init; }
    public float CloseDepth { get; init; }
    /// <summary>A depth that is an opening by itself, without the continuation check.</summary>
    public float PassageDepth { get; init; }
    public float ReachDepth { get; init; }
    /// <summary>A depth step within one poll that means the probe slipped past a wall's edge.</summary>
    public float EdgeJump { get; init; }
    public float ConfirmDistance { get; init; }
    public float BoundaryRange { get; init; }
    public float TeleportDistance { get; init; }

    public static RadarSettings FromBodyWidth(float bodyWidth,
        float openDepthInBodyWidths = DefaultOpenDepthInBodyWidths)
    {
        if (!float.IsFinite(bodyWidth) || bodyWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(bodyWidth));
        if (!float.IsFinite(openDepthInBodyWidths) || openDepthInBodyWidths <= 1)
            throw new ArgumentOutOfRangeException(nameof(openDepthInBodyWidths));

        // Ratios port D2's three-subtile human footprint; they do not infer LE world units.
        float open = bodyWidth * openDepthInBodyWidths;
        float neighbourhood = bodyWidth * SourceReachDepth / SourceBodyWidth;
        float passage = Math.Max(open, neighbourhood);
        return new RadarSettings
        {
            OpenDepth = open,
            CloseDepth = Math.Max(bodyWidth, open - bodyWidth),
            PassageDepth = passage,
            ReachDepth = passage * ProbeDepthInPassageDepths,
            // One body width: walking or sliding along a wall moves the probe tip far less in one 50 ms poll.
            EdgeJump = bodyWidth,
            ConfirmDistance = bodyWidth / SourceBodyWidth,
            BoundaryRange = neighbourhood,
            TeleportDistance = neighbourhood
        };
    }
}
