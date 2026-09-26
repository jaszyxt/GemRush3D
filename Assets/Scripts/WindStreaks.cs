using UnityEngine;

namespace GemRush
{
    /// Speed streaks while the wind carries Pip: a small looping particle
    /// rig parented to the player, its cone aimed against the flow so the
    /// streaks whip past and stretch into lines. Air-pale (the wind family,
    /// never hazard red) and self-cleaning — the rig stops and destroys
    /// itself when the ride ends, and a ride whose heartbeat stops without
    /// a clean End (paused world, torn-down zone) still lapses on the
    /// unscaled clock.
    public class WindStreaks : MonoBehaviour
    {
        // Phone budget: rate x lifetime keeps ~14 alive, far under the ~30
        // the wind-ride spec allows.
        const float RatePerSecond = 40f;
        const float MaxLifetime = 0.35f;
        const float RefreshTtl = 0.25f;  // unscaled; matches the haptic heartbeat
        const float FadeOutTime = 0.6f;  // lets live streaks finish after End

        ParticleSystem ps;
        float lastRefresh;
        float destroyAt = -1f;

        public static WindStreaks Attach(Transform player)
        {
            GameObject go = new GameObject("WindStreaks");
            go.transform.SetParent(player, false);
            WindStreaks streaks = go.AddComponent<WindStreaks>();

            streaks.ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = streaks.ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, MaxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = ArtLib.Air;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = streaks.ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(RatePerSecond);

            // A narrow cone trailing the flow: particles stream backwards
            // past Pip, stretched into lines by velocityScale below.
            ParticleSystem.ShapeModule shape = streaks.ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.45f;

            ParticleSystem.ColorOverLifetimeModule fade = streaks.ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(ArtLib.Air, 0f),
                    new GradientColorKey(ArtLib.Air, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.15f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                renderer.sharedMaterial = new Material(shader);
                renderer.sharedMaterial.mainTexture = Fx.DiscTexture();
            }
            renderer.velocityScale = 1.4f; // stretch the motes into streak lines

            streaks.ps.Play();
            return streaks;
        }

        /// Re-asserted every physics frame of the ride; keeps the cone
        /// aimed against the flow.
        public void Refresh(Vector3 flow)
        {
            lastRefresh = Time.unscaledTime;
            destroyAt = -1f;
            if (flow.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(-flow.normalized);
        }

        /// The ride ended: stop emitting, let the live streaks fade, clean up.
        public void End()
        {
            if (destroyAt > 0f) return;
            if (ps != null) ps.Stop();
            destroyAt = Time.unscaledTime + FadeOutTime;
        }

        void Update()
        {
            if (destroyAt > 0f)
            {
                if (Time.unscaledTime >= destroyAt) Destroy(gameObject);
                return;
            }
            // Missed End (pause, world torn down mid-ride): the heartbeat
            // stopped, so clean up on our own unscaled clock.
            if (Time.unscaledTime - lastRefresh > RefreshTtl) End();
        }
    }
}
