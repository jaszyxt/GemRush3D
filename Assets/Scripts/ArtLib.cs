using UnityEngine;

namespace GemRush
{
    /// Small library of materials and shared colors so every script builds
    /// its visuals with the same palette.
    public static class ArtLib
    {
        public static readonly Color Grass = new Color(0.36f, 0.72f, 0.34f);
        public static readonly Color Dirt = new Color(0.50f, 0.37f, 0.25f);
        public static readonly Color Stone = new Color(0.58f, 0.60f, 0.64f);
        public static readonly Color MoverOrange = new Color(0.90f, 0.55f, 0.18f);
        public static readonly Color ElevatorBlue = new Color(0.28f, 0.62f, 0.88f);
        public static readonly Color PlayerSkin = new Color(1.00f, 0.55f, 0.15f);
        public static readonly Color PlayerNose = new Color(0.85f, 0.30f, 0.10f);
        public static readonly Color GemPink = new Color(0.98f, 0.30f, 0.75f);
        public static readonly Color PortalCyan = new Color(0.20f, 0.90f, 0.95f);
        public static readonly Color CloudWhite = new Color(0.97f, 0.98f, 1.00f);
        /// Kick-up dust: jump, landing and skid puffs. Slightly warm rather
        /// than pure white, so dust reads as disturbed ground and never as
        /// a portal/bell glow that happens to be flying past.
        public static readonly Color Dust = new Color(0.90f, 0.90f, 0.90f);
        public static readonly Color HazardRed = new Color(0.85f, 0.20f, 0.15f);
        /// Reward gold — hearts, bells, stars, the daily gift, gold UI
        /// accents. One gold for everything the game gives you: rewards
        /// must never share the hazard red (same-hue pickups and dangers
        /// read as one thing, especially for red-green colourblind
        /// players). New reward visuals reference this, never a literal.
        public static readonly Color Gold = new Color(1.00f, 0.84f, 0.25f);
        /// Moving air — updraft columns, mirror panes, Gloomfang's spark.
        /// The wind/mirror family: one pale cyan everywhere air is the
        /// subject, so "invisible" forces still read as one substance.
        public static readonly Color Air = new Color(0.65f, 0.92f, 1f);
        public static readonly Color CheckpointOff = new Color(0.55f, 0.58f, 0.65f);
        public static readonly Color CheckpointOn = new Color(0.30f, 0.95f, 0.40f);
        /// The Long Winter family: snow-soft tops, ice gates and frost
        /// props share one pale glacial blue so "frozen" reads everywhere
        /// the same way; the sunstone lantern keeps the reward gold so
        /// "what the world gives you" stays one hue.
        public static readonly Color Snow = new Color(0.93f, 0.95f, 0.99f);
        /// Rain, pale and blue-shifted against Snow: the two weathers fall
        /// in different realms, and a flurry must never be mistaken for a
        /// shower at a glance.
        public static readonly Color Rain = new Color(0.72f, 0.82f, 0.95f);
        public static readonly Color IceBlue = new Color(0.62f, 0.85f, 0.98f);
        /// The Undercloud's glowing stones: the story says Gloomfang carried
        /// every one down himself. Pale cyan-white, cooler than the portal
        /// and dimmer than a gem, so in the dark realm they read as the only
        /// human warmth in the gloom — lanterns someone left behind.
        public static readonly Color GloomStone = new Color(0.60f, 0.85f, 0.95f);
        public static readonly Color FrostedLeaf = new Color(0.72f, 0.80f, 0.80f);
        public static readonly Color FrostedRock = new Color(0.84f, 0.87f, 0.92f);
        /// Props families: trunk/leaf/rock dress every platform, the pale
        /// bud is the Sky Garden's sleeping flower, wood is the Homecoming's
        /// see-saw planks. One constant per material family — new props
        /// reference these instead of re-inventing the hue.
        public static readonly Color Trunk = new Color(0.45f, 0.30f, 0.18f);
        public static readonly Color Leaf = new Color(0.30f, 0.62f, 0.28f);
        public static readonly Color Rock = new Color(0.52f, 0.54f, 0.58f);
        public static readonly Color Bud = new Color(0.80f, 0.88f, 0.70f);
        public static readonly Color Wood = new Color(0.62f, 0.45f, 0.28f);
        /// The Aurora Festival family, in shimmer order. Ribbons, sky bands
        /// and finale visuals drift through exactly these four, in this
        /// order, so every aurora surface breathes in unison.
        public static readonly Color AuroraGreen = new Color(0.35f, 0.95f, 0.65f);
        public static readonly Color AuroraCyan = new Color(0.35f, 0.80f, 0.95f);
        public static readonly Color AuroraViolet = new Color(0.75f, 0.55f, 0.98f);
        public static readonly Color AuroraPink = new Color(0.98f, 0.55f, 0.85f);
        /// The aurora shimmer cycle, in drift order.
        public static readonly Color[] Aurora = { AuroraGreen, AuroraCyan,
            AuroraViolet, AuroraPink };
        /// The flower-petal pastels, shared by platform flowers, poke
        /// flowers and the garden bloom wave — one garden, one palette.
        public static readonly Color[] Pastels =
        {
            new Color(1.00f, 0.70f, 0.80f),
            new Color(1.00f, 0.92f, 0.55f),
            new Color(0.65f, 0.82f, 1.00f),
            new Color(0.98f, 0.98f, 0.94f),
            new Color(0.80f, 0.70f, 0.95f)
        };

        /// One hover bob for everything that floats and waits to be
        /// collected. Gems, golden gems, the daily star, hearts and the
        /// lantern sunstone all used their own frequency (2.0-2.5), so two
        /// collectibles in the same level drifted visibly out of phase and
        /// read as unrelated objects. Amplitudes stay per-item (a big gem
        /// and a small heart should not travel the same distance).
        public const float HoverBobRate = 2.2f;

        static Shader standardShader;

        /// Lit material with an optional emission glow (emission &gt; 0).
        /// URP/Lit: main colour maps to _BaseColor via Material.color, and
        /// the emission keyword/property keep their Standard-shader names.
        public static Material Solid(Color color, float emission)
        {
            if (standardShader == null)
            {
                standardShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            Material mat = new Material(standardShader);
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.35f);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }

        /// Turns a URP/Lit material into a true Fade (alpha-blended)
        /// material. Setting the color's alpha alone does nothing — the
        /// surface/blend state, keywords and render queue must all be
        /// configured, or the material renders opaque (the original cloud
        /// bug, URP edition).
        public static void SetFade(Material mat, float alpha)
        {
            mat.SetFloat("_Surface", 1f); // Transparent surface type
            mat.SetFloat("_Blend", 0f);   // Alpha blend
            mat.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_AlphaClip", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }

        /// Builds a cube used purely as decoration (no collider).
        public static GameObject DecorCube(Transform parent, Vector3 position, Vector3 scale,
            Quaternion rotation, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider col = go.GetComponent<Collider>();
            Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = rotation;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// Builds a sphere used purely as decoration (no collider).
        public static GameObject DecorSphere(Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider col = go.GetComponent<Collider>();
            Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
