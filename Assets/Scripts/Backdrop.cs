using UnityEngine;

namespace GemRush
{
    /// Painted gradient sky locked to the camera, plus distant island
    /// silhouettes on two parallax rigs that follow the camera at fractional
    /// speeds — instant depth, zero shaders, zero assets.
    public class Backdrop : MonoBehaviour
    {
        Transform cam;
        Vector3 origin;
        Transform farRig;
        Transform nearRig;
        float drift;

        public static void Create(Transform cameraTransform, Color skyBase)
        {
            GameObject go = new GameObject("Backdrop");
            Backdrop backdrop = go.AddComponent<Backdrop>();
            backdrop.cam = cameraTransform;
            backdrop.origin = cameraTransform.position;

            // Gradient sky quad parented to the camera so it always fills view.
            // Top/bottom shades derive from the level's sky colour so the
            // Undercloud gets a dusk gradient for free.
            Material skyMat = UnlitMaterial("Unlit/Texture", Color.white);
            skyMat.mainTexture = GradientTexture(
                new Color(skyBase.r * 0.42f, skyBase.g * 0.45f, skyBase.b * 0.55f),
                skyBase * 0.9f);
            GameObject sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(sky.GetComponent<Collider>());
            sky.name = "SkyGradient";
            sky.transform.SetParent(cameraTransform, false);
            sky.transform.localPosition = new Vector3(0f, 24f, 150f);
            sky.transform.localScale = new Vector3(360f, 190f, 1f);
            sky.GetComponent<MeshRenderer>().sharedMaterial = skyMat;

            Color farColor = Color.Lerp(skyBase * 0.6f, new Color(0.30f, 0.46f, 0.74f), 0.35f);
            Color nearColor = Color.Lerp(skyBase * 0.75f, new Color(0.38f, 0.55f, 0.80f), 0.35f);

            backdrop.farRig = BuildIslandLayer("ParallaxFar", farColor, 0.30f, 46f).transform;
            backdrop.nearRig = BuildIslandLayer("ParallaxNear", nearColor, 0.55f, 30f).transform;
        }

        /// Shader lookup with an unlit fallback: Shader.Find returns null
        /// on device for shaders stripped from the build, which used to abort
        /// level loading entirely. URP/Unlit is pinned via EnsureShaders, so
        /// the worst case is a solid-colour backdrop, never a frozen menu.
        static Material UnlitMaterial(string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material mat = new Material(shader);
            if (shaderName == "Unlit/Color") mat.color = color;
            return mat;
        }

        /// Builds a rig of island silhouettes spread along the course.
        /// Each silhouette is a body slab plus a lighter "turf" cap and
        /// 0–2 peak cubes, so the horizon reads as craggy floating
        /// islands rather than blue rectangles.
        static Transform BuildIslandLayer(string name, Color color,
            float followFactor, float lateralOffset)
        {
            GameObject rig = new GameObject(name);
            rig.transform.SetParent(null);
            // The body: darker than the passed colour for aerial
            // perspective — distant islands sit lower-contrast against
            // the sky than near objects, but still need silhouette.
            Material bodyMat = UnlitMaterial("Unlit/Color",
                Color.Lerp(color * 0.65f, Color.black, 0.15f));
            // The turf cap: lighter than the body, faking a sunlit top.
            Material topMat = UnlitMaterial("Unlit/Color",
                Color.Lerp(color, Color.white, 0.15f));

            for (int i = 0; i < 7; i++)
            {
                float z = -30f + i * 38f;
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = side * (lateralOffset + (i * 7) % 18);
                float y = -6f + (i * 11) % 16;
                float w = 14f + (i * 5) % 14;

                GameObject island = new GameObject("Silhouette");
                island.transform.SetParent(rig.transform, false);
                island.transform.localPosition = new Vector3(x, y, z);

                // Body slab: the island's mass.
                ArtLib.DecorCube(island.transform, new Vector3(0f, 0f, 0f),
                    new Vector3(w, w * 0.22f, 6f), Quaternion.identity, bodyMat);
                ArtLib.DecorCube(island.transform,
                    new Vector3(0f, -w * 0.16f, 0f),
                    new Vector3(w * 0.6f, w * 0.28f, 4.5f), Quaternion.identity, bodyMat);
                // Turf cap: a thin lighter slab on the top face, faking
                // the sun catching the island's upper surface.
                ArtLib.DecorCube(island.transform,
                    new Vector3(0f, w * 0.11f + w * 0.03f, 0f),
                    new Vector3(w * 1.02f, w * 0.05f, 6.2f),
                    Quaternion.identity, topMat);
                // Peaks: 0–2 smaller cubes above the cap, breaking the
                // flat horizon line. Deterministic per island.
                int peaks = i % 3; // 0, 1, or 2
                for (int p = 0; p < peaks; p++)
                {
                    float px = (p == 0 ? -1f : 1f) * w * 0.18f;
                    float pw = w * (0.14f + 0.06f * p);
                    float ph = w * (0.10f + 0.05f * (i + p) % 3);
                    ArtLib.DecorCube(island.transform,
                        new Vector3(px, w * 0.14f + ph * 0.5f, 0f),
                        new Vector3(pw, ph, 4f), Quaternion.identity, bodyMat);
                }
            }
            return rig.transform;
        }

        static Texture2D GradientTexture(Color top, Color bottom)
        {
            int w = 8;
            int h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 delta = cam.position - origin;
            drift += Time.deltaTime * 0.4f;
            float sway = Mathf.Sin(drift) * 1.5f;

            if (farRig != null)
                farRig.position = origin + new Vector3(
                    delta.x * 0.25f + sway, delta.y * 0.18f, delta.z * 0.25f);
            if (nearRig != null)
                nearRig.position = origin + new Vector3(
                    delta.x * 0.5f - sway, delta.y * 0.35f, delta.z * 0.5f);
        }
    }
}
