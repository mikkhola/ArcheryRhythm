namespace ArcheryRhythm.Core;

public sealed record SequenceSettings
{
    public int PrepareSeconds { get; init; } = 10;
    public int DrawSeconds { get; init; } = 5;
    public int AimSeconds { get; init; } = 4;
    public int ArrowsPerEnd { get; init; } = 3;

    public string Culture { get; init; } = "fi-FI";
    public bool SpeechEnabled { get; init; } = true;
    public string? SelectedVoiceUri { get; init; }
    public double SpeechVolume { get; init; } = 1.0;

    public SequenceSettings Normalize()
    {
        return this with
        {
            PrepareSeconds = Math.Max(0, PrepareSeconds),
            DrawSeconds = Math.Max(0, DrawSeconds),
            AimSeconds = Math.Max(0, AimSeconds),
            ArrowsPerEnd = Math.Clamp(ArrowsPerEnd, 1, 12),
            Culture = string.IsNullOrWhiteSpace(Culture) ? "fi-FI" : Culture,
            SpeechVolume = Math.Clamp(SpeechVolume, 0.0, 1.0),
        };
    }
}

