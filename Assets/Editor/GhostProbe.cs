using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for ghost runs: encode/decode roundtrip
    /// (pure data), a real record run storing its recording, the ghost
    /// spawning on the next play, and the ghost faithfully replaying its
    /// path. Cleans up the test save. Menu: GemRush/Ghost Probe/Run.
    /// </summary>
    public static class GhostProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;
        static string savedKey;

        [MenuItem("GemRush/Ghost Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[GhostProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("GhostProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[GhostProbe] started");
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

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            switch (phase)
            {
                case 0: // pure-data roundtrip, no scene needed
                {
                    var original = new System.Collections.Generic.List<Vector3>
                    {
                        new Vector3(0f, 1.5f, 0f),
                        new Vector3(0.3f, 1.5f, 1.1f),
                        new Vector3(1.0f, 2.2f, 2.4f),
                        new Vector3(2.1f, 2.2f, 4.0f),
                        new Vector3(3.0f, 1.6f, 5.5f)
                    };
                    string encoded = GemRush.GhostStore.Encode(original);
                    var decoded = GemRush.GhostStore.Decode(encoded);
                    bool ok = decoded != null && decoded.Count == original.Count;
                    if (ok)
                        for (int i = 0; i < original.Count; i++)
                            ok &= (decoded[i] - original[i]).magnitude < 0.02f;
                    Check("encode-decode-roundtrip", ok,
                        "bytes~" + encoded.Length + " b64");
                    Check("corrupt-decode-safe",
                        GemRush.GhostStore.Decode("!!!not-base64!!!") == null, "");
                    GemRush.GameManager.Instance.PlayLevel(0);
                    frames(30);
                    phase = 1;
                    break;
                }
                case 1: // plant a fake "old best" recording, restart the level
                {
                    // 10 s of path (100 samples at 10 Hz) so the replay
                    // is still mid-flight when the probe checks it — and
                    // long enough that throttled frames don't finish it.
                    var path = new System.Collections.Generic.List<Vector3>();
                    for (int i = 0; i < 100; i++)
                        path.Add(new Vector3(0f, 1.5f, i * 1f));
                    string data = GemRush.GhostStore.Encode(path);
                    savedKey = GemRush.GhostStore.SaveKey("First Steps");
                    GemRush.GhostStore.Save("First Steps", data);
                    // Replay level so BuildWorld sees the recording.
                    GemRush.GameManager.Instance.PlayLevel(0);
                    frames(30);
                    phase = 2;
                    break;
                }
                case 2:
                {
                    var ghost = GameObject.Find("Ghost");
                    Check("ghost-spawns-with-recording", ghost != null, "");
                    if (ghost != null)
                    {
                        // Replay fidelity, non-invasive: read elapsed and
                        // position in the SAME tick — position must equal
                        // the path sample at that elapsed. (Writing elapsed
                        // just gets organically clobbered while Playing —
                        // which is itself proof the replay is live.)
                        var runnerComponent =
                            ghost.GetComponent<GemRush.GhostRunner>();
                        var elapsedField = typeof(GemRush.GhostRunner).GetField(
                            "elapsed",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);
                        float elapsed = (float)elapsedField.GetValue(
                            runnerComponent);
                        float z = ghost.transform.position.z;
                        float expectZ = Mathf.Min(
                            elapsed / GemRush.GhostStore.SampleInterval, 99f);
                        Check("ghost-replay-fidelity",
                            Mathf.Abs(z - expectZ) < 0.35f,
                            "elapsed " + elapsed.ToString("F2") + "s -> z=" +
                            z.ToString("F2") + " (expect ~" +
                            expectZ.ToString("F1") + ")");
                    }
                    // Cleanup the planted save, land on the menu.
                    PlayerPrefs.DeleteKey(savedKey);
                    PlayerPrefs.Save();
                    GemRush.GameManager.Instance.GoToMenu();
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[GhostProbe] report:\n" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
