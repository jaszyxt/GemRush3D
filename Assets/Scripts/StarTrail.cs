using UnityEngine;

namespace GemRush
{
    /// Star-milestone trail (DESIGN.md cosmetic identity): a soft sparkle
    /// wake behind Pip once you cross 15 total stars, shifting character at
    /// 30 and 45. Gold = reward by palette law; the higher tiers borrow the
    /// festival's pinks and the aurora's greens — the sky realms' colours,
    /// earned. A quiet reward: motes drift where Pip was, nothing chases.
    ///
    /// The tier is read from the SAVE, which is per device — the same build
    /// can legitimately show gold on one machine and green on another, which
    /// read as a bug until each tier learned to announce itself (see
    /// AnnounceTier). The trail also re-tints live: crossing a milestone
    /// mid-session must not leave the old colour behind until the next
    /// world rebuild.
    public class StarTrail : MonoBehaviour
    {
        /// Star thresholds for the three trail tiers.
        public const int Tier1Stars = 15;
        public const int Tier2Stars = 30;
        public const int Tier3Stars = 45;

        /// The trail tier for a star count: 0 = none, 1..3 = tiers.
        public static int TierFor(int totalStars)
        {
            if (totalStars >= Tier3Stars) return 3;
            if (totalStars >= Tier2Stars) return 2;
            if (totalStars >= Tier1Stars) return 1;
            return 0;
        }

        public static Color ColorForTier(int tier)
        {
            if (tier >= 3) return ArtLib.AuroraGreen;
            if (tier == 2) return ArtLib.AuroraPink;
            return ArtLib.Gold;
        }

        ParticleSystem ps;
        ParticleSystem.MainModule main;
        ParticleSystem.ColorOverLifetimeModule fade;
        int tier;
        float checkTimer;
        Transform owner;

        /// Creates the trail behind the player for the current star count.
        /// No-op below the first milestone. Dies with the world rebuild
        /// (parented under the player), so it never leaks across levels.
        public static void Create(Transform player)
        {
            int totalStars = SaveSystem.TotalStars(LevelLibrary.Levels.Length);
            int tier = TierFor(totalStars);
            if (tier == 0) return;

            GameObject go = new GameObject("StarTrail");
            go.transform.SetParent(player, false);
            go.transform.localPosition = new Vector3(0f, -0.55f, 0f);

            StarTrail trail = go.AddComponent<StarTrail>();
            trail.owner = player;
            trail.Build(tier);
            AnnounceTier(tier);
        }

        /// Says the tier out loud the first time this device reaches it.
        /// Static because the announcement outlives any one trail instance
        /// (a level rebuild recreates the trail but must not re-announce).
        public static void AnnounceTier(int tier)
        {
            if (tier <= SaveSystem.TrailTierAnnounced) return;
            SaveSystem.TrailTierAnnounced = tier;
            AudioManager.Instance.PlayTrailTier();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowStoryToast(Strings.TrailTierLine(tier));
        }

        void Build(int initialTier)
        {
            ps = gameObject.AddComponent<ParticleSystem>();

            main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.gravityModifier = -0.02f; // motes drift up, like embers
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(9f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            fade = ps.colorOverLifetime;
            fade.enabled = true;

            ParticleSystemRenderer renderer =
                gameObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                renderer.sharedMaterial = new Material(shader);
                renderer.sharedMaterial.mainTexture = Fx.DiscTexture();
            }

            ApplyTier(initialTier);
        }

        /// Writes the tier's colour into the live system: the start colour
        /// for new motes and the lifetime gradient for the ones already
        /// drifting. Rewriting both is what makes the change read in place
        /// instead of waiting for the old motes to die.
        void ApplyTier(int newTier)
        {
            tier = newTier;
            Color color = ColorForTier(tier);
            // Boost past 1.0 so the brightest motes exceed the bloom
            // threshold (0.95) and the trail glows rather than just tints.
            // Alpha is preserved so the fade still works; the bloom
            // appears on fresh motes and fades naturally as they die.
            main.startColor = new Color(
                color.r * 1.3f, color.g * 1.3f, color.b * 1.3f, 0.85f);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        void Update()
        {
            // TotalStars walks every level, so it is far too heavy for a
            // per-frame call; a star can only change at a finish (which
            // rebuilds the world anyway) — but a milestone crossing must
            // not wait for that. A slow poll costs nothing and closes the
            // window where the wake keeps a colour the player has outgrown.
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 1f;

            int want = TierFor(SaveSystem.TotalStars(LevelLibrary.Levels.Length));
            if (want > tier)
            {
                ApplyTier(want);
                AnnounceTier(want);
            }
            // The trail sits under the player, but the player can be
            // destroyed with its world while this polls; retire quietly.
            if (owner == null) Destroy(gameObject);
        }
    }
}
