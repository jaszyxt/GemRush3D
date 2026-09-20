using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack nine — levels 23-25, "The Bell Towers". The towers that once
    /// sang storms home still stand above the clouds. Ring an echo bell
    /// and its song turns hidden bridges solid while the tone rings out —
    /// six, seven, eight seconds of solid light, then the sky takes them
    /// back. Ring again any time.
    public static class LevelPackNine
    {
        static readonly Vector3 StandardBridge = new Vector3(3f, 0.5f, 9f);

        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            TheFirstBell(),
            ChorusInTheClouds(),
            TheSilentSpire()
        };

        // ------------------------------------------------------------------
        // Level 23 — "The First Bell": one bell, one sleeping bridge, one
        // lesson — the song makes the path.
        // ------------------------------------------------------------------
        static LevelDefinition TheFirstBell()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The First Bell";
            l.Mood = SoundMood.Bells;
            l.Mission = "Above the clouds stand towers that once sang the storms home. Their bells still work — touch one and its echo turns the hidden bridges solid for as long as the tone sings. The towers have waited a thousand years; they can wait for you to practice.";
            l.WinLine = "The first bell rung in a thousand years. Somewhere, a tower remembered what it was for.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 16f, 4f, 1f, 4f));     // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 0f, 32f, 6f, 1f, 6f));     // bridge landing
            l.Platforms.Add(new PlatformSpec(3f, 1f, 40f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 48f, 5f, 1f, 5f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 58f, 7f, 1f, 7f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 64f, 4f, 1f, 4f));     // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 3f, 79f, 6f, 1f, 6f));     // bridge landing
            l.Platforms.Add(new PlatformSpec(3f, 4f, 87f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 95f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 6f, 105f, 12f, 1f, 12f));  // summit

            l.Bells.Add(new BellSpec(0f, 0.5f, 16f, 6f, 0));
            l.Bells.Add(new BellSpec(0f, 3.5f, 64f, 6f, 1));

            l.EchoBridges.Add(new EchoBridgeSpec(0f, -0.25f, 24f,
                new Vector3(3f, 0.5f, 10f), 0));
            l.EchoBridges.Add(new EchoBridgeSpec(0f, 2.75f, 71f,
                new Vector3(3f, 0.5f, 10f), 1));

            l.Checkpoints.Add(new Vector3(-2f, 2.5f, 48f));
            l.Checkpoints.Add(new Vector3(0f, 3.5f, 79f));

            l.StoryBeats.Add("The bridge is made of the same stuff as the echo. Listen with your feet.");
            l.StoryBeats.Add("Two bells, two notes. The towers are tuning themselves to you.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 1.8f, 16f));     // beside the bell
            l.Gems.Add(new Vector3(0f, 1.7f, 24f));     // on the echo bridge
            l.Gems.Add(new Vector3(0f, 1.6f, 32f));
            l.Gems.Add(new Vector3(3f, 2.6f, 40f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 48f));
            l.Gems.Add(new Vector3(0f, 4.6f, 58f));
            l.Gems.Add(new Vector3(0f, 4.8f, 64f));     // beside bell two
            l.Gems.Add(new Vector3(0f, 4.7f, 71f));     // on the echo bridge
            l.Gems.Add(new Vector3(0f, 4.6f, 79f));
            l.Gems.Add(new Vector3(3f, 5.6f, 87f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 95f));

            l.Portal = new Vector3(0f, 6.5f, 108f);

            // Dusk gold over old stone.
            l.SkyColor = new Color(0.85f, 0.70f, 0.55f);
            l.FogColor = new Color(0.80f, 0.65f, 0.50f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 24 — "Chorus in the Clouds": bells and guardians share the
        // towers now. Ring while they dream; run while the song holds.
        // ------------------------------------------------------------------
        static LevelDefinition ChorusInTheClouds()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Chorus in the Clouds";
            l.Mood = SoundMood.Bells;
            l.Mission = "Three towers, three bells, and a chorus that needs a conductor with very small hands. Ring the first, cross on the echo, and mind the guardians — they wake if you linger, and they do not appreciate bell music. Philistines.";
            l.WinLine = "A three-bell chorus, performed flawlessly. The guardians slept through the finale. Critics.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 27f, 6f, 1f, 6f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 37f, 9f, 1f, 9f));     // guardian arena + bell
            l.Platforms.Add(new PlatformSpec(0f, 3f, 56f, 5f, 1f, 5f));     // echo landing
            l.Platforms.Add(new PlatformSpec(-2f, 4f, 63f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(3f, 5f, 70f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 6f, 78f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 6f, 84f, 4f, 1f, 4f));     // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 7f, 99f, 6f, 1f, 6f));     // bridge landing + checkpoint
            l.Platforms.Add(new PlatformSpec(0f, 8f, 108f, 9f, 1f, 9f));    // guardian arena
            l.Platforms.Add(new PlatformSpec(3f, 9f, 118f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 10f, 125f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 11f, 133f, 7f, 1f, 7f));   // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 11f, 148f, 12f, 1f, 12f)); // summit

            l.Bells.Add(new BellSpec(0f, 3.5f, 41f, 6f, 0));
            l.Bells.Add(new BellSpec(0f, 6.5f, 84f, 7f, 1));
            l.Bells.Add(new BellSpec(0f, 11.5f, 133f, 8f, 2));

            l.EchoBridges.Add(new EchoBridgeSpec(0f, 3.25f, 48f,
                new Vector3(3f, 0.5f, 8f), 0));
            l.EchoBridges.Add(new EchoBridgeSpec(0f, 6.25f, 91f,
                new Vector3(3f, 0.5f, 9f), 1));
            l.EchoBridges.Add(new EchoBridgeSpec(0f, 11.25f, 140f,
                new Vector3(3f, 0.5f, 9f), 2));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 37f, 60f, 7f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 8.5f, 108f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(-2f, 2.5f, 27f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 78f));
            l.Checkpoints.Add(new Vector3(0f, 7.5f, 99f));
            l.Hearts.Add(new Vector3(2f, 6.6f, 78f));

            l.StoryBeats.Add("The first tower's bell is tuned to Gloomfang's old lullaby. He asked us not to ring that one. We ring that one.");
            l.StoryBeats.Add("Bridges of sound hold you the way songs hold memories: briefly, and only if you keep moving.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 27f));
            l.Gems.Add(new Vector3(-3.5f, 4.6f, 34f));
            l.Gems.Add(new Vector3(3.5f, 4.6f, 40f));
            l.Gems.Add(new Vector3(0f, 4.3f, 48f));     // on the echo bridge
            l.Gems.Add(new Vector3(0f, 4.6f, 56f));
            l.Gems.Add(new Vector3(-2f, 5.6f, 63f));
            l.Gems.Add(new Vector3(3f, 6.6f, 70f));
            l.Gems.Add(new Vector3(0f, 7.6f, 78f));
            l.Gems.Add(new Vector3(0f, 7.3f, 91f));     // on the echo bridge
            l.Gems.Add(new Vector3(0f, 9.6f, 99f));
            l.Gems.Add(new Vector3(-3.5f, 9.6f, 105f));
            l.Gems.Add(new Vector3(3.5f, 9.6f, 111f));

            l.Portal = new Vector3(0f, 11.5f, 151f);

            // Lavender dusk.
            l.SkyColor = new Color(0.75f, 0.65f, 0.85f);
            l.FogColor = new Color(0.70f, 0.60f, 0.80f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 25 — "The Silent Spire": the tallest tower has the quietest
        // bell. Guardians on two floors, one gust crossing, and the spire
        // at the top where no one has ever sung.
        // ------------------------------------------------------------------
        static LevelDefinition TheSilentSpire()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Silent Spire";
            l.Mood = SoundMood.Bells;
            l.Mission = "The Silent Spire: the tallest tower, the quietest bell, and the one note no one has ever heard. Two guardians patrol the climb, a gust crosses the mid-air gap, and at the very top waits the note the whole sky has been missing. Ring it, Pip.";
            l.WinLine = "The Silent Spire sang its first note in a thousand years, and every bell in every tower answered. The sky realm has a choir now.";
            l.Milestone = "REGION CHARTED: THE BELL TOWERS. The page hums.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 27f, 5f, 1f, 5f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 36f, 9f, 1f, 9f));     // guardian arena
            l.Platforms.Add(new PlatformSpec(0f, 3f, 42f, 4f, 1f, 4f));     // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 3f, 57f, 6f, 1f, 6f));     // echo landing
            l.Platforms.Add(new PlatformSpec(3f, 4f, 79f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 87f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 6f, 95f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 6f, 101f, 4f, 1f, 4f));    // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 7f, 116f, 6f, 1f, 6f));    // echo landing
            l.Platforms.Add(new PlatformSpec(0f, 10f, 130f, 9f, 1f, 9f));   // guardian arena
            l.Platforms.Add(new PlatformSpec(0f, 10f, 135f, 4f, 1f, 4f));   // bell pedestal
            l.Platforms.Add(new PlatformSpec(0f, 11f, 151f, 12f, 1f, 12f)); // The Silent Spire

            // The lane spans from the echo landing (z 54-60) all the way
            // OVER the far platform (z 77-81) and lifts as it blows — the
            // ride has to land you, not drop you at its edge (this lane
            // used to end at z 68 and dump Pip 9 units short of the next
            // platform, mid-air with no lift: an impossible crossing).
            l.Gusts.Add(new GustSpec(0f, 3.5f, 68f, new Vector3(5f, 4f, 20f),
                new Vector3(0f, 0f, 1f), 4.4f, 2.2f, 8f, 1.2f));

            l.Bells.Add(new BellSpec(0f, 3.5f, 42f, 6f, 0));
            l.Bells.Add(new BellSpec(0f, 6.5f, 101f, 7f, 1));
            l.Bells.Add(new BellSpec(0f, 10.5f, 135f, 8f, 2));

            l.EchoBridges.Add(new EchoBridgeSpec(0f, 3.25f, 49f,
                new Vector3(3f, 0.5f, 9f), 0));
            l.EchoBridges.Add(new EchoBridgeSpec(0f, 6.25f, 108f,
                new Vector3(3f, 0.5f, 9f), 1));
            l.EchoBridges.Add(new EchoBridgeSpec(0f, 10.25f, 142f,
                new Vector3(3f, 0.5f, 10f), 2));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 36f, 60f, 7f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 10.5f, 130f, 75f, 8f, 12f));

            l.Checkpoints.Add(new Vector3(-2f, 2.5f, 27f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 95f));
            l.Hearts.Add(new Vector3(2f, 6.6f, 95f));

            l.StoryBeats.Add("A spire with no name and a bell with no scratch on it. It has never been rung. Not once. Not ever.");
            l.StoryBeats.Add("Up here the wind stands aside. Even it wants to hear this.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 27f));
            l.Gems.Add(new Vector3(-3.5f, 4.6f, 33f));
            l.Gems.Add(new Vector3(3.5f, 4.6f, 39f));
            l.Gems.Add(new Vector3(0f, 4.8f, 42f));     // beside the bell
            l.Gems.Add(new Vector3(0f, 4.3f, 49f));     // on the echo bridge
            l.Gems.Add(new Vector3(0f, 4.6f, 57f));
            l.Gems.Add(new Vector3(0f, 4.4f, 63f));     // mid-gust, flying
            l.Gems.Add(new Vector3(0f, 4.6f, 71f));
            l.Gems.Add(new Vector3(3f, 5.6f, 79f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 87f));
            l.Gems.Add(new Vector3(0f, 7.6f, 95f));
            l.Gems.Add(new Vector3(0f, 7.3f, 108f));    // on the echo bridge
            l.Gems.Add(new Vector3(0f, 11.6f, 118f));

            l.Portal = new Vector3(0f, 11.5f, 154f);

            // Deep violet, high and hushed.
            l.SkyColor = new Color(0.45f, 0.35f, 0.70f);
            l.FogColor = new Color(0.41f, 0.31f, 0.64f);
            return l;
        }
    }
}
