# ArcheryRhythm
An application for learning the correct shooting rhythm in archery.

## Local dev
- `dotnet test`
- `dotnet run --project .\\src\\ArcheryRhythm.Client\\ArcheryRhythm.Client.csproj`

The app is a Blazor WebAssembly PWA with spoken guidance (Finnish by default).

## Generate Finnish audio cues (optional)
If browser Finnish TTS voices sound bad, you can generate a small offline audio pack (mp3) for the common cues:
- Install Python + `edge-tts`
- Run `python scripts\\generate_finnish_audio_assets.py --overwrite`
- Output goes to `src\\ArcheryRhythm.Client\\wwwroot\\audio\\fi\\`

When the app language is `fi-FI`, it plays these audio cues (numbers + commands) instead of browser TTS.

## Azure (cheap hosting)
Deploy as a static site with **Azure Static Web Apps**.
- Create an Azure Static Web Apps resource and connect this repo.
- Use `.github/workflows/azure-static-web-apps.yml` as the GitHub Actions workflow template.
