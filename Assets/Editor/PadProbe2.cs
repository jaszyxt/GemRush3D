using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using GamepadButton = UnityEngine.InputSystem.LowLevel.GamepadButton;
using InputState = UnityEngine.InputSystem.LowLevel.InputState;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Follow-up gamepad probe: verifies the two checks the first run
    /// couldn't isolate — A-jump on the flat tutorial spawn, and B's
    /// pause/resume edges with long-enough holds — without the spawn-hop
    /// interference of the full walk. Run in play mode from
    /// GemRush/Pad Probe/Run 2.
    /// </summary>
    public static class PadProbe2
    {
        public static string Report { get; private set; } = "not run";

        static Gamepad pad;
        static Runner runner;
        static int phase;
        static int waitFrames;

        [MenuItem("GemRush/Pad Probe/Run 2")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[PadProbe2] play mode only.");
                return;
            }
            pad = Gamepad.current;
            if (pad == null) pad = InputSystem.AddDevice<Gamepad>();
            Report = "";
            phase = 0;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("PadProbe2Runner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[PadProbe2] started");
        }

        class Runner : MonoBehaviour
        {
            void Update() { Step(); }
        }

        static void Write<TValue>(UnityEngine.InputSystem.InputControl<TValue> control,
            TValue value) where TValue : struct
        {
            using (var buffer = UnityEngine.InputSystem.LowLevel.StateEvent.From(
                pad, out var eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        static void Check(string label, bool pass, string detail)
        {
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static Rigidbody Body()
        {
            return GemRush.GameBootstrap.Player != null
                ? GemRush.GameBootstrap.Player.GetComponent<Rigidbody>() : null;
        }

        static void Finish()
        {
            if (GemRush.GameManager.Instance != null &&
                GemRush.GameManager.Instance.State != GemRush.GameState.Menu)
                GemRush.GameManager.Instance.GoToMenu();
            if (runner != null)
            {
                runner.gameObject.SetActive(false);
                Object.Destroy(runner.gameObject);
                runner = null;
            }
            Debug.Log("[PadProbe2] report:\n" + Report);
        }

        static void Step()
        {
            if (pad == null || !Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            GemRush.GameState state = GemRush.GameManager.Instance != null
                ? GemRush.GameManager.Instance.State : GemRush.GameState.Menu;

            switch (phase)
            {
                case 0: // straight to the flat tutorial level
                    GemRush.GameManager.Instance.PlayLevel(0);
                    frames(90); // intro window passes, spawn hop fully settled
                    phase = 1;
                    break;
                case 1:
                    Check("level1-playing", state == GemRush.GameState.Playing,
                        "state=" + state + " groundedVy=" +
                        Body().linearVelocity.y.ToString("F1"));
                    Write(pad[GamepadButton.South], 1f);
                    frames(8);
                    phase = 2;
                    break;
                case 2:
                    Write(pad[GamepadButton.South], 0f);
                    Check("A-jumps-on-flat-ground",
                        Body().linearVelocity.y > 1f,
                        "vy=" + Body().linearVelocity.y.ToString("F1"));
                    frames(70); // land
                    phase = 3;
                    break;
                case 3: // B walks the back stack from play into pause
                    Write(pad[GamepadButton.East], 1f);
                    frames(12);
                    phase = 4;
                    break;
                case 4:
                    Check("B-pauses-from-play", state == GemRush.GameState.Paused,
                        "state=" + state);
                    Write(pad[GamepadButton.East], 0f);
                    frames(10);
                    phase = 5;
                    break;
                case 5:
                    Write(pad[GamepadButton.East], 1f);
                    frames(12);
                    phase = 6;
                    break;
                case 6:
                    Check("B-resumes-from-pause", state == GemRush.GameState.Playing,
                        "state=" + state);
                    Write(pad[GamepadButton.East], 0f);
                    frames(5);
                    phase = 7;
                    break;
                case 7:
                    Finish();
                    break;
            }
        }

        static void frames(int n) { waitFrames = n; }
    }
}
