using UnityEngine;

namespace GemRush
{
    /// Gloomfang, weather support (badge official since the homecoming).
    /// After the befriending he tags along on Pip's post-story adventures.
    ///
    /// View contract (player-reported, non-negotiable): he is anchored to
    /// the CAMERA's right edge, never between the camera and Pip, and his
    /// body is vapor-translucent — even at the worst angle he cannot block
    /// the view of the player.
    public class Gloomfang : MonoBehaviour
    {
        Transform target;
        Transform bodyVisual;
        Transform pupilL;
        Transform pupilR;
        Transform spark;
        Material sparkMaterial;
        Vector3 velocity;
        float bob;
        bool mirror; // mirror twin: haunts the opposite side of the sky
        float sparkTimer;
        float bodyScale = 1f;
        float wobble;    // giggle wobble energy, decaying
        float wobbleAge;
        bool shading;    // nap watch: hovering right above sleeping Pip
        Transform shadeBlob;

        /// The level's companion (never the mirror twin): PlayerController
        /// finds him for jump giggles, the idle ladder for nap shade.
        public static Gloomfang Companion { get; private set; }

        /// One raindrop in the air at a time.
        public static bool DropInFlight { get; set; }

        // Pip's jumps read as "nearby" from ~4.5 u out: the view contract
        // already parks him 3.2 u right and 2.2 u back of Pip, so a tighter
        // ring could never fire.
        const float NearbyRadius = 4.5f;
        const float RaindropCooldown = 10f;
        static float nextRaindropAt;

        /// Shared body builder: soft overlapping spheres at vapor opacity.
        /// Used by the follower AND the playable Gloomfang so they always
        /// look identical. Eyes and badge stay fully opaque for expression.
        public static void BuildBody(Transform parent, float scale,
            out Transform body, out Transform pupilL, out Transform pupilR)
        {
            body = new GameObject("Body").transform;
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * scale;

            Material vapor = ArtLib.Solid(new Color(0.72f, 0.76f, 0.88f), 0f);
            ArtLib.SetFade(vapor, 0.78f);
            Material vaporLight = ArtLib.Solid(new Color(0.80f, 0.84f, 0.94f), 0f);
            ArtLib.SetFade(vaporLight, 0.78f);
            Material belly = ArtLib.Solid(new Color(0.60f, 0.64f, 0.80f), 0f);
            ArtLib.SetFade(belly, 0.82f);

            ArtLib.DecorSphere(body.transform, Vector3.zero,
                new Vector3(2.6f, 1.9f, 1.9f), vapor);
            ArtLib.DecorSphere(body.transform, new Vector3(-0.55f, 0.5f, 0f),
                new Vector3(1.5f, 1.2f, 1.4f), vaporLight);
            ArtLib.DecorSphere(body.transform, new Vector3(0.65f, 0.45f, 0f),
                new Vector3(1.35f, 1.1f, 1.3f), vaporLight);
            ArtLib.DecorSphere(body.transform, new Vector3(0.05f, -0.4f, 0f),
                new Vector3(2.0f, 1.1f, 1.6f), belly);

            // Eyes and badge stay opaque — the character must stay readable.
            Material eyeWhite = ArtLib.Solid(new Color(0.97f, 0.97f, 1f), 0f);
            Material pupil = ArtLib.Solid(new Color(0.1f, 0.1f, 0.12f), 0f);
            pupilL = BuildEye(body.transform, eyeWhite, pupil,
                new Vector3(-0.38f, 0.14f, 0.72f));
            pupilR = BuildEye(body.transform, eyeWhite, pupil,
                new Vector3(0.38f, 0.14f, 0.72f));

            ArtLib.DecorCube(body.transform, new Vector3(-1.0f, -0.15f, 0.45f),
                new Vector3(0.28f, 0.28f, 0.08f), Quaternion.identity,
                ArtLib.Solid(ArtLib.Gold, 0.4f));
        }

        static Transform BuildEye(Transform body, Material white, Material pupilMat,
            Vector3 position)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(eye.GetComponent<SphereCollider>());
            eye.transform.SetParent(body, false);
            eye.transform.localPosition = position;
            eye.transform.localScale = new Vector3(0.42f, 0.34f, 0.16f);
            eye.GetComponent<MeshRenderer>().sharedMaterial = white;

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(dot.GetComponent<SphereCollider>());
            dot.transform.SetParent(eye.transform, false);
            dot.transform.localPosition = new Vector3(0f, -0.08f, 0.5f);
            dot.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
            dot.GetComponent<MeshRenderer>().sharedMaterial = pupilMat;
            return dot.transform;
        }

        public static void Create(Transform parent, Transform followTarget,
            bool mirror = false)
        {
            GameObject go = new GameObject(mirror ? "MirrorGloomfang" : "Gloomfang");
            go.transform.SetParent(parent, false);
            Gloomfang g = go.AddComponent<Gloomfang>();
            g.target = followTarget;
            g.mirror = mirror;
            if (!mirror) Companion = g;
            BuildBody(go.transform, 1f, out g.bodyVisual, out g.pupilL, out g.pupilR);

            if (mirror)
            {
                // The mirror twin is a reflection: same shape, ghostlier.
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    Color c = r.sharedMaterial.color;
                    c.a = 0.55f;
                    Material m = new Material(r.sharedMaterial);
                    ArtLib.SetFade(m, 0.55f);
                    if (c.r < 0.2f && c.b > 0.05f && c.b < 0.2f) { } // pupils stay solid
                    r.sharedMaterial = m;
                    r.sharedMaterial.color = c;
                }
            }

            // The occasional tiny spark flicker under his belly.
            g.sparkMaterial = ArtLib.Solid(ArtLib.Air, 2f);
            g.spark = ArtLib.DecorCube(g.bodyVisual,
                new Vector3(0.35f, -0.85f, 0.3f),
                new Vector3(0.3f, 0.45f, 0.1f), Quaternion.identity,
                g.sparkMaterial).transform;
            g.spark.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (target == null) return;
            bob += Time.deltaTime;

            // View contract: anchor to the CAMERA's right edge. He lives in
            // the right quarter of the screen by construction and can never
            // end up between the camera and Pip.
            Vector3 right = Vector3.right;
            Vector3 fwd = Vector3.forward;
            Camera cam = Camera.main;
            if (cam != null)
            {
                right = cam.transform.right;
                fwd = cam.transform.forward;
            }
            right.y = 0f; fwd.y = 0f;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            right.Normalize(); fwd.Normalize();

            Vector3 anchor = mirror
                ? new Vector3(-target.position.x, 0f, target.position.z)
                : target.position;
            Vector3 station;
            if (shading)
            {
                // Nap watch: directly above sleeping Pip, a shade against
                // the sun until he wakes.
                station = anchor + Vector3.up *
                    (2.6f + Mathf.Sin(bob * 1.3f) * 0.15f);
            }
            else
            {
                station = anchor
                    + right * 3.2f
                    + Vector3.up * (1.5f + Mathf.Sin(bob * 1.3f) * 0.25f)
                    - fwd * 2.2f;
            }

            // Critically-damped chase: never snaps, never overshoots.
            Vector3 toStation = station - transform.position;
            velocity = Vector3.Lerp(velocity,
                toStation * 2.2f, 1f - Mathf.Exp(-4f * Time.deltaTime));
            transform.position += velocity * Time.deltaTime;

            // Body leans into his own drift; a giggle wobbles the whole
            // cloud with a scale-and-yaw pulse.
            float wiggle = 0f;
            float puff = 0f;
            if (wobble > 0f)
            {
                wobbleAge += Time.deltaTime;
                wobble = Mathf.Max(0f, wobble - Time.deltaTime * 1.3f);
                // ~1.4 Hz: a soft jiggle, kept under the ~1.8 Hz comfort
                // law (motion, but never flicker).
                float s = Mathf.Sin(wobbleAge * 9f) * wobble;
                puff = s * 0.07f;
                wiggle = s * 14f;
            }
            if (bodyVisual != null)
            {
                bodyVisual.localScale = Vector3.one * (bodyScale * (1f + puff));
                bodyVisual.localRotation = Quaternion.Euler(
                    Mathf.Clamp(-velocity.y * 0.8f, -8f, 8f),
                    wiggle,
                    Mathf.Clamp(velocity.x * 0.8f, -8f, 8f));
            }

            // Pupils drift toward Pip, because what else is there to look at.
            if (pupilL != null && pupilR != null)
            {
                Vector3 gaze = new Vector3(
                    Mathf.Clamp(velocity.x * 0.01f, -0.06f, 0.06f),
                    Mathf.Clamp(velocity.y * 0.008f, -0.05f, 0.05f), 0f);
                pupilL.localPosition = gaze;
                pupilR.localPosition = gaze;
            }

            // The occasional tiny spark: blink on briefly, then shy away.
            sparkTimer -= Time.deltaTime;
            if (sparkTimer <= 0f)
            {
                if (spark != null && spark.gameObject.activeSelf)
                {
                    spark.gameObject.SetActive(false);
                    sparkTimer = Random.Range(2.5f, 6f);
                }
                else
                {
                    if (spark != null) spark.gameObject.SetActive(true);
                    sparkTimer = Random.Range(0.15f, 0.35f);
                }
            }
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Companion, this)) Companion = null;
        }

        // Near-collect spark: a delighted blink when Pip gathers gems
        /// close by — he loves watching his old job done well.
        const float SparkCooldown = 1.2f;
        const float SparkHoldSeconds = 0.5f;
        static float nextSparkAt;

        /// Pip collected a gem somewhere near Gloomfang: the spark flashes
        /// on, a happy wobble, a mote of weather-pale light. Cooldown keeps
        /// gem trails from strobing him.
        public static void OnGemCollectedNear(Vector3 gemPosition)
        {
            Gloomfang g = Companion;
            if (g == null) return;
            if (Time.time < nextSparkAt) return;
            Vector3 flat = gemPosition - g.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > NearbyRadius * NearbyRadius) return;

            nextSparkAt = Time.time + SparkCooldown;
            if (g.spark != null && !g.spark.gameObject.activeSelf)
            {
                g.spark.gameObject.SetActive(true);
                g.sparkTimer = SparkHoldSeconds;
            }
            g.wobble = 0.7f;
            g.wobbleAge = 0f;
            Fx.Burst(g.transform.position, ArtLib.Air * 1.3f, 8);
        }

        /// Pip jumped nearby: a happy wobble, a shy giggle, and — his old
        /// joy, remembered — one soft raindrop that blooms where it lands.
        public static void OnPipJumped(Vector3 pipPosition)
        {
            Gloomfang g = Companion;
            if (g == null) return;
            if (Time.time < nextRaindropAt) return;
            Vector3 flat = pipPosition - g.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > NearbyRadius * NearbyRadius) return;

            nextRaindropAt = Time.time + RaindropCooldown;
            g.wobble = 1f;
            g.wobbleAge = 0f;
            AudioManager.Instance.PlaySoftGiggle(
                AudioManager.Falloff(g.transform.position, 20f));
            if (!DropInFlight)
            {
                DropInFlight = true;
                GloomfangRaindrop.Spawn(g, pipPosition);
            }
        }

        /// Nap watch: while Pip sleeps he drifts from his perch to hover
        /// right above him, casting a soft shade until wake. A null sleeper
        /// sends him back to the camera edge.
        public void SetShade(Transform sleeper)
        {
            bool want = sleeper != null;
            if (want == shading) return;
            shading = want;
            if (shading)
            {
                shadeBlob = GameObject.CreatePrimitive(PrimitiveType.Sphere)
                    .transform;
                SphereCollider col = shadeBlob.gameObject
                    .GetComponent<SphereCollider>();
                if (col != null) Object.Destroy(col); // decoration only
                shadeBlob.name = "Shade";
                shadeBlob.SetParent(sleeper, false);
                shadeBlob.localPosition = new Vector3(0f, -0.95f, 0f);
                shadeBlob.localScale = new Vector3(1.5f, 0.1f, 1.5f);
                Material m = ArtLib.Solid(new Color(0.30f, 0.34f, 0.48f), 0f);
                ArtLib.SetFade(m, 0.22f);
                shadeBlob.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            else if (shadeBlob != null)
            {
                Object.Destroy(shadeBlob.gameObject);
                shadeBlob = null;
            }
        }
    }

    /// The single raindrop Gloomfang lets fall when Pip jumps near him:
    /// a small Air-colored bead, a soft accelerating fall, and where it
    /// lands a tiny flower blooms. Purely cosmetic — no collider, no
    /// gameplay — and it cleans itself up once the bloom settles.
    [UnityEngine.Scripting.Preserve]
    class GloomfangRaindrop : MonoBehaviour
    {
        const float FallSeconds = 0.6f;
        const float BloomSeconds = 0.5f;

        Vector3 start;
        Vector3 end;
        float age;
        Transform bead;
        Transform flower;
        bool landed;

        public static void Spawn(Gloomfang from, Vector3 pipPosition)
        {
            GameObject go = new GameObject("Raindrop");
            go.transform.SetParent(from.transform.parent, false);
            GloomfangRaindrop drop = go.AddComponent<GloomfangRaindrop>();

            // Land one step toward Gloomfang's side of Pip, at Pip's feet.
            Vector3 side = from.transform.position - pipPosition;
            side.y = 0f;
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            else side.Normalize();
            drop.end = pipPosition + side * 0.9f;
            drop.end.y = pipPosition.y - 0.95f;
            drop.start = from.transform.position + Vector3.down * 0.4f;
            go.transform.position = drop.start;

            drop.bead = GameObject.CreatePrimitive(PrimitiveType.Sphere)
                .transform;
            SphereCollider col = drop.bead.gameObject
                .GetComponent<SphereCollider>();
            if (col != null) Object.Destroy(col); // decoration only
            drop.bead.SetParent(go.transform, false);
            drop.bead.localPosition = Vector3.zero;
            drop.bead.localScale = new Vector3(0.14f, 0.18f, 0.14f);
            drop.bead.GetComponent<MeshRenderer>().sharedMaterial =
                ArtLib.Solid(ArtLib.Air, 1.2f);
        }

        void Update()
        {
            // Paused-safe clock (D-5 class: a visual state machine on the
            // wrong clock). The drop used to keep falling and land behind
            // the pause menu; the same defect the checkpoint twirl had.
            // Decorations are allowed to run while paused because they
            // only bob in place — a drop that RESOLVES into a landing and
            // a flower does not.
            age += Time.timeScale > 0f ? Time.deltaTime : 0f;
            if (!landed)
            {
                float k = Mathf.Clamp01(age / FallSeconds);
                // Ease-in fall: gravity reads in the accelerating half.
                transform.position = Vector3.Lerp(start, end, k * k);
                if (k >= 1f) Land();
            }
            else
            {
                float k = Mathf.Clamp01(age / BloomSeconds);
                float pop = Mathf.Sin(k * Mathf.PI) * 0.22f;
                if (flower != null)
                    flower.localScale = Vector3.one *
                        (Mathf.Lerp(0.05f, 1f, k) * (1f + pop));
                if (k >= 1f)
                {
                    flower = null; // the bloom stays; only the spawner leaves
                    Destroy(gameObject);
                }
            }
        }

        void Land()
        {
            landed = true;
            age = 0f;
            Gloomfang.DropInFlight = false;
            if (bead != null) bead.gameObject.SetActive(false);
            flower = Props.SproutFlower(transform.parent, end);
            // Activation and scale are set in the same statement pair so the
            // flower is never drawn at full size for a frame.
            if (flower != null)
            {
                flower.localScale = Vector3.one * 0.05f;
                flower.gameObject.SetActive(true);
            }
            AudioManager.Instance.PlayRaindropBloom(
                AudioManager.Falloff(end, 16f));
            Fx.PetalPuff(end + Vector3.up * 0.05f, ArtLib.Air, 3);
        }

        void OnDestroy()
        {
            // A world torn down mid-fall must never strand the "one at a
            // time" gate.
            Gloomfang.DropInFlight = false;
        }
    }
}
