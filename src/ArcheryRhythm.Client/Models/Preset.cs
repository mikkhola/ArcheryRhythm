using ArcheryRhythm.Core;

namespace ArcheryRhythm.Client.Models;

public sealed record Preset
{
    public required string Name { get; init; }
    public required SequenceSettings Settings { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}

