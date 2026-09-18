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
        public static readonly Color IceBlue = new Color(0.62f, 0.85f, 0.98f);
        public static readonly Color FrostedLeaf = new Color(0.72f, 0.80f, 0.80f);
        public static readonly Color FrostedRock = new Color(0.84f, 0.87f, 0.92f);

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
