using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for the living calendar: the pure date table
    /// (Tuesdays rain, festival week around the first-flight anniversary),
    /// the rain remix on a real Day-mood build (Rainfall present + rain
    /// palette + mood), realms staying dry, the home island keeping its
    /// shelf under a rainy remix, and Gloomfang's near-collect spark.
    /// Restores every touched PlayerPrefs key and calendar override.
    /// Menu: GemRush/Calendar Probe/Run.
    /// </summary>
    public static class CalendarProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;
        static DateTime? savedOverride;
        static string savedFirstFlight;
        static bool hadFirstFlight;

        [MenuItem("GemRush/Calendar Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[CalendarProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            savedOverride = SkyCalendar.OverrideToday;
            Application.runInBackground = true;
            var go = new GameObject("CalendarProbeRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[CalendarProbe] started");
        }

        class Runner : MonoBehaviour
        {
            void Update() { Step(); }
        }

        static void Check(string label, bool pass, string detail)
        {
            if (Report.Length > 4000) return;
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static SkyCalendar.Weather W(int y, int m, int d)
        {
            return SkyCalendar.WeatherFor(new DateTime(y, m, d));
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (GemRush.GameManager.Instance == null ||
                GemRush.UIManager.Instance == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0: // pure date table (against a controlled
                // first-flight date, far from every tested date)
                {
                    savedFirstFlight = PlayerPrefs.GetString(
                        "gemrush_v2_firstflight", "");
                    hadFirstFlight = savedFirstFlight.Length > 0;
                    PlayerPrefs.SetString("gemrush_v2_firstflight",
                        "2020-01-01");
                    // 2026-09-22 is a Tuesday; 2026-09-19 is a Saturday.
                    Check("tuesday-brings-rain", W(2026, 9, 22) == SkyCalendar.Weather.Rain,
                        W(2026, 9, 22).ToString());
                    Check("ordinary-day-is-clear", W(2026, 9, 19) == SkyCalendar.Weather.None,
                        W(2026, 9, 19).ToString());
                    // First flight planted to 2024-06-15; its anniversary
                    // week (2026-06-14) is a festival.
                    PlayerPrefs.SetString("gemrush_v2_firstflight", "2024-06-15");
                    Check("anniversary-week-is-festival",
                        W(2026, 6, 14) == SkyCalendar.Weather.FestivalWeek,
                        W(2026, 6, 14).ToString());
                    Check("far-from-anniversary-clear",
                        W(2026, 9, 19) == SkyCalendar.Weather.None, "");
                    phase = 1;
                    break;
                }
                case 1: // rain remix on a real Day-mood level
                {
                    SkyCalendar.OverrideToday = new DateTime(2026, 9, 22); // Tue
                    GemRush.GameManager.Instance.PlayLevel(1); // Spinner Gauntlet, Day
                    frames(50);
                    phase = 2;
                    break;
                }
                case 2:
                {
                    bool rain = GameObject.Find("Rainfall") != null;
                    Camera cam = Camera.main;
                    bool palette = cam != null &&
                        Mathf.Abs(cam.backgroundColor.r -
                            SkyCalendar.RainSky.r) < 0.02f;
                    var mood = typeof(GemRush.AudioManager).GetField(
                        "activeMood",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                        .GetValue(GemRush.AudioManager.Instance);
                    Check("day-level-gets-rain", rain, "");
                    Check("rain-palette-applied", palette,
                        cam != null ? cam.backgroundColor.ToString("F2") : "no cam");
                    Check("rain-mood-plays",
                        (GemRush.SoundMood)mood == GemRush.SoundMood.Rain,
                        mood.ToString());
                    // Realm stays dry: the Long Winter (level 31).
                    GemRush.GameManager.Instance.PlayLevel(31);
                    frames(50);
                    phase = 3;
                    break;
                }
                case 3:
                {
                    Check("winter-realm-stays-dry",
                        GameObject.Find("Rainfall") == null, "");
                    Check("snow-still-falls",
                        GameObject.Find("Snowfall") != null, "");
                    // Home island under rain keeps its shelf (name-based fix).
                    SkyCalendar.OverrideToday = new DateTime(2026, 9, 22);
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(50);
                    phase = 4;
                    break;
                }
                case 4:
                {
                    bool shelf = GameObject.Find("PipShelf") != null;
                    bool menuOk = GemRush.GameManager.Instance.State ==
                        GemRush.GameState.Menu;
                    Check("home-island-keeps-shelf-in-rain", shelf, "");
                    Check("menu-still-works", menuOk, "");
                    // Gloomfang spark: level 9+ has a companion; collect a
                    // gem near him via the manager hook.
                    GemRush.GameManager.Instance.PlayLevel(10);
                    frames(40);
                    phase = 5;
                    break;
                }
                case 5:
                {
                    var gf = UnityEngine.Object.FindObjectOfType<GemRush.Gloomfang>();
                    if (gf != null)
                    {
                        Vector3 near = gf.transform.position + Vector3.one;
                        GemRush.Gloomfang.OnGemCollectedNear(near);
                        Check("spark-hook-runs-clean", true, "");
                    }
                    else
                    {
                        Check("spark-hook-runs-clean", false, "no companion");
                    }
                    // Restore everything.
                    if (hadFirstFlight)
                        PlayerPrefs.SetString("gemrush_v2_firstflight",
                            savedFirstFlight);
                    else
                        PlayerPrefs.DeleteKey("gemrush_v2_firstflight");
                    SkyCalendar.OverrideToday = savedOverride;
                    GemRush.GameManager.Instance.GoToMenu();
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        UnityEngine.Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[CalendarProbe] report:\n" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
