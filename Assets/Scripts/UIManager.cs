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
        Text winMilestone;
        Text completeStats;
        RectTransform titleRect;
        Image[] winStars;
        Text[] levelButtonTexts;
        Button[] levelButtons;
        Text[] settingsLabels; // 0 sound, 1 shake, 2 haptics, 3 shadows,
                               // 4 lefty, 5 fullscreen (desktop only)
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
        readonly Color starGold = ArtLib.Gold;   // the world's reward gold
        readonly Color starDim = new Color(0.3f, 0.3f, 0.34f);
        GameObject quitConfirmPanel; // D4: Esc/back from the menu asks before quitting
        System.Action quitConfirmedAction;

        void Awake()
        {
            Instance = this;
            font = LoadFont();

            // Desktop: honor the persisted fullscreen/windowed choice
            // before any screen exists, so the Settings toggle survives
            // restarts. Mobile is always fullscreen and never reads it.
            if (IsDesktopPlatform())
                Screen.fullScreenMode = SaveSystem.FullscreenOn
                    ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            GameObject canvasGo = new GameObject("UICanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            // Landscape-locked game: scale by height, so phones, tablets and
            // desktop all show UI of the same physical size and the extra
            // width of any screen simply shows wider panels.
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(transform, false);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Every screen parents under this full-stretch root so
            // notches, punch-holes, rounded corners and gesture bars never
            // clip interactive UI; SafeArea keeps it fitted as the safe
            // area changes (rotation, resolution, device).
            GameObject safeGo = new GameObject("SafeRoot", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            RectTransform safeRect = (RectTransform)safeGo.transform;
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeGo.AddComponent<SafeArea>();

            BuildMenu(safeGo.transform);
            BuildHUD(safeGo.transform);
            BuildWin(safeGo.transform);
            BuildGameOver(safeGo.transform);
            BuildComplete(safeGo.transform);
            BuildSettings(safeGo.transform);
            BuildPause(safeGo.transform);
            BuildIntro(safeGo.transform);
            BuildStoryToast(safeGo.transform);

            HideAll();
        }

        // Star-ding scheduler: on the win screen the earned stars ding one
        // by one (then a flourish if it's a new record). Timer-driven on
        // unscaled time — no coroutines, no timeScale coupling.
        int pendingStarDings;
        int starsDinged;
        float starDingTimer;
        float recordFlourishTimer; // >0 counts down to the new-record chime

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
            if (pendingStarDings > 0)
            {
                starDingTimer -= Time.unscaledDeltaTime;
                if (starDingTimer <= 0f)
                {
                    AudioManager.Instance.PlayStarDing(starsDinged);
                    if (winStars != null && starsDinged < winStars.Length)
                    {
                        Image star = winStars[starsDinged];
                        star.color = starGold;
                        // Land big, settle small — same overshoot grammar
                        // as Pip's squash-and-stretch.
                        RectTransform srt = star.rectTransform;
                        Tweener.Value(1.6f, 1f, 0.28f, delegate(float k)
                        {
                            srt.localScale = new Vector3(k, k, 1f);
                        });
                    }
                    starsDinged++;
                    pendingStarDings--;
                    starDingTimer = 0.34f;
                    if (pendingStarDings == 0 && recordFlourishTimer > 0f)
                        recordFlourishTimer = 0.5f;
                }
            }
            else if (recordFlourishTimer > 0f)
            {
                recordFlourishTimer -= Time.unscaledDeltaTime;
                if (recordFlourishTimer <= 0f)
                    AudioManager.Instance.PlayNewRecord();
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
                starGold, TextAnchor.MiddleCenter,
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

            // Touch builds pin every tappable target to at least 100x100
            // ref units (~48 dp); keyboard and mouse keep the compact
            // sizes. The taller touch PLAY needs headroom under the
            // tagline, so on touch it anchors a little higher — the grid
            // below derives its band from the same anchor.
            bool touch = Input.touchSupported;
            float playY = touch ? 0.5f : 0.46f;
            MakeButton(menuPanel.transform, "PLAY",
                new Vector2(0.5f, playY), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.PlayContinue(); });

            // Fifteen levels fit a 5x3 grid; smaller counts use 3 per row.
            int count = LevelLibrary.Levels.Length;
            levelButtons = new Button[count];
            levelButtonTexts = new Text[count];
            int perRow = count <= 4 ? count : (count > 12 ? 5 : 3);
            int rows = (count + perRow - 1) / perRow;
            // The grid must fit between PLAY and the instructions band
            // (top y≈0.085) however many levels exist. Spacing is derived
            // from the row count so the last row never collides with it.
            // On touch the buttons are pinned at 100 units tall (~48 dp),
            // so the same derivation runs from the real button size and
            // PLAY's higher anchor instead of the fixed band — three rows
            // of 100-unit buttons only fit when PLAY moves up with them.
            float gridTop = 0.395f;
            float gridBottom = 0.135f;
            float rowStep = rows > 1
                ? Mathf.Min(0.075f, (gridTop - gridBottom) / (rows - 1))
                : 0f;
            float buttonHeight = Mathf.Min(60f, 46f + rowStep * 120f);
            Vector2 levelSize = new Vector2(200f, buttonHeight);
            if (touch)
            {
                buttonHeight = 100f;
                levelSize = new Vector2(240f, buttonHeight);
                float half = buttonHeight / 900f * 0.5f;
                float margin = 0.006f;
                gridTop = playY - half * 2f - margin;
                gridBottom = 0.085f + half + margin;
                rowStep = rows > 1
                    ? Mathf.Min(buttonHeight / 900f + margin * 2f,
                        (gridTop - gridBottom) / (rows - 1))
                    : 0f;
            }
            for (int i = 0; i < count; i++)
            {
                int index = i; // capture for the delegate
                int row = i / perRow;
                // Center whichever buttons actually landed on the last row.
                int inRow = (row == rows - 1) ? (count - row * perRow) : perRow;
                int col = i - row * perRow;
                float x = 0.5f - ((inRow - 1) / 2f - col) * 0.185f;
                float y = gridTop - row * rowStep;
                Button b = MakeButton(menuPanel.transform, "LEVEL " + (i + 1),
                    new Vector2(x, y), new Vector2(0f, 0f), levelSize,
                    delegate { GameManager.Instance.PlayLevel(index); });
                levelButtons[i] = b;
                levelButtonTexts[i] = b.GetComponentInChildren<Text>();
                if (levelButtonTexts[i] != null)
                    levelButtonTexts[i].fontSize = rowStep < 0.06f ? 19 : 21;
            }

            // The floor-enlarged touch SETTINGS (220x100) would poke past
            // the top and right edges at the keyboard/mouse anchor, so
            // touch tucks it slightly inward.
            MakeButton(menuPanel.transform, "SETTINGS",
                new Vector2(touch ? 0.905f : 0.925f, touch ? 0.93f : 0.965f),
                new Vector2(0f, 0f),
                touch ? new Vector2(220f, 100f) : new Vector2(190f, 54f),
                delegate { ShowSettings(); });

            MakeText(menuPanel.transform, "Instructions",
                "Move: WASD / Arrows    Jump: Space    (ENTER works too)\n" +
                "Collect gems for stars, dodge the red spinners, reach the portal!\n" +
                "On touch: drag left side to move, tap JUMP.",
                18, new Color(0.85f, 0.87f, 0.92f), TextAnchor.LowerLeft,
                new Vector2(0.03f, 0.01f), new Vector2(0.46f, 0.085f), 0f, 0f, 0f, 0f);

            menuQuote = MakeText(menuPanel.transform, "MenuQuote", "", 20,
                new Color(0.72f, 0.78f, 0.88f), TextAnchor.LowerRight,
                new Vector2(0.52f, 0.01f), new Vector2(0.97f, 0.085f), 0f, 0f, 0f, 0f);
            menuQuote.fontStyle = FontStyle.Italic;

            visitRecap = MakeText(menuPanel.transform, "VisitRecap", "", 19,
                starGold, TextAnchor.UpperLeft,
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
                        img.color = starGold; // the reward gold, on a button
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

            // The floor-enlarged touch pause button (100 units) would poke
            // past the top edge at the keyboard/mouse anchor, so touch sits
            // it a little lower.
            bool touch = Input.touchSupported;
            Button pause = MakeButton(hudPanel.transform, "II",
                new Vector2(0.645f, touch ? 0.93f : 0.965f), new Vector2(0f, 0f),
                new Vector2(58f, 58f), delegate { GameManager.Instance.PauseGame(); });
            Text pauseLabel = pause.GetComponentInChildren<Text>();
            if (pauseLabel != null) pauseLabel.fontSize = 26;

            hudLives = MakeText(hudPanel.transform, "Lives", "Lives  3", 28,
                starGold, TextAnchor.MiddleRight,
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
                img.sprite = Fx.StarSprite(); // a star that looks like one
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0.5f + (i - 1) * 0.09f, 0.5f);
                rt.anchorMax = rt.anchorMin;
                rt.sizeDelta = new Vector2(84f, 84f);
                winStars[i] = img;
            }

            winStats = MakeText(winPanel.transform, "Stats", "", 30,
                Color.white, TextAnchor.UpperCenter,
                new Vector2(0f, 0.36f), new Vector2(1f, 0.46f), 0f, 0f, 0f, 0f);

            winStory = MakeText(winPanel.transform, "Story", "", 26,
                new Color(0.75f, 0.82f, 0.95f), TextAnchor.UpperCenter,
                new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.38f), 0f, 0f, 0f, 0f);
            winStory.fontStyle = FontStyle.Italic;

            // Atlas stamp: the last level of a pack carries a milestone
            // line. It hangs above the title like a ribbon, so the
            // world-grew-a-page moment is the first thing read.
            winMilestone = MakeText(winPanel.transform, "Milestone", "", 24,
                starGold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.775f), new Vector2(0.95f, 0.855f), 0f, 0f, 0f, 0f);
            winMilestone.fontStyle = FontStyle.Bold;
            winMilestone.gameObject.SetActive(false);

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
                "EVERY PORTAL LIT!", 72,
                starGold, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.58f), new Vector2(1f, 0.76f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            completeStats = MakeText(completePanel.transform, "Stats", "", 34,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.40f), new Vector2(1f, 0.54f), 0f, 0f, 0f, 0f);

            MakeText(completePanel.transform, "Sub",
                "The storm has a job now. The map has room left.\n" +
                "Pip's shelf keeps one spot open — for whatever comes next.", 24,
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
            AudioManager.Instance.PlayPageTurn();
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

            // Rows: Sound, Screen Shake, Haptics, Shadows, Left-handed
            // Controls — plus Fullscreen on desktop only. The step derives
            // from the row count so the six-row desktop layout still keeps
            // its last row clear of BACK below.
            string[] names = IsDesktopPlatform()
                ? new string[] { "Sound", "Screen Shake", "Haptics", "Shadows",
                    "Left-handed Controls", "Fullscreen" }
                : new string[] { "Sound", "Screen Shake", "Haptics", "Shadows",
                    "Left-handed Controls" };
            settingsLabels = new Text[names.Length];
            // On touch the 100-unit-tall rows cannot stack five high
            // between the title and BACK, so they reflow into a 2-column
            // grid (reading order; an odd last row centers itself).
            bool touch = Input.touchSupported;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                float x = 0.5f;
                float y = 0.62f - i * 0.0745f;
                if (touch)
                {
                    int row = i / 2;
                    int inRow = (i == names.Length - 1 && i % 2 == 0) ? 1 : 2;
                    x = 0.5f + (i % 2 - (inRow - 1) / 2f) * 0.42f;
                    y = 0.59f - row * 0.125f;
                }
                Button b = MakeButton(settingsPanel.transform, names[i],
                    new Vector2(x, y), new Vector2(0f, 0f),
                    new Vector2(480f, 60f), delegate { ToggleSetting(index); });
                settingsLabels[i] = b.GetComponentInChildren<Text>();
            }

            MakeButton(settingsPanel.transform, "BACK",
                new Vector2(0.5f, 0.17f), new Vector2(0f, 0f),
                new Vector2(260f, 64f), delegate { CloseSettings(); });

            settingsPanel.SetActive(false);
        }

        void ShowSettings()
        {
            RefreshSettings();
            settingsPanel.SetActive(true);
            AudioManager.Instance.PlayPanel(true);
        }

        void RefreshSettings()
        {
            if (settingsLabels == null) return;
            ApplyLabel(settingsLabels[0], "Sound", SaveSystem.SoundOn);
            ApplyLabel(settingsLabels[1], "Screen Shake", SaveSystem.ShakeOn);
            ApplyLabel(settingsLabels[2], "Haptics", SaveSystem.HapticsOn);
            ApplyLabel(settingsLabels[3], "Shadows", SaveSystem.ShadowsOn);
            ApplyLabel(settingsLabels[4], "Left-handed Controls", SaveSystem.LeftyOn);
            if (settingsLabels.Length > 5)
                ApplyLabel(settingsLabels[5], "Fullscreen", SaveSystem.FullscreenOn);
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
            else if (index == 1) SaveSystem.ShakeOn = !SaveSystem.ShakeOn;
            else if (index == 2) SaveSystem.HapticsOn = !SaveSystem.HapticsOn;
            else if (index == 3)
            {
                SaveSystem.ShadowsOn = !SaveSystem.ShadowsOn;
                QualitySettings.shadows = SaveSystem.ShadowsOn
                    ? ShadowQuality.All : ShadowQuality.Disable;
            }
            else if (index == 4)
            {
                SaveSystem.LeftyOn = !SaveSystem.LeftyOn;
                // Live-mirror the HUD: re-anchor the jump button now, no
                // level restart needed (no-op off touch, where the controls
                // do not exist).
                if (TouchControls.Instance != null) TouchControls.Instance.ApplySide();
            }
            else if (index == 5)
            {
                SaveSystem.FullscreenOn = !SaveSystem.FullscreenOn;
                Screen.fullScreenMode = SaveSystem.FullscreenOn
                    ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }
            // A flipped setting answers with its own blip: up when it
            // turned on, down when it turned off.
            bool[] states =
            {
                SaveSystem.SoundOn, SaveSystem.ShakeOn, SaveSystem.HapticsOn,
                SaveSystem.ShadowsOn, SaveSystem.LeftyOn, SaveSystem.FullscreenOn
            };
            if (index >= 0 && index < states.Length)
                AudioManager.Instance.PlayUIToggle(states[index]);
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

            // On touch the three floor-enlarged buttons (100 units each)
            // would collide as a stack, so the bottom one drops a step.
            MakeButton(pausePanel.transform, "MENU",
                new Vector2(0.5f, Input.touchSupported ? 0.16f : 0.17f),
                new Vector2(0f, 0f),
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
                starGold, TextAnchor.MiddleCenter,
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
            AudioManager.Instance.PlayIntro();
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
            // Stars start dim and land one by one: the ding scheduler in
            // Update golds each star (with a small slam-settle pop) at the
            // exact moment its ding plays. A new record gets a flourish
            // once the last star has landed.
            if (winStars != null)
            {
                for (int i = 0; i < winStars.Length; i++)
                {
                    winStars[i].color = starDim;
                    winStars[i].rectTransform.localScale = Vector3.one;
                }
            }
            pendingStarDings = stars;
            starsDinged = 0;
            starDingTimer = 0.55f;
            recordFlourishTimer = newRecord ? 0.01f : 0f;
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
            if (winMilestone != null)
            {
                string milestone = LevelLibrary.Levels[
                    Mathf.Clamp(level, 0, LevelLibrary.Levels.Length - 1)].Milestone;
                winMilestone.text = milestone;
                winMilestone.gameObject.SetActive(
                    !string.IsNullOrEmpty(milestone));
            }
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

        // ---------- Back-stack support (D4) ----------

        /// True while the settings panel is on screen. The GameManager
        /// back/Escape stack closes Settings before any other back action.
        public bool SettingsOpen => settingsPanel != null && settingsPanel.activeSelf;

        /// Closes the settings panel (same as pressing its BACK button).
        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
            AudioManager.Instance.PlayPanel(false);
        }

        /// Shows the quit-confirmation dialog ("QUIT THE GAME?"). Quitting
        /// always goes through this dialog on desktop — Esc/back never quits
        /// instantly. QUIT invokes onConfirmed then quits; CANCEL (or the
        /// back stack) just closes the dialog.
        public void ShowQuitConfirm(System.Action onConfirmed)
        {
            if (quitConfirmPanel == null) BuildQuitConfirm();
            quitConfirmedAction = onConfirmed;
            quitConfirmPanel.SetActive(true);
            AudioManager.Instance.PlayPanel(true);
        }

        /// Closes the quit-confirmation dialog if it is open. Returns true
        /// when it was open (and is now closed), so the back stack can treat
        /// Esc/back as CANCEL; returns false when nothing was open.
        public bool CloseQuitConfirm()
        {
            if (quitConfirmPanel == null || !quitConfirmPanel.activeSelf) return false;
            quitConfirmPanel.SetActive(false);
            AudioManager.Instance.PlayPanel(false);
            return true;
        }

        void BuildQuitConfirm()
        {
            // Same settings-style overlay: full-screen dim panel with a bold
            // title and centered buttons. Built lazily, parented under the
            // same canvas as every other screen.
            quitConfirmPanel = MakePanel(menuPanel.transform.parent,
                "QuitConfirmPanel", new Color(0f, 0f, 0.05f, 0.8f));

            Text title = MakeText(quitConfirmPanel.transform, "Title",
                "QUIT THE GAME?", 64, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.56f), new Vector2(1f, 0.70f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            MakeButton(quitConfirmPanel.transform, "QUIT",
                new Vector2(0.5f - 0.13f, 0.38f), new Vector2(0f, 0f),
                new Vector2(260f, 84f), delegate { ConfirmQuit(); });

            MakeButton(quitConfirmPanel.transform, "CANCEL",
                new Vector2(0.5f + 0.13f, 0.38f), new Vector2(0f, 0f),
                new Vector2(260f, 84f), delegate { CloseQuitConfirm(); });

            quitConfirmPanel.SetActive(false);
        }

        void ConfirmQuit()
        {
            System.Action action = quitConfirmedAction;
            CloseQuitConfirm();
            if (action != null) action.Invoke();
            Application.Quit();
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

        /// Touch hit-target floor: on touch devices no tappable button may
        /// be smaller than 100x100 ref units (~48 dp at the 900-unit
        /// reference height), so requested sizes grow to meet the floor.
        /// Keyboard and mouse builds keep their designed sizes unchanged.
        static Vector2 TouchTarget(Vector2 size)
        {
            if (!Input.touchSupported) return size;
            return new Vector2(Mathf.Max(size.x, 100f), Mathf.Max(size.y, 100f));
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
            // Every button in the game answers with the same tiny tick,
            // first in the listener order so it precedes the action's own
            // sounds (whooshes, fanfares) rather than stacking on them.
            button.onClick.AddListener(delegate { AudioManager.Instance.PlayUIClick(); });
            button.onClick.AddListener(onClick);

            // Press feedback: quick dip and spring back.
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = TouchTarget(size);
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

        /// True on desktop standalone players and in the editor — the
        /// platforms that get the Fullscreen settings row and the persisted
        /// window mode. Mobile builds are always fullscreen.
        static bool IsDesktopPlatform()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.LinuxPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.LinuxEditor;
        }
    }
}
