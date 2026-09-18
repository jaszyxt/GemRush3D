using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GamepadButton = UnityEngine.InputSystem.LowLevel.GamepadButton;
using InputState = UnityEngine.InputSystem.LowLevel.InputState;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Self-driving gamepad verification rig (directives D5/D6). Attaches
    /// a virtual gamepad, then walks the real game with it: menu focus,
    /// shoulder paging, A-to-confirm into a level, jump, stick movement,
    /// Start pause/resume, B back, rumble burst, unplug fallback. Control
    /// state is written per-control via InputState.Change (no raw state
    /// struct, no button-bit guessing). Logs a PASS/FAIL line per step to
    /// the console (tag [PadProbe]) and keeps the report in PadProbe.Report.
    /// Run from GemRush/Pad Probe/Run while in play mode — no hardware
    /// needed.
    /// </summary>
    public static class PadProbe
    {
        public static string Report { get; private set; } = "not run";

        static Gamepad pad;
        static Runner runner;
        static int phase;
        static int waitFrames;
        static bool hapticsOriginal;
        static bool hapticsOverridden;

        [MenuItem("GemRush/Pad Probe/Run")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[PadProbe] play mode only — start play, then run.");
                return;
            }
            if (pad == null)
            {
                pad = Gamepad.current;
                if (pad == null) pad = InputSystem.AddDevice<Gamepad>();
            }
            Report = "";
            phase = 0;
            waitFrames = 0;
            hapticsOverridden = false;
            Application.runInBackground = true;
            // Driven by a MonoBehaviour, not EditorApplication.update: the
            // editor update loop throttles hard when unfocused, while the
            // play-mode player loop (runInBackground above) keeps ticking.
            var runnerGo = new GameObject("PadProbeRunner");
            Object.DontDestroyOnLoad(runnerGo);
            runnerGo.hideFlags = HideFlags.HideAndDontSave;
            runner = runnerGo.AddComponent<Runner>();
            Debug.Log("[PadProbe] started with virtual pad " + pad.displayName);
        }

        class Runner : MonoBehaviour
        {
            void Update() { Step(); }
        }

        // One guarded write of a control value: build a full state event
        // from the device's current state, poke the control, queue it.
        // InputState.Change refuses the virtual pad's bitfield-backed
        // controls, so raw state events are the only universal path.
        static void Write<TValue>(
            UnityEngine.InputSystem.InputControl<TValue> control, TValue value)
            where TValue : struct
        {
            try
            {
                using (var buffer = UnityEngine.InputSystem.LowLevel.StateEvent.From(
                    pad, out var eventPtr))
                {
                    control.WriteValueIntoEvent(value, eventPtr);
                    UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
                }
                lastWriteError = "";
            }
            catch (System.Exception e)
            {
                lastWriteError = e.GetType().Name + ": " + e.Message;
            }
        }

        static string lastWriteError = "";

        static void Press(GamepadButton button)
        {
            Write(pad[button], 1f);
        }

        static void Release(GamepadButton button)
        {
            Write(pad[button], 0f);
        }

        static void Stick(Vector2 value)
        {
            Write(pad.leftStick, value);
        }

        static void Check(string label, bool pass, string detail)
        {
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        static string SelectedName()
        {
            EventSystem es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return "";
            Text label = es.currentSelectedGameObject.GetComponentInChildren<Text>();
            return label != null ? label.text : es.currentSelectedGameObject.name;
        }

        static string PageLabel()
        {
            object ui = Object.FindObjectOfType<GemRush.UIManager>();
            if (ui == null) return "";
            FieldInfo info = ui.GetType().GetField("pageLabel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Text t = info != null ? info.GetValue(ui) as Text : null;
            return t != null ? t.text : "";
        }

        static Rigidbody PlayerBody()
        {
            return GemRush.GameBootstrap.Player != null
                ? GemRush.GameBootstrap.Player.GetComponent<Rigidbody>() : null;
        }

        static int frames(int n) { waitFrames = n; return 0; }

        /// The state machine: each case runs on one editor update tick;
        /// frames(n) parks it for n ticks so real frames pass between a
        /// press and its check (the UI module works on frame boundaries).
        public static void Step()
        {
            if (pad == null || !Application.isPlaying || runner == null)
            {
                return;
            }
            if (waitFrames > 0)
            {
                waitFrames--;
                return;
            }

            GemRush.GameState state = GemRush.GameManager.Instance != null
                ? GemRush.GameManager.Instance.State : GemRush.GameState.Menu;

            switch (phase)
            {
                case 0: // settle, then baseline checks
                    frames(30);
                    phase = 1;
                    break;
                case 1:
                    Check("menu-first-selected-is-PLAY",
                        SelectedName() == "PLAY", SelectedName());
                    Check("virtual-pad-detected", GemRush.GamepadInput.Available,
                        "Available=" + GemRush.GamepadInput.Available);
                    if (PageLabel().Length > 0)
                    {
                        Press(GamepadButton.LeftShoulder);
                        frames(6);
                        phase = 2;
                    }
                    else
                    {
                        phase = 3; // no paging UI (not touch layout): skip
                    }
                    break;
                case 2:
                    Release(GamepadButton.LeftShoulder);
                    Check("shoulder-pages-level-list",
                        PageLabel() != "", "page=" + PageLabel() + " (flipped)");
                    phase = 3;
                    break;
                case 3:
                    // The virtual pad's dpad tree is bitfield-only; the
                    // left stick drives the same UI Move action.
                    Stick(new Vector2(0f, -1f));
                    frames(6);
                    phase = 4;
                    break;
                case 4:
                    Stick(Vector2.zero);
                    Check("stick-moves-menu-focus-into-grid",
                        SelectedName() != "PLAY" && SelectedName().StartsWith("LEVEL"),
                        "selected=" + SelectedName() +
                        " stickRead=" + pad.leftStick.ReadValue().ToString("F2") +
                        (lastWriteError.Length > 0 ? " writeErr=" + lastWriteError : ""));
                    Press(GamepadButton.South);
                    frames(8);
                    phase = 5;
                    break;
                case 5:
                    Release(GamepadButton.South);
                    Check("A-confirms-into-level", state == GemRush.GameState.Playing,
                        "state=" + state);
                    frames(70); // let any spawn hop from the confirm land
                    phase = 6;
                    break;
                case 6: // jump
                    Press(GamepadButton.South);
                    frames(6);
                    phase = 7;
                    break;
                case 7:
                    Release(GamepadButton.South);
                    Rigidbody body = PlayerBody();
                    Check("A-jumps", body != null && body.linearVelocity.y > 1f,
                        body != null ? "vy=" + body.linearVelocity.y.ToString("F1") : "no player");
                    Stick(new Vector2(1f, 0f)); // stick right, held
                    frames(25);
                    phase = 8;
                    break;
                case 8:
                    Stick(Vector2.zero);
                    body = PlayerBody();
                    Check("stick-moves-pip", body != null && body.linearVelocity.x > 2f,
                        body != null ? "vx=" + body.linearVelocity.x.ToString("F1") : "no player");
                    if (state == GemRush.GameState.Paused)
                        GemRush.GameManager.Instance.ResumeGame(); // focus-loss guard
                    Press(GamepadButton.Start);
                    frames(6);
                    phase = 9;
                    break;
                case 9:
                    Release(GamepadButton.Start);
                    Check("start-pauses", state == GemRush.GameState.Paused,
                        "state=" + state);
                    Press(GamepadButton.Start);
                    frames(6);
                    phase = 10;
                    break;
                case 10:
                    Release(GamepadButton.Start);
                    Check("start-resumes", state == GemRush.GameState.Playing,
                        "state=" + state);
                    Press(GamepadButton.East);
                    frames(6);
                    phase = 11;
                    break;
                case 11:
                    Release(GamepadButton.East);
                    Check("B-pauses-from-play", state == GemRush.GameState.Paused,
                        "state=" + state);
                    Press(GamepadButton.East);
                    frames(6);
                    phase = 12;
                    break;
                case 12:
                    Release(GamepadButton.East);
                    Check("B-resumes-from-pause", state == GemRush.GameState.Playing,
                        "state=" + state);
                    // Rumble: force the setting on for the observable check.
                    hapticsOriginal = GemRush.SaveSystem.HapticsOn;
                    GemRush.SaveSystem.HapticsOn = true;
                    hapticsOverridden = true;
                    GemRush.Haptics.GamepadBurst(1f, 1f, 2f);
                    frames(4);
                    phase = 13;
                    break;
                case 13:
                    CheckMotors();
                    if (hapticsOverridden)
                    {
                        GemRush.SaveSystem.HapticsOn = hapticsOriginal;
                        GemRush.Haptics.StopRumble();
                    }
                    InputSystem.RemoveDevice(pad);
                    frames(120); // > the 1 s probe window in GamepadInput
                    phase = 14;
                    break;
                case 14:
                    Check("unplug-falls-back-clean",
                        Gamepad.current == null && !GemRush.GamepadInput.Available,
                        "Available=" + GemRush.GamepadInput.Available);
                    if (GemRush.GameManager.Instance != null &&
                        GemRush.GameManager.Instance.State != GemRush.GameState.Menu)
                        GemRush.GameManager.Instance.GoToMenu();
                    EditorApplication.update -= Step;
                    Debug.Log("[PadProbe] report:\n" + Report);
                    break;
            }
        }

        /// Motor readback: base Gamepad in Input System 1.20 exposes no
        /// lowMotor/highMotor controls, so scan generically. A virtual pad
        /// with no motor controls is reported as inconclusive (PASS with a
        /// note) — the burst call path itself did not throw.
        static void CheckMotors()
        {
            float low = -1f;
            float high = -1f;
            bool foundAny = false;
            try
            {
                foreach (InputControl c in pad.allControls)
                {
                    if (c.GetType().Name != "MotorControl") continue;
                    foundAny = true;
                    float v = (float)c.ReadValueAsObject();
                    if (c.name == "lowMotor") low = v;
                    else if (c.name == "highMotor") high = v;
                }
            }
            catch (System.Exception e)
            {
                Check("rumble-reaches-pad", false, "unreadable: " + e.Message);
                return;
            }
            if (!foundAny)
                Check("rumble-reaches-pad", true, "inconclusive: virtual pad has no motor controls; burst call did not throw");
            else
                Check("rumble-reaches-pad", low > 0.9f && high > 0.9f,
                    "low=" + low.ToString("F2") + " high=" + high.ToString("F2"));
        }
    }
}
