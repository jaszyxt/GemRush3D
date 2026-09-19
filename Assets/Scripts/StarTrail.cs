using UnityEngine;

namespace GemRush
{
    /// Star-milestone trail (DESIGN.md cosmetic identity): a soft sparkle
    /// wake behind Pip once you cross 15 total stars, shifting character at
    /// 30 and 45. Gold = reward by palette law; the higher tiers borrow the
    /// festival's pinks and the aurora's greens — the sky realms' colors,
    /// earned. A quiet reward: motes drift where Pip was, nothing chases.
    public static class StarTrail
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

        static readonly Color Tier1Color =
            new Color(1.00f, 0.84f, 0.25f);   // gold — the reward color
        static readonly Color Tier2Color =
            new Color(0.98f, 0.55f, 0.85f);   // festival pink
        static readonly Color Tier3Color =
            new Color(0.35f, 0.95f, 0.65f);   // aurora green

        /// Creates the trail behind the player for the current star count.
        /// No-op below the first milestone. Dies with the world rebuild
        /// (parented under the player), so it never leaks across levels.
        public static void Create(Transform player)
        {
            int stars = SaveSystem.TotalStars(LevelLibrary.Levels.Length);
            int tier = TierFor(stars);
            if (tier == 0) return;

            Color color = tier == 3 ? Tier3Color
                : tier == 2 ? Tier2Color : Tier1Color;

            GameObject go = new GameObject("StarTrail");
            go.transform.SetParent(player, false);
            go.transform.localPosition = new Vector3(0f, -0.55f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.startColor = new Color(color.r, color.g, color.b, 0.85f);
            main.gravityModifier = -0.02f; // motes drift up, like embers
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(9f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
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

            ParticleSystemRenderer renderer =
                go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
        }
    }
}
