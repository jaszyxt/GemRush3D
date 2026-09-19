using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for the Atlas screen: opens it from the
    /// menu, checks the first region renders (header, 3 rows), flips to
    /// the one-level region (spare rows deactivate), launches a level
    /// from a row, and confirms the back-stack closes it. Reports via
    /// [AtlasProbe] log + Report property. Menu: GemRush/Atlas Probe/Run.
    /// </summary>
    public static class AtlasProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        [MenuItem("GemRush/Atlas Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[AtlasProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("AtlasProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[AtlasProbe] started");
        }

        class Runner : MonoBehaviour
        {
            void Update() { Step(); }
        }

        static void Check(string label, bool pass, string detail)
        {
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static void Finish()
        {
            if (runner != null)
            {
                runner.gameObject.SetActive(false);
                Object.Destroy(runner.gameObject);
                runner = null;
            }
            Debug.Log("[AtlasProbe] report:\n" + Report);
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0:
                    GemRush.UIManager.Instance.ShowAtlas();
                    frames(10);
                    phase = 1;
                    break;
                case 1:
                {
                    var ui = GemRush.UIManager.Instance;
                    Check("atlas-opens", ui.AtlasOpen, "");
                    var header = ui.transform.Find(
                        "UICanvas/SafeRoot/AtlasPanel/RegionHeader")
                        .GetComponent<Text>();
                    Check("region-one-renders",
                        header.text.StartsWith("REGION I"),
                        header.text);
                    var rows = FindRows();
                    int active = 0;
                    foreach (var r in rows) if (r.activeSelf) active++;
                    Check("three-rows-active", active == 3, "active=" + active);
                    FlipAtlasRegion(5);
                    frames(5);
                    phase = 2;
                    break;
                }
                case 2:
                {
                    var ui = GemRush.UIManager.Instance;
                    var header = ui.transform.Find(
                        "UICanvas/SafeRoot/AtlasPanel/RegionHeader")
                        .GetComponent<Text>();
                    var rows = FindRows();
                    int active = 0;
                    foreach (var r in rows) if (r.activeSelf) active++;
                    Check("one-level-region-flips",
                        header.text.Contains("DAY OFF") && active == 1,
                        header.text + " active=" + active);
                    FlipAtlasRegion(8);
                    frames(5); // back to region 0
                    phase = 3;
                    break;
                }
                case 3:
                {
                    // Launch level 1 from the first atlas row.
                    var rows = FindRows();
                    rows[0].GetComponent<Button>().onClick.Invoke();
                    frames(20);
                    phase = 4;
                    break;
                }
                case 4:
                {
                    var gm = GemRush.GameManager.Instance;
                    Check("row-launches-level",
                        gm.State == GemRush.GameState.Playing &&
                        gm.CurrentLevel == 0,
                        "state=" + gm.State + " level=" + gm.CurrentLevel);
                    // Back to the menu; the atlas must be closed by HideAll.
                    gm.GoToMenu();
                    frames(10);
                    phase = 5;
                    break;
                }
                case 5:
                    Check("atlas-closes-on-menu",
                        !GemRush.UIManager.Instance.AtlasOpen, "");
                    Finish();
                    break;
            }
        }

        static GameObject[] FindRows()
        {
            // The rows were built before the arrows and BACK, so the first
            // three children named "Button" under the panel are the rows.
            var panel = GemRush.UIManager.Instance.transform.Find(
                "UICanvas/SafeRoot/AtlasPanel");
            var found = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < panel.childCount; i++)
            {
                var child = panel.GetChild(i);
                if (child.name == "Button" && found.Count < 3)
                    found.Add(child.gameObject);
            }
            return found.ToArray();
        }

        static void FlipAtlasRegion(int dir)
        {
            var ui = GemRush.UIManager.Instance;
            var m = typeof(GemRush.UIManager).GetMethod("FlipAtlasRegion",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            m.Invoke(ui, new object[] { dir });
        }

        static void frames(int n) { waitFrames = n; }
    }
}
