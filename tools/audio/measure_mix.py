#!/usr/bin/env python3
"""Gem Rush 3D — mix measurement tool.

Faithfully replicates the runtime synthesis (SfxSynth/MusicSynth) in
Python and reports the game's real loudness map: peak, gated RMS, ITU-R
BS.1770 LUFS, and spectral centroid per clip, at the levels each clip is
actually played (source volumes and PlayOneShot scales included).

This is the audio owner's instrument for the "is the mix pleasant"
question: judgments are made against this table, not against guesses.

Usage:
    python tools/audio/measure_mix.py                # full map
    python tools/audio/measure_mix.py --group music  # one group
    python tools/audio/measure_mix.py --csv out.csv  # machine-readable
"""

import argparse
import math
import sys
from pathlib import Path

import numpy as np

try:
    import pyloudnorm as pyln
    METER = pyln.Meter(44100)
except ImportError:
    METER = None

SR = 44100
TAU = 2.0 * math.pi


# ----------------------------------------------------------------------
# Exact replicas of the C# DSP primitives (SfxSynth.cs)
# ----------------------------------------------------------------------

def clamp01(x):
    return 0.0 if x < 0.0 else (1.0 if x > 1.0 else x)


def voice(data, freq, start_sec, dur_sec, vol, partials, weights, decays,
          attack_sec=0.004, decay_pow=1.6, vib_hz=0.0, vib_depth=0.0,
          vib_ramp=0.5):
    start = max(0, min(int(start_sec * SR), len(data) - 1))
    n = min(int(dur_sec * SR), len(data) - start)
    if n <= 0:
        return
    phases = [0.0] * len(partials)
    inv_attack = (1.0 / attack_sec) if attack_sec > 0 else 0.0
    for i in range(n):
        t = i / n
        tt = i / SR
        env = min(tt * inv_attack, 1.0) * (1.0 - t) ** decay_pow
        vib = 1.0
        if vib_hz > 0:
            ramp = clamp01(t / max(0.01, vib_ramp))
            vib = 1.0 + math.sin(TAU * vib_hz * tt) * vib_depth * ramp
        s = 0.0
        for p in range(len(partials)):
            phases[p] += TAU * freq * partials[p] * vib / SR
            p_env = (1.0 - t) ** (decay_pow * decays[p])
            s += math.sin(phases[p]) * weights[p] * p_env
        data[start + i] += s * env * vol


def noise_voice(data, start_sec, dur_sec, vol, cutoff_from, cutoff_to,
                attack_sec, release_sec, seed, decay_pow=1.0):
    start = max(0, min(int(start_sec * SR), len(data) - 1))
    n = min(int(dur_sec * SR), len(data) - start)
    if n <= 0:
        return
    rng = np.random.default_rng(seed)
    noise = rng.uniform(-1.0, 1.0, n)
    y1 = 0.0
    y2 = 0.0
    attack_n = max(1.0, attack_sec * SR)
    release_n = max(1.0, release_sec * SR)
    for i in range(n):
        t = i / n
        cutoff = cutoff_from + (cutoff_to - cutoff_from) * t
        alpha = 1.0 - math.exp(-TAU * cutoff / SR)
        y1 += alpha * (noise[i] - y1)
        y2 += alpha * (y1 - y2)
        env = min(i / attack_n, 1.0) * min((n - i) / release_n, 1.0) * \
            (1.0 - t) ** decay_pow
        data[start + i] += y2 * 1.5 * env * vol


def finalize(data, peak_cap=0.95):
    peak = max(abs(x) for x in data) if len(data) else 0.0
    if peak > peak_cap:
        g = peak_cap / peak
        return np.array([x * g for x in data])
    return np.array(data)


# Timbres (SfxSynth.cs)
CHIME = ([1, 2.01, 3.02, 4.16], [1, 0.35, 0.18, 0.08], [1, 1.6, 2.4, 3.4])
BOX = ([1, 4.0], [1, 0.06], [1, 1.2])
BELL = ([0.5, 1, 2.0, 2.76, 5.42, 8.93],
        [0.35, 1, 0.5, 0.4, 0.15, 0.05],
        [0.6, 1, 1.4, 1.9, 3, 4.5])


def chime_note(freq, dur, vol):
    d = [0.0] * (int(dur * SR) + 1)
    voice(d, freq, 0, dur, vol, *CHIME, 0.003, 1.7)
    return finalize(d)


def box_note(freq, dur, vol):
    d = [0.0] * (int(dur * SR) + 1)
    voice(d, freq, 0, dur, vol, *BOX, 0.006, 1.8)
    return finalize(d)


# ----------------------------------------------------------------------
# Clip builders — every voice the game plays (name, group, role)
# ----------------------------------------------------------------------

def build_clips():
    C = {}

    C["jump"] = ("movement", _jump(1.0))
    C["land_mid"] = ("movement", _land())          # played 0.45-1.0 x impact
    C["gem_pickup"] = ("collect", chime_note(880.0, 0.35, 0.42))
    C["gem_combo_step6"] = ("collect", chime_note(880.0 * 2 ** (6 / 12), 0.35, 0.42))
    C["melody_note"] = ("collect", box_note(659.25, 0.35, 0.4))
    C["checkpoint"] = ("collect", _checkpoint())
    C["heart"] = ("collect", _heart())
    C["gift"] = ("collect", _gift())
    C["bounce"] = ("movement", _bounce())
    C["milestone"] = ("collect", _milestone())

    C["die_hazard"] = ("danger", _die_hazard())
    C["die_fall"] = ("danger", _die_fall())
    C["game_over"] = ("danger", _game_over())
    C["lives_low"] = ("danger", _lives_low())

    C["win_fanfare"] = ("win", _win())
    C["star_ding"] = ("win", chime_note(1046.5, 0.6, 0.42))
    C["record"] = ("win", _record())
    C["bloom_run"] = ("win", _bloom())
    C["crystal_run"] = ("win", _crystal())
    C["concert"] = ("win", _concert())

    C["bell_6s"] = ("world", _bell(6.0))
    C["bell_9s"] = ("world", _bell(9.0))
    C["bridge_on"] = ("world", _bridge_on())
    C["bridge_off"] = ("world", _bridge_off())
    C["guardian_wake"] = ("world", _guardian_wake())
    C["giggle_full"] = ("world", _giggle())
    C["gust_swell"] = ("world", _noise_swell())
    C["mirror"] = ("world", _mirror())

    C["ui_click"] = ("ui", _ui_click())
    C["ui_toggle"] = ("ui", _ui_toggle())
    C["pause"] = ("ui", _pause_blip())
    C["panel"] = ("ui", _panel())
    C["intro"] = ("ui", _intro())
    C["page"] = ("ui", _page())

    # Music at device level: 0.13 synth x 0.26 musicSource volume
    src = 0.13 * 0.26
    C["music_day"] = ("music", np.array(_pad("day")) * 0.26)
    C["music_dark"] = ("music", np.array(_pad("dark")) * 0.26)
    C["music_wind"] = ("music", np.array(_pad("wind")) * 0.26)
    C["music_bells"] = ("music", np.array(_pad("bells")) * 0.26)
    C["music_menu"] = ("music", np.array(_pad("menu")) * 0.26)
    # Music ducked for voice (0.3 duck fraction)
    C["music_day_ducked"] = ("music", np.array(_pad("day")) * 0.26 * 0.3)
    return C


def _jump(pitch):
    d = [0.0] * (int(0.16 * SR) + 1)
    noise_voice(d, 0, 0.09, 0.18, 500, 2600, 0.004, 0.05, 101, 1.2)
    voice(d, 340.0 * pitch, 0.005, 0.155, 0.42, [1, 2], [1, 0.12],
          [1, 1.5], 0.005, 1.4)
    return finalize(d)


def _land():
    d = [0.0] * (int(0.16 * SR) + 1)
    voice(d, 170.0, 0, 0.13, 0.5, [1, 0.5], [1, 0.4], [1, 0.8], 0.002, 1.8)
    noise_voice(d, 0.004, 0.11, 0.2, 1000, 380, 0.003, 0.06, 202, 1.4)
    return finalize(d)


def _checkpoint():
    d = [0.0] * (int(0.75 * SR) + 1)
    for i, n in enumerate([440.0, 659.25, 880.0]):
        voice(d, n, i * 0.1, 0.42, 0.4, *CHIME, 0.004, 1.9)
    return finalize(d)


def _heart():
    d = [0.0] * (int(0.8 * SR) + 1)
    voice(d, 130.8, 0, 0.5, 0.14, [1], [1], [1], 0.01, 1.2)
    for i, n in enumerate([523.25, 659.25, 783.99]):
        voice(d, n, i * 0.09, 0.4, 0.36, *BOX, 0.006, 1.8)
    return finalize(d)


def _gift():
    d = [0.0] * (int(1.0 * SR) + 1)
    for i, n in enumerate([783.99, 1174.66, 1567.98]):
        voice(d, n, i * 0.12, 0.6, 0.34, *BELL, 0.003, 2.0)
    return finalize(d)


def _milestone():
    d = [0.0] * (int(1.3 * SR) + 1)
    for i, n in enumerate([880.0, 1108.73, 1318.5]):
        voice(d, n, i * 0.07, 0.7, 0.24, *BOX, 0.004, 2.2)
    return finalize(d)


def _bounce():
    d = [0.0] * (int(0.24 * SR) + 1)
    noise_voice(d, 0, 0.02, 0.2, 3000, 3000, 0.001, 0.015, 303)
    n = len(d) - 1
    ph0 = ph1 = 0.0
    for i in range(n):
        t = i / n
        f = 150.0 + (880.0 - 150.0) * t ** 0.6
        wob = 1.0 + math.sin(TAU * 9.0 * t) * 0.05 * t
        ph0 += TAU * f * wob / SR
        ph1 += TAU * f * 2.0 * wob / SR
        env = (1.0 - t) ** 1.3
        d[i] = (math.sin(ph0) + 0.3 * math.sin(ph1)) * min(t * 200.0, 1.0) * env * 0.5
    return finalize(d)


def _die_hazard():
    d = [0.0] * (int(0.62 * SR) + 1)
    noise_voice(d, 0, 0.05, 0.5, 3400, 2400, 0.001, 0.04, 404)
    voice(d, 200.0, 0.02, 0.6, 0.55, [1, 0.5], [1, 0.5], [1, 0.7], 0.003, 1.3)
    return finalize(d)


def _die_fall():
    d = [0.0] * (int(0.66 * SR) + 1)
    noise_voice(d, 0, 0.5, 0.5, 2400, 220, 0.05, 0.16, 505, 1.5)
    voice(d, 130.0, 0.44, 0.2, 0.4, [1, 0.5], [1, 0.4], [1, 0.8], 0.002, 1.8)
    return finalize(d)


def _game_over():
    d = [0.0] * (int(1.5 * SR) + 1)
    voice(d, 90.0, 0, 0.35, 0.5, [1, 2], [1, 0.2], [1, 1.6], 0.002, 1.6)
    for c in [110.0, 130.8, 164.8]:
        voice(d, c * 0.998, 0.1, 1.35, 0.16, [1], [1], [1], 0.3, 1.0)
        voice(d, c * 1.003, 0.1, 1.35, 0.16, [1], [1], [1], 0.3, 1.0)
    return finalize(d)


def _lives_low():
    d = [0.0] * (int(0.8 * SR) + 1)
    voice(d, 329.63, 0, 0.4, 0.28, *BOX, 0.01, 1.6)
    voice(d, 261.63, 0.22, 0.5, 0.28, *BOX, 0.01, 1.6)
    return finalize(d)


def _win():
    d = [0.0] * (int(1.4 * SR) + 1)
    for n in [523.25, 659.25, 783.99]:
        voice(d, n, 0, 0.55, 0.22, *CHIME, 0.003, 1.6)
    for i, n in enumerate([523.25, 659.25, 783.99, 1046.5, 1318.5]):
        voice(d, n, 0.1 + i * 0.1, 0.4, 0.3, *CHIME, 0.003, 1.8)
    voice(d, 2093.0, 0.62, 0.75, 0.16, *BOX, 0.004, 2.2)
    return finalize(d)


def _record():
    d = [0.0] * (int(0.9 * SR) + 1)
    for i, n in enumerate([1318.5, 1567.98, 2093.0]):
        voice(d, n, i * 0.08, 0.55, 0.28, *CHIME, 0.003, 2.0)
    return finalize(d)


def _bloom():
    lead = 0.4
    notes = [523.25, 587.33, 659.25, 783.99, 880.0, 1046.5, 1174.66, 1318.5]
    dur = lead + len(notes) * 0.16 + 0.6
    d = [0.0] * (int(dur * SR) + 1)
    for i, n in enumerate(notes):
        voice(d, n, lead + i * 0.16, 0.5, 0.3, *BOX, 0.006, 2.0)
    noise_voice(d, lead, dur - lead - 0.1, 0.05, 2600, 3400, 0.3, 0.5, 606)
    return finalize(d)


def _crystal():
    lead = 0.45
    notes = [523.25, 659.25, 783.99, 880.0, 1046.5, 1318.5]
    dur = lead + len(notes) * 0.18 + 0.9
    d = [0.0] * (int(dur * SR) + 1)
    partials, weights, decays = [1, 2.76, 5.42], [1, 0.35, 0.12], [1, 1.8, 3]
    for i, n in enumerate(notes):
        voice(d, n, lead + i * 0.18, 0.9, 0.26, partials, weights, decays, 0.005, 2.2)
    noise_voice(d, lead, dur - lead - 0.2, 0.045, 3000, 4200, 0.4, 0.6, 607)
    return finalize(d)


def _concert():
    lead = 0.5
    melody = [523.25, 659.25, 783.99, 1046.5, 783.99, 1046.5, 1318.5]
    dur = lead + len(melody) * 0.22 + 2.2
    d = [0.0] * (int(dur * SR) + 1)
    partials, weights, decays = [0.5, 1, 2.76, 5.42], [0.35, 1, 0.3, 0.1], [0.8, 1, 1.9, 3.2]
    for i, n in enumerate(melody):
        voice(d, n, lead + i * 0.22, 1.4, 0.3, partials, weights, decays, 0.006, 1.8)
    for n in [523.25, 659.25, 783.99, 1046.5]:
        voice(d, n, lead + len(melody) * 0.22 + 0.35, 2.0, 0.22,
              partials, weights, decays, 0.02, 1.6)
    noise_voice(d, lead, dur - lead - 0.3, 0.04, 2800, 3800, 0.5, 0.8, 610)
    return finalize(d)


def _bell(seconds):
    d = [0.0] * int(seconds * SR)
    voice(d, 196.0, 0, seconds, 0.30, *BELL, 0.002, 1.7)
    noise_voice(d, 0, 0.03, 0.30 * 0.4, 5000, 3000, 0.001, 0.025, 808)
    return finalize(d)


def _bridge_on():
    d = [0.0] * (int(0.65 * SR) + 1)
    for i, n in enumerate([1046.5, 1318.5, 1567.98]):
        voice(d, n, i * 0.05, 0.3, 0.24, *CHIME, 0.003, 2.2)
    noise_voice(d, 0, 0.4, 0.06, 2800, 3600, 0.03, 0.3, 909)
    return finalize(d)


def _bridge_off():
    d = [0.0] * (int(0.55 * SR) + 1)
    for i, n in enumerate([1567.98, 1318.5, 1046.5]):
        voice(d, n, i * 0.06, 0.28, 0.14, *CHIME, 0.004, 2.2)
    return finalize(d)


def _guardian_wake():
    d = [0.0] * (int(0.38 * SR) + 1)
    n = len(d) - 1
    ph0 = ph1 = 0.0
    for i in range(n):
        t = i / n
        f = 105.0 + (196.0 - 105.0) * t
        ph0 += TAU * f / SR
        ph1 += TAU * f * 2.0 / SR
        growl = 0.08 + (0.4 - 0.08) * t
        d[i] = (math.sin(ph0) + growl * math.sin(ph1)) * min(t * 30.0, 1.0) * \
            (1.0 - t) ** 0.9 * 0.34
    noise_voice(d, 0.3, 0.04, 0.12, 2000, 2000, 0.002, 0.03, 110)
    return finalize(d)


def _giggle():
    d = [0.0] * (int(0.75 * SR) + 1)
    notes = [659.25, 783.99, 880.0, 783.99, 659.25]
    starts = [0, 0.09, 0.185, 0.29, 0.405]
    for i, n in enumerate(notes):
        last = i == len(notes) - 1
        voice(d, n, starts[i], 0.22 if last else 0.07, 0.3,
              [1, 2], [1, 0.15], [1, 1.5], 0.008, 2.2,
              11.0 if last else 0.0, 0.02, 0.3)
    return finalize(d)


def _noise_swell():
    d = [0.0] * int(2.2 * SR)
    lp1 = 0.0
    lp2 = 0.0
    rp = 0.0
    rng = np.random.default_rng(9)
    noise = rng.uniform(-1.0, 1.0, len(d))
    for i in range(len(d)):
        t = i / len(d)
        arc = math.sin(t * math.pi)
        cutoff = 250.0 + (1500.0 - 250.0) * arc
        alpha = min(1.0, TAU * cutoff / SR)
        lp1 += alpha * (noise[i] - lp1)
        lp2 += alpha * (lp1 - lp2)
        rp += TAU * 130.8 / SR
        d[i] = (lp2 * 3.0 + 0.3 * math.sin(rp) + 0.12 * math.sin(2 * rp)) * arc * 0.22
    return finalize(d)


def _mirror():
    d = [0.0] * (int(0.62 * SR) + 1)
    for n in [1975.5, 2093.0, 2349.3]:
        voice(d, n, 0, 0.18, 0.08, [1], [1], [1], 0.03, 1.2)
    noise_voice(d, 0.05, 0.34, 0.4, 800, 2200, 0.08, 0.18, 121, 0.8)
    voice(d, 100.0, 0.36, 0.22, 0.3, [1, 0.5], [1, 0.4], [1, 0.8], 0.002, 1.8)
    return finalize(d)


def _ui_click():
    d = [0.0] * (int(0.05 * SR) + 1)
    voice(d, 1250.0, 0, 0.04, 0.2, [1], [1], [1], 0.001, 2.4)
    noise_voice(d, 0, 0.012, 0.08, 4200, 4200, 0.001, 0.01, 131)
    return finalize(d)


def _ui_toggle():
    n = int(0.08 * SR)
    d = [0.0] * n
    ph = 0.0
    for i in range(n):
        t = i / n
        ph += TAU * (760.0 + (1000.0 - 760.0) * t) / SR
        d[i] = math.sin(ph) * min(t * 200.0, 1.0) * (1.0 - t) * 0.22
    return finalize(d)


def _pause_blip():
    n = int(0.1 * SR)
    d = [0.0] * n
    ph = 0.0
    for i in range(n):
        t = i / n
        ph += TAU * (520.0 + (390.0 - 520.0) * t) / SR
        d[i] = math.sin(ph) * min(t * 200.0, 1.0) * (1.0 - t) * 0.24
    return finalize(d)


def _panel():
    d = [0.0] * (int(0.17 * SR) + 1)
    noise_voice(d, 0, 0.17, 0.5, 350, 1400, 0.03, 0.08, 141)
    return finalize(d)


def _intro():
    d = [0.0] * (int(0.42 * SR) + 1)
    noise_voice(d, 0, 0.42, 0.45, 260, 950, 0.1, 0.22, 151, 0.8)
    return finalize(d)


def _page():
    d = [0.0] * (int(0.11 * SR) + 1)
    noise_voice(d, 0, 0.06, 0.32, 1900, 900, 0.004, 0.05, 161, 1.2)
    voice(d, 880.0, 0.03, 0.06, 0.1, [1], [1], [1], 0.002, 2.4)
    return finalize(d)


# ----------------------------------------------------------------------
# Music pads (MusicSynth.MoodLoop), at play level
# ----------------------------------------------------------------------

def mood_spec(mood):
    spec = {
        "chord_seconds": 2.2, "h2": 0.22, "sub": 0.10, "detune": 0.0,
        "shimmer": 0.0, "motif": None, "motif_times": None, "bell_motif": False,
    }
    chords = {
        "day": [[261.6, 329.6, 392.0], [220.0, 261.6, 329.6],
                [174.6, 220.0, 261.6], [196.0, 246.9, 293.7]],
        "dark": [[110.0, 130.8, 164.8], [87.3, 110.0, 130.8],
                 [73.4, 87.3, 110.0], [82.4, 98.0, 123.5]],
        "wind": [[130.8, 196.0, 293.7], [110.0, 164.8, 220.0, 261.6],
                 [87.3, 174.6, 196.0, 261.6], [98.0, 196.0, 246.9, 293.7]],
        "bells": [[110.0, 220.0, 261.6], [98.0, 196.0, 246.9],
                  [87.3, 174.6, 220.0], [82.4, 164.8, 196.0]],
        "menu": [[261.6, 329.6, 392.0, 493.9], [220.0, 261.6, 329.6, 392.0],
                 [174.6, 220.0, 261.6, 329.6], [196.0, 246.9, 293.7, 392.0]],
    }[mood]
    if mood == "menu":
        spec["chord_seconds"] = 3.1
        spec["motif"] = [659.25, 783.99, 523.25, 880.0]
        spec["motif_times"] = [1.55, 2.33, 5.0, 8.1]
    if mood == "dark":
        spec["h2"], spec["sub"] = 0.12, 0.30
    if mood == "wind":
        spec["h2"], spec["sub"] = 0.12, 0.18
    if mood == "bells":
        spec["h2"], spec["sub"] = 0.10, 0.20
        spec["bell_motif"] = True
        spec["motif"] = [220.0, 196.0, 174.6, 164.8]
        spec["motif_times"] = [0.0, 2.2, 4.4, 6.6]
    return chords, spec


def _pad(mood):
    chords, s = mood_spec(mood)
    per = int(s["chord_seconds"] * SR)
    d = [0.0] * (per * len(chords))
    for c, chord in enumerate(chords):
        for i in range(per):
            t = i / per
            env = (0.0 if t * 4 >= 1 else math.sin(t * 4 * math.pi / 2) ** 2) if False else \
                _smooth(min(t * 4, 1.0)) * _smooth(min((1.0 - t) * 4, 1.0))
            sample = 0.0
            for v in chord:
                a = TAU * v * i / SR
                sample += (math.sin(a) + s["h2"] * math.sin(2 * a)
                           + s["sub"] * math.sin(0.5 * a))
            d[c * per + i] = sample * env * 0.13
    if s["motif"]:
        partials = [0.5, 1, 2.76, 5.42] if s["bell_motif"] else [1, 4.0]
        weights = [0.35, 1, 0.3, 0.1] if s["bell_motif"] else [1, 0.06]
        decays = [0.7, 1, 1.6, 2.6] if s["bell_motif"] else [0.7, 1.4]
        for n, tm in zip(s["motif"], s["motif_times"]):
            voice(d, n, tm, min(0.7, s["chord_seconds"] * 0.5), 0.13 * (2.4 if s["bell_motif"] else 3.2),
                  partials, weights, decays, 0.008, 1.9)
    return finalize(d)


def _smooth(t):
    t = clamp01(t)
    return t * t * (3 - 2 * t)


# ----------------------------------------------------------------------
# Measurement
# ----------------------------------------------------------------------

def measure(x):
    x = np.asarray(x)
    peak = float(np.max(np.abs(x))) if len(x) else 0.0
    rms = float(np.sqrt(np.mean(x ** 2))) if len(x) else 0.0
    rms_db = 20 * math.log10(max(rms, 1e-9))
    if METER is not None and peak > 1e-4 and len(x) > 4410:
        try:
            lufs = METER.integrated_loudness(x.reshape(-1, 1))
            if not math.isfinite(lufs):
                lufs = float("nan")
        except Exception:
            lufs = float("nan")
    else:
        lufs = float("nan")
    # spectral centroid via one-sided FFT (magnitude-weighted)
    spec = np.abs(np.fft.rfft(x * np.hanning(len(x)))) if len(x) > 64 else None
    if spec is not None and spec.sum() > 0:
        freqs = np.fft.rfftfreq(len(x), 1 / SR)
        centroid = float((spec * freqs).sum() / spec.sum())
    else:
        centroid = float("nan")
    return peak, rms_db, lufs, centroid


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--group", default="")
    ap.add_argument("--csv", default="")
    args = ap.parse_args()

    clips = build_clips()
    rows = []
    for name, (group, data) in clips.items():
        if args.group and args.group != group:
            continue
        peak, rms_db, lufs, centroid = measure(data)
        rows.append((group, name, len(data) / SR, peak, rms_db, lufs, centroid))

    rows.sort(key=lambda r: (r[0], -(r[4] if r[4] > -99 else -999)))
    hdr = f"{'group':<9} {'clip':<22} {'sec':>5} {'peak':>6} {'RMS dB':>8} {'LUFS':>7} {'centroid':>9}"
    print(hdr)
    print("-" * len(hdr))
    for g, n, sec, peak, rms_db, lufs, cent in rows:
        print(f"{g:<9} {n:<22} {sec:5.1f} {peak:6.2f} {rms_db:8.1f} "
              f"{lufs:7.1f} {cent:9.0f}")

    if args.csv:
        out = Path(args.csv)
        out.write_text("group,clip,seconds,peak,rms_db,lufs,centroid\n" +
                       "\n".join(f"{g},{n},{sec:.2f},{peak:.4f},{rms_db:.1f},"
                                 f"{lufs:.1f},{cent:.0f}"
                                 for g, n, sec, peak, rms_db, lufs, cent in rows))
        print(f"\nCSV -> {out}")


if __name__ == "__main__":
    main()
