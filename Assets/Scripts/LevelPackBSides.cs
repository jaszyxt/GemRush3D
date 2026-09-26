using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// The B-Sides: remixed shipped levels — same bones, different sky.
    /// Each one carries a small twist of meaning rather than a difficulty
    /// bump, per the design bible. They live at the end of the level grid
    /// and unlock through the normal ladder.
    public static class LevelPackBSides
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            Remixes.Remixed(LevelPackEight.Levels[0], l =>
            {
                l.Name = "Gust Alley — Nightfall";
                l.WinLine = "Gust Alley after dark, charted! The night lanes blow quieter, and Nim's snores keep the same steady tempo. Somebody has to check on him. It was always going to be Pip.";
                l.DarkRealm = true;
                l.SkyColor = new Color(0.10f, 0.14f, 0.30f);
                l.FogColor = new Color(0.09f, 0.12f, 0.26f);
                l.Hearts.Add(new Vector3(0f, 1.1f, 46f));
                l.Platforms.Add(new PlatformSpec(0f, 0f, 47f, 4f, 1f, 4f)); // night safety isle
                // A checkpoint on the new isle, so the added line below
                // actually reaches the player: beats pair to checkpoints by
                // index, and the parent course only has two. Without this
                // the night line was written and then silently dropped —
                // caught by NoStoryBeat_IsSilentlyDropped.
                l.Checkpoints.Add(new Vector3(0f, 0.5f, 47f));
                l.StoryBeats.Add("Night wind is just day wind wearing a darker coat. Nim sleeps in this lane — his giggles became snores.");
                l.Gems.Add(new Vector3(0f, 2.1f, 55f));
                l.Checkpoints.Add(new Vector3(-2f, 7.5f, 100f));
                l.StoryBeats.Add("The night wind carries every sound a little further. Pip's footsteps echo off the dark.");
            }),
            Remixes.Remixed(LevelPackNine.Levels[0], l =>
            {
                l.Name = "The Garden That Dreams";
                l.WinLine = "The Garden That Dreams, charted! It kept the tune while it slept, and Pip kept to the beat. Nobody woke up. That was the whole trick.";
                l.SkyColor = new Color(0.42f, 0.60f, 0.72f);
                l.FogColor = new Color(0.38f, 0.56f, 0.66f);
                l.Gusts.Add(new GustSpec(0f, 3.5f, 44f, new Vector3(5f, 4f, 10f),
                    new Vector3(0f, 0f, 1f)));
                l.Gusts.Add(new GustSpec(0f, 3.5f, 71f, new Vector3(5f, 4f, 10f),
                    new Vector3(0f, 0f, 1f)));
                l.Hearts.Add(new Vector3(2f, 6.6f, 66f));
                l.Gems.Add(new Vector3(0f, 5.1f, 44f));    // ride the new lane
                l.Gems.Add(new Vector3(0f, 8.1f, 72f));    // ride the new lane
                // A third checkpoint on the bell pedestal the added lane
                // rides past, so the line below is actually shown (beats
                // pair to checkpoints by index; the parent has only two).
                l.Checkpoints.Add(new Vector3(0f, 3.5f, 64f));
                l.StoryBeats.Add("The wind learned the garden's melody. Now the beds sway in time, and the guardians dream on the beat.");
                l.Checkpoints.Add(new Vector3(0f, 6.5f, 105f));
                l.StoryBeats.Add("Even the sleeping guardians are swaying now. The garden keeps dreaming, and the music keeps playing.");
            }),
            Remixes.Remixed(LevelLibrary.TheAscent(), l =>
            {
                l.Name = "The Ascent — Nightfall";
                l.WinLine = "The beacon route, climbed in the dark the way the first keepers did. From up here every portal Pip ever lit is still burning — and now the sky has one more, lit after hours.";
                l.DarkRealm = true;
                l.SkyColor = new Color(0.14f, 0.16f, 0.34f);
                l.FogColor = new Color(0.12f, 0.14f, 0.30f);
                l.Hearts.Add(new Vector3(-2f, 13.5f, 52f));
                // The two added beats need two homes: the parent course has
                // three checkpoints and three beats, so anything written
                // after them was dropped. One sits on the high arena the
                // climb crosses, one on the summit ledge.
                l.Checkpoints.Add(new Vector3(0f, 16f, 80f));
                l.Checkpoints.Add(new Vector3(0f, 22f, 103f));
                l.StoryBeats.Add("Climb the beacon route in the dark, the way the first keepers did, before anyone thought to make it easy.");
                l.StoryBeats.Add("From the summit at night you can see every portal you ever lit. All of them. Still burning.");
                l.Milestone = "REGION CHARTED: THE B-SIDES. The atlas keeps a shelf for after dark.";
            })
        };
    }
}
