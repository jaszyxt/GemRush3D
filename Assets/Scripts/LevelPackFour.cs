using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack four — levels 10-12, "The Two Suns". The story is over: the sky
    /// realm glows from below, Gloomfang carries rain on Tuesdays, and these
    /// three courses are the celebration lap. Golden light, playful combos,
    /// and a final walk home.
    public static class LevelPackFour
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            TwinlightTerrace(),
            RaindanceRevels(),
            PipsHomecoming()
        };

        // ------------------------------------------------------------------
        // Level 10 — "Twinlight Terrace": a victory lap at golden hour. Wide
        // hops, one tame guardian, one ferry ride into the sunset.
        // ------------------------------------------------------------------
        static LevelDefinition TwinlightTerrace()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Twinlight Terrace";
            l.Mission = "With the Undercloud lit from below, the sky realm has two suns now — the old one, and the one Pip made. The terraces glow gold all day. No storm. No pressure. Just the best views in the realm and a guardian who half-heartedly spins.";
            l.WinLine = "Golden hour, golden gems. Gloomfang watched the whole run and clapped — very quietly, from very far away.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 9f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 17f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 26f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 35f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 45f, 11f, 1f, 11f));   // spinner terrace
            l.Platforms.Add(new PlatformSpec(3f, 5f, 56f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 6f, 64f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 7f, 73f, 8f, 1f, 8f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 8f, 83f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(2f, 9f, 92f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-3f, 10f, 101f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 11f, 111f, 12f, 1f, 12f)); // summit terrace

            l.Movers.Add(new MoverSpec(0f, 4.2f, 51f, new Vector3(4f, 0f, 0f), 3.5f));

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 45f, 60f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 35f));
            l.Checkpoints.Add(new Vector3(0f, 7.5f, 73f));

            l.StoryBeats.Add("Two suns mean two shadows. Pip has decided the second one is Gloomfang, walking along.");
            l.StoryBeats.Add("The tame guardian spins at exactly the speed of a comfortable afternoon.");

            l.BouncePads.Add(new Vector3(3f, 0.5f, 3f));
            l.BouncePads.Add(new Vector3(4.5f, 4.5f, 49f));

            l.Gems.Add(new Vector3(0f, 1.6f, 9f));
            l.Gems.Add(new Vector3(3f, 2.6f, 17f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 26f));
            l.Gems.Add(new Vector3(0f, 4.6f, 35f));
            l.Gems.Add(new Vector3(-4.5f, 5.6f, 42f));
            l.Gems.Add(new Vector3(4.5f, 5.6f, 48f));
            l.Gems.Add(new Vector3(0f, 5.8f, 51f));     // over the ferry
            l.Gems.Add(new Vector3(3f, 6.6f, 56f));
            l.Gems.Add(new Vector3(-2f, 7.6f, 64f));
            l.Gems.Add(new Vector3(0f, 8.6f, 73f));
            l.Gems.Add(new Vector3(2f, 10.6f, 92f));
            l.Gems.Add(new Vector3(0f, 12.6f, 108f));
            l.Gems.Add(new Vector3(3f, 8.6f, 3f));      // apex of the start pad
            l.Gems.Add(new Vector3(4.5f, 12f, 49f));    // apex of the terrace pad

            l.Portal = new Vector3(0f, 11.5f, 114f);

            // Golden hour.
            l.SkyColor = new Color(0.99f, 0.78f, 0.52f);
            l.FogColor = new Color(0.95f, 0.70f, 0.45f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 11 — "Raindance Revels": Gloomfang's first official Tuesday
        // rain, celebrated with playful mover-and-spinner combos.
        // ------------------------------------------------------------------
        static LevelDefinition RaindanceRevels()
        {
            LevelDefinition l = new LevelDefinition();
            l.Mission = "It's Tuesday — Gloomfang's first official rain delivery. The islands are slick with celebration, the guardians are dancing, and the elevators run on schedule. Splashing is encouraged. Falling is traditional.";
            l.WinLine = "Pip danced through the first rain the realm has ever liked. Gloomfang took a bow and made it rain confetti. Briefly. Accidentally.";
            l.Name = "Raindance Revels";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-3f, 1f, 19f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(3f, 2f, 27f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 36f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 47f, 11f, 1f, 11f));   // dance floor A
            l.Platforms.Add(new PlatformSpec(0f, 9f, 62f, 6f, 1f, 6f));     // high ledge
            l.Platforms.Add(new PlatformSpec(3f, 10f, 68f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(4f, 11f, 74f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 12f, 82f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 13f, 91f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 14f, 102f, 11f, 1f, 11f)); // dance floor B
            l.Platforms.Add(new PlatformSpec(0f, 18f, 122f, 12f, 1f, 12f)); // finale stage

            l.Movers.Add(new MoverSpec(0f, 6f, 56f, new Vector3(0f, 3f, 0f), 4.5f));    // elevator
            l.Movers.Add(new MoverSpec(0f, 14.5f, 111f, new Vector3(4f, 0f, 0f), 3.2f));// confetti ferry

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 47f, 75f));
            l.Spinners.Add(new SpinnerSpec(0f, 14.5f, 102f, 90f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 36f));
            l.Checkpoints.Add(new Vector3(0f, 13.5f, 91f));

            l.StoryBeats.Add("The guardians learned to dance from watching Pip fall with style. They think that's dancing. Nobody corrected them.");
            l.StoryBeats.Add("Rain, it turns out, sounds like applause when it lands on Sunstones.");

            l.BouncePads.Add(new Vector3(3f, 0.5f, 3f));

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(-3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(3f, 3.6f, 27f));
            l.Gems.Add(new Vector3(0f, 4.6f, 36f));
            l.Gems.Add(new Vector3(-4f, 5.6f, 44f));
            l.Gems.Add(new Vector3(4f, 5.6f, 50f));
            l.Gems.Add(new Vector3(0f, 10.2f, 56f));    // grab at the elevator's top
            l.Gems.Add(new Vector3(0f, 10.1f, 62f));
            l.Gems.Add(new Vector3(3f, 11.6f, 68f));
            l.Gems.Add(new Vector3(-2f, 13.6f, 82f));
            l.Gems.Add(new Vector3(0f, 14.6f, 91f));
            l.Gems.Add(new Vector3(4.5f, 15.6f, 99f));
            l.Gems.Add(new Vector3(-4.5f, 15.6f, 105f));
            l.Gems.Add(new Vector3(0f, 16.1f, 111f));   // over the confetti ferry
            l.Gems.Add(new Vector3(3f, 8.6f, 3f));      // apex of the start pad

            l.Portal = new Vector3(0f, 18.5f, 125f);

            // Soft rain-washed blue.
            l.SkyColor = new Color(0.58f, 0.78f, 0.88f);
            l.FogColor = new Color(0.54f, 0.72f, 0.82f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 12 — "Pip's Homecoming": a mirror of the very first course,
        // walked the other way through time. Ends at the little island where
        // everything started.
        // ------------------------------------------------------------------
        static LevelDefinition PipsHomecoming()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Pip's Homecoming";
            l.Mission = "One more walk, and it's the oldest one there is: the way home. The first course looks smaller now — same islands, same gentle guardian, same movers that once felt impossible. The shelf by the window still has room for one more gem.";
            l.WinLine = "Home. The shelf gets the last gem — and the window keeps the best view in the realm. Thanks for playing.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // home start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 26f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 44f, 9f, 1f, 9f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 56f, 12f, 1f, 12f));   // old spinner arena
            l.Platforms.Add(new PlatformSpec(5f, 5f, 68f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-1f, 6f, 76f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 10.5f, 94f, 11f, 1f, 11f));// home summit

            l.Movers.Add(new MoverSpec(0f, 2f, 34f, new Vector3(5f, 0f, 0f), 4f));
            l.Movers.Add(new MoverSpec(-5f, 7f, 84f, new Vector3(0f, 4f, 0f), 5f));

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 56f, 60f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 44f));
            l.Checkpoints.Add(new Vector3(-1f, 6.5f, 76f));

            l.StoryBeats.Add("Everything looks smaller than Pip remembers — except the sky, which got bigger.");
            l.StoryBeats.Add("The old spinner still guards this arena. These days it mostly guards the view.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 26f));
            l.Gems.Add(new Vector3(0f, 3.7f, 34f));     // over the mover's path, like the first time
            l.Gems.Add(new Vector3(0f, 4.7f, 42f));
            l.Gems.Add(new Vector3(-3f, 4.7f, 46f));
            l.Gems.Add(new Vector3(4.5f, 5.3f, 60.5f));
            l.Gems.Add(new Vector3(-4.5f, 5.3f, 51.5f));
            l.Gems.Add(new Vector3(5f, 6.7f, 68f));
            l.Gems.Add(new Vector3(-1f, 7.6f, 76f));
            l.Gems.Add(new Vector3(4.5f, 12.3f, 90f));

            l.Portal = new Vector3(0f, 11f, 97f);

            // Clear morning light.
            l.SkyColor = new Color(0.52f, 0.76f, 0.99f);
            l.FogColor = new Color(0.50f, 0.72f, 0.94f);
            return l;
        }
    }
}
