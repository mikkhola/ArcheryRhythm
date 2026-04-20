using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ArcheryRhythm.Client.Services;
using ArcheryRhythm.Client.Pages;

namespace ArcheryRhythm.Tests;

public sealed class ComponentRenderingTests
{
    [Fact]
    public async Task Training_RendersStartButton()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        ctx.Services.AddScoped<LocalStorage>();
        ctx.Services.AddScoped<SettingsStore>();
        ctx.Services.AddScoped<SpeechService>();
        ctx.Services.AddScoped<AudioCueService>();
        ctx.Services.AddScoped<SpeechTextProvider>();
        ctx.Services.AddScoped<RunEngine>();

        var cut = ctx.Render<Training>();
        cut.WaitForState(() => cut.Markup.Contains("Start", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Settings_RendersSaveButton()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        ctx.Services.AddScoped<LocalStorage>();
        ctx.Services.AddScoped<SettingsStore>();
        ctx.Services.AddScoped<SpeechService>();
        ctx.Services.AddScoped<AudioCueService>();
        ctx.Services.AddScoped<SpeechTextProvider>();

        var cut = ctx.Render<Settings>();
        cut.WaitForState(() => cut.Markup.Contains("Save", StringComparison.OrdinalIgnoreCase));
    }
}
