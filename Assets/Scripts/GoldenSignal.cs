using UnityEngine;

namespace GemRush
{
    /// The golden gem's *signal* — the taught cue that turns a hidden
    /// collectible into a discovery instead of a secret nobody finds.
    ///
    /// The research law behind it (docs/Fun-Creativity-Directives.md):
    /// secrets need a signal taught early, delivered by diegetic cues
    /// rather than UI markers, and they should reward curiosity without
    /// ever punishing a miss. So a few slow gold motes drift from the
    /// nearest gem of the ordinary trail toward the hiding spot — follow
    /// the gold and it leads somewhere — the gem itself wears a faint
    /// glimmer bright enough to spot from the route, and once found a
    /// faded outline stays behind: you were here, and that is all it says.
    ///
    /// Everything here is cosmetic: no colliders, no gameplay state, and
    /// nothing is added to the gem/star math.
    public static class GoldenSignal
    {
        /// Where along the nearest-gem→golden line the motes pick up: the
        /// far half, which is the stretch a player would otherwise have
        /// to guess at. Nearer the gem, the trail itself is already the
        /// clue, so nothing is added.
        const float TrailFrom = 0.55f;

        /// The trail runs from the ordinary gem nearest the golden toward
        /// the hiding spot — the last stretch a player would otherwise
        /// have to guess. Motes spawn on that line, so the eye is being
        /// led somewhere rather than told.
        public static void Trail(LevelDefinition level, Vector3 spot,
            Transform parent)
        {
            if (level.Gems == null || level.Gems.Count == 0) return;

            Vector3 nearest = level.Gems[0];
            float best = float.MaxValue;
            for (int i = 0; i < level.Gems.Count; i++)
            {
                float d = Vector3.Distance(level.Gems[i], spot);
                if (d < best)
                {
                    best = d;
                    nearest = level.Gems[i];
                }
            }

            // Only the far end of that line — the part that is genuinely
            // a hint. Near the gem the trail is already the clue.
            Vector3 from = Vector3.Lerp(nearest, spot, TrailFrom);
            Vector3 to = spot;
            Vector3 line = to - from;

            GameObject go = new GameObject("GoldenTrail");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = from;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
            main.startColor = ArtLib.Gold;
            main.gravityModifier = -0.02f; // a hair of rise: drift, not fall
            main.maxParticles = 24; // a hint, not a beacon
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(2.2f);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            // A thin ribbon along the hint line, so motes rise from the
            // whole stretch rather than one point. The rotation is set on
            // the transform below (not the module) so the box's own
            // velocity contribution stays in one mode.
            shape.scale = new Vector3(0.6f, 0.4f, line.magnitude);
            go.transform.localRotation =
                line.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(line.normalized)
                    : Quaternion.identity;

            // Fade in and out over each mote's life: nothing pops.
            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(ArtLib.Gold, 0f),
                    new GradientColorKey(ArtLib.Gold, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.75f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer =
                go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = TrailMaterial();

            ps.Play();
        }

        /// A faint breathing glimmer the gem wears so a curious player can
        /// spot it from the route. Slow enough to read as "something is
        /// over there", not as a beacon demanding attention — and on the
        /// same gentle pulse family as every other glow in the game.
        public static void Glimmer(Transform gem, Material body)
        {
            if (gem == null || body == null) return;
            gem.gameObject.AddComponent<GoldenGlimmer>().body = body;
        }

        // The found spot used to hold a "trace" here (FoundOutline): a
        // faint, full-size gold cube at gem height, meant as Odyssey-style
        // softness for a level whose golden was already found. It was a
        // non-gem wearing a gem's shape, and that is what players reported
        // for three releases — "i can see the yellow gem, but it is not
        // solid, pip can pass thru it like a cloud, nothing happened, no
        // sound no anything, just a shape" — on every level whose find
        // flag was set (32 of 37 on one device, 11-13 on another). Deleted
        // on the user's call, 2026-09-22: "if it's not a gem, then DELETE
        // it / if it's a gem, then make it a gem". The rule that replaces
        // it holds game-wide now: a gem spot holds a REAL collectible gem
        // or nothing at all, and a find is remembered in the atlas, not by
        // a ghost standing in the world.

        // One cached white material for the trail, colour carried in the
        // particle startColor/gradient — the same no-orphan-material rule
        // Fx uses for its shared burst material.
        static Shader trailShader;
        static Material trailMaterial;

        static Material TrailMaterial()
        {
            if (trailShader == null)
                trailShader = Shader.Find(
                    "Universal Render Pipeline/Particles/Unlit");
            if (trailMaterial == null && trailShader != null)
            {
                trailMaterial = new Material(trailShader);
                // Round motes, not squares (same fix as Fx.SharedBurstMaterial).
                trailMaterial.mainTexture = Fx.DiscTexture();
            }
            return trailMaterial;
        }
    }

    /// Slow breathing emission on the golden's body material — the
    /// "something glints over there" tell. Unscaled, so a photogenic
    /// pause never freezes the cue mid-breath.
    [UnityEngine.Scripting.Preserve]
    class GoldenGlimmer : MonoBehaviour
    {
        public Material body;

        void Update()
        {
            if (body == null) return;
            // The game's 0.48 Hz glow family (see Lantern), a shade
            // deeper so a hidden gem stays softer than a lit shrine.
            float pulse = 1.25f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
            body.SetColor("_EmissionColor", ArtLib.Gold * pulse);
        }
    }
}
