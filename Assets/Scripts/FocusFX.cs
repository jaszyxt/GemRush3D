using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemRush
{
    /// Visible focus for gamepad/keyboard menus (directive D5): the
    /// selected button brightens and grows slightly; deselect restores.
    /// A controller-first menu that lights no focus is unreadable on a
    /// couch. Non-interactable buttons (locked levels) highlight with
    /// neither — selection rests there without pretending they are alive.
    public class FocusFX : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        const float FocusScale = 1.08f;
        const float FocusBrightness = 0.3f;
        /// The same quick dip the press feedback uses: focus is the most
        /// frequently triggered motion in the game (every nav step), and it
        /// used to snap while every peer tweened. Colour still leads the
        /// scale so a held D-pad does not read as a stutter.
        const float FocusSeconds = 0.12f;

        Image img;
        Color restColor;
        int focusSeq; // retires an in-flight dip when focus moves on

        void Awake()
        {
            img = GetComponent<Image>();
            restColor = img != null ? img.color : Color.white;
        }

        public void OnSelect(BaseEventData eventData)
        {
            Button button = GetComponent<Button>();
            if (button != null && !button.interactable) return;
            SetFocus(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocus(false);
        }

        /// Eases scale and colour toward the focused or resting pose. The
        /// sequence token means a fast Up/Down scroll retires the previous
        /// dip instead of stacking tweens, exactly like the gem counter.
        void SetFocus(bool focused)
        {
            if (img != null)
                img.color = focused
                    ? Color.Lerp(restColor, Color.white, FocusBrightness)
                    : restColor;

            float target = focused ? FocusScale : 1f;
            Transform tr = transform;
            int seq = ++focusSeq;
            float from = tr.localScale.x;
            Tweener.Value(from, target, FocusSeconds, delegate (float k)
            {
                if (seq != focusSeq) return; // focus moved on
                tr.localScale = new Vector3(k, k, 1f);
            });
        }

        /// Called after settings/level-grid code repaints img.color: pick
        /// up the new base on idle buttons, and re-brighten a focused one
        /// from its remembered base (never from the brightened value, or
        /// repeated refreshes would whiten it step by step).
        public void RefreshRestColor()
        {
            if (img == null) return;
            EventSystem es = EventSystem.current;
            bool focused = es != null && es.currentSelectedGameObject == gameObject;
            if (!focused)
            {
                restColor = img.color;
                return;
            }
            img.color = Color.Lerp(restColor, Color.white, FocusBrightness);
        }
    }
}
