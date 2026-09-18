using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack six — the secret bonus level. Unlocked by clearing level 15.
    /// You play as Gloomfang: no gravity, hold jump to rise, drift through
    /// his first solo Tuesday run. Pure celebration, zero falling deaths.
    public static class LevelPackSix
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            GloomfangsDayOff()
        };

        static LevelDefinition GloomfangsDayOff()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Gloomfang's Day Off";
            l.BonusFlight = true;
            l.Mission = "TUESDAY. The Sky-Keeper is away, the badge is ON, and for the first time ever, Gloomfang does his run alone. Deliver the last Sunstones to the summit porch, weave the dancing guardians, and absolutely do not cry at the sunset. (He will cry at the sunset.) Hold jump to rise. Let go to just... be.";
            l.WinLine = "Delivery complete. He hummed the whole way home — and if anyone asks, the rain was confetti. It was confetti.";
            l.Milestone = "BONUS LOGGED: GLOOMFANG'S DAY OFF. The atlas lists its first employee.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // his porch
            l.Platforms.Add(new PlatformSpec(0f, 2f, 18f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-3f, 4f, 30f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(3f, 6f, 42f, 5f, 1f, 5f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 8f, 55f, 7f, 1f, 7f));     // guardian rest
            l.Platforms.Add(new PlatformSpec(0f, 10f, 72f, 6f, 1f, 6f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(4f, 11f, 84f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(-3f, 12f, 95f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 13f, 108f, 7f, 1f, 7f));   // guardian rest
            l.Platforms.Add(new PlatformSpec(0f, 14f, 125f, 12f, 1f, 12f)); // summit porch

            l.WindZones.Add(new WindSpec(0f, 8f, 63f, new Vector3(3f, 5f, 3f), 10f));
            l.WindZones.Add(new WindSpec(0f, 11f, 99f, new Vector3(3f, 6f, 3f), 10f));

            l.Spinners.Add(new SpinnerSpec(0f, 8.5f, 55f, 60f));
            l.Spinners.Add(new SpinnerSpec(0f, 13.5f, 108f, 75f));

            l.Checkpoints.Add(new Vector3(3f, 6.5f, 42f));
            l.Checkpoints.Add(new Vector3(0f, 10.5f, 72f));

            l.StoryBeats.Add("Being the weather, it turns out, is mostly being the sky's mood ring.");
            l.StoryBeats.Add("The guardians wave. He waves back. The wave is a small localized drizzle. Progress.");

            l.BouncePads.Add(new Vector3(0f, 0.5f, 3f)); // for old times' sake

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 3.6f, 18f));
            l.Gems.Add(new Vector3(-3f, 5.6f, 30f));
            l.Gems.Add(new Vector3(3f, 7.1f, 42f));
            l.Gems.Add(new Vector3(-3.5f, 7.6f, 50f));
            l.Gems.Add(new Vector3(3.5f, 7.6f, 60f));
            l.Gems.Add(new Vector3(0f, 10.1f, 63f));    // inside the updraft
            l.Gems.Add(new Vector3(0f, 11.1f, 72f));
            l.Gems.Add(new Vector3(4f, 12.6f, 84f));
            l.Gems.Add(new Vector3(-3f, 13.6f, 95f));
            l.Gems.Add(new Vector3(-4f, 14.1f, 90f));
            l.Gems.Add(new Vector3(0f, 14.6f, 99f));    // inside updraft 2
            l.Gems.Add(new Vector3(0f, 15.6f, 108f));
            l.Gems.Add(new Vector3(-4.5f, 15.6f, 118f));
            l.Gems.Add(new Vector3(4.5f, 15.6f, 119f));
            l.Gems.Add(new Vector3(0f, 5.5f, 3f));      // above his own trampoline

            l.Portal = new Vector3(0f, 14.5f, 128f);

            // Sunset delivery light.
            l.SkyColor = new Color(0.95f, 0.72f, 0.5f);
            l.FogColor = new Color(0.90f, 0.66f, 0.45f);
            return l;
        }
    }
}
