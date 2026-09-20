using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pack eleven — levels 32-34, "The Long Winter". The quietest pack:
    /// the Far Isles' sleepy corner, where Gloomfang's oldest snow never
    /// melted. One new idea, taught gently: the sunstone lantern — wake it
    /// and its warm light travels with Pip, melting the frozen gates in
    /// his path. No new danger; the ice never hurts, it only waits. On the
    /// Crystal Summit, every melted path refreezes into a map of the walk.
    public static class LevelPackEleven
    {
        public static LevelDefinition[] Levels = new LevelDefinition[]
        {
            FirstSnow(),
            FrozenFountains(),
            TheCrystalSummit()
        };

        // ------------------------------------------------------------------
        // Level 32 — "First Snow": the lantern teaches itself. Wake it at
        // the shore, walk up to the first frozen gate and stand with it a
        // moment; a side pocket hides gems behind one more optional gate.
        // ------------------------------------------------------------------
        static LevelDefinition FirstSnow()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "First Snow";
            l.Mission = "Past the mirror meadows the wind simply stopped: the Far Isles' quiet corner has fallen asleep under snow that never melts — Gloomfang's oldest winter, the charts call it. An old sunstone lantern waits on the shore, still warm underneath. Wake it, walk with its light, and let the ice remember it is water.";
            l.WinLine = "First Snow, charted! The lantern hums a lullaby it learned from a glacier. Gloomfang, professional rain carrier, takes notes.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start + lantern
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 20f, 10f, 1f, 8f));    // gate isle
            l.Platforms.Add(new PlatformSpec(3f, 1f, 30f, 9f, 1f, 9f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 38f, 5f, 1f, 5f));    // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 2f, 48f, 9f, 1f, 9f));     // pocket isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 62f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 4f, 74f, 12f, 1f, 12f));   // summit

            l.Lanterns.Add(new LanternSpec(2.5f, 0.5f, 3f));
            l.IceGates.Add(new IceGateSpec(0f, 1.8f, 20f, 9.4f, 2.6f, 0.7f));
            l.IceGates.Add(new IceGateSpec(2.2f, 3.8f, 48f, 0.7f, 2.4f, 6f));

            l.Checkpoints.Add(new Vector3(-2f, 2.5f, 38f));
            l.StoryBeats.Add("The lantern doesn't light the way so much as warm it. The ice steps aside politely. Ice is very patient.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(2f, 1.6f, 17f));
            l.Gems.Add(new Vector3(-2.5f, 1.6f, 23f));   // behind gate A
            l.Gems.Add(new Vector3(3f, 2.6f, 30f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 38f));
            l.Gems.Add(new Vector3(0f, 3.6f, 44f));
            l.Gems.Add(new Vector3(4f, 3.1f, 46f));      // the pocket trio
            l.Gems.Add(new Vector3(4f, 3.1f, 48.5f));
            l.Gems.Add(new Vector3(4f, 3.1f, 51f));
            l.Gems.Add(new Vector3(0f, 4.6f, 62f));
            l.Gems.Add(new Vector3(-3.5f, 5.6f, 71f));
            l.Gems.Add(new Vector3(3.5f, 5.6f, 74f));

            l.Portal = new Vector3(0f, 5f, 76f);

            // Winter pale: milk-blue sky, hushed fog.
            l.SkyColor = new Color(0.80f, 0.87f, 0.95f);
            l.FogColor = new Color(0.78f, 0.85f, 0.93f);
            l.LongWinter = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 33 — "Frozen Fountains": the updraft columns learned to
        // freeze. One gate stands inside a wind column (melt it while you
        // hover), one gates a gem at the top, and the ferry carries Pip
        // straight through a frozen doorway.
        // ------------------------------------------------------------------
        static LevelDefinition FrozenFountains()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Frozen Fountains";
            l.Mission = "The old fountains of the winter isles still try to sing — you can hear them humming under the ice. Ride the columns, walk with the lantern, and melt whatever stands in the way. One frozen door sits right in the ferry's path: stand on the moving ice as it carries you through, and let the light do the rest.";
            l.WinLine = "Frozen Fountains, charted! The columns sing again, a little off-key. The lantern hums along. It is possibly the worst choir in the sky realm, and Pip applauds anyway.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start + lantern
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 20f, 10f, 1f, 8f));    // gate isle
            l.Platforms.Add(new PlatformSpec(0f, 1f, 33f, 8f, 1f, 8f));     // fountain isle
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 44f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(2f, 3f, 53f, 8f, 1f, 8f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 66f, 10f, 1f, 9f));    // ferry pier
            l.Platforms.Add(new PlatformSpec(0f, 3f, 86f, 7f, 1f, 7f));     // ferry landing
            l.Platforms.Add(new PlatformSpec(-2f, 4f, 94f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 5f, 104f, 12f, 1f, 12f));  // summit

            l.Lanterns.Add(new LanternSpec(2.5f, 0.5f, 3f));
            l.IceGates.Add(new IceGateSpec(0f, 1.8f, 20f, 9.4f, 2.6f, 0.7f));
            l.IceGates.Add(new IceGateSpec(0f, 6.4f, 33f, 3.2f, 2.4f, 0.7f));
            l.IceGates.Add(new IceGateSpec(0f, 4.9f, 77f, 6f, 2.8f, 0.7f));

            l.WindZones.Add(new WindSpec(0f, 2f, 33f, new Vector3(3.5f, 7f, 3.5f), 11f));
            l.Movers.Add(new MoverSpec(0f, 3f, 74f, new Vector3(0f, 0f, 6f), 4.5f));

            l.Checkpoints.Add(new Vector3(2f, 3.5f, 53f));
            l.Checkpoints.Add(new Vector3(-2f, 4.5f, 94f));
            l.StoryBeats.Add("Press up against a frozen door and hold still. The lantern does the rest. Patience, again — winter's favorite word.");
            l.StoryBeats.Add("The ferry has been stuck mid-crossing since the freeze. Pip melts the door; the ferry finally finishes a run. It seems pleased.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 1.6f, 20f));      // behind gate A
            l.Gems.Add(new Vector3(3f, 1.6f, 24f));
            l.Gems.Add(new Vector3(0f, 3.1f, 31f));
            l.Gems.Add(new Vector3(0f, 9f, 33f));        // above the column, past gate B
            l.Gems.Add(new Vector3(-2f, 3.6f, 44f));
            l.Gems.Add(new Vector3(2f, 4.6f, 53f));
            l.Gems.Add(new Vector3(0f, 4.6f, 62f));
            l.Gems.Add(new Vector3(0f, 5.2f, 77f));      // mid-ferry, behind gate C
            l.Gems.Add(new Vector3(0f, 4.6f, 86f));
            l.Gems.Add(new Vector3(-2f, 5.6f, 94f));
            l.Gems.Add(new Vector3(-3.5f, 6.6f, 101f));
            l.Gems.Add(new Vector3(3.5f, 6.6f, 101f));

            l.Portal = new Vector3(0f, 6f, 107f);

            l.SkyColor = new Color(0.76f, 0.85f, 0.95f);
            l.FogColor = new Color(0.74f, 0.83f, 0.92f);
            l.LongWinter = true;
            return l;
        }

        // ------------------------------------------------------------------
        // Level 34 — "The Crystal Summit": the finale. Gates, a column, the
        // ferry trick practiced on a climb, and one sleeping guardian in
        // the meadow below the last ascent. At the portal, the melted paths
        // refreeze into a crystal map of the whole walk.
        // ------------------------------------------------------------------
        static LevelDefinition TheCrystalSummit()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Crystal Summit";
            l.Mission = "One last climb, to the winter's rooftop. The charts say something odd happens up there: every path the lantern's light opened is supposed to refreeze at once — into a map of everywhere you walked. A guardian naps in the meadow on the way up. Step lightly. Or don't; he sleeps like weather.";
            l.WinLine = "The Crystal Summit, charted! The refreeze settles into one enormous crystal map, and there in the middle, tiny and unmistakable: a small orange shape, walking. Pip frames it in his hands and refuses to move until everyone has looked.";
            l.Milestone = "REGION CHARTED: THE LONG WINTER. The atlas shines a little warmer now.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start + lantern
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 0f, 20f, 10f, 1f, 8f));    // gate isle
            l.Platforms.Add(new PlatformSpec(3f, 1f, 30f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 2f, 39f, 7f, 1f, 7f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 3f, 52f, 8f, 1f, 8f));     // fountain isle
            l.Platforms.Add(new PlatformSpec(-2f, 3.5f, 60f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 4f, 67f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(2f, 5f, 75f, 5f, 1f, 5f));     // checkpoint isle + heart
            l.Platforms.Add(new PlatformSpec(0f, 6f, 88f, 12f, 1f, 12f));   // meadow arena
            l.Platforms.Add(new PlatformSpec(0f, 7f, 101f, 6f, 1f, 6f));    // gate approach
            l.Platforms.Add(new PlatformSpec(0f, 8f, 113f, 12f, 1f, 12f));  // summit

            l.Lanterns.Add(new LanternSpec(2.5f, 0.5f, 3f));
            l.IceGates.Add(new IceGateSpec(0f, 1.8f, 20f, 9.4f, 2.6f, 0.7f));
            l.IceGates.Add(new IceGateSpec(0f, 7.6f, 52f, 3.2f, 2.4f, 0.7f));
            l.IceGates.Add(new IceGateSpec(0f, 8.9f, 101f, 5f, 2.6f, 0.7f));

            l.WindZones.Add(new WindSpec(0f, 4f, 52f, new Vector3(3.5f, 8f, 3.5f), 11f));

            l.Spinners.Add(new SpinnerSpec(0f, 6.5f, 88f, 60f, 7f, 12f));

            l.Checkpoints.Add(new Vector3(0f, 2.5f, 39f));
            l.Checkpoints.Add(new Vector3(2f, 5.5f, 75f));
            l.Checkpoints.Add(new Vector3(-4f, 6.5f, 83f));
            l.Hearts.Add(new Vector3(4f, 5.6f, 75f));

            l.StoryBeats.Add("A guardian footprint, frozen mid-step, preserved like a fossil. Guardians nap where they guard. It is the whole job description.");
            l.StoryBeats.Add("The lantern flickers in time with the columns' humming. Somehow it knows this place. Lanterns are older than maps.");
            l.StoryBeats.Add("The meadow below is already glowing. The refreeze has started — the winter is drawing the map early, the way children pack before the goodbyes.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(0f, 1.6f, 20f));      // behind gate A
            l.Gems.Add(new Vector3(3f, 2.6f, 30f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 38f));
            l.Gems.Add(new Vector3(2f, 3.6f, 40f));
            l.Gems.Add(new Vector3(0f, 4.1f, 50f));
            l.Gems.Add(new Vector3(0f, 10.5f, 52f));     // above the column, past gate B
            l.Gems.Add(new Vector3(-2f, 4.6f, 60f));
            l.Gems.Add(new Vector3(0f, 5.1f, 67f));
            l.Gems.Add(new Vector3(2f, 6.1f, 75f));
            l.Gems.Add(new Vector3(-3f, 6.1f, 75f));
            l.Gems.Add(new Vector3(-4f, 7.1f, 83f));
            l.Gems.Add(new Vector3(4f, 7.1f, 93f));
            l.Gems.Add(new Vector3(0f, 9.6f, 104f));

            l.Portal = new Vector3(0f, 9f, 116f);

            l.SkyColor = new Color(0.72f, 0.82f, 0.94f);
            l.FogColor = new Color(0.70f, 0.80f, 0.91f);
            l.LongWinter = true;
            return l;
        }
    }
}
