using UnityEngine;

namespace GemRush
{
    /// Ambient atmospheric particles: a handful of realm-tinted motes
    /// drifting through the course corridor, giving each realm a sense
    /// of stillness between events. Not weather (rain/snow are their own
    /// systems), not event-driven (bursts answer actions) — this is the
    /// air itself, alive.
    ///
    /// Only the realms that benefit from extra atmosphere get motes:
    /// Garden (pollen), Sunset (gold dust), Undercloud (cold wisps),
    /// Aurora (violet shimmer). Other realms already have weather,
    /// distinctive light, or event density — adding motes there would
    /// be noise for its own sake.
    public static class AmbientMotes
    {
        /// The tint for a realm, or clear if this realm doesn't get motes.
        static Color TintFor(LevelDefinition level)
        {
            if (level.SkyGarden)
                return new Color(ArtLib.GemPink.r, ArtLib.GemPink.g,
                    ArtLib.GemPink.b, 0.4f); // pollen
            if (level.Mood == SoundMood.Sunset)
                return new Color(ArtLib.Gold.r, ArtLib.Gold.g,
                    ArtLib.Gold.b, 0.35f); // gold dust
            if (level.DarkRealm)
                return new Color(ArtLib.Air.r, ArtLib.Air.g,
                    ArtLib.Air.b, 0.25f); // cold wisps
            if (level.AuroraFestival)
                return new Color(ArtLib.AuroraViolet.r, ArtLib.AuroraViolet.g,
                    ArtLib.AuroraViolet.b, 0.3f); // shimmer
            return Color.clear; // this realm doesn't get motes
        }

        /// Creates the motes for a level, if the realm warrants them.
        /// Called from LevelBuilder after the weather systems.
        public static void Create(LevelDefinition level, Transform parent,
            float courseLength)
        {
            Color tint = TintFor(level);
            if (tint.a <= 0f) return; // this realm is already alive

            GameObject go = new GameObject("AmbientMotes");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 5f, courseLength * 0.5f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = tint;
            main.gravityModifier = -0.01f; // motes drift gently upward
            main.maxParticles = 15;        // phone budget: fewer than snow
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(2f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 8f, courseLength + 16f);
            shape.position = Vector3.zero;

            // Gentle sine drift: the motes wander, they don't fall.
            ParticleSystem.VelocityOverLifetimeModule drift =
                ps.velocityOverLifetime;
            drift.enabled = true;
            drift.x = new ParticleSystem.MinMaxCurve(
                0.15f, new AnimationCurve(
                    new Keyframe(0f, -0.5f), new Keyframe(1f, 0.5f)));
            drift.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            drift.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(tint, 0f),
                    new GradientColorKey(tint, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(tint.a, 0.2f),
                    new GradientAlphaKey(tint.a, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer =
                go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                renderer.sharedMaterial = new Material(shader);
                renderer.sharedMaterial.mainTexture = Fx.DiscTexture();
            }
        }
    }
}
