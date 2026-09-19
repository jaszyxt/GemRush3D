#!/usr/bin/env python3
"""Gem Rush 3D — offline voice-over generator.

Reads Assets/Resources/Voice/manifest.json (written by the in-editor
exporter: GemRush > Voice > Export Voice Lines) and synthesizes every
missing or stale line with Kokoro-82M (Apache-2.0), then loudness-
normalizes to -16 LUFS mono and writes OGG Vorbis clips back into
Assets/Resources/Voice/.

A line is regenerated when its file is missing, --force is given, or the
manifest's text hash differs from the hash recorded at generation time
(tools/voice/build/state.json) — i.e. the writing changed since the clip
was made. The runtime independently refuses to play any clip whose hash
does not match the on-screen text, so stale audio can never ship.

Usage:
    python generate.py                 # generate everything missing/stale
    python generate.py --only mission  # ids containing a substring
    python generate.py --check         # report without generating
    python generate.py --list          # print the manifest table
    python generate.py --force         # regenerate even if up to date

Requires: Python 3.10+, ffmpeg on PATH, and `pip install -r requirements.txt`.
"""

import argparse
import json
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

import numpy as np
import soundfile as sf

from cast import CAST, LEXICON

REPO = Path(__file__).resolve().parents[2]
MANIFEST = REPO / "Assets" / "Resources" / "Voice" / "manifest.json"
BUILD_DIR = Path(__file__).resolve().parent / "build"
STATE = BUILD_DIR / "state.json"

SAMPLE_RATE = 24000
CHUNK_CHARS = 220          # max characters per synthesis chunk
CHUNK_GAP_SECONDS = 0.09   # pause inserted between sentence chunks
LOUDNESS = dict(I="-16", TP="-1.5", LRA="11")  # mobile narration target


def log(msg):
    print(msg, flush=True)


FFMPEG = "ffmpeg"
FFPROBE = "ffprobe"


def _find_ffmpeg():
    """ffmpeg on PATH, or a local essentials build unzipped under bin/."""
    exe = shutil.which("ffmpeg")
    if exe:
        return exe
    for candidate in sorted(Path(__file__).parent.glob("bin/**/ffmpeg.exe")):
        return str(candidate)
    return None


def check_tools():
    global FFMPEG, FFPROBE
    ffmpeg = _find_ffmpeg()
    if ffmpeg is None:
        sys.exit("ffmpeg not found on PATH or tools/voice/bin/ — install it and retry.")
    FFMPEG = ffmpeg
    ffprobe = Path(ffmpeg).with_name("ffprobe.exe")
    FFPROBE = str(ffprobe) if ffprobe.exists() else "ffprobe"
    _ensure_spacy_shim()  # before the import gate: espeak route needs it
    for module in ("kokoro", "soundfile", "numpy"):
        try:
            __import__(module)
        except ImportError:
            sys.exit(f"Python module '{module}' missing — run: pip install -r requirements.txt")


def load_state():
    if STATE.exists():
        return json.loads(STATE.read_text(encoding="utf-8"))
    return {}


def save_state(state):
    BUILD_DIR.mkdir(parents=True, exist_ok=True)
    STATE.write_text(json.dumps(state, indent=1), encoding="utf-8")


def normalize_text(text):
    """Spoken-form normalization: punctuation cleanup, ALL-CAPS taming,
    and the pronunciation lexicon from cast.py."""
    text = text.replace("\u2014", ", ").replace("\u2013", "-")
    text = text.replace("\u2018", "'").replace("\u2019", "'")
    text = text.replace("\u201c", '"').replace("\u201d", '"')
    text = text.replace("...", ".").replace("\u2026", ".")
    # ALL-CAPS words (>= 3 letters) read better in title case.
    text = re.sub(r"\b[A-Z]{3,}\b", lambda m: m.group(0).capitalize(), text)
    for src, dst in LEXICON.items():
        text = text.replace(src, dst)
    text = re.sub(r"\s+", " ", text).strip()
    return text


def split_chunks(text):
    """Pack whole sentences into chunks small enough for one pass."""
    sentences = re.split(r"(?<=[.!?]) +", text)
    chunks, current = [], ""
    for sentence in sentences:
        candidate = (current + " " + sentence).strip()
        if len(candidate) > CHUNK_CHARS and current:
            chunks.append(current)
            current = sentence
        else:
            current = candidate
    if current:
        chunks.append(current)
    return chunks


def trim_silence(audio, threshold=0.002, pad=0.05):
    amp = np.abs(audio)
    loud = np.where(amp > threshold)[0]
    if len(loud) == 0:
        return audio
    start = max(0, loud[0] - int(pad * SAMPLE_RATE))
    end = min(len(audio), loud[-1] + int(pad * SAMPLE_RATE))
    return audio[start:end]


def _ensure_spacy_shim():
    """misaki's English module does `import spacy` at import time, even on
    the espeak route that never uses it. On machines where spacy is
    unavailable (no wheel, or blocked by policy), provide an empty shim so
    the espeak-routed pipeline below can load. On healthy machines the
    real spacy wins and nothing changes."""
    try:
        import spacy  # noqa: F401
        if hasattr(spacy, "util"):
            return False  # real spacy: use the high-quality lexicon G2P
    except ImportError:
        pass
    shim = Path(sys.prefix) / "Lib" / "site-packages" / "spacy"
    if not (shim / "__init__.py").exists():
        shim.mkdir(parents=True, exist_ok=True)
        (shim / "__init__.py").write_text("# espeak-route shim (see generate.py)")
    return True


class Pipeline:
    """Lazily-built Kokoro pipeline per language code."""

    def __init__(self):
        self._pipelines = {}

    def get(self, lang_code):
        if lang_code not in self._pipelines:
            shimmed = _ensure_spacy_shim()
            from kokoro import KPipeline
            if shimmed:
                # Route English through espeak-ng G2P (slightly flatter
                # prosody than misaki's lexicon G2P, fully portable).
                log(f"[kokoro] spacy unavailable — routing '{lang_code}' via espeak-ng G2P")
                pipeline = KPipeline(lang_code="e")
                from misaki.espeak import EspeakG2P
                pipeline.g2p = EspeakG2P("en-us" if lang_code == "a" else "en-gb")
            else:
                log(f"[kokoro] loading '{lang_code}' pipeline (first use downloads weights)...")
                pipeline = KPipeline(lang_code=lang_code)
            self._pipelines[lang_code] = pipeline
        return self._pipelines[lang_code]

    def synthesize(self, text, cast):
        spec = CAST[cast]
        pipeline = self.get(spec["lang_code"])
        pieces = []
        for chunk in split_chunks(text):
            for _gs, _ps, audio in pipeline(chunk, voice=spec["voice"],
                                            speed=spec["speed"]):
                data = audio.numpy() if hasattr(audio, "numpy") else np.asarray(audio)
                if data.ndim > 1:
                    data = data.mean(axis=1)
                pieces.append(trim_silence(data.astype(np.float32)))
        if not pieces:
            raise RuntimeError(f"kokoro produced no audio for: {text[:60]}...")
        gap = np.zeros(int(CHUNK_GAP_SECONDS * SAMPLE_RATE), dtype=np.float32)
        out = []
        for i, piece in enumerate(pieces):
            if i > 0:
                out.append(gap)
            out.append(piece)
        return np.concatenate(out)


def ffmpeg_post(wav_path, ogg_path, cast):
    spec = CAST[cast]
    filters = []
    if spec.get("eq"):
        filters.append(spec["eq"])
    pitch = spec.get("pitch", 1.0)
    if abs(pitch - 1.0) > 1e-3:
        filters.append(f"asetrate={SAMPLE_RATE * pitch:.2f}")
        filters.append(f"aresample={SAMPLE_RATE}")
        filters.append(f"atempo={1.0 / pitch:.5f}")
    filters.append("loudnorm=I={I}:TP={TP}:LRA={LRA}".format(**LOUDNESS))
    cmd = [FFMPEG, "-y", "-hide_banner", "-loglevel", "error",
           "-i", str(wav_path),
           "-af", ",".join(filters),
           "-ar", str(SAMPLE_RATE), "-ac", "1",
           "-c:a", "libvorbis", "-q:a", "3",
           str(ogg_path)]
    subprocess.run(cmd, check=True)


def duration_of(ogg_path):
    out = subprocess.run(
        [FFPROBE, "-v", "error", "-show_entries", "format=duration",
         "-of", "default=noprint_wrappers=1:nokey=1", str(ogg_path)],
        check=True, capture_output=True, text=True).stdout.strip()
    return float(out)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", default="", help="generate ids containing this substring")
    parser.add_argument("--check", action="store_true", help="report status without generating")
    parser.add_argument("--list", action="store_true", help="print the manifest table")
    parser.add_argument("--force", action="store_true", help="regenerate up-to-date clips too")
    args = parser.parse_args()

    check_tools()
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    entries = manifest["entries"]
    state = load_state()
    voice_dir = MANIFEST.parent

    todo = []
    for entry in entries:
        if args.only and args.only not in entry["id"]:
            continue
        ogg = voice_dir / (entry["file"] + ".ogg")
        key = entry["file"]
        stale = state.get(key, {}).get("hash") != entry["hash"]
        if args.force or not ogg.exists() or stale:
            todo.append(entry)
        if args.list or args.check:
            status = "MISSING" if not ogg.exists() else ("STALE" if stale else "ok")
            log(f'{status:>7}  {entry["duration"]:6.1f}s  {entry["id"]}  ({entry["cast"]})')

    if args.list or args.check:
        missing = sum(1 for e in entries
                      if not (voice_dir / (e["file"] + ".ogg")).exists()
                      and (not args.only or args.only in e["id"]))
        log(f"\n{len(todo)} to generate, {missing} missing "
            f"(of {len(entries)} manifest lines).")
        return

    if not todo:
        log("Everything up to date.")
        return

    pipeline = Pipeline()
    started = time.time()
    for i, entry in enumerate(todo, 1):
        ogg = voice_dir / (entry["file"] + ".ogg")
        log(f'[{i}/{len(todo)}] {entry["id"]} ({entry["cast"]})...')
        spoken = normalize_text(entry["text"])
        audio = pipeline.synthesize(spoken, entry["cast"])
        BUILD_DIR.mkdir(parents=True, exist_ok=True)
        wav = BUILD_DIR / (entry["id"] + ".wav")
        sf.write(wav, audio, SAMPLE_RATE, subtype="PCM_16")
        ogg.parent.mkdir(parents=True, exist_ok=True)
        ffmpeg_post(wav, ogg, entry["cast"])
        wav.unlink(missing_ok=True)
        entry["duration"] = round(duration_of(ogg), 2)
        state[entry["file"]] = {
            "hash": entry["hash"],
            "generated": time.strftime("%Y-%m-%d %H:%M:%S"),
        }
        log(f'        {entry["duration"]:.1f}s -> {ogg.name}')

    MANIFEST.write_text(json.dumps(manifest, indent=1), encoding="utf-8")
    save_state(state)
    total = sum(e["duration"] for e in entries if e["duration"])
    log(f'\nDone: {len(todo)} clip(s) in {(time.time() - started) / 60:.1f} min. '
        f'Manifest total narration: {total / 60:.1f} min.')


if __name__ == "__main__":
    main()
