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
            "completePanel", "settingsPanel", "atlasPanel", "pausePanel"
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
    }
}
