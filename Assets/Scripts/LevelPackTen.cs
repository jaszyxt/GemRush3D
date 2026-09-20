using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack ten — levels 26-28, "Mirror Skies". A mirrored sky has appeared
    /// over the Far Isles, and paired mirror doors now hop Pip across
    /// impossible gaps. Walk into one, walk out of its twin — and try not
    /// to think too hard about which side of the glass you're on.
    public static class LevelPackTen
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            MirrorLake(),
            TwinTowers(),
            TheMirrorMeadow()
        };

        // ------------------------------------------------------------------
        // Level 26 — "Mirror Lake": one still lake, one pair of doors. Walk
        // in, walk out the other side of the sky.
        // ------------------------------------------------------------------
        static LevelDefinition MirrorLake()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Mirror Lake";
            l.Mission = "The lake past the wind lanes has gone perfectly still, and in its reflection there are DOORS. Walk into one and you walk out of its twin — somewhere that used to be much too far away. The first Pip through reported it felt 'important.'";
            l.WinLine = "Mirror Lake, charted! Pip waved at his reflection. His reflection waved back half a second late. Nobody mentions it.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 16f, 4f, 1f, 4f));     // door A pedestal
            l.Platforms.Add(new PlatformSpec(0f, 0f, 34f, 6f, 1f, 6f));     // far shore
            l.Platforms.Add(new PlatformSpec(3f, 1f, 42f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 50f, 5f, 1f, 5f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 60f, 7f, 1f, 7f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 66f, 4f, 1f, 4f));     // door A pedestal
            l.Platforms.Add(new PlatformSpec(0f, 3f, 88f, 6f, 1f, 6f));     // far shore 2
            l.Platforms.Add(new PlatformSpec(3f, 4f, 96f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 104f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 6f, 113f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 7f, 124f, 12f, 1f, 12f));  // summit

            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 0.5f, 16f, 0f, 0.5f, 32f));
            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 3.5f, 66f, 0f, 3.5f, 86f));

            l.Checkpoints.Add(new Vector3(-2f, 2.5f, 50f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 113f));

            l.StoryBeats.Add("The doors show a sky that's the same but backwards. Pip has decided not to worry about it.");
            l.StoryBeats.Add("Rule of the mirror: the way in is never the way out. The way out is wherever you were going anyway.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 1.6f, 34f));
            l.Gems.Add(new Vector3(3f, 2.6f, 42f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 50f));
            l.Gems.Add(new Vector3(0f, 4.6f, 60f));
            l.Gems.Add(new Vector3(0f, 4.6f, 88f));
            l.Gems.Add(new Vector3(3f, 5.6f, 96f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 104f));
            l.Gems.Add(new Vector3(0f, 7.6f, 113f));
            l.Gems.Add(new Vector3(-3.5f, 8.6f, 121f));
            l.Gems.Add(new Vector3(3.5f, 8.6f, 127f));
            l.Gems.Add(new Vector3(0f, 1.6f, 16f));     // beside door A — a hello

            l.Portal = new Vector3(0f, 7.5f, 127f);

            // Mirror-silver blue.
            l.SkyColor = new Color(0.65f, 0.82f, 0.95f);
            l.FogColor = new Color(0.60f, 0.78f, 0.92f);
            l.MirrorSkies = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 27 — "Twin Towers": the doors do more than cross gaps — one
        // exits high up, turning a door into a staircase made of light.
        // A sleeping guardian naps beside the first bell... wrong game. The
        // first door.
        // ------------------------------------------------------------------
        static LevelDefinition TwinTowers()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Twin Towers";
            l.Mission = "Two towers, perfectly alike, each holding half of a door. The guardian between them has been guarding the same nine square meters for a century — the doors have been open the whole time. Being a guardian is not a thinking job.";
            l.WinLine = "Twin Towers, charted! The guardian is still guarding. Out of respect, Pip left one gem where it can watch it.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-2f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 2f, 27f, 6f, 1f, 6f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 37f, 9f, 1f, 9f));     // guardian arena
            l.Platforms.Add(new PlatformSpec(0f, 3f, 46f, 7f, 1f, 7f));     // door pedestal
            l.Platforms.Add(new PlatformSpec(0f, 8f, 60f, 6f, 1f, 6f));     // high exit ledge
            l.Platforms.Add(new PlatformSpec(-2f, 9f, 68f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(3f, 10f, 76f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 11f, 85f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 11f, 94f, 7f, 1f, 7f));    // door pedestal 2
            l.Platforms.Add(new PlatformSpec(0f, 12f, 112f, 7f, 1f, 7f));   // door exit isle
            l.Platforms.Add(new PlatformSpec(3f, 13f, 121f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 14f, 129f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 15f, 139f, 12f, 1f, 12f)); // summit

            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 3.5f, 46f, 0f, 8.5f, 60f));
            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 11.5f, 94f, 0f, 12.5f, 112f));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 37f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 27f));
            l.Checkpoints.Add(new Vector3(0f, 11.5f, 85f));
            l.Hearts.Add(new Vector3(2f, 11.6f, 85f));

            l.StoryBeats.Add("The door exits five floors up. Mirror physics: what goes in at walking height comes out where it was always going.");
            l.StoryBeats.Add("Gloomfang's reflection floats on the wrong side of the sky, copying your moves perfectly. Slightly better than you, if you're honest.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(-2f, 2.6f, 18f));
            l.Gems.Add(new Vector3(0f, 3.6f, 27f));
            l.Gems.Add(new Vector3(-3.5f, 4.6f, 34f));
            l.Gems.Add(new Vector3(3.5f, 4.6f, 40f));
            l.Gems.Add(new Vector3(0f, 4.8f, 46f));     // at door A
            l.Gems.Add(new Vector3(0f, 9.6f, 60f));     // at the high exit
            l.Gems.Add(new Vector3(-2f, 10.6f, 68f));
            l.Gems.Add(new Vector3(3f, 11.6f, 76f));
            l.Gems.Add(new Vector3(0f, 12.6f, 85f));
            l.Gems.Add(new Vector3(0f, 12.8f, 94f));    // at door A two
            l.Gems.Add(new Vector3(0f, 13.6f, 112f));
            l.Gems.Add(new Vector3(3f, 14.6f, 121f));
            l.Gems.Add(new Vector3(-2f, 15.6f, 129f));

            l.Portal = new Vector3(0f, 15.5f, 142f);

            // Twin-tower silver.
            l.SkyColor = new Color(0.70f, 0.80f, 0.92f);
            l.FogColor = new Color(0.66f, 0.76f, 0.88f);
            l.MirrorSkies = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 28 — "The Mirror Meadow": the finale. Three door pairs, a
        // sleeping guardian in the meadow, and the mirrored sky itself
        // watching. What walks in is never quite what walks out.
        // ------------------------------------------------------------------
        static LevelDefinition TheMirrorMeadow()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Mirror Meadow";
            l.Mission = "The last chart is the strangest: a meadow where the sky lies down on the ground and the doors stand in pairs like strung pearls. One guardian sleeps in the clover. Somewhere on the wrong side of the glass, a storm floats along with you, learning to be a reflection. Both of you are almost ready.";
            l.WinLine = "The Mirror Meadow, charted! On the mirrored side, a reflection-Gloomfang saluted. Pip saluted back. The chart now reads: 'population: one more, sort of.'";
            l.Milestone = "REGION CHARTED: MIRROR SKIES. The atlas has a reflection now.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 17f, 4f, 1f, 4f));     // door A pedestal
            l.Platforms.Add(new PlatformSpec(0f, 1f, 32f, 6f, 1f, 6f));     // door exit isle
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 41f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(3f, 3f, 50f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 4f, 59f, 7f, 1f, 7f));     // checkpoint isle
            // 6 wide, not 4: this pedestal is the only platform in the
            // game reached by a jump with NO alternative route (the door
            // beside it leads onward, not up), and at 4 wide the hop left
            // 11% margin — the tightest forced jump in 40 levels. The
            // other door pedestals stay 4 wide: those are optional
            // approaches with a route around them.
            l.Platforms.Add(new PlatformSpec(0f, 4f, 67f, 6f, 1f, 6f));     // door A pedestal
            l.Platforms.Add(new PlatformSpec(0f, 8f, 82f, 6f, 1f, 6f));     // high exit isle
            l.Platforms.Add(new PlatformSpec(-2f, 9f, 90f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(3f, 10f, 98f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 11f, 107f, 7f, 1f, 7f));   // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 11f, 115f, 5f, 1f, 5f));   // door A pedestal
            l.Platforms.Add(new PlatformSpec(0f, 14f, 132f, 8f, 1f, 8f));   // door exit isle
            l.Platforms.Add(new PlatformSpec(0f, 15f, 142f, 9f, 1f, 9f));   // meadow arena
            l.Platforms.Add(new PlatformSpec(0f, 16f, 152f, 12f, 1f, 12f)); // meadow summit

            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 0.5f, 17f, 0f, 1.5f, 30f));
            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 4.5f, 67f, 0f, 8.5f, 80f));
            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 11.5f, 115f, 0f, 14.5f, 130f));

            l.Spinners.Add(new SpinnerSpec(0f, 15.5f, 142f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 4.5f, 59f));
            l.Checkpoints.Add(new Vector3(0f, 11.5f, 107f));
            l.Hearts.Add(new Vector3(2f, 11.6f, 107f));

            l.StoryBeats.Add("In the mirror, the meadow is the sky and the sky is the meadow. The flowers don't mind. Flowers are flexible.");
            l.StoryBeats.Add("Your reflection-Gloomfang hums the lullaby before you do. The mirror is half a second ahead now. Neither of you mentions it.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 1.8f, 17f));     // at door one
            l.Gems.Add(new Vector3(0f, 2.6f, 32f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 41f));
            l.Gems.Add(new Vector3(3f, 4.6f, 50f));
            l.Gems.Add(new Vector3(0f, 5.6f, 59f));
            l.Gems.Add(new Vector3(0f, 5.8f, 67f));     // at door two
            l.Gems.Add(new Vector3(0f, 9.6f, 82f));
            l.Gems.Add(new Vector3(-2f, 10.6f, 90f));
            l.Gems.Add(new Vector3(3f, 11.6f, 98f));
            l.Gems.Add(new Vector3(0f, 12.6f, 107f));
            l.Gems.Add(new Vector3(0f, 12.8f, 115f));   // at door three
            l.Gems.Add(new Vector3(0f, 15.6f, 132f));
            l.Gems.Add(new Vector3(-3.5f, 16.6f, 139f));
            l.Gems.Add(new Vector3(3.5f, 16.6f, 145f));

            l.Portal = new Vector3(0f, 16.5f, 155f);

            // Mirror-meadow silver-green.
            l.SkyColor = new Color(0.68f, 0.85f, 0.88f);
            l.FogColor = new Color(0.63f, 0.80f, 0.84f);
            l.MirrorSkies = true;
            return l;
        }
    }
}
