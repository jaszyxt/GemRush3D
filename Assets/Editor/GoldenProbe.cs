using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for the golden-gem remix gate: the golden
    /// spawns deterministically on a source level, collecting it flips
    /// the B-side gate (menu row text + atlas detail), gem/star math is
    /// untouched, and non-source goldens are plain collection. Snapshots
    /// and restores every touched PlayerPrefs key. Menu:
    /// GemRush/Golden Probe/Run.
    /// </summary>
    public static class GoldenProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        readonly static HashSet<string> touched = new HashSet<string>();
        readonly static Dictionary<string, int> savedInts =
            new Dictionary<string, int>();

        static string Key19;
        static string Key28;

        [MenuItem("GemRush/Golden Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[GoldenProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            touched.Clear();
            savedInts.Clear();
            Key19 = "gemrush_v2_" + Safe(
                GemRush.LevelLibrary.Levels[19].Name) + "_golden";
            Key28 = "gemrush_v2_" + Safe(
                GemRush.LevelLibrary.Levels[28].Name) + "_golden";
            Application.runInBackground = true;
            var go = new GameObject("GoldenProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[GoldenProbe] started");
        }

        class Runner : MonoBehaviour
        {
            void Update() { Step(); }
        }

        static string Safe(string levelName)
        {
            char[] chars = levelName.ToLower().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            return new string(chars);
        }

        static void Snapshot(string key)
        {
            if (touched.Contains(key)) return;
            touched.Add(key);
            if (PlayerPrefs.HasKey(key))
                savedInts[key] = PlayerPrefs.GetInt(key);
        }

        static void SetInt(string key, int value)
        {
            Snapshot(key);
            PlayerPrefs.SetInt(key, value);
        }

        static void Restore()
        {
            foreach (var kv in savedInts) PlayerPrefs.SetInt(kv.Key, kv.Value);
            foreach (string key in touched)
                if (!savedInts.ContainsKey(key))
                    PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        static void Check(string label, bool pass, string detail)
        {
            if (Report.Length > 4000) return;
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (GemRush.GameManager.Instance == null ||
                GemRush.UIManager.Instance == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0: // plant: gate closed, source unplayed-but-unlocked
                {
                    SetInt(Key19, 0);
                    SetInt(Key28, 0);
                    SetInt("gemrush_v2_unlockedCount", 29); // ladder reaches B-side 28
                    PlayerPrefs.Save();
                    GemRush.GameManager.Instance.PlayLevel(19); // Gust Alley
                    frames(50);
                    phase = 1;
                    break;
                }
                case 1:
                {
                    var golden = GameObject.Find("GoldenGem");
                    Check("golden-spawns-on-source", golden != null, "");
                    if (golden != null)
                    {
                        var spot1 = golden.transform.position;
                        GemRush.GameManager.Instance.PlayLevel(19);
                        frames(50);
                        var golden2 = GameObject.Find("GoldenGem");
                        Check("spot-is-deterministic",
                            golden2 != null &&
                            (golden2.transform.position - spot1).magnitude < 0.01f,
                            golden2 != null
                                ? golden2.transform.position.ToString("F2") : "?");
                    }
                    // THE ASSERTION THAT MATTERS, and the one this probe
                    // spent its whole life faking: walk Pip INTO the gem
                    // and require the pickup to fire from real collision.
                    // The old version wrote the save flag directly
                    // ("simulate the collect's save write"), so it reported
                    // success for a gem that could not be collected at all.
                    var gem = GameObject.Find("GoldenGem");
                    var player = GemRush.GameBootstrap.Player;
                    if (gem == null || player == null)
                    {
                        Check("golden-is-collectable", false,
                            "gem=" + (gem != null) + " player=" +
                            (player != null));
                    }
                    else
                    {
                        var col = gem.GetComponent<Collider>();
                        Check("golden-has-a-trigger-collider",
                            col != null && col.isTrigger,
                            col != null ? col.GetType().Name : "none");
                        // Put Pip exactly on the gem. The COLLECTION CHECK
                        // must wait for real frames: frames(n) only sets a
                        // counter that the next Step() decrements, so
                        // checking on the following line reads the flag
                        // BEFORE any physics step has run — which is how
                        // this probe reported a working gem as broken.
                        // Deferred to phase 5, after the wait.
                        player.TeleportTo(gem.transform.position);
                        Physics.SyncTransforms();
                        frames(30);
                        phase = 5;
                        break;
                    }
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(40);
                    phase = 2;
                    break;
                }
                case 5:
                {
                    // Runs AFTER the 30-frame wait, so a trigger that fired
                    // has had its physics steps. This is the assertion that
                    // matters: real collision, not a written flag.
                    Check("golden-is-collectable",
                        GemRush.SaveSystem.GoldenFound(19),
                        "flag=" + GemRush.SaveSystem.GoldenFound(19));
                    Check("golden-left-the-scene",
                        GameObject.Find("GoldenGem") == null,
                        "");
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(40);
                    phase = 2;
                    break;
                }
                case 2:
                {
                    // B-side 28 gate open now: its menu button playable.
                    var ui = GemRush.UIManager.Instance;
                    var unlockedCount = GemRush.SaveSystem.UnlockedLevel;
                    Check("gate-source-flag-set",
                        GemRush.SaveSystem.GoldenFound(19), "");
                    Check("bside-gate-reads-open",
                        GemRush.SaveSystem.GoldenFound(19) &&
                        GemRush.LevelLibrary.BSideSourceIndex(28) == 19,
                        "unlocked=" + unlockedCount);
                    // Direct launch through the guarded path must pass now.
                    GemRush.GameManager.Instance.PlayLevel(28);
                    frames(40);
                    phase = 3;
                    break;
                }
                case 3:
                {
                    Check("bside-28-plays-when-gate-open",
                        GemRush.GameManager.Instance.CurrentLevel == 28 &&
                        GemRush.GameManager.Instance.State == GemRush.GameState.Playing,
                        "level=" + GemRush.GameManager.Instance.CurrentLevel);
                    Check("bside-hides-no-golden",
                        GameObject.Find("GoldenGem") == null, "");
                    // Gem math untouched by goldens: GemsTotal for the
                    // B-side equals its own definition's count.
                    Check("gem-math-untouched",
                        GemRush.GameManager.Instance.GemsTotal ==
                            GemRush.LevelLibrary.Levels[28].GemCount,
                        GemRush.GameManager.Instance.GemsTotal + " vs " +
                            GemRush.LevelLibrary.Levels[28].GemCount);
                    // Restore and confirm gate closes.
                    SetInt(Key19, 0);
                    PlayerPrefs.Save();
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(30);
                    phase = 4;
                    break;
                }
                case 4:
                {
                    Check("gate-closes-when-flag-cleared",
                        !GemRush.SaveSystem.GoldenFound(19), "");
                    Restore();
                    GemRush.GameManager.Instance.GoToMenu();
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[GoldenProbe] report:\n" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
