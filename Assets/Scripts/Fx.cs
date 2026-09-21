using UnityEngine;

namespace GemRush
{
    /// One-shot particle helpers and shared procedural sprites.
    public static class Fx
    {
        static Sprite cachedCircle;
        static Sprite cachedStar;
        static Texture2D cachedRing;
        static Texture2D cachedDisc;
        static Sprite cachedTopScrim;

        /// A top-edge scrim for the HUD band: opaque-ish dark at the very
        /// top, fading to nothing at the bottom. The HUD sits on bare sky
        /// with no panel, and against the brightest realms (winter 0.80,
        /// 0.87, 0.95 / pale blue) white text measures ~1.6:1 and the gold
        /// lives counter ~1.02:1 — effectively invisible on a phone in
        /// daylight. A gradient reads as a lens falloff rather than a UI
        /// bar, so it frames the view instead of boxing it.
        public static Sprite TopScrimSprite()
        {
            if (cachedTopScrim != null) return cachedTopScrim;
            int h = 64;
            Texture2D tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                // Row 0 = bottom of the sprite. Solid at the top, gone by
                // the bottom edge, squared so it hugs the very top band.
                float t = y / (float)(h - 1);
                float a = t * t;
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0.03f, a));
            }
            tex.Apply();
            cachedTopScrim = Sprite.Create(tex, new Rect(0f, 0f, 4f, h),
                new Vector2(0.5f, 0.5f), 100f);
            return cachedTopScrim;
        }

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

        // Shared particle materials (fx pooling stage 1): Burst/PetalPuff/
        // Celebration never tint their material — color lives in the
        // particle startColor/gradient — so one cached white material per
        // texture variant replaces a new-Material + Shader.Find on every
        // effect call (~1000 orphaned materials per long level before).
        // Ring stays per-call: it animates material color per frame.
        static Shader particleShader;
        static Material burstMaterial;
        static Material petalMaterial;
        static Material celebrationMaterial;

        static Material SharedBurstMaterial()
        {
            if (particleShader == null)
                particleShader = Shader.Find(
                    "Universal Render Pipeline/Particles/Unlit");
            if (burstMaterial == null && particleShader != null)
                burstMaterial = new Material(particleShader);
            return burstMaterial;
        }

        static Material SharedPetalMaterial()
        {
            if (petalMaterial == null && SharedBurstMaterial() != null)
            {
                petalMaterial = new Material(SharedBurstMaterial());
                petalMaterial.mainTexture = DiscTexture();
            }
            return petalMaterial;
        }

        static Material SharedCelebrationMaterial(Texture2D texture)
        {
            // Untextured celebrations share one material; the one textured
            // variant (petals) gets its own cached instance.
            if (texture == null)
            {
                return SharedBurstMaterial();
            }
            if (celebrationMaterial == null && SharedBurstMaterial() != null)
            {
                celebrationMaterial = new Material(SharedBurstMaterial());
                celebrationMaterial.mainTexture = texture;
            }
            return celebrationMaterial;
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

        /// Floating world-space text (e.g. "+1" on gem pickup). `scale`
        /// lets the caller ramp the size with the combo streak so mastery
        /// reads at a glance, not just in the pitch ramp.
        public static void Popup(Vector3 position, string text, Color color,
            float scale = 1f)
        {
            GameObject go = new GameObject("Popup");
            go.transform.position = position;
            TextMesh tm = go.AddComponent<TextMesh>();
            Font font = UiFont();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.22f * scale;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
                renderer.sharedMaterial = font.material;

            Transform tr = go.transform;
            Tweener.Value(0f, 1f, 0.9f, delegate (float k)
            {
                if (tr == null) return; // defensive, like Ring/SleepMote
                tr.position = position + Vector3.up * (k * 1.7f);
                Color c = color;
                c.a = 1f - k;
                tm.color = c;
            }, delegate { if (go != null) Object.Destroy(go); });
        }

        /// The built-in UI font, resolved once (Popup and SleepMote share it).
        static Font UiFont()
        {
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
            return uiFont;
        }

        /// One sleepy "z": a tiny wordless mote drifting up and fading on
        /// the unscaled clock — the nap ladder's tell. One text mesh,
        /// auto-destroys.
        public static void SleepMote(Vector3 position)
        {
            GameObject go = new GameObject("SleepMote");
            go.transform.position = position;
            TextMesh tm = go.AddComponent<TextMesh>();
            Font font = UiFont();
            tm.font = font;
            tm.text = "z";
            tm.fontSize = 48;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            Color color = ArtLib.CloudWhite;
            color.a = 0f;
            tm.color = color;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
                renderer.sharedMaterial = font.material;

            Transform tr = go.transform;
            Tweener.Value(0f, 1f, 2.2f, delegate (float k)
            {
                if (go == null) return; // world tore down mid-mote
                tr.position = position + Vector3.up * (k * 1.15f)
                    + Vector3.right * (Mathf.Sin(k * 6.6f) * 0.08f);
                Color c = color;
                c.a = 0.85f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.2f))
                    * (1f - Mathf.Clamp01((k - 0.55f) / 0.45f));
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
            Material shared = SharedBurstMaterial();
            if (shared != null) renderer.sharedMaterial = shared;

            go.AddComponent<AutoDestroy>();
            ps.Play();
        }

        /// A soft puff of tiny round particles on a gentle arc — flower
        /// petals when a poke sings, the raindrop's splash. One-shot,
        /// budget-capped under the 30-particle rule, auto-destroys exactly
        /// like Burst.
        public static void PetalPuff(Vector3 position, Color color, int count)
        {
            if (count > 30) count = 30;
            GameObject go = new GameObject("PetalPuff");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            main.startColor = color;
            main.gravityModifier = 0.55f; // a gentle arc, not a splatter
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, (short)count);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

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
            Material sharedPetal = SharedPetalMaterial();
            if (sharedPetal != null) renderer.sharedMaterial = sharedPetal;

            go.AddComponent<AutoDestroy>();
            ps.Play();
        }

        /// Soft ring band (annulus) texture, white — callers tint it.
        static Texture2D RingTexture()
        {
            if (cachedRing != null) return cachedRing;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / half;
                    // Band centred at 0.78 with soft edges, so the ring
                    // reads chubby-cozy at world scale.
                    float a = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.78f) / 0.14f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            cachedRing = tex;
            return tex;
        }

        /// Milestone shockwave: a gold ring that expands around a point and
        /// fades (~0.5 s, unscaled time). Billboarded to the camera; the
        /// same unlit shader Fx.Burst uses, so it works wherever bursts do.
        public static void Ring(Vector3 position, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>()); // decoration only
            go.name = "Ring";
            go.transform.position = position;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material mat = null;
            if (shader != null)
            {
                mat = new Material(shader);
                mat.mainTexture = RingTexture();
                mat.color = color;
                renderer.sharedMaterial = mat;
            }

            Transform tr = go.transform;
            Tweener.Value(0f, 1f, 0.5f, delegate (float k)
            {
                if (go == null) return; // world tore down mid-ring
                float s = Mathf.Lerp(0.5f, 3.2f, k);
                tr.localScale = new Vector3(s, s, 1f);
                if (mat != null)
                {
                    Color c = color;
                    c.a = 0.9f * (1f - k);
                    mat.color = c;
                }
                // The quad's visible face is its -Z side, so aim its
                // forward AWAY from the camera to face the camera.
                Camera cam = Camera.main;
                if (cam != null)
                    tr.LookAt(tr.position + cam.transform.forward);
            }, delegate { Object.Destroy(go); });
        }

        /// Soft disc texture for round celebration particles (petals).
        static Texture2D DiscTexture()
        {
            if (cachedDisc != null) return cachedDisc;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / (half - 1f);
                    float alpha = 1f;
                    if (d > 0.7f) alpha = Mathf.Clamp01((1f - d) / 0.3f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            cachedDisc = tex;
            return tex;
        }

        /// One-shot celebration burst that rains down with gravity: the
        /// game's reward palette (gold, gem pink, portal cyan) as confetti,
        /// optionally mixed with soft petal circles for the Two Suns.
        /// Whole celebration stays under the 60-particle phone budget and
        /// auto-destroys, exactly like Burst.
        public static void Confetti(Vector3 position, int count,
            bool petals = false)
        {
            if (count > 60) count = 60;
            int petalCount = petals ? Mathf.Min(20, count / 2) : 0;
            int mainCount = count - petalCount;

            // One gradient, three ArtLib families: MinMaxGradient samples a
            // random color per particle, so one emitter carries the palette.
            Gradient palette = new Gradient();
            palette.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(ArtLib.Gold, 0f),
                    new GradientColorKey(ArtLib.GemPink, 0.5f),
                    new GradientColorKey(ArtLib.PortalCyan, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            if (mainCount > 0)
                Celebration(position, mainCount,
                    new ParticleSystem.MinMaxGradient(palette),
                    2.5f, 5.5f, 0.10f, 0.2f, 0.9f, 0.7f, 1.3f, null);
            if (petalCount > 0)
            {
                ParticleSystem.MinMaxGradient pink = ArtLib.GemPink;
                Celebration(position, petalCount, pink,
                    0.8f, 1.8f, 0.28f, 0.45f, 0.25f, 1.4f, 2.1f, DiscTexture());
            }
        }

        /// Shared confetti emitter: one positional burst, world-simulated,
        /// fading out over its lifetime. `texture` rounds the particles.
        static void Celebration(Vector3 position, int count,
            ParticleSystem.MinMaxGradient color,
            float speedMin, float speedMax, float sizeMin, float sizeMax,
            float gravity, float lifeMin, float lifeMax, Texture2D texture)
        {
            GameObject go = new GameObject("Confetti");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, (short)count);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient fadeOut = new Gradient();
            fadeOut.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(fadeOut);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            Material sharedCelebration = SharedCelebrationMaterial(texture);
            if (sharedCelebration != null)
                renderer.sharedMaterial = sharedCelebration;

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
