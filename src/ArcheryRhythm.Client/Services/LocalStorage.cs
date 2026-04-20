using System.Text.Json;
using Microsoft.JSInterop;

namespace ArcheryRhythm.Client.Services;

public sealed class LocalStorage
{
    private readonly IJSRuntime _js;

    public LocalStorage(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<string?> GetStringAsync(string key)
        => _js.InvokeAsync<string?>("archeryRhythm.storage.get", key);

    public ValueTask SetStringAsync(string key, string value)
        => _js.InvokeVoidAsync("archeryRhythm.storage.set", key, value);

    public ValueTask RemoveAsync(string key)
        => _js.InvokeVoidAsync("archeryRhythm.storage.remove", key);

    public async ValueTask<T?> GetJsonAsync<T>(string key, JsonSerializerOptions? options = null)
    {
        var json = await GetStringAsync(key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, options ?? JsonDefaults.Options);
    }

    public ValueTask SetJsonAsync<T>(string key, T value, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(value, options ?? JsonDefaults.Options);
        return SetStringAsync(key, json);
    }
}

