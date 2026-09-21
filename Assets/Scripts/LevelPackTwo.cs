using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack two — levels 4-6. Gloomfang answered the first three courses with
    /// his stormcell elite, a garden full of spinning guardians, and finally
    /// the rematch at Skyfall Summit.
    public static class LevelPackTwo
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            StormcellSteps(),
            GloomfangsGarden(),
            SkyfallSummit()
        };

        // ------------------------------------------------------------------
        // Level 4 — "Stormcell Steps": fast horizontal movers, medium gaps
        // and two spinners. Riding platforms across a live storm front.
        // ------------------------------------------------------------------
        static LevelDefinition StormcellSteps()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Stormcell Steps";
            l.Mission = "Gloomfang retaliates. His stormcell elite ride the front line on platforms that never slow down, and the gaps come with no handrails. Time your rides, Pip — the storm is in no rush, and neither are you.";
            l.WinLine = "The stormcell elite have been downsized.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 9f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 1.5f, 35f, 8f, 1f, 8f));   // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 2.5f, 47f, 11f, 1f, 11f)); // spinner arena
            l.Platforms.Add(new PlatformSpec(5f, 3.5f, 58f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 5.5f, 83f, 6f, 1f, 6f));  // checkpoint ledge
            l.Platforms.Add(new PlatformSpec(0f, 6f, 93f, 10f, 1f, 10f));   // spinner arena 2
            l.Platforms.Add(new PlatformSpec(0f, 7.5f, 106f, 12f, 1f, 12f));// summit

            l.Movers.Add(new MoverSpec(0f, 0f, 17f, new Vector3(4.5f, 0f, 0f), 2.8f));
            l.Movers.Add(new MoverSpec(0f, 0.5f, 25f, new Vector3(-4f, 0f, 0f), 3.5f));
            l.Movers.Add(new MoverSpec(0f, 4f, 66f, new Vector3(5f, 0f, 0f), 2.5f));
            l.Movers.Add(new MoverSpec(0f, 4.5f, 74f, new Vector3(-4.5f, 0f, 0f), 3f));

            l.Spinners.Add(new SpinnerSpec(0f, 3f, 47f, 75f));
            l.Spinners.Add(new SpinnerSpec(0f, 6.5f, 93f, 75f));

            l.Checkpoints.Add(new Vector3(0f, 2f, 35f));
            l.Checkpoints.Add(new Vector3(-2f, 6f, 83f));
            l.StoryBeats.Add("The stormcell elite ride lightning for fun. Pip rides orange planks for survival. Everyone has a hobby.");
            l.StoryBeats.Add("Above the second arena, the clouds part for a second — long enough to see the summit waiting. Gloomfang saw Pip looking.");

            l.Gems.Add(new Vector3(0f, 1.6f, 9f));
            l.Gems.Add(new Vector3(0f, 1.7f, 17f));     // over the first rider
            l.Gems.Add(new Vector3(0f, 2.2f, 25f));     // over the second rider
            l.Gems.Add(new Vector3(0f, 3.1f, 35f));
            l.Gems.Add(new Vector3(-4f, 4.1f, 44f));
            l.Gems.Add(new Vector3(4f, 4.1f, 50f));
            l.Gems.Add(new Vector3(5f, 5.1f, 58f));
            l.Gems.Add(new Vector3(0f, 5.7f, 66f));     // fast mover crossing
            l.Gems.Add(new Vector3(0f, 6.2f, 74f));
            l.Gems.Add(new Vector3(-2f, 7.1f, 83f));
            l.Gems.Add(new Vector3(4.5f, 7.6f, 90f));

            l.Portal = new Vector3(0f, 8f, 109f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 5 — "Gloomfang's Garden": a dense spinner gauntlet across
        // narrow 4x4 bridge islands, with elevator rides between tiers.
        // ------------------------------------------------------------------
        static LevelDefinition GloomfangsGarden()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Gloomfang's Garden";
            l.Mission = "Past the gate lies Gloomfang's private garden, where the topiary spins at unsafe speeds and the stepping stones were placed by someone in a hurry to be somewhere else. Tiptoe through, Pip — and whatever you do, do not feed the guardians.";
            l.WinLine = "Three guardians pruned and one garden thoroughly disrespected.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 9f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-2f, 1f, 17f, 4f, 1f, 4f));    // bridge isles
            l.Platforms.Add(new PlatformSpec(2f, 2f, 25f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 34f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 44f, 11f, 1f, 11f));   // spinner arena 1
            l.Platforms.Add(new PlatformSpec(0f, 9f, 64f, 6f, 1f, 6f));     // high ledge
            l.Platforms.Add(new PlatformSpec(4f, 10f, 72f, 6f, 1f, 6f));    // high bridge isles
            l.Platforms.Add(new PlatformSpec(-2f, 11f, 80f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 12f, 90f, 7f, 1f, 7f));    // checkpoint isle 2
            l.Platforms.Add(new PlatformSpec(0f, 13f, 101f, 11f, 1f, 11f)); // spinner arena 2
            l.Platforms.Add(new PlatformSpec(3f, 14f, 112f, 4f, 1f, 4f));   // bridge isle
            l.Platforms.Add(new PlatformSpec(0f, 15f, 122f, 12f, 1f, 12f)); // spinner arena 3
            l.Platforms.Add(new PlatformSpec(0f, 22f, 143f, 12f, 1f, 12f)); // summit

            l.Movers.Add(new MoverSpec(0f, 6f, 55f, new Vector3(0f, 3.5f, 0f), 4f)); // elevator 1
            l.Movers.Add(new MoverSpec(0f, 18f, 134f, new Vector3(0f, 4f, 0f), 5f)); // summit elevator

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 44f, 75f));
            l.Spinners.Add(new SpinnerSpec(0f, 13.5f, 101f, 90f));
            l.Spinners.Add(new SpinnerSpec(0f, 15.5f, 122f, 90f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 34f));
            l.Checkpoints.Add(new Vector3(0f, 12.5f, 90f));
            l.StoryBeats.Add("This garden was Gloomfang's happy place, back when he had one. The topiary misses him.");
            l.StoryBeats.Add("The last guardian pretends not to see Pip. Even topiary knows when a fight is over.");

            l.Gems.Add(new Vector3(0f, 1.6f, 9f));
            l.Gems.Add(new Vector3(-2f, 2.6f, 17f));
            l.Gems.Add(new Vector3(2f, 3.6f, 25f));
            l.Gems.Add(new Vector3(0f, 4.6f, 34f));
            l.Gems.Add(new Vector3(-4f, 5.6f, 41f));
            l.Gems.Add(new Vector3(4f, 5.6f, 47f));
            l.Gems.Add(new Vector3(0f, 11.2f, 55f));    // grab at the first lift's top
            l.Gems.Add(new Vector3(4f, 11.6f, 72f));
            l.Gems.Add(new Vector3(-2f, 12.6f, 80f));
            l.Gems.Add(new Vector3(0f, 13.6f, 90f));
            l.Gems.Add(new Vector3(4f, 14.6f, 104f));
            l.Gems.Add(new Vector3(4.5f, 16.6f, 125f));

            l.Portal = new Vector3(0f, 22.5f, 146f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 6 — "Skyfall Summit": the rematch. A long vertical climb that
        // chains elevators with tight hop chains, gaps up to 5 and a summit
        // in the clouds.
        // ------------------------------------------------------------------
        static LevelDefinition SkyfallSummit()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Skyfall Summit";
            l.Mission = "Skyfall Summit: the last Sunstones, the thinning air, and Gloomfang holding the high ground he insists he won fairly. One final climb, Pip — end the storm where the sky begins.";
            l.WinLine = "Rematch settled — tomorrow's forecast calls for sunshine and zero chance of Gloomfang.";
            l.Milestone = "REGION CHARTED: THE REMATCH. The atlas orders a second page.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 8f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(4f, 1f, 15f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 2f, 22f, 6f, 1f, 6f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 8.5f, 40f, 6f, 1f, 6f));   // ledge, checkpoint
            l.Platforms.Add(new PlatformSpec(4f, 10f, 49f, 4f, 1f, 4f));    // tight hop chain
            l.Platforms.Add(new PlatformSpec(-2f, 11.5f, 57f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 12.5f, 64f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 13.5f, 73f, 7f, 1f, 7f));  // mid checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 20f, 92f, 6f, 1f, 6f));    // ledge 2
            l.Platforms.Add(new PlatformSpec(4f, 21f, 101f, 4f, 1f, 4f));   // tight hop chain 2
            l.Platforms.Add(new PlatformSpec(-2f, 22f, 109f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 23f, 118f, 6f, 1f, 6f));   // pre-summit isle
            l.Platforms.Add(new PlatformSpec(0f, 30.5f, 139f, 12f, 1f, 12f));// summit

            l.Movers.Add(new MoverSpec(0f, 4.5f, 30f, new Vector3(0f, 4f, 0f), 5f));      // lift 1
            l.Movers.Add(new MoverSpec(0f, 16.5f, 82f, new Vector3(0f, 4f, 0f), 5.5f));   // lift 2
            l.Movers.Add(new MoverSpec(0f, 26f, 127f, new Vector3(0f, 4.5f, 0f), 6f));    // summit lift

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 22f));
            l.Checkpoints.Add(new Vector3(0f, 9f, 40f));
            l.Checkpoints.Add(new Vector3(0f, 14f, 73f));
            l.Checkpoints.Add(new Vector3(0f, 23.5f, 118f));
            l.Hearts.Add(new Vector3(2f, 10.1f, 40f));
            l.StoryBeats.Add("The rematch. Gloomfang brought his best clouds. Pip brought snacks.");
            l.StoryBeats.Add("Wind's thinner up here. Or that's Gloomfang, sulking loudly.");
            l.StoryBeats.Add("The clouds below look soft enough to land on. They are not. Pip checked. Twice.");
            l.StoryBeats.Add("Last rest stop before the summit. Gloomfang's grudge is the size of the weather — his hugs, reportedly, are bigger.");

            l.Gems.Add(new Vector3(0f, 1.6f, 8f));
            l.Gems.Add(new Vector3(4f, 2.6f, 15f));
            l.Gems.Add(new Vector3(0f, 10.1f, 40f));
            l.Gems.Add(new Vector3(4f, 11.6f, 49f));
            l.Gems.Add(new Vector3(-2f, 13.1f, 57f));
            l.Gems.Add(new Vector3(3f, 14.1f, 64f));
            l.Gems.Add(new Vector3(0f, 15.1f, 73f));
            l.Gems.Add(new Vector3(0f, 22.2f, 82f));    // grab at lift 2's top
            l.Gems.Add(new Vector3(0f, 21.6f, 92f));
            l.Gems.Add(new Vector3(4f, 22.6f, 101f));
            l.Gems.Add(new Vector3(-2f, 23.6f, 109f));
            l.Gems.Add(new Vector3(0f, 32.2f, 127f));   // grab at the summit lift's top
            l.Gems.Add(new Vector3(-4f, 32.1f, 136f));
            l.Gems.Add(new Vector3(4f, 32.1f, 136f));

            l.Portal = new Vector3(0f, 31f, 142f);
            return l;
        }
    }
}
