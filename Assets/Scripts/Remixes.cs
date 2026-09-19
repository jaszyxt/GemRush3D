using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Turns a shipped level into new content: same geometry, new mood,
    /// new twist. Medals and par times recompute for free, the audit
    /// rules apply unchanged, and players who memorized the original get
    /// to meet it again wearing a different sky.
    ///
    /// Mutations should append (gems, hearts, gusts) and restyle (colors,
    /// moods, text) — never shrink or move the original geometry, so the
    /// original's playtest knowledge stays true.
    public static class Remixes
    {
        public static LevelDefinition Remixed(LevelDefinition baseLevel,
            Action<LevelDefinition> mutate)
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = baseLevel.Name;
            l.Mission = baseLevel.Mission;
            l.WinLine = baseLevel.WinLine;
            l.Spawn = baseLevel.Spawn;
            l.KillY = baseLevel.KillY;
            l.Portal = baseLevel.Portal;
            l.SkyColor = baseLevel.SkyColor;
            l.FogColor = baseLevel.FogColor;
            l.DarkRealm = baseLevel.DarkRealm;
            l.SkyGarden = baseLevel.SkyGarden;
            l.MirrorSkies = baseLevel.MirrorSkies;
            l.LongWinter = baseLevel.LongWinter;
            l.AuroraFestival = baseLevel.AuroraFestival;
            l.Mood = baseLevel.Mood;
            l.BonusFlight = false;

            // Spec objects are shared by reference: mutations must never
            // mutate spec instances, only append to the lists or change the
            // level's own scalars.
            l.Platforms = new List<PlatformSpec>(baseLevel.Platforms);
            l.Movers = new List<MoverSpec>(baseLevel.Movers);
            l.Spinners = new List<SpinnerSpec>(baseLevel.Spinners);
            l.Gems = new List<Vector3>(baseLevel.Gems);
            l.Checkpoints = new List<Vector3>(baseLevel.Checkpoints);
            l.BouncePads = new List<Vector3>(baseLevel.BouncePads);
            l.Hearts = new List<Vector3>(baseLevel.Hearts);
            l.WindZones = new List<WindSpec>(baseLevel.WindZones);
            l.Gusts = new List<GustSpec>(baseLevel.Gusts);
            l.Bells = new List<BellSpec>(baseLevel.Bells);
            l.EchoBridges = new List<EchoBridgeSpec>(baseLevel.EchoBridges);
            l.StoryBeats = new List<string>(baseLevel.StoryBeats);

            mutate?.Invoke(l);
            return l;
        }

        /// Convenience: one more gem hanging in the sky above a point.
        public static void AddGem(LevelDefinition l, Vector3 position)
        {
            l.Gems.Add(position);
        }

        /// Convenience: one more heart pickup at a platform-top position.
        public static void AddHeart(LevelDefinition l, Vector3 position)
        {
            l.Hearts.Add(position);
        }
    }
}
