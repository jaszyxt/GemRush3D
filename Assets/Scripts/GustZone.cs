using UnityEngine;

namespace GemRush
{
    /// A tailwind gust: every Period seconds, a wall of wind blows along
    /// Direction for ActiveTime seconds, carrying Pip across the gap it
    /// spans. Telegraphed by drifting petals (Nim's giggle) half a second
    /// before each blow; the streaks brighten while the gust is live, and
    /// each onset adds a wind swell on the chord root.
    ///
    /// The cycle runs on the ZONE's own clock, not the music's. The music
    /// only supplies a one-time offset at spawn so the first onset feels
    /// like it lands with the pad. This is deliberate: the duty cycle must
    /// be structural, because a phase derived from the pad breaks on any
    /// mood whose chord length does not divide the period (the Festival's
    /// 13.6 s loop against a 4.4 s period made the finale's crossing
    /// uncrossable — see GustPhase). The swell at each onset keeps the
    /// musical link audible without making gameplay depend on it.
    public class GustZone : MonoBehaviour
    {
        /// The canonical wind settings — THE one place gust feel is
        /// defined. D-10 (user, 2026-09-26): "all the gusts should have
        /// the same settings" — every gust in every level rides this same
        /// wind; level data places lanes (position, size, direction) and
        /// never tunes the wind. Exposed so the audits assert the
        /// gameplay contract against the REAL constants instead of copies
        /// (a duplicated literal in a test cannot catch a regression in
        /// the value it duplicates). The blow is most of the cycle: a
        /// waiting player spends most of their time able to cross, and
        /// the giggle telegraph fills the short lull.
        public const float DefaultPeriod = 4.4f;
        public const float DefaultActiveTime = 3.0f;
        public const float DefaultStrength = 8f;
        public const float DefaultLift = 3f;

        public Vector3 direction = new Vector3(0f, 0f, 1f);
        public float period = DefaultPeriod;
        public float activeTime = DefaultActiveTime;
        public float strength = DefaultStrength;
        public float lift = DefaultLift;

        Transform[] streaks;
        Transform[] petals;
        Material streakMat;
        float t;
        float phaseOffset;   // musical alignment; see GustPhase
        float lockedMusicPhase; // music phase we were aligned at
        bool wasActive;
        bool wasTelegraph;
        float streakLength;
        Vector3 volumeSize;

        /// How long before the blow Nim giggles — just enough time to step in.
        const float GiggleLead = 0.55f;

        public static void Create(Transform parent, Vector3 center,
            Vector3 size, Vector3 direction)
        {
            GameObject go = new GameObject("GustZone");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            BoxCollider volume = go.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = size;

            GustZone g = go.AddComponent<GustZone>();
            g.direction = direction.normalized;
            // D-10: the field initializers above already carry the shared
            // wind; every zone rides the same gust by construction.
            g.volumeSize = size;
            g.streakLength = Mathf.Max(size.x, size.z) * 1.4f;
            // Align the first blow with the music once, at spawn (the pad
            // loop is 8.8s in the wind realms, so this reads as on-beat);
            // from then on the zone keeps its own strict beat, which is
            // what guarantees it always blows. Realigned after any pause —
            // see RelockToMusic.
            g.RelockToMusic();

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

        /// Gust phase within the period. A gust must ALWAYS blow its
        /// active slice of every cycle, so the cycle is driven by the
        /// zone's own accumulated clock — the music only supplies a
        /// one-time offset so onsets feel like they land with the pad.
        ///
        /// This used to read the raw music phase and wrap it by `period`,
        /// which silently broke every mood whose chord length did not
        /// divide the period: the Festival loop is 13.6s (4 x 3.4), not a
        /// multiple of the 4.4s period, so the phase stepped 0 -> 4.4 ->
        /// 8.8 -> 13.2 and only landed inside the 2.2s blow window on the
        /// FIRST cycle. The Festival Finale's crossing became genuinely
        /// uncrossable — the wind never blew again, however long the
        /// player waited, and the giggle telegraph never matched a blow.
        /// Locking to our own clock makes the duty cycle structural
        /// instead of a happy accident of the music.
        float GustPhase()
        {
            return Mathf.Repeat(t + phaseOffset, period);
        }

        /// Re-anchor the musical offset to the live music phase. The zone's
        /// own clock (`t`) freezes with Time.timeScale while the music DSP
        /// clock keeps running, so a pause or a hit-stop leaves the two out
        /// of step FOREVER — after one pause the "gust lands on the chord"
        /// feel was permanently lost, not just momentarily. Called at spawn
        /// and on resume: `t` is the reference, so the shift is exact.
        ///
        /// The sign matters and was inverted at first. GustPhase() is
        /// Repeat(t + phaseOffset, period) and the invariant is that the
        /// gust's phase EQUALS the music's at the moment of locking, so
        /// `t + phaseOffset == music` and therefore phaseOffset is
        /// `music - t`. The first version wrote `t - music`, which makes
        /// the phase `2t - music` — so instead of re-aligning, every
        /// relock pushed the gust further off the beat as `t` grew. The
        /// offset is negative whenever music < t (the common case), which
        /// is fine — GustPhase wraps with Mathf.Repeat.
        public void RelockToMusic()
        {
            if (AudioManager.Instance == null) return;
            float music = AudioManager.Instance.GetMusicPhase();
            phaseOffset = music - t;
            lockedMusicPhase = music;
        }

        /// The gust's phase, for callers that want to assert the lock
        /// contract without reaching into a live zone's internals.
        public float PhaseAt(float clock)
        {
            return Mathf.Repeat(clock + phaseOffset, period);
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
            // Reads the same phase the Update loop uses, so a body resting
            // inside the volume is pushed only while the blow is genuinely
            // live (OnTriggerStay would otherwise carry a still body
            // forever). The phase is monotonic and pause-safe: it advances
            // on scaled time, so a paused game holds the gust mid-blow and
            // resumes exactly where it stopped.
            if (GustPhase() >= activeTime) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            player.SetGustPush(direction * strength, lift);
        }
    }
}
