using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ArcheryRhythm.Client;
using ArcheryRhythm.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<LocalStorage>();
builder.Services.AddScoped<SettingsStore>();
builder.Services.AddScoped<SpeechService>();
builder.Services.AddScoped<AudioCueService>();
builder.Services.AddScoped<SpeechTextProvider>();
builder.Services.AddScoped<RunEngine>();

await builder.Build().RunAsync();
