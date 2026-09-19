using UnityEngine;

namespace GemRush
{
    /// Synthesizes the game's score at runtime — the game ships with zero
    /// audio files. Every realm gets its own mood: a chord progression with
    /// its own tempo, voicing (harmonics, sub warmth, detune width, glassy
    /// shimmer) and, for some realms, a sparse music-box motif. Moods that
    /// host gusts keep four 2.2 s chords (the 8.8 s loop the gust clock
    /// phase-locks to).
    ///
    /// Quiet by design; the SFX sit on top. Seamless: each chord swells and
    /// releases, so the loop point lands on silence.
    public static class MusicSynth
    {
        const int SampleRate = 44100;

        /// Ambience bed a mood wants under its pad (played by AudioManager).
        public enum AmbienceKind { None, Wind, WindHigh, Rumble }

        struct MoodSpec
        {
            public float[][] Chords;   // four chords, voiced mid-low
            public float ChordSeconds;
            public float Harmonic2;    // octave shimmer weight
            public float Sub;          // sub-octave warmth weight
            public float Detune;       // relative detune of a width voice
            public float Shimmer;      // 3.02x glassy partial weight
            public float[] MotifNotes; // sparse music-box notes (seconds into loop)
            public float[] MotifTimes;
            public bool BellMotif;     // motif voiced as soft bell tones
            public AmbienceKind Ambience;
        }

        // C-major pentatonic helpers for the motifs.
        const float C5 = 523.25f, D5 = 587.33f, E5 = 659.25f,
                    G5 = 783.99f, A5 = 880f;

        static readonly float[][] DayChords = {
            new float[] { 261.6f, 329.6f, 392.0f },
            new float[] { 220.0f, 261.6f, 329.6f },
            new float[] { 174.6f, 220.0f, 261.6f },
            new float[] { 196.0f, 246.9f, 293.7f }
        };

        static readonly float[][] DarkChords = {
            new float[] { 110.0f, 130.8f, 164.8f },
            new float[] { 87.3f, 110.0f, 130.8f },
            new float[] { 73.4f, 87.3f, 110.0f },
            new float[] { 82.4f, 98.0f, 123.5f }
        };

        static readonly float[][] MenuChords = {   // Cmaj7 - Am7 - Fmaj7 - G6
            new float[] { 261.6f, 329.6f, 392.0f, 493.9f },
            new float[] { 220.0f, 261.6f, 329.6f, 392.0f },
            new float[] { 174.6f, 220.0f, 261.6f, 329.6f },
            new float[] { 196.0f, 246.9f, 293.7f, 392.0f }
        };

        static readonly float[][] SunsetChords = { // Fmaj7 - C - Am7 - G6
            new float[] { 174.6f, 220.0f, 261.6f, 329.6f },
            new float[] { 130.8f, 196.0f, 261.6f },
            new float[] { 220.0f, 261.6f, 329.6f, 392.0f },
            new float[] { 196.0f, 246.9f, 293.7f, 392.0f }
        };

        static readonly float[][] GardenChords = { // Cadd9 - Am7 - Fmaj7 - G6
            new float[] { 261.6f, 293.7f, 329.6f, 392.0f },
            new float[] { 220.0f, 261.6f, 329.6f, 392.0f },
            new float[] { 174.6f, 220.0f, 261.6f, 329.6f },
            new float[] { 196.0f, 246.9f, 293.7f, 392.0f }
        };

        static readonly float[][] WindChords = {   // Csus2 - Am7 - Fsus2 - G6
            new float[] { 130.8f, 196.0f, 293.7f },
            new float[] { 110.0f, 164.8f, 220.0f, 261.6f },
            new float[] { 87.3f, 174.6f, 196.0f, 261.6f },
            new float[] { 98.0f, 196.0f, 246.9f, 293.7f }
        };

        static readonly float[][] BellsChords = {  // Am - G - F - Em, low
            new float[] { 110.0f, 220.0f, 261.6f },
            new float[] { 98.0f, 196.0f, 246.9f },
            new float[] { 87.3f, 174.6f, 220.0f },
            new float[] { 82.4f, 164.8f, 196.0f }
        };

        static readonly float[][] FlightChords = { // Cmaj7 - Em7 - Fmaj7 - Am7
            new float[] { 130.8f, 329.6f, 392.0f, 493.9f },
            new float[] { 82.4f, 164.8f, 196.0f, 293.7f },
            new float[] { 87.3f, 174.6f, 261.6f, 329.6f },
            new float[] { 110.0f, 220.0f, 261.6f, 329.6f }
        };

        static readonly float[][] MirrorChords = { // Csus2 - Dsus2 - Csus2 - Gsus2
            new float[] { 130.8f, 146.8f, 196.0f },
            new float[] { 146.8f, 164.8f, 220.0f },
            new float[] { 130.8f, 146.8f, 196.0f },
            new float[] { 98.0f, 220.0f, 293.7f }
        };

        static readonly float[][] WinterChords = { // Am7 - Fmaj7 - Cmaj7 - Gsus2, low and hushed
            new float[] { 110.0f, 164.8f, 261.6f },
            new float[] { 87.3f, 174.6f, 220.0f },
            new float[] { 130.8f, 196.0f, 329.6f },
            new float[] { 98.0f, 146.8f, 246.9f }
        };

        static readonly float[][] FestivalChords = { // C - G - Am - F, bright and open
            new float[] { 130.8f, 261.6f, 329.6f, 392.0f },
            new float[] { 98.0f, 196.0f, 293.7f, 392.0f },
            new float[] { 110.0f, 220.0f, 329.6f },
            new float[] { 87.3f, 220.0f, 349.2f }
        };

        static MoodSpec Spec(SoundMood mood)
        {
            MoodSpec s = new MoodSpec();
            s.ChordSeconds = 2.2f;
            s.Harmonic2 = 0.22f;
            s.Sub = 0.10f;
            switch (mood)
            {
                case SoundMood.Menu:
                    s.Chords = MenuChords;
                    s.ChordSeconds = 3.1f;
                    s.MotifNotes = new float[] { E5, G5, C5, A5, C5, D5 };
                    s.MotifTimes = new float[] { 1.55f, 2.33f, 5.0f, 8.1f, 9.9f, 11.4f };
                    break;
                case SoundMood.Dark:
                    s.Chords = DarkChords;
                    s.Harmonic2 = 0.12f;
                    s.Sub = 0.3f;
                    s.Ambience = AmbienceKind.Rumble;
                    break;
                case SoundMood.Sunset:
                    s.Chords = SunsetChords;
                    s.Harmonic2 = 0.18f;
                    s.Sub = 0.14f;
                    break;
                case SoundMood.Garden:
                    s.Chords = GardenChords;
                    s.MotifNotes = new float[] { G5, E5, C5, E5, D5 };
                    s.MotifTimes = new float[] { 1.4f, 3.9f, 5.8f, 6.5f, 7.9f };
                    break;
                case SoundMood.Wind:
                    s.Chords = WindChords;
                    s.Harmonic2 = 0.12f;
                    s.Sub = 0.18f;
                    s.Ambience = AmbienceKind.Wind;
                    break;
                case SoundMood.Bells:
                    s.Chords = BellsChords;
                    s.Harmonic2 = 0.10f;
                    s.Sub = 0.2f;
                    s.BellMotif = true;
                    // One low bell per chord change: the towers keep time.
                    s.MotifNotes = new float[] { 220f, 196f, 174.6f, 164.8f };
                    s.MotifTimes = new float[] { 0f, 2.2f, 4.4f, 6.6f };
                    s.Ambience = AmbienceKind.WindHigh;
                    break;
                case SoundMood.Flight:
                    s.Chords = FlightChords;
                    s.ChordSeconds = 2.75f;
                    s.Harmonic2 = 0.15f;
                    s.Sub = 0.12f;
                    s.Detune = 0.002f;
                    s.Ambience = AmbienceKind.WindHigh;
                    break;
                case SoundMood.Mirror:
                    s.Chords = MirrorChords;
                    s.ChordSeconds = 2.75f;
                    s.Harmonic2 = 0.16f;
                    s.Sub = 0.08f;
                    s.Shimmer = 0.12f;
                    break;
                case SoundMood.Winter:
                    // The quietest pack: long hushed chords (3.4 s each,
                    // a 13.6 s loop) and a handful of distant music-box
                    // twinkles, like snowfall catching light.
                    s.Chords = WinterChords;
                    s.ChordSeconds = 3.4f;
                    s.Harmonic2 = 0.14f;
                    s.Sub = 0.14f;
                    s.Shimmer = 0.10f;
                    s.MotifNotes = new float[] { E5, G5, A5, D5, C5 };
                    s.MotifTimes = new float[] { 2.1f, 5.4f, 8.2f, 10.6f, 12.4f };
                    break;
                case SoundMood.Festival:
                    // The realm's celebration: bright open chords with a
                    // sprinkle of high bell notes — the sky realm at its
                    // most joyful.
                    s.Chords = FestivalChords;
                    s.Harmonic2 = 0.2f;
                    s.Sub = 0.12f;
                    s.Shimmer = 0.12f;
                    s.BellMotif = true;
                    s.MotifNotes = new float[] { 783.99f, 1046.5f, 880f,
                                                 1318.5f, 1046.5f };
                    s.MotifTimes = new float[] { 0.9f, 2.4f, 4.1f, 5.9f, 7.4f };
                    break;
                default: // Day — the realm's home sound, close to the original.
                    s.Chords = DayChords;
                    break;
            }
            return s;
        }

        /// The pad loop's exact length; world rhythms (gusts) phase-lock to
        /// it. Moods that host gusts keep the 8.8 s four-chord loop.
        public static float MoodLoopLength(SoundMood mood)
        {
            return 4f * Spec(mood).ChordSeconds;
        }

        /// Which ambience bed the mood wants under its pad.
        public static AmbienceKind MoodAmbience(SoundMood mood)
        {
            return Spec(mood).Ambience;
        }

        /// The full mood loop: swelling chord pad plus, for some moods, a
        /// sparse motif on top.
        public static AudioClip MoodLoop(string name, SoundMood mood, float volume)
        {
            return MoodLoop(name, mood, volume, true);
        }

        /// withMotif = false renders the bed layer: chords only, no
        /// music-box melody. Identical length and chord envelopes to the
        /// full loop, so the two crossfade sample-for-sample.
        public static AudioClip MoodLoop(string name, SoundMood mood,
            float volume, bool withMotif)
        {
            MoodSpec s = Spec(mood);
            int perChord = Mathf.Max(1, (int)(s.ChordSeconds * SampleRate));
            float[] data = new float[perChord * s.Chords.Length];

            for (int c = 0; c < s.Chords.Length; c++)
            {
                for (int i = 0; i < perChord; i++)
                {
                    float t = (float)i / perChord;
                    // Slow swell in, slow release out — no clicks anywhere.
                    float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(t * 4f, 1f)) *
                                     Mathf.SmoothStep(0f, 1f, Mathf.Min((1f - t) * 4f, 1f));
                    float sample = 0f;
                    for (int v = 0; v < s.Chords[c].Length; v++)
                    {
                        float angle = 2f * Mathf.PI * s.Chords[c][v] * i / SampleRate;
                        sample += Mathf.Sin(angle)
                            + s.Harmonic2 * Mathf.Sin(2f * angle)
                            + s.Sub * Mathf.Sin(0.5f * angle); // sub warmth
                        if (s.Detune > 0f)
                        {
                            // A slightly flat twin widens the pad into a wash.
                            sample += 0.6f * Mathf.Sin(angle * (1f - s.Detune));
                        }
                        if (s.Shimmer > 0f)
                        {
                            sample += s.Shimmer * Mathf.Sin(3.02f * angle);
                        }
                    }
                    data[c * perChord + i] = sample * envelope * volume;
                }
            }

            // The motif: music-box notes dropped into the pad, always
            // pentatonic against the current chords.
            if (withMotif && s.MotifNotes != null && s.MotifTimes != null)
            {
                float[] partials = s.BellMotif
                    ? new float[] { 0.5f, 1f, 2.76f, 5.42f }
                    : new float[] { 1f, 4f };
                float[] weights = s.BellMotif
                    ? new float[] { 0.35f, 1f, 0.3f, 0.1f }
                    : new float[] { 1f, 0.06f };
                float[] decays = s.BellMotif
                    ? new float[] { 0.7f, 1f, 1.6f, 2.6f }
                    : new float[] { 0.7f, 1.4f };
                float decay = Mathf.Min(0.7f, s.ChordSeconds * 0.5f);
                float motifVol = volume * (s.BellMotif ? 2.4f : 3.2f);
                for (int n = 0; n < s.MotifNotes.Length &&
                                n < s.MotifTimes.Length; n++)
                {
                    SfxSynth.Voice(data, s.MotifNotes[n], s.MotifTimes[n],
                        decay, motifVol, partials, weights, decays, 0.008f, 1.9f);
                }
            }

            return SfxSynth.FinalizeClip(name, data);
        }

        /// A seamless filtered-noise bed: `seconds` of wind with a circular
        /// crossfade, so it loops without a seam. The undulation must be a
        /// multiple of 1/seconds to stay loop-exact.
        public static AudioClip WindLoop(string name, float volume, float cutoff,
            float undulateHz, int seed)
        {
            const float seconds = 4f;
            int length = (int)(seconds * SampleRate);
            int fade = (int)(0.4f * SampleRate);
            float[] raw = new float[length + fade];
            System.Random rng = new System.Random(seed);
            float y = 0f;
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);
            for (int i = 0; i < raw.Length; i++)
                y += alpha * ((float)rng.NextDouble() * 2f - 1f - y);

            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float s = raw[i];
                if (i < fade)
                {
                    // Head blends with the beyond-end content, so the wrap
                    // point plays continuously through.
                    float w = (float)i / fade;
                    s = raw[length + i] * (1f - w) + raw[i] * w;
                }
                float und = 0.78f + 0.22f *
                    Mathf.Sin(2f * Mathf.PI * undulateHz * i / SampleRate);
                data[i] = s * und * volume;
            }
            return SfxSynth.FinalizeClip(name, data);
        }

        /// A deep air rumble for the Undercloud: dark noise over a 55 Hz
        /// sub (integer cycles in the 4 s loop, so it wraps exactly).
        public static AudioClip RumbleLoop(string name, float volume)
        {
            const float seconds = 4f;
            int length = (int)(seconds * SampleRate);
            int fade = (int)(0.4f * SampleRate);
            float[] raw = new float[length + fade];
            System.Random rng = new System.Random(4404);
            float y = 0f;
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * 110f / SampleRate);
            for (int i = 0; i < raw.Length; i++)
                y += alpha * ((float)rng.NextDouble() * 2f - 1f - y);

            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float s = raw[i];
                if (i < fade)
                {
                    float w = (float)i / fade;
                    s = raw[length + i] * (1f - w) + raw[i] * w;
                }
                float sub = Mathf.Sin(2f * Mathf.PI * 55f * i / SampleRate);
                data[i] = (s * 2.2f + 0.5f * sub) * volume;
            }
            return SfxSynth.FinalizeClip(name, data);
        }

        /// The goal portal's warm hum. Every partial completes whole cycles
        /// in the 4 s loop, and the beating between detuned pairs is a
        /// multiple of 0.25 Hz — the loop is exact by construction.
        public static AudioClip HumLoop(string name, float volume)
        {
            const float seconds = 4f;
            int length = (int)(seconds * SampleRate);
            float[] data = new float[length];
            // { base frequency, beat between the pair, weight }
            float[][] voices = {
                new float[] { 130.75f, 0.5f, 1f },
                new float[] { 196.0f, 0.25f, 0.5f },
                new float[] { 261.5f, 0.5f, 0.35f }
            };
            for (int v = 0; v < voices.Length; v++)
            {
                float f = voices[v][0];
                float beat = voices[v][1];
                float w = voices[v][2];
                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / SampleRate;
                    data[i] += w * 0.5f *
                        (Mathf.Sin(2f * Mathf.PI * (f + beat * 0.5f) * t) +
                         Mathf.Sin(2f * Mathf.PI * (f - beat * 0.5f) * t));
                }
            }
            for (int i = 0; i < length; i++) data[i] *= volume;
            return SfxSynth.FinalizeClip(name, data);
        }

        /// Legacy entry: the original two-mood pad, kept for the bool-based
        /// SetMood path.
        public static AudioClip PadLoop(string name, bool dark, float volume)
        {
            return MoodLoop(name, dark ? SoundMood.Dark : SoundMood.Day, volume);
        }
    }
}
