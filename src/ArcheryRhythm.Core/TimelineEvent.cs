namespace ArcheryRhythm.Core;

public sealed record TimelineEvent
{
    public required double OffsetSeconds { get; init; }
    public required TimelineEventType Type { get; init; }

    public StageType? Stage { get; init; }
    public int? ArrowIndex { get; init; }
    public int? ArrowCount { get; init; }
    public int? CountdownValue { get; init; }

    public double? StageDurationSeconds { get; init; }
}

