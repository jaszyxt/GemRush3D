using UnityEngine;
using UnityEngine.UI;

namespace GemRush
{
    /// On-screen controls for touch devices: a floating virtual joystick on
    /// the left half of the screen and a jump button on the right. Built
    /// entirely in code (procedural circle sprites) and only created when the
    /// device supports touch, so desktop players never see it.
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        /// Set by the jump button; consumed (and cleared) by the player controller.
        public static bool JumpQueued;

        /// True the whole time the jump button is held — variable jump height
        /// and fly mode read this (a Button.onClick only fires on release,
        /// which both delayed every jump and broke hold-to-rise on touch).
        public static bool JumpHeld;

        /// Normalized move vector (-1..1 on each axis) from the joystick.
        public Vector2 MoveVector { get; private set; }

        RectTransform baseRect;
        RectTransform knobRect;
        RectTransform jumpRect;
        float canvasScale = 1f;
        float radiusPx = 120f;
        int joystickFingerId = -1;
        Vector2 stickCenter;

        public static void Create(Transform hudParent, Font font)
        {
            if (!Input.touchSupported) return;

            GameObject root = new GameObject("TouchControls");
            root.transform.SetParent(hudParent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            StretchFull(rootRect);

            Sprite circle = Fx.CircleSprite();
            TouchControls controls = root.AddComponent<TouchControls>();
            TouchControls.JumpQueued = false;
            TouchControls.JumpHeld = false;

            // Joystick base + knob; floated under the thumb while touching.
            GameObject baseGo = NewCircleImage(root.transform, "StickBase",
                circle, new Color(1f, 1f, 1f, 0.22f), false);
            controls.baseRect = baseGo.GetComponent<RectTransform>();
            controls.baseRect.sizeDelta = new Vector2(240f, 240f);
            controls.baseRect.anchoredPosition = new Vector2(-1000f, -1000f);

            GameObject knobGo = NewCircleImage(controls.baseRect.transform, "Knob",
                circle, new Color(1f, 1f, 0.65f), false);
            controls.knobRect = knobGo.GetComponent<RectTransform>();
            controls.knobRect.sizeDelta = new Vector2(110f, 110f);

            // Jump button in the dominant thumb's arc. Fires on PRESS, not
            // release (uGUI Button.onClick would add ~80ms to the game's
            // most-pressed input), and reports hold state for fly mode and
            // variable jump height.
            GameObject jumpGo = NewCircleImage(root.transform, "JumpButton",
                circle, new Color(1f, 0.45f, 0.2f, 0.8f), true);
            controls.jumpRect = jumpGo.GetComponent<RectTransform>();
            controls.jumpRect.sizeDelta = new Vector2(180f, 180f);
            controls.ApplySide();

            JumpTouch jump = jumpGo.AddComponent<JumpTouch>();

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(jumpGo.transform, false);
            Text label = labelGo.AddComponent<Text>();
            if (font != null) label.font = font;
            label.text = Strings.Jump;
            label.fontSize = 38;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            StretchFull(label.rectTransform);
        }

        /// Re-anchors the jump button from the current LeftyOn setting so
        /// flipping the Settings row mirrors the HUD immediately, without
        /// rebuilding the controls or restarting the level. The joystick's
        /// touch-region logic already reads LeftyOn live in Update.
        public void ApplySide()
        {
            if (jumpRect == null) return;
            bool lefty = SaveSystem.LeftyOn;
            jumpRect.anchorMin = new Vector2(lefty ? 0f : 1f, 0f);
            jumpRect.anchorMax = new Vector2(lefty ? 0f : 1f, 0f);
            jumpRect.anchoredPosition = new Vector2(lefty ? 150f : -150f, 160f);
        }

        /// Clears interrupted-touch state: if the OS (call, notification
        /// shade) swallows the TouchPhase.Ended event, the joystick finger
        /// id would otherwise stay claimed forever and brick all input.
        public static void ResetInput()
        {
            JumpQueued = false;
            JumpHeld = false;
            if (Instance != null) Instance.joystickFingerId = -1;
        }

        /// Press-and-hold jump button; also queues the buffered jump.
        class JumpTouch : MonoBehaviour,
            UnityEngine.EventSystems.IPointerDownHandler,
            UnityEngine.EventSystems.IPointerUpHandler
        {
            public void OnPointerDown(UnityEngine.EventSystems.PointerEventData data)
            {
                JumpQueued = true;
                JumpHeld = true;
            }

            public void OnPointerUp(UnityEngine.EventSystems.PointerEventData data)
            {
                JumpHeld = false;
            }
        }

        void Awake()
        {
            Instance = this;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.scaleFactor > 0f) canvasScale = canvas.scaleFactor;
        }

        void Update()
        {
            Vector2 move = Vector2.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                if (t.phase == TouchPhase.Began && joystickFingerId == -1 &&
                    (SaveSystem.LeftyOn
                        ? t.position.x > Screen.width * 0.4f
                        : t.position.x < Screen.width * 0.6f))
                {
                    joystickFingerId = t.fingerId;
                    stickCenter = t.position;
                    radiusPx = Mathf.Min(Screen.width, Screen.height) * 0.14f;
                    if (baseRect != null)
                        baseRect.position = new Vector3(stickCenter.x, stickCenter.y, 0f);
                }

                if (t.fingerId == joystickFingerId)
                {
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        joystickFingerId = -1;
                    }
                    else
                    {
                        Vector2 delta = t.position - stickCenter;
                        float mag = delta.magnitude;
                        if (mag > radiusPx) delta *= radiusPx / mag;
                        if (knobRect != null)
                            knobRect.anchoredPosition = delta / canvasScale;
                        if (mag > 0.08f * radiusPx)
                            move = delta / radiusPx;
                    }
                }
            }

            if (joystickFingerId == -1 && knobRect != null)
                knobRect.anchoredPosition = Vector2.zero;
            MoveVector = move;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static GameObject NewCircleImage(Transform parent, string name,
            Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycastTarget;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }
    }
}
