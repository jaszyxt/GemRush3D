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
        readonly Dictionary<int, AudioClip> noteCache =
            new Dictionary<int, AudioClip>();

        // ---- Music / mood ----
        static readonly Dictionary<int, AudioClip> moodLoops =
            new Dictionary<int, AudioClip>();
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
            Instance = this;
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

            // Pip's movement: three baked pitch variants so hop chains
            // never sound like a sampled loop.
            jumpVariants = new AudioClip[]
            {
                SfxSynth.Jump("sfx_jump_a", 0.96f),
                SfxSynth.Jump("sfx_jump_b", 1f),
                SfxSynth.Jump("sfx_jump_c", 1.05f)
            };
            land = SfxSynth.Land("sfx_land");
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
        }

        // ------------------------------------------------------------------
        // Mood / music
        // ------------------------------------------------------------------

        /// Called when a level is built (or the menu is shown): switch the
        /// pad loop and ambience bed to the level's mood. Clips are
        /// synthesized lazily, once per mood, and cached statically.
        public void SetMood(SoundMood mood)
        {
            if (moodInitialized && activeMood == mood) return;
            activeMood = mood;
            moodInitialized = true;

            musicLoopLength = MusicSynth.MoodLoopLength(mood);
            if (!moodLoops.TryGetValue((int)mood, out AudioClip loop))
            {
                loop = MusicSynth.MoodLoop("music_" + mood, mood, 0.13f);
                moodLoops[(int)mood] = loop;
            }
            musicSource.clip = loop;
            if (!moodBedLoops.TryGetValue((int)mood, out AudioClip bed))
            {
                bed = MusicSynth.MoodLoop("music_bed_" + mood, mood, 0.13f,
                    withMotif: false);
                moodBedLoops[(int)mood] = bed;
            }
            bedSource.clip = bed;

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
                moodWindBase = amb == MusicSynth.AmbienceKind.Rumble ? 0.5f
                    : amb == MusicSynth.AmbienceKind.WindHigh ? 0.3f : 0.42f;
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
                    return MusicSynth.WindLoop("amb_wind", 0.16f, 500f, 0.25f, 77);
                case MusicSynth.AmbienceKind.WindHigh:
                    return MusicSynth.WindLoop("amb_wind_high", 0.09f, 950f, 0.5f, 78);
                case MusicSynth.AmbienceKind.Rumble:
                    return MusicSynth.RumbleLoop("amb_rumble", 0.2f);
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

        // Death/win ducking: snap the pad down to a fraction of its level,
        // then ease it back on unscaled time — the sting reads in near
        // silence, and the pad breathes back in afterwards. Voice lines
        // duck deeper and longer via DuckFor (while narration plays).
        const float MusicVolume = 0.55f;
        const float DuckFraction = 0.35f;
        const float DuckRestoreSeconds = 2.5f;
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

        bool MelodyWanted
        {
            get
            {
                if (GameManager.Instance == null) return true;
                switch (GameManager.Instance.State)
                {
                    case GameState.GameOver:
                        return false;
                    case GameState.Playing:
                    case GameState.Paused:
                        return GameManager.Instance.Lives >= 2;
                    default: // menu, won, complete: celebrate
                        return true;
                }
            }
        }

        void Update()
        {
            // One volume pass per frame: duck factor (death/win sting)
            // times the melody crossfade, applied to both layers.
            float duck = 1f;
            if (duckTimer > 0f)
            {
                duckTimer = Mathf.Max(0f, duckTimer - Time.unscaledDeltaTime);
                float restore = Mathf.SmoothStep(0f, 1f,
                    1f - duckTimer / DuckRestoreSeconds);
                duck = Mathf.Lerp(duckFraction, 1f, restore);
            }
            float wanted = MelodyWanted ? 1f : 0f;
            melodyFactor = Mathf.MoveTowards(melodyFactor, wanted,
                Time.unscaledDeltaTime / MelodyFadeSeconds);
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
            }

            bool shouldPlay = lastSoundOn &&
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
            if (!SaveSystem.SoundOn) return;
            if (windSource.clip == null) return;
            windPulseUntil = Time.unscaledTime + WindPulseHoldSeconds;
        }

        void UpdateWind()
        {
            if (windSource.clip == null || !SaveSystem.SoundOn)
            {
                if (windSource.volume > 0f && !windSource.isPlaying) return;
                windSource.volume = 0f;
                if (windSource.isPlaying) windSource.Stop();
                return;
            }
            bool pulsed = Time.unscaledTime < windPulseUntil;
            float target = Mathf.Clamp01(
                moodWindBase + (pulsed ? WindPulseLevel : 0f));
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

        const float HumMaxVolume = 0.16f;
        const float HumFadeSpeed = 1.4f;
        float humTarget;

        public void SetPortalProximity(float proximity01)
        {
            humTarget = Mathf.Clamp01(proximity01);
        }

        void UpdateHum()
        {
            bool allowed = lastSoundOn && humTarget > 0.001f;
            if (allowed && humSource.clip == null)
                humSource.clip = MusicSynth.HumLoop("amb_portal_hum", 1f);
            float target = allowed ? humTarget * HumMaxVolume : 0f;
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

        /// One full pad cycle: four chords; gust periods divide it so
        /// onsets land on chord boundaries. Default 8.8 s (4 x 2.2) —
        /// every gust-hosting mood keeps that length. Load-bearing — do
        /// not change without re-checking the gust math.
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
            PlayIfOn(jumpVariants[Random.Range(0, jumpVariants.Length)]);
        }

        /// Landing impact, from soft walk-offs (2.5) to terminal-velocity
        /// plunges (28): the thud scales, tiny hops stay silent.
        public void PlayLand(float impactSpeed)
        {
            if (impactSpeed < 2.5f) return;
            float strength = Mathf.Clamp01((impactSpeed - 2.5f) / 14f);
            PlayIfOn(land, 0.45f + 0.55f * strength);
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
            source.PlayOneShot(clip);
        }

        /// Cached bright triad for the every-10th-gem milestone.
        public void PlayMilestoneChime()
        {
            if (!noteCache.TryGetValue(9500, out AudioClip clip))
            {
                clip = SfxSynth.MilestoneChime("sfx_milestone");
                noteCache[9500] = clip;
            }
            source.PlayOneShot(clip);
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
            duckFraction = DuckFraction;
            duckTimer = DuckRestoreSeconds;
            musicSource.volume = MusicVolume * DuckFraction;
        }

        /// While a voice line plays, duck deeper than a sting and hold for
        /// the line's whole length (plus its breath). Extends an existing
        /// duck rather than restarting it; the restore ramp is unchanged.
        public void DuckFor(float seconds, float fraction)
        {
            duckFraction = Mathf.Clamp01(fraction);
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
            source.PlayOneShot(clip);
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
            source.PlayOneShot(clip);
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
            source.PlayOneShot(clip);
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
            source.PlayOneShot(clip);
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
            source.PlayOneShot(clip);
        }

        /// The Aurora Festival: the realm's concert under the win fanfare.
        public void PlayConcert()
        {
            if (!SaveSystem.SoundOn) return;
            if (!noteCache.TryGetValue(9600, out AudioClip clip))
            {
                clip = SfxSynth.FestivalConcert("sfx_concert");
                noteCache[9600] = clip;
            }
            source.PlayOneShot(clip);
        }

        public void PlayBounce()
        {
            PlayIfOn(bounce);
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
                    bellFrequencies[safe], Mathf.Max(4f, toneSeconds + 1.5f), 0.42f);
                noteCache[key] = clip;
            }
            source.PlayOneShot(clip);
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
                gustSwell = SfxSynth.NoiseSwell("sfx_gust", 2.2f, 0.16f);
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
            source.PlayOneShot(clip);
        }

        // ------------------------------------------------------------------
        // UI: tiny, polite, never louder than gameplay
        // ------------------------------------------------------------------

        public void PlayUIClick() { PlayIfOn(uiClick); }
        public void PlayUIToggle(bool on) { PlayIfOn(on ? uiToggleOn : uiToggleOff); }
        public void PlayPanel(bool open) { PlayIfOn(open ? panelOpen : panelClose); }
        public void PlayPauseSound() { PlayIfOn(pauseBlip); }
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
