using UnityEngine;

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

        UIManager ui;

        void Awake()
        {
            Instance = this;
            State = GameState.Menu;
            CurrentLevel = 0;
            Lives = 3;
            ui = GetComponent<UIManager>();
        }

        void Start()
        {
            ShowMenu();
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                Elapsed += Time.deltaTime;
                ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing) PauseGame();
                else if (State == GameState.Paused) ResumeGame();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
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

        // ---------- Flow ----------

        public void PlayContinue()
        {
            PlayLevel(Mathf.Clamp(SaveSystem.UnlockedLevel, 0, LevelLibrary.Levels.Length - 1));
        }

        public void PlayLevel(int index)
        {
            Time.timeScale = 1f;
            CurrentLevel = Mathf.Clamp(index, 0, LevelLibrary.Levels.Length - 1);
            GemsCollected = 0;
            Lives = 3;
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
            Time.timeScale = 1f;
            ShowMenu();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            ui.ShowPaused();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            State = GameState.Playing;
            ui.HidePaused();
        }

        void ShowMenu()
        {
            Time.timeScale = 1f;
            State = GameState.Menu;
            ui.ShowMenu();
        }

        // ---------- Level events ----------

        public void ConfigureLevel(LevelDefinition level)
        {
            CurrentLevelDefinition = level;
            GemsTotal = level.GemCount;
            SpawnPoint = level.Spawn;
            AudioManager.Instance.SetMood(level.DarkRealm);
        }

        public void OnHeartCollected()
        {
            if (State != GameState.Playing) return;
            Lives = Mathf.Min(Lives + 1, 5);
            ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
        }

        public void OnGemCollected(bool playPickupSound = true)
        {
            if (State != GameState.Playing) return;
            GemsCollected++;
            if (playPickupSound) AudioManager.Instance.PlayPickup();
        }

        public void SetSpawn(Vector3 position)
        {
            SpawnPoint = position;
        }

        public void OnPlayerDied()
        {
            if (State != GameState.Playing) return;
            Lives--;
            AudioManager.Instance.PlayDie();
            Haptics.Heavy();
            Fx.Burst(GameBootstrap.Player.transform.position,
                ArtLib.HazardRed * 1.5f, 26);
            GameBootstrap.CameraRig.Shake(0.35f, 0.25f);

            if (Lives <= 0)
            {
                State = GameState.GameOver;
                ui.ShowGameOver();
            }
            else
            {
                GameBootstrap.Player.TeleportTo(SpawnPoint);
                GameBootstrap.CameraRig.SnapToTarget();
                ui.UpdateHUD(CurrentLevel, GemsCollected, GemsTotal, Lives, Elapsed);
            }
        }

        public void OnReachGoal()
        {
            if (State != GameState.Playing) return;

            gemsToStars();
            newRecord = SaveSystem.RecordResult(CurrentLevel, Elapsed, starsEarned);
            SaveSystem.UnlockLevel(Mathf.Min(CurrentLevel + 1,
                LevelLibrary.Levels.Length - 1));

            AudioManager.Instance.PlayWin();
            Haptics.Heavy();
            Fx.Burst(GameBootstrap.Player.transform.position,
                ArtLib.PortalCyan * 1.5f, 40);

            // Sky Garden: victory blooms every bud on the course.
            if (GameBootstrap.World != null && CurrentLevelDefinition != null)
                BloomFlower.Trigger(GameBootstrap.World.transform,
                    CurrentLevelDefinition.Portal);

            bool lastLevel = CurrentLevel >= LevelLibrary.Levels.Length - 1;
            if (lastLevel)
            {
                State = GameState.Complete;
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
