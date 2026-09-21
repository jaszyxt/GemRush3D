using UnityEngine;
using System.Collections.Generic;

namespace GemRush
{
    /// Holds the 2D AudioSources and every synthesized clip; other scripts
    /// call the PlayXxx wrappers. Everything respects the sound setting
    /// ("Sound" covers music, ambience and effects alike).
    ///
    /// Channels:
    ///   source      — all one-shot effects and UI sounds
    ///   musicSource — the per-mood pad loop (see MusicSynth)
    ///   windSource  — the wind bed: mood ambience plus updraft/gust pulse
    ///   humSource   — the goal portal's proximity hum
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource source;
        AudioSource musicSource;
        AudioSource bedSource;   // the same pad, melody stripped (adaptive)
        AudioSource windSource;
        AudioSource humSource;

        // ---- One-shot effects, synthesized once in Awake ----
        AudioClip[] jumpVariants;
        AudioClip[] landVariants;
        AudioClip[] stepVariants;

        // Baked variant pitches (fatigue pass): the ear flags an identical
        // take faster than an identical loudness.
        static readonly float[] JumpPitches = { 0.96f, 1f, 1.05f };
        static readonly float[] LandPitches = { 0.94f, 1f, 1.06f };
        static readonly float[] StepPitches = { 0.9f, 1f, 1.1f };
        AudioClip land;
        AudioClip pickup;
        AudioClip checkpoint;
        AudioClip win;
        AudioClip dieHazard;
        AudioClip dieFall;
        AudioClip gameOver;
        AudioClip livesLow;
        AudioClip bounce;
        AudioClip heart;
        AudioClip gift;
        AudioClip mirror;
        AudioClip bridgeOn;
        AudioClip bridgeOff;
        AudioClip guardianWake;
        AudioClip seesawCreak;
        AudioClip seesawSpring;
        AudioClip skid;
        AudioClip giggle;
        AudioClip softGiggle;
        AudioClip raindropBloom;
        AudioClip lanternLight;
        AudioClip meltSigh;
        AudioClip uiClick;
        AudioClip uiToggleOn;
        AudioClip uiToggleOff;
        AudioClip panelOpen;
        AudioClip panelClose;
        AudioClip pauseBlip;
        AudioClip resumeBlip;
        AudioClip introSwoosh;
        AudioClip pageTurn;

        // ---- Lazily synthesized celebratory clips ----
        // Shared cache; keep this key map current. A collision silently
        // plays the WRONG sound at a key moment — which is exactly what
        // happened when PlayTrophy and PlayConcert both claimed 9600.
        //   0..9000   melody notes, keyed by frequency*10 (PlayNote)
        //   100..112  gem combo ladder steps (PlayPickup)
        //   1000..    bell tones, keyed by index and ring length
        //   9000      complete fanfare     9100+step  star dings
        //   9200      new record           9300       bloom run
        //   9400      crystal run          9500       milestone chime
        //   9600      trophy chime         9700       trail tier
        //   9800      level unlock         9900       festival concert
        //   9950      photo shutter
        readonly Dictionary<int, AudioClip> noteCache =
            new Dictionary<int, AudioClip>();

        /// Plays a clip that came out of the lazy cache, null-safe. These
        /// paths used to call source.PlayOneShot directly, so a builder
        /// that ever returned null would NRE on the gem-collect path
        /// rather than simply falling silent.
        void PlayCached(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || !SaveSystem.SoundOn) return;
            source.PlayOneShot(clip, volumeScale);
        }

        // ---- Music / mood ----
        // Mood pads are the largest resident audio in the game (an 8.8 s
        // 44.1 kHz mono float clip is ~1.5 MB, and a full playthrough visits
        // a dozen moods). A plain static dictionary grew without bound and
        // Resources.UnloadUnusedAssets could never reclaim it, because the
        // dictionary held the only reference. Hence: a small LRU that
        // destroys what it evicts. Three moods covers menu -> level A ->
        // level B with no re-synthesis, which is every realistic hop.
        const int MoodCacheLimit = 3;
        static readonly Dictionary<int, AudioClip> moodLoops =
            new Dictionary<int, AudioClip>();
        static readonly List<int> moodLru = new List<int>();

        static readonly Dictionary<int, AudioClip> moodBedLoops =
            new Dictionary<int, AudioClip>();

        static readonly Dictionary<int, AudioClip> ambienceLoops =
            new Dictionary<int, AudioClip>();
        SoundMood activeMood = SoundMood.Day;
        bool moodInitialized;
        AudioClip moodBed;         // this mood's ambience bed, or null
        float moodWindBase;        // bed level on the wind channel

        void Awake()
        {
            // NOTE: Instance is assigned at the END of Awake, after the
            // sources and clips are built. Assigning it first meant an
            // allocation failure partway through boot left a singleton
            // whose sources were still null, so every later frame NREd in
            // Update. Until the end, the manager is simply not published.
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = MusicVolume;
            musicSource.loop = true;

            // The bed layer: the same pad loop with the melody stripped.
            // Always in sync with musicSource; the melody crossfades to it
            // when the music "holds its breath" (last life, game over).
            bedSource = gameObject.AddComponent<AudioSource>();
            bedSource.playOnAwake = false;
            bedSource.spatialBlend = 0f;
            bedSource.volume = 0f;
            bedSource.loop = true;

            windSource = gameObject.AddComponent<AudioSource>();
            windSource.playOnAwake = false;
            windSource.spatialBlend = 0f;
            windSource.loop = true;
            windSource.volume = 0f;

            humSource = gameObject.AddComponent<AudioSource>();
            humSource.playOnAwake = false;
            humSource.spatialBlend = 0f;
            humSource.loop = true;
            humSource.volume = 0f;

            // Pip's movement: baked pitch variants so repeated moves never
            // sound like a sampled loop (locked by AudioAuditTests).
            jumpVariants = new AudioClip[JumpPitches.Length];
            for (int i = 0; i < JumpPitches.Length; i++)
                jumpVariants[i] = SfxSynth.Jump("sfx_jump_" + i, JumpPitches[i]);
            landVariants = new AudioClip[LandPitches.Length];
            for (int i = 0; i < LandPitches.Length; i++)
                landVariants[i] = SfxSynth.Land("sfx_land_" + i, LandPitches[i]);
            stepVariants = new AudioClip[StepPitches.Length];
            for (int i = 0; i < StepPitches.Length; i++)
                stepVariants[i] = SfxSynth.Footstep("sfx_step_" + i, StepPitches[i]);
            pickup = SfxSynth.GemPickup("sfx_pickup", 880f);
            checkpoint = SfxSynth.CheckpointChime("sfx_checkpoint");
            win = SfxSynth.WinFanfare("sfx_win");
            dieHazard = SfxSynth.HazardDeath("sfx_die_hazard");
            dieFall = SfxSynth.FallDeath("sfx_die_fall");
            gameOver = SfxSynth.GameOverSting("sfx_gameover");
            livesLow = SfxSynth.LivesLow("sfx_liveslow");
            bounce = SfxSynth.BounceSpring("sfx_bounce");
            heart = SfxSynth.HeartChime("sfx_heart");
            gift = SfxSynth.GiftChime("sfx_gift");
            mirror = SfxSynth.MirrorTransit("sfx_mirror");
            bridgeOn = SfxSynth.BridgeOn("sfx_bridge_on");
            bridgeOff = SfxSynth.BridgeOff("sfx_bridge_off");
            guardianWake = SfxSynth.GuardianWake("sfx_guardian_wake");
            seesawCreak = SfxSynth.SeeSawCreak("sfx_seesaw_creak");
            seesawSpring = SfxSynth.SeeSawSpring("sfx_seesaw_spring");
            skid = SfxSynth.Skid("sfx_skid");
            giggle = SfxSynth.Giggle("sfx_giggle");
            softGiggle = SfxSynth.SoftGiggle("sfx_gloomfang_giggle");
            raindropBloom = SfxSynth.RaindropBloom("sfx_raindrop");
            lanternLight = SfxSynth.LanternLight("sfx_lantern");
            meltSigh = SfxSynth.MeltSigh("sfx_melt");

            uiClick = SfxSynth.UIClick("ui_click");
            uiToggleOn = SfxSynth.UIToggle("ui_toggle_on", true);
            uiToggleOff = SfxSynth.UIToggle("ui_toggle_off", false);
            panelOpen = SfxSynth.PanelSwoosh("ui_panel_open", true);
            panelClose = SfxSynth.PanelSwoosh("ui_panel_close", false);
            pauseBlip = SfxSynth.PauseBlip("ui_pause", true);
            resumeBlip = SfxSynth.PauseBlip("ui_resume", false);
            introSwoosh = SfxSynth.IntroSwoosh("ui_intro");
            pageTurn = SfxSynth.PageTurn("ui_page");

            // Fully built; safe to publish.
            Instance = this;
        }

        // ------------------------------------------------------------------
        // Mood / music
        // ------------------------------------------------------------------

        /// Called when a level is built (or the menu is shown): switch the
        /// pad loop and ambience bed to the level's mood. Clips are
        /// synthesized lazily, once per mood, and cached statically.
        /// True while the melody layer is faded out — the only time the
        /// separate melody-stripped bed is audible. Set by the crossfade
        /// below; read by SetMood so it never renders a bed nobody hears.
        bool bedNeeded;

        /// The melody-stripped bed for a mood, synthesized on demand and
        /// cached alongside the pad. Distinct from MoodPad so the common
        /// case (melody audible) never pays for it.
        static AudioClip MoodBed(SoundMood mood)
        {
            if (moodBedLoops.TryGetValue((int)mood, out AudioClip bed))
                return bed;
            bed = MusicSynth.MoodLoop("music_bed_" + mood, mood, 0.13f,
                withMotif: false);
            moodBedLoops[(int)mood] = bed;
            return bed;
        }

        /// A mood's full pad, held in a bounded LRU: the least recently
        /// used mood is destroyed (AudioClip.Destroy, so the PCM actually
        /// goes back) when a fourth mood is requested.
        static AudioClip MoodPad(SoundMood mood)
        {
            int key = (int)mood;
            if (moodLoops.TryGetValue(key, out AudioClip clip))
            {
                moodLru.Remove(key);
                moodLru.Add(key);
                return clip;
            }
            clip = MusicSynth.MoodLoop("music_" + mood, mood, 0.13f);
            moodLoops[key] = clip;
            moodLru.Add(key);
            while (moodLru.Count > MoodCacheLimit)
            {
                int evict = moodLru[0];
                moodLru.RemoveAt(0);
                AudioClip evicted;
                if (moodLoops.TryGetValue(evict, out evicted) &&
                    evicted != null)
                {
                    moodLoops.Remove(evict);
                    Object.Destroy(evicted);
                }
                // Its bed goes too — same mood, same memory class.
                AudioClip bed;
                if (moodBedLoops.TryGetValue(evict, out bed) && bed != null)
                {
                    moodBedLoops.Remove(evict);
                    Object.Destroy(bed);
                }
            }
            return clip;
        }

        public void SetMood(SoundMood mood)
        {
            if (moodInitialized && activeMood == mood) return;
            activeMood = mood;
            moodInitialized = true;

            musicLoopLength = MusicSynth.MoodLoopLength(mood);
            musicSource.clip = MoodPad(mood);

            // The melody-stripped bed is only ever heard when the melody
            // ducks (two lives left, or game over) — so it is synthesized
            // on first actual need instead of doubling every mood's cost
            // up front. Until then the full pad doubles as the bed.
            bedSource.clip = bedNeeded ? MoodBed(mood) : musicSource.clip;

            // The mood's ambience bed sits under the pad on the wind
            // channel; updrafts and gusts pulse the same channel on top.
            moodBed = null;
            moodWindBase = 0f;
            MusicSynth.AmbienceKind amb = MusicSynth.MoodAmbience(mood);
            if (amb != MusicSynth.AmbienceKind.None)
            {
                if (!ambienceLoops.TryGetValue((int)amb, out moodBed))
                {
                    moodBed = BuildAmbience(amb);
                    ambienceLoops[(int)amb] = moodBed;
                }
                moodWindBase = amb == MusicSynth.AmbienceKind.Rumble ? 0.42f
                    : amb == MusicSynth.AmbienceKind.WindHigh ? 0.45f
                    : amb == MusicSynth.AmbienceKind.Rain ? 0.50f
                    : amb == MusicSynth.AmbienceKind.Snow ? 0.40f : 0.55f;
            }
            if (windSource.clip != moodBed)
            {
                windSource.clip = moodBed;
                if (moodBed != null && windSource.isPlaying) windSource.Stop();
            }

            SyncMusic();
        }

        static AudioClip BuildAmbience(MusicSynth.AmbienceKind kind)
        {
            switch (kind)
            {
                case MusicSynth.AmbienceKind.Wind:
                    return MusicSynth.WindLoop("amb_wind", 0.3f, 500f, 0.25f, 77);
                case MusicSynth.AmbienceKind.WindHigh:
                    return MusicSynth.WindLoop("amb_wind_high", 0.22f, 950f, 0.5f, 78);
                case MusicSynth.AmbienceKind.Rumble:
                    return MusicSynth.RumbleLoop("amb_rumble", 0.2f);
                case MusicSynth.AmbienceKind.Rain:
                    // Rain is a wind loop in rain's clothes: higher cutoff
                    // (a soft steady hiss on the roofs), almost no swell.
                    return MusicSynth.WindLoop("amb_rain", 0.14f, 1700f,
                        0.08f, 79);
                case MusicSynth.AmbienceKind.Snow:
                    // Snow is a hush, not a hiss: very high and very still,
                    // well under the rain bed so the two never read alike.
                    return MusicSynth.WindLoop("amb_snow", 0.09f, 2600f,
                        0.04f, 80);
                default:
                    return null;
            }
        }

        /// Legacy bool path: the old day/dark switch.
        public void SetMood(bool dark)
        {
            SetMood(dark ? SoundMood.Dark : SoundMood.Day);
        }

        /// Keeps the loop in step with the sound setting without an extra
        /// settings screen — music is part of "Sound". The setting poll runs
        // a few times a second, not every frame.
        float soundCheckTimer;
        bool lastSoundOn = true;
        bool lastMusicOn = true;
        bool lastAmbienceOn = true;

        // Death/win ducking: snap the pad down to a fraction of its level,
        // then ease it back on unscaled time — the sting reads in near
        // silence, and the pad breathes back in afterwards. Voice lines
        // duck deeper and longer via DuckFor (while narration plays).
        // Mix calibration (measure_mix.py + industry norms): the score
        // is a bed and must sit ~6-10 dB under key SFX — at 0.55 it measured
        // AS LOUD as the pickups/checkpoints it should be sitting under.
        const float MusicVolume = 0.26f;
        const float DuckFraction = 0.35f;
        const float DuckRestoreSeconds = 2.5f;

        /// Voice sits deeper than a sting so narration clearly leads.
        public const float VoiceDuckFraction = 0.18f;

        /// Current duck gain (1 = unducked), recomputed each frame and read
        /// by every bed — music, ambience and the portal hum alike. Ambience
        /// holding full level through a death sting was why the sting failed
        /// to read "in near silence" on wind and rain levels.
        float duckGain = 1f;
        float duckTimer;
        float duckFraction = DuckFraction;

        // Self-accumulated clock for GetMusicPhase while the pad is
        // silent, so world rhythms (gusts) keep their beat with sound off.
        float fallbackPhase;

        // Adaptive intensity (RESEARCH audio queue): the melody layer
        // crossfades OUT when the music holds its breath — game over, or
        // Pip down to his last life — and back IN when a heart returns.
        // The chord bed never stops, so the loop stays seamless; the fade
        // runs on unscaled time and is slow enough to feel like weather.
        const float MelodyFadeSeconds = 2f;
        float melodyFactor = 1f;

        float MelodyTarget
        {
            get
            {
                if (GameManager.Instance == null) return 1f;
                switch (GameManager.Instance.State)
                {
                    case GameState.GameOver:
                        return 0f;
                    case GameState.Playing:
                    case GameState.Paused:
                        // Graded thinning (RESEARCH near-death state): the
                        // melody thins at two lives — the song starts
                        // holding its breath before the last one.
                        if (GameManager.Instance.Lives <= 1) return 0f;
                        if (GameManager.Instance.Lives == 2) return 0.55f;
                        return 1f;
                    default: // menu, won, complete: celebrate
                        return 1f;
                }
            }
        }

        void Update()
        {
            // One volume pass per frame: duck factor (death/win sting)
            // times the melody crossfade, applied to both layers.
            if (duckTimer > 0f)
            {
                duckTimer = Mathf.Max(0f, duckTimer - Time.unscaledDeltaTime);
                float restore = Mathf.SmoothStep(0f, 1f,
                    1f - duckTimer / DuckRestoreSeconds);
                duckGain = Mathf.Lerp(duckFraction, 1f, restore);
            }
            else
            {
                // Expired: return to full and forget the depth, so the next
                // duck starts from a clean slate rather than inheriting the
                // shallowest fraction ever requested.
                duckGain = 1f;
                duckFraction = DuckFraction;
            }
            float duck = duckGain;
            melodyFactor = Mathf.MoveTowards(melodyFactor, MelodyTarget,
                Time.unscaledDeltaTime / MelodyFadeSeconds);
            // The bed only becomes audible as the melody ducks; synthesize
            // it at that moment rather than at every mood change.
            bool needBed = MelodyTarget < 1f;
            if (needBed && !bedNeeded && moodInitialized)
            {
                bedNeeded = true;
                bedSource.clip = MoodBed(activeMood);
                SyncMusic();
            }
            bedSource.volume = MusicVolume * duck;
            musicSource.volume = MusicVolume * duck * melodyFactor;

            // The fallback clock only advances while the real music clock
            // is silent, so GetMusicPhase always offers a fresh phase.
            if (musicSource.clip == null || !musicSource.isPlaying)
                fallbackPhase += Time.deltaTime;

            soundCheckTimer -= Time.unscaledDeltaTime;
            if (soundCheckTimer <= 0f)
            {
                soundCheckTimer = 0.25f;
                lastSoundOn = SaveSystem.SoundOn;
                lastMusicOn = SaveSystem.MusicOn;
                lastAmbienceOn = SaveSystem.AmbienceOn;
            }

            bool musicAllowed = lastSoundOn && lastMusicOn;
            bool shouldPlay = musicAllowed &&
                              musicSource.clip != null &&
                              GameManager.Instance != null &&
                              (GameManager.Instance.State == GameState.Playing ||
                               GameManager.Instance.State == GameState.Menu ||
                               GameManager.Instance.State == GameState.Paused);
            if (shouldPlay && !musicSource.isPlaying) SyncMusic();
            if (!shouldPlay && musicSource.isPlaying)
            {
                musicSource.Stop();
                bedSource.Stop();
            }

            UpdateWind();
            UpdateHum();
        }

        void SyncMusic()
        {
            if (musicSource.clip == null || !SaveSystem.SoundOn) return;
            musicSource.Play();
            bedSource.clip = moodBedLoops.TryGetValue((int)activeMood,
                out AudioClip bed) ? bed : null;
            if (bedSource.clip != null)
            {
                bedSource.time = musicSource.time;
                bedSource.Play();
            }
        }

        // ------------------------------------------------------------------
        // The wind bed: the mood's ambience plays at its base level, and
        // gameplay (updrafts) pulses it harder via a heartbeat — callers
        // re-assert every frame they want wind, and the level decays away.
        // ------------------------------------------------------------------

        const float WindPulseLevel = 0.75f;
        const float WindPulseHoldSeconds = 0.3f;
        const float WindFadeSpeed = 2.2f;
        float windPulseUntil;

        /// While Pip is inside an updraft (or a gust lane), call every
        /// frame; the wind swells up and decays back to the mood bed.
        public void PulseWind()
        {
            if (!SaveSystem.SoundOn || !SaveSystem.AmbienceOn) return;
            if (windSource.clip == null) return;
            windPulseUntil = Time.unscaledTime + WindPulseHoldSeconds;
        }

        void UpdateWind()
        {
            if (windSource.clip == null || !SaveSystem.SoundOn ||
                !SaveSystem.AmbienceOn)
            {
                if (windSource.volume > 0f && !windSource.isPlaying) return;
                windSource.volume = 0f;
                if (windSource.isPlaying) windSource.Stop();
                return;
            }
            bool pulsed = Time.unscaledTime < windPulseUntil;
            // The bed rides the same duck as the music: a death sting has to
            // read over the wind too, or it does not read at all on the
            // wind and rain levels where the bed is loudest.
            float target = Mathf.Clamp01(
                moodWindBase + (pulsed ? WindPulseLevel : 0f)) * duckGain;
            windSource.volume = Mathf.MoveTowards(windSource.volume, target,
                WindFadeSpeed * Time.unscaledDeltaTime);
            if (windSource.volume > 0.005f && !windSource.isPlaying)
                windSource.Play();
            if (windSource.volume <= 0.005f && windSource.isPlaying)
                windSource.Stop();
        }

        // ------------------------------------------------------------------
        // The portal hum: GoalPortal reports player proximity each frame;
        // the hum fades toward it, so "getting close to the goal" is heard
        // before it is seen.
        // ------------------------------------------------------------------

        // Calibrated: at full proximity the hum sat ABOVE the music bed;
        // ambience belongs under it.
        const float HumMaxVolume = 0.08f;
        const float HumFadeSpeed = 1.4f;
        float humTarget;

        public void SetPortalProximity(float proximity01)
        {
            humTarget = Mathf.Clamp01(proximity01);
        }

        /// The portal hum clip, synthesized off the Update path. Rendering
        /// it inside UpdateHum used to stall the frame the player first got
        /// near a portal — the worst possible moment to hitch.
        static AudioClip humClip;

        static AudioClip HumClip()
        {
            if (humClip == null)
                humClip = MusicSynth.HumLoop("amb_portal_hum", 1f);
            return humClip;
        }

        void UpdateHum()
        {
            bool allowed = lastSoundOn && lastAmbienceOn && humTarget > 0.001f;
            if (allowed && humSource.clip == null)
                humSource.clip = HumClip();
            float target = (allowed ? humTarget * HumMaxVolume : 0f) * duckGain;
            humSource.volume = Mathf.MoveTowards(humSource.volume, target,
                HumFadeSpeed * Time.unscaledDeltaTime);
            if (humSource.volume > 0.005f && !humSource.isPlaying)
                humSource.Play();
            if (humSource.volume <= 0.005f && humSource.isPlaying)
                humSource.Stop();
        }

        // ------------------------------------------------------------------
        // The music clock (gusts phase-lock to it)
        // ------------------------------------------------------------------

        /// The gust-hosting loop length: 8.8 s (4 x 2.2), the value every
        /// mood that hosts a gust keeps so gust periods divide it evenly.
        /// Moods that never host a gust (Menu 12.4 s, Winter 13.6 s, Rain /
        /// Flight / Mirror 11.0 s) are free to differ — the live length is
        /// set per mood in SetMood and read through GetMusicPhase.
        public const float MusicLoopLength = 8.8f;

        float musicLoopLength = MusicLoopLength;

        /// Musical time in seconds: the pad's position in its loop while
        /// the music plays, or a self-accumulated clock when it is silent.
        /// World rhythms (gusts) read this to stay in phase with the chords.
        public float GetMusicPhase()
        {
            if (musicSource.clip != null && musicSource.isPlaying)
                return Mathf.Repeat(musicSource.time, musicLoopLength);
            return fallbackPhase;
        }

        /// Restarts the pad from time 0 — the loop swells in on the tonic,
        /// so a respawn lands back on the home chord.
        public void RestartMusicAtTonic()
        {
            if (musicSource.clip == null) return;
            musicSource.time = 0f;
            bedSource.time = 0f;
            SyncMusic();
        }

        /// Checkpoint cadence: restarts the pad at its LAST chord (the
        /// dominant), so the loop's next step resolves home to the tonic
        /// right under the checkpoint chime. Phase-lock worlds (gusts)
        /// simply re-sync to the new phase on their next read.
        public void RestartMusicAtDominant()
        {
            if (musicSource.clip == null) return;
            musicSource.time = musicLoopLength * 0.75f;
            bedSource.time = musicLoopLength * 0.75f;
            SyncMusic();
        }

        // ------------------------------------------------------------------
        // Pip: movement
        // ------------------------------------------------------------------

        public void PlayJump()
        {
            if (jumpVariants == null) return;
            PlayIfOn(jumpVariants[Random.Range(0, jumpVariants.Length)],
                Random.Range(0.92f, 1.05f));
        }

        /// Per-playback volume jitter: the ear flags an identical sample
        /// faster than an identical loudness, so repeated one-shots wobble
        /// slightly around their calibrated level (never more than ~1 dB).
        static float Jitter(float min, float max)
        {
            return Random.Range(min, max);
        }

        /// Landing impact, from soft walk-offs (2.5) to terminal-velocity
        /// plunges (28): the thud scales, tiny hops stay silent.
        public void PlayLand(float impactSpeed)
        {
            if (impactSpeed < 2.5f) return;
            if (landVariants == null) return;
            float strength = Mathf.Clamp01((impactSpeed - 2.5f) / 14f);
            PlayIfOn(landVariants[Random.Range(0, landVariants.Length)],
                (0.45f + 0.55f * strength) * Jitter(0.94f, 1.06f));
            // Only firm landings register tactually — a walk-off hop should
            // not buzz, or the phone becomes a rattle during normal play.
            if (impactSpeed > 8f) Haptics.Light();
        }

        // ------------------------------------------------------------------
        // Collecting
        // ------------------------------------------------------------------

        // Combo chain for the pickup pitch-ramp: each chained gem climbs
        // one semitone, capped at an octave — gem trails become little
        // rising songs. Zeroed on death so every run starts the song over.
        static int comboStreak;

        /// The live chained-gem count (read-only): the visible-mastery
        /// layer (popups, crown) ramps on the same number the audio does.
        public static int CurrentStreak { get { return comboStreak; } }

        public void PlayPickup()
        {
            int step = Mathf.Min(comboStreak, 12);
            comboStreak++;

            // Visible mastery fires before the sound gate: the streak
            // crown and the every-10th-gem shockwave are rewards the
            // player SEES, with sound on or off.
            ComboCrown.Notify(comboStreak);
            bool milestone = comboStreak > 0 && comboStreak % 10 == 0;
            if (milestone && GameBootstrap.Player != null)
                Fx.Ring(GameBootstrap.Player.transform.position, ArtLib.Gold);

            // Haptics ride OUTSIDE the sound gate, and ONLY on the streak
            // milestone. A gem is the most repeated interaction in the game
            // (dozens per level, often in fast chains) — buzzing every one
            // turns the phone into a rattle in the player's hands, which is
            // the opposite of what a haptic is for. The every-10th gem
            // already has a chime and a gold ring; the buzz marks that same
            // moment, so the ladder is felt exactly when it is celebrated.
            if (milestone) Haptics.Medium();

            if (!SaveSystem.SoundOn) return;

            // Every 10th chained gem sings a tiny fanfare on top — a
            // milestone in the streak, festival-style.
            if (milestone)
                PlayMilestoneChime();

            // One clip per step, cached like the notes and bells; step 0
            // reuses the clip synthesized in Awake.
            AudioClip clip;
            if (step == 0)
            {
                clip = pickup;
            }
            else if (!noteCache.TryGetValue(100 + step, out clip))
            {
                float scale = Mathf.Pow(2f, step / 12f);
                clip = SfxSynth.GemPickup("pickup_" + step, 880f * scale);
                noteCache[100 + step] = clip;
            }
            PlayCached(clip);
        }

        /// Cached bright triad for the every-10th-gem milestone.
        public void PlayMilestoneChime()
        {
            if (!noteCache.TryGetValue(9500, out AudioClip clip))
            {
                clip = SfxSynth.MilestoneChime("sfx_milestone");
                noteCache[9500] = clip;
            }
            PlayCached(clip);
        }

        /// Zeros the pickup combo chain so the next gem sings the base note.
        public static void ResetPickupStreak()
        {
            comboStreak = 0;
            ComboCrown.Notify(0); // the crown fades with the streak
        }

        public void PlayCheckpoint()
        {
            PlayIfOn(checkpoint);
        }

        public void PlayHeart()
        {
            PlayIfOn(heart);
        }

        /// Gloomfang's Gift: its own discovery chime, distinct from a win.
        public void PlayGift()
        {
            PlayIfOn(gift);
        }

        // ------------------------------------------------------------------
        // Danger
        // ------------------------------------------------------------------

        /// fell: Pip dropped past the kill line; otherwise a hazard hit.
        public void PlayDie(bool fell)
        {
            DuckMusic();
            PlayIfOn(fell ? dieFall : dieHazard);
        }

        /// Legacy single entry (hazard default).
        public void PlayDie()
        {
            PlayDie(false);
        }

        /// Out of lives: the dark sting under the game-over panel.
        public void PlayGameOver()
        {
            PlayIfOn(gameOver);
        }

        /// Just dropped to the last life: a gentle "careful now".
        public void PlayLivesLow()
        {
            PlayIfOn(livesLow);
        }

        // ------------------------------------------------------------------
        // Winning
        // ------------------------------------------------------------------

        /// Dips the pad to ~35% of its level so a death or win sting plays
        /// in near-silence; Update eases it back over ~2.5 s.
        void DuckMusic()
        {
            duckFraction = Mathf.Min(duckFraction, DuckFraction);
            duckTimer = Mathf.Max(duckTimer, DuckRestoreSeconds);
        }

        /// While a voice line plays, duck deeper than a sting and hold for
        /// the line's whole length (plus its breath). The fraction takes the
        /// MINIMUM with whatever duck is already active: a voice line
        /// starting during a death sting must never raise the music back up
        /// mid-sting, which is what a plain assignment did.
        public void DuckFor(float seconds, float fraction)
        {
            duckFraction = Mathf.Min(duckFraction, Mathf.Clamp01(fraction));
            duckTimer = Mathf.Max(duckTimer, seconds);
        }

        public void PlayWin()
        {
            DuckMusic();
            PlayIfOn(win);
        }

        /// The whole game beaten: four swelling chords after the fanfare.
        public void PlayComplete()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9000, out AudioClip clip))
            {
                clip = SfxSynth.CompleteFanfare("sfx_complete");
                noteCache[9000] = clip;
            }
            PlayCached(clip);
        }

        /// A star landing on the win screen (0, 1, 2 = first..third star).
        public void PlayStarDing(int step)
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9100 + step, out AudioClip clip))
            {
                clip = SfxSynth.StarDing("ui_star_" + step, step);
                noteCache[9100 + step] = clip;
            }
            PlayCached(clip);
        }

        /// The photo-mode shutter — its own voice, not a borrowed star ding.
        public void PlayShutter()
        {
            if (!SaveSystem.SoundOn) return;
            // 9950, not 9200: 9200 is PlayNewRecord's. Reusing it made the
            // shutter and the new-record flourish share one cached clip, so
            // one of them would silently play the other's sound — the same
            // class of collision the key map below warns about.
            if (!noteCache.TryGetValue(9950, out AudioClip clip))
            {
                clip = SfxSynth.Shutter("ui_shutter");
                noteCache[9950] = clip;
            }
            PlayCached(clip);
        }

        /// New best time: a bright little flourish.
        public void PlayNewRecord()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9200, out AudioClip clip))
            {
                clip = SfxSynth.RecordFlourish("ui_record");
                noteCache[9200] = clip;
            }
            PlayCached(clip);
        }

        /// A shelf trophy appearing: a small glassy keepsake chime.
        public void PlayTrophy()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9600, out AudioClip clip))
            {
                clip = SfxSynth.TrophyChime("sfx_trophy");
                noteCache[9600] = clip;
            }
            PlayCached(clip);
        }

        /// Crossing a star-trail mastery tier.
        public void PlayTrailTier()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9700, out AudioClip clip))
            {
                clip = SfxSynth.TrailTierSting("sfx_trail_tier");
                noteCache[9700] = clip;
            }
            PlayCached(clip);
        }

        /// A new level opening up: two bright notes that fan outward.
        public void PlayLevelUnlock()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9800, out AudioClip clip))
            {
                clip = SfxSynth.LevelUnlock("sfx_level_unlock");
                noteCache[9800] = clip;
            }
            PlayCached(clip);
        }

        /// Sky Garden: the bloom wave as a rising music-box run.
        public void PlayBloom()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9300, out AudioClip clip))
            {
                clip = SfxSynth.BloomRun("sfx_bloom");
                noteCache[9300] = clip;
            }
            PlayCached(clip);
        }

        /// The Long Winter: waking the sunstone lantern.
        public void PlayLantern()
        {
            PlayIfOn(lanternLight);
        }

        /// An ice gate melting away beside Pip.
        public void PlayMelt()
        {
            PlayIfOn(meltSigh);
        }

        /// The Long Winter victory: the crystal-map refreeze run.
        public void PlayCrystal()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9400, out AudioClip clip))
            {
                clip = SfxSynth.CrystalRun("sfx_crystal");
                noteCache[9400] = clip;
            }
            PlayCached(clip);
        }

        /// The Aurora Festival: the realm's concert under the win fanfare.
        public void PlayConcert()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9900, out AudioClip clip))
            {
                clip = SfxSynth.FestivalConcert("sfx_concert");
                noteCache[9900] = clip;
            }
            PlayCached(clip);
        }

        public void PlayBounce()
        {
            PlayIfOn(bounce);
            // A launch is an impact-class event: the pad should be felt.
            Haptics.Light();
        }

        // ------------------------------------------------------------------
        // The world
        // ------------------------------------------------------------------

        readonly float[] bellFrequencies = { 196f, 220f, 261.6f, 293.7f };

        /// Echo bells: a long inharmonic strike; cached per bell index.
        public void PlayBellTone(int bellIndex, float toneSeconds)
        {
            if (!SaveSystem.SoundOn) return;
            int safe = Mathf.Clamp(bellIndex, 0, bellFrequencies.Length - 1);
            // The ring lasts the echo's length plus a tail; cache per
            // length bucket so spire bells ring longer than garden bells.
            int bucket = Mathf.RoundToInt(toneSeconds * 2f);
            int key = 1000 + safe * 32 + Mathf.Clamp(bucket, 0, 31);
            if (!noteCache.TryGetValue(key, out AudioClip clip))
            {
                clip = SfxSynth.BellTone("bell_" + safe + "_" + bucket,
                    bellFrequencies[safe], Mathf.Max(4f, toneSeconds + 1.5f), 0.30f);
                noteCache[key] = clip;
            }
            PlayCached(clip);
        }

        /// Legacy: six-second ring.
        public void PlayBellTone(int bellIndex)
        {
            PlayBellTone(bellIndex, 6f);
        }

        /// Mirror doors: glass cluster, breath of travel, elsewhere.
        public void PlayMirror()
        {
            PlayIfOn(mirror);
        }

        /// An echo bridge materializing (or expiring).
        public void PlayBridge(bool solid)
        {
            PlayIfOn(solid ? bridgeOn : bridgeOff);
        }

        /// A sleeping guardian waking up grumpy.
        public void PlayGuardianWake(float volumeScale)
        {
            if (volumeScale < 0.03f) return;
            PlayIfOn(guardianWake, volumeScale);
        }

        AudioClip gustSwell;

        // Gust swell anti-stack: gust lanes sharing a period phase-lock to
        // the same music clock, so two zones onset in the same frame —
        // without this guard every onset doubled into +6 dB of wind.
        const float GustSwellStackWindow = 0.4f;
        float lastGustSwellAt = -99f;

        /// Gust onsets: a filtered-noise swell resting on the chord root,
        /// so the wind audibly "plays the chord". One clip, synthesized
        /// once and cached like the bells. Fades with distance from Pip —
        /// only the lane you're near speaks; far lanes stay silent — and
        /// same-frame onsets from sibling lanes collapse into one swell.
        public void PlayGustSwell(Vector3 origin)
        {
            float volume = Falloff(origin, 28f);
            if (volume < 0.03f) return;
            if (Time.unscaledTime - lastGustSwellAt < GustSwellStackWindow) return;
            lastGustSwellAt = Time.unscaledTime;
            if (!SaveSystem.SoundOn) return;
            if (gustSwell == null)
                gustSwell = SfxSynth.NoiseSwell("sfx_gust", 2.2f, 0.22f);
            source.PlayOneShot(gustSwell, volume);
        }

        /// Legacy entry (full volume, no falloff) — kept for callers that
        /// have no world position.
        public void PlayGustSwell()
        {
            PlayGustSwell(GameBootstrap.Player != null
                ? GameBootstrap.Player.transform.position
                : Vector3.zero);
        }

        /// The Homecoming's see-saw: a low wooden groan as Pip's weight
        /// tips the plank, a lighter settle as it springs back level.
        /// Distance-faded like every other world sound, so a plank across
        /// the island stays quiet.
        public void PlaySeeSaw(Vector3 origin, bool tipping)
        {
            float volume = Falloff(origin, 22f);
            if (volume < 0.03f) return;
            PlayIfOn(tipping ? seesawCreak : seesawSpring, volume);
        }

        /// A hard reversal at speed: a scrape under the dust puff. Strength
        /// (0..1, from how fast Pip was moving) scales the volume.
        /// A footfall while running. `speed01` is the fraction of top speed:
        /// a walk is a whisper, a sprint is audible texture. The caller owns
        /// the gait clock, so this can never machine-gun.
        public void PlayFootstep(float speed01)
        {
            if (stepVariants == null) return;
            float level = Mathf.Lerp(0.16f, 0.42f, Mathf.Clamp01(speed01));
            PlayIfOn(stepVariants[Random.Range(0, stepVariants.Length)],
                level * Jitter(0.9f, 1.1f));
        }

        public void PlaySkid(float strength)
        {
            PlayIfOn(skid, (0.5f + 0.5f * Mathf.Clamp01(strength))
                * Jitter(0.92f, 1.08f));
        }

        /// Nim's giggle just before a gust blows — the invitation to step
        /// in. volumeScale fades with distance from Pip.
        public void PlayGiggle(float volumeScale)
        {
            if (volumeScale < 0.03f) return;
            PlayIfOn(giggle, volumeScale);
        }

        /// Gloomfang's own giggle when Pip jumps close by: lower and
        /// softer than Nim's gust-giggle — a big storm being discreet
        /// about his delight. volumeScale fades with distance from Pip.
        public void PlaySoftGiggle(float volumeScale)
        {
            if (volumeScale < 0.03f) return;
            PlayIfOn(softGiggle, volumeScale);
        }

        /// The giggle-raindrop landing: a breathy plop and a two-note
        /// bloom chime, very soft.
        public void PlayRaindropBloom(float volumeScale)
        {
            if (volumeScale < 0.03f) return;
            PlayIfOn(raindropBloom, volumeScale);
        }

        /// Melody gems: plays a soft music-box tone; cached per frequency.
        public void PlayNote(float frequency)
        {
            if (!SaveSystem.SoundOn) return;
            int key = Mathf.RoundToInt(frequency * 10f);
            if (!noteCache.TryGetValue(key, out AudioClip clip))
            {
                clip = SfxSynth.Note("note_" + key, frequency, 0.35f, 0.4f);
                noteCache[key] = clip;
            }
            // Music-box humanization: slight dynamics so a gem trail reads
            // as played, not sequenced.
            source.PlayOneShot(clip, Jitter(0.89f, 1.12f));
        }

        // ------------------------------------------------------------------
        // UI: tiny, polite, never louder than gameplay
        // ------------------------------------------------------------------

        public void PlayUIClick() { PlayIfOn(uiClick, Jitter(0.85f, 1.15f)); }
        public void PlayUIToggle(bool on) { PlayIfOn(on ? uiToggleOn : uiToggleOff); }
        public void PlayPanel(bool open) { PlayIfOn(open ? panelOpen : panelClose); }
        public void PlayPauseSound() { PlayIfOn(pauseBlip); }
        /// Called whenever the game clock resumes (unpause, hit-stop end).
        /// World rhythms that keep their own accumulator freeze with
        /// Time.timeScale while the music DSP clock does not, so they must
        /// re-anchor or they drift out of musical step permanently.
        public void OnClockResumed()
        {
            GustZone[] zones = Object.FindObjectsOfType<GustZone>();
            for (int i = 0; i < zones.Length; i++)
                zones[i].RelockToMusic();
        }

        public void PlayResumeSound() { PlayIfOn(resumeBlip); }
        public void PlayIntro() { PlayIfOn(introSwoosh); }
        public void PlayPageTurn() { PlayIfOn(pageTurn); }

        // ------------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------------

        /// Loudness by distance from Pip: 1 at his feet, 0 at `range`
        /// metres. World events call this so far-off things stay quiet.
        public static float Falloff(Vector3 position, float range)
        {
            if (GameBootstrap.Player == null) return 1f;
            float d = Vector3.Distance(position,
                GameBootstrap.Player.transform.position);
            float f = 1f - d / range;
            return Mathf.Clamp01(f) * Mathf.Clamp01(f);
        }

        void PlayIfOn(AudioClip clip, float volumeScale)
        {
            if (clip == null || !SaveSystem.SoundOn) return;
            source.PlayOneShot(clip, volumeScale);
        }

        void PlayIfOn(AudioClip clip)
        {
            if (clip != null && SaveSystem.SoundOn) source.PlayOneShot(clip);
        }
    }
}
