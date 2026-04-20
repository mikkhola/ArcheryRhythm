using System.Text.Json;
using ArcheryRhythm.Client.Models;
using ArcheryRhythm.Core;

namespace ArcheryRhythm.Client.Services;

public sealed class SettingsStore
{
    private const string SettingsKey = "archeryrhythm.settings.v1";
    private const string PresetsKey = "archeryrhythm.presets.v1";
    private const string CultureKey = "archeryrhythm.culture";

    private readonly LocalStorage _storage;

    public SettingsStore(LocalStorage storage)
    {
        _storage = storage;
    }

    public async Task<SequenceSettings> LoadSettingsAsync()
    {
        var settings = await _storage.GetJsonAsync<SequenceSettings>(SettingsKey, JsonDefaults.Options);
        return (settings ?? new SequenceSettings()).Normalize();
    }

    public Task SaveSettingsAsync(SequenceSettings settings)
        => _storage.SetJsonAsync(SettingsKey, settings.Normalize(), JsonDefaults.Options).AsTask();

    public async Task<IReadOnlyList<Preset>> LoadPresetsAsync()
    {
        var presets = await _storage.GetJsonAsync<List<Preset>>(PresetsKey, JsonDefaults.Options);
        return (presets ?? new List<Preset>()).OrderByDescending(p => p.CreatedUtc).ToArray();
    }

    public Task SavePresetsAsync(IEnumerable<Preset> presets)
        => _storage.SetJsonAsync(PresetsKey, presets.ToList(), JsonDefaults.Options).AsTask();

    public Task SetCultureAsync(string culture)
        => _storage.SetStringAsync(CultureKey, culture).AsTask();

    public ValueTask<string?> GetCultureAsync()
        => _storage.GetStringAsync(CultureKey);

    public static SequenceSettingsForm ToForm(SequenceSettings settings)
    {
        var normalized = settings.Normalize();
        return new SequenceSettingsForm
        {
            PrepareSeconds = normalized.PrepareSeconds,
            DrawSeconds = normalized.DrawSeconds,
            AimSeconds = normalized.AimSeconds,
            ArrowsPerEnd = normalized.ArrowsPerEnd,
            Culture = normalized.Culture,
            SpeechEnabled = normalized.SpeechEnabled,
            SelectedVoiceUri = normalized.SelectedVoiceUri,
            SpeechVolume = normalized.SpeechVolume,
        };
    }

    public static SequenceSettings FromForm(SequenceSettingsForm form)
    {
        return new SequenceSettings
        {
            PrepareSeconds = form.PrepareSeconds,
            DrawSeconds = form.DrawSeconds,
            AimSeconds = form.AimSeconds,
            ArrowsPerEnd = form.ArrowsPerEnd,
            Culture = form.Culture,
            SpeechEnabled = form.SpeechEnabled,
            SelectedVoiceUri = form.SelectedVoiceUri,
            SpeechVolume = form.SpeechVolume,
        }.Normalize();
    }

    public static string ToJson(SequenceSettings settings)
        => JsonSerializer.Serialize(settings.Normalize(), JsonDefaults.Options);
}

