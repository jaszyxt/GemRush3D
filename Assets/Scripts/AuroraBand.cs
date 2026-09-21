using System.Collections;
using UnityEngine;

namespace GemRush
{
    /// Decorative aurora ribbons hanging high in the sky: the Aurora
    /// Festival's backdrop, and — once the festival finale is cleared — a
    /// permanent fixture over the menu screen. Pure decoration: no
    /// colliders, slow hue drift and breathing (small area, slow, audited
    /// cadence), world-parented so they die with rebuilds.
    public static class AuroraBand
    {
        /// True once the finale was cleared (SaveSystem-backed).
        public static bool Unlocked
        {
            get { return SaveSystem.AuroraUnlocked; }
        }

        public static void Create(Transform parent, float courseLength)
        {
            // One guard child: re-running ShowMenu in the same world must
            // never stack duplicate skies.
            if (parent.Find("AuroraBands") != null) return;

            GameObject root = new GameObject("AuroraBands");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0f, 0f);

            int count = 4;
            for (int i = 0; i < count; i++)
            {
                Material band = ArtLib.Solid(BandColor(i), 1.1f);
                ArtLib.SetFade(band, 0.4f);
                float length = courseLength + 60f;
                GameObject strip = ArtLib.DecorCube(root.transform,
                    new Vector3(
                        ((i % 2) * 2f - 1f) * (6f + i * 3.5f),
                        26f + i * 4.5f,
                        courseLength * 0.5f),
                    new Vector3(2.2f + (i % 3) * 0.9f, 1.1f, length),
                    Quaternion.Euler(0f, i * 7f - 10f, 0f),
                    band);
                strip.AddComponent<BandDrift>().seed = i * 3.17f;
            }
        }

        static Color BandColor(int i)
        {
            // The shared festival palette, in shimmer order.
            return ArtLib.Aurora[i % ArtLib.Aurora.Length];
        }
    }

    /// One band: a slow sideways serpentine plus a gentle brightness
    /// breathe, each band on its own phase.
    ///
    /// [Preserve] IS LOAD-BEARING — do not remove it. Created ONLY through
    /// AddComponent<BandDrift>() and referenced nowhere else in managed code,
    /// so IL2CPP's linker may strip it from the Android player. The aurora
    /// would still render (the geometry is built elsewhere) but the bands
    /// would hang motionless. See GoldenStar for how this class fails.
    [UnityEngine.Scripting.Preserve]
    public class BandDrift : MonoBehaviour
    {
        public float seed;
        Vector3 basePosition;
        Material mat;

        void Start()
        {
            basePosition = transform.localPosition;
            MeshRenderer r = GetComponent<MeshRenderer>();
            if (r != null) mat = r.material;
        }

        void Update()
        {
            float t = Time.time + seed;
            Vector3 p = basePosition;
            p.x += Mathf.Sin(t * 0.21f) * 2.4f;
            p.y += Mathf.Sin(t * 0.13f) * 0.9f;
            transform.localPosition = p;
            if (mat != null)
            {
                Color c = mat.color;
                c.a = 0.3f + 0.14f * (Mathf.Sin(t * 0.4f) * 0.5f + 0.5f);
                mat.color = c;
            }
        }
    }
}
