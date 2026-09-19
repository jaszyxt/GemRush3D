using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for The Homecoming (pack 13): loads level
    /// 38, checks the see-saws are built on their pedestals, that the
    /// plank TIPS toward Pip when he stands on an end, and that it
    /// springs level when he steps off. Menu: GemRush/Homecoming
    /// Probe/Run (play mode + editor focused).
    /// </summary>
    public static class HomecomingProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        [MenuItem("GemRush/Homecoming Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[HomecomingProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("HomecomingProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[HomecomingProbe] started");
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

        static float TiltDegrees(GemRush.SeeSaw s)
        {
            // The plank child rotates; read its signed X tilt from the
            // rigidbody rotation.
            var rb = s.GetComponentInChildren<Rigidbody>();
            Vector3 e = rb.rotation.eulerAngles;
            float x = e.x > 180f ? e.x - 360f : e.x;
            return x;
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0:
                    GemRush.GameManager.Instance.PlayLevel(37); // The Long Way Home
                    frames(60);
                    phase = 1;
                    break;
                case 1:
                {
                    var saws = Object.FindObjectsOfType<GemRush.SeeSaw>();
                    Check("homecoming-level-loaded",
                        GemRush.GameManager.Instance.CurrentLevel == 37,
                        "level=" + GemRush.GameManager.Instance.CurrentLevel);
                    Check("see-saws-built", saws.Length == 3,
                        "saws=" + saws.Length);
                    Check("plank-starts-level",
                        Mathf.Abs(TiltDegrees(saws[0])) < 3f,
                        "tilt=" + TiltDegrees(saws[0]).ToString("F1"));
                    // Pip stands on the plank's far (+z) end.
                    GemRush.GameBootstrap.Player.TeleportTo(
                        new Vector3(0f, 1.8f, 21.2f));
                    frames(90); // ~1.5 s for the tilt to settle
                    phase = 2;
                    break;
                }
                case 2:
                {
                    var saws = Object.FindObjectsOfType<GemRush.SeeSaw>();
                    float tilt = TiltDegrees(saws[0]);
                    Check("plank-tips-under-pip", tilt < -5f,
                        "tilt=" + tilt.ToString("F1") +
                        " (negative = +z end down toward Pip)");
                    // Step far off the plank.
                    GemRush.GameBootstrap.Player.TeleportTo(
                        new Vector3(0f, 1.8f, 26f));
                    frames(90);
                    phase = 3;
                    break;
                }
                case 3:
                {
                    var saws = Object.FindObjectsOfType<GemRush.SeeSaw>();
                    float tilt = TiltDegrees(saws[0]);
                    Check("plank-springs-level", Mathf.Abs(tilt) < 4f,
                        "tilt=" + tilt.ToString("F1"));
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[HomecomingProbe] report:\n" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
