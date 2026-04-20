using System.ComponentModel.DataAnnotations;

namespace ArcheryRhythm.Client.Models;

public sealed class SequenceSettingsForm
{
    [Range(0, 60)]
    public int PrepareSeconds { get; set; } = 5;

    [Range(0, 60)]
    public int DrawSeconds { get; set; } = 3;

    [Range(0, 60)]
    public int AimSeconds { get; set; } = 3;

    [Range(1, 12)]
    public int ArrowsPerEnd { get; set; } = 3;

    [Required]
    public string Culture { get; set; } = "fi-FI";

    public bool SpeechEnabled { get; set; } = true;
    public string? SelectedVoiceUri { get; set; }

    [Range(0.0, 1.0)]
    public double SpeechVolume { get; set; } = 1.0;
}

