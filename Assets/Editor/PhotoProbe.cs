using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Play-mode verification for photo mode: pauses a level, enters photo
    /// mode, checks the follow camera yields to the orbit, takes a CAPTURE
    /// and verifies the PNG lands on disk, then exits and checks the
    /// follow camera is restored. Deletes the probe's test capture after.
    /// Menu: GemRush/Photo Probe/Run (play mode + editor focused).
    /// </summary>
    public static class PhotoProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;

        [MenuItem("GemRush/Photo Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[PhotoProbe] play mode only.");
                return;
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("PhotoProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[PhotoProbe] started");
        }

        class Runner : MonoBehaviour
        {
            float age;

            void Update()
            {
                // A probe that outlives its run must never haunt a later
                // play session: DontDestroyOnLoad survives domain reloads,
                // and a stale runner was observed driving photo mode in an
                // unrelated one. Any runner still stepping 90 s after its
                // own start (the full probe takes ~10) self-destructs, and
                // a manager-less world (post-reload, mid-teardown) ends it.
                age += Time.unscaledDeltaTime;
                if (age > 90f || GemRush.GameManager.Instance == null)
                {
                    Debug.Log("[PhotoProbe] runner retired (age " +
                        (int)age + "s) — stale or world torn down.");
                    Destroy(gameObject);
                }
                PhotoProbe.Step();
            }
        }

        static void Check(string label, bool pass, string detail)
        {
            if (Report.Length > 4000) return;
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static void Invoke(string method)
        {
            typeof(GemRush.UIManager).GetMethod(method,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic)
                .Invoke(GemRush.UIManager.Instance, null);
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }
            switch (phase)
            {
                case 0:
                    try
                    {
                        GemRush.GameManager.Instance.PlayLevel(0);
                        frames(30);
                        GemRush.GameManager.Instance.PauseGame();
                        GemRush.UIManager.Instance.ShowPhotoMode();
                        frames(20);
                    }
                    catch (System.Exception e)
                    {
                        Check("setup-threw", false, e.Message);
                    }
                    phase = 1;
                    break;
                case 1:
                {
                    var follow = GemRush.GameBootstrap.CameraRig;
                    Check("photo-mode-opens",
                        GemRush.UIManager.Instance.PhotoModeOpen, "");
                    Check("follow-camera-yields",
                        follow != null && !follow.enabled, "");
                    Invoke("CapturePhoto");
                    frames(30); // sync capture needs a frame to encode/write
                    phase = 2;
                    break;
                }
                case 2:
                {
                    var status = GemRush.UIManager.Instance.transform.Find(
                        "UICanvas/SafeRoot/PhotoPanel/PhotoStatus")
                        .GetComponent<Text>();
                    string prefix = "Saved to ";
                    string path = status.text.StartsWith(prefix)
                        ? status.text.Substring(prefix.Length) : "";
                    bool exists = path.Length > 0 && File.Exists(path);
                    if (exists || ++waitFrames > 300)
                    {
                        Check("capture-writes-png", exists,
                            path + (exists ? "" : " (never appeared)"));
                        if (exists) File.Delete(path); // probe cleanup
                        GemRush.UIManager.Instance.ClosePhotoMode();
                        frames(10);
                        phase = 3;
                    }
                    break;
                }
                case 3:
                {
                    var follow = GemRush.GameBootstrap.CameraRig;
                    Check("exit-restores-pause",
                        GemRush.UIManager.Instance.PhotoModeOpen == false &&
                        GemRush.GameManager.Instance.State == GemRush.GameState.Paused,
                        "state=" + GemRush.GameManager.Instance.State);
                    Check("follow-camera-restored",
                        follow != null && follow.enabled, "");
                    GemRush.GameManager.Instance.GoToMenu();
                    if (runner != null)
                    {
                        runner.gameObject.SetActive(false);
                        Object.Destroy(runner.gameObject);
                        runner = null;
                    }
                    Debug.Log("[PhotoProbe] report:\n" + Report);
                    break;
                }
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
