using UnityEngine;

namespace GemRush
{
    /// Synthesizes short sound effects into AudioClips at runtime,
    /// so the game ships with zero audio files.
    public static class SfxSynth
    {
        const int SampleRate = 44100;

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
            return Arp(name, new float[] { frequency }, duration, volume);
        }

        static AudioClip MakeClip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
