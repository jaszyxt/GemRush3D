using UnityEngine;

namespace GemRush
{
    /// Pack twelve — levels 35-37, "The Aurora Festival". The realm's
    /// thanks: with every region charted, the Sky-Keeper throws the sky
    /// realm's first festival, and the aurora itself joins in. The ride is
    /// the **aurora ribbon** — a flowing bridge of light that sways as it
    /// carries Pip across. The finale is the concert: a lap through every
    /// mechanic in the atlas, and the last note lights a permanent aurora
    /// over the menu forever.
    public static class LevelPackTwelve
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            FestivalLights(),
            RibbonDance(),
            TheFestivalFinale()
        };

        // ------------------------------------------------------------------
        // Level 35 — "Festival Lights": the ribbons teach themselves. Walk
        // on when the light reaches your shore, ride the sway, hop off at
        // the far glow. Two gentle crossings, no hazards.
        // ------------------------------------------------------------------
        static LevelDefinition FestivalLights()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Festival Lights";
            l.Mission = "The Sky-Keeper has declared a festival, and the aurora itself has come down to join it. The lights lay themselves across the gaps like ribbons off a parcel. Step on when the glow reaches your shore. Tonight the realm carries you again, for fun.";
            l.WinLine = "Festival Lights, charted! The ribbons sway in time with the bells. Nobody taught them. Auroras, it turns out, have excellent taste.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));     // walkway
            l.Platforms.Add(new PlatformSpec(0f, 2f, 38f, 7f, 1f, 7f));     // mid isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 68f, 9f, 1f, 9f));     // festival isle
            l.Platforms.Add(new PlatformSpec(0f, 5f, 80f, 8f, 1f, 8f));     // portal isle

            l.AuroraRibbons.Add(new AuroraRibbonSpec(0f, 1f, 19f,
                new Vector3(0f, 0f, 14f), 6f, 0.8f,
                new Vector3(3.4f, 0.5f, 5f)));
            l.AuroraRibbons.Add(new AuroraRibbonSpec(0f, 3f, 47f,
                new Vector3(0f, 0f, 14f), 6.5f, 1.4f,
                new Vector3(3.4f, 0.5f, 5f)));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 38f));
            l.StoryBeats.Add("Rule of ribbons: they come to your shore, they wait a breath, and then they flow. Auroras have been waiting all season to show someone this.");
            l.Checkpoints.Add(new Vector3(0f, 4.5f, 68f));
            l.StoryBeats.Add("The ribbons carry more than weight. They carry the memory of every region Pip has ever crossed.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 2.1f, 19f));      // at the R1 shore
            l.Gems.Add(new Vector3(0f, 2.1f, 26f));      // R1 mid-flow
            l.Gems.Add(new Vector3(0f, 2.1f, 32f));      // R1 far glow
            l.Gems.Add(new Vector3(0f, 3.1f, 38f));
            l.Gems.Add(new Vector3(-2.5f, 3.1f, 36f));
            l.Gems.Add(new Vector3(0f, 4.1f, 47f));      // R2 shore
            l.Gems.Add(new Vector3(0f, 4.1f, 54f));      // R2 mid
            l.Gems.Add(new Vector3(0f, 4.1f, 60f));      // R2 far
            l.Gems.Add(new Vector3(-3f, 5.1f, 68f));
            l.Gems.Add(new Vector3(3f, 5.1f, 70f));
            l.Gems.Add(new Vector3(0f, 6.1f, 79f));

            l.Portal = new Vector3(0f, 6f, 83f);

            // Festival dusk: violet-teal twilight for the aurora to glow in.
            l.SkyColor = new Color(0.45f, 0.40f, 0.62f);
            l.FogColor = new Color(0.42f, 0.38f, 0.60f);
            l.AuroraFestival = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 36 — "Ribbon Dance": the ribbons learn to swing. A deeper
        // sway, a ride that climbs as it crosses, and a drowsy guardian in
        // the meadow who wishes the music were quieter.
        // ------------------------------------------------------------------
        static LevelDefinition RibbonDance()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Ribbon Dance";
            l.Mission = "The aurora practiced. Tonight's ribbons swing as they flow — an honest-to-goodness dance — and one of them climbs while it crosses, which the charts call 'showing off.' A guardian sleeps in the meadow below, one arm over the music. Step softly, ride high.";
            l.WinLine = "Ribbon Dance, charted! The guardian slept through the whole finale. In the morning it will insist it heard everything. Let it.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));     // walkway
            l.Platforms.Add(new PlatformSpec(0f, 1f, 38f, 8f, 1f, 8f));     // ribbon isle
            l.Platforms.Add(new PlatformSpec(3f, 2f, 48f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 58f, 9f, 1f, 9f));     // guardian meadow
            l.Platforms.Add(new PlatformSpec(0f, 5f, 87f, 6f, 1f, 6f));     // high isle
            l.Platforms.Add(new PlatformSpec(2f, 6f, 96f, 5f, 1f, 5f));     // checkpoint isle + heart
            l.Platforms.Add(new PlatformSpec(0f, 7f, 107f, 10f, 1f, 10f));  // summit

            l.AuroraRibbons.Add(new AuroraRibbonSpec(0f, 1f, 19f,
                new Vector3(0f, 0f, 14f), 6f, 1.6f,
                new Vector3(3.4f, 0.5f, 5f)));
            l.AuroraRibbons.Add(new AuroraRibbonSpec(0f, 4f, 67f,
                new Vector3(0f, 0f, 12f), 6.5f, 1.6f,
                new Vector3(3.4f, 0.5f, 5f)));

            l.Spinners.Add(new SpinnerSpec(0f, 3.5f, 58f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 1.5f, 38f));
            l.Checkpoints.Add(new Vector3(2f, 6.5f, 96f));
            l.Hearts.Add(new Vector3(4f, 6.6f, 96f));
            l.StoryBeats.Add("The sway is the dance: the ribbon swings widest mid-flow and settles just as it reaches you. Time the glow, not the gap.");
            l.StoryBeats.Add("The climbing ribbon is the same aurora that carried the winter's lantern-light, still a little proud of itself.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 2.1f, 19f));      // R1 shore
            l.Gems.Add(new Vector3(1.2f, 2.1f, 26f));    // R1 mid, swung wide
            l.Gems.Add(new Vector3(0f, 2.1f, 32f));      // R1 far
            l.Gems.Add(new Vector3(0f, 2.6f, 38f));
            l.Gems.Add(new Vector3(3f, 3.6f, 48f));
            l.Gems.Add(new Vector3(-3f, 4.6f, 55f));     // meadow west
            l.Gems.Add(new Vector3(3f, 4.6f, 61f));      // meadow east
            l.Gems.Add(new Vector3(0f, 5.6f, 67f));      // R2 shore
            l.Gems.Add(new Vector3(0f, 5.6f, 73f));      // R2 mid, climbing
            l.Gems.Add(new Vector3(0f, 5.6f, 79f));      // R2 far
            l.Gems.Add(new Vector3(-2f, 6.6f, 87f));
            l.Gems.Add(new Vector3(0f, 6.6f, 89f));

            l.Portal = new Vector3(0f, 8f, 110f);

            l.SkyColor = new Color(0.42f, 0.38f, 0.62f);
            l.FogColor = new Color(0.40f, 0.37f, 0.60f);
            l.AuroraFestival = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 37 — "The Festival Finale": the concert. One lap through
        // every mechanic in the atlas — movers, a napping guardian, an
        // updraft, a gust lane, a bell and its bridge, a mirror door, the
        // winter's lantern-light, and the ribbons — ending at the portal
        // where the last note lights the permanent aurora.
        // ------------------------------------------------------------------
        static LevelDefinition TheFestivalFinale()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Festival Finale";
            l.Mission = "The last night of the festival, and the concert is built from everything you ever charted: movers keeping the beat, a guardian on percussion, wind, bells, a mirror door. Walk the whole atlas in one night, Pip. The last note is yours.";
            l.WinLine = "The Festival Finale, played! The last note rang out across every charted region at once — and high above the Sky-Keeper's hall, the sky glowed, and decided to stay that way.";
            l.Milestone = "REGION CHARTED: THE AURORA FESTIVAL. The last note is still ringing — and the sky learned to glow.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start + lantern
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 1f, 28f, 9f, 1f, 9f));     // guardian arena
            l.Platforms.Add(new PlatformSpec(0f, 1f, 40f, 6f, 1f, 6f));     // updraft isle
            l.Platforms.Add(new PlatformSpec(0f, 6f, 52f, 5f, 1f, 5f));     // high shelf
            l.Platforms.Add(new PlatformSpec(0f, 6f, 74f, 6f, 1f, 6f));     // bell isle
            l.Platforms.Add(new PlatformSpec(0f, 6f, 94f, 7f, 1f, 7f));     // bridge far isle
            l.Platforms.Add(new PlatformSpec(0f, 6f, 110f, 6f, 1f, 6f));    // door B isle
            l.Platforms.Add(new PlatformSpec(0f, 6f, 118f, 9f, 1f, 9f));    // gate isle
            l.Platforms.Add(new PlatformSpec(0f, 9f, 168f, 12f, 1f, 12f));  // summit

            l.Lanterns.Add(new LanternSpec(2.5f, 0.5f, 3f));
            l.IceGates.Add(new IceGateSpec(0f, 6.9f, 118f, 4.6f, 2.6f, 0.7f));

            l.AuroraRibbons.Add(new AuroraRibbonSpec(0f, 7f, 128f,
                new Vector3(0f, 0f, 12f), 6f, 1.2f,
                new Vector3(3.4f, 0.5f, 5f)));
            l.AuroraRibbons.Add(new AuroraRibbonSpec(0f, 8f, 149f,
                new Vector3(0f, 0f, 12f), 6.5f, 1.5f,
                new Vector3(3.4f, 0.5f, 5f)));

            l.Movers.Add(new MoverSpec(0f, 1f, 17f, new Vector3(0f, 0f, 5f), 4f));
            l.Movers.Add(new MoverSpec(0f, 1f, 46f, new Vector3(0f, 5f, 0f), 5f));

            l.Spinners.Add(new SpinnerSpec(0f, 1.5f, 28f, 60f, 7f, 12f));
            l.WindZones.Add(new WindSpec(0f, 2f, 40f, new Vector3(3.5f, 7f, 3.5f), 11f));
            // The lane spans the WHOLE crossing: from the takeoff shelf
            // (z ends 54.5) to the bell isle (z starts 71). The old
            // 12-long box (z 56-68) left a dead glide on BOTH ends — a
            // jump to get in, then 3 units of unsupported fall to the
            // isle — and play-testing the shipped build killed Pip on
            // every entry (carried ~2s, then the void). Same repair The
            // Silent Spire got: the wind overlaps takeoff and landing, so
            // stepping off the shelf is enough and the blow delivers onto
            // the isle. Lift stays 3: the isle top sits at entry height,
            // so the ride must arrive HIGH, not sinking (this is why the
            // working lift-0 lanes all overlap their landings instead).
            // The blow is 3.0s, not the default 2.2: measured in-engine,
            // one 2.2s window carries a STANDING start exactly 17.6 units
            // — 0.4 short of the isle — so a child waiting still on the
            // shelf was swept into the void. 3.0s carries the whole
            // 19-unit ride with margin; the giggle telegraph still leads
            // each blow, and a running start lands mid-isle either way.
            l.Gusts.Add(new GustSpec(0f, 6f, 62f, new Vector3(4f, 4f, 20f),
                new Vector3(0f, 0f, 1f), 4.4f, 3.0f, 8f, 3f));
            l.Bells.Add(new BellSpec(2f, 6.5f, 74f, 6f, 0));
            l.EchoBridges.Add(new EchoBridgeSpec(0f, 6.5f, 84f,
                new Vector3(3f, 0.5f, 10f), 0));
            l.MirrorDoors.Add(new MirrorDoorSpec(0f, 6.5f, 96f, 0f, 6.5f, 110f));

            l.Checkpoints.Add(new Vector3(-3f, 1.5f, 25f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 52f));
            l.Checkpoints.Add(new Vector3(0f, 6.5f, 94f));
            l.Hearts.Add(new Vector3(-3f, 2.6f, 31f));
            l.Hearts.Add(new Vector3(4f, 6.6f, 94f));

            l.StoryBeats.Add("The movers kept the beat, the guardian kept the tempo, and the wind kept its promise. Every region is playing tonight.");
            l.StoryBeats.Add("The bell is the melody now. Ring it and the bridge hums along — the towers taught it that.");
            l.StoryBeats.Add("Out of the mirror, over the winter's last gate, and onto the ribbons: the melody is waiting for someone small to walk it home.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 2.6f, 17f));      // over the mover
            l.Gems.Add(new Vector3(-2.5f, 2.6f, 25f));
            l.Gems.Add(new Vector3(2.5f, 2.6f, 31f));
            l.Gems.Add(new Vector3(0f, 2.6f, 40f));
            l.Gems.Add(new Vector3(0f, 4.1f, 46f));      // on the rising mover
            l.Gems.Add(new Vector3(0f, 7.1f, 52f));
            l.Gems.Add(new Vector3(0f, 7.1f, 62f));      // riding the gust
            l.Gems.Add(new Vector3(2f, 7.1f, 74f));      // by the bell
            l.Gems.Add(new Vector3(0f, 7.1f, 84f));      // mid-bridge
            l.Gems.Add(new Vector3(-2f, 7.1f, 110f));    // by door B
            l.Gems.Add(new Vector3(0f, 7.1f, 118f));     // at the winter gate
            l.Gems.Add(new Vector3(0f, 8.1f, 128f));     // R1 shore
            l.Gems.Add(new Vector3(0f, 8.1f, 134f));     // R1 mid
            l.Gems.Add(new Vector3(0f, 9.1f, 155f));     // R2 mid
            l.Gems.Add(new Vector3(-4f, 10.1f, 173f));

            l.Portal = new Vector3(0f, 10f, 171f);

            l.SkyColor = new Color(0.38f, 0.35f, 0.60f);
            l.FogColor = new Color(0.36f, 0.34f, 0.58f);
            l.AuroraFestival = true;
            l.AuroraUnlock = true;
            return l;
        }
    }
}
