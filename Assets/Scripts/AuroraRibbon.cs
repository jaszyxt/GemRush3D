using UnityEngine;

namespace GemRush
{
    /// The Aurora Festival's ride: a flowing bridge of light. Unlike a
    /// mover's straight shuttle, a ribbon flows along its travel path
    /// while swaying sideways in a gentle S-curve — standing on one feels
    /// like riding a wave of aurora. Subclasses MovingPlatform, so every
    /// existing rider (the platform-carry in PlayerController.TryRide,
    /// the audit's standable tops) already understands it.
    public class AuroraRibbon : MovingPlatform
    {
        public float sway = 1.5f;   // sideways amplitude, units
        public float waves = 1f;    // sway half-waves per period

        Vector3 startPosition;
        Vector3 swayAxis;
        Rigidbody rb;

        public static AuroraRibbon Create(Transform parent, AuroraRibbonSpec spec)
        {
            GameObject go = new GameObject("AuroraRibbon");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = spec.Center;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = spec.Size;

            // The ribbon slab: translucent, luminous, hue-drifting through
            // the shared aurora palette (ArtLib.Aurora).
            Material ribbon = ArtLib.Solid(ArtLib.Aurora[0], 0.9f);
            ArtLib.SetFade(ribbon, 0.7f);
            ArtLib.DecorCube(go.transform, Vector3.zero, spec.Size,
                Quaternion.identity, ribbon);
            // A soft underside glow so it reads from below too.
            Material underglow = ArtLib.Solid(ArtLib.Aurora[1], 0.6f);
            ArtLib.SetFade(underglow, 0.3f);
            ArtLib.DecorCube(go.transform,
                new Vector3(0f, -spec.Size.y * 0.6f, 0f),
                new Vector3(spec.Size.x * 0.8f, 0.16f, spec.Size.z * 0.85f),
                Quaternion.identity, underglow);

            AuroraRibbon r = go.AddComponent<AuroraRibbon>();
            r.moveOffset = spec.Travel;
            r.period = spec.Period;
            r.sway = spec.Sway;
            r.waves = spec.Waves;
            r.startPosition = spec.Center;
            r.swayAxis = Vector3.Cross(Vector3.Normalize(spec.Travel),
                Vector3.up);
            if (swayAxisFlat(r.swayAxis) < 0.05f)
                r.swayAxis = Vector3.right; // vertical travel: sway in X
            r.rb = go.GetComponent<Rigidbody>(); // added by base Awake
            return r;
        }

        static float swayAxisFlat(Vector3 v)
        {
            return Mathf.Abs(v.x) + Mathf.Abs(v.z);
        }

        void Start()
        {
            // Hue-shimmer driver: the ribbon drifts through the aurora
            // palette on its own clock (small area, slow — audited cadence).
            StartCoroutine(Shimmer());
        }

        System.Collections.IEnumerator Shimmer()
        {
            // Grab BOTH materials: the slab (first renderer found) and the
            // underglow (last renderer found). Previously only the slab
            // shimmered — the underglow stayed frozen AuroraCyan forever.
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            Material slab = renderers.Length > 0 ? renderers[0].material : null;
            Material underglow = renderers.Length > 1
                ? renderers[renderers.Length - 1].material : null;
            const float FadeAlpha = 0.7f;
            const float GlowAlpha = 0.3f;
            int a = 0;
            while (true)
            {
                if (slab != null)
                {
                    Color from = ArtLib.Aurora[a % ArtLib.Aurora.Length];
                    Color to = ArtLib.Aurora[(a + 1) % ArtLib.Aurora.Length];
                    // The underglow chases one palette entry behind, so
                    // the two surfaces visibly pursue each other through
                    // the aurora — a chasing wave, not a uniform flash.
                    Color glowFrom = ArtLib.Aurora[(a + 1) % ArtLib.Aurora.Length];
                    Color glowTo = ArtLib.Aurora[(a + 2) % ArtLib.Aurora.Length];
                    Color emissionFrom = from * 0.9f;
                    Color emissionTo = to * 0.9f;
                    for (float k = 0f; k < 1f; k += Time.deltaTime / 2.2f)
                    {
                        Color slabC = Color.Lerp(from, to, k);
                        slab.color = new Color(slabC.r, slabC.g, slabC.b,
                            FadeAlpha);
                        slab.SetColor("_EmissionColor",
                            Color.Lerp(emissionFrom, emissionTo, k));
                        if (underglow != null)
                        {
                            Color glowC = Color.Lerp(glowFrom, glowTo, k);
                            underglow.color = new Color(glowC.r, glowC.g,
                                glowC.b, GlowAlpha);
                        }
                        yield return null;
                    }
                }
                else yield return null;
                a++;
            }
        }

        void FixedUpdate()
        {
            Vector3 previous = rb.position;
            // Same eased out-and-back phase as a mover...
            float phase = (Mathf.Sin((Time.time / period) * Mathf.PI * 2f
                - Mathf.PI * 0.5f) + 1f) * 0.5f;
            // ...plus a perpendicular sway riding on raw time, so the
            // path is a flowing curve rather than a straight shuttle.
            float swayT = Mathf.Sin((Time.time / period) * Mathf.PI * 2f
                * waves);
            Vector3 next = startPosition + moveOffset * phase
                + swayAxis * (sway * swayT * Mathf.Sin(phase * Mathf.PI));
            rb.MovePosition(next);
            Velocity = (next - previous) / Time.fixedDeltaTime;
        }
    }
}
