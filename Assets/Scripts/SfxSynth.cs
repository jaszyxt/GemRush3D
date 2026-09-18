using UnityEngine;

namespace GemRush
{
    /// Synthesizes every sound effect into AudioClips at runtime, so the
    /// game ships with zero audio files.
    ///
    /// The palette has two timbre families that do all the work:
    ///   Chime — pure sines with shimmering, fast-dying upper partials.
    ///           Everything positive (gems, hearts, stars, bells, UI).
    ///   Breath — low-pass filtered noise. Motion and weather (jumps,
    ///           landings, wind, teleports, page turns).
    /// Dark moments invert them: low hollow sines, dark filtered noise.
    ///
    /// Positive events are tuned near C-major pentatonic, so anything the
    /// player does lands in key with the score.
    public static class SfxSynth
    {
        const int SampleRate = 44100;

        // ------------------------------------------------------------------
        // Legacy voices (still used by the pickup combo cache and notes)
        // ------------------------------------------------------------------

        /// A sine sweep from f0 to f1 with a fast decay envelope.
        public static AudioClip Sweep(string name, float f0, float f1, float duration, float volume)
        {
            int samples = Mathf.Max(1, (int)(duration * SampleRate));
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float envelope = (1f - t) * (1f - t);
                data[i] = Mathf.Sin(phase) * envelope * volume;
            }
            return MakeClip(name, data);
        }

        /// A little sequence of notes; each note is a sine plus one soft octave harmonic.
        public static AudioClip Arp(string name, float[] notes, float noteDuration, float volume)
        {
            int perNote = Mathf.Max(1, (int)(noteDuration * SampleRate));
            float[] data = new float[perNote * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / perNote;
                    float envelope = (1f - t) * (1f - t);
                    float angle = 2f * Mathf.PI * notes[k] * i / SampleRate;
                    data[k * perNote + i] =
                        (Mathf.Sin(angle) + 0.35f * Mathf.Sin(2f * angle)) * envelope * volume;
                }
            }
            return MakeClip(name, data);
        }

        /// A single soft tone with a gentle decay — the voice of melody gems.
        public static AudioClip Note(string name, float frequency, float duration, float volume)
        {
            return MusicBoxNote(name, frequency, duration, volume);
        }

        // ------------------------------------------------------------------
        // Two primitives every sound is built from: an additive voice with
        // per-partial decay, and filtered noise with a sweeping low-pass.
        // Envelopes are click-free (linear attack, power-law decay).
        // ------------------------------------------------------------------

        /// A note made of sine partials, each dying at its own rate so the
        /// shimmer fades before the body — how real struck things behave.
        public static void Voice(float[] data, float freq, float startSec, float durSec,
            float vol, float[] partials, float[] weights, float[] decays,
            float attackSec = 0.004f, float decayPow = 1.6f,
            float vibratoHz = 0f, float vibratoDepth = 0f, float vibratoRamp = 0.5f)
        {
            int start = Mathf.Clamp((int)(startSec * SampleRate), 0, Mathf.Max(0, data.Length - 1));
            int samples = Mathf.Min((int)(durSec * SampleRate), data.Length - start);
            if (samples <= 0) return;
            float[] phase = new float[partials.Length];
            float invAttack = attackSec > 0f ? 1f / attackSec : 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;                 // 0..1 across the note
                float tt = (float)i / SampleRate;             // seconds
                float env = Mathf.Min(tt * invAttack, 1f) *
                            Mathf.Pow(1f - t, decayPow);
                float vib = 1f;
                if (vibratoHz > 0f)
                {
                    float ramp = Mathf.Clamp01(t / Mathf.Max(0.01f, vibratoRamp));
                    vib = 1f + Mathf.Sin(2f * Mathf.PI * vibratoHz * tt) *
                        vibratoDepth * ramp;
                }
                float s = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    phase[p] += 2f * Mathf.PI * freq * partials[p] * vib / SampleRate;
                    float pEnv = Mathf.Pow(1f - t, decayPow * decays[p]);
                    s += Mathf.Sin(phase[p]) * weights[p] * pEnv;
                }
                data[start + i] += s * env * vol;
            }
        }

        /// Filtered noise with a sweeping one-pole low-pass and an
        /// attack/release envelope — wind, breath, dust, paper.
        public static void NoiseVoice(float[] data, float startSec, float durSec, float vol,
            float cutoffFrom, float cutoffTo, float attackSec, float releaseSec,
            int seed, float decayPow = 1f)
        {
            int start = Mathf.Clamp((int)(startSec * SampleRate), 0, Mathf.Max(0, data.Length - 1));
            int samples = Mathf.Min((int)(durSec * SampleRate), data.Length - start);
            if (samples <= 0) return;
            System.Random rng = new System.Random(seed);
            float y = 0f;
            float attackSamples = Mathf.Max(1f, attackSec * SampleRate);
            float releaseSamples = Mathf.Max(1f, releaseSec * SampleRate);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float cutoff = Mathf.Lerp(cutoffFrom, cutoffTo, t);
                float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);
                y += alpha * ((float)rng.NextDouble() * 2f - 1f - y);

                float env = Mathf.Min(i / attackSamples, 1f) *
                            Mathf.Min((samples - i) / releaseSamples, 1f) *
                            Mathf.Pow(1f - t, decayPow);
                data[start + i] += y * env * vol;
            }
        }

        // ------------------------------------------------------------------
        // Timbres: named partial sets. A voice + a timbre + a pitch = a sound.
        // ------------------------------------------------------------------

        // Bright struck-crystal: pickups, stars, UI positives.
        static readonly float[] ChimePartials = { 1f, 2.01f, 3.02f, 4.16f };
        static readonly float[] ChimeWeights = { 1f, 0.35f, 0.18f, 0.08f };
        static readonly float[] ChimeDecays = { 1f, 1.6f, 2.4f, 3.4f };

        // Music box: pure fundamental with a faint high ghost — dreamy.
        static readonly float[] BoxPartials = { 1f, 4.0f };
        static readonly float[] BoxWeights = { 1f, 0.06f };
        static readonly float[] BoxDecays = { 1f, 1.2f };

        // Tower bell: inharmonic, hum-weighted, long body.
        static readonly float[] BellPartials = { 0.5f, 1f, 2.0f, 2.76f, 5.42f, 8.93f };
        static readonly float[] BellWeights = { 0.35f, 1f, 0.5f, 0.4f, 0.18f, 0.07f };
        static readonly float[] BellDecays = { 0.6f, 1f, 1.4f, 1.9f, 3f, 4.5f };

        /// Music-box note — the voice of melody gems and gentle moments.
        public static AudioClip MusicBoxNote(string name, float freq, float dur, float vol)
        {
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, freq, 0f, dur, vol, BoxPartials, BoxWeights, BoxDecays,
                0.006f, 1.8f);
            return MakeClip(name, data);
        }

        /// A chime note — bright, struck, shimmering.
        public static AudioClip Chime(string name, float freq, float dur, float vol)
        {
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, freq, 0f, dur, vol, ChimePartials, ChimeWeights, ChimeDecays,
                0.003f, 1.7f);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // Pip: movement
        // ------------------------------------------------------------------

        /// A jump: a breathy puff wrapped around a soft rising chirp.
        /// pitchScale bakes variants so hop chains never sound sampled.
        public static AudioClip Jump(string name, float pitchScale)
        {
            float dur = 0.16f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, 0.09f, 0.14f, 500f, 2600f, 0.004f, 0.05f, 101, 1.2f);
            Voice(data, 340f * pitchScale, 0.005f, dur - 0.005f, 0.42f,
                new float[] { 1f, 2f }, new float[] { 1f, 0.12f }, new float[] { 1f, 1.5f },
                0.005f, 1.4f);
            return MakeClip(name, data);
        }

        /// A landing: soft body thump plus a little dust puff. Louder
        /// landings scale up at the call site via PlayOneShot's volume.
        public static AudioClip Land(string name)
        {
            float dur = 0.16f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, 170f, 0f, 0.13f, 0.5f,
                new float[] { 1f, 0.5f }, new float[] { 1f, 0.4f }, new float[] { 1f, 0.8f },
                0.002f, 1.8f);
            NoiseVoice(data, 0.004f, 0.11f, 0.16f, 1000f, 380f, 0.003f, 0.06f, 202, 1.4f);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // Collecting
        // ------------------------------------------------------------------

        /// A gem: bright chime; the combo ladder in AudioManager picks the
        /// pitch, so a gem run climbs the scale and becomes a riff.
        public static AudioClip GemPickup(string name, float freq)
        {
            return Chime(name, freq, 0.35f, 0.42f);
        }

        /// A checkpoint: three warm rising chimes — progress that sings.
        public static AudioClip CheckpointChime(string name)
        {
            float[] notes = { 440f, 659.25f, 880f };
            float dur = 0.75f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], i * 0.1f, 0.42f, 0.4f,
                    ChimePartials, ChimeWeights, ChimeDecays, 0.004f, 1.9f);
            return MakeClip(name, data);
        }

        /// A spare life: three soft notes over a warm low bed — a hug in C.
        public static AudioClip HeartChime(string name)
        {
            float dur = 0.8f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, 130.8f, 0f, 0.5f, 0.14f,
                new float[] { 1f }, new float[] { 1f }, new float[] { 1f }, 0.01f, 1.2f);
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], i * 0.09f, 0.4f, 0.36f,
                    BoxPartials, BoxWeights, BoxDecays, 0.006f, 1.8f);
            return MakeClip(name, data);
        }

        /// A bounce pad: a springy boing — fast rise, wobbling settle.
        public static AudioClip BounceSpring(string name)
        {
            float dur = 0.24f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, 0.02f, 0.2f, 3000f, 3000f, 0.001f, 0.015f, 303);
            int samples = data.Length - 1;
            float phase0 = 0f, phase1 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Frequency springs up fast (t^0.6), then eases into the top.
                float f = Mathf.Lerp(150f, 880f, Mathf.Pow(t, 0.6f));
                float wobble = 1f + Mathf.Sin(2f * Mathf.PI * 9f * t) * 0.05f * t;
                phase0 += 2f * Mathf.PI * f * wobble / SampleRate;
                phase1 += 2f * Mathf.PI * f * 2f * wobble / SampleRate;
                float env = Mathf.Pow(1f - t, 1.3f);
                data[i] = (Mathf.Sin(phase0) + 0.3f * Mathf.Sin(phase1)) *
                    Mathf.Min(t * 200f, 1f) * env * 0.5f;
            }
            return MakeClip(name, data);
        }

        /// Gloomfang's Gift: three bell-toned notes, a found-treasure ring.
        public static AudioClip GiftChime(string name)
        {
            float[] notes = { 783.99f, 1174.66f, 1567.98f };
            float dur = 1f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], i * 0.12f, 0.6f, 0.34f,
                    BellPartials, BellWeights, BellDecays, 0.003f, 2f);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // Danger
        // ------------------------------------------------------------------

        /// Hazard death: a bright crack, then a long dark drop.
        public static AudioClip HazardDeath(string name)
        {
            float dur = 0.62f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, 0.05f, 0.5f, 3400f, 2400f, 0.001f, 0.04f, 404);
            Voice(data, 200f, 0.02f, 0.6f, 0.55f,
                new float[] { 1f, 0.5f }, new float[] { 1f, 0.5f }, new float[] { 1f, 0.7f },
                0.003f, 1.3f);
            return MakeClip(name, data);
        }

        /// Fall death: the wind rises past Pip's ears, then a far-off poof.
        public static AudioClip FallDeath(string name)
        {
            float dur = 0.66f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, 0.5f, 0.42f, 2400f, 220f, 0.05f, 0.16f, 505, 1.5f);
            Voice(data, 130f, 0.44f, 0.2f, 0.4f,
                new float[] { 1f, 0.5f }, new float[] { 1f, 0.4f }, new float[] { 1f, 0.8f },
                0.002f, 1.8f);
            return MakeClip(name, data);
        }

        /// Game over: a low thud under a slow minor swell. Rest, not punish.
        public static AudioClip GameOverSting(string name)
        {
            float dur = 1.5f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, 90f, 0f, 0.35f, 0.5f,
                new float[] { 1f, 2f }, new float[] { 1f, 0.2f }, new float[] { 1f, 1.6f },
                0.002f, 1.6f);
            float[] chord = { 110f, 130.8f, 164.8f };
            for (int i = 0; i < chord.Length; i++)
            {
                // A slightly detuned pair per voice breathes instead of beeping.
                Voice(data, chord[i] * 0.998f, 0.1f, 1.35f, 0.16f,
                    new float[] { 1f }, new float[] { 1f }, new float[] { 1f }, 0.3f, 1f);
                Voice(data, chord[i] * 1.003f, 0.1f, 1.35f, 0.16f,
                    new float[] { 1f }, new float[] { 1f }, new float[] { 1f }, 0.3f, 1f);
            }
            return MakeClip(name, data);
        }

        /// Down to the last life: two gentle "careful now" notes.
        public static AudioClip LivesLow(string name)
        {
            float dur = 0.8f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, 329.63f, 0f, 0.4f, 0.28f,
                BoxPartials, BoxWeights, BoxDecays, 0.01f, 1.6f);
            Voice(data, 261.63f, 0.22f, 0.5f, 0.28f,
                BoxPartials, BoxWeights, BoxDecays, 0.01f, 1.6f);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // Winning
        // ------------------------------------------------------------------

        /// Level won: a chord stab, a rising run, one high sparkle.
        public static AudioClip WinFanfare(string name)
        {
            float dur = 1.4f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            float[] stab = { 523.25f, 659.25f, 783.99f };
            for (int i = 0; i < stab.Length; i++)
                Voice(data, stab[i], 0f, 0.55f, 0.22f,
                    ChimePartials, ChimeWeights, ChimeDecays, 0.003f, 1.6f);
            float[] run = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            for (int i = 0; i < run.Length; i++)
                Voice(data, run[i], 0.1f + i * 0.1f, 0.4f, 0.3f,
                    ChimePartials, ChimeWeights, ChimeDecays, 0.003f, 1.8f);
            Voice(data, 2093f, 0.62f, 0.75f, 0.16f,
                BoxPartials, BoxWeights, BoxDecays, 0.004f, 2.2f);
            return MakeClip(name, data);
        }

        /// The whole game beaten: four swelling chords, starting after the
        /// level fanfare has had its moment (head silence keeps the two
        /// celebrations out of each other's way).
        public static AudioClip CompleteFanfare(string name)
        {
            float lead = 1.15f;
            float dur = lead + 2.9f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            float[][] chords = {
                new float[] { 261.6f, 329.6f, 392.0f, 523.25f },
                new float[] { 174.6f, 220.0f, 261.6f, 349.2f },
                new float[] { 196.0f, 246.9f, 293.7f, 392.0f },
                new float[] { 261.6f, 329.6f, 392.0f, 523.25f, 659.25f }
            };
            float[] starts = { 0f, 0.6f, 1.2f, 1.8f };
            float chordLen = 0.85f;
            for (int c = 0; c < chords.Length; c++)
            {
                float len = (c == chords.Length - 1) ? 1.5f : chordLen;
                for (int v = 0; v < chords[c].Length; v++)
                    Voice(data, chords[c][v], lead + starts[c], len, 0.15f,
                        new float[] { 1f, 2f }, new float[] { 1f, 0.18f },
                        new float[] { 1f, 1.4f }, 0.04f, 1.2f);
            }
            Voice(data, 2093f, lead + 2.1f, 0.9f, 0.13f,
                BoxPartials, BoxWeights, BoxDecays, 0.01f, 2.4f);
            return MakeClip(name, data);
        }

        /// A star landing on the win screen; pitch climbs per star.
        public static AudioClip StarDing(string name, int step)
        {
            float[] pitches = { 1046.5f, 1318.5f, 1567.98f };
            return Chime(name, pitches[Mathf.Clamp(step, 0, pitches.Length - 1)],
                0.6f, 0.42f);
        }

        /// New best time: a quick bright flourish.
        public static AudioClip RecordFlourish(string name)
        {
            float[] notes = { 1318.5f, 1567.98f, 2093f };
            float dur = 0.9f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], i * 0.08f, 0.55f, 0.28f,
                    ChimePartials, ChimeWeights, ChimeDecays, 0.003f, 2f);
            return MakeClip(name, data);
        }

        /// Sky Garden victory: the bloom wave as a music-box run, starting
        /// just after the fanfare so both read as one long celebration.
        public static AudioClip BloomRun(string name)
        {
            float lead = 0.4f;
            float[] notes = { 523.25f, 587.33f, 659.25f, 783.99f,
                              880f, 1046.5f, 1174.66f, 1318.5f };
            float dur = lead + notes.Length * 0.16f + 0.6f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], lead + i * 0.16f, 0.5f, 0.3f,
                    BoxPartials, BoxWeights, BoxDecays, 0.006f, 2f);
            NoiseVoice(data, lead, dur - lead - 0.1f, 0.05f, 2600f, 3400f,
                0.3f, 0.5f, 606);
            return MakeClip(name, data);
        }

        /// The Long Winter: waking the sunstone lantern — a warm two-note
        /// rise with a soft shimmer tail, like a held breath letting go.
        public static AudioClip LanternLight(string name)
        {
            float[] data = new float[(int)(2.2f * SampleRate) + 1];
            Voice(data, 523.25f, 0.02f, 0.9f, 0.3f,
                BoxPartials, BoxWeights, BoxDecays, 0.006f, 2f);
            Voice(data, 783.99f, 0.16f, 1.4f, 0.28f,
                BoxPartials, BoxWeights, BoxDecays, 0.008f, 2.2f);
            NoiseVoice(data, 0.05f, 1.6f, 0.04f, 2400f, 3600f, 0.25f, 0.9f, 608);
            return MakeClip(name, data);
        }

        /// An ice gate giving way to the lantern's warmth: a glassy
        /// downward sigh, two water-drop notes at the end.
        public static AudioClip MeltSigh(string name)
        {
            float[] data = new float[(int)(1.7f * SampleRate) + 1];
            NoiseVoice(data, 0f, 1.2f, 0.10f, 3200f, 700f, 0.08f, 0.7f, 609);
            Voice(data, 1046.5f, 0.55f, 0.7f, 0.16f,
                BoxPartials, BoxWeights, BoxDecays, 0.004f, 2.4f);
            Voice(data, 659.25f, 0.85f, 0.7f, 0.12f,
                BoxPartials, BoxWeights, BoxDecays, 0.004f, 2.6f);
            return MakeClip(name, data);
        }

        /// The Long Winter victory: the crystal map — the melted paths
        /// refreezing as a glassy rising run with bell-like inharmonic
        /// partials, slower and cooler than the garden's bloom.
        public static AudioClip CrystalRun(string name)
        {
            float lead = 0.45f;
            float[] notes = { 523.25f, 659.25f, 783.99f, 880f, 1046.5f, 1318.5f };
            float dur = lead + notes.Length * 0.18f + 0.9f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            float[] partials = { 1f, 2.76f, 5.42f };
            float[] weights = { 1f, 0.35f, 0.12f };
            float[] decays = { 1f, 1.8f, 3f };
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], lead + i * 0.18f, 0.9f, 0.26f,
                    partials, weights, decays, 0.005f, 2.2f);
            NoiseVoice(data, lead, dur - lead - 0.2f, 0.045f, 3000f, 4200f,
                0.4f, 0.6f, 607);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // The world: wind, bells, bridges, guardians, mirrors
        // ------------------------------------------------------------------

        /// Nim's giggle: five staccato blips in a laughing arc — the
        /// telegraph that a gust is about to carry whoever waits for it.
        public static AudioClip Giggle(string name)
        {
            float[] notes = { 659.25f, 783.99f, 880f, 783.99f, 659.25f };
            float[] starts = { 0f, 0.09f, 0.185f, 0.29f, 0.405f };
            float dur = 0.75f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
            {
                bool last = i == notes.Length - 1;
                Voice(data, notes[i], starts[i], last ? 0.22f : 0.07f, 0.3f,
                    new float[] { 1f, 2f }, new float[] { 1f, 0.15f },
                    new float[] { 1f, 1.5f }, 0.008f, 2.2f,
                    last ? 11f : 0f, 0.02f, 0.3f);
            }
            return MakeClip(name, data);
        }

        /// A bell strike: hum-weighted inharmonic partials, each dying at
        /// its own pace, ringing for `duration` seconds.
        public static AudioClip BellTone(string name, float frequency, float duration, float volume)
        {
            int samples = Mathf.Max(1, (int)(duration * SampleRate));
            float[] data = new float[samples];
            Voice(data, frequency, 0f, duration, volume,
                BellPartials, BellWeights, BellDecays, 0.002f, 1.7f);
            // The strike itself: a bright transient, gone in 30 ms.
            NoiseVoice(data, 0f, 0.03f, volume * 0.4f, 5000f, 3000f, 0.001f, 0.025f, 808);
            return MakeClip(name, data);
        }

        /// An echo bridge turning solid: three quick high chimes in a breath
        /// of air.
        public static AudioClip BridgeOn(string name)
        {
            float[] notes = { 1046.5f, 1318.5f, 1567.98f };
            float dur = 0.65f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], i * 0.05f, 0.3f, 0.24f,
                    ChimePartials, ChimeWeights, ChimeDecays, 0.003f, 2.2f);
            NoiseVoice(data, 0f, 0.4f, 0.06f, 2800f, 3600f, 0.03f, 0.3f, 909);
            return MakeClip(name, data);
        }

        /// An echo expiring: the same idea, falling, quieter.
        public static AudioClip BridgeOff(string name)
        {
            float[] notes = { 1567.98f, 1318.5f, 1046.5f };
            float dur = 0.55f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            for (int i = 0; i < notes.Length; i++)
                Voice(data, notes[i], i * 0.06f, 0.28f, 0.14f,
                    ChimePartials, ChimeWeights, ChimeDecays, 0.004f, 2.2f);
            return MakeClip(name, data);
        }

        /// A sleeping guardian waking: a rising growl with a final tick.
        public static AudioClip GuardianWake(string name)
        {
            float dur = 0.38f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            int samples = data.Length - 1;
            float phase0 = 0f, phase1 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float f = Mathf.Lerp(105f, 196f, t);
                phase0 += 2f * Mathf.PI * f / SampleRate;
                phase1 += 2f * Mathf.PI * f * 2f / SampleRate;
                // The growl: the octave grows as it wakes.
                float growl = Mathf.Lerp(0.08f, 0.4f, t);
                data[i] = (Mathf.Sin(phase0) + growl * Mathf.Sin(phase1)) *
                    Mathf.Min(t * 30f, 1f) * Mathf.Pow(1f - t, 0.9f) * 0.34f;
            }
            NoiseVoice(data, 0.3f, 0.04f, 0.12f, 2000f, 2000f, 0.002f, 0.03f, 110);
            return MakeClip(name, data);
        }

        /// Stepping through a mirror door: a glassy cluster, a breath of
        /// travel, arrival somewhere slightly elsewhere.
        public static AudioClip MirrorTransit(string name)
        {
            float dur = 0.62f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            float[] glass = { 1975.5f, 2093f, 2349.3f };
            for (int i = 0; i < glass.Length; i++)
                Voice(data, glass[i], 0f, 0.18f, 0.08f,
                    new float[] { 1f }, new float[] { 1f }, new float[] { 1f },
                    0.03f, 1.2f);
            NoiseVoice(data, 0.05f, 0.34f, 0.3f, 800f, 2200f, 0.08f, 0.18f, 121, 0.8f);
            Voice(data, 100f, 0.36f, 0.22f, 0.3f,
                new float[] { 1f, 0.5f }, new float[] { 1f, 0.4f }, new float[] { 1f, 0.8f },
                0.002f, 1.8f);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // UI: tiny, polite, never louder than gameplay
        // ------------------------------------------------------------------

        /// The universal button tick: wood and one millisecond of air.
        public static AudioClip UIClick(string name)
        {
            float dur = 0.05f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            Voice(data, 1250f, 0f, 0.04f, 0.2f,
                new float[] { 1f }, new float[] { 1f }, new float[] { 1f }, 0.001f, 2.4f);
            NoiseVoice(data, 0f, 0.012f, 0.08f, 4200f, 4200f, 0.001f, 0.01f, 131);
            return MakeClip(name, data);
        }

        /// A settings toggle flipping: up for on, down for off.
        public static AudioClip UIToggle(string name, bool on)
        {
            float f0 = on ? 760f : 1000f;
            float f1 = on ? 1000f : 740f;
            return TwoToneBlip(name, f0, f1, 0.08f, 0.22f);
        }

        /// Pause breathes out, resume breathes in.
        public static AudioClip PauseBlip(string name, bool pausing)
        {
            return TwoToneBlip(name, pausing ? 520f : 380f, pausing ? 390f : 540f,
                0.1f, 0.24f);
        }

        static AudioClip TwoToneBlip(string name, float f0, float f1,
            float duration, float volume)
        {
            int samples = Mathf.Max(1, (int)(duration * SampleRate));
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, t) / SampleRate;
                data[i] = Mathf.Sin(phase) * Mathf.Min(t * 200f, 1f) *
                    (1f - t) * volume;
            }
            return MakeClip(name, data);
        }

        /// A panel sliding: breath rising (open) or settling (close).
        public static AudioClip PanelSwoosh(string name, bool open)
        {
            float dur = 0.17f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, dur, 0.16f,
                open ? 350f : 1400f, open ? 1400f : 320f, 0.03f, 0.08f, 141);
            return MakeClip(name, data);
        }

        /// The mission card arriving: a soft low page of air.
        public static AudioClip IntroSwoosh(string name)
        {
            float dur = 0.42f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, dur, 0.14f, 260f, 950f, 0.1f, 0.22f, 151, 0.8f);
            return MakeClip(name, data);
        }

        /// An epilogue page: paper flick and a soft settle.
        public static AudioClip PageTurn(string name)
        {
            float dur = 0.11f;
            float[] data = new float[(int)(dur * SampleRate) + 1];
            NoiseVoice(data, 0f, 0.06f, 0.16f, 1900f, 900f, 0.004f, 0.05f, 161, 1.2f);
            Voice(data, 880f, 0.03f, 0.06f, 0.1f,
                new float[] { 1f }, new float[] { 1f }, new float[] { 1f }, 0.002f, 2.4f);
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // Weather swells (gust onsets ride the music clock)
        // ------------------------------------------------------------------

        /// A wind swell: white noise through a one-pole low-pass whose
        /// cutoff arcs up then back down (breath in, breath out), over a
        /// quiet sine resting on the chord root — gusts audibly "play the
        /// chord" they are phase-locked to.
        public static AudioClip NoiseSwell(string name, float duration, float volume)
        {
            int samples = Mathf.Max(1, (int)(duration * SampleRate));
            float[] data = new float[samples];
            const float rootFrequency = 130.8f;
            float lowpassed = 0f;
            float rootPhase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Cutoff and loudness share one arc: rise to mid-swell,
                // fall away after.
                float arc = Mathf.Sin(t * Mathf.PI);
                float cutoff = Mathf.Lerp(250f, 1500f, arc);
                float alpha = Mathf.Min(1f, 2f * Mathf.PI * cutoff / SampleRate);
                lowpassed += alpha * ((Random.value * 2f - 1f) - lowpassed);
                rootPhase += 2f * Mathf.PI * rootFrequency / SampleRate;
                data[i] = (lowpassed * 2.5f
                    + 0.3f * Mathf.Sin(rootPhase)
                    + 0.12f * Mathf.Sin(2f * rootPhase)) * arc * volume;
            }
            return MakeClip(name, data);
        }

        // ------------------------------------------------------------------
        // Assembly
        // ------------------------------------------------------------------

        /// Peak-guards the buffer and wraps it in a clip; shared with
        /// MusicSynth so the score gets the same protection.
        public static AudioClip FinalizeClip(string name, float[] data)
        {
            // Peak-guard: additive voices can stack past full scale; keep
            // every clip inside ±1 so Unity's hard clipper never bites.
            float peak = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > peak) peak = a;
            }
            if (peak > 0.95f)
            {
                float g = 0.95f / peak;
                for (int i = 0; i < data.Length; i++) data[i] *= g;
            }
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeClip(string name, float[] data)
        {
            return FinalizeClip(name, data);
        }
    }
}
