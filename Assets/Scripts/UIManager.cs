using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GemRush
{
    /// Builds every screen in code using the built-in uGUI system: menu with
    /// level select, HUD, pause, settings, win (with stars), game over and a
    /// completion screen. No prefabs, no imported assets.
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        Font font;
        GameObject menuPanel;
        GameObject hudPanel;
        GameObject winPanel;
        GameObject overPanel;
        GameObject completePanel;
        GameObject settingsPanel;
        GameObject pausePanel;

        Text hudLevel;
        Text hudGems;
        Text hudTime;
        Text hudLives;
        Text winStats;
        Text winStory;
        Text completeStats;
        RectTransform titleRect;
        Image[] winStars;
        Text[] levelButtonTexts;
        Button[] levelButtons;
        Text[] settingsLabels; // 0 sound, 1 haptics, 2 shadows
        GameObject introPanel;
        Text introTitle;
        Text introMission;
        float introTimer;
        GameObject storyToastPanel;
        Text storyToastText;
        float storyToastTimer;
        Text completeSub;
        Text completeStory;
        Button completeNext;
        string[] epiloguePages;
        int epiloguePage;
        Text menuQuote;
        Text visitRecap;
        readonly Color onColor = new Color(0.16f, 0.55f, 0.32f);
        readonly Color offColor = new Color(0.45f, 0.25f, 0.22f);
        readonly Color lockedColor = new Color(0.35f, 0.37f, 0.42f);
        readonly Color starGold = new Color(1f, 0.84f, 0.25f);
        readonly Color starDim = new Color(0.3f, 0.3f, 0.34f);

        void Awake()
        {
            Instance = this;
            font = LoadFont();

            GameObject canvasGo = new GameObject("UICanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(transform, false);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            BuildMenu(canvasGo.transform);
            BuildHUD(canvasGo.transform);
            BuildWin(canvasGo.transform);
            BuildGameOver(canvasGo.transform);
            BuildComplete(canvasGo.transform);
            BuildSettings(canvasGo.transform);
            BuildPause(canvasGo.transform);
            BuildIntro(canvasGo.transform);
            BuildStoryToast(canvasGo.transform);

            HideAll();
        }

        void Update()
        {
            if (titleRect != null && menuPanel != null && menuPanel.activeSelf)
            {
                titleRect.anchoredPosition =
                    new Vector2(0f, Mathf.Sin(Time.unscaledTime * 1.7f) * 9f);
            }
            if (introPanel != null && introPanel.activeSelf)
            {
                introTimer -= Time.unscaledDeltaTime;
                if (introTimer <= 0f) introPanel.SetActive(false);
            }
            if (storyToastPanel != null && storyToastPanel.activeSelf)
            {
                storyToastTimer -= Time.unscaledDeltaTime;
                if (storyToastTimer <= 0f) storyToastPanel.SetActive(false);
            }
        }

        static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception) { f = null; }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (System.Exception) { f = null; }
            }
            return f;
        }

        // ---------- Screens ----------

        void BuildMenu(Transform canvas)
        {
            menuPanel = MakePanel(canvas, "MenuPanel", new Color(0f, 0f, 0.05f, 0.55f));

            Text title = MakeText(menuPanel.transform, "Title", "GEM RUSH 3D", 92,
                new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.62f), new Vector2(1f, 0.82f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;
            titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleRect.sizeDelta = new Vector2(1000f, 130f);
            titleRect.anchoredPosition = Vector2.zero;

            MakeText(menuPanel.transform, "Tagline",
                "Pip vs. Gloomfang — a very small hero, a very large storm",
                28, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.54f), new Vector2(1f, 0.62f), 0f, 0f, 0f, 0f);

            MakeButton(menuPanel.transform, "PLAY",
                new Vector2(0.5f, 0.46f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.PlayContinue(); });

            // Fifteen levels fit a 5x3 grid; smaller counts use 3 per row.
            int count = LevelLibrary.Levels.Length;
            levelButtons = new Button[count];
            levelButtonTexts = new Text[count];
            int perRow = count <= 4 ? count : (count > 12 ? 5 : 3);
            int rows = (count + perRow - 1) / perRow;
            for (int i = 0; i < count; i++)
            {
                int index = i; // capture for the delegate
                int row = i / perRow;
                // Center whichever buttons actually landed on the last row.
                int inRow = (row == rows - 1) ? (count - row * perRow) : perRow;
                int col = i - row * perRow;
                float x = 0.5f - ((inRow - 1) / 2f - col) * 0.185f;
                float y = 0.365f - row * 0.075f;
                Button b = MakeButton(menuPanel.transform, "LEVEL " + (i + 1),
                    new Vector2(x, y), new Vector2(0f, 0f),
                    new Vector2(200f, 60f),
                    delegate { GameManager.Instance.PlayLevel(index); });
                levelButtons[i] = b;
                levelButtonTexts[i] = b.GetComponentInChildren<Text>();
                if (levelButtonTexts[i] != null) levelButtonTexts[i].fontSize = 21;
            }

            MakeButton(menuPanel.transform, "SETTINGS",
                new Vector2(0.925f, 0.965f), new Vector2(0f, 0f),
                new Vector2(190f, 54f), delegate { ShowSettings(); });

            MakeText(menuPanel.transform, "Instructions",
                "Move: WASD / Arrows    Jump: Space    (ENTER works too)\n" +
                "Collect gems for stars, dodge the red spinners, reach the portal!\n" +
                "On touch: drag left side to move, tap JUMP.",
                20, new Color(0.85f, 0.87f, 0.92f), TextAnchor.LowerLeft,
                new Vector2(0.03f, 0.02f), new Vector2(0.5f, 0.115f), 0f, 0f, 0f, 0f);

            menuQuote = MakeText(menuPanel.transform, "MenuQuote", "", 20,
                new Color(0.72f, 0.78f, 0.88f), TextAnchor.LowerRight,
                new Vector2(0.52f, 0.02f), new Vector2(0.97f, 0.115f), 0f, 0f, 0f, 0f);
            menuQuote.fontStyle = FontStyle.Italic;

            visitRecap = MakeText(menuPanel.transform, "VisitRecap", "", 19,
                new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft,
                new Vector2(0.03f, 0.90f), new Vector2(0.60f, 0.95f), 12f, 0f, 0f, 0f);
        }

        void RefreshMenu()
        {
            if (levelButtons == null) return;
            int dailyIndex = DailyGem.TodayIndex();
            bool giftTaken = DailyGem.GiftAlreadyCollectedToday();
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool unlocked = i <= SaveSystem.UnlockedLevel;
                levelButtons[i].interactable = unlocked;
                Image img = levelButtons[i].targetGraphic as Image;
                bool isDaily = unlocked && i == dailyIndex && !giftTaken;
                if (img != null)
                {
                    if (isDaily)
                        img.color = new Color(1f, 0.80f, 0.2f); // golden glow
                    else img.color = unlocked ? onColor : lockedColor;
                }
                if (levelButtonTexts[i] != null)
                {
                    if (unlocked)
                    {
                        levelButtonTexts[i].text = "LEVEL " + (i + 1) +
                            "\nStars " + SaveSystem.Stars(i) + "/3" +
                            (isDaily ? "  · DAILY" : "");
                    }
                    else
                    {
                        levelButtonTexts[i].text = "LEVEL " + (i + 1) + "\nLOCKED";
                    }
                }
            }
        }

        void BuildHUD(Transform canvas)
        {
            hudPanel = MakePanel(canvas, "HudPanel", new Color(0f, 0f, 0f, 0f));

            hudLevel = MakeText(hudPanel.transform, "Level", "", 26,
                new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0.93f), new Vector2(0.22f, 1f), 24f, 4f, 4f, 2f);

            hudGems = MakeText(hudPanel.transform, "Gems", "Gems  0 / 0", 28,
                Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.22f, 0.93f), new Vector2(0.48f, 1f), 12f, 4f, 4f, 2f);

            hudTime = MakeText(hudPanel.transform, "Time", "0:00.0", 28,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.48f, 0.93f), new Vector2(0.62f, 1f), 4f, 4f, 4f, 2f);

            Button pause = MakeButton(hudPanel.transform, "II",
                new Vector2(0.645f, 0.965f), new Vector2(0f, 0f),
                new Vector2(58f, 58f), delegate { GameManager.Instance.PauseGame(); });
            Text pauseLabel = pause.GetComponentInChildren<Text>();
            if (pauseLabel != null) pauseLabel.fontSize = 26;

            hudLives = MakeText(hudPanel.transform, "Lives", "Lives  3", 28,
                new Color(1f, 0.5f, 0.45f), TextAnchor.MiddleRight,
                new Vector2(0.68f, 0.93f), new Vector2(1f, 1f), 8f, 4f, 24f, 2f);

            // Virtual joystick + jump button; only appears on touch devices.
            TouchControls.Create(hudPanel.transform, font);
        }

        void BuildWin(Transform canvas)
        {
            winPanel = MakePanel(canvas, "WinPanel", new Color(0f, 0.1f, 0.05f, 0.65f));

            Text title = MakeText(winPanel.transform, "Title", "LEVEL COMPLETE!", 76,
                new Color(0.45f, 1f, 0.55f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.60f), new Vector2(1f, 0.76f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            winStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject dot = new GameObject("Star" + i);
                dot.transform.SetParent(winPanel.transform, false);
                Image img = dot.AddComponent<Image>();
                img.sprite = Fx.CircleSprite();
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0.5f + (i - 1) * 0.09f, 0.5f);
                rt.anchorMax = rt.anchorMin;
                rt.sizeDelta = new Vector2(76f, 76f);
                winStars[i] = img;
            }

            winStats = MakeText(winPanel.transform, "Stats", "", 30,
                Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 0.36f), new Vector2(1f, 0.46f), 0f, 0f, 0f, 0f);

            winStory = MakeText(winPanel.transform, "Story", "", 26,
                new Color(0.75f, 0.82f, 0.95f), TextAnchor.UpperCenter,
                new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.38f), 0f, 0f, 0f, 0f);
            winStory.fontStyle = FontStyle.Italic;

            MakeButton(winPanel.transform, "NEXT  LEVEL",
                new Vector2(0.5f, 0.26f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.StartNextLevel(); });

            MakeButton(winPanel.transform, "REPLAY",
                new Vector2(0.5f - 0.14f, 0.145f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            MakeButton(winPanel.transform, "MENU",
                new Vector2(0.5f + 0.14f, 0.145f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });
        }

        void BuildGameOver(Transform canvas)
        {
            overPanel = MakePanel(canvas, "GameOverPanel", new Color(0.15f, 0f, 0f, 0.7f));

            Text title = MakeText(overPanel.transform, "Title", "GAME OVER", 84,
                new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.56f), new Vector2(1f, 0.76f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            MakeText(overPanel.transform, "Sub",
                "Every legend takes a nap sometimes. One more try, Pip —\nGloomfang isn't getting less gloomy on his own.", 28,
                new Color(0.9f, 0.85f, 0.85f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.40f), new Vector2(1f, 0.54f), 0f, 0f, 0f, 0f);

            MakeButton(overPanel.transform, "TRY  AGAIN",
                new Vector2(0.5f, 0.28f), new Vector2(0f, 0f),
                new Vector2(360f, 84f),
                delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            MakeButton(overPanel.transform, "MENU",
                new Vector2(0.5f, 0.155f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });
        }

        void BuildComplete(Transform canvas)
        {
            completePanel = MakePanel(canvas, "CompletePanel",
                new Color(0.02f, 0.08f, 0.14f, 0.72f));

            Text title = MakeText(completePanel.transform, "Title",
                "YOU BEAT GEM RUSH 3D!", 72,
                new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.58f), new Vector2(1f, 0.76f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            completeStats = MakeText(completePanel.transform, "Stats", "", 34,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.40f), new Vector2(1f, 0.54f), 0f, 0f, 0f, 0f);

            MakeText(completePanel.transform, "Sub",
                "The beacon roars back to life. The clouds part. The realm exhales.\n" +
                "The sky is bright, Pip is home — and Gloomfang is thinking\nvery hard about his choices.", 24,
                new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.26f), new Vector2(1f, 0.40f), 0f, 0f, 0f, 0f);
            completeSub = completePanel.transform.Find("Sub")
                .GetComponent<Text>();

            // Epilogue: story pages shown before the stats, advanced with NEXT.
            completeStory = MakeText(completePanel.transform, "Epilogue", "", 26,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.56f), 0f, 0f, 0f, 0f);
            completeNext = MakeButton(completePanel.transform, "NEXT",
                new Vector2(0.79f, 0.18f), new Vector2(0f, 0f),
                new Vector2(240f, 78f), delegate { AdvanceEpilogue(); });

            MakeButton(completePanel.transform, "MENU",
                new Vector2(0.5f, 0.18f), new Vector2(0f, 0f),
                new Vector2(300f, 78f), delegate { GameManager.Instance.GoToMenu(); });
        }

        void AdvanceEpilogue()
        {
            epiloguePage++;
            if (epiloguePages == null || epiloguePage >= epiloguePages.Length)
            {
                // Pages done: reveal the stats layout.
                if (completeStory != null) completeStory.gameObject.SetActive(false);
                if (completeNext != null) completeNext.gameObject.SetActive(false);
                if (completeStats != null) completeStats.gameObject.SetActive(true);
                if (completeSub != null) completeSub.gameObject.SetActive(true);
            }
            else if (completeStory != null)
            {
                completeStory.text = epiloguePages[epiloguePage];
            }
        }

        void BuildSettings(Transform canvas)
        {
            settingsPanel = MakePanel(canvas, "SettingsPanel",
                new Color(0f, 0f, 0.05f, 0.8f));

            Text title = MakeText(settingsPanel.transform, "Title", "SETTINGS", 64,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.66f), new Vector2(1f, 0.78f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            settingsLabels = new Text[3];
            string[] names = new string[] { "Sound", "Haptics", "Shadows" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Button b = MakeButton(settingsPanel.transform, names[i],
                    new Vector2(0.5f, 0.54f - i * 0.1f), new Vector2(0f, 0f),
                    new Vector2(480f, 68f), delegate { ToggleSetting(index); });
                settingsLabels[i] = b.GetComponentInChildren<Text>();
            }

            MakeButton(settingsPanel.transform, "BACK",
                new Vector2(0.5f, 0.17f), new Vector2(0f, 0f),
                new Vector2(260f, 64f), delegate { settingsPanel.SetActive(false); });

            settingsPanel.SetActive(false);
        }

        void ShowSettings()
        {
            RefreshSettings();
            settingsPanel.SetActive(true);
        }

        void RefreshSettings()
        {
            if (settingsLabels == null) return;
            ApplyLabel(settingsLabels[0], "Sound", SaveSystem.SoundOn);
            ApplyLabel(settingsLabels[1], "Haptics", SaveSystem.HapticsOn);
            ApplyLabel(settingsLabels[2], "Shadows", SaveSystem.ShadowsOn);
        }

        void ApplyLabel(Text label, string name, bool on)
        {
            if (label == null) return;
            label.text = name + ":  " + (on ? "ON" : "OFF");
            Image img = label.transform.parent.GetComponent<Image>();
            if (img != null) img.color = on ? onColor : offColor;
        }

        void ToggleSetting(int index)
        {
            if (index == 0) SaveSystem.SoundOn = !SaveSystem.SoundOn;
            else if (index == 1) SaveSystem.HapticsOn = !SaveSystem.HapticsOn;
            else if (index == 2)
            {
                SaveSystem.ShadowsOn = !SaveSystem.ShadowsOn;
                QualitySettings.shadows = SaveSystem.ShadowsOn
                    ? ShadowQuality.All : ShadowQuality.Disable;
            }
            RefreshSettings();
        }

        void BuildPause(Transform canvas)
        {
            pausePanel = MakePanel(canvas, "PausePanel", new Color(0f, 0f, 0f, 0.6f));

            Text title = MakeText(pausePanel.transform, "Title", "PAUSED", 72,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.56f), new Vector2(1f, 0.72f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            MakeButton(pausePanel.transform, "RESUME",
                new Vector2(0.5f, 0.40f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.ResumeGame(); });

            MakeButton(pausePanel.transform, "RESTART LEVEL",
                new Vector2(0.5f, 0.28f), new Vector2(0f, 0f),
                new Vector2(360f, 68f),
                delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            MakeButton(pausePanel.transform, "MENU",
                new Vector2(0.5f, 0.17f), new Vector2(0f, 0f),
                new Vector2(360f, 68f), delegate { GameManager.Instance.GoToMenu(); });

            pausePanel.SetActive(false);
        }

        /// Mission briefing shown for a few seconds when a level starts.
        /// Non-blocking: its image never raycasts, so touch controls keep
        /// working underneath.
        void BuildIntro(Transform canvas)
        {
            introPanel = MakePanel(canvas, "IntroPanel", new Color(0f, 0f, 0f, 0.45f));
            introPanel.GetComponent<Image>().raycastTarget = false;

            introTitle = MakeText(introPanel.transform, "IntroTitle", "", 56,
                new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.56f), new Vector2(1f, 0.70f), 0f, 0f, 0f, 0f);
            introTitle.fontStyle = FontStyle.Bold;

            introMission = MakeText(introPanel.transform, "IntroMission", "", 30,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.55f), 0f, 0f, 0f, 0f);

            introPanel.SetActive(false);
        }

        public void ShowLevelIntro(int levelIndex)
        {
            if (introPanel == null) return;
            LevelDefinition def = LevelLibrary.Levels[
                Mathf.Clamp(levelIndex, 0, LevelLibrary.Levels.Length - 1)];
            if (introTitle != null)
                introTitle.text = "LEVEL " + (levelIndex + 1) + "  —  " + def.Name.ToUpper();
            if (introMission != null)
                introMission.text = def.Mission;
            introTimer = 3.5f;
            introPanel.SetActive(true);
        }

        /// Story beat band at the bottom of the screen, shown when a
        /// checkpoint is touched. Non-blocking, like the intro card.
        void BuildStoryToast(Transform canvas)
        {
            storyToastPanel = MakePanel(canvas, "StoryToast", new Color(0f, 0f, 0f, 0.6f));
            storyToastPanel.GetComponent<Image>().raycastTarget = false;
            RectTransform rt = storyToastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.13f, 0.055f);
            rt.anchorMax = new Vector2(0.87f, 0.125f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            storyToastText = MakeText(storyToastPanel.transform, "Beat", "", 24,
                new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, 14f, 4f, 14f, 4f);
            storyToastText.fontStyle = FontStyle.Italic;

            storyToastPanel.SetActive(false);
        }

        public void ShowStoryToast(string line)
        {
            if (storyToastPanel == null || string.IsNullOrEmpty(line)) return;
            if (storyToastText != null) storyToastText.text = line;
            storyToastTimer = 4f;
            storyToastPanel.SetActive(true);
        }

        // ---------- Public API ----------

        public void ShowMenu()
        {
            HideAll();
            RefreshMenu();
            if (menuQuote != null) menuQuote.text = Story.MenuQuote();
            UpdateVisitRecap();
            menuPanel.SetActive(true);
        }

        /// Session-recap line: what changed since the app was opened. The
        /// tone stays warm — progress framed as the world noticing you.
        void UpdateVisitRecap()
        {
            if (visitRecap == null) return;
            int count = LevelLibrary.Levels.Length;
            int stars = SaveSystem.TotalStars(count) - SaveSystem.VisitStartStars;
            int medals = SaveSystem.TotalMedals(count) - SaveSystem.VisitStartMedals;
            int gifts = SaveSystem.Gifts;
            if (stars > 0 || medals > 0)
                visitRecap.text = string.Format(
                    "Since you arrived: +{0} stars, +{1} medal{2}.\nThe garden noticed.",
                    stars, medals, medals == 1 ? "" : "s");
            else if (gifts > 0)
                visitRecap.text = "Gloomfang's Gifts: " + gifts +
                    (DailyGem.GiftAlreadyCollectedToday()
                        ? "  ·  today's gift is yours"
                        : "  ·  today's gift is still out there");
            else
                visitRecap.text = "";
        }

        public void ShowHUD()
        {
            HideAll();
            hudPanel.SetActive(true);
        }

        public void ShowPaused()
        {
            pausePanel.SetActive(true);
        }

        public void HidePaused()
        {
            pausePanel.SetActive(false);
        }

        public void ShowWin(int level, int stars, float time, float best,
            bool newRecord, int gems, int total)
        {
            HideAll();
            if (winStars != null)
            {
                for (int i = 0; i < winStars.Length; i++)
                    winStars[i].color = i < stars ? starGold : starDim;
            }
            if (winStats != null)
            {
                string bestText;
                if (best < 0f) bestText = "First clear!";
                else bestText = "Best " + FormatTime(best) + (newRecord ? "  (NEW!)" : "");
                string medal = LevelLibrary.Levels[
                    Mathf.Clamp(level, 0, LevelLibrary.Levels.Length - 1)].MedalFor(time);
                if (medal != "") medal = "   " + medal + "!";
                winStats.text = string.Format("Level {0}      Time  {1}{2}      Gems  {3}/{4}\n{5}",
                    level + 1, FormatTime(time), medal, gems, total, bestText);
            }
            if (winStory != null)
                winStory.text = LevelLibrary.Levels[
                    Mathf.Clamp(level, 0, LevelLibrary.Levels.Length - 1)].WinLine;
            winPanel.SetActive(true);
        }

        public void ShowGameOver()
        {
            HideAll();
            overPanel.SetActive(true);
        }

        public void ShowComplete(int totalStars, int maxStars)
        {
            HideAll();
            if (completeStats != null)
                completeStats.text = string.Format(
                    "All levels cleared!\nTotal stars  {0} / {1}",
                    totalStars, maxStars);

            // Story first: page through the epilogue, then the stats appear.
            epiloguePages = Story.Epilogue;
            epiloguePage = 0;
            bool hasPages = epiloguePages != null && epiloguePages.Length > 0;
            if (hasPages)
            {
                if (completeStats != null) completeStats.gameObject.SetActive(false);
                if (completeSub != null) completeSub.gameObject.SetActive(false);
                if (completeStory != null)
                {
                    completeStory.text = epiloguePages[0];
                    completeStory.gameObject.SetActive(true);
                }
                if (completeNext != null) completeNext.gameObject.SetActive(true);
            }
            else
            {
                if (completeStats != null) completeStats.gameObject.SetActive(true);
                if (completeSub != null) completeSub.gameObject.SetActive(true);
                if (completeStory != null) completeStory.gameObject.SetActive(false);
                if (completeNext != null) completeNext.gameObject.SetActive(false);
            }
            completePanel.SetActive(true);
        }

        public void UpdateHUD(int level, int gems, int total, int lives, float time)
        {
            if (hudLevel != null)
                hudLevel.text = "LV " + (level + 1);
            if (hudGems != null)
                hudGems.text = string.Format("Gems  {0} / {1}", gems, total);
            if (hudTime != null)
                hudTime.text = FormatTime(time);
            if (hudLives != null)
                hudLives.text = string.Format("Lives  {0}", lives);
        }

        static string FormatTime(float t)
        {
            int minutes = (int)(t / 60f);
            float seconds = t - minutes * 60f;
            return string.Format("{0}:{1:00.0}", minutes, seconds);
        }

        // ---------- Widget builders ----------

        static void HideAll()
        {
            UIManager self = Instance;
            if (self == null) return;
            self.menuPanel.SetActive(false);
            self.hudPanel.SetActive(false);
            self.winPanel.SetActive(false);
            self.overPanel.SetActive(false);
            self.completePanel.SetActive(false);
            self.settingsPanel.SetActive(false);
            self.pausePanel.SetActive(false);
            self.introPanel.SetActive(false);
        }

        static GameObject MakePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        Text MakeText(Transform parent, string name, string content, int size, Color color,
            TextAnchor align, Vector2 anchorMin, Vector2 anchorMax,
            float padL, float padB, float padR, float padT)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            if (font != null) text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            RectTransform rt = text.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(padL, padB);
            rt.offsetMax = new Vector2(-padR, -padT);
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        Button MakeButton(Transform parent, string label, Vector2 anchor,
            Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = Fx.CircleSprite();
            img.type = Image.Type.Sliced;
            img.color = onColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);

            // Press feedback: quick dip and spring back.
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            button.onClick.AddListener(delegate
            {
                Tweener.Value(0f, 1f, 0.16f, delegate(float k)
                {
                    float s = Mathf.Lerp(0.9f, 1f, k);
                    rt.localScale = new Vector3(s, s, 1f);
                });
            });

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            Text text = labelGo.AddComponent<Text>();
            if (font != null) text.font = font;
            text.text = label;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform lrt = text.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            return button;
        }
    }
}
