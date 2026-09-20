using UnityEngine;

namespace GemRush
{
    /// A tailwind gust: every Period seconds, a wall of wind blows along
    /// Direction for ActiveTime seconds, carrying Pip across the gap it
    /// spans. Telegraphed by drifting petals; the streaks brighten while
    /// the gust is live. Timing is phase-locked to the music pad — the
    /// default period and active time divide the 8.8 s loop, so every
    /// onset lands on a chord boundary — and each onset adds a wind swell
    /// on the chord root.
    public class GustZone : MonoBehaviour
    {
        public Vector3 direction = new Vector3(0f, 0f, 1f);
        public float period = 4.4f;
        public float activeTime = 2.2f;
        public float strength = 8f;
        public float lift = 0f;

        Transform[] streaks;
        Transform[] petals;
        Material streakMat;
        float t;
        bool wasActive;
        bool wasTelegraph;
        float streakLength;
        Vector3 volumeSize;

        /// How long before the blow Nim giggles — just enough time to step in.
        const float GiggleLead = 0.55f;

        public static void Create(Transform parent, Vector3 center,
            Vector3 size, Vector3 direction, float period, float activeTime,
            float strength, float lift = 0f)
        {
            GameObject go = new GameObject("GustZone");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            BoxCollider volume = go.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = size;

            GustZone g = go.AddComponent<GustZone>();
            g.direction = direction.normalized;
            g.period = period;
            g.activeTime = activeTime;
            g.strength = strength;
            g.lift = lift;
            g.volumeSize = size;
            g.streakLength = Mathf.Max(size.x, size.z) * 1.4f;

            Material streak = ArtLib.Solid(ArtLib.Air, 0f);
            ArtLib.SetFade(streak, 0.22f);
            g.streakMat = streak;

            // Streak walls: thin translucent sheets that sweep with the
            // gust while it blows.
            g.streaks = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(s.GetComponent<BoxCollider>());
                s.transform.SetParent(go.transform, false);
                s.transform.localScale = new Vector3(
                    size.x * 0.95f, size.y * 0.9f, 0.12f);
                s.GetComponent<MeshRenderer>().sharedMaterial = streak;
                g.streaks[i] = s.transform;
            }

            // Petals: tiny pink motes drifting along the direction — the
            // telegraph that this lane blows.
            Material petal = ArtLib.Solid(ArtLib.GemPink, 0f);
            ArtLib.SetFade(petal, 0.7f);
            g.petals = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(p.GetComponent<BoxCollider>());
                p.transform.SetParent(go.transform, false);
                p.transform.localScale = new Vector3(0.14f, 0.1f, 0.2f);
                p.transform.localRotation = Quaternion.Euler(0f, 30f * i, 0f);
                p.GetComponent<MeshRenderer>().sharedMaterial = petal;
                g.petals[i] = p.transform;
            }

            g.OrientVolume(size);
        }

        /// Aligns streaks and their motion with the gust direction. The
        /// volume itself stays axis-aligned; +z and ±x gusts are supported.
        void OrientVolume(Vector3 size)
        {
            bool alongX = Mathf.Abs(direction.x) > 0.5f;
            for (int i = 0; i < streaks.Length; i++)
            {
                if (alongX)
                {
                    streaks[i].localScale = new Vector3(
                        0.12f, size.y * 0.9f, size.z * 0.95f);
                }
                streaks[i].localPosition = new Vector3(0f,
                    -size.y * 0.35f + i * size.y * 0.35f, 0f);
            }
        }

        /// Gust phase within the period, read from the music clock so every
        /// onset lands on a chord boundary (period and active time both
        /// divide the 8.8 s pad loop). The clock keeps its own beat while
        /// the music is silent, so the visuals never freeze.
        float GustPhase()
        {
            float clock = AudioManager.Instance != null
                ? AudioManager.Instance.GetMusicPhase() : t;
            return Mathf.Repeat(clock, period);
        }

        void Update()
        {
            t += Time.deltaTime;
            float phase = GustPhase();
            bool active = phase < activeTime;

            // Each onset adds a wind swell on the chord root — the gust
            // audibly plays the chord it is locked to. Quieter with
            // distance, and sibling lanes sharing the phase collapse into
            // one swell instead of doubling.
            if (active && !wasActive && AudioManager.Instance != null)
                AudioManager.Instance.PlayGustSwell(transform.position);
            wasActive = active;

            // Nim's giggle just before the blow: the invitation the levels
            // promise ("wait for Nim's giggle"). Fires once per cycle in
            // the last half-second of the lull, quieter with distance.
            bool telegraph = !active && phase >= period - GiggleLead;
            if (telegraph && !wasTelegraph && AudioManager.Instance != null)
                AudioManager.Instance.PlayGiggle(
                    AudioManager.Falloff(transform.position, 26f));
            wasTelegraph = telegraph;

            // While blowing, grab every PlayerController whose collider is
            // inside the volume — an OverlapBox, because OnTriggerStay
            // never fires for a standing-still (sleeping) rigidbody, and
            // Pip waits for the gust while standing perfectly still.
            if (active)
            {
                Collider[] hits = Physics.OverlapBox(transform.position,
                    volumeSize * 0.5f, transform.rotation, ~0,
                    QueryTriggerInteraction.Collide);
                foreach (Collider hit in hits)
                {
                    PlayerController player =
                        hit.GetComponentInParent<PlayerController>();
                    if (player != null)
                        player.SetGustPush(direction * strength, lift);
                }
            }

            // Streaks sweep through the volume while the gust blows, then
            // fade back.
            float sweep;
            if (active) sweep = (phase / activeTime) * 2f - 1f;   // -1..1
            else sweep = -1f + (phase - activeTime) * 0.4f;        // parked low
            if (streakMat != null)
            {
                Color c = streakMat.color;
                c.a = active ? 0.45f : 0.12f;
                streakMat.color = c;
            }
            if (streaks != null)
            {
                for (int i = 0; i < streaks.Length; i++)
                {
                    float offset = (sweep + (i - 1) * 0.25f) *
                        streakLength * 0.45f;
                    Vector3 lp = streaks[i].localPosition;
                    lp.x = direction.x * offset;
                    lp.z = direction.z * offset;
                    streaks[i].localPosition = lp;
                }
            }

            // Petals always drift, slower — the invitation.
            if (petals != null)
            {
                for (int i = 0; i < petals.Length; i++)
                {
                    float d = Mathf.Repeat(t * (1.5f + 0.3f * i) +
                        i * 1.9f, streakLength);
                    Vector3 lp = petals[i].localPosition;
                    lp.x = direction.x * (d - streakLength * 0.5f);
                    lp.z = direction.z * (d - streakLength * 0.5f);
                    lp.y = Mathf.Sin(t * 2f + i) * 0.3f;
                    petals[i].localPosition = lp;
                }
            }
        }

        void OnTriggerStay(Collider other)
        {
            // Re-derives the phase from the shared music clock rather than
            // the frame-time accumulator: correct even after pauses.
            if (GustPhase() >= activeTime) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            player.SetGustPush(direction * strength, lift);
        }
    }
}
