using UnityEngine;

namespace GemRush
{
    /// Holds 2D AudioSources and the synthesized clips; other scripts call
    /// the PlayXxx wrappers. Also plays the ambient chord-pad loop, switching
    /// between a day mood and a darker Undercloud mood per level.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource source;
        AudioSource musicSource;
        AudioClip jump;
        AudioClip pickup;
        AudioClip checkpoint;
        AudioClip win;
        AudioClip die;
        AudioClip bounce;
        AudioClip heart;
        AudioClip dayLoop;
        AudioClip darkLoop;
        bool darkMood;
        bool moodInitialized;

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.55f;
            musicSource.loop = true;

            jump = SfxSynth.Sweep("sfx_jump", 320f, 660f, 0.16f, 0.5f);
            pickup = SfxSynth.Arp("sfx_pickup", new float[] { 880f, 1318.5f }, 0.09f, 0.42f);
            checkpoint = SfxSynth.Arp("sfx_checkpoint", new float[] { 659.3f, 987.8f }, 0.12f, 0.45f);
            win = SfxSynth.Arp("sfx_win",
                new float[] { 523.3f, 659.3f, 784.0f, 1046.5f }, 0.16f, 0.5f);
            die = SfxSynth.Sweep("sfx_die", 440f, 130f, 0.4f, 0.5f);
            bounce = SfxSynth.Sweep("sfx_bounce", 180f, 720f, 0.2f, 0.5f);
            heart = SfxSynth.Arp("sfx_heart",
                new float[] { 659.3f, 880.0f, 1318.5f }, 0.11f, 0.48f);
        }

        /// Called when a level is built: pick the pad loop matching the
        /// level's mood. Clips are synthesized lazily, once per mood.
        public void SetMood(bool dark)
        {
            if (moodInitialized && darkMood == dark) return;
            darkMood = dark;
            moodInitialized = true;

            if (dark && darkLoop == null)
                darkLoop = MusicSynth.PadLoop("music_dark", true, 0.13f);
            if (!dark && dayLoop == null)
                dayLoop = MusicSynth.PadLoop("music_day", false, 0.13f);

            musicSource.clip = dark ? darkLoop : dayLoop;
            SyncMusic();
        }

        /// Keeps the loop in step with the sound setting without an extra
        /// settings screen — music is part of "Sound". The setting poll runs
        /// a few times a second, not every frame.
        float soundCheckTimer;
        bool lastSoundOn = true;

        void Update()
        {
            soundCheckTimer -= Time.unscaledDeltaTime;
            if (soundCheckTimer <= 0f)
            {
                soundCheckTimer = 0.25f;
                lastSoundOn = SaveSystem.SoundOn;
            }

            bool shouldPlay = lastSoundOn &&
                              musicSource.clip != null &&
                              GameManager.Instance != null &&
                              (GameManager.Instance.State == GameState.Playing ||
                               GameManager.Instance.State == GameState.Menu ||
                               GameManager.Instance.State == GameState.Paused);
            if (shouldPlay && !musicSource.isPlaying) SyncMusic();
            if (!shouldPlay && musicSource.isPlaying) musicSource.Stop();
        }

        void SyncMusic()
        {
            if (musicSource.clip != null && SaveSystem.SoundOn) musicSource.Play();
        }

        public void PlayJump()
        {
            PlayIfOn(jump);
        }

        public void PlayPickup()
        {
            PlayIfOn(pickup);
        }

        public void PlayCheckpoint()
        {
            PlayIfOn(checkpoint);
        }

        public void PlayWin()
        {
            PlayIfOn(win);
        }

        public void PlayDie()
        {
            PlayIfOn(die);
        }

        public void PlayBounce()
        {
            PlayIfOn(bounce);
        }

        public void PlayHeart()
        {
            PlayIfOn(heart);
        }

        readonly System.Collections.Generic.Dictionary<int, AudioClip> noteCache =
            new System.Collections.Generic.Dictionary<int, AudioClip>();

        /// Melody gems: plays a soft tone; clips are cached per frequency.
        public void PlayNote(float frequency)
        {
            if (!SaveSystem.SoundOn) return;
            int key = Mathf.RoundToInt(frequency * 10f);
            if (!noteCache.TryGetValue(key, out AudioClip clip))
            {
                clip = SfxSynth.Note("note_" + key, frequency, 0.35f, 0.4f);
                noteCache[key] = clip;
            }
            source.PlayOneShot(clip);
        }

        void PlayIfOn(AudioClip clip)
        {
            if (clip != null && SaveSystem.SoundOn) source.PlayOneShot(clip);
        }
    }
}
