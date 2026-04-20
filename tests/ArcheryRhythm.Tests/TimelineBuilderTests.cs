using ArcheryRhythm.Core;

namespace ArcheryRhythm.Tests;

public sealed class TimelineBuilderTests
{
    [Fact]
    public void Build_DefaultSettings_ProducesExpectedTotalDuration()
    {
        var settings = new SequenceSettings
        {
            PrepareSeconds = 5,
            DrawSeconds = 3,
            AimSeconds = 3,
            ArrowsPerEnd = 3,
        };

        var timeline = TimelineBuilder.Build(settings);
        var end = Assert.Single(timeline, e => e.Type == TimelineEventType.End);

        Assert.Equal((5 + 3 + 3) * 3, end.OffsetSeconds, 6);
    }

    [Theory]
    [InlineData(3, new[] { 3, 2, 1 })]
    [InlineData(2, new[] { 2, 1 })]
    [InlineData(1, new[] { 1 })]
    [InlineData(0, new int[] { })]
    public void Build_AimCountdown_UsesExpectedValues(int aimSeconds, int[] expectedCountdown)
    {
        var settings = new SequenceSettings
        {
            PrepareSeconds = 0,
            DrawSeconds = 0,
            AimSeconds = aimSeconds,
            ArrowsPerEnd = 1,
        };

        var timeline = TimelineBuilder.Build(settings);
        var countdownValues = timeline
            .Where(e => e.Type == TimelineEventType.Countdown)
            .Select(e => e.CountdownValue)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToArray();

        Assert.Equal(expectedCountdown, countdownValues);
    }

    [Fact]
    public void Build_MultipleArrows_StagesRepeatInOrder()
    {
        var settings = new SequenceSettings
        {
            PrepareSeconds = 2,
            DrawSeconds = 1,
            AimSeconds = 2,
            ArrowsPerEnd = 2,
        };

        var timeline = TimelineBuilder.Build(settings);
        var stageStarts = timeline.Where(e => e.Type == TimelineEventType.StageStart).ToArray();

        Assert.Equal(6, stageStarts.Length);

        Assert.Equal(StageType.Prepare, stageStarts[0].Stage);
        Assert.Equal(1, stageStarts[0].ArrowIndex);
        Assert.Equal(0, stageStarts[0].OffsetSeconds, 6);

        Assert.Equal(StageType.Draw, stageStarts[1].Stage);
        Assert.Equal(1, stageStarts[1].ArrowIndex);
        Assert.Equal(2, stageStarts[1].OffsetSeconds, 6);

        Assert.Equal(StageType.Aim, stageStarts[2].Stage);
        Assert.Equal(1, stageStarts[2].ArrowIndex);
        Assert.Equal(3, stageStarts[2].OffsetSeconds, 6);

        Assert.Equal(StageType.Prepare, stageStarts[3].Stage);
        Assert.Equal(2, stageStarts[3].ArrowIndex);
        Assert.Equal(5, stageStarts[3].OffsetSeconds, 6);
    }
}
