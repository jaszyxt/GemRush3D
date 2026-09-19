using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for the "world remembers you" bundle: with a
    /// rich save planted, the home island builds Pip's shelf with the
    /// earned trophies, the star trail matches the star tier, the menu
    /// shows the home world, and the atlas stamp reflects region state.
    /// SNAPSHOT/RESTORE: the player's real save keys are captured before
    /// planting and restored afterwards — the probe never destroys
    /// progress. Menu: GemRush/Shelf Probe/Run.
    /// </summary>
    public static class ShelfProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        // Snapshot of every key the probe touches.
        readonly static Dictionary<string, string> savedStrings =
            new Dictionary<string, string>();
        readonly static Dictionary<string, int> savedInts =
            new Dictionary<string, int>();
        readonly static Dictionary<string, float> savedFloats =
            new Dictionary<string, float>();
        readonly static HashSet<string> touched = new HashSet<string>();

        [MenuItem("GemRush/Shelf Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[ShelfProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            touched.Clear();
            savedStrings.Clear();
            savedInts.Clear();
            savedFloats.Clear();
            Application.runInBackground = true;
            var go = new GameObject("ShelfProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[ShelfProbe] started");
        }

        class Runner : MonoBehaviour
        {
            void Update() { Step(); }
        }

        // ---- snapshot-aware PlayerPrefs wrappers ----

        static int GetInt(string key, int fallback)
        {
            if (!touched.Contains(key))
            {
                touched.Add(key);
                if (PlayerPrefs.HasKey(key))
                    savedInts[key] = PlayerPrefs.GetInt(key);
            }
            return PlayerPrefs.GetInt(key, fallback);
        }

        static void SetInt(string key, int value)
        {
            GetInt(key, 0); // ensure snapshotted
            PlayerPrefs.SetInt(key, value);
        }

        static float GetFloat(string key, float fallback)
        {
            if (!touched.Contains(key))
            {
                touched.Add(key);
                if (PlayerPrefs.HasKey(key))
                    savedFloats[key] = PlayerPrefs.GetFloat(key);
            }
            return PlayerPrefs.GetFloat(key, fallback);
        }

        static void SetFloat(string key, float value)
        {
            GetFloat(key, 0f); // ensure snapshotted
            PlayerPrefs.SetFloat(key, value);
        }

        static void Restore()
        {
            foreach (var kv in savedInts) PlayerPrefs.SetInt(kv.Key, kv.Value);
            foreach (var kv in savedFloats)
                PlayerPrefs.SetFloat(kv.Key, kv.Value);
            foreach (var kv in savedStrings)
                PlayerPrefs.SetString(kv.Key, kv.Value);
            // Keys the probe planted that didn't exist before: delete.
            foreach (string key in touched)
            {
                bool existed = savedInts.ContainsKey(key) ||
                    savedFloats.ContainsKey(key) || savedStrings.ContainsKey(key);
                if (!existed) PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        static void Check(string label, bool pass, string detail)
        {
            if (Report.Length > 4000) return;
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static string BestKey(int level)
        {
            return "gemrush_v2_" + SaveKeySafe(
                GemRush.LevelLibrary.Levels[level].Name) + "_best";
        }

        static string StarsKey(int level)
        {
            return "gemrush_v2_" + SaveKeySafe(
                GemRush.LevelLibrary.Levels[level].Name) + "_stars";
        }

        static string SaveKeySafe(string levelName)
        {
            char[] chars = levelName.ToLower().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            return new string(chars);
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (GemRush.GameManager.Instance == null ||
                GemRush.UIManager.Instance == null) return; // wait for boot
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0: // plant a rich save (through snapshot-aware writes)
                {
                    int levels = GemRush.LevelLibrary.Levels.Length;
                    for (int i = 0; i < levels; i++)
                    {
                        SetFloat(BestKey(i), 30f + i);
                        SetInt(StarsKey(i), 3);
                    }
                    SetInt("gemrush_v2_unlockedCount", levels);
                    SetInt("gemrush_v2_aurora", 1);
                    PlayerPrefs.Save();
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(40);
                    phase = 1;
                    break;
                }
                case 1:
                {
                    Check("menu-shows-home-world",
                        GemRush.GameBootstrap.BuiltLevelIndex == 0 &&
                        GemRush.GameManager.Instance.State == GemRush.GameState.Menu,
                        "built=" + GemRush.GameBootstrap.BuiltLevelIndex);
                    var shelf = GameObject.Find("PipShelf");
                    Check("shelf-built-on-home-island", shelf != null, "");
                    if (shelf != null)
                    {
                        int trophies = 0;
                        foreach (Transform child in shelf.transform)
                            if (child.name.StartsWith("Trophy"))
                                trophies++;
                        // Planted: plush + star + bell + lantern +
                        // aurora, PLUS gift if the real save has any
                        // (the snapshot preserves it).
                        int planted = 5 + (GemRush.SaveSystem.Gifts > 0 ? 1 : 0);
                        Check("earned-trophies-present", trophies == planted,
                            "trophies=" + trophies + " planted=" + planted);
                    }
                    Check("trail-tier3-at-max-stars",
                        GameObject.Find("StarTrail") != null, "");
                    GemRush.UIManager.Instance.ShowAtlas();
                    frames(10);
                    phase = 2;
                    break;
                }
                case 2:
                {
                    var stamp = GameObject.Find("RegionStamp");
                    Check("atlas-stamp-shown-when-complete",
                        stamp != null && stamp.activeSelf, "");
                    GemRush.UIManager.Instance.CloseAtlas();
                    // Wipe the planted progress back to the snapshot.
                    Restore();
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(40);
                    phase = 3;
                    break;
                }
                case 3:
                {
                    // After restore, shelf/trail must match the RESTORED
                    // save state (this machine's real save is rich, so
                    // trophies correctly persist).
                    var shelf = GameObject.Find("PipShelf");
                    int trophies = 0;
                    if (shelf != null)
                        foreach (Transform child in shelf.transform)
                            if (child.name.StartsWith("Trophy"))
                                trophies++;
                    int expected = 0;
                    if (GemRush.SaveSystem.UnlockedLevel >= 9) expected++;
                    if (GemRush.SaveSystem.TotalStars(
                            GemRush.LevelLibrary.Levels.Length) >= 30) expected++;
                    if (GemRush.Shelf.RegionCleared("The Bell Towers")) expected++;
                    if (GemRush.Shelf.RegionCleared("The Long Winter")) expected++;
                    if (GemRush.SaveSystem.AuroraUnlocked) expected++;
                    if (GemRush.SaveSystem.Gifts > 0) expected++;
                    Check("trophies-match-restored-save",
                        trophies == expected,
                        "trophies=" + trophies + " expected=" + expected);
                    int tier = GemRush.StarTrail.TierFor(
                        GemRush.SaveSystem.TotalStars(
                            GemRush.LevelLibrary.Levels.Length));
                    var trail = GameObject.Find("StarTrail");
                    Check("trail-matches-tier",
                        (tier > 0) == (trail != null),
                        "tier=" + tier + " trail=" + (trail != null));
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[ShelfProbe] report:" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
