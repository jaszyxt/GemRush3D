using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for the adaptive music intensity: loads a
    /// level, drops Pip to his last life and verifies the melody layer
    /// fades out while the chord bed carries on (in sync), then returns
    /// the life and verifies the melody fades back in. Menu:
    /// GemRush/Music Probe/Run (play mode + editor focused).
    /// </summary>
    public static class MusicProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;
        static int thinPolls;
        static System.Reflection.BindingFlags Priv =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic;

        [MenuItem("GemRush/Music Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[MusicProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("MusicProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[MusicProbe] started");
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

        static AudioSource MusicSrc()
        {
            return (AudioSource)typeof(GemRush.AudioManager).GetField(
                "musicSource", Priv).GetValue(GemRush.AudioManager.Instance);
        }

        static AudioSource BedSrc()
        {
            return (AudioSource)typeof(GemRush.AudioManager).GetField(
                "bedSource", Priv).GetValue(GemRush.AudioManager.Instance);
        }

        static float MelodyFactor()
        {
            return (float)typeof(GemRush.AudioManager).GetField(
                "melodyFactor", Priv).GetValue(GemRush.AudioManager.Instance);
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0:
                    GemRush.GameManager.Instance.PlayLevel(0);
                    frames(60);
                    phase = 1;
                    break;
                case 1:
                {
                    Check("level-playing",
                        GemRush.GameManager.Instance.State == GemRush.GameState.Playing,
                        "");
                    Check("melody-up-at-full-lives",
                        MelodyFactor() > 0.99f && MusicSrc().volume > 0.05f,
                        "factor=" + MelodyFactor().ToString("F2") +
                        " vol=" + MusicSrc().volume.ToString("F2") +
                        " (duck overlap tolerated)");
                    float drift = Mathf.Abs(MusicSrc().time - BedSrc().time);
                    Check("layers-in-sync", drift < 0.05f,
                        "drift=" + drift.ToString("F3") + "s");
                    // First death: 3 -> 2 lives, melody THINS. The fade
                    // runs on unscaled time; poll across Step() entries
                    // (a synchronous loop can never see it move).
                    GemRush.GameManager.Instance.OnPlayerDied(true);
                    thinPolls = 0;
                    frames(10);
                    phase = 2;
                    break;
                }
                case 2:
                {
                    float thinFactor = MelodyFactor();
                    if (thinFactor > 0.4f && thinFactor < 0.7f || ++thinPolls > 600)
                    {
                        Check("melody-thins-at-two-lives",
                            GemRush.GameManager.Instance.Lives == 2 &&
                            thinFactor > 0.4f && thinFactor < 0.7f,
                            "lives=" + GemRush.GameManager.Instance.Lives +
                            " factor=" + thinFactor.ToString("F2"));
                        // Second death: down to the last life.
                        GemRush.GameManager.Instance.OnPlayerDied(true);
                        frames(160); // fade = 2 s on unscaled time
                        phase = 3;
                    }
                    else frames(5);
                    break;
                }
                case 3:
                {
                    Check("melody-drops-at-last-life",
                        GemRush.GameManager.Instance.Lives == 1 &&
                        MelodyFactor() < 0.05f && MusicSrc().volume < 0.05f,
                        "lives=" + GemRush.GameManager.Instance.Lives +
                        " factor=" + MelodyFactor().ToString("F2") +
                        " melodyVol=" + MusicSrc().volume.ToString("F2") +
                        " bedVol=" + BedSrc().volume.ToString("F2"));
                    Check("bed-still-singing",
                        BedSrc().volume > 0.15f && BedSrc().isPlaying, "");
                    GemRush.GameManager.Instance.OnHeartCollected();
                    GemRush.GameManager.Instance.OnHeartCollected();
                    frames(160);
                    phase = 4;
                    break;
                }
                case 4:
                {
                    Check("melody-returns-on-hearts",
                        GemRush.GameManager.Instance.Lives >= 3 &&
                        MelodyFactor() > 0.95f && MusicSrc().volume > 0.15f,
                        "lives=" + GemRush.GameManager.Instance.Lives +
                        " factor=" + MelodyFactor().ToString("F2"));
                    float drift = Mathf.Abs(MusicSrc().time - BedSrc().time);
                    Check("layers-still-in-sync", drift < 0.05f,
                        "drift=" + drift.ToString("F3") + "s");
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[MusicProbe] report:\n" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
