using ArcheryRhythm.Core;

namespace ArcheryRhythm.Client.Services;

// v1 pragmatic fallback: speak in English so browser voices pronounce reliably.
public sealed class SpeechTextProvider
{
    public const string SpeechLang = "en-US";

    public string Stage(StageType stage) => stage switch
    {
        StageType.Prepare => "Prepare",
        StageType.Draw => "Draw to anchor",
        StageType.Aim => "Aim and expand",
        _ => stage.ToString(),
    };

    public string Countdown(int value) => value switch
    {
        3 => "three",
        2 => "two",
        1 => "one",
        _ => value.ToString(),
    };

    public string Shoot() => "shoot";

    public string TestPhrase() => "Testing speech.";
}
