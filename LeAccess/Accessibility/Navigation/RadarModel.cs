using System;

namespace LeAccess.Accessibility.Navigation;

public enum RadarDirection { Up, Left, Right, Down }
public enum RadarState { Unknown, Open, Blocked }
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
    public const float DefaultOpenDepthInBodyWidths = SourceOpenDepth / SourceBodyWidth;

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
        float reach = Math.Max(open + bodyWidth, bodyWidth * SourceReachDepth / SourceBodyWidth);
        return new RadarSettings
        {
            OpenDepth = open,
            CloseDepth = Math.Max(bodyWidth, open - bodyWidth),
            ReachDepth = reach,
            ConfirmDistance = bodyWidth / SourceBodyWidth,
            BoundaryRange = reach,
            TeleportDistance = reach
        };
    }
}
