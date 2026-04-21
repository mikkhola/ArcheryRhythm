import argparse
import asyncio
import json
from pathlib import Path

import edge_tts


DEFAULT_VOICE = "fi-FI-NooraNeural"


def build_cues() -> dict[str, str]:
    # Keep this vocabulary small so it remains easy to cache and ship with the app.
    return {
        "stage_prepare": "Valmistaudu",
        "stage_draw": "Nosto, veto ja ankkuri",
        "count_3": "kolme",
        "count_2": "kaksi",
        "count_1": "yksi",
        "cue_shoot": "ammu",
    }


async def synthesize_to_mp3(text: str, voice: str, out_path: Path) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    communicate = edge_tts.Communicate(text=text, voice=voice)
    await communicate.save(str(out_path))


async def main_async() -> int:
    parser = argparse.ArgumentParser(description="Generate Finnish audio cue assets (mp3) for ArcheryRhythm.")
    parser.add_argument(
        "--out-dir",
        default=str(Path("src") / "ArcheryRhythm.Client" / "wwwroot" / "audio" / "fi"),
        help="Output directory for generated mp3 files.",
    )
    parser.add_argument("--voice", default=DEFAULT_VOICE, help="Edge TTS voice name.")
    parser.add_argument(
        "--rate",
        default="+0%",
        help="Default speaking rate (e.g. '+0%%', '-15%%', '+10%%').",
    )
    parser.add_argument(
        "--rate-stage-draw",
        default="-20%",
        help="Speaking rate override for stage_draw (default: slower).",
    )
    parser.add_argument(
        "--pause-ms-stage-draw",
        type=int,
        default=400,
        help="Approximate pause (ms) between words in stage_draw using punctuation. Use 0 to disable.",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Overwrite existing files.",
    )
    args = parser.parse_args()

    out_dir = Path(args.out_dir)
    cues = build_cues()

    manifest: dict[str, str] = {}
    for key, text in cues.items():
        filename = f"{key}.mp3"
        out_file = out_dir / filename
        manifest[key] = filename

        if out_file.exists() and not args.overwrite:
            continue

        rate = args.rate_stage_draw if key == "stage_draw" else args.rate

        if key == "stage_draw" and args.pause_ms_stage_draw and args.pause_ms_stage_draw > 0:
            pause_ms = int(args.pause_ms_stage_draw)
            # edge-tts escapes input into SSML internally, so we can't inject <break>.
            # Instead, approximate pauses with punctuation that most voices render as short silences.
            dot_count = max(3, int(round(pause_ms / 100)))
            pause = "." * dot_count
            text = f"Nosto{pause} veto{pause} ja ankkuri"

        communicate = edge_tts.Communicate(text=text, voice=args.voice, rate=rate)

        await communicate.save(str(out_file))

    (out_dir / "cues.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0


def main() -> int:
    return asyncio.run(main_async())


if __name__ == "__main__":
    raise SystemExit(main())
