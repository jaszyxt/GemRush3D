using System.Collections;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for The Aurora Festival (pack 12): loads
    /// level 35, checks the ribbons and aurora bands are built, that the
    /// mood resolves to Festival, and — the important part — that a
    /// ribbon actually CARRIES Pip while it flows (player tracks the
    /// ribbon's motion, proving the MovingPlatform carry works through
    /// the subclass). Menu: GemRush/Aurora Probe/Run.
    /// </summary>
    public static class AuroraProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        [MenuItem("GemRush/Aurora Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[AuroraProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("AuroraProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[AuroraProbe] started");
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

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0:
                    GemRush.GameManager.Instance.PlayLevel(34); // Festival Lights
                    frames(60);
                    phase = 1;
                    break;
                case 1:
                {
                    var ribbons = Object.FindObjectsOfType<GemRush.AuroraRibbon>();
                    Check("festival-level-loaded",
                        GemRush.GameManager.Instance.CurrentLevelDefinition.AuroraFestival,
                        "level=" + GemRush.GameManager.Instance.CurrentLevel);
                    Check("ribbons-built", ribbons.Length == 2,
                        "ribbons=" + ribbons.Length);
                    Check("aurora-bands-built",
                        GemRush.GameBootstrap.World != null &&
                        GemRush.GameBootstrap.World.transform.Find("AuroraBands") != null,
                        "");
                    var mood = typeof(GemRush.AudioManager)
                        .GetField("activeMood",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic)
                        .GetValue(GemRush.AudioManager.Instance);
                    Check("mood-resolves-festival",
                        (GemRush.SoundMood)mood == GemRush.SoundMood.Festival,
                        "mood=" + mood);

                    // Teleport Pip onto ribbon 0's slab, wherever it is in
                    // its flow right now.
                    GemRush.AuroraRibbon r = ribbons[0];
                    GemRush.GameBootstrap.Player.TeleportTo(
                        r.transform.position + new Vector3(0f, 1.2f, 0f));
                    rProbe = r;
                    pStart = GemRush.GameBootstrap.Player.transform.position;
                    rStart = r.transform.position;
                    frames(90); // 1.5 s of flow
                    phase = 2;
                    break;
                }
                case 2:
                {
                    Vector3 playerMoved =
                        GemRush.GameBootstrap.Player.transform.position - pStart;
                    Vector3 ribbonMoved = rProbe.transform.position - rStart;
                    // Riding = the player's offset from the ribbon stays
                    // (roughly) constant while the ribbon flows. The 2 u
                    // budget absorbs the initial drop onto the slab.
                    Vector3 drift = (playerMoved - ribbonMoved);
                    Check("ribbon-carries-pip",
                        ribbonMoved.magnitude > 0.5f && drift.magnitude < 2f,
                        "ribbon " + ribbonMoved.magnitude.ToString("F1") +
                        "u, drift " + drift.magnitude.ToString("F1") + "u");
                    Check("ribbon-velocity-live",
                        rProbe.Velocity.magnitude > 0.01f,
                        "v=" + rProbe.Velocity.magnitude.ToString("F2"));

                    // Aurora-unlock flag round-trip, then restore.
                    GemRush.SaveSystem.AuroraUnlocked = true;
                    bool on = GemRush.SaveSystem.AuroraUnlocked;
                    GemRush.SaveSystem.AuroraUnlocked = false;
                    Check("aurora-flag-roundtrip",
                        on && !GemRush.SaveSystem.AuroraUnlocked, "");
                    frames(10);
                    phase = 3;
                    break;
                }
                case 3:
                    GemRush.GameManager.Instance.GoToMenu();
                    frames(30);
                    phase = 4;
                    break;
                case 4:
                    Check("menu-aurora-spawns",
                        GemRush.GameBootstrap.World != null &&
                        GemRush.GameBootstrap.World.transform.Find("AuroraBands") != null,
                        "bands=" + (GemRush.GameBootstrap.World != null &&
                            GemRush.GameBootstrap.World.transform.Find("AuroraBands") != null));
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[AuroraProbe] report:\n" + Report);
                    break;
            }
        }

        static GemRush.AuroraRibbon rProbe;
        static Vector3 pStart;
        static Vector3 rStart;

        static void frames(int n) { waitFrames = n; }
    }
}
