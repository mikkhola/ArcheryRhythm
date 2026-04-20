using Microsoft.JSInterop;

namespace ArcheryRhythm.Client.Services;

public sealed class AudioCueService
{
    private readonly IJSRuntime _js;

    public AudioCueService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<bool> IsSupportedAsync()
        => _js.InvokeAsync<bool>("archeryRhythm.audio.isSupported");

    public ValueTask<bool> PlayFinnishCueAsync(string cueKey, double volume)
        => _js.InvokeAsync<bool>("archeryRhythm.audio.playFiCue", cueKey, volume);
}

