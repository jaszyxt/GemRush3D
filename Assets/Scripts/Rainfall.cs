using UnityEngine;

namespace GemRush
{
    /// Rainfall for the living calendar's Tuesdays: a soft world-space
    /// drizzle draped over the course corridor, the Snowfall template
    /// retuned — faster fall, stretched motes, no sideways sway. Same
    /// phone-friendly particle budget as the winter snow.
    public static class Rainfall
    {
        public static void Create(Transform parent, float courseLength)
        {
            GameObject go = new GameObject("Rainfall");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 11f, courseLength * 0.5f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 1.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 8.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            main.startColor = new Color(ArtLib.Rain.r, ArtLib.Rain.g,
                ArtLib.Rain.b, 0.55f);
            main.gravityModifier = 0.1f;
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(24f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 1f, courseLength + 24f);
            shape.position = Vector3.zero;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(ArtLib.Rain, 0f),
                    new GradientColorKey(ArtLib.Rain, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.55f, 0.15f),
                    new GradientAlphaKey(0.55f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            // Stretched billboards: the fall speed draws each mote into a
            // rain streak — rain reads by motion, not by sprite.
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.6f;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.sharedMaterial = new Material(shader);
        }
    }
}
