using UnityEngine;

namespace GemRush
{
    /// Pack thirteen — levels 38-40, "The Homecoming". The atlas is
    /// complete, and the Sky-Keeper sends Pip home the long way. One last
    /// new idea, the gentlest yet: **see-saw planks** that tip under Pip's
    /// weight — tip an end down and the treats on the shelves beneath come
    /// up to meet you; cross one and it becomes a living ramp. At the end
    /// of the road is home, and Gloomfang's badge stops saying
    /// "probationary". Content era closes at forty levels.
    public static class LevelPackThirteen
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            TheLongWayHome(),
            TippingPoints(),
            ComingHome()
        };

        // ------------------------------------------------------------------
        // Level 38 — "The Long Way Home": the see-saws teach themselves.
        // Stand on an end and it dips, bringing the gem on the shelf below
        // up within reach. Cross the plank and hop off the far side.
        // ------------------------------------------------------------------
        static LevelDefinition TheLongWayHome()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Long Way Home";
            l.Mission = "The atlas is full — every region charted, every portal lit — and the Sky-Keeper has one last errand: walk home the long way, the scenic way, past the see-saw meadows the old keepers built for exactly this kind of stroll. Tip a plank and see what tips back. Home isn't going anywhere.";
            l.WinLine = "The Long Way Home, walked! The see-saws kept tipping long after Pip left, greeting nobody. The old keepers would have called that the whole point.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));     // walkway
            l.Platforms.Add(new PlatformSpec(0f, 0f, 19f, 4f, 1f, 4f));     // see-saw pedestal
            l.Platforms.Add(new PlatformSpec(0f, 0f, 26f, 6f, 1f, 6f));     // mid isle
            l.Platforms.Add(new PlatformSpec(0f, 0f, 35f, 4f, 1f, 4f));     // see-saw pedestal
            l.Platforms.Add(new PlatformSpec(0f, 1f, 44f, 6f, 1f, 6f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 1f, 53f, 4f, 1f, 4f));     // see-saw pedestal
            l.Platforms.Add(new PlatformSpec(0f, 2f, 62f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 74f, 10f, 1f, 10f));   // summit

            l.SeeSaws.Add(new SeeSawSpec(0f, 0.5f, 19f, 7f, 2.6f, "z", 11f));
            l.SeeSaws.Add(new SeeSawSpec(0f, 0.5f, 35f, 7f, 2.6f, "z", 11f));
            l.SeeSaws.Add(new SeeSawSpec(0f, 1.5f, 53f, 8f, 2.6f, "z", 11f));

            l.Checkpoints.Add(new Vector3(0f, 1.5f, 44f));
            l.StoryBeats.Add("A see-saw remembers who stands on it. Pip tips it this way, and dinner — gems, in this case — tips back.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 0.2f, 16f));      // S1 near shelf
            l.Gems.Add(new Vector3(0f, 0.2f, 22.5f));    // S1 far shelf
            l.Gems.Add(new Vector3(0f, 1.1f, 26f));
            l.Gems.Add(new Vector3(0f, 0.2f, 32.5f));    // S2 near shelf
            l.Gems.Add(new Vector3(0f, 0.2f, 37.5f));    // S2 far shelf
            l.Gems.Add(new Vector3(0f, 2.1f, 44f));
            l.Gems.Add(new Vector3(0f, 1.2f, 50f));      // S3 near shelf
            l.Gems.Add(new Vector3(0f, 1.2f, 56f));      // S3 far shelf
            l.Gems.Add(new Vector3(0f, 3.1f, 62f));
            l.Gems.Add(new Vector3(-3f, 4.1f, 71f));
            l.Gems.Add(new Vector3(3f, 4.1f, 71f));

            l.Portal = new Vector3(0f, 4f, 77f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 39 — "Tipping Points": wider planks, a crossing that tips
        // side-to-side instead, and a drowsy guardian who has opinions
        // about all the creaking.
        // ------------------------------------------------------------------
        static LevelDefinition TippingPoints()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Tipping Points";
            l.Mission = "The long way home runs through the old testing grounds, where the keepers built see-saws in every size and temperament: one long enough to sunbathe on, one that tips sideways just to be contrarian. A guardian lies in the meadow between them, complaining about the creaking in its sleep. Mind the tipping points.";
            l.WinLine = "Tipping Points, charted! The contrarian see-saw tipped sideways out of pure spite and dropped a gem in Pip's hands. Nobody can prove it meant to.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));     // walkway
            l.Platforms.Add(new PlatformSpec(0f, 1f, 28f, 8f, 1f, 8f));     // plank arena A
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 40f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 50f, 9f, 1f, 9f));     // guardian meadow
            l.Platforms.Add(new PlatformSpec(0f, 4f, 63f, 8f, 1f, 8f));     // plank isle
            l.Platforms.Add(new PlatformSpec(-2f, 4.5f, 72f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(2f, 5f, 80f, 5f, 1f, 5f));     // checkpoint isle + heart
            l.Platforms.Add(new PlatformSpec(0f, 6f, 92f, 10f, 1f, 10f));   // summit

            l.Movers.Add(new MoverSpec(0f, 0f, 17f, new Vector3(0f, 0f, 5f), 4f));

            l.SeeSaws.Add(new SeeSawSpec(0f, 1.5f, 28f, 8f, 3f, "x", 12f));
            l.SeeSaws.Add(new SeeSawSpec(0f, 4.5f, 63f, 9f, 2.6f, "z", 12f));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 50f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(-3.5f, 3.5f, 47f));
            l.Checkpoints.Add(new Vector3(2f, 5.5f, 80f));
            l.Hearts.Add(new Vector3(3.4f, 5.6f, 80f));

            l.StoryBeats.Add("Stand on an end and the world leans with you. The keepers called it engineering; the gems call it room service.");
            l.StoryBeats.Add("The sideways plank is the youngest see-saw in the realm. It has not learned which way is down yet. Pip finds this relatable.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 2.1f, 15f));
            l.Gems.Add(new Vector3(-3.8f, 1.2f, 28f));   // plank A tip shelf, west
            l.Gems.Add(new Vector3(3.8f, 1.2f, 28f));    // plank A tip shelf, east
            l.Gems.Add(new Vector3(-2f, 3.1f, 40f));
            l.Gems.Add(new Vector3(-3f, 4.1f, 47f));
            l.Gems.Add(new Vector3(3f, 4.1f, 53f));
            l.Gems.Add(new Vector3(0f, 5.1f, 59f));      // plank B shore
            l.Gems.Add(new Vector3(0f, 5.1f, 66f));      // plank B far
            l.Gems.Add(new Vector3(-2f, 5.6f, 72f));
            l.Gems.Add(new Vector3(2f, 6.1f, 80f));
            l.Gems.Add(new Vector3(-3.5f, 7.1f, 89f));
            l.Gems.Add(new Vector3(3.5f, 7.1f, 89f));

            l.Portal = new Vector3(0f, 7f, 95f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 40 — "Coming Home": the last level. Sunset light, a gentle
        // lap through everything, and the road ends at home.
        // ------------------------------------------------------------------
        static LevelDefinition ComingHome()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Coming Home";
            l.Mission = "The last stretch of the long way home, walked at sunset. Movers and wind and one very sleepy guardian — old friends, all of them — and the see-saws creaking their welcome. At the end of this road there is a small island, a shelf with one open spot, and a storm who has been practicing saying 'welcome home' without crying. Mostly without crying.";
            l.WinLine = "Home. Gloomfang said it without crying, right up until Pip put the last gem on the shelf, and then he cried a little, and that was fine. The Sky-Keeper, watching from the hall, took out a pen and crossed out one word on a badge.";
            l.Milestone = "REGION CHARTED: THE HOMECOMING. Gloomfang's badge is official — the word 'probationary' is gone.";

            l.Mood = SoundMood.Sunset;

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));     // walkway
            l.Platforms.Add(new PlatformSpec(0f, 0f, 19f, 4f, 1f, 4f));     // see-saw pedestal
            l.Platforms.Add(new PlatformSpec(0f, 0f, 26f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 1f, 44f, 9f, 1f, 9f));     // guardian meadow
            l.Platforms.Add(new PlatformSpec(0f, 1f, 57f, 4f, 1f, 4f));     // see-saw pedestal
            l.Platforms.Add(new PlatformSpec(0f, 2f, 66f, 6f, 1f, 6f));     // updraft isle
            l.Platforms.Add(new PlatformSpec(0f, 7f, 76f, 6f, 1f, 6f));     // high shelf
            l.Platforms.Add(new PlatformSpec(0f, 7f, 85f, 4f, 1f, 4f));     // see-saw pedestal
            l.Platforms.Add(new PlatformSpec(0f, 8f, 94f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 9f, 106f, 12f, 1f, 12f));  // home isle

            l.SeeSaws.Add(new SeeSawSpec(0f, 0.5f, 19f, 7f, 2.6f, "z", 11f));
            l.SeeSaws.Add(new SeeSawSpec(0f, 1.5f, 57f, 9f, 2.6f, "z", 12f));
            l.SeeSaws.Add(new SeeSawSpec(0f, 7.5f, 85f, 8f, 2.6f, "z", 11f));

            l.Movers.Add(new MoverSpec(0f, 1f, 34f, new Vector3(0f, 0f, 5f), 4.5f));
            l.Spinners.Add(new SpinnerSpec(0f, 1.5f, 44f, 60f, 7f, 12f));
            l.WindZones.Add(new WindSpec(0f, 3f, 66f, new Vector3(3.5f, 7f, 3.5f), 11f));

            l.Checkpoints.Add(new Vector3(-3.5f, 1.5f, 41f));
            l.Checkpoints.Add(new Vector3(0f, 2.5f, 66f));
            l.Checkpoints.Add(new Vector3(0f, 7.5f, 76f));
            l.Hearts.Add(new Vector3(3.5f, 2.6f, 41f));
            l.Hearts.Add(new Vector3(2.5f, 8.1f, 76f));

            l.StoryBeats.Add("Every creak of the see-saws is a hello from a keeper who finished their walk long ago and left the planks oiled for whoever came after.");
            l.StoryBeats.Add("The wind on the last climb is warm. Gloomfang warmed it on purpose. He would deny this under oath.");
            l.StoryBeats.Add("There it is: the little island where a very small hero once picked up a very large errand. The shelf is waiting. So is everyone else.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 0.2f, 16f));      // S1 shelves
            l.Gems.Add(new Vector3(0f, 0.2f, 22.5f));
            l.Gems.Add(new Vector3(0f, 1.1f, 26f));
            l.Gems.Add(new Vector3(0f, 2.1f, 36.5f));    // on the mover's path
            l.Gems.Add(new Vector3(-3f, 2.6f, 41f));
            l.Gems.Add(new Vector3(3f, 2.6f, 47f));
            l.Gems.Add(new Vector3(0f, 2.2f, 54f));      // S2 shore
            l.Gems.Add(new Vector3(0f, 2.2f, 60f));      // S2 far
            l.Gems.Add(new Vector3(0f, 3.1f, 66f));
            l.Gems.Add(new Vector3(0f, 8.1f, 76f));
            l.Gems.Add(new Vector3(0f, 8.2f, 83f));      // S3 shore
            l.Gems.Add(new Vector3(0f, 8.2f, 87f));      // S3 far
            l.Gems.Add(new Vector3(-4f, 10.1f, 107f));

            l.Portal = new Vector3(0f, 10f, 109f);
            return l;
        }
    }
}
