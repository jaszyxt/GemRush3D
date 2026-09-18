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
        static readonly Vector3 FocusedScale = new Vector3(1.08f, 1.08f, 1f);
        const float FocusBrightness = 0.3f;

        Image img;
        Color restColor;

        void Awake()
        {
            img = GetComponent<Image>();
            restColor = img != null ? img.color : Color.white;
        }

        public void OnSelect(BaseEventData eventData)
        {
            Button button = GetComponent<Button>();
            if (button != null && !button.interactable) return;
            transform.localScale = FocusedScale;
            if (img != null) img.color = Color.Lerp(restColor, Color.white, FocusBrightness);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            transform.localScale = Vector3.one;
            if (img != null) img.color = restColor;
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
