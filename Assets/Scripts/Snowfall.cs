using UnityEngine;

namespace GemRush
{
    /// Gentle falling snow for The Long Winter: one looping particle
    /// system draped over the course corridor. A slow, sparse flurry —
    /// phone-friendly particle budget, world-space so it drifts past Pip
    /// as he travels, and it never collides with anything.
    public static class Snowfall
    {
        public static void Create(Transform parent, float courseLength)
        {
            GameObject go = new GameObject("Snowfall");
            go.transform.SetParent(parent, false);

            // Centered over the play corridor, covering the whole course
            // plus a little past the portal.
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            go.transform.localPosition = new Vector3(0f, 10f, courseLength * 0.5f);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new Color(ArtLib.Snow.r, ArtLib.Snow.g,
                ArtLib.Snow.b, 0.75f);
            main.gravityModifier = 0.25f;
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(11f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 1f, courseLength + 24f);
            shape.position = Vector3.zero;

            // Flake drift: a slow sideways sway so it falls like snow,
            // not rain.
            ParticleSystem.VelocityOverLifetimeModule drift =
                ps.velocityOverLifetime;
            drift.enabled = true;
            drift.x = new ParticleSystem.MinMaxCurve(
                0.35f, new AnimationCurve(
                    new Keyframe(0f, -1f), new Keyframe(1f, 1f)));
            drift.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

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
                    new GradientAlphaKey(0.8f, 0.15f),
                    new GradientAlphaKey(0.8f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
            Material flakeMat = renderer.sharedMaterial;
            if (flakeMat != null)
            {
                flakeMat.color = new Color(ArtLib.Snow.r, ArtLib.Snow.g,
                    ArtLib.Snow.b, 0.75f);
            }
        }
    }
}
