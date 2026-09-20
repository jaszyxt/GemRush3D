using UnityEngine;

namespace GemRush
{
    /// The spawn sign's level name is diegetic during play — readable from
    /// the spawn camera, by design. But on the main menu the boot world
    /// shows through the dim panel, and the full-brightness sign floats at
    /// screen center colliding with the menu text (players read it as
    /// broken UI overlap). Fades the sign to a whisper while the menu is
    /// up; restores it the moment play begins. Polls state cheaply and
    /// stops touching the color once settled.
    public class WorldSign : MonoBehaviour
    {
        const float MenuAlpha = 0.12f;
        const float PlayAlpha = 1f;
        const float FadeSpeed = 3f; // alpha units per second, unscaled

        TextMesh text;
        Color baseColor;
        float alpha = PlayAlpha;

        void Awake()
        {
            text = GetComponent<TextMesh>();
            if (text != null) baseColor = text.color;
        }

        void Update()
        {
            if (text == null) return;
            bool menu = GameManager.Instance != null &&
                GameManager.Instance.State == GameState.Menu;
            float target = menu ? MenuAlpha : PlayAlpha;
            if (Mathf.Approximately(alpha, target)) return;

            alpha = Mathf.MoveTowards(alpha, target,
                Time.unscaledDeltaTime * FadeSpeed);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}
