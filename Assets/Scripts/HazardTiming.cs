using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Timing-fairness analysis for the game's lethal hazards.
    ///
    /// The reachability engine measures SPACE — gaps, margins, routes. This
    /// measures TIME, which is what makes a spinner fair or unfair. A
    /// spinner's arm sweeps a disc; the player must cross part of that disc
    /// without being hit. How long the arm is clear at the crossing point,
    /// against how long the crossing takes, is the whole difficulty of the
    /// hazard, and until now nothing measured it — which is why the ramp
    /// below went unnoticed for 40 levels.
    ///
    /// Thresholds are grounded in child-development research rather than
    /// taste. A 7-year-old's simple visual reaction time is ~370-407 ms and
    /// choice reaction time 711-893 ms; the platformer fairness standard
    /// (Celeste's "widen the window in the player's favour") is
    /// response budget = reaction + movement. So the floor is a whole
    /// response budget for a child, not a fraction of a revolution.
    public static class HazardTiming
    {
        // ---- Player constants (mirror PlayerController) ----
        public const float RunSpeed = 8f;
        public const float PipRadius = 0.5f;

        // ---- Spinner geometry (mirror Spinner.Create) ----
        public const float ArmLength = 9f;                  // tip to tip
        public const float ArmDepth = 0.9f;
        public const float SweepRadius = ArmLength * 0.5f;  // 4.5 each side
        public const float ArmHeight = 1.4f;
        public const float ArmThickness = 0.7f;

        /// A 7-year-old needs roughly this long to notice, decide and act.
        /// Under it, a hazard is a reflex test for an adult rather than a
        /// hazard a child can read.
        public const float ChildResponseSeconds = 0.45f;

        /// D-6: fast spinning arms are the one mechanic the user named as
        /// unfun, so speed is capped. Comfort beats spectacle — interesting
        /// and evolving, with little difficulty. 90 is the hard ceiling and
        /// lower is better (60-75 preferred; the sleeping-guardian shape is
        /// the kindest).
        public const float MaxDegreesPerSecond = 90f;

        /// Above this fraction of a platform inside the sweep there is
        /// nowhere to stand and read the hazard — the arm owns the deck.
        public const float MaxExposure = 0.85f;

        /// One hazard's timing profile.
        public struct Hazard
        {
            public string LevelName;
            public Vector3 Top;
            public float DegreesPerSecond;
            public float WakeRadius;        // 0 = always awake
            public float RevolutionSeconds;
            /// How long the arm is at a given point as it sweeps past.
            public float ArmPassSeconds;
            /// Time for the player to run across the swept band.
            public float CrossSeconds;
            /// Arm passes at the crossing point during one traversal.
            public float PassesPerCross;
            /// Clear time at the crossing point: a revolution minus a pass.
            public float SafeWindowSeconds;
            /// Fraction of the standing platform inside the swept disc.
            public float Exposure;
        }

        public static List<Hazard> Survey(LevelDefinition level)
        {
            List<Hazard> result = new List<Hazard>();
            for (int i = 0; i < level.Spinners.Count; i++)
            {
                SpinnerSpec s = level.Spinners[i];
                if (s.DegreesPerSecond <= 0.01f) continue;

                float rev = 360f / s.DegreesPerSecond;

                // The arm crosses a point at tangential speed; measure at
                // the mid-band, where a traversal usually happens. The
                // "hit window" is the arm's own depth plus the player's
                // diameter sweeping past.
                float midRadius = SweepRadius * 0.5f;
                float tangential = Mathf.Max(0.01f,
                    s.DegreesPerSecond * Mathf.Deg2Rad * midRadius);
                float passSeconds = (ArmDepth + PipRadius * 2f) / tangential;

                float crossSeconds =
                    (SweepRadius * 2f + PipRadius * 2f) / RunSpeed;
                float passes = Mathf.Max(1f, crossSeconds / rev);
                float safeWindow = Mathf.Max(0f, rev - passSeconds);

                result.Add(new Hazard
                {
                    LevelName = level.Name,
                    Top = s.PlatformTop,
                    DegreesPerSecond = s.DegreesPerSecond,
                    WakeRadius = s.WakeRadius,
                    RevolutionSeconds = rev,
                    ArmPassSeconds = passSeconds,
                    CrossSeconds = crossSeconds,
                    PassesPerCross = passes,
                    SafeWindowSeconds = safeWindow,
                    Exposure = ExposureOf(level, s)
                });
            }
            return result;
        }

        /// Fraction of the platform the spinner stands on that lies inside
        /// its swept disc. Sampled on a grid: the exact circle/rectangle
        /// intersection is fiddly and this only needs to be indicative.
        static float ExposureOf(LevelDefinition level, SpinnerSpec s)
        {
            float platformArea = 0f;
            float sweptArea = 0f;
            const int Grid = 12;

            for (int p = 0; p < level.Platforms.Count; p++)
            {
                PlatformSpec pl = level.Platforms[p];
                float top = pl.Center.y + pl.Size.y * 0.5f;
                if (Mathf.Abs(top - s.PlatformTop.y) > 0.6f) continue;

                float halfX = pl.Size.x * 0.5f;
                float halfZ = pl.Size.z * 0.5f;
                // Only the platform the spinner actually stands on.
                if (Mathf.Abs(s.PlatformTop.x - pl.Center.x) > halfX + 0.5f ||
                    Mathf.Abs(s.PlatformTop.z - pl.Center.z) > halfZ + 0.5f)
                    continue;

                platformArea += pl.Size.x * pl.Size.z;

                float cellX = pl.Size.x / Grid;
                float cellZ = pl.Size.z / Grid;
                for (int gx = 0; gx < Grid; gx++)
                    for (int gz = 0; gz < Grid; gz++)
                    {
                        float px = pl.Center.x - halfX + (gx + 0.5f) * cellX;
                        float pz = pl.Center.z - halfZ + (gz + 0.5f) * cellZ;
                        float dx = px - s.PlatformTop.x;
                        float dz = pz - s.PlatformTop.z;
                        if (dx * dx + dz * dz <= SweepRadius * SweepRadius)
                            sweptArea += cellX * cellZ;
                    }
            }

            return platformArea > 0.01f ? sweptArea / platformArea : 0f;
        }

        /// Unfair for a young player: either the clear window is shorter
        /// than they need to read and cross, or the arm sweeps so much of
        /// the deck there is nowhere to stand and watch it.
        public static bool IsUnfair(Hazard h)
        {
            return h.SafeWindowSeconds < ChildResponseSeconds
                || h.Exposure > MaxExposure;
        }

        /// Fairness only matters where contact is lethal. Gloomfang's Day
        /// Off is a bonus flight level: Pip flies as the storm, falling
        /// cannot kill, and the guardians are danced past rather than
        /// dodged. Measuring exposure there reports the level's own size
        /// (a 6x6 deck under a 4.5 radius sweep is 97% covered) rather
        /// than any danger, so flight levels are excluded.
        public static bool FairnessApplies(LevelDefinition level)
        {
            return !level.BonusFlight;
        }
    }
}
