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
        public static readonly Color CheckpointOff = new Color(0.55f, 0.58f, 0.65f);
        public static readonly Color CheckpointOn = new Color(0.30f, 0.95f, 0.40f);

        static Shader standardShader;

        /// Standard-shader material with an optional emission glow (emission &gt; 0).
        public static Material Solid(Color color, float emission)
        {
            if (standardShader == null)
            {
                standardShader = Shader.Find("Standard");
            }
            Material mat = new Material(standardShader);
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.35f);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }

        /// Turns a Standard-shader material into a true Fade (alpha-blended)
        /// material. Setting the color's alpha alone does nothing — the blend
        /// state, keywords and render queue must all be configured, or the
        /// material renders opaque (the original cloud bug).
        public static void SetFade(Material mat, float alpha)
        {
            mat.SetFloat("_Mode", 2f); // Fade
            mat.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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
