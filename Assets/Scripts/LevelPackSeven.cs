using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack seven — levels 17-19, "The Sky Garden". Gloomfang's Tuesday
    /// rain woke a garden that shouldn't exist, and its guardians sleep
    /// inside the flower beds: they only spin while you linger nearby, so
    /// keep moving and they keep dreaming. Gems here hum a melody.
    public static class LevelPackSeven
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            FirstBlooms(),
            PetalDrift(),
            TheBloomingGate()
        };

        // ------------------------------------------------------------------
        // Level 17 — "First Blooms": meet the sleeping guardians. Keep
        // moving and they never even open an eye.
        // ------------------------------------------------------------------
        static LevelDefinition FirstBlooms()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "First Blooms";
            l.Mission = "Gloomfang's rain woke a garden in the sky, and the garden came with guardians — big, red, and fast asleep. They wake when you get close and dawdle. So don't dawdle. And listen: the Sunstones here hum, and they hum in tune.";
            l.WinLine = "First blooms! The guardians went back to sleep the moment you left. One of them is snoring.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 19f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 28f, 6f, 1f, 6f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 38f, 9f, 1f, 9f));     // garden arena
            l.Platforms.Add(new PlatformSpec(4f, 4f, 48f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 57f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 6f, 66f, 8f, 1f, 8f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 7f, 77f, 9f, 1f, 9f));     // garden arena
            l.Platforms.Add(new PlatformSpec(3f, 8f, 88f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 9f, 97f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 10f, 107f, 12f, 1f, 12f)); // summit terrace

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 38f, 60f, 7f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 7.5f, 77f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 28f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 66f));

            l.StoryBeats.Add("Guardian lullaby, translated from the hum: 'spin when they're near, dream when they're gone.'");
            l.StoryBeats.Add("The garden grew overnight. Nobody planted it. Gloomfang is whistling like he had nothing to do with it.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 28f));
            l.Gems.Add(new Vector3(-3.5f, 4.6f, 35f));
            l.Gems.Add(new Vector3(3.5f, 4.6f, 41f));
            l.Gems.Add(new Vector3(4f, 5.6f, 48f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 57f));
            l.Gems.Add(new Vector3(0f, 7.6f, 66f));
            l.Gems.Add(new Vector3(-3.5f, 8.6f, 74f));
            l.Gems.Add(new Vector3(3.5f, 8.6f, 80f));
            l.Gems.Add(new Vector3(3f, 9.6f, 88f));
            l.Gems.Add(new Vector3(-2f, 10.6f, 97f));

            l.Portal = new Vector3(0f, 10.5f, 110f);

            // Fresh spring green.
            l.SkyColor = new Color(0.60f, 0.88f, 0.70f);
            l.FogColor = new Color(0.56f, 0.83f, 0.66f);
            l.SkyGarden = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 18 — "Petal Drift": sleeping guardians + an elevator ride,
        // petals everywhere, and a spare life tucked by the second rest.
        // ------------------------------------------------------------------
        static LevelDefinition PetalDrift()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Petal Drift";
            l.Mission = "The higher beds bloom louder. Petals ride the updrafts all the way to the top of the garden, and the guardians up here dream a little livelier. Same rule, though: keep moving, and the garden keeps napping.";
            l.WinLine = "Petal Drift, charted! A petal landed on Pip's head on the way out. The garden says thank you.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-3f, 1f, 19f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(3f, 2f, 27f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 36f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 46f, 10f, 1f, 10f));   // garden arena
            l.Platforms.Add(new PlatformSpec(0f, 9f, 64f, 6f, 1f, 6f));     // high ledge
            l.Platforms.Add(new PlatformSpec(4f, 10f, 72f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 11f, 80f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 12f, 89f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 13f, 100f, 10f, 1f, 10f)); // garden arena
            l.Platforms.Add(new PlatformSpec(3f, 14f, 111f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 15f, 119f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 16f, 129f, 12f, 1f, 12f)); // summit

            l.Movers.Add(new MoverSpec(0f, 5.5f, 57f, new Vector3(0f, 3f, 0f), 4.5f));

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 46f, 75f, 8f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 13.5f, 100f, 75f, 8f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 36f));
            l.Checkpoints.Add(new Vector3(0f, 12.5f, 89f));
            l.Hearts.Add(new Vector3(2f, 12.6f, 89f));

            l.StoryBeats.Add("Petals ride the wind because they trust it. So far the wind has a perfect record.");
            l.StoryBeats.Add("The second bed's guardian dreams out loud. It mumbles in its sleep. It's humming YOUR theme.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(-3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(3f, 3.6f, 27f));
            l.Gems.Add(new Vector3(0f, 4.6f, 36f));
            l.Gems.Add(new Vector3(-4f, 5.6f, 43f));
            l.Gems.Add(new Vector3(4f, 5.6f, 49f));
            l.Gems.Add(new Vector3(0f, 8.6f, 57f));     // ride the lift's top
            l.Gems.Add(new Vector3(0f, 10.1f, 64f));
            l.Gems.Add(new Vector3(4f, 11.1f, 72f));
            l.Gems.Add(new Vector3(-2f, 12.1f, 80f));
            l.Gems.Add(new Vector3(0f, 13.1f, 89f));
            l.Gems.Add(new Vector3(-4f, 14.6f, 97f));
            l.Gems.Add(new Vector3(4f, 14.6f, 103f));

            l.Portal = new Vector3(0f, 16.5f, 132f);

            l.SkyColor = new Color(0.55f, 0.85f, 0.72f);
            l.FogColor = new Color(0.51f, 0.80f, 0.68f);
            l.SkyGarden = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 19 — "The Blooming Gate": the garden's front door. Three
        // guardians, one light sleeper, and the bloom of all blooms.
        // ------------------------------------------------------------------
        static LevelDefinition TheBloomingGate()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Blooming Gate";
            l.Mission = "Every garden has a front gate, and this one has kept the sky realm's oldest promise: whoever tends it may pass. Three guardians. One of them is a light sleeper — the garden apologizes in advance. Ring the course with your steps and the gate will open in flowers.";
            l.WinLine = "The Blooming Gate opened in a wave of color you chose, gem by gem. The guardians clapped. Slowly. They're still waking up.";
            l.Milestone = "REGION CHARTED: THE SKY GARDEN. The page bloomed while it was drawn.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 27f, 6f, 1f, 6f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 37f, 10f, 1f, 10f));   // garden arena A
            l.Platforms.Add(new PlatformSpec(3f, 4f, 47f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 55f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 6f, 63f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 7f, 73f, 10f, 1f, 10f));   // garden arena B
            l.Platforms.Add(new PlatformSpec(4f, 8f, 84f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 9f, 92f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 10f, 101f, 7f, 1f, 7f));   // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 11f, 112f, 10f, 1f, 10f)); // garden arena C
            l.Platforms.Add(new PlatformSpec(3f, 12.5f, 123f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 13.5f, 131f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 16f, 139f, 13f, 1f, 13f)); // The Blooming Gate

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 37f, 75f, 8f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 7.5f, 73f, 75f, 8f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 11.5f, 112f, 90f, 8f, 15f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 27f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 63f));
            l.Checkpoints.Add(new Vector3(0f, 10.5f, 101f));

            l.StoryBeats.Add("The light sleeper's name is Red Nine. He dreams of being a carousel. Be kind.");
            l.StoryBeats.Add("Past the last bed, the petals go quiet — they only get this quiet right before something wonderful.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 27f));
            l.Gems.Add(new Vector3(-4f, 4.6f, 34f));
            l.Gems.Add(new Vector3(4f, 4.6f, 40f));
            l.Gems.Add(new Vector3(3f, 5.6f, 47f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 55f));
            l.Gems.Add(new Vector3(0f, 7.6f, 63f));
            l.Gems.Add(new Vector3(-4f, 8.6f, 70f));
            l.Gems.Add(new Vector3(4f, 8.6f, 76f));
            l.Gems.Add(new Vector3(0f, 10.6f, 101f));
            l.Gems.Add(new Vector3(-4f, 12.6f, 109f));
            l.Gems.Add(new Vector3(4f, 12.6f, 115f));
            l.Gems.Add(new Vector3(3f, 14.1f, 123f));

            l.Portal = new Vector3(0f, 16.5f, 142f);

            l.SkyColor = new Color(0.65f, 0.88f, 0.78f);
            l.FogColor = new Color(0.61f, 0.84f, 0.74f);
            l.SkyGarden = true;
            return l;
        }
    }
}
