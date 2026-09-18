using UnityEngine;
using UnityEngine.InputSystem;

namespace GemRush
{
    /// Gamepad polling (directives D5/D6): one guarded door to the Input
    /// System so the rest of the game never touches it directly. Under
    /// "Both" input handling the legacy API (touch, keyboard axes) keeps
    /// working unchanged; if the native backend is not live yet (editor
    /// not restarted after the package landed) every read degrades to a
    /// silent no-op instead of throwing every frame. An internally
    /// throttled probe notices a pad plugged in (or yanked out) within a
    /// second of the next read.
    public static class GamepadInput
    {
        // Radial deadzone; the stick rescales so the deadzone edge reads
        // zero and full tilt reads exactly 1.
        const float Deadzone = 0.15f;

        static bool probed;
        static bool available;
        static float nextProbe;

        /// True when the Input System backend is live and a gamepad exists.
        public static bool Available
        {
            get
            {
                if (!probed || Time.unscaledTime >= nextProbe) Probe();
                return available;
            }
        }

        /// The most recent input came from a gamepad (the menu hint line
        /// adapts to it). Keyboard, touch or mouse activity flips it back.
        public static bool LastDeviceWasGamepad { get; private set; }

        static void Probe()
        {
            probed = true;
            nextProbe = Time.unscaledTime + 1f;
            try { available = Gamepad.current != null; }
            catch (System.InvalidOperationException) { available = false; }
        }

        public static Vector2 LeftStick
        {
            get
            {
                if (!Available) return Vector2.zero;
                try
                {
                    Vector2 v = Gamepad.current.leftStick.ReadValue();
                    float mag = v.magnitude;
                    if (mag < Deadzone) return Vector2.zero;
                    return v / mag * ((mag - Deadzone) / (1f - Deadzone));
                }
                catch (System.InvalidOperationException) { return Vector2.zero; }
            }
        }

        /// South face button (A / Cross): jump — a press edge for the
        /// buffered queue, held state for variable height and fly mode.
        public static bool JumpPressed
        {
            get
            {
                if (!Available) return false;
                try { return Gamepad.current.buttonSouth.wasPressedThisFrame; }
                catch (System.InvalidOperationException) { return false; }
            }
        }

        public static bool JumpHeld
        {
            get
            {
                if (!Available) return false;
                try { return Gamepad.current.buttonSouth.isPressed; }
                catch (System.InvalidOperationException) { return false; }
            }
        }

        /// Start / Options: the pause toggle.
        public static bool StartPressed
        {
            get
            {
                if (!Available) return false;
                try { return Gamepad.current.startButton.wasPressedThisFrame; }
                catch (System.InvalidOperationException) { return false; }
            }
        }

        /// East face button (B / Circle): back, walking the same stack as
        /// Escape (D4).
        public static bool BackPressed
        {
            get
            {
                if (!Available) return false;
                try { return Gamepad.current.buttonEast.wasPressedThisFrame; }
                catch (System.InvalidOperationException) { return false; }
            }
        }

        /// Shoulders page the touch-style level list on the menu.
        public static bool PageLeftPressed
        {
            get
            {
                if (!Available) return false;
                try { return Gamepad.current.leftShoulder.wasPressedThisFrame; }
                catch (System.InvalidOperationException) { return false; }
            }
        }

        public static bool PageRightPressed
        {
            get
            {
                if (!Available) return false;
                try { return Gamepad.current.rightShoulder.wasPressedThisFrame; }
                catch (System.InvalidOperationException) { return false; }
            }
        }

        public static void MarkGamepad() { LastDeviceWasGamepad = true; }
        public static void MarkOther() { LastDeviceWasGamepad = false; }
    }
}
