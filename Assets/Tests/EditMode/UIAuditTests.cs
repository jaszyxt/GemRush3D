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
    }
}
