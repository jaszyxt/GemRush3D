using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack five — levels 13-15, "The Far Isles". Past the edge of the old
    /// map, the Sky-Keeper asks Pip to chart lands no one has ever seen.
    /// Gloomfang comes along as "weather support". The wind here is strong
    /// enough to stand in: updraft columns do the climbing, so the pack
    /// stays gentle while going higher than ever.
    public static class LevelPackFive
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            TailwindPoint(),
            MonsoonMosaic(),
            TheLastBlankSpace()
        };

        // ------------------------------------------------------------------
        // Level 13 — "Tailwind Point": first contact with the standing winds.
        // One updraft does all the climbing; the rest is a breeze. Literally.
        // ------------------------------------------------------------------
        static LevelDefinition TailwindPoint()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Tailwind Point";
            l.Mission = "The Sky-Keeper unrolls a brand-new map — mostly blank — and taps the empty corner: the Far Isles. First stop, Tailwind Point, where the wind stands still and the islands float on it. Gloomfang insists on coming as 'weather support.' He IS the weather.";
            l.WinLine = "Tailwind Point, charted! The wind says hello. Loudly. Gloomfang translated: it's been waiting for company.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 19f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 28f, 6f, 1f, 6f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 2f, 35f, 4f, 1f, 4f));     // updraft base
            l.Platforms.Add(new PlatformSpec(0f, 8.5f, 43f, 6f, 1f, 6f));   // high ledge
            l.Platforms.Add(new PlatformSpec(4f, 9.5f, 52f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 10.5f, 60f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 11.5f, 69f, 7f, 1f, 7f));  // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 12f, 77f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 12.5f, 83f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 13.5f, 92f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 14.5f, 101f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 15.5f, 111f, 12f, 1f, 12f));// summit

            l.Movers.Add(new MoverSpec(0f, 11.7f, 73f, new Vector3(3f, 0f, 0f), 3.5f));

            l.WindZones.Add(new WindSpec(0f, 2.5f, 35f, new Vector3(3f, 6f, 3f), 11f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 28f));
            l.Checkpoints.Add(new Vector3(0f, 12f, 69f));

            l.StoryBeats.Add("The wind here doesn't push. It holds. Gloomfang says the Far Isles are 'the sky holding its breath.'");
            l.StoryBeats.Add("Pip floats. Pip actually FLOATS. This is the best job ever.");

            l.BouncePads.Add(new Vector3(3f, 0.5f, 3f));

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 28f));
            l.Gems.Add(new Vector3(0f, 5f, 35f));       // inside the updraft
            l.Gems.Add(new Vector3(0f, 7.5f, 35f));     // near its top
            l.Gems.Add(new Vector3(0f, 10.1f, 43f));
            l.Gems.Add(new Vector3(4f, 11.1f, 52f));
            l.Gems.Add(new Vector3(-2f, 12.1f, 60f));
            l.Gems.Add(new Vector3(0f, 13.1f, 69f));
            l.Gems.Add(new Vector3(0f, 13.6f, 77f));
            l.Gems.Add(new Vector3(3f, 15.1f, 92f));
            l.Gems.Add(new Vector3(-2f, 16.1f, 101f));
            l.Gems.Add(new Vector3(3f, 6f, 3f));        // apex of the start pad

            l.Portal = new Vector3(0f, 16f, 114f);

            // Bright charting-day teal.
            l.SkyColor = new Color(0.55f, 0.85f, 0.85f);
            l.FogColor = new Color(0.52f, 0.80f, 0.80f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 14 — "Monsoon Mosaic": Gloomfang's test-flight as weather
        // support. His rain and the updrafts take turns carrying Pip.
        // ------------------------------------------------------------------
        static LevelDefinition MonsoonMosaic()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Monsoon Mosaic";
            l.Mission = "Weather support, day one: Gloomfang practices his Tuesday rain over the Mosaic Isles, and everything blooms mid-jump. The guardians have learned to dance in it. The updrafts smell like wet stone and are twice as strong.";
            l.WinLine = "Monsoon Mosaic, charted! Gloomfang's rain passed inspection. He has been insufferably proud ever since.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-3f, 1f, 19f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(3f, 2f, 27f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 36f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 47f, 11f, 1f, 11f));   // spinner arena A
            l.Platforms.Add(new PlatformSpec(0f, 4f, 58f, 4f, 1f, 4f));     // updraft base
            l.Platforms.Add(new PlatformSpec(0f, 10f, 66f, 6f, 1f, 6f));    // high ledge
            l.Platforms.Add(new PlatformSpec(3f, 11f, 74f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 12f, 82f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 13f, 91f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 14f, 102f, 11f, 1f, 11f)); // spinner arena B
            l.Platforms.Add(new PlatformSpec(0f, 17f, 122f, 12f, 1f, 12f)); // mosaic summit

            l.Movers.Add(new MoverSpec(0f, 14.5f, 113f, new Vector3(4f, 0f, 0f), 3.2f));

            l.WindZones.Add(new WindSpec(0f, 4.5f, 58f, new Vector3(3f, 5f, 3f), 12f));

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 47f, 75f));
            l.Spinners.Add(new SpinnerSpec(0f, 14.5f, 102f, 90f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 36f));
            l.Checkpoints.Add(new Vector3(0f, 13.5f, 91f));
            l.Hearts.Add(new Vector3(2f, 13.6f, 91f));

            l.StoryBeats.Add("Rain + updraft = one very confused, very beautiful waterfall going the wrong way.");
            l.StoryBeats.Add("The dancing guardians have started charging admission. One gem per show. They accept compliments too.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(-3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(3f, 3.6f, 27f));
            l.Gems.Add(new Vector3(0f, 4.6f, 36f));
            l.Gems.Add(new Vector3(-4f, 5.6f, 44f));
            l.Gems.Add(new Vector3(4f, 5.6f, 50f));
            l.Gems.Add(new Vector3(0f, 7.5f, 58f));     // inside the updraft
            l.Gems.Add(new Vector3(0f, 11.1f, 66f));
            l.Gems.Add(new Vector3(3f, 12.1f, 74f));
            l.Gems.Add(new Vector3(-2f, 13.1f, 82f));
            l.Gems.Add(new Vector3(0f, 14.1f, 91f));
            l.Gems.Add(new Vector3(-4.5f, 15.6f, 99f));
            l.Gems.Add(new Vector3(4.5f, 15.6f, 105f));
            l.Gems.Add(new Vector3(0f, 15.1f, 113f));   // over the ferry

            l.Portal = new Vector3(0f, 17.5f, 125f);

            // Rain-washed teal.
            l.SkyColor = new Color(0.50f, 0.72f, 0.80f);
            l.FogColor = new Color(0.47f, 0.68f, 0.76f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 15 — "The Last Blank Space": the farthest point on the map.
        // Three updrafts, two guardians, and the corner where the atlas ends.
        // ------------------------------------------------------------------
        static LevelDefinition TheLastBlankSpace()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Last Blank Space";
            l.Mission = "One blank corner remains on the Sky-Keeper's map, and the wind blows hardest exactly there. Three standing winds, two guardians who wandered out this far to be alone, and the spot where the atlas ends. Draw the last line, Pip.";
            l.WinLine = "The map is full. The Sky-Keeper looks at it for a long time, then writes one word in the corner: 'more.'";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 19f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 28f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 37f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 48f, 10f, 1f, 10f));   // guardian arena A
            l.Platforms.Add(new PlatformSpec(0f, 4f, 59f, 4f, 1f, 4f));     // updraft base
            l.Platforms.Add(new PlatformSpec(0f, 11f, 68f, 6f, 1f, 6f));    // high ledge
            l.Platforms.Add(new PlatformSpec(4f, 12f, 76f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 13f, 84f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 14f, 93f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 15f, 104f, 10f, 1f, 10f)); // guardian arena B
            l.Platforms.Add(new PlatformSpec(0f, 15f, 115f, 4f, 1f, 4f));   // updraft base 2
            l.Platforms.Add(new PlatformSpec(0f, 22f, 124f, 6f, 1f, 6f));   // high ledge 2
            l.Platforms.Add(new PlatformSpec(4f, 23f, 132f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 24f, 140f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 25f, 149f, 13f, 1f, 13f)); // The Blank Spot

            l.WindZones.Add(new WindSpec(0f, 4.5f, 59f, new Vector3(3f, 6f, 3f), 12f));
            l.WindZones.Add(new WindSpec(0f, 15.5f, 115f, new Vector3(3f, 6f, 3f), 13f));

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 48f, 90f));
            l.Spinners.Add(new SpinnerSpec(0f, 15.5f, 104f, 105f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 37f));
            l.Checkpoints.Add(new Vector3(0f, 14.5f, 93f));
            l.Hearts.Add(new Vector3(2f, 14.6f, 93f));

            l.StoryBeats.Add("The guardians out here didn't flee the storm. They came to listen to it think.");
            l.StoryBeats.Add("At the very edge of the map, the wind spells something. Pip is 90% sure it says 'welcome.'");

            l.BouncePads.Add(new Vector3(0f, 25.5f, 152f));

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 28f));
            l.Gems.Add(new Vector3(0f, 4.6f, 37f));
            l.Gems.Add(new Vector3(-4f, 5.6f, 45f));
            l.Gems.Add(new Vector3(4f, 5.6f, 51f));
            l.Gems.Add(new Vector3(0f, 8f, 59f));       // inside updraft 1
            l.Gems.Add(new Vector3(0f, 12.1f, 68f));
            l.Gems.Add(new Vector3(4f, 13.1f, 76f));
            l.Gems.Add(new Vector3(-2f, 14.1f, 84f));
            l.Gems.Add(new Vector3(0f, 15.1f, 93f));
            l.Gems.Add(new Vector3(4.5f, 16.6f, 107f));
            l.Gems.Add(new Vector3(0f, 19f, 115f));     // inside updraft 2
            l.Gems.Add(new Vector3(0f, 23.1f, 124f));
            l.Gems.Add(new Vector3(0f, 31f, 152f));     // apex of the Blank Spot pad

            l.Portal = new Vector3(0f, 25.5f, 154f);

            // Pale, uncharted light.
            l.SkyColor = new Color(0.78f, 0.88f, 0.96f);
            l.FogColor = new Color(0.74f, 0.84f, 0.92f);
            return l;
        }
    }
}
