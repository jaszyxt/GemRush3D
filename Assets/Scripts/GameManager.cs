using UnityEngine;
using UnityEngine.EventSystems;

namespace GemRush
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Won,
        GameOver,
        Complete
    }

    /// Central state machine: menu -> playing -> won/game over -> next level,
    /// plus score, timer, lives, respawn point and progression.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }
        public int CurrentLevel { get; private set; }
        public LevelDefinition CurrentLevelDefinition { get; private set; }
        public int GemsCollected { get; private set; }
        public int GemsTotal { get; private set; }
        public int Lives { get; private set; }
        public float Elapsed { get; private set; }
        public Vector3 SpawnPoint { get; private set; }

        int starsEarned;
        bool newRecord;

        // Death hit-stop (RESEARCH.md guard design): one small state
        // machine owns it — see the Hit-stop region — so it can never
        // stack with the Escape pause or outlive a state transition.
        const float HitStopSeconds = 0.12f;

        // Child-safe death (difficulty pass, research: ages 6-8 begin
        // struggling from about a third into a progression, and failure
        // that erases progress is the main source of quitting). Five
        // lives, and running out REFILLS rather than ending the run: a
        // death costs a few seconds at the last checkpoint, never the
        // gems you already collected or the level you already climbed.
        public const int StartingLives = 5;
        bool hitStopActive;
        float hitStopResumeRealtime;

        UIManager ui;

        void Awake()
        {
            Instance = this;
            State = GameState.Menu;
            CurrentLevel = 0;
            Lives = StartingLives;
            ui = GetComponent<UIManager>();
        }

        void Start()
        {
            ShowMenu();
        }

        // A phone call or the notification shade can interrupt any run.
        // Auto-pausing (and clearing half-swallowed touch state) means the
        // resume is always the calm pause menu — never mid-air next to the
        // thing that was about to kill you.
        void OnApplicationPause(bool paused)
        {
            if (paused && State == GameState.Playing)
            {
                TouchControls.ResetInput();
                Haptics.StopRumble();
                PauseGame();
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && State == GameState.Playing)
            {
                TouchControls.ResetInput();
                Haptics.StopRumble();
                PauseGame();
            }
        }

        void OnApplicationQuit()
        {
            Haptics.StopRumble();
        }

        void Update()
        {
            TickHitStop();
            if (State == GameState.Playing)
            {
                Elapsed += Time.deltaTime;
                ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
            }

            // Rumble housekeeping: motors stop when a burst expires; the
            // gamepad's B walks the same back stack as Escape/Android back,
            // and Start toggles pause. Both mark the last-used device so
            // the menu hints follow the pad.
            Haptics.TickRumble();
            bool keyboardBack = Input.GetKeyDown(KeyCode.Escape);
            bool padBack = GamepadInput.BackPressed;
            if (keyboardBack) GamepadInput.MarkOther();
            if (padBack) GamepadInput.MarkGamepad();
            if (keyboardBack || padBack)
            {
                HandleBackNavigation();
            }

            if (GamepadInput.StartPressed)
            {
                GamepadInput.MarkGamepad();
                if (State == GameState.Playing) PauseGame();
                // Photo mode keeps State Paused while the pause panel is
                // hidden — Start there hands the pause menu back instead
                // of resuming gameplay under the orbit camera.
                else if (State == GameState.Paused && ui.PhotoModeOpen)
                    ui.ClosePhotoMode();
                else if (State == GameState.Paused) ResumeGame();
            }

            // The level list pages with the pad's shoulders; the on-screen
            // arrows stay pointer targets. Only while the bare menu shows —
            // overlays holding the menu must not flip pages underneath
            // themselves.
            if (State == GameState.Menu && !ui.SettingsOpen && !ui.QuitOpen &&
                !ui.AtlasOpen && !ui.PhotoModeOpen)
            {
                if (GamepadInput.PageLeftPressed)
                {
                    GamepadInput.MarkGamepad();
                    ui.FlipPage(-1);
                }
                else if (GamepadInput.PageRightPressed)
                {
                    GamepadInput.MarkGamepad();
                    ui.FlipPage(1);
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GamepadInput.MarkOther();
                // Enter confirms whatever screen is on top; while an overlay
                // (Settings, Atlas, quit dialog) holds the menu, it must
                // not fire the shortcuts underneath it. And when a menu
                // button already holds focus, the UI module's Submit has
                // confirmed it — the shortcut below is for the pre-focus
                // keyboard flow only, or PLAY would fire twice in one press.
                if (ui.SettingsOpen || ui.QuitOpen || ui.AtlasOpen ||
                    ui.PhotoModeOpen) return;
                EventSystem es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject != null) return;

                switch (State)
                {
                    case GameState.Menu:
                        PlayContinue();
                        break;
                    case GameState.Paused:
                        ResumeGame();
                        break;
                    case GameState.Won:
                        StartNextLevel();
                        break;
                    case GameState.GameOver:
                        PlayLevel(CurrentLevel);
                        break;
                    case GameState.Complete:
                        GoToMenu();
                        break;
                }
            }
        }

        // Android's back button arrives as KeyCode.Escape and desktop Escape
        // maps to the same action, so both follow one navigation stack: close
        // the quit-confirm dialog, then Settings, then pause/resume. Only
        // from the menu — and only on desktop — does back offer to quit, and
        // always through the confirmation dialog, never a hard quit. Won,
        // GameOver and Complete screens have no back action.
        void HandleBackNavigation()
        {
            if (ui.CloseQuitConfirm()) return; // Escape over the dialog = CANCEL
            if (ui.SettingsOpen)
            {
                ui.CloseSettings();
                return;
            }
            if (ui.AtlasOpen)
            {
                ui.CloseAtlas();
                return;
            }
            if (ui.PhotoModeOpen)
            {
                ui.ClosePhotoMode(); // B/Escape leaves the photo, stays paused
                return;
            }
            switch (State)
            {
                case GameState.Playing:
                    PauseGame();
                    break;
                case GameState.Paused:
                    ResumeGame();
                    break;
                case GameState.Menu:
                    if (IsDesktopPlatform())
                        ui.ShowQuitConfirm(delegate { Application.Quit(); });
                    break;
            }
        }

        // Platforms where Escape from the menu may offer to quit the app.
        // Android never quits from back — the OS owns app lifetime there.
        static bool IsDesktopPlatform()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.OSXPlayer
                || Application.platform == RuntimePlatform.LinuxPlayer
                || Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.WindowsEditor;
        }

        // ---------- Flow ----------

        public void PlayContinue()
        {
            PlayLevel(Mathf.Clamp(SaveSystem.UnlockedLevel, 0, LevelLibrary.Levels.Length - 1));
        }

        GhostRecorder ghostRecorder;

        public void PlayLevel(int index)
        {
            EndHitStop();
            ghostRecorder = GhostRecorder.Create(GameBootstrap.World.transform);
            ghostRecorder.Begin();
            Time.timeScale = 1f;
            ui.CloseQuitConfirm(); // the dialog lives over the menu only
            CurrentLevel = Mathf.Clamp(index, 0, LevelLibrary.Levels.Length - 1);
            GemsCollected = 0;
            Lives = StartingLives;
            Elapsed = 0f;
            State = GameState.Playing;

            GameBootstrap.BuildWorld(CurrentLevel);
            ui.ShowHUD();
            ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
            ui.ShowLevelIntro(CurrentLevel);
        }

        public void StartNextLevel()
        {
            PlayLevel(CurrentLevel + 1);
        }

        public void GoToMenu()
        {
            EndHitStop();
            Time.timeScale = 1f;
            ShowMenu();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            EndHitStop(); // pause owns timeScale from here, freeze or not
            State = GameState.Paused;
            Time.timeScale = 0f;
            Haptics.StopRumble();
            AudioManager.Instance.PlayPauseSound();
            ui.ShowPaused();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            EndHitStop();
            Time.timeScale = 1f;
            State = GameState.Playing;
            AudioManager.Instance.PlayResumeSound();
            ui.HidePaused();
        }

        void ShowMenu()
        {
            EndHitStop();
            Time.timeScale = 1f;
            Haptics.StopRumble();
            ui.CloseQuitConfirm(); // defensive: never rebuild a screen under it
            State = GameState.Menu;
            AudioManager.Instance.SetMood(SoundMood.Menu);
            // The menu is home: always show the start island behind it,
            // so Pip's shelf (and its fresh trophies) is what you see —
            // wherever you just came from. EnsureWorld skips the churn
            // when the home island is already standing.
            GameBootstrap.EnsureWorld(0);
            ui.ShowMenu();

            // The festival's thank-you: once the finale is cleared, the
            // aurora hangs over the menu forever. The world behind the
            // menu panel is the current level's, so the bands parent to it
            // (AuroraBand.Create is idempotent per world).
            if (SaveSystem.AuroraUnlocked && GameBootstrap.World != null)
                AuroraBand.Create(GameBootstrap.World.transform, 160f);
        }

        // ---------- Level events ----------

        public void ConfigureLevel(LevelDefinition level)
        {
            CurrentLevelDefinition = level;
            GemsTotal = level.GemCount;
            SpawnPoint = level.Spawn;
            AudioManager.Instance.SetMood(level.ResolveMood());
        }

        public void OnHeartCollected()
        {
            if (State != GameState.Playing) return;
            // A heart tops you up above the starting five: the buffer is
            // there to be spent, and collecting one should feel like it.
            Lives = Mathf.Min(Lives + 1, StartingLives + 3);
            ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
        }

        public void OnGemCollected(bool playPickupSound = true,
            Vector3 gemPosition = default)
        {
            if (State != GameState.Playing) return;
            GemsCollected++;
            if (playPickupSound) AudioManager.Instance.PlayPickup();
            // Gloomfang loves watching his old job done well: collecting
            // near him earns a delighted spark.
            Gloomfang.OnGemCollectedNear(gemPosition);
        }

        public void SetSpawn(Vector3 position)
        {
            SpawnPoint = position;
        }

        /// fell: Pip went past the kill line (wind-rush death) rather than
        /// hitting a hazard (crack death).
        public void OnPlayerDied(bool fell)
        {
            if (State != GameState.Playing) return;
            BeginHitStop(); // hold the impact frame; the feedback plays out after
            Lives--;
            AudioManager.ResetPickupStreak();
            AudioManager.Instance.PlayDie(fell);
            Haptics.StopRideTexture(); // free the ride before the impact burst
            Haptics.Heavy();
            Haptics.GamepadBurst(0.85f, 0.55f, 0.45f);
            Fx.Burst(GameBootstrap.Player.transform.position,
                ArtLib.HazardRed * 1.5f, 26);
            GameBootstrap.CameraRig.Shake(0.35f, 0.25f);

            if (Lives <= 0)
            {
                // Never end the run. Running out refills the buffer and
                // puts Pip back at the checkpoint he already earned: for
                // a young player the cost of dying is a few seconds, and
                // the gems, the climb and the timer all survive. (This
                // branch used to end the level and wipe the run; the
                // GameOver screen remains for compatibility but play no
                // longer reaches it.)
                Lives = StartingLives;
                ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal,
                    Lives, Elapsed);
            }
            else if (Lives == 1) AudioManager.Instance.PlayLivesLow();

            GameBootstrap.Player.TeleportTo(SpawnPoint);
            GameBootstrap.CameraRig.SnapToTarget();
            AudioManager.Instance.RestartMusicAtTonic();
            ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
        }

        public void OnPlayerDied()
        {
            OnPlayerDied(false);
        }

        // ---------- Hit-stop ----------

        // ≤150 ms freeze on death, released on the UNSCALED clock so it
        // keeps ticking while the world is frozen — and every state
        // transition drops a pending freeze, so pause, respawn or the
        // game-over screen can never leave the game stuck at timeScale 0.

        void BeginHitStop()
        {
            // Only freeze live, unpaused gameplay: the pause menu owns
            // timeScale 0 and must not inherit (or extend) the freeze.
            if (Time.timeScale != 1f) return;
            hitStopActive = true;
            hitStopResumeRealtime = Time.realtimeSinceStartup + HitStopSeconds;
            Time.timeScale = 0f;
        }

        void TickHitStop()
        {
            if (!hitStopActive) return;
            if (Time.realtimeSinceStartup < hitStopResumeRealtime) return;
            hitStopActive = false;
            Time.timeScale = 1f;
        }

        // Transitions call this WITHOUT writing timeScale — they each set
        // it themselves, and restoring here could un-pause a paused game.
        void EndHitStop()
        {
            hitStopActive = false;
        }

        public void OnReachGoal()
        {
            if (State != GameState.Playing) return;

            gemsToStars();
            newRecord = SaveSystem.RecordResult(CurrentLevel, Elapsed, starsEarned);

            // A record run becomes tomorrow's ghost: only recordings that
            // beat the best are kept, so the ghost is always the pace.
            if (newRecord && ghostRecorder != null)
            {
                var path = ghostRecorder.Finish();
                if (path != null)
                    GhostStore.Save(CurrentLevelDefinition.Name,
                        GhostStore.Encode(path));
            }
            if (ghostRecorder != null) ghostRecorder.Stop();
            SaveSystem.UnlockLevel(Mathf.Min(CurrentLevel + 1,
                LevelLibrary.Levels.Length - 1));

            AudioManager.Instance.PlayWin();
            Haptics.Fanfare();
            Haptics.GamepadBurst(0.3f, 0.7f, 0.35f);
            Fx.Burst(GameBootstrap.Player.transform.position,
                ArtLib.PortalCyan * 1.5f, 40);

            // Sky Garden: victory blooms every bud on the course, and the
            // bloom wave sings a rising run under the fanfare.
            if (GameBootstrap.World != null && CurrentLevelDefinition != null)
            {
                if (CurrentLevelDefinition.SkyGarden)
                    AudioManager.Instance.PlayBloom();
                BloomFlower.Trigger(GameBootstrap.World.transform,
                    CurrentLevelDefinition.Portal);

                // The Long Winter: every melted gate refreezes into a
                // crystal shard, sweeping out from the portal — the melted
                // paths become one map of everywhere Pip walked.
                if (CurrentLevelDefinition.IceGates.Count > 0)
                {
                    AudioManager.Instance.PlayCrystal();
                    IceGate.TriggerCrystalMap(GameBootstrap.World.transform,
                        CurrentLevelDefinition.Portal);
                }

                // The Aurora Festival: the realm's concert. Clearing the
                // finale lights the permanent aurora over the menu.
                if (CurrentLevelDefinition.AuroraFestival)
                    AudioManager.Instance.PlayConcert();
                if (CurrentLevelDefinition.AuroraUnlock)
                    SaveSystem.AuroraUnlocked = true;
            }

            bool lastLevel = CurrentLevel >= LevelLibrary.Levels.Length - 1;
            if (lastLevel)
            {
                State = GameState.Complete;
                AudioManager.Instance.PlayComplete();
                ui.ShowComplete(SaveSystem.TotalStars(LevelLibrary.Levels.Length),
                    LevelLibrary.Levels.Length * 3);
            }
            else
            {
                State = GameState.Won;
                ui.ShowWin(CurrentLevel, starsEarned, Elapsed,
                    SaveSystem.BestTime(CurrentLevel), newRecord,
                    GemsCollected, GemsTotal);
            }
        }

        void gemsToStars()
        {
            if (GemsCollected >= GemsTotal) starsEarned = 3;
            else if (GemsCollected * 2 >= GemsTotal) starsEarned = 2;
            else starsEarned = 1;
        }
    }
}
