# Voice-over pipeline (tools/voice)

Turns the game's in-code narrative text into shipped voice clips — with
zero hand-recorded audio and zero third-party voice licenses.

```
C# text (Story.cs / LevelLibrary / packs)
        │  GemRush ▸ Voice ▸ Export Voice Lines        (in-editor)
        ▼
Assets/Resources/Voice/manifest.json          id + cast + text + hash
        │  python tools/voice/generate.py             (offline, this folder)
        ▼
Assets/Resources/Voice/<cast>/<id>.ogg        committed, regenerated at will
```

At runtime `VoiceOver.cs` loads the manifest, looks the line up by ID,
verifies the on-screen text still hashes to the value the clip was
generated from, and plays it while ducking the score. A clip whose text
was edited after generation is silently skipped — voice can never
contradict the words on screen.

## One-time setup

1. **Python 3.10–3.12** for the generation venv (Kokoro's pinned numpy
   has no wheels for 3.13 yet). On this machine:
   `winget install -e --id Python.Python.3.12`, then
   `py -3.12 -m venv tools/voice/.venv`
2. In that venv: `.venv/Scripts/pip install kokoro soundfile`
   (Kokoro-82M weights download automatically on first run, ~300 MB).
3. **ffmpeg** on PATH, or parked locally (git-ignored):
   download an essentials build from gyan.dev and unzip into
   `tools/voice/bin/` — `generate.py` picks up whatever is on PATH.

> On Python 3.13 the pinned `numpy==1.26.4` in kokoro has no wheel and
> the spacy/blis G2P chain tries to compile — use a 3.12 venv, it is
> the path of least resistance until kokoro re-pins.

**The espeak route (automatic):** misaki's high-quality English G2P
needs spacy; on machines where spacy is unavailable or blocked by
policy, `generate.py` detects it, drops a tiny `spacy` shim into the
venv, and routes English phonemization through espeak-ng (bundled with
kokoro's `misaki` dependency). Prosody is slightly flatter on this
route; on normal machines the lexicon G2P is used and nothing changes.
Regenerating later on a healthy machine upgrades the same clips in
place — the manifest and hash contract do not move.

## Regenerating without the Unity editor

`ManifestBootstrap.cs` reproduces the exporter's collection from the
pure-C# level data and runs under Mono (Unity's bundled Roslyn + stubs):

```
mono csc.exe -out:bootstrap.exe -recurse:tools/stubs/*.cs \
     Assets/Scripts/*.cs tools/voice/ManifestBootstrap.cs
mono bootstrap.exe
```

The in-editor exporter stays canonical — it preserves `file`/`duration`
from the previous manifest, as does this bootstrap.

## Regenerating voice

```
# In Unity (or a batched -executeMethod), refresh the line list:
GemRush ▸ Voice ▸ Export Voice Lines

python tools/voice/generate.py            # everything missing or stale
python tools/voice/generate.py --check    # what would happen, no audio
python tools/voice/generate.py --only epilogue
python tools/voice/generate.py --force    # re-do even up-to-date clips
```

Generated OGGs live under `Assets/Resources/Voice/` and **are committed**
— the repo always contains a build that ships with narration, and any
machine can reproduce every byte with the two steps above. Build temps
stay in `tools/voice/build/` (git-ignored).

## Changing the words

Edit the text in C# as usual, then export + generate. Only lines whose
hash changed are re-synthesized. If you edit text and skip generation,
the line simply plays as text-only (no stale audio, no crash).

## Changing the voices

All casting lives in `cast.py`: the narrator is Kokoro's `af_heart` at
0.95 speed; a `gloomfang` slot is pre-tuned for phase 2. Swapping the
whole engine (e.g. to Qwen3-TTS on a GPU) only means reimplementing
`Pipeline.synthesize` — the manifest, hash guard and import pipeline
stay identical.

## License notes

- Kokoro-82M: Apache-2.0. Voice presets are the model's own; no
  performer royalties. Credit in the README is courtesy, not obligation.
- espeak-ng (optional generation-time fallback): GPL — used only as a
  dev tool, never distributed with the game.
- Generated audio carries no third-party restrictions; the game's
  copyright covers it like the rest of its code-generated assets.
