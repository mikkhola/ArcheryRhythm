namespace ArcheryRhythm.Core;

public static class TimelineBuilder
{
    public static IReadOnlyList<TimelineEvent> Build(SequenceSettings settings)
    {
        var normalized = settings.Normalize();

        var timeline = new List<TimelineEvent>(capacity: normalized.ArrowsPerEnd * 6);
        var arrowCount = normalized.ArrowsPerEnd;

        double offset = 0;
        for (var arrowIndex = 1; arrowIndex <= arrowCount; arrowIndex++)
        {
            AddStageStart(timeline, offset, StageType.Prepare, normalized.PrepareSeconds, arrowIndex, arrowCount);
            offset += normalized.PrepareSeconds;

            AddStageStart(timeline, offset, StageType.Draw, normalized.DrawSeconds, arrowIndex, arrowCount);
            offset += normalized.DrawSeconds;

            AddStageStart(timeline, offset, StageType.Aim, normalized.AimSeconds, arrowIndex, arrowCount);
            AddAimCountdown(timeline, offset, normalized.AimSeconds, arrowIndex, arrowCount);
            offset += normalized.AimSeconds;

            timeline.Add(new TimelineEvent
            {
                OffsetSeconds = offset,
                Type = TimelineEventType.Shoot,
                ArrowIndex = arrowIndex,
                ArrowCount = arrowCount,
                Stage = StageType.Aim,
            });
        }

        timeline.Add(new TimelineEvent
        {
            OffsetSeconds = offset,
            Type = TimelineEventType.End,
            ArrowIndex = arrowCount,
            ArrowCount = arrowCount,
        });

        return timeline;
    }

    private static void AddStageStart(
        List<TimelineEvent> timeline,
        double offsetSeconds,
        StageType stage,
        int stageDurationSeconds,
        int arrowIndex,
        int arrowCount)
    {
        timeline.Add(new TimelineEvent
        {
            OffsetSeconds = offsetSeconds,
            Type = TimelineEventType.StageStart,
            Stage = stage,
            StageDurationSeconds = stageDurationSeconds,
            ArrowIndex = arrowIndex,
            ArrowCount = arrowCount,
        });
    }

    private static void AddAimCountdown(
        List<TimelineEvent> timeline,
        double aimStageOffsetSeconds,
        int aimSeconds,
        int arrowIndex,
        int arrowCount)
    {
        var seconds = Math.Max(0, aimSeconds);

        var countdownStartValue = seconds >= 3 ? 3 : seconds;
        if (countdownStartValue <= 0)
        {
            return;
        }

        var countdownOffset = aimStageOffsetSeconds + (seconds - countdownStartValue);
        for (var value = countdownStartValue; value >= 1; value--)
        {
            timeline.Add(new TimelineEvent
            {
                OffsetSeconds = countdownOffset,
                Type = TimelineEventType.Countdown,
                CountdownValue = value,
                ArrowIndex = arrowIndex,
                ArrowCount = arrowCount,
                Stage = StageType.Aim,
            });
            countdownOffset += 1;
        }
    }
}

