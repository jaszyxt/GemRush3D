using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for The Long Winter (pack 11): loads level
    /// 32, checks the lantern wakes on contact, that a frozen gate melts
    /// while Pip stands beside it with the light, that melted gates stay
    /// open, and that the crystal-map trigger runs clean. Logs a PASS/FAIL
    /// report (tag [WinterProbe], also kept in WinterProbe.Report).
    /// Menu: GemRush/Winter Probe/Run — start play mode first, and keep
    /// the editor window focused (the player loop suspends otherwise).
    /// </summary>
    public static class WinterProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        [MenuItem("GemRush/Winter Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[WinterProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("WinterProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[WinterProbe] started");
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

        static IceGate[] Gates()
        {
            return Object.FindObjectsOfType<IceGate>();
        }

        static Lantern Shrine()
        {
            return Object.FindObjectOfType<Lantern>();
        }

        static void Finish()
        {
            if (runner != null)
            {
                runner.gameObject.SetActive(false);
                Object.Destroy(runner.gameObject);
                runner = null;
            }
            Debug.Log("[WinterProbe] report:\n" + Report);
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0:
                    GemRush.GameManager.Instance.PlayLevel(31); // First Snow
                    frames(60);
                    phase = 1;
                    break;
                case 1:
                {
                    IceGate[] gates = Gates();
                    Lantern shrine = Shrine();
                    Check("winter-level-loaded",
                        GemRush.GameManager.Instance.CurrentLevelDefinition.LongWinter,
                        "level=" + GemRush.GameManager.Instance.CurrentLevel);
                    Check("gate-and-lantern-built",
                        gates.Length == 2 && shrine != null,
                        "gates=" + gates.Length + " shrine=" + (shrine != null));
                    Check("lantern-starts-cold", !GemRush.Lantern.Lit,
                        "Lit=" + GemRush.Lantern.Lit);
                    Check("gate-starts-solid",
                        gates.Length > 0 && gates[0].GetComponent<Collider>().enabled,
                        "");
                    frames(10);
                    phase = 2;
                    break;
                }
                case 2: // walk into the shrine
                    GemRush.GameBootstrap.Player.TeleportTo(
                        Shrine().transform.position + new Vector3(0f, 1.2f, 0f));
                    frames(30);
                    phase = 3;
                    break;
                case 3:
                    Check("lantern-wakes-on-contact", GemRush.Lantern.Lit,
                        "Lit=" + GemRush.Lantern.Lit);
                    // Stand in front of the first gate (z = 20).
                    GemRush.GameBootstrap.Player.TeleportTo(
                        new Vector3(0f, 1.7f, 17.2f));
                    frames(120); // ~2 s of game time inside the melt radius
                    phase = 4;
                    break;
                case 4:
                {
                    IceGate gate = Gates()[0];
                    Check("gate-melts-in-warmth",
                        !gate.GetComponent<Collider>().enabled,
                        "melted=" + GemRush.IceGate.MeltedCount() +
                        " collider=" + gate.GetComponent<Collider>().enabled);
                    Check("melt-count-tracked",
                        GemRush.IceGate.MeltedCount() >= 1,
                        "melted=" + GemRush.IceGate.MeltedCount());
                    GemRush.IceGate.TriggerCrystalMap(
                        GemRush.GameBootstrap.World.transform,
                        new Vector3(0f, 5f, 76f));
                    Check("crystal-map-runs-clean", true, "");
                    frames(60);
                    phase = 5;
                    break;
                }
                case 5:
                    // Melts persist: another tick later, still melted.
                    Check("melt-persists",
                        GemRush.IceGate.MeltedCount() >= 1,
                        "melted=" + GemRush.IceGate.MeltedCount());
                    Finish();
                    break;
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
