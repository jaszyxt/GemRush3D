using UnityEngine;

namespace GemRush
{
    /// One-shot particle helpers and shared procedural sprites.
    public static class Fx
    {
        static Sprite cachedCircle;

        /// A soft-edged white disc with a 9-slice border, shared by
        /// particles, touch controls and UI (stretched = rounded rect).
        public static Sprite CircleSprite()
        {
            if (cachedCircle != null) return cachedCircle;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    float alpha = 1f;
                    if (d > 0.8f) alpha = Mathf.Clamp01((1f - d) / 0.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            float border = size / 3f;
            cachedCircle = Sprite.Create(tex, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            return cachedCircle;
        }

        static Font uiFont;

        /// Floating world-space text (e.g. "+1" on gem pickup).
        public static void Popup(Vector3 position, string text, Color color)
        {
            GameObject go = new GameObject("Popup");
            go.transform.position = position;
            TextMesh tm = go.AddComponent<TextMesh>();
            if (uiFont == null)
            {
                try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch (System.Exception) { uiFont = null; }
                if (uiFont == null)
                {
                    try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                    catch (System.Exception) { uiFont = null; }
                }
            }
            tm.font = uiFont;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.22f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && uiFont != null)
                renderer.sharedMaterial = uiFont.material;

            Transform tr = go.transform;
            Tweener.Value(0f, 1f, 0.9f, delegate(float k)
            {
                tr.position = position + Vector3.up * (k * 1.7f);
                Color c = color;
                c.a = 1f - k;
                tm.color = c;
            }, delegate { Object.Destroy(go); });
        }

        public static void Burst(Vector3 position, Color color, int count)
        {
            GameObject go = new GameObject("Burst");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = color;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, (short)count);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null) renderer.sharedMaterial = new Material(shader);

            go.AddComponent<AutoDestroy>();
            ps.Play();
        }
    }

    /// Destroys its GameObject once the attached particle effect has finished.
    public class AutoDestroy : MonoBehaviour
    {
        float remaining;

        void Start()
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            remaining = ps.main.duration + 1.2f;
        }

        void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) Destroy(gameObject);
        }
    }
}
