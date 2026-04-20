using Microsoft.JSInterop;

namespace ArcheryRhythm.Client.Services;

public sealed record SpeechVoice(string VoiceUri, string Name, string Lang, bool Default);

public sealed class SpeechService
{
    private readonly IJSRuntime _js;

    public SpeechService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<bool> IsSupportedAsync()
        => _js.InvokeAsync<bool>("archeryRhythm.speech.isSupported");

    public async Task<IReadOnlyList<SpeechVoice>> GetVoicesAsync()
    {
        var voices = await _js.InvokeAsync<SpeechVoice[]>("archeryRhythm.speech.getVoices");
        return voices ?? Array.Empty<SpeechVoice>();
    }

    public ValueTask CancelAsync()
        => _js.InvokeVoidAsync("archeryRhythm.speech.cancel");

    public ValueTask SpeakAsync(string text, string? voiceUri, double volume, string? lang)
        => _js.InvokeVoidAsync("archeryRhythm.speech.speak", text, voiceUri, volume, lang);
}
