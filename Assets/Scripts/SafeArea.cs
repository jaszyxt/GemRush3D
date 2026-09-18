using UnityEngine;

namespace GemRush
{
    /// Constrains a RectTransform to Screen.safeArea. All screens sit under
    /// this node so notches, punch-holes, rounded corners and gesture bars
    /// never clip interactive UI. Backgrounds that must bleed keep their own
    /// node outside.
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect applied;

        void OnEnable() { Apply(); }

        void Update()   // cheap property read; catches rotation & resolution
        {
            if (Screen.safeArea != applied) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            RectTransform rt = (RectTransform)transform;
            Vector2 min = applied.position;
            Vector2 max = applied.position + applied.size;
            // Some environments report safeArea larger than the actual view
            // (the editor game view can get the whole desktop resolution;
            // some Android punch-hole devices misreport too). Unclamped,
            // anchors beyond 1 stretch every screen far off-screen, so the
            // ratios are clamped to the honest 0..1 range — a bogus report
            // then degrades to "no safe-area inset", never a broken UI.
            rt.anchorMin = new Vector2(
                Mathf.Clamp01(min.x / Screen.width),
                Mathf.Clamp01(min.y / Screen.height));
            rt.anchorMax = new Vector2(
                Mathf.Clamp01(max.x / Screen.width),
                Mathf.Clamp01(max.y / Screen.height));
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
