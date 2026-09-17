using UnityEngine;

namespace GemRush
{
    /// Synthesizes gentle ambient chord-pad loops at runtime — the game
    /// ships with zero audio files. Two moods: a warm day loop and a lower,
    /// darker loop for the Undercloud. Quiet by design; the SFX sit on top.
    public static class MusicSynth
    {
        const int SampleRate = 44100;

        // Day: C - Am - F - G, around C4. Warm and open.
        static readonly float[][] DayChords = new float[][]
        {
            new float[] { 261.6f, 329.6f, 392.0f },
            new float[] { 220.0f, 261.6f, 329.6f },
            new float[] { 174.6f, 220.0f, 261.6f },
            new float[] { 196.0f, 246.9f, 293.7f }
        };

        // Dark: Am - F - Dm - Em, dropped an octave. Undercloud mood.
        static readonly float[][] DarkChords = new float[][]
        {
            new float[] { 110.0f, 130.8f, 164.8f },
            new float[] { 87.3f, 110.0f, 130.8f },
            new float[] { 73.4f, 87.3f, 110.0f },
            new float[] { 82.4f, 98.0f, 123.5f }
        };

        /// A seamless chord-pad loop: each chord swells and releases, so the
        /// loop point lands on silence. One soft octave harmonic per voice
        /// keeps it warm without getting shrill.
        public static AudioClip PadLoop(string name, bool dark, float volume)
        {
            float[][] chords = dark ? DarkChords : DayChords;
            float secondsPerChord = 2.2f;
            int perChord = Mathf.Max(1, (int)(secondsPerChord * SampleRate));
            float[] data = new float[perChord * chords.Length];

            for (int c = 0; c < chords.Length; c++)
            {
                for (int i = 0; i < perChord; i++)
                {
                    float t = (float)i / perChord;
                    // Slow swell in, slow release out — no clicks anywhere.
                    float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(t * 4f, 1f)) *
                                     Mathf.SmoothStep(0f, 1f, Mathf.Min((1f - t) * 4f, 1f));
                    float sample = 0f;
                    for (int v = 0; v < chords[c].Length; v++)
                    {
                        float angle = 2f * Mathf.PI * chords[c][v] * i / SampleRate;
                        sample += Mathf.Sin(angle)
                            + 0.22f * Mathf.Sin(2f * angle)
                            + 0.10f * Mathf.Sin(0.5f * angle); // sub warmth
                    }
                    data[c * perChord + i] = sample * envelope * volume;
                }
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
