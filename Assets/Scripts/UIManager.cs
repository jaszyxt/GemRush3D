using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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

        // Touch level select is paged (5x2 per page): once the packs grew
        // past four rows, 100-unit-tall touch buttons could no longer stack
        // between PLAY and the instructions without covering each other's
        // tap area. Keyboard/mouse keeps the one dense grid.
        const int PageSize = 10;
        int levelPage;
        Button pagePrev;
        Button pageNext;
        Text pageLabel;

        // Settings opened while paused becomes a layer over the pause menu:
        // pause hides while Settings is up and returns when it closes.
        bool settingsOverPause;

        // Gamepad support (D5): captured buttons for first-selected focus
        // and explicit navigation wiring.
        Button playButton;
        Button menuSettingsButton;
        Button[] settingsButtons;
        Button settingsBackButton;
        Button winNextButton, winReplayButton, winMenuButton;
        Button overTryButton, overMenuButton;
        Button pauseResumeButton, pauseRestartButton, pauseMenuButton,
            pauseSettingsButton;
        Button quitButton, cancelButton;
        Button completeMenuButton;
        int menuPerRow = 3;

        // Input-aware menu hints (D6): the instructions block follows the
        // last-used device — gamepad, touch or keyboard.
        Text instructionsText;
        int instructionsMode = -1;

        // Change-cache for the HUD: callers may push every frame, but a
        // write (and its string allocation) only happens when the displayed
        // value actually moved.
        int hudCacheLevel = -1;
        int hudCacheGems = -1;
        int hudCacheTotal = -1;
        int hudCacheLives = -1;
        int hudCacheDeciseconds = -1;

        // Delight feedback state: the gem-counter pulse (token-guarded so a
        // fast chain restarts the tween instead of stacking tweens) and the
        // low-life heart breathing (a slow warm gold pulse, never red).
        int gemPulseSeq;
        float heartGlowPhase;
        bool heartGlowing;

        // Per-panel transition generations: a stale fade (show or hide) must
        // never land on a panel whose state has since changed. The in-play
        // HUD keeps instant show/hide — gameplay never waits on a tween.
        readonly System.Collections.Generic.Dictionary<GameObject, int>
            panelGenerations =
                new System.Collections.Generic.Dictionary<GameObject, int>();

        // Text Size setting: every label registers its designed size here,
        // so Large mode re-derives from the base and can never overflow a
        // layout that was fitted for the default.
        readonly System.Collections.Generic.List<Text> scalableTexts =
            new System.Collections.Generic.List<Text>();
        readonly System.Collections.Generic.List<int> scalableBaseSizes =
            new System.Collections.Generic.List<int>();

        // The Atlas (meta-collection): one region shown at a time over a
        // dim panel, with per-level stars, medals and best times.
        GameObject atlasPanel;
        Text atlasHeader;
        Text atlasStarsTotal;
        Text atlasMilestone;
        Text atlasPageLabel;
        Button[] atlasRows;
        Button atlasPrev;
        Button atlasNext;
        Button atlasBack;
        int atlasRegion;

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
            // Input System UI module (D5): gamepad stick/dpad menu
            // navigation plus B-cancel and A-submit, with mouse and touch
            // pointer unchanged. Requires the "Both" input backend that
            // EnsureInput pins at project load.
            esGo.AddComponent<InputSystemUIInputModule>();

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
            BuildAtlas(safeGo.transform);
            BuildPause(safeGo.transform);
            BuildIntro(safeGo.transform);
            BuildStoryToast(safeGo.transform);

            // Honor a persisted Text Size preference on every label (a no-op
            // at the default size).
            ApplyTextSize();

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
                // The hint line follows the last-used device; rewrite only
                // on an actual flip (the mode read is throttled internally).
                int mode = CurrentInstructionMode();
                if (mode != instructionsMode && instructionsText != null)
                {
                    instructionsMode = mode;
                    instructionsText.text = InstructionsForCurrentDevice();
                }
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

            UpdateHeartGlow();
        }

        // Low-life heart glow: at the last life the hearts breathe — a slow
        // sine on scale and warmth, GOLD-tinted and calm (0.8 s period, well
        // under the ~1.8 Hz flash ceiling; red stays hazard-only). Stops the
        // moment lives recover, restoring the exact resting look.
        void UpdateHeartGlow()
        {
            bool low = hudPanel != null && hudPanel.activeSelf &&
                hudCacheLives == 1 && hudLives != null;
            if (low)
            {
                heartGlowing = true;
                heartGlowPhase += Time.unscaledDeltaTime;
                float breathe = 0.5f + 0.5f * Mathf.Sin(
                    heartGlowPhase * (2f * Mathf.PI / 0.8f));
                float s = 1f + 0.07f * breathe;
                hudLives.rectTransform.localScale = new Vector3(s, s, 1f);
                Color c = Color.Lerp(ArtLib.Gold * 0.85f, ArtLib.Gold * 1.3f,
                    breathe);
                c.a = Mathf.Lerp(0.85f, 1f, breathe);
                hudLives.color = c;
            }
            else if (heartGlowing)
            {
                heartGlowing = false;
                heartGlowPhase = 0f;
                hudLives.rectTransform.localScale = Vector3.one;
                hudLives.color = starGold;
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
            // sizes. Both share PLAY's anchor: with paging (below) the
            // touch grid needs only two rows, so the desktop rhythm works
            // for touch too.
            bool touch = Input.touchSupported;
            float playY = 0.46f;
            playButton = MakeButton(menuPanel.transform, "PLAY",
                new Vector2(0.5f, playY), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.PlayContinue(); });

            // Fifteen levels fit a 5x3 grid; smaller counts use 3 per row.
            int count = LevelLibrary.Levels.Length;
            levelButtons = new Button[count];
            levelButtonTexts = new Text[count];
            int perRow;
            int rows;
            float gridTop, gridBottom, rowStep, buttonHeight;
            Vector2 levelSize;
            int levelLabelSize;
            if (touch)
            {
                // Paged 5x2: two fixed rows of 100-unit buttons in the band
                // between PLAY and the instructions, arrows at the band's
                // vertical middle. Page-flipping is in FlipPage/RefreshMenu.
                perRow = 5;
                rows = 2;
                buttonHeight = 100f;
                levelSize = new Vector2(240f, buttonHeight);
                levelLabelSize = 24;
                gridTop = 0.32f;
                gridBottom = 0.165f;
                rowStep = gridTop - gridBottom;
            }
            else
            {
                perRow = count <= 4 ? count : (count > 12 ? 5 : 3);
                rows = (count + perRow - 1) / perRow;
                // The grid must fit between PLAY (y≈0.46) and the
                // instructions band (top y≈0.085) however many levels
                // exist. Spacing is derived from the row count so the last
                // row never collides with it.
                gridTop = 0.395f;
                gridBottom = 0.135f;
                rowStep = rows > 1
                    ? Mathf.Min(0.075f, (gridTop - gridBottom) / (rows - 1))
                    : 0f;
                buttonHeight = Mathf.Min(60f, 46f + rowStep * 120f);
                levelSize = new Vector2(200f, buttonHeight);
                levelLabelSize = rowStep < 0.06f ? 20 : 21;
            }
            menuPerRow = perRow;
            for (int i = 0; i < count; i++)
            {
                int index = i; // capture for the delegate
                int row, col, inRow;
                float x, y;
                if (touch)
                {
                    // Position within the page: row 0 is this page's first
                    // five levels, row 1 its second five.
                    row = (i / perRow) % 2;
                    col = i % perRow;
                    inRow = perRow;
                    // Step 0.17 keeps the outer columns clear of the page
                    // arrows flanking the grid (240-wide buttons end at
                    // x 0.085 / begin at 0.915; the arrows end at 0.073 /
                    // begin at 0.927).
                    x = 0.5f - (2 - col) * 0.17f;
                    y = row == 0 ? gridTop : gridBottom;
                }
                else
                {
                    row = i / perRow;
                    // Center whichever buttons actually landed on the last row.
                    inRow = (row == rows - 1) ? (count - row * perRow) : perRow;
                    col = i - row * perRow;
                    x = 0.5f - ((inRow - 1) / 2f - col) * 0.185f;
                    y = gridTop - row * rowStep;
                }
                Button b = MakeButton(menuPanel.transform, "LEVEL " + (i + 1),
                    new Vector2(x, y), new Vector2(0f, 0f), levelSize,
                    delegate { GameManager.Instance.PlayLevel(index); },
                    levelLabelSize);
                levelButtons[i] = b;
                levelButtonTexts[i] = b.GetComponentInChildren<Text>();
            }

            if (touch)
            {
                // Page flipper: arrows flank the grid; the "n / N" readout
                // sits in the bottom-center gap between the instructions
                // band (left) and the flavor quotes (right).
                pagePrev = MakeButton(menuPanel.transform, "‹",
                    new Vector2(0.045f, 0.2425f), new Vector2(0f, 0f),
                    new Vector2(90f, 100f), delegate { FlipPage(-1); }, 44);
                pageNext = MakeButton(menuPanel.transform, "›",
                    new Vector2(0.955f, 0.2425f), new Vector2(0f, 0f),
                    new Vector2(90f, 100f), delegate { FlipPage(1); }, 44);
                pageLabel = MakeText(menuPanel.transform, "PageLabel", "", 20,
                    new Color(0.85f, 0.87f, 0.92f), TextAnchor.MiddleCenter,
                    new Vector2(0.455f, 0.005f), new Vector2(0.545f, 0.05f),
                    0f, 0f, 0f, 0f);
            }

            // The floor-enlarged touch SETTINGS (220x100) would poke past
            // the top and right edges at the keyboard/mouse anchor, so
            // touch tucks it slightly inward.
            menuSettingsButton = MakeButton(menuPanel.transform, "SETTINGS",
                new Vector2(touch ? 0.905f : 0.925f, touch ? 0.93f : 0.965f),
                new Vector2(0f, 0f),
                touch ? new Vector2(220f, 100f) : new Vector2(190f, 54f),
                delegate { ShowSettings(); });

            // The Atlas: the collection view, one region at a time.
            MakeButton(menuPanel.transform, "ATLAS",
                new Vector2(touch ? 0.75f : 0.775f, touch ? 0.93f : 0.965f),
                new Vector2(0f, 0f),
                touch ? new Vector2(190f, 100f) : new Vector2(160f, 54f),
                delegate { ShowAtlas(); });

            // Input-aware hints (D6): the block follows the last-used
            // device (gamepad / touch / keyboard) and rewrites itself the
            // moment that changes while the menu is open.
            instructionsText = MakeText(menuPanel.transform, "Instructions", "", 22,
                new Color(0.85f, 0.87f, 0.92f), TextAnchor.LowerLeft,
                new Vector2(0.03f, 0.01f), new Vector2(0.46f, 0.085f), 0f, 0f, 0f, 0f);
            instructionsMode = CurrentInstructionMode();
            instructionsText.text = InstructionsForCurrentDevice();

            menuQuote = MakeText(menuPanel.transform, "MenuQuote", "", 22,
                new Color(0.72f, 0.78f, 0.88f), TextAnchor.LowerRight,
                new Vector2(0.52f, 0.01f), new Vector2(0.97f, 0.085f), 0f, 0f, 0f, 0f);
            menuQuote.fontStyle = FontStyle.Italic;

            visitRecap = MakeText(menuPanel.transform, "VisitRecap", "", 22,
                starGold, TextAnchor.UpperLeft,
                new Vector2(0.03f, 0.90f), new Vector2(0.60f, 0.95f), 12f, 0f, 0f, 0f);

            WireMenuNav(touch);
        }

        /// Explicit gamepad navigation for the menu (D5): PLAY anchors
        /// everything, the grid wraps per row, SETTINGS hangs off PLAY's
        /// right and drops into the grid's top-right cell. The paged touch
        /// grid wires only its visible page, so rewire after every flip.
        void WireMenuNav(bool touch)
        {
            if (playButton == null || menuSettingsButton == null ||
                levelButtons == null || levelButtons.Length == 0) return;
            if (!touch)
            {
                MenuNav.Grid(levelButtons, menuPerRow, playButton);
                MenuNav.Set(playButton, null, levelButtons[0], null,
                    menuSettingsButton);
                MenuNav.Set(menuSettingsButton, null,
                    levelButtons[menuPerRow - 1], playButton, null);
                return;
            }
            int pages = PageCount();
            int first = Mathf.Clamp(levelPage, 0, pages - 1) * PageSize;
            int inPage = Mathf.Min(PageSize, levelButtons.Length - first);
            Button[] page = new Button[inPage];
            for (int i = 0; i < inPage; i++) page[i] = levelButtons[first + i];
            MenuNav.Grid(page, menuPerRow, playButton);
            MenuNav.Set(playButton, null, page[0], null, menuSettingsButton);
            MenuNav.Set(menuSettingsButton, null,
                page[Mathf.Min(menuPerRow - 1, inPage - 1)], playButton, null);
        }

        /// Which device the menu hint line should describe right now:
        /// 2 = gamepad, 1 = touch, 0 = keyboard.
        int CurrentInstructionMode()
        {
            if (GamepadInput.Available) return 2;
            if (Input.touchSupported) return 1;
            return 0;
        }

        string InstructionsForCurrentDevice()
        {
            const string goal =
                "Collect gems for stars, dodge the red spinners, reach the portal!";
            switch (CurrentInstructionMode())
            {
                case 2:
                    return "Move: Left Stick / D-Pad    Jump: (A) / Cross    " +
                        "Pause: Start\n" + goal +
                        "\nMenus: D-Pad choose · (A) confirm · (B) back · " +
                        "shoulders flip pages.";
                case 1:
                    return "On touch: drag the left side to move, tap JUMP.\n" +
                        goal + "\nKeyboard: WASD / Arrows + Space.";
                default:
                    return "Move: WASD / Arrows    Jump: Space    (ENTER works too)\n" +
                        goal + "\nGamepad: Left Stick + (A) — plug one in and " +
                        "this line follows it.";
            }
        }

        void RefreshMenu()
        {
            if (levelButtons == null) return;
            int count = levelButtons.Length;
            int dailyIndex = DailyGem.TodayIndex();
            bool giftTaken = DailyGem.GiftAlreadyCollectedToday();
            int first = 0;
            int last = count;
            if (Input.touchSupported)
            {
                int pages = PageCount();
                levelPage = Mathf.Clamp(levelPage, 0, pages - 1);
                first = levelPage * PageSize;
                last = Mathf.Min(first + PageSize, count);
                if (pageLabel != null)
                    pageLabel.text = (levelPage + 1) + " / " + pages;
                if (pagePrev != null) pagePrev.interactable = levelPage > 0;
                if (pageNext != null) pageNext.interactable = levelPage < pages - 1;
            }
            for (int i = 0; i < count; i++)
            {
                // Off-page buttons deactivate entirely — no stray raycasts,
                // no golden daily glow leaking through from another page.
                bool onPage = i >= first && i < last;
                if (levelButtons[i].gameObject.activeSelf != onPage)
                    levelButtons[i].gameObject.SetActive(onPage);
                if (!onPage) continue;

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
                // The repaint above overwrites the base tint; keep any
                // focused button's highlight honest (D5).
                FocusFX fx = levelButtons[i].GetComponent<FocusFX>();
                if (fx != null) fx.RefreshRestColor();
            }
        }

        int PageCount()
        {
            return (levelButtons.Length + PageSize - 1) / PageSize;
        }

        /// Public so the gamepad's shoulder buttons can page the list from
        /// GameManager (D6); the on-screen arrows remain pointer targets.
        public void FlipPage(int dir)
        {
            levelPage = Mathf.Clamp(levelPage + dir, 0, PageCount() - 1);
            RefreshMenu();
            WireMenuNav(Input.touchSupported);
        }

        void BuildHUD(Transform canvas)
        {
            hudPanel = MakePanel(canvas, "HudPanel", new Color(0f, 0f, 0f, 0f));

            // The four HUD readouts change constantly (the clock rebuilds
            // its mesh ~10x/s even when nothing else moves). uGUI dirties a
            // whole canvas when any of its graphics changes, so they get a
            // small nested canvas of their own and the static pause button
            // never re-batches with them.
            GameObject hudDyn = new GameObject("HudDynamic", typeof(RectTransform));
            hudDyn.transform.SetParent(hudPanel.transform, false);
            RectTransform dynRect = (RectTransform)hudDyn.transform;
            dynRect.anchorMin = Vector2.zero;
            dynRect.anchorMax = Vector2.one;
            dynRect.offsetMin = Vector2.zero;
            dynRect.offsetMax = Vector2.zero;
            hudDyn.AddComponent<Canvas>();
            Transform dyn = hudDyn.transform;

            hudLevel = MakeText(dyn, "Level", "", 26,
                new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0.93f), new Vector2(0.22f, 1f), 24f, 4f, 4f, 2f);

            hudGems = MakeText(dyn, "Gems", "Gems  0 / 0", 28,
                Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.22f, 0.93f), new Vector2(0.48f, 1f), 12f, 4f, 4f, 2f);

            hudTime = MakeText(dyn, "Time", "0:00.0", 28,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.48f, 0.93f), new Vector2(0.62f, 1f), 4f, 4f, 4f, 2f);

            // The floor-enlarged touch pause button (100 units) would poke
            // past the top edge at the keyboard/mouse anchor, so touch sits
            // it a little lower.
            bool touch = Input.touchSupported;
            Button pause = MakeButton(hudPanel.transform, "II",
                new Vector2(0.645f, touch ? 0.93f : 0.965f), new Vector2(0f, 0f),
                new Vector2(58f, 58f),
                delegate { GameManager.Instance.PauseGame(); }, 26);

            hudLives = MakeText(dyn, "Lives", "Lives  3", 28,
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

            winNextButton = MakeButton(winPanel.transform, "NEXT  LEVEL",
                new Vector2(0.5f, 0.26f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.StartNextLevel(); });

            winReplayButton = MakeButton(winPanel.transform, "REPLAY",
                new Vector2(0.5f - 0.14f, 0.145f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            winMenuButton = MakeButton(winPanel.transform, "MENU",
                new Vector2(0.5f + 0.14f, 0.145f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });

            MenuNav.Set(winNextButton, null, winReplayButton, null, null);
            MenuNav.Set(winReplayButton, winNextButton, winMenuButton,
                null, winMenuButton);
            MenuNav.Set(winMenuButton, winReplayButton, null, winReplayButton, null);
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

            overTryButton = MakeButton(overPanel.transform, "TRY  AGAIN",
                new Vector2(0.5f, 0.28f), new Vector2(0f, 0f),
                new Vector2(360f, 84f),
                delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            overMenuButton = MakeButton(overPanel.transform, "MENU",
                new Vector2(0.5f, 0.155f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });

            MenuNav.Set(overTryButton, null, overMenuButton, null, null);
            MenuNav.Set(overMenuButton, overTryButton, null, null, null);
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

            completeMenuButton = MakeButton(completePanel.transform, "MENU",
                new Vector2(0.5f, 0.18f), new Vector2(0f, 0f),
                new Vector2(300f, 78f), delegate { GameManager.Instance.GoToMenu(); });

            MenuNav.Set(completeNext, null, completeMenuButton, null,
                completeMenuButton);
            MenuNav.Set(completeMenuButton, completeNext, null, completeNext,
                completeNext);
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
                Focus(completeMenuButton); // NEXT left the screen with the story
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
            // Controls, Text Size — plus Fullscreen on desktop only. The
            // desktop step derives from seven rows so the last one keeps
            // its distance from BACK; on touch the 100-unit-tall rows
            // reflow into a 2-column grid (reading order; an odd last row
            // centers itself).
            string[] names = IsDesktopPlatform()
                ? new string[] { "Sound", "Screen Shake", "Haptics", "Shadows",
                    "Left-handed Controls", "Text Size", "Fullscreen" }
                : new string[] { "Sound", "Screen Shake", "Haptics", "Shadows",
                    "Left-handed Controls", "Text Size" };
            settingsLabels = new Text[names.Length];
            settingsButtons = new Button[names.Length];
            bool touch = Input.touchSupported;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                float x = 0.5f;
                float y = touch ? 0f : 0.63f - i * 0.068f;
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
                settingsButtons[i] = b;
            }

            settingsBackButton = MakeButton(settingsPanel.transform, "BACK",
                new Vector2(0.5f, touch ? 0.17f : 0.11f), new Vector2(0f, 0f),
                new Vector2(260f, 64f), delegate { CloseSettings(); });

            // Gamepad navigation (D5): touch lays the rows out as a
            // 2-column grid; desktop keeps one column. BACK sits below
            // either layout and the bottom row(s) drop into it.
            if (touch)
            {
                MenuNav.Grid(settingsButtons, 2, null);
                int lastRowStart = (settingsButtons.Length - 1) / 2 * 2;
                MenuNav.Set(settingsBackButton,
                    settingsButtons[lastRowStart], null, null, null);
                for (int i = lastRowStart; i < settingsButtons.Length; i++)
                {
                    Navigation nav = settingsButtons[i].navigation;
                    nav.selectOnDown = settingsBackButton;
                    settingsButtons[i].navigation = nav;
                }
            }
            else
            {
                MenuNav.Chain(settingsButtons);
                Button last = settingsButtons[settingsButtons.Length - 1];
                MenuNav.Set(settingsBackButton, last, null, null, null);
                Navigation nav = last.navigation;
                nav.selectOnDown = settingsBackButton;
                last.navigation = nav;
            }

            settingsPanel.SetActive(false);
        }

        // ------------------------------------------------------------------
        // The Atlas: the collection view. One region at a time — header,
        // its milestone line, one row per level (stars, medal, best time),
        // region arrows, BACK. Rows launch unlocked levels directly.
        // ------------------------------------------------------------------
        void BuildAtlas(Transform canvas)
        {
            atlasPanel = MakePanel(canvas, "AtlasPanel",
                new Color(0f, 0f, 0.05f, 0.82f));

            Text title = MakeText(atlasPanel.transform, "Title", "THE ATLAS",
                64, starGold, TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0.86f), new Vector2(0.7f, 0.96f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            atlasStarsTotal = MakeText(atlasPanel.transform, "StarsTotal", "",
                26, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0.79f), new Vector2(0.7f, 0.86f), 0f, 0f, 0f, 0f);

            atlasHeader = MakeText(atlasPanel.transform, "RegionHeader", "",
                34, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.77f), 0f, 0f, 0f, 0f);
            atlasHeader.fontStyle = FontStyle.Bold;

            atlasMilestone = MakeText(atlasPanel.transform, "RegionMilestone", "",
                22, new Color(0.75f, 0.82f, 0.95f), TextAnchor.MiddleCenter,
                new Vector2(0.14f, 0.60f), new Vector2(0.86f, 0.68f), 0f, 0f, 0f, 0f);
            atlasMilestone.fontStyle = FontStyle.Italic;

            // Three level rows; regions with fewer deactivate the spares.
            atlasRows = new Button[3];
            float[] rowY = { 0.48f, 0.34f, 0.20f };
            for (int i = 0; i < atlasRows.Length; i++)
            {
                int slot = i; // captured for the delegate
                atlasRows[i] = MakeButton(atlasPanel.transform, "",
                    new Vector2(0.5f, rowY[i]), new Vector2(0f, 0f),
                    new Vector2(560f, 60f),
                    delegate { AtlasRowClicked(slot); }, 24);
            }

            atlasPrev = MakeButton(atlasPanel.transform, "‹",
                new Vector2(0.10f, 0.34f), new Vector2(0f, 0f),
                new Vector2(90f, 100f), delegate { FlipAtlasRegion(-1); }, 44);
            atlasNext = MakeButton(atlasPanel.transform, "›",
                new Vector2(0.90f, 0.34f), new Vector2(0f, 0f),
                new Vector2(90f, 100f), delegate { FlipAtlasRegion(1); }, 44);
            atlasPageLabel = MakeText(atlasPanel.transform, "RegionLabel", "",
                20, new Color(0.85f, 0.87f, 0.92f), TextAnchor.MiddleCenter,
                new Vector2(0.44f, 0.545f), new Vector2(0.56f, 0.59f), 0f, 0f, 0f, 0f);

            atlasBack = MakeButton(atlasPanel.transform, "BACK",
                new Vector2(0.5f, 0.09f), new Vector2(0f, 0f),
                new Vector2(260f, 64f), delegate { CloseAtlas(); });

            WireAtlasNav();
            atlasPanel.SetActive(false);
        }

        /// A row was activated (gamepad Submit or click): play its level.
        /// The interactable flag already gates locked levels; this is the
        /// belt to those braces.
        void AtlasRowClicked(int slot)
        {
            if (atlasRegion < 0 || atlasRegion >= LevelLibrary.Regions.Length)
                return;
            LevelLibrary.Region region = LevelLibrary.Regions[atlasRegion];
            int index = region.First + slot;
            if (slot >= region.Count) return;
            if (index > SaveSystem.UnlockedLevel) return;
            GameManager.Instance.PlayLevel(index);
        }

        void FlipAtlasRegion(int dir)
        {
            int count = LevelLibrary.Regions.Length;
            // Wrap-around: the atlas is a loop, like the festival.
            atlasRegion = (atlasRegion + dir + count) % count;
            RefreshAtlas();
            WireAtlasNav();
        }

        public void ShowAtlas()
        {
            atlasRegion = 0;
            RefreshAtlas();
            WireAtlasNav();
            AnimateShow(atlasPanel);
            // Land on the frontier: the first not-yet-cleared level of the
            // first region that still has one, else the first row.
            Focus(FirstAtlasRow());
            AudioManager.Instance.PlayPanel(true);
        }

        public void CloseAtlas()
        {
            if (atlasPanel == null || !atlasPanel.activeSelf) return;
            AnimateHide(atlasPanel);
            EventSystem es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null)
                es.SetSelectedGameObject(null);
            AudioManager.Instance.PlayPanel(false);
            Focus(playButton); // the menu is underneath; hand focus back
        }

        public bool AtlasOpen
        {
            get { return atlasPanel != null && atlasPanel.activeSelf; }
        }

        Button FirstAtlasRow()
        {
            LevelLibrary.Region region = LevelLibrary.Regions[atlasRegion];
            for (int i = 0; i < region.Count && i < atlasRows.Length; i++)
            {
                int index = region.First + i;
                if (index <= SaveSystem.UnlockedLevel &&
                    atlasRows[i].gameObject.activeInHierarchy &&
                    atlasRows[i].interactable)
                    return atlasRows[i];
            }
            return atlasRows[0];
        }

        void RefreshAtlas()
        {
            if (atlasRows == null) return;
            LevelLibrary.Region region =
                LevelLibrary.Regions[Mathf.Clamp(atlasRegion, 0,
                    LevelLibrary.Regions.Length - 1)];

            int regionStars = 0;
            for (int i = 0; i < region.Count; i++)
                regionStars += SaveSystem.Stars(region.First + i);
            atlasHeader.text = "REGION " + region.Roman + " — " +
                region.Name.ToUpper();
            atlasStarsTotal.text = "STARS " + SaveSystem.TotalStars(
                LevelLibrary.Levels.Length) + " / " +
                LevelLibrary.Levels.Length * 3 +
                "        REGION " + regionStars + " / " + region.Count * 3;

            string milestone = LevelLibrary.Levels[
                region.First + region.Count - 1].Milestone;
            atlasMilestone.text = string.IsNullOrEmpty(milestone)
                ? "Charted skies, drawn in Pip's small, determined handwriting."
                : milestone;
            atlasPageLabel.text = (atlasRegion + 1) + " / " +
                LevelLibrary.Regions.Length;

            for (int i = 0; i < atlasRows.Length; i++)
            {
                bool used = i < region.Count;
                if (atlasRows[i].gameObject.activeSelf != used)
                    atlasRows[i].gameObject.SetActive(used);
                if (!used) continue;

                int index = region.First + i;
                LevelDefinition def = LevelLibrary.Levels[index];
                bool unlocked = index <= SaveSystem.UnlockedLevel;
                atlasRows[i].interactable = unlocked;

                int stars = SaveSystem.Stars(index);
                float best = SaveSystem.BestTime(index);
                string detail;
                if (!unlocked) detail = "LOCKED";
                else if (best < 0f) detail = "Cleared awaits — no time yet";
                else
                {
                    string medal = def.MedalFor(best);
                    detail = "Stars " + stars + "/3" +
                        (medal != "" ? "  ·  " + medal : "") +
                        "  ·  Best " + FormatTime(best);
                }
                Text label = atlasRows[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.text = "LEVEL " + (index + 1) + "  ·  " + def.Name +
                        "\n" + detail;

                Image img = atlasRows[i].targetGraphic as Image;
                if (img != null)
                    img.color = unlocked ? onColor : lockedColor;
                FocusFX fx = atlasRows[i].GetComponent<FocusFX>();
                if (fx != null) fx.RefreshRestColor();
            }
        }

        /// Gamepad navigation for the atlas (D5): the rows chain
        /// vertically with the region arrows flanking horizontally, and
        /// BACK hangs under the last row. Rewired per region — regions
        /// with fewer levels deactivate their spare rows.
        void WireAtlasNav()
        {
            if (atlasRows == null) return;
            LevelLibrary.Region region =
                LevelLibrary.Regions[Mathf.Clamp(atlasRegion, 0,
                    LevelLibrary.Regions.Length - 1)];
            for (int i = 0; i < atlasRows.Length; i++)
            {
                Button up = i > 0 && i - 1 < region.Count
                    ? atlasRows[i - 1] : null;
                Button down = i + 1 < region.Count ? atlasRows[i + 1] : null;
                MenuNav.Set(atlasRows[i], up, down, atlasPrev, atlasNext);
            }
            Button last = atlasRows[region.Count - 1];
            MenuNav.Set(atlasPrev, null, atlasRows[0], null, null);
            MenuNav.Set(atlasNext, null, atlasRows[0], null, null);
            MenuNav.Set(atlasBack, last, null, null, null);
            Navigation backNav = last.navigation;
            backNav.selectOnDown = atlasBack;
            last.navigation = backNav;
        }

        void ShowSettings()
        {
            // From pause, Settings becomes a layer: the pause panel draws
            // above Settings in sibling order, so it hides while Settings
            // is up and comes straight back on close — the run stays
            // paused underneath the whole time.
            settingsOverPause = GameManager.Instance != null &&
                GameManager.Instance.State == GameState.Paused;
            if (settingsOverPause) HidePaused();
            RefreshSettings();
            AnimateShow(settingsPanel);
            if (settingsButtons != null && settingsButtons.Length > 0)
                Focus(settingsButtons[0]);
            AudioManager.Instance.PlayPanel(true);
        }

        /// Closes the settings panel (same as pressing its BACK button).
        public void CloseSettings()
        {
            AnimateHide(settingsPanel);
            if (settingsOverPause && GameManager.Instance != null &&
                GameManager.Instance.State == GameState.Paused)
                ShowPaused();
            else Focus(playButton); // opened from the menu: hand focus back
            settingsOverPause = false;
            AudioManager.Instance.PlayPanel(false);
        }

        void RefreshSettings()
        {
            if (settingsLabels == null) return;
            ApplyLabel(settingsLabels[0], "Sound", SaveSystem.SoundOn);
            ApplyLabel(settingsLabels[1], "Screen Shake", SaveSystem.ShakeOn);
            ApplyLabel(settingsLabels[2], "Haptics", SaveSystem.HapticsOn);
            ApplyLabel(settingsLabels[3], "Shadows", SaveSystem.ShadowsOn);
            ApplyLabel(settingsLabels[4], "Left-handed Controls", SaveSystem.LeftyOn);
            if (settingsLabels.Length > 5 && settingsLabels[5] != null)
            {
                // A mode, not an on/off: the label names the value, and the
                // row tint follows it like every other toggle.
                settingsLabels[5].text = "Text Size:  " +
                    (SaveSystem.TextLargeOn ? "LARGE" : "NORMAL");
                Image img = settingsLabels[5].transform.parent.GetComponent<Image>();
                if (img != null) img.color = SaveSystem.TextLargeOn ? onColor : offColor;
            }
            if (settingsLabels.Length > 6)
                ApplyLabel(settingsLabels[6], "Fullscreen", SaveSystem.FullscreenOn);
            // Keep the focus highlight honest after the tint repaints (D5).
            for (int i = 0; i < settingsButtons.Length; i++)
            {
                FocusFX fx = settingsButtons[i].GetComponent<FocusFX>();
                if (fx != null) fx.RefreshRestColor();
            }
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
                SaveSystem.TextLargeOn = !SaveSystem.TextLargeOn;
                ApplyTextSize();
            }
            else if (index == 6)
            {
                SaveSystem.FullscreenOn = !SaveSystem.FullscreenOn;
                Screen.fullScreenMode = SaveSystem.FullscreenOn
                    ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }
            // A flipped setting answers with its own blip: up when it
            // turned on, down when it turned off. (Index 5 reads its state
            // after ApplyTextSize, so the blip matches what is on screen.)
            bool[] states =
            {
                SaveSystem.SoundOn, SaveSystem.ShakeOn, SaveSystem.HapticsOn,
                SaveSystem.ShadowsOn, SaveSystem.LeftyOn, SaveSystem.TextLargeOn,
                SaveSystem.FullscreenOn
            };
            if (index >= 0 && index < settingsLabels.Length)
                AudioManager.Instance.PlayUIToggle(states[index]);
            RefreshSettings();
        }

        /// Re-derives every registered label from its designed size. Runs
        /// once at build and whenever Text Size flips; a few hundred
        /// fontSize writes, never per frame.
        void ApplyTextSize()
        {
            float mult = SaveSystem.TextLargeOn ? 1.15f : 1f;
            for (int i = 0; i < scalableTexts.Count; i++)
            {
                if (scalableTexts[i] == null) continue;
                scalableTexts[i].fontSize =
                    Mathf.Max(12, Mathf.RoundToInt(scalableBaseSizes[i] * mult));
            }
        }

        void BuildPause(Transform canvas)
        {
            pausePanel = MakePanel(canvas, "PausePanel", new Color(0f, 0f, 0f, 0.6f));

            Text title = MakeText(pausePanel.transform, "Title", "PAUSED", 72,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.56f), new Vector2(1f, 0.72f), 0f, 0f, 0f, 0f);
            title.fontStyle = FontStyle.Bold;

            pauseResumeButton = MakeButton(pausePanel.transform, "RESUME",
                new Vector2(0.5f, 0.40f), new Vector2(0f, 0f),
                new Vector2(360f, 84f), delegate { GameManager.Instance.ResumeGame(); });

            pauseRestartButton = MakeButton(pausePanel.transform, "RESTART LEVEL",
                new Vector2(0.5f, 0.285f), new Vector2(0f, 0f),
                new Vector2(360f, 68f),
                delegate { GameManager.Instance.PlayLevel(GameManager.Instance.CurrentLevel); });

            // Settings joins the pause menu (D9): sound/haptics/text size
            // are adjustable mid-run, without abandoning the level. MENU and
            // SETTINGS share the bottom row; the touch floor widens both,
            // which still clears side by side.
            pauseMenuButton = MakeButton(pausePanel.transform, "MENU",
                new Vector2(0.5f - 0.13f, 0.165f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { GameManager.Instance.GoToMenu(); });

            pauseSettingsButton = MakeButton(pausePanel.transform, "SETTINGS",
                new Vector2(0.5f + 0.13f, 0.165f), new Vector2(0f, 0f),
                new Vector2(260f, 62f), delegate { ShowSettings(); });

            MenuNav.Set(pauseResumeButton, null, pauseRestartButton, null, null);
            MenuNav.Set(pauseRestartButton, pauseResumeButton, pauseMenuButton,
                null, pauseSettingsButton);
            MenuNav.Set(pauseMenuButton, pauseRestartButton, null, null,
                pauseSettingsButton);
            MenuNav.Set(pauseSettingsButton, pauseRestartButton, null,
                pauseMenuButton, null);

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
            // Touch paging opens on the page holding the next unplayed
            // level, so "continue" is where the eyes land first.
            if (Input.touchSupported && levelButtons != null &&
                levelButtons.Length > 0)
            {
                int frontier = Mathf.Clamp(SaveSystem.UnlockedLevel,
                    0, levelButtons.Length - 1);
                levelPage = frontier / PageSize;
            }
            RefreshMenu();
            if (menuQuote != null) menuQuote.text = Story.MenuQuote();
            UpdateVisitRecap();
            AnimateShow(menuPanel);
            Focus(playButton);
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
            // A fresh level must repaint the HUD even when the cached values
            // happen to match what the last run displayed (restart with
            // identical gem count, etc.).
            hudCacheLevel = -1;
            hudCacheGems = -1;
            hudCacheTotal = -1;
            hudCacheLives = -1;
            hudCacheDeciseconds = -1;
            hudPanel.SetActive(true); // in-play: instant, never a transition
            // A streak carried across a portal re-crowns the fresh Pip.
            ComboCrown.Notify(AudioManager.CurrentStreak);
        }

        public void ShowPaused()
        {
            AnimateShow(pausePanel);
            Focus(pauseResumeButton);
        }

        public void HidePaused()
        {
            AnimateHide(pausePanel);
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
            // Celebration rain: a perfect gem run earns the full confetti —
            // over the Two Suns it mixes in petals — and a new best time
            // gets a smaller burst of its own. World-space at Pip, so it
            // reads through the win dim without ever flashing the screen.
            if (GameBootstrap.Player != null)
            {
                Vector3 at = GameBootstrap.Player.transform.position
                    + Vector3.up * 2f;
                if (stars >= 3)
                    Fx.Confetti(at, 60, TwoSunsRegion(level));
                else if (newRecord)
                    Fx.Confetti(at, 30);
            }
            AnimateShow(winPanel);
            Focus(winNextButton);
        }

        /// Pack 4's celebration lap: levels of the Two Suns region mix
        /// petals into the confetti. Read-only lookup of the region the
        /// level belongs to, in play order.
        static bool TwoSunsRegion(int level)
        {
            for (int i = 0; i < LevelLibrary.Regions.Length; i++)
            {
                LevelLibrary.Region region = LevelLibrary.Regions[i];
                if (level >= region.First && level < region.First + region.Count)
                    return region.Name == "The Two Suns";
            }
            return false;
        }

        public void ShowGameOver()
        {
            HideAll();
            AnimateShow(overPanel);
            Focus(overTryButton);
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
            AnimateShow(completePanel);
            Focus(hasPages ? (Button)completeNext : completeMenuButton);
        }

        /// Push-based HUD update with a change-cache: a steady frame does
        /// no string formatting and no .text writes. The clock formats only
        /// when its displayed tenth-of-a-second moved, which caps it at ten
        /// allocations per second instead of sixty.
        public void UpdateHUD(int level, int gems, int total, int lives, float time)
        {
            if (hudLevel != null && level != hudCacheLevel)
            {
                hudCacheLevel = level;
                hudLevel.text = "LV " + (level + 1);
            }
            if (hudGems != null && (gems != hudCacheGems || total != hudCacheTotal))
            {
                int previous = hudCacheGems;
                hudCacheGems = gems;
                hudCacheTotal = total;
                hudGems.text = string.Format("Gems  {0} / {1}", gems, total);
                // Same star math the win screen uses: a pickup that crosses
                // the 2-star or 3-star line makes the counter pop harder.
                // Still change-cached — this only runs when the count moved.
                bool crossed = previous >= 0 &&
                    ((gems >= total && previous < total) ||
                     (gems * 2 >= total && previous * 2 < total));
                PulseGemCounter(crossed);
            }
            int decis = (int)(time * 10f);
            if (hudTime != null && decis != hudCacheDeciseconds)
            {
                hudCacheDeciseconds = decis;
                hudTime.text = FormatTime(time);
            }
            if (hudLives != null && lives != hudCacheLives)
            {
                hudCacheLives = lives;
                hudLives.text = string.Format("Lives  {0}", lives);
            }
        }

        /// The gem counter's spring pop (≈1 → 1.25/1.45 → 1) plus a brief
        /// gold flash back to white. Unscaled time; ends on the exact rest
        /// pose. The sequence token retires the previous pulse when a chain
        /// of pickups lands within one tween, so they never fight.
        void PulseGemCounter(bool crossed)
        {
            if (hudGems == null) return;
            int seq = ++gemPulseSeq;
            RectTransform rt = hudGems.rectTransform;
            float amp = crossed ? 0.45f : 0.25f;
            float seconds = crossed ? 0.4f : 0.25f;
            Tweener.Value(0f, 1f, seconds, delegate(float k)
            {
                if (seq != gemPulseSeq) return;
                float s = 1f + amp * Mathf.Sin(k * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1f);
                hudGems.color = Color.Lerp(ArtLib.Gold, Color.white, k);
            }, delegate
            {
                if (seq != gemPulseSeq) return;
                rt.localScale = Vector3.one;
                hudGems.color = Color.white;
            });
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

        /// True while the quit-confirmation dialog is on screen; Enter and
        /// the menu shortcuts must not fire underneath it.
        public bool QuitOpen =>
            quitConfirmPanel != null && quitConfirmPanel.activeSelf;

        /// Shows the quit-confirmation dialog ("QUIT THE GAME?"). Quitting
        /// always goes through this dialog on desktop — Esc/back never quits
        /// instantly. QUIT invokes onConfirmed then quits; CANCEL (or the
        /// back stack) just closes the dialog.
        public void ShowQuitConfirm(System.Action onConfirmed)
        {
            if (quitConfirmPanel == null) BuildQuitConfirm();
            quitConfirmedAction = onConfirmed;
            quitConfirmPanel.SetActive(true);
            Focus(cancelButton); // safe default: focus never starts on QUIT
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

            quitButton = MakeButton(quitConfirmPanel.transform, "QUIT",
                new Vector2(0.5f - 0.13f, 0.38f), new Vector2(0f, 0f),
                new Vector2(260f, 84f), delegate { ConfirmQuit(); });

            cancelButton = MakeButton(quitConfirmPanel.transform, "CANCEL",
                new Vector2(0.5f + 0.13f, 0.38f), new Vector2(0f, 0f),
                new Vector2(260f, 84f), delegate { CloseQuitConfirm(); });

            // Pair left/right; CANCEL is the safe first-selected default.
            MenuNav.Set(quitButton, null, null, null, cancelButton);
            MenuNav.Set(cancelButton, null, null, quitButton, null);

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

        /// Controller-first menus (D5): every panel sets a first-selected
        /// object when it opens, so a gamepad (or arrows) always lands on
        /// a visible, sensible default. Guarded for EditMode probes where
        /// no EventSystem exists.
        static void Focus(Button target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            EventSystem es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(target.gameObject);
        }

        // ---------- Panel transitions ----------

        /// Menu-style panels arrive with a 0.2 s scale-and-fade in (0.92 → 1
        /// scale, alpha 0 → 1) and leave with a quick 0.12 s fade before
        /// SetActive(false). Unscaled time (Tweener), ease-out-quad, and both
        /// always end on the exact rest pose (scale 1, alpha 1) so the
        /// safe-area layout is never left rescaled. The in-play HUD is
        /// excluded — its show/hide stays instant.
        CanvasGroup PanelGroup(GameObject panel)
        {
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();
            return group;
        }

        int PanelGeneration(GameObject panel, bool advance)
        {
            int gen;
            if (!panelGenerations.TryGetValue(panel, out gen)) gen = 0;
            if (advance) panelGenerations[panel] = ++gen;
            return gen;
        }

        void AnimateShow(GameObject panel)
        {
            CanvasGroup group = PanelGroup(panel);
            RectTransform rt = (RectTransform)panel.transform;
            int gen = PanelGeneration(panel, true);
            panel.SetActive(true);
            group.alpha = 0f;
            Tweener.Value(0f, 1f, 0.2f, delegate(float k)
            {
                if (gen != PanelGeneration(panel, false)) return;
                group.alpha = k;
                float s = Mathf.Lerp(0.92f, 1f, k);
                rt.localScale = new Vector3(s, s, 1f);
            }, delegate
            {
                if (gen != PanelGeneration(panel, false)) return;
                group.alpha = 1f;
                rt.localScale = Vector3.one;
            });
        }

        void AnimateHide(GameObject panel)
        {
            CanvasGroup group = PanelGroup(panel);
            int gen = PanelGeneration(panel, true);
            Tweener.Value(1f, 0f, 0.12f, delegate(float k)
            {
                if (gen != PanelGeneration(panel, false)) return;
                group.alpha = k;
            }, delegate
            {
                if (gen != PanelGeneration(panel, false)) return;
                group.alpha = 1f; // reset for the next show
                panel.SetActive(false);
            });
        }

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
            self.atlasPanel.SetActive(false);
            self.pausePanel.SetActive(false);
            self.introPanel.SetActive(false);
            // No panel owns focus while nothing is on screen — gameplay
            // must never leave a selectable behind for Submit to hit.
            EventSystem es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null)
                es.SetSelectedGameObject(null);
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
            // Registered for the Text Size setting: base size remembered,
            // so Large mode re-derives instead of compounding.
            scalableTexts.Add(text);
            scalableBaseSizes.Add(size);
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
            Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick,
            int labelSize = 28)
        {
            GameObject go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = Fx.CircleSprite();
            img.type = Image.Type.Sliced;
            img.color = onColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            // Visible focus for gamepad/keyboard menus (D5).
            go.AddComponent<FocusFX>();
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
            text.fontSize = labelSize;
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
            // Registered for the Text Size setting, like every other label.
            scalableTexts.Add(text);
            scalableBaseSizes.Add(labelSize);
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
