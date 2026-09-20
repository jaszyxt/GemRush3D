using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack three — levels 7-9, the Undercloud arc. After losing the rematch
    /// at Skyfall Summit, Gloomfang fled downwards with the Master Sunstone.
    /// Pip follows — and discovers the storm was never hoarding light out of
    /// greed, but out of fear of the dark below. The arc ends in friendship,
    /// as it should.
    public static class LevelPackThree
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            TheLongFall(),
            TheUndercloud(),
            HeartOfTheStorm()
        };

        // ------------------------------------------------------------------
        // Level 7 — "The Long Fall": the realm's first descent. The way down
        // is open air; the danger is falling PAST the ledges into the dark.
        // ------------------------------------------------------------------
        static LevelDefinition TheLongFall()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Long Fall";
            l.Mission = "Gloomfang took the rematch badly — he dove straight through the cloud floor with the Master Sunstone, heading down where no Sky-Keeper has ever gone. Down is the only way left, Pip. Try to fall with style.";
            l.WinLine = "The bottom of the sky has a floor after all. It looks... nervous to see you.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 10f, 1f, 10f));     // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 9f, 7f, 1f, 7f));
            l.Platforms.Add(new PlatformSpec(3f, -3f, 17f, 5f, 1f, 5f));    // first drop
            l.Platforms.Add(new PlatformSpec(-2f, -5f, 26f, 5f, 1f, 5f));
            // 10 wide, not 7: this is the descent's landing pad, and the
            // straight drop from the opening deck leaves only a 6% margin
            // — tighter than anything else in the teaching regions. The
            // level's theme is falling, so the drop stays long; the
            // landing just gets wide enough to forgive an over-eager run.
            l.Platforms.Add(new PlatformSpec(0f, -7f, 35f, 10f, 1f, 10f));  // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, -12f, 44f, 5f, 1f, 5f));   // the big drop
            l.Platforms.Add(new PlatformSpec(4f, -14f, 53f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-1f, -16f, 62f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, -18f, 71f, 8f, 1f, 8f));   // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, -21f, 90f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, -23f, 99f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-3f, -25f, 107f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, -27f, 117f, 10f, 1f, 10f));// spinner shelf
            l.Platforms.Add(new PlatformSpec(0f, -29f, 128f, 12f, 1f, 12f));// cloud floor

            l.Movers.Add(new MoverSpec(0f, -20f, 80f, new Vector3(5f, 0f, 0f), 3.2f));

            l.Spinners.Add(new SpinnerSpec(0f, -26.5f, 117f, 75f));

            l.Checkpoints.Add(new Vector3(0f, -6.5f, 35f));
            l.Checkpoints.Add(new Vector3(0f, -17.5f, 71f));
            l.Checkpoints.Add(new Vector3(0f, -28.5f, 125f));
            l.Hearts.Add(new Vector3(2.5f, -16.4f, 71f));

            l.StoryBeats.Add("Down here the wind changes — it doesn't howl anymore. It almost sounds like breathing.");
            l.StoryBeats.Add("You are now below every map the Sky-Keeper ever drew. Even the clouds look nervous.");
            l.StoryBeats.Add("Something enormous is glowing far below. It is NOT the Master Sunstone. It's much bigger.");

            l.Gems.Add(new Vector3(0f, 1.6f, 9f));
            l.Gems.Add(new Vector3(3f, -1.4f, 17f));
            l.Gems.Add(new Vector3(-2f, -3.4f, 26f));
            l.Gems.Add(new Vector3(0f, -5.4f, 35f));
            l.Gems.Add(new Vector3(-2.5f, -10.4f, 41f));    // beside the big drop
            // Both of these sat 8.1 units UNDER their platform's deck (the
            // pack's other gems sit +1.1 above it) — inside the solid slab
            // with no approach from any side: impossible to collect, and a
            // silent cap at 2 stars. Restored to the house grab height.
            l.Gems.Add(new Vector3(0f, -12.4f, 53f));
            l.Gems.Add(new Vector3(4f, -14.4f, 62f));
            l.Gems.Add(new Vector3(0f, -18.9f, 71f));
            l.Gems.Add(new Vector3(0f, -17.9f, 80f));       // over the crossing
            l.Gems.Add(new Vector3(0f, -19.4f, 90f));
            l.Gems.Add(new Vector3(3f, -21.4f, 99f));
            l.Gems.Add(new Vector3(-3f, -23.4f, 108f));

            l.Portal = new Vector3(0f, -28.5f, 131f);
            l.KillY = -36f;

            // Pre-storm dusk: the daylight dims as you go under.
            l.SkyColor = new Color(0.36f, 0.50f, 0.80f);
            l.FogColor = new Color(0.33f, 0.45f, 0.72f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 8 — "The Undercloud": the dark beneath the sky. Narrow paths,
        // fast guardians, and the first evidence that Gloomfang has been
        // carrying light down here for a very long time.
        // ------------------------------------------------------------------
        static LevelDefinition TheUndercloud()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Undercloud";
            l.Mission = "Welcome to the Undercloud — the dark Gloomfang never talks about. The glowing stones in the walls? He carried every one of them down himself. Hoarding the light, the guardians say. But hoarding it from WHAT?";
            l.WinLine = "The guardians step aside at last. They only ever wanted the dark kept out too.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 6f, 1f, 6f));     // bridge isles
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 26f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 36f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 47f, 11f, 1f, 11f));   // spinner arena A
            l.Platforms.Add(new PlatformSpec(0f, 9f, 66f, 6f, 1f, 6f));     // high ledge
            l.Platforms.Add(new PlatformSpec(4f, 10f, 74f, 6f, 1f, 6f));    // bridge isles
            l.Platforms.Add(new PlatformSpec(-2f, 11f, 82f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 12f, 92f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 13f, 103f, 11f, 1f, 11f)); // spinner arena B
            l.Platforms.Add(new PlatformSpec(3f, 15f, 122f, 6f, 1f, 6f));   // bridge isle
            l.Platforms.Add(new PlatformSpec(0f, 16f, 130f, 6f, 1f, 6f));   // last step
            l.Platforms.Add(new PlatformSpec(0f, 17f, 138f, 12f, 1f, 12f)); // summit

            l.Movers.Add(new MoverSpec(0f, 6.5f, 57f, new Vector3(0f, 3f, 0f), 4.5f));  // lift
            l.Movers.Add(new MoverSpec(0f, 14f, 115f, new Vector3(5f, 0f, 0f), 2.8f));  // fast ferry

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 47f, 115f));
            l.Spinners.Add(new SpinnerSpec(0f, 13.5f, 103f, 130f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 36f));
            l.Checkpoints.Add(new Vector3(0f, 12.5f, 92f));

            l.StoryBeats.Add("The stones in the walls glow faintly. Somebody carried light down here — one armful at a time — for a very long time.");
            l.StoryBeats.Add("The guardians aren't attacking. They're herding you away from something deeper in. Something that hums.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 26f));
            l.Gems.Add(new Vector3(0f, 4.6f, 36f));
            l.Gems.Add(new Vector3(-4f, 5.6f, 44f));
            l.Gems.Add(new Vector3(4f, 5.6f, 50f));
            l.Gems.Add(new Vector3(0f, 10.6f, 57f));        // grab at the lift's top
            l.Gems.Add(new Vector3(0f, 10.1f, 66f));
            l.Gems.Add(new Vector3(4f, 11.6f, 74f));
            l.Gems.Add(new Vector3(-2f, 12.6f, 82f));
            l.Gems.Add(new Vector3(0f, 13.6f, 92f));
            l.Gems.Add(new Vector3(4.5f, 15.6f, 107f));
            l.Gems.Add(new Vector3(-4.5f, 15.6f, 99f));
            l.Gems.Add(new Vector3(3f, 16.6f, 122f));

            l.Portal = new Vector3(0f, 17.5f, 141f);

            // The Undercloud: indigo dusk, heavy fog, no sun to speak of.
            l.DarkRealm = true;
            l.SkyColor = new Color(0.16f, 0.18f, 0.30f);
            l.FogColor = new Color(0.11f, 0.12f, 0.21f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 9 — "Heart of the Storm": the finale. A long climb down to
        // the bottom of everything, where Gloomfang waits with the Master
        // Sunstone — and finally runs out of excuses.
        // ------------------------------------------------------------------
        static LevelDefinition HeartOfTheStorm()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Heart of the Storm";
            l.Mission = "At the bottom of everything beats the Heart of the Storm — and in front of it stands Gloomfang, out of storm and nearly out of excuses. 'Go back,' he thunders. It comes out as a whisper. One last climb, Pip. Not to win. To listen.";
            l.WinLine = "Pip takes his hand. The sky realm lights from below for the first time in a thousand years.";
            l.Milestone = "REGION CHARTED: THE UNDERCLOUD. The atlas grows a bottom.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 5f, 20f, 6f, 1f, 6f));     // checkpoint ledge
            l.Platforms.Add(new PlatformSpec(4f, 6f, 28f, 4f, 1f, 4f));     // hop chain
            l.Platforms.Add(new PlatformSpec(-2f, 7f, 36f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 8f, 46f, 10f, 1f, 10f));   // guardian arena
            l.Platforms.Add(new PlatformSpec(0f, 16f, 63f, 6f, 1f, 6f));    // checkpoint ledge
            l.Platforms.Add(new PlatformSpec(3f, 17f, 72f, 6f, 1f, 6f));    // hop chain
            l.Platforms.Add(new PlatformSpec(-2f, 18f, 80f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 19f, 88f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 20f, 97f, 7f, 1f, 7f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 21.5f, 108f, 10f, 1f, 10f));// guardian arena
            l.Platforms.Add(new PlatformSpec(0f, 29f, 134f, 14f, 1f, 14f)); // the Heart

            l.Movers.Add(new MoverSpec(0f, 1.5f, 10f, new Vector3(0f, 4f, 0f), 5f));     // lift 1
            l.Movers.Add(new MoverSpec(0f, 11.5f, 56f, new Vector3(0f, 4.5f, 0f), 5.5f));// lift 2
            l.Movers.Add(new MoverSpec(0f, 25f, 120f, new Vector3(0f, 3.5f, 0f), 6f));   // last lift

            l.Spinners.Add(new SpinnerSpec(0f, 8.5f, 46f, 115f));
            l.Spinners.Add(new SpinnerSpec(0f, 22f, 108f, 125f));

            l.Checkpoints.Add(new Vector3(0f, 5.5f, 20f));
            l.Checkpoints.Add(new Vector3(0f, 16.5f, 63f));
            l.Checkpoints.Add(new Vector3(0f, 20.5f, 97f));
            l.Hearts.Add(new Vector3(2f, 17.1f, 63f));

            l.StoryBeats.Add("Gloomfang: 'I took the light so the dark couldn't have it. That was the plan. It was a bad plan.'");
            l.StoryBeats.Add("The humming is louder here. It's the Heart — and it's beating out of rhythm. It's been alone even longer than he has.");
            l.StoryBeats.Add("Gloomfang holds out the Master Sunstone with both enormous hands. 'You came all this way... FOR me?'");

            l.Gems.Add(new Vector3(0f, 6.1f, 10f));         // grab at lift 1's top
            l.Gems.Add(new Vector3(0f, 6.1f, 20f));
            l.Gems.Add(new Vector3(4f, 7.6f, 28f));
            l.Gems.Add(new Vector3(-2f, 8.6f, 36f));
            l.Gems.Add(new Vector3(-4f, 9.6f, 43f));
            l.Gems.Add(new Vector3(4f, 9.6f, 49f));
            l.Gems.Add(new Vector3(0f, 16.6f, 56f));        // grab at lift 2's top
            l.Gems.Add(new Vector3(0f, 17.1f, 63f));
            l.Gems.Add(new Vector3(3f, 18.6f, 72f));
            l.Gems.Add(new Vector3(-2f, 19.6f, 80f));
            l.Gems.Add(new Vector3(3f, 20.6f, 88f));
            l.Gems.Add(new Vector3(0f, 21.1f, 97f));
            l.Gems.Add(new Vector3(4.5f, 23.1f, 111f));
            l.Gems.Add(new Vector3(0f, 29.1f, 120f));       // grab at the last lift's top
            l.Gems.Add(new Vector3(-4.5f, 30.1f, 132f));
            l.Gems.Add(new Vector3(4.5f, 30.1f, 132f));

            l.Portal = new Vector3(0f, 29.5f, 138f);

            // Deepest dark: near-black indigo, dense fog.
            l.DarkRealm = true;
            l.SkyColor = new Color(0.12f, 0.13f, 0.24f);
            l.FogColor = new Color(0.08f, 0.09f, 0.17f);
            return l;
        }
    }
}
