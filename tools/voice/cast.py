"""Voice cast configuration for Gem Rush 3D.

One entry per cast member. Every generated clip's `cast` field in
Assets/Resources/Voice/manifest.json selects an entry here.

Voice names are Kokoro-82M presets (Apache-2.0 model; the preset voices
are the model's own — no third-party performance to license).

Fields:
    voice           Kokoro voice id (see hexgrad/Kokoro-82M VOICES.md)
    lang_code       Kokoro language pipeline ('a' = American English)
    speed           Speech rate multiplier (0.9-1.1 sane; lower = calmer)
    pitch           Playback pitch factor (1.0 = untouched). Applied in
                    ffmpeg; != 1.0 also needs a small atempo correction so
                    pacing stays natural. Only use for character color.
    eq              Optional ffmpeg filter(s) appended before loudnorm
                    (e.g. warmth shelf for a giant storm cloud).
"""

CAST = {
    # The storyteller: warm, clear, unhurried. Reads missions, story
    # beats, win lines, milestones, the epilogue and the menu quotes.
    "narrator": {
        "voice": "af_heart",
        "lang_code": "a",
        "speed": 0.95,
        "pitch": 1.0,
        "eq": "",
    },
    # Future cast slot (phase 2): Gloomfang's menu quotes in his own
    # voice — a kinder, deeper storm.
    "gloomfang": {
        "voice": "am_michael",
        "lang_code": "a",
        "speed": 0.92,
        "pitch": 0.89,
        "eq": "bass=g=3:f=150",
    },
}

# Spoken-form fixes applied before synthesis, after the general text
# normalization. Keys are exact substrings to replace. Use for invented
# names espeak/misaki reads oddly and for abbreviations that should be
# spoken as words.
LEXICON = {
    "B-Sides": "B sides",
}
