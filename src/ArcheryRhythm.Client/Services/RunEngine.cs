using System.Diagnostics;
using ArcheryRhythm.Core;

namespace ArcheryRhythm.Client.Services;

public sealed record RunEngineState
{
    public bool IsRunning { get; init; }
    public bool IsPaused { get; init; }

    public int ArrowIndex { get; init; } = 1;
    public int ArrowCount { get; init; } = 1;
    public StageType Stage { get; init; } = StageType.Prepare;
    public StageType? NextStage { get; init; }
    public double? NextStageDurationSeconds { get; init; }

    public double StageElapsedSeconds { get; init; }
    public double StageDurationSeconds { get; init; } = 1;
    public double TotalElapsedSeconds { get; init; }
    public double TotalDurationSeconds { get; init; } = 1;

    public bool SpeechSupported { get; init; } = true;
    public string? NonBlockingBannerKey { get; init; }

    public double StageRemainingSeconds => Math.Max(0, StageDurationSeconds - StageElapsedSeconds);
    public double TotalRemainingSeconds => Math.Max(0, TotalDurationSeconds - TotalElapsedSeconds);
    public double StageProgress01 => StageDurationSeconds <= 0 ? 1 : Math.Clamp(StageElapsedSeconds / StageDurationSeconds, 0, 1);
}

internal sealed record StageSegment(int ArrowIndex, int ArrowCount, StageType Stage, double StartOffsetSeconds, double DurationSeconds);

public sealed class RunEngine : IAsyncDisposable
{
    private readonly SpeechService _speech;
    private readonly AudioCueService _audioCues;
    private readonly SpeechTextProvider _speechText;

    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private PeriodicTimer? _timer;
    private Stopwatch? _stopwatch;

    private SequenceSettings? _settings;
    private IReadOnlyList<TimelineEvent>? _timeline;
    private StageSegment[] _segments = Array.Empty<StageSegment>();
    private int _nextTimelineEventIndex;
    private int _segmentIndex;
    private bool _useFinnishAudioCues;

    private bool _isPaused;
    private double _pausedElapsedSeconds;

    public event Action? StateChanged;

    public RunEngineState State { get; private set; } = new();

    public RunEngine(SpeechService speech, AudioCueService audioCues, SpeechTextProvider speechText)
    {
        _speech = speech;
        _audioCues = audioCues;
        _speechText = speechText;
    }

    public async Task StartAsync(SequenceSettings settings)
    {
        await StopAsync();

        var normalized = settings.Normalize();
        var supported = await _speech.IsSupportedAsync();
        var audioSupported = await _audioCues.IsSupportedAsync();
        IReadOnlyList<TimelineEvent> timeline;

        lock (_lock)
        {
            _settings = normalized;
            _timeline = TimelineBuilder.Build(normalized);
            timeline = _timeline;
            _segments = BuildSegments(normalized);
            _nextTimelineEventIndex = 0;
            _segmentIndex = 0;
            _isPaused = false;
            _pausedElapsedSeconds = 0;
            _useFinnishAudioCues = audioSupported && normalized.Culture.StartsWith("fi", StringComparison.OrdinalIgnoreCase);

            _cts = new CancellationTokenSource();
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            _stopwatch = Stopwatch.StartNew();

            var totalDuration = _timeline.LastOrDefault(e => e.Type == TimelineEventType.End)?.OffsetSeconds ?? 0;
            var wantsFinnishAudio = normalized.Culture.StartsWith("fi", StringComparison.OrdinalIgnoreCase);
            string? bannerKey;
            if (wantsFinnishAudio)
            {
                bannerKey = audioSupported ? null : (supported ? "Audio_Unsupported" : "Speech_Unsupported");
            }
            else
            {
                bannerKey = supported ? null : "Speech_Unsupported";
            }

            State = new RunEngineState
            {
                IsRunning = true,
                IsPaused = false,
                SpeechSupported = supported,
                NonBlockingBannerKey = bannerKey,
                ArrowIndex = _segments.Length > 0 ? _segments[0].ArrowIndex : 1,
                ArrowCount = normalized.ArrowsPerEnd,
                Stage = _segments.Length > 0 ? _segments[0].Stage : StageType.Prepare,
                StageDurationSeconds = _segments.Length > 0 ? _segments[0].DurationSeconds : 1,
                NextStage = GetNextStage(_segments, 0)?.Stage,
                NextStageDurationSeconds = GetNextStage(_segments, 0)?.DurationSeconds,
                TotalDurationSeconds = totalDuration,
            };

            _loopTask = LoopAsync(_cts.Token);
        }

        NotifyStateChanged();

        // Trigger initial stage announcement (offset 0) immediately on Start to avoid waiting for the first timer tick.
        try
        {
            await TriggerDueEventsAsync(0, timeline, normalized, CancellationToken.None);
        }
        catch
        {
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (!State.IsRunning || _isPaused || _stopwatch is null)
            {
                return;
            }

            _pausedElapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Stop();
            _isPaused = true;
            State = State with { IsPaused = true };
        }

        NotifyStateChanged();
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (!State.IsRunning || !_isPaused || _stopwatch is null)
            {
                return;
            }

            _stopwatch.Start();
            _isPaused = false;
            State = State with { IsPaused = false };
        }

        NotifyStateChanged();
    }

    public Task StopAsync() => StopAsync(fromLoop: false);

    private async Task StopAsync(bool fromLoop)
    {
        CancellationTokenSource? cts;
        Task? loopTask;

        lock (_lock)
        {
            cts = _cts;
            loopTask = _loopTask;
            _cts = null;
            _loopTask = null;

            _timer?.Dispose();
            _timer = null;

            _stopwatch?.Stop();
            _stopwatch = null;

            _settings = null;
            _timeline = null;
            _segments = Array.Empty<StageSegment>();
            _nextTimelineEventIndex = 0;
            _segmentIndex = 0;
            _isPaused = false;
            _pausedElapsedSeconds = 0;

            State = new RunEngineState();
        }

        if (cts is not null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (loopTask is not null)
        {
            if (!fromLoop)
            {
                try { await loopTask; } catch (OperationCanceledException) { }
            }
        }

        try { await _speech.CancelAsync(); } catch { }

        NotifyStateChanged();
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            PeriodicTimer? timer;
            lock (_lock) { timer = _timer; }

            if (timer is null)
            {
                return;
            }

            await timer.WaitForNextTickAsync(ct);

            SequenceSettings? settings;
            IReadOnlyList<TimelineEvent>? timeline;
            StageSegment[] segments;
            Stopwatch? stopwatch;
            bool isPaused;
            double pausedElapsed;

            lock (_lock)
            {
                settings = _settings;
                timeline = _timeline;
                segments = _segments;
                stopwatch = _stopwatch;
                isPaused = _isPaused;
                pausedElapsed = _pausedElapsedSeconds;
            }

            if (settings is null || timeline is null || stopwatch is null || segments.Length == 0)
            {
                continue;
            }

            var elapsed = isPaused ? pausedElapsed : stopwatch.Elapsed.TotalSeconds;
            var totalDuration = timeline.LastOrDefault(e => e.Type == TimelineEventType.End)?.OffsetSeconds ?? 0;

            AdvanceSegments(elapsed, segments);
            await TriggerDueEventsAsync(elapsed, timeline, settings, ct);

            lock (_lock)
            {
                if (!State.IsRunning)
                {
                    continue;
                }

                var segment = segments[Math.Clamp(_segmentIndex, 0, segments.Length - 1)];
                var stageElapsed = Math.Max(0, elapsed - segment.StartOffsetSeconds);
            State = State with
            {
                ArrowIndex = segment.ArrowIndex,
                ArrowCount = segment.ArrowCount,
                Stage = segment.Stage,
                StageElapsedSeconds = stageElapsed,
                StageDurationSeconds = segment.DurationSeconds,
                TotalElapsedSeconds = elapsed,
                TotalDurationSeconds = totalDuration,
                NextStage = GetNextStage(segments, _segmentIndex)?.Stage,
                NextStageDurationSeconds = GetNextStage(segments, _segmentIndex)?.DurationSeconds,
            };
        }

            NotifyStateChanged();

            if (elapsed >= totalDuration)
            {
                await StopAsync(fromLoop: true);
                return;
            }
        }
    }

    private void AdvanceSegments(double elapsed, StageSegment[] segments)
    {
        lock (_lock)
        {
            while (_segmentIndex + 1 < segments.Length)
            {
                var next = segments[_segmentIndex + 1];
                if (elapsed < next.StartOffsetSeconds)
                {
                    break;
                }

                _segmentIndex++;
            }
        }
    }

    private async Task TriggerDueEventsAsync(double elapsed, IReadOnlyList<TimelineEvent> timeline, SequenceSettings settings, CancellationToken ct)
    {
        if (!settings.SpeechEnabled || (!_useFinnishAudioCues && !State.SpeechSupported))
        {
            lock (_lock)
            {
                while (_nextTimelineEventIndex < timeline.Count && elapsed + 0.02 >= timeline[_nextTimelineEventIndex].OffsetSeconds)
                {
                    _nextTimelineEventIndex++;
                }
            }
            return;
        }

        while (true)
        {
            TimelineEvent? next = null;
            lock (_lock)
            {
                if (_nextTimelineEventIndex < timeline.Count && elapsed + 0.02 >= timeline[_nextTimelineEventIndex].OffsetSeconds)
                {
                    next = timeline[_nextTimelineEventIndex];
                    _nextTimelineEventIndex++;
                }
            }

            if (next is null)
            {
                return;
            }

            if (_useFinnishAudioCues)
            {
                var cueKey = next.Type switch
                {
                    TimelineEventType.StageStart when next.Stage == StageType.Prepare => "stage_prepare",
                    TimelineEventType.StageStart when next.Stage == StageType.Draw => "stage_draw",
                    TimelineEventType.Countdown when next.CountdownValue == 3 => "count_3",
                    TimelineEventType.Countdown when next.CountdownValue == 2 => "count_2",
                    TimelineEventType.Countdown when next.CountdownValue == 1 => "count_1",
                    TimelineEventType.Shoot => "cue_shoot",
                    _ => null,
                };

                if (!string.IsNullOrWhiteSpace(cueKey))
                {
                    try
                    {
                        var ok = await _audioCues.PlayFinnishCueAsync(cueKey, settings.SpeechVolume);
                        if (!ok)
                        {
                            lock (_lock)
                            {
                                State = State with { NonBlockingBannerKey = "Audio_Blocked" };
                            }
                        }
                    }
                    catch
                    {
                        lock (_lock)
                        {
                            State = State with { NonBlockingBannerKey = "Audio_Blocked" };
                        }
                    }
                }

                if (ct.IsCancellationRequested)
                {
                    return;
                }

                continue;
            }

            var text = next.Type switch
            {
                TimelineEventType.StageStart when next.Stage.HasValue && next.Stage.Value is not (StageType.Aim or StageType.Rest) => _speechText.Stage(next.Stage.Value),
                TimelineEventType.Countdown when next.CountdownValue.HasValue => _speechText.Countdown(next.CountdownValue.Value),
                TimelineEventType.Shoot => _speechText.Shoot(),
                _ => null,
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            try
            {
                await _speech.SpeakAsync(text, settings.SelectedVoiceUri, settings.SpeechVolume, SpeechTextProvider.SpeechLang);
            }
            catch
            {
                lock (_lock)
                {
                    State = State with { NonBlockingBannerKey = "Speech_Error" };
                }
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private static StageSegment[] BuildSegments(SequenceSettings settings)
    {
        var s = settings.Normalize();
        var arrowCount = s.ArrowsPerEnd;
        const int restBetweenArrowsSeconds = 3;

        var segments = new List<StageSegment>(capacity: arrowCount * 4);
        double offset = 0;

        for (var arrowIndex = 1; arrowIndex <= arrowCount; arrowIndex++)
        {
            segments.Add(new StageSegment(arrowIndex, arrowCount, StageType.Prepare, offset, s.PrepareSeconds));
            offset += s.PrepareSeconds;

            segments.Add(new StageSegment(arrowIndex, arrowCount, StageType.Draw, offset, s.DrawSeconds));
            offset += s.DrawSeconds;

            segments.Add(new StageSegment(arrowIndex, arrowCount, StageType.Aim, offset, s.AimSeconds));
            offset += s.AimSeconds;

            if (arrowIndex < arrowCount)
            {
                segments.Add(new StageSegment(arrowIndex, arrowCount, StageType.Rest, offset, restBetweenArrowsSeconds));
                offset += restBetweenArrowsSeconds;
            }
        }

        return segments.ToArray();
    }

    private static StageSegment? GetNextStage(StageSegment[] segments, int segmentIndex)
    {
        var nextIndex = segmentIndex + 1;
        return nextIndex >= 0 && nextIndex < segments.Length ? segments[nextIndex] : null;
    }

    private void NotifyStateChanged()
    {
        try { StateChanged?.Invoke(); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
