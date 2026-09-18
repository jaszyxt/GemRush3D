using UnityEngine;

namespace GemRush
{
    /// One-shot particle helpers and shared procedural sprites.
    public static class Fx
    {
        static Sprite cachedCircle;
        static Sprite cachedStar;

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

        /// A five-pointed star with a soft anti-aliased edge — the win
        /// screen's reward icon, so the symbol the game counts in looks
        /// like its name (it used to be a plain circle). White, so the UI
        /// tints it gold or dim per star.
        public static Sprite StarSprite()
        {
            if (cachedStar != null) return cachedStar;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Outline: ten vertices alternating outer/inner radius, point
            // up. Inner/outer is the pentagram ratio 0.382 nudged to 0.42
            // so the star reads chubby-cozy at UI sizes.
            float half = size * 0.5f;
            float outer = size * 0.46f;
            float inner = outer * 0.42f;
            float[] vx = new float[10];
            float[] vy = new float[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = (0.5f + i * 0.1f) * 2f * Mathf.PI; // 90° + i·36°
                float r = (i % 2 == 0) ? outer : inner;
                vx[i] = half + Mathf.Cos(ang) * r;
                vy[i] = half + Mathf.Sin(ang) * r;
            }

            float soft = 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float minDist = 1e6f;
                    bool inside = false;
                    for (int i = 0; i < 10; i++)
                    {
                        int j = (i + 1) % 10;
                        float ax = vx[i]; float ay = vy[i];
                        float bx = vx[j]; float by = vy[j];

                        // Distance to this edge (projection clamped to it).
                        float abx = bx - ax; float aby = by - ay;
                        float t = ((px - ax) * abx + (py - ay) * aby)
                            / (abx * abx + aby * aby);
                        t = Mathf.Clamp01(t);
                        float dx = px - (ax + abx * t);
                        float dy = py - (ay + aby * t);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d < minDist) minDist = d;

                        // Even-odd crossing test for inside/outside.
                        if ((ay > py) != (by > py) &&
                            px < (bx - ax) * (py - ay) / (by - ay) + ax)
                            inside = !inside;
                    }
                    float signed = inside ? minDist : -minDist;
                    float alpha = Mathf.Clamp01(0.5f + signed / soft);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            cachedStar = Sprite.Create(tex, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            return cachedStar;
        }

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
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
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
