using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GemRush.Tests
{
    /// <summary>
    /// Headless UI audit (docs/UIUX-Multiplatform-Directives.md queue):
    /// structural checks that the code-built UI stays correct as screens
    /// grow — every panel under the safe-area root, the level grid matching
    /// the library, and the safe-area fitter never producing anchors that
    /// would push screens off a device. Pure hierarchy math; no rendering.
    /// </summary>
    [TestFixture]
    public class UIAuditTests
    {
        static readonly string[] PanelFields =
        {
            "menuPanel", "hudPanel", "winPanel", "overPanel",
            "completePanel", "settingsPanel", "atlasPanel", "pausePanel",
            "photoPanel"
        };

        static object InvokePrivate(object target, string method, params object[] args)
        {
            MethodInfo info = target.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "expected private method " + method);
            return info.Invoke(target, args);
        }

        static object InvokeStaticPrivate(System.Type type, string method,
            params object[] args)
        {
            MethodInfo info = type.GetMethod(method,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "expected private static method " + method);
            return info.Invoke(null, args);
        }

        static object GetPrivate(object target, string field)
        {
            return target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetStaticPrivate(System.Type type, string field, object value)
        {
            type.GetField(field,
                BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
        }

        // ------------------------------------------------------------------
        // Layout probes. The screens are built for a 1600x900 reference
        // (CanvasScaler matchWidthOrHeight = 1), so a panel's rect resolves
        // to a real band in reference units once the parent's height is
        // known. These helpers exist because the earlier settings test
        // compared ANCHOR values, which pass while the rendered rects
        // overlap by 60 units — the bug that motivated this harness.
        // ------------------------------------------------------------------

        /// A vertical/horizontal band in reference units.
        struct Band
        {
            public float Min, Max;
            public float Span { get { return Max - Min; } }
            public bool Overlaps(Band other)
            {
                return Min < other.Max && other.Min < Max;
            }
            public float OverlapAmount(Band other)
            {
                return Mathf.Min(Max, other.Max) - Mathf.Max(Min, other.Min);
            }
        }

        /// Resolve a point-anchored button's Y band in reference units.
        /// Buttons use anchorMin == anchorMax (a point anchor) with a pivot
        /// of (0.5, 0.5) and anchoredPosition (0,0), so the rect is centred
        /// on the anchor: the band is centre +/- half the size.
        static Band YBand(Button b, float parentHeight = 900f)
        {
            RectTransform rt = b.GetComponent<RectTransform>();
            float centre = rt.anchorMin.y * parentHeight;
            float half = rt.sizeDelta.y * 0.5f;
            Band band;
            band.Min = centre - half;
            band.Max = centre + half;
            return band;
        }

        static Band XBand(Button b, float parentWidth = 1600f)
        {
            RectTransform rt = b.GetComponent<RectTransform>();
            float centre = rt.anchorMin.x * parentWidth;
            float half = rt.sizeDelta.x * 0.5f;
            Band band;
            band.Min = centre - half;
            band.Max = centre + half;
            return band;
        }

        /// Fail with both spans quoted, so a future regression is readable
        /// rather than a bare assertion.
        static void AssertNoOverlap(Band a, Band b, string label)
        {
            if (a.Overlaps(b))
                Assert.Fail(label + ": overlap of " +
                    a.OverlapAmount(b).ToString("0.0") + " ref units (" +
                    a.Min.ToString("0.0") + ".." + a.Max.ToString("0.0") +
                    " vs " + b.Min.ToString("0.0") + ".." + b.Max.ToString("0.0") + ")");
        }

        /// Build the UI with the TOUCH layout forced on. The editor always
        /// reports Input.touchSupported == false, so without this the whole
        /// mobile layout is untestable — and that blindness is exactly how
        /// the settings grid came to overlap its BACK button unnoticed.
        static UIManager BuildUI(bool touch)
        {
            UIManager.ForceTouchLayoutForTests = touch;
            GameObject go = new GameObject(touch ? "UIProbeTouch" : "UIProbeDesk",
                typeof(RectTransform));
            go.AddComponent<UIManager>();
            InvokePrivate(go.GetComponent<UIManager>(), "Awake");
            return go.GetComponent<UIManager>();
        }

        static void TearDownUI(UIManager ui)
        {
            UIManager.ForceTouchLayoutForTests = false;
            if (ui != null) Object.DestroyImmediate(ui.gameObject);
            SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
        }

        // The touch settings grid must not overlap itself or its BACK button.
        // This is the test the old anchor-comparison version should have been:
        // it caught a real 60-unit overlap between the bottom row and BACK
        // that the anchor check passed straight through.
        [Test]
        public void Settings_TouchLayout_NoOverlap()
        {
            UIManager ui = BuildUI(true);
            try
            {
                AssertSettingsLayoutSane(ui, "touch");
            }
            finally
            {
                TearDownUI(ui);
            }
        }

        // The DESKTOP column carries the extra Fullscreen row, so it is the
        // longer list of the two. It was passing the overlap assertions while
        // running off the bottom of the screen entirely.
        [Test]
        public void Settings_DesktopLayout_NoOverlap()
        {
            UIManager ui = BuildUI(false);
            try
            {
                AssertSettingsLayoutSane(ui, "desktop");
            }
            finally
            {
                TearDownUI(ui);
            }
        }

        static void AssertSettingsLayoutSane(UIManager ui, string label)
        {
            Button[] rows = GetPrivate(ui, "settingsButtons") as Button[];
            Button back = GetPrivate(ui, "settingsBackButton") as Button;
            Assert.IsNotNull(rows, label + ": settings rows built");
            Assert.IsNotNull(back, label + ": settings BACK built");
            Assert.GreaterOrEqual(rows.Length, 8, label + ": rows present");

            for (int i = 0; i < rows.Length; i++)
            {
                for (int j = i + 1; j < rows.Length; j++)
                {
                    // Only same-column pairs can collide vertically.
                    if (Mathf.Abs(XBand(rows[i]).Min - XBand(rows[j]).Min) < 1f)
                        AssertNoOverlap(YBand(rows[i]), YBand(rows[j]),
                            label + ": settings row " + i + " vs row " + j);
                }
            }

            Band backBand = YBand(back);
            for (int i = 0; i < rows.Length; i++)
            {
                AssertNoOverlap(YBand(rows[i]), backBand,
                    label + ": settings row " + i + " vs BACK");
            }

            // On-screen containment, both ends. Its absence is what let a
            // real off-screen desktop column ship.
            for (int i = 0; i < rows.Length; i++)
            {
                Band b = YBand(rows[i]);
                Assert.GreaterOrEqual(b.Min, 0f, label + ": row " + i + " on screen");
                Assert.LessOrEqual(b.Max, 900f, label + ": row " + i + " on screen");
            }
            Assert.GreaterOrEqual(backBand.Min, 0f, label + ": BACK on screen");
            Assert.LessOrEqual(backBand.Max, 900f, label + ": BACK on screen");
        }

        // The pause stack (RESUME / RESTART / PHOTO / MENU+SETTINGS) is the
        // tightest vertical stack in the game: at the old floor its lowest gap
        // was 1 unit short before any change.
        [Test]
        public void Pause_TouchLayout_NoOverlap()
        {
            UIManager ui = BuildUI(true);
            try
            {
                Button resume = GetPrivate(ui, "pauseResumeButton") as Button;
                Button restart = GetPrivate(ui, "pauseRestartButton") as Button;
                Button menu = GetPrivate(ui, "pauseMenuButton") as Button;
                Assert.IsNotNull(resume, "RESUME built");
                Assert.IsNotNull(restart, "RESTART built");
                Assert.IsNotNull(menu, "MENU built");

                AssertNoOverlap(YBand(resume), YBand(restart), "RESUME vs RESTART");
                AssertNoOverlap(YBand(restart), YBand(menu), "RESTART vs MENU row");

                // PHOTO is desktop-only, so the touch build may not have it.
                Button photo = GetPrivate(ui, "pausePhotoButton") as Button;
                if (photo != null)
                {
                    AssertNoOverlap(YBand(restart), YBand(photo),
                        "RESTART vs PHOTO");
                    AssertNoOverlap(YBand(photo), YBand(menu), "PHOTO vs MENU row");
                }

                for (int i = 0; i < 4; i++)
                {
                    Button b = i == 0 ? resume : i == 1 ? restart
                        : i == 2 ? (photo != null ? photo : menu) : menu;
                    if (b == null) continue;
                    Band band = YBand(b);
                    Assert.GreaterOrEqual(band.Min, 0f, "pause button on screen");
                    Assert.LessOrEqual(band.Max, 900f, "pause button on screen");
                }
            }
            finally
            {
                TearDownUI(ui);
            }
        }

        // Win and game-over screens: the primary action and the row beneath it.
        [Test]
        public void ResultScreens_TouchLayout_NoOverlap()
        {
            UIManager ui = BuildUI(true);
            try
            {
                Button winNext = GetPrivate(ui, "winNextButton") as Button;
                Button winReplay = GetPrivate(ui, "winReplayButton") as Button;
                Button winMenu = GetPrivate(ui, "winMenuButton") as Button;
                AssertNoOverlap(YBand(winNext), YBand(winReplay),
                    "win NEXT vs REPLAY");
                AssertNoOverlap(XBand(winReplay), XBand(winMenu),
                    "win REPLAY vs MENU");

                Button overTry = GetPrivate(ui, "overTryButton") as Button;
                Button overMenu = GetPrivate(ui, "overMenuButton") as Button;
                AssertNoOverlap(YBand(overTry), YBand(overMenu),
                    "game over TRY AGAIN vs MENU");
            }
            finally
            {
                TearDownUI(ui);
            }
        }

        [Test]
        public void SafeArea_KeepsAnchorsInsideTheView()
        {
            GameObject go = new GameObject("SafeAreaProbe", typeof(RectTransform));
            go.AddComponent<SafeArea>();
            try
            {
                InvokePrivate(go.GetComponent<SafeArea>(), "Apply");
                RectTransform rt = (RectTransform)go.transform;
                Assert.GreaterOrEqual(rt.anchorMin.x, 0f, "anchorMin.x inside view");
                Assert.GreaterOrEqual(rt.anchorMin.y, 0f, "anchorMin.y inside view");
                Assert.LessOrEqual(rt.anchorMax.x, 1f, "anchorMax.x inside view");
                Assert.LessOrEqual(rt.anchorMax.y, 1f, "anchorMax.y inside view");
                Assert.LessOrEqual(rt.anchorMin.x, rt.anchorMax.x, "min <= max (x)");
                Assert.LessOrEqual(rt.anchorMin.y, rt.anchorMax.y, "min <= max (y)");
                Assert.AreEqual(Vector2.zero, rt.offsetMin, "no residual offsets");
                Assert.AreEqual(Vector2.zero, rt.offsetMax, "no residual offsets");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UIManager_EveryScreenBuildsUnderTheSafeRoot()
        {
            GameObject go = new GameObject("UIProbe", typeof(RectTransform));
            go.AddComponent<UIManager>();
            try
            {
                InvokePrivate(go.GetComponent<UIManager>(), "Awake");

                // The safe-area root exists and every screen hangs off it,
                // so no notch or gesture bar can clip a panel by construction.
                Transform safeRoot = go.transform.Find("UICanvas/SafeRoot");
                Assert.IsNotNull(safeRoot, "SafeRoot under UICanvas");
                Assert.IsNotNull(safeRoot.GetComponent<SafeArea>(),
                    "SafeRoot carries the SafeArea fitter");
                foreach (string field in PanelFields)
                {
                    GameObject panel = GetPrivate(go.GetComponent<UIManager>(), field)
                        as GameObject;
                    Assert.IsNotNull(panel, field + " built");
                    Assert.IsTrue(panel.transform.IsChildOf(safeRoot),
                        field + " parented under SafeRoot");
                    Assert.IsFalse(panel.activeSelf, field + " hidden after build");
                }

                // The level grid always mirrors the library, whatever the
                // paging layer shows.
                Button[] levelButtons = GetPrivate(
                    go.GetComponent<UIManager>(), "levelButtons") as Button[];
                Assert.IsNotNull(levelButtons, "level buttons built");
                Assert.AreEqual(LevelLibrary.Levels.Length, levelButtons.Length,
                    "one button per library level");

                // Paging is universal: after ShowMenu, off-page buttons are
                // inactive, and the two page rows cannot overlap (the
                // 40-level desktop overlap regression — rows 33 units apart
                // under 50-unit buttons).
                int perRow = (int)GetPrivate(
                    go.GetComponent<UIManager>(), "menuPerRow");
                Assert.GreaterOrEqual(levelButtons.Length, 2 * perRow,
                    "enough levels to exercise paging");
                RectTransform topRow = levelButtons[0].GetComponent<RectTransform>();
                RectTransform bottomRow =
                    levelButtons[perRow].GetComponent<RectTransform>();
                Assert.AreEqual(0.32f, topRow.anchorMin.y, 0.001f,
                    "page row 0 anchor");
                Assert.AreEqual(0.165f, bottomRow.anchorMin.y, 0.001f,
                    "page row 1 anchor");
                float rowGapUnits = 0.155f * 900f; // row anchors, ref units
                float halfSum = (topRow.sizeDelta.y + bottomRow.sizeDelta.y) * 0.5f;
                Assert.LessOrEqual(halfSum, rowGapUnits,
                    "grid rows can never overlap");

                ((UIManager)go.GetComponent<UIManager>()).ShowMenu();
                Assert.IsFalse(levelButtons[2 * perRow].gameObject.activeSelf,
                    "off-page buttons are inactive once the menu shows");
                Assert.IsTrue(levelButtons[0].gameObject.activeSelf,
                    "page 0 buttons are visible");

                Assert.IsFalse(((UIManager)go.GetComponent<UIManager>()).SettingsOpen,
                    "settings closed after build");
            }
            finally
            {
                Object.DestroyImmediate(go);
                SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
            }
        }

        [Test]
        public void UIManager_TextSizeSetting_ReappliesFromBaseSizes()
        {
            GameObject go = new GameObject("UIProbe2", typeof(RectTransform));
            go.AddComponent<UIManager>();
            bool originalLarge = SaveSystem.TextLargeOn;
            try
            {
                InvokePrivate(go.GetComponent<UIManager>(), "Awake");
                SaveSystem.TextLargeOn = false;
                InvokePrivate(go.GetComponent<UIManager>(), "ApplyTextSize");
                Text instructions = go.GetComponent<UIManager>()
                    .transform.Find("UICanvas/SafeRoot/MenuPanel/Instructions")
                    .GetComponent<Text>();
                Assert.AreEqual(22, instructions.fontSize,
                    "default size restored from the registered base");

                // Prose wraps inside its band (the mission card used to run
                // off both screen edges); labels and HUD numbers must not
                // re-flow.
                Text mission = go.GetComponent<UIManager>()
                    .transform.Find("UICanvas/SafeRoot/IntroPanel/IntroMission")
                    .GetComponent<Text>();
                Assert.AreEqual(HorizontalWrapMode.Wrap, mission.horizontalOverflow,
                    "mission prose wraps");
                Text quote = go.GetComponent<UIManager>()
                    .transform.Find("UICanvas/SafeRoot/MenuPanel/MenuQuote")
                    .GetComponent<Text>();
                Assert.AreEqual(HorizontalWrapMode.Wrap, quote.horizontalOverflow,
                    "menu quote wraps");
                Text hud = go.GetComponent<UIManager>()
                    .transform.Find("UICanvas/SafeRoot/HudPanel/HudDynamic/Gems")
                    .GetComponent<Text>();
                Assert.AreEqual(HorizontalWrapMode.Overflow, hud.horizontalOverflow,
                    "HUD numbers never re-flow");

                SaveSystem.TextLargeOn = true;
                InvokePrivate(go.GetComponent<UIManager>(), "ApplyTextSize");
                Assert.AreEqual(
                    Mathf.RoundToInt(22 * 1.15f), instructions.fontSize,
                    "Large mode scales from the same base, not compounding");
            }
            finally
            {
                SaveSystem.TextLargeOn = originalLarge;
                Object.DestroyImmediate(go);
                SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
            }
        }

        [Test]
        public void UIManager_AtlasButton_IsReachableInMenuNav()
        {
            GameObject go = new GameObject("UIProbe3", typeof(RectTransform));
            go.AddComponent<UIManager>();
            try
            {
                InvokePrivate(go.GetComponent<UIManager>(), "Awake");
                UIManager ui = go.GetComponent<UIManager>();
                Button atlas = GetPrivate(ui, "atlasMenuButton") as Button;
                Button settings = GetPrivate(ui, "menuSettingsButton") as Button;
                Button[] levels = GetPrivate(ui, "levelButtons") as Button[];
                int perRow = (int)GetPrivate(ui, "menuPerRow");

                Assert.IsNotNull(atlas, "atlas menu button captured for nav");
                Assert.AreEqual(atlas, settings.navigation.selectOnRight,
                    "SETTINGS right steps to ATLAS");
                Assert.AreEqual(settings, atlas.navigation.selectOnLeft,
                    "ATLAS left returns to SETTINGS");
                Assert.IsNull(atlas.navigation.selectOnRight,
                    "ATLAS is the top-right dead end");
                Assert.AreEqual(levels[perRow - 1],
                    atlas.navigation.selectOnDown,
                    "ATLAS down drops into the grid's top-right cell");
            }
            finally
            {
                Object.DestroyImmediate(go);
                SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
            }
        }

        [Test]
        public void UIManager_HideAll_RetiresThePhotoBar()
        {
            GameObject go = new GameObject("UIProbe4", typeof(RectTransform));
            go.AddComponent<UIManager>();
            try
            {
                InvokePrivate(go.GetComponent<UIManager>(), "Awake");
                UIManager ui = go.GetComponent<UIManager>();
                GameObject photoPanel = GetPrivate(ui, "photoPanel") as GameObject;
                GameObject scorecard = GetPrivate(ui, "scorecard") as GameObject;
                Assert.IsNotNull(photoPanel, "photo bar built");

                photoPanel.SetActive(true);
                if (scorecard != null) scorecard.SetActive(true);
                InvokeStaticPrivate(typeof(UIManager), "HideAll");
                Assert.IsFalse(photoPanel.activeSelf,
                    "HideAll retires the photo bar");
                if (scorecard != null)
                    Assert.IsFalse(scorecard.activeSelf,
                        "HideAll retires the share card");
            }
            finally
            {
                Object.DestroyImmediate(go);
                SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
            }
        }

        // The v1.21.0 share-card edit dropped the PNG write, so CAPTURE
        // reported success without saving. These pin the write helper.
        [Test]
        public void TryWritePng_WritesBytes_AndReportsFailureHonestly()
        {
            string dir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "gemrush-tests");
            string path = System.IO.Path.Combine(dir, "probe.png");
            try
            {
                System.IO.Directory.CreateDirectory(dir);
                byte[] payload = { 1, 2, 3, 4 };
                bool ok = (bool)InvokeStaticPrivate(typeof(UIManager),
                    "TryWritePng", path, payload);
                Assert.IsTrue(ok, "write to a valid path succeeds");
                Assert.IsTrue(System.IO.File.Exists(path), "file lands on disk");

                bool bad = (bool)InvokeStaticPrivate(typeof(UIManager),
                    "TryWritePng",
                    System.IO.Path.Combine(dir, "missing-dir", "x.png"), payload);
                Assert.IsFalse(bad, "an unwritable path reports failure");
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                if (System.IO.Directory.Exists(dir))
                    System.IO.Directory.Delete(dir);
            }
        }

        // The mission briefing moved out of the play area: the card used to
        // be a full-screen dim with the text dead-center over the course,
        // and it drew straight through the pause menu (ShowPaused does not
        // call HideAll). Both are pinned here.
        [Test]
        public void UIManager_BriefingBand_SitsInTheBottomStrip_AndPauseRetiresIt()
        {
            GameObject go = new GameObject("UIProbe5", typeof(RectTransform));
            go.AddComponent<UIManager>();
            try
            {
                InvokePrivate(go.GetComponent<UIManager>(), "Awake");
                UIManager ui = go.GetComponent<UIManager>();
                GameObject intro = GetPrivate(ui, "introPanel") as GameObject;
                Assert.IsNotNull(intro, "briefing band built");
                RectTransform band = (RectTransform)intro.transform;
                Assert.LessOrEqual(band.anchorMax.y, 0.20f,
                    "briefing never reaches the middle of the screen");
                Assert.GreaterOrEqual(band.anchorMin.x, 0.05f,
                    "briefing is inset, not a full-screen dim");
                Assert.LessOrEqual(band.anchorMax.x, 0.95f,
                    "briefing is inset, not a full-screen dim");

                // Mission Text defaults off: the briefing is narrated.
                Assert.IsFalse(SaveSystem.MissionTextOn,
                    "text briefing is opt-in by default");

                // Reproduced bug: pausing mid-briefing left the band behind
                // the pause menu. ShowPaused must retire it.
                intro.SetActive(true);
                ui.ShowPaused();
                Assert.IsFalse(intro.activeSelf,
                    "pausing retires the briefing band");
            }
            finally
            {
                Object.DestroyImmediate(go);
                SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
            }
        }

        // Nine desktop settings rows must not collide with the BACK button,
        // and the touch 2-column grid must still hold every row on screen.
        [Test]
        public void UIManager_SettingsRows_FitAboveBack()
        {
            GameObject go = new GameObject("UIProbe6", typeof(RectTransform));
            go.AddComponent<UIManager>();
            try
            {
                InvokePrivate(go.GetComponent<UIManager>(), "Awake");
                UIManager ui = go.GetComponent<UIManager>();
                Button[] rows = GetPrivate(ui, "settingsButtons") as Button[];
                Button back = GetPrivate(ui, "settingsBackButton") as Button;
                Assert.IsNotNull(rows, "settings rows built");
                Assert.IsNotNull(back, "settings BACK built");
                Assert.GreaterOrEqual(rows.Length, 8,
                    "Voice and Mission Text rows are present");

                float lowestRow = 1f;
                float highestRow = 0f;
                for (int i = 0; i < rows.Length; i++)
                {
                    RectTransform rt = rows[i].GetComponent<RectTransform>();
                    if (rt.anchorMin.y < lowestRow) lowestRow = rt.anchorMin.y;
                    if (rt.anchorMin.y > highestRow) highestRow = rt.anchorMin.y;
                }
                float backY = ((RectTransform)back.transform).anchorMin.y;
                Assert.Less(lowestRow, highestRow,
                    "rows are actually laid out in a stack");
                Assert.Greater(lowestRow, backY,
                    "every settings row clears the BACK button");
            }
            finally
            {
                Object.DestroyImmediate(go);
                SetStaticPrivate(typeof(UIManager), "<Instance>k__BackingField", null);
            }
        }
    }
}
