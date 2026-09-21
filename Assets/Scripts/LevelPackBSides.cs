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
                l.DarkRealm = true;
                l.SkyColor = new Color(0.10f, 0.14f, 0.30f);
                l.FogColor = new Color(0.09f, 0.12f, 0.26f);
                l.Hearts.Add(new Vector3(0f, 1.1f, 46f));
                l.Platforms.Add(new PlatformSpec(0f, 0f, 47f, 4f, 1f, 4f)); // night safety isle
                l.StoryBeats.Add("Night wind is just day wind wearing a darker coat. Nim sleeps in this lane — his giggles became snores.");
                l.Gems.Add(new Vector3(0f, 2.1f, 55f));
            }),
            Remixes.Remixed(LevelPackNine.Levels[0], l =>
            {
                l.Name = "The Garden That Dreams";
                l.SkyColor = new Color(0.42f, 0.60f, 0.72f);
                l.FogColor = new Color(0.38f, 0.56f, 0.66f);
                l.Gusts.Add(new GustSpec(0f, 3.5f, 44f, new Vector3(5f, 4f, 10f),
                    new Vector3(0f, 0f, 1f), 4.4f, 2.2f, 8f));
                l.Gusts.Add(new GustSpec(0f, 3.5f, 71f, new Vector3(5f, 4f, 10f),
                    new Vector3(0f, 0f, 1f), 4.4f, 2.2f, 8f));
                l.Hearts.Add(new Vector3(2f, 6.6f, 66f));
                l.Gems.Add(new Vector3(0f, 5.1f, 44f));    // ride the new lane
                l.Gems.Add(new Vector3(0f, 8.1f, 72f));    // ride the new lane
                l.StoryBeats.Add("The wind learned the garden's melody. Now the beds sway in time, and the guardians dream on the beat.");
            }),
            Remixes.Remixed(LevelLibrary.TheAscent(), l =>
            {
                l.Name = "The Ascent — Nightfall";
                l.DarkRealm = true;
                l.SkyColor = new Color(0.14f, 0.16f, 0.34f);
                l.FogColor = new Color(0.12f, 0.14f, 0.30f);
                l.Hearts.Add(new Vector3(-2f, 13.5f, 52f));
                l.StoryBeats.Add("Climb the beacon route in the dark, the way the first keepers did, before anyone thought to make it easy.");
                l.StoryBeats.Add("From the summit at night you can see every portal you ever lit. All of them. Still burning.");
                l.Milestone = "REGION CHARTED: THE B-SIDES. The atlas keeps a shelf for after dark.";
            })
        };
    }
}
