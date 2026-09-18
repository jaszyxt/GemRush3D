using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack eight — levels 20-22, "Storm Chasers". A runaway baby cloud —
    /// Pip names it Nim — is giggling its way across the sky, and every
    /// laugh is a tailwind gust. Ride the gusts to gaps no jump could
    /// cross, and find where the little one sleeps.
    public static class LevelPackEight
    {
        static readonly Vector3 ZPlus = new Vector3(0f, 0f, 1f);
        static readonly Vector3 Alley = new Vector3(5f, 4f, 12f);
        static readonly Vector3 LongLane = new Vector3(5f, 5f, 14f);

        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            GustAlley(),
            WhereNimLaughs(),
            TheBabysHome()
        };

        // ------------------------------------------------------------------
        // Level 20 — "Gust Alley": the intro. Stand in the lane, wait for
        // Nim's laugh, and let the wind do the jumping.
        // ------------------------------------------------------------------
        static LevelDefinition GustAlley()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Gust Alley";
            l.Mood = SoundMood.Wind;
            l.Mission = "Something small is laughing in the wind, and every laugh blows a gust down this alley. The gaps ahead are far too wide to jump — but Nim's giggles carry further than you'd think. Stand in the lane. Wait for the giggle. Ride it.";
            l.WinLine = "Gust Alley, charted! Nim followed you the whole way, giggling. Gloomfang pretended not to be jealous. Of the baby. Or the wind. Unclear.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 26f, 7f, 1f, 7f));     // gust landing
            l.Platforms.Add(new PlatformSpec(3f, 1f, 38f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 2f, 47f, 6f, 1f, 6f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(-2f, 3f, 56f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 4f, 65f, 7f, 1f, 7f));
            l.Platforms.Add(new PlatformSpec(0f, 4f, 83f, 6f, 1f, 6f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(4f, 6f, 92f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 7f, 100f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 8f, 109f, 12f, 1f, 12f));  // summit

            l.Gusts.Add(new GustSpec(0f, 0.5f, 20f, new Vector3(5f, 4f, 16f), ZPlus, 4.4f, 2.2f, 8f));
            l.Gusts.Add(new GustSpec(0f, 4.5f, 74f, new Vector3(5f, 4f, 14f), ZPlus, 4.4f, 2.2f, 8f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 47f));
            l.Checkpoints.Add(new Vector3(0f, 4.5f, 83f));

            l.StoryBeats.Add("Nim is a cloud the size of a pillow with the volume of a parade. Gloomfang has started calling him 'the apprentice.'");
            l.StoryBeats.Add("The gusts come when Nim laughs. Pip has started telling jokes while waiting. Morale is extremely high.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 1.8f, 19f));     // mid-gust, flying
            l.Gems.Add(new Vector3(0f, 1.6f, 29f));
            l.Gems.Add(new Vector3(3f, 2.6f, 38f));
            l.Gems.Add(new Vector3(0f, 3.6f, 47f));
            l.Gems.Add(new Vector3(-2f, 4.6f, 56f));
            l.Gems.Add(new Vector3(0f, 5.6f, 65f));
            l.Gems.Add(new Vector3(0f, 5.8f, 74f));     // mid-gust, flying
            l.Gems.Add(new Vector3(0f, 5.6f, 83f));
            l.Gems.Add(new Vector3(4f, 7.6f, 92f));
            l.Gems.Add(new Vector3(-2f, 8.6f, 100f));
            l.Gems.Add(new Vector3(0f, 9.6f, 106f));

            l.Portal = new Vector3(0f, 8.5f, 112f);

            // Wide open chase-sky blue.
            l.SkyColor = new Color(0.52f, 0.70f, 0.92f);
            l.FogColor = new Color(0.48f, 0.66f, 0.88f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 21 — "Where Nim Laughs": the gusts and the sleeping
        // guardians share the garden now. Nim giggles; the guardians dream
        // through it. The course weaves both.
        // ------------------------------------------------------------------
        static LevelDefinition WhereNimLaughs()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Where Nim Laughs";
            l.Mood = SoundMood.Wind;
            l.Mission = "Nim led you to his favorite spot: a garden where the guardians sleep through weather, giggles, and everything. Gust lanes cross the beds here — so the game is timing laughs around naps. Nim, for the record, has no timing at all.";
            l.WinLine = "Where Nim Laughs, charted! The guardians slept through an entire baby storm. Professional.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 19f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 28f, 6f, 1f, 6f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 38f, 9f, 1f, 9f));     // garden arena
            l.Platforms.Add(new PlatformSpec(0f, 3f, 54f, 8f, 1f, 8f));     // gust landing
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 63f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(3f, 6f, 71f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 7f, 80f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 8f, 90f, 10f, 1f, 10f));   // garden arena
            l.Platforms.Add(new PlatformSpec(0f, 8f, 110f, 6f, 1f, 6f));    // gust landing
            l.Platforms.Add(new PlatformSpec(3f, 10f, 119f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 11f, 127f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 12f, 137f, 12f, 1f, 12f)); // summit

            l.Gusts.Add(new GustSpec(0f, 3.5f, 46f, new Vector3(5f, 4f, 10f), ZPlus, 4.4f, 2.2f, 8f));
            l.Gusts.Add(new GustSpec(0f, 8.5f, 100f, new Vector3(5f, 4f, 10f), ZPlus, 4.4f, 2.2f, 8f));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 38f, 60f, 7f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 8.5f, 90f, 75f, 8f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 28f));
            l.Checkpoints.Add(new Vector3(0f, 7.5f, 80f));
            l.Hearts.Add(new Vector3(2f, 7.6f, 80f));

            l.StoryBeats.Add("Nim laughed so hard once that he blew himself backwards. The guardians slept through it. Legendary.");
            l.StoryBeats.Add("Gloomfang is teaching Nim the lullaby hum. It is the most dangerous duet in the sky realm, and the sweetest.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 28f));
            l.Gems.Add(new Vector3(-3.5f, 4.6f, 35f));
            l.Gems.Add(new Vector3(3.5f, 4.6f, 41f));
            l.Gems.Add(new Vector3(0f, 4.8f, 46f));     // mid-gust, flying
            l.Gems.Add(new Vector3(0f, 5.6f, 54f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 63f));
            l.Gems.Add(new Vector3(3f, 7.6f, 71f));
            l.Gems.Add(new Vector3(0f, 8.6f, 80f));
            l.Gems.Add(new Vector3(-4f, 9.6f, 87f));
            l.Gems.Add(new Vector3(4f, 9.6f, 93f));
            l.Gems.Add(new Vector3(0f, 9.8f, 100f));    // mid-gust, flying
            l.Gems.Add(new Vector3(0f, 10.6f, 110f));
            l.Gems.Add(new Vector3(0f, 10.6f, 104f));

            l.Portal = new Vector3(0f, 12.5f, 140f);

            // Storm-chase evening blue.
            l.SkyColor = new Color(0.52f, 0.70f, 0.88f);
            l.FogColor = new Color(0.48f, 0.66f, 0.84f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 22 — "The Baby's Home": the longest gust lanes in the game,
        // two sleepy guardians on patrol, and at the top of the world, a
        // nest of clouds with a very small snore coming from it.
        // ------------------------------------------------------------------
        static LevelDefinition TheBabysHome()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Baby's Home";
            l.Mood = SoundMood.Wind;
            l.Mission = "The giggles end somewhere above the garden — and the wind up here blows longer and softer than anywhere else. Two guardians patrol the route. At the top: a nest of clouds, a very small snore, and a very large storm who has decided this is everyone's problem now. Ours, specifically.";
            l.WinLine = "Nim is home, asleep, one cloud-big and one Pip-small. Gloomfang built the crib out of mist and refuses to discuss it. The atlas grows one word: 'family.'";
            l.Milestone = "REGION CHARTED: STORM CHASERS. The atlas adds its first lullaby.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 19f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 28f, 5f, 1f, 5f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 38f, 9f, 1f, 9f));     // garden arena
            l.Platforms.Add(new PlatformSpec(0f, 3f, 57f, 6f, 1f, 6f));     // gust landing
            l.Platforms.Add(new PlatformSpec(4f, 5f, 66f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 6f, 75f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 7f, 84f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 8f, 95f, 10f, 1f, 10f));   // garden arena
            l.Platforms.Add(new PlatformSpec(0f, 8f, 118f, 6f, 1f, 6f));    // gust landing
            l.Platforms.Add(new PlatformSpec(3f, 10f, 127f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-2f, 11f, 136f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 12f, 146f, 13f, 1f, 13f)); // The Nest

            l.Gusts.Add(new GustSpec(0f, 3.5f, 48f, LongLane, ZPlus, 4.4f, 2.2f, 8f));
            l.Gusts.Add(new GustSpec(0f, 8.5f, 108f, new Vector3(5f, 5f, 18f), ZPlus, 4.4f, 2.2f, 8f));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 38f, 75f, 7f, 12f));
            l.Spinners.Add(new SpinnerSpec(0f, 8.5f, 95f, 75f, 8f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 28f));
            l.Checkpoints.Add(new Vector3(0f, 7.5f, 84f));
            l.Hearts.Add(new Vector3(2f, 7.6f, 84f));

            l.StoryBeats.Add("The long lanes blow soft and steady. Nim made them himself. You can tell because they wobble like a lullaby.");
            l.StoryBeats.Add("The snore from the nest echoes off the clouds. Gloomfang says it's 'rehearsing.' He is also, quietly, rehearsing.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 19f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 28f));
            l.Gems.Add(new Vector3(-3.5f, 4.6f, 35f));
            l.Gems.Add(new Vector3(3.5f, 4.6f, 41f));
            l.Gems.Add(new Vector3(0f, 4.8f, 44f));     // gust lane, takeoff
            l.Gems.Add(new Vector3(0f, 5.4f, 48f));     // gust lane, mid-flight
            l.Gems.Add(new Vector3(0f, 5.6f, 57f));
            l.Gems.Add(new Vector3(4f, 6.6f, 66f));
            l.Gems.Add(new Vector3(-2f, 7.6f, 75f));
            l.Gems.Add(new Vector3(0f, 8.6f, 84f));
            l.Gems.Add(new Vector3(-4f, 9.6f, 92f));
            l.Gems.Add(new Vector3(4f, 9.6f, 98f));
            l.Gems.Add(new Vector3(0f, 9.9f, 107f));    // gust lane 2, mid-flight
            l.Gems.Add(new Vector3(0f, 10.6f, 118f));

            l.Portal = new Vector3(0f, 12.5f, 149f);

            // Blue-white nest light.
            l.SkyColor = new Color(0.62f, 0.78f, 0.95f);
            l.FogColor = new Color(0.58f, 0.74f, 0.91f);
            return l;
        }
    }
}
