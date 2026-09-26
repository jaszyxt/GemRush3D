using UnityEngine;

namespace GemRush
{
    /// Festival skies for the anniversary week of Pip's first flight: a
    /// sparse, slow confetti drift high over the course — celebration
    /// weather, gentle enough to play under. The Celebration palette on
    /// the Snowfall corridor template; a fraction of the budget.
    public static class ConfettiSky
    {
        // The reward palette (same families Fx.Confetti rains on a win):
        // gold, festival pink, portal cyan, cloud white.
        static readonly Color[] Pastels =
        {
            ArtLib.Gold,
            ArtLib.AuroraPink,
            ArtLib.PortalCyan,
            ArtLib.CloudWhite
        };

        public static void Create(Transform parent, float courseLength)
        {
            GameObject go = new GameObject("ConfettiSky");
            go.transform.SetParent(parent, false);
            go.transform.localPosition =
                new Vector3(0f, 14f, courseLength * 0.5f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            // Two-color gradient: each mote lands somewhere between the
            // festival gold and the cloud white.
            Gradient palette = new Gradient();
            palette.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Pastels[0], 0f),
                    new GradientColorKey(Pastels[1], 0.33f),
                    new GradientColorKey(Pastels[2], 0.66f),
                    new GradientColorKey(Pastels[3], 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            main.startColor = new ParticleSystem.MinMaxGradient(
                palette);
            main.gravityModifier = 0.06f; // a slow, lazy fall
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Each mote picks a pastel: the color-by-speed module spreads
            // the palette across the drift without per-particle scripting.
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(4f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, courseLength + 24f);

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.75f, 0.2f),
                    new GradientAlphaKey(0.75f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.VelocityOverLifetimeModule sway =
                ps.velocityOverLifetime;
            sway.enabled = true;
            sway.x = new ParticleSystem.MinMaxCurve(0.3f);
            sway.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            ParticleSystemRenderer renderer =
                go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.mainTexture = Fx.DiscTexture();
                mat.color = new Color(1f, 1f, 1f, 0.75f);
                renderer.sharedMaterial = mat;
            }
        }
    }
}
