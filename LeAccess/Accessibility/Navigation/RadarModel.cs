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

    public RadarReading(float depth, bool available)
    {
        Depth = depth;
        Available = available;
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
    // An opening is a passage as deep as D2's probe reach; a nearer wall is not an exit (reference/navigation-radar.md).
    public const float DefaultOpenDepthInBodyWidths = SourceReachDepth / SourceBodyWidth;
    // The probe looks as far again, so a wall's end can be told from the same wall seen farther away.
    private const float ProbeDepthInOpenDepths = 2f;

    public float OpenDepth { get; init; }
    public float CloseDepth { get; init; }
    public float ReachDepth { get; init; }
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
        // The knock keeps D2's closing distance, one body width inside its open depth, whatever the passage depth.
        float close = Math.Max(bodyWidth, Math.Min(bodyWidth * (SourceOpenDepth - SourceBodyWidth) / SourceBodyWidth,
            open - bodyWidth));
        float neighbourhood = bodyWidth * SourceReachDepth / SourceBodyWidth;
        return new RadarSettings
        {
            OpenDepth = open,
            CloseDepth = close,
            ReachDepth = open * ProbeDepthInOpenDepths,
            ConfirmDistance = bodyWidth / SourceBodyWidth,
            BoundaryRange = neighbourhood,
            TeleportDistance = neighbourhood
        };
    }
}
