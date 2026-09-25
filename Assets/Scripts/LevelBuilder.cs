using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GemRush
{
    /// Turns a LevelDefinition into GameObjects. Knows nothing about which
    /// level is current or progression — GameManager drives that.
    public static class LevelBuilder
    {
        public static void Build(LevelDefinition level, Transform parent)
        {
            Material grass = ArtLib.Solid(ArtLib.Grass, 0f);
            Material dirt = ArtLib.Solid(ArtLib.Dirt, 0f);
            // Snow-soft tops: winter levels swap the grass slab for snow.
            Material snow = ArtLib.Solid(ArtLib.Snow, 0.05f);
            Material topMat = level.LongWinter ? snow : grass;
            Material moverMat = ArtLib.Solid(ArtLib.MoverOrange, 0.15f);
            Material elevatorMat = ArtLib.Solid(ArtLib.ElevatorBlue, 0.15f);
            Material stone = ArtLib.Solid(ArtLib.Stone, 0f);
            Material cloudMat = ArtLib.Solid(ArtLib.CloudWhite, 0.25f);
            Material trunk = ArtLib.Solid(ArtLib.Trunk, 0f);
            // Winter props wear frost: muted white-green canopies, pale rocks.
            Material leaf = ArtLib.Solid(
                level.LongWinter ? ArtLib.FrostedLeaf : ArtLib.Leaf, 0f);
            Material rock = ArtLib.Solid(
                level.LongWinter ? ArtLib.FrostedRock : ArtLib.Rock, 0f);

            int nameSeed = 0;
            for (int i = 0; i < level.Name.Length; i++) nameSeed += level.Name[i];

            for (int i = 0; i < level.Platforms.Count; i++)
            {
                PlatformSpec p = level.Platforms[i];
                GameObject platformObj = Platform(parent, p.Center, p.Size, topMat, dirt);

                // Guardians standing on this platform own a keep-clear zone:
                // props (especially tall trees) must never grow inside their
                // sweep, or the spinning arm would pass through them.
                List<Vector2> spinnerZones = new List<Vector2>();
                for (int s = 0; s < level.Spinners.Count; s++)
                {
                    Vector3 top = level.Spinners[s].PlatformTop;
                    float dx = top.x - p.Center.x;
                    float dz = top.z - p.Center.z;
                    if (Mathf.Abs(dx) <= p.Size.x * 0.5f &&
                        Mathf.Abs(dz) <= p.Size.z * 0.5f)
                        spinnerZones.Add(new Vector2(dx, dz));
                }

                Props.DressPlatform(platformObj.transform, p.Size,
                    nameSeed * 17 + i * 101, trunk, leaf, rock, spinnerZones);
            }

            for (int i = 0; i < level.Movers.Count; i++)
            {
                MoverSpec m = level.Movers[i];
                Mover(parent, m.Center, m.Size, m.Offset, m.Period, moverMat, dirt);
            }

            for (int i = 0; i < level.Spinners.Count; i++)
            {
                SpinnerSpec s = level.Spinners[i];
                Spinner.Create(parent, s.PlatformTop, s.DegreesPerSecond,
                    s.WakeRadius, s.SleepSpeed);
            }

            for (int i = 0; i < level.Checkpoints.Count; i++)
            {
                string beat = i < level.StoryBeats.Count
                    ? level.StoryBeats[i] : null;
                Checkpoint.Create(parent, level.Checkpoints[i], beat,
                    string.IsNullOrEmpty(beat)
                        ? null : VoiceIds.Beat(level.Name, i));
            }

            for (int i = 0; i < level.BouncePads.Count; i++)
                BouncePad.Create(parent, level.BouncePads[i]);

            for (int i = 0; i < level.Hearts.Count; i++)
                HeartPickup.Create(parent, level.Hearts[i]);

            for (int i = 0; i < level.WindZones.Count; i++)
            {
                WindSpec w = level.WindZones[i];
                Updraft.Create(parent, w.Center, w.Size, w.Lift);
            }

            for (int i = 0; i < level.Gusts.Count; i++)
            {
                GustSpec g = level.Gusts[i];
                GustZone.Create(parent, g.Center, g.Size, g.Direction,
                    g.Period, g.ActiveTime, g.Strength);
            }

            if (level.Bells.Count > 0 || level.EchoBridges.Count > 0)
                BellRig.Create(parent);

            Material bellGold = ArtLib.Solid(ArtLib.Gold, 0.8f);
            for (int i = 0; i < level.Bells.Count; i++)
                Bell.Create(parent, level.Bells[i], bellGold);

            for (int i = 0; i < level.EchoBridges.Count; i++)
            {
                EchoBridgeSpec b = level.EchoBridges[i];
                // Emission 0.6: a solid bridge must read as WALKABLE
                // SURFACE, not atmosphere. Without emission it sat at
                // ~1.03:1 against pale skies — invisible on the very
                // viewing angle that matters (down a long Z-span).
                EchoBridge.Create(parent, b,
                    ArtLib.Solid(ArtLib.Air, 0.6f));
            }

            for (int i = 0; i < level.MirrorDoors.Count; i++)
            {
                MirrorDoorSpec d = level.MirrorDoors[i];
                MirrorDoor.Create(parent, d.DoorA, d.DoorB);
            }

            for (int i = 0; i < level.Lanterns.Count; i++)
                Lantern.Create(parent, level.Lanterns[i]);

            for (int i = 0; i < level.IceGates.Count; i++)
                IceGate.Create(parent, level.IceGates[i]);

            for (int i = 0; i < level.AuroraRibbons.Count; i++)
                AuroraRibbon.Create(parent, level.AuroraRibbons[i]);

            for (int i = 0; i < level.SeeSaws.Count; i++)
                SeeSaw.Create(parent, level.SeeSaws[i]);

            for (int i = 0; i < level.Gems.Count; i++)
            {
                Gem gem = Gem.Create(parent, level.Gems[i]);
                if (level.SkyGarden)
                {
                    // Melody gems: a C-major pentatonic climb, so any trail
                    // of collection sounds like a little song.
                    float[] melody = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };
                    gem.noteFrequency = melody[i % melody.Length];
                }
            }

            if (level.SkyGarden) ScatterBuds(level, parent);
            if (level.DarkRealm) ScatterGlowStones(level, parent);

            GoalPortal.Create(parent, level.Portal);

            DailyGem.PlaceIfActive(level, parent);
            GoldenGem.PlaceIfHidden(level, parent);
            // A bench on the level's calmest off-route platform. Derived
            // from level data (nothing to author, no spec type), so it
            // cannot be forgotten by a future pack or break the audits.
            Perch.PlaceIfCalm(level, parent);

            float courseLength = level.Portal.z + 14f;
            for (int i = 0; i < level.Platforms.Count; i++)
                courseLength = Mathf.Max(courseLength,
                    level.Platforms[i].Center.z +
                    level.Platforms[i].Size.z * 0.5f);

            if (level.LongWinter)
                Snowfall.Create(parent, courseLength);
            if (level.RainyDay)
                Rainfall.Create(parent, courseLength);
            if (SkyCalendar.TodayWeather() == SkyCalendar.Weather.FestivalWeek)
                ConfettiSky.Create(parent, courseLength);

            if (level.AuroraFestival)
                AuroraBand.Create(parent, level.Portal.z + 20f);

            // Pip's shelf lives on the home island only — the start
            // platform of First Steps, next to the flag post. Name-based:
            // a rainy-day remix rebuilds the definition object, so
            // reference equality would lose the shelf on Tuesdays.
            if (level.Name == LevelLibrary.Levels[0].Name)
                Shelf.Create(parent,
                    new Vector3(-2.6f, level.Platforms[0].Center.y +
                        level.Platforms[0].Size.y * 0.5f, -1.6f));

            ApplyAtmosphere(level);

            // Ambient realm motes: subtle atmospheric life for the realms
            // that warrant it (Garden, Sunset, Undercloud, Aurora).
            AmbientMotes.Create(level, parent, courseLength);

            BuildBackdrop(level, parent, cloudMat, stone);

            GameManager.Instance.ConfigureLevel(level);
        }

        /// Sleepy flower buds on the platforms. They stay closed until the
        /// level is won — then the whole garden blooms in a sweep.
        static void ScatterBuds(LevelDefinition level, Transform parent)
        {
            int seed = 0;
            for (int i = 0; i < level.Name.Length; i++) seed += level.Name[i];
            System.Random rng = new System.Random(seed + 999);
            Material budMat = ArtLib.Solid(ArtLib.Bud, 0f);

            foreach (PlatformSpec p in level.Platforms)
            {
                if (Mathf.Min(p.Size.x, p.Size.z) < 5f) continue;
                int count = 2;
                for (int b = 0; b < count; b++)
                {
                    float bx = (((b * 29 + seed) % 7) - 3f) * 0.9f;
                    float bz = (((b * 41 + seed) % 7) - 3f) * 0.9f;
                    ArtLib.DecorSphere(parent,
                        p.Center + new Vector3(bx, p.Size.y * 0.5f + 0.1f, bz),
                        new Vector3(0.22f, 0.2f, 0.22f), budMat)
                        .name = "Bud";
                }
            }
        }

        /// The Undercloud's glowing stones: small emissive crystals set
        /// into large platforms' sides, so the dark realm carries points
        /// of warmth the story already promised ("He carried every one of
        /// them down himself"). Deterministic per level, like buds.
        static void ScatterGlowStones(LevelDefinition level, Transform parent)
        {
            int seed = 0;
            for (int i = 0; i < level.Name.Length; i++) seed += level.Name[i];
            System.Random rng = new System.Random(seed + 4177);
            Material stoneMat = ArtLib.Solid(ArtLib.GloomStone, 1.8f);

            foreach (PlatformSpec p in level.Platforms)
            {
                if (Mathf.Min(p.Size.x, p.Size.z) < 5f) continue;
                // 2–3 stones per large platform, placed on the SIDE faces
                // (not the top) so they read as set into the walls, not
                // scattered on the ground — the story describes walls.
                int count = 2 + (int)(rng.NextDouble() * 1.99);
                for (int g = 0; g < count; g++)
                {
                    // Pick a side face (±x or ±z) and a height within the
                    // platform's body, slightly below the top surface.
                    int side = (g + seed) % 4;
                    float along = (((g * 37 + seed) % 7) - 3f) * 0.8f;
                    float height = p.Size.y * (0.2f + 0.3f * (float)rng.NextDouble());
                    Vector3 pos = p.Center;
                    Vector3 scale = new Vector3(0.14f, 0.22f, 0.14f);
                    Quaternion rot = Quaternion.Euler(0f, 45f, 0f);
                    float out_ = 0.06f; // how far the stone protrudes
                    if (side == 0) // +x face
                    {
                        pos += new Vector3(p.Size.x * 0.5f + out_, height - p.Size.y * 0.5f, along);
                    }
                    else if (side == 1) // -x face
                    {
                        pos += new Vector3(-p.Size.x * 0.5f - out_, height - p.Size.y * 0.5f, along);
                    }
                    else if (side == 2) // +z face
                    {
                        pos += new Vector3(along, height - p.Size.y * 0.5f, p.Size.z * 0.5f + out_);
                    }
                    else // -z face
                    {
                        pos += new Vector3(along, height - p.Size.y * 0.5f, -p.Size.z * 0.5f - out_);
                    }
                    ArtLib.DecorCube(parent, pos, scale, rot, stoneMat)
                        .name = "GloomStone";
                }
            }
        }

        /// Fog, sky, sun and ambient light per level. The Undercloud pack
        /// runs dim and indigo; everything else keeps the bootstrap's
        /// daylight defaults. Ambient follows the mood too, or dark levels
        /// stay washed out by the bootstrap's bright Trilight.
        static void ApplyAtmosphere(LevelDefinition level)
        {
            RenderSettings.fogColor = level.FogColor;
            RenderSettings.fogDensity =
                level.DarkRealm ? 0.013f :
                level.LongWinter ? 0.011f :
                level.RainyDay ? 0.010f :
                level.MirrorSkies ? 0.010f : 0.008f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            bool isSunset = level.Mood == SoundMood.Sunset;
            if (level.DarkRealm)
            {
                RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.38f);
                RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.28f);
                RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.16f);
            }
            else if (level.LongWinter)
            {
                // Winter light: everything a little paler and cooler, so
                // the snow tops glow without the world going grey.
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.88f);
                RenderSettings.ambientEquatorColor = new Color(0.52f, 0.56f, 0.66f);
                RenderSettings.ambientGroundColor = new Color(0.44f, 0.46f, 0.50f);
            }
            else if (level.RainyDay)
            {
                // Rain-washed light: softer, cooler, the world under a
                // friendly blanket of cloud.
                RenderSettings.ambientSkyColor = new Color(0.48f, 0.56f, 0.70f);
                RenderSettings.ambientEquatorColor = new Color(0.42f, 0.48f, 0.58f);
                RenderSettings.ambientGroundColor = new Color(0.36f, 0.39f, 0.44f);
            }
            else if (isSunset)
            {
                // Golden hour: warm amber ambient that matches the peach
                // sky. Without this branch the Two Suns realms had an
                // amber sky under harsh noon light — the warmth was skin
                // deep, the light itself didn't know it was sunset.
                RenderSettings.ambientSkyColor = new Color(0.72f, 0.58f, 0.42f);
                RenderSettings.ambientEquatorColor = new Color(0.60f, 0.48f, 0.38f);
                RenderSettings.ambientGroundColor = new Color(0.42f, 0.36f, 0.30f);
            }
            else if (level.MirrorSkies)
            {
                // Mirror Skies: cool and even, like light arriving through
                // glass. Slightly desaturated so the realm reads as
                // "slightly wrong" without being gloomy.
                RenderSettings.ambientSkyColor = new Color(0.52f, 0.58f, 0.72f);
                RenderSettings.ambientEquatorColor = new Color(0.44f, 0.50f, 0.62f);
                RenderSettings.ambientGroundColor = new Color(0.36f, 0.38f, 0.42f);
            }
            else
            {
                RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.85f);
                RenderSettings.ambientEquatorColor = new Color(0.45f, 0.50f, 0.60f);
                RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.35f);
            }

            GameObject sunGo = GameObject.Find("Sun");
            Light sun = sunGo != null ? sunGo.GetComponent<Light>() : null;
            if (sun != null)
            {
                // Intensities compensated for the retired scene-default
                // light that used to double-light every level under the
                // Built-in pipeline (see GameBootstrap.Boot).
                if (level.DarkRealm)
                {
                    sun.intensity = 0.95f;
                    sun.color = new Color(0.75f, 0.82f, 1f);
                    sun.transform.rotation = Quaternion.Euler(64f, -35f, 0f);
                }
                else if (level.LongWinter)
                {
                    sun.intensity = 1.6f;
                    sun.color = new Color(0.92f, 0.95f, 1f);
                    sun.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
                }
                else if (level.RainyDay)
                {
                    sun.intensity = 1.15f;
                    sun.color = new Color(0.82f, 0.88f, 1f);
                    sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                }
                else if (isSunset)
                {
                    // Golden hour: the sun sits low and warm, so the
                    // platforms catch long amber light instead of the
                    // default high noon.
                    sun.intensity = 1.35f;
                    sun.color = new Color(1f, 0.72f, 0.45f);
                    sun.transform.rotation = Quaternion.Euler(22f, -35f, 0f);
                }
                else if (level.MirrorSkies)
                {
                    // Mirror light: cool and even, a reflection of the sun
                    // rather than the sun itself.
                    sun.intensity = 1.5f;
                    sun.color = new Color(0.88f, 0.94f, 1f);
                    sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                }
                else
                {
                    sun.intensity = 1.9f;
                    sun.color = new Color(1f, 0.96f, 0.88f);
                    sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                }
            }
        }

        /// Decorative clouds and the start marker, offset per level so every
        /// course feels a little different.
        static void BuildBackdrop(LevelDefinition level, Transform parent,
            Material cloudMat, Material stone)
        {
            int seed = 0;
            for (int i = 0; i < level.Name.Length; i++) seed += level.Name[i];

            // Soft, see-through clouds: they must never block the course, so
            // every puff uses alpha blending and stays clear of the |x| < 12
            // corridor the courses run through.
            Material cloudSoft = ArtLib.Solid(ArtLib.CloudWhite, 0f);
            ArtLib.SetFade(cloudSoft, 0.45f);
            Material cloudFaint = ArtLib.Solid(ArtLib.CloudWhite, 0f);
            ArtLib.SetFade(cloudFaint, 0.3f);

            GameObject clouds = new GameObject("Clouds");
            clouds.transform.SetParent(parent, false);

            System.Random rng = new System.Random(seed);
            for (int i = 0; i < 10; i++)
            {
                // Keep puffs well clear of the play corridor: |x| from 20 to
                // 32, so even a drifting, wide-puffed cloud never crosses
                // the center of the screen where the course lives.
                float side = (i % 2 == 0) ? -1f : 1f;
                float fx = side * (20f + ((i * 73 + seed) % 12)
                    + (float)rng.NextDouble() * 3f);
                float fy = (((i * 31 + seed) % 22) - 8f) + (float)rng.NextDouble() * 3f;
                float fz = ((i * 47 + seed) % 100) + (float)i * 10f;
                MakePuffCloud(clouds.transform, new Vector3(fx, fy, fz),
                    4.5f + (float)rng.NextDouble() * 3.5f, cloudSoft, rng);
            }

            // A sparse high layer for depth, drifting the other way.
            GameObject highClouds = new GameObject("HighClouds");
            highClouds.transform.SetParent(parent, false);
            for (int i = 0; i < 6; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float fx = side * (16f + (i * 91 + seed) % 18);
                float fz = ((i * 67 + seed) % 120) + (float)i * 14f;
                MakePuffCloud(highClouds.transform,
                    new Vector3(fx, 16f + (i % 4) * 2f, fz),
                    6.5f + (float)rng.NextDouble() * 3.5f, cloudFaint, rng);
            }

            CloudDrift.Create(clouds.transform, highClouds.transform);

            // Start marker: stone post with a gem-pink flag at the spawn.
            Vector3 post = level.Spawn + new Vector3(0f, 0.5f, -3f);
            ArtLib.DecorCube(parent, post + new Vector3(0f, 1f, 0f),
                new Vector3(0.3f, 2f, 0.3f), Quaternion.identity, stone);
            ArtLib.DecorCube(parent, post + new Vector3(0f, 2.2f, 0f),
                new Vector3(0.9f, 0.55f, 0.12f), Quaternion.identity,
                ArtLib.Solid(ArtLib.GemPink, 0.6f));

            // Level name on a small floating sign, readable from the spawn.
            // No rotation: a TextMesh reads correctly from -Z, which is
            // exactly where the spawn camera sits — rotating it 180° used
            // to mirror the level name on screen.
            GameObject signGo = new GameObject("SignText");
            signGo.transform.SetParent(parent, false);
            signGo.transform.localPosition = post + new Vector3(0f, 3.4f, 0f);
            TextMesh sign = signGo.AddComponent<TextMesh>();
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception) { f = null; }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (System.Exception) { f = null; }
            }
            if (f != null)
            {
                sign.font = f;
                MeshRenderer signRenderer = signGo.GetComponent<MeshRenderer>();
                if (signRenderer != null) signRenderer.sharedMaterial = f.material;
            }
            sign.text = level.Name;
            sign.fontSize = 48;
            sign.characterSize = 0.22f;
            sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            sign.color = new Color(1f, 0.97f, 0.9f);
            // Diegetic in play, visual noise on the menu: fade while the
            // main menu shows over the boot world (WorldSign).
            signGo.AddComponent<WorldSign>();
        }

        /// A puffy cloud built from overlapping spheres — soft cumulus
        /// contours instead of boxy slabs. Deterministic per seed.
        static void MakePuffCloud(Transform parent, Vector3 position,
            float width, Material mat, System.Random rng)
        {
            GameObject cloud = new GameObject("Cloud");
            cloud.transform.SetParent(parent, false);
            cloud.transform.localPosition = position;
            cloud.transform.localRotation = Quaternion.Euler(0f,
                (float)rng.NextDouble() * 40f - 20f, 0f);

            // Flat base: one wide, squashed sphere.
            ArtLib.DecorSphere(cloud.transform, Vector3.zero,
                new Vector3(width, width * 0.28f, width * 0.55f), mat);

            // Billowy tops: 4-6 spheres of varying size climbing slightly.
            int puffs = 4 + (int)(rng.NextDouble() * 2.99f);
            for (int p = 0; p < puffs; p++)
            {
                float r = width * (0.16f + (float)rng.NextDouble() * 0.14f);
                float px = ((float)rng.NextDouble() * 2f - 1f) * width * 0.32f;
                float py = width * 0.12f + (float)rng.NextDouble() * width * 0.1f;
                float pz = ((float)rng.NextDouble() * 2f - 1f) * width * 0.18f;
                ArtLib.DecorSphere(cloud.transform, new Vector3(px, py, pz),
                    new Vector3(r * 2f, r * 1.7f, r * 2f), mat);
            }
        }

        /// A grass-topped dirt island with a single full-size box collider.
        static GameObject Platform(Transform parent, Vector3 center, Vector3 size,
            Material top, Material bottom)
        {
            GameObject go = new GameObject("Platform");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = size;

            float slab = Mathf.Min(0.3f, size.y);
            ArtLib.DecorCube(go.transform,
                new Vector3(0f, size.y * 0.5f - slab * 0.5f, 0f),
                new Vector3(size.x, slab, size.z), Quaternion.identity, top);
            ArtLib.DecorCube(go.transform,
                new Vector3(0f, -slab * 0.5f, 0f),
                new Vector3(size.x, size.y - slab, size.z), Quaternion.identity, bottom);
            return go;
        }

        static void Mover(Transform parent, Vector3 center, Vector3 size,
            Vector3 offset, float period, Material top, Material bottom)
        {
            GameObject go = new GameObject("MovingPlatform");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = size;

            ArtLib.DecorCube(go.transform,
                new Vector3(0f, size.y * 0.5f - 0.15f, 0f),
                new Vector3(size.x, 0.3f, size.z), Quaternion.identity, top);
            ArtLib.DecorCube(go.transform,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(size.x, size.y - 0.3f, size.z), Quaternion.identity, bottom);

            // Direction chevrons: two gold strips flush into the top slab,
            // pointing along the travel axis. A mover used to be a plain
            // dirt-and-orange box, identical in shape to a static platform
            // — direction was only ever inferred by watching it move.
            // Gold trim says "this one goes somewhere" at a glance.
            Vector3 dir = offset.normalized;
            bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
            float chevronInset = 0.25f; // margin from the slab's edges
            float stripLen = alongX
                ? 0.5f : size.z * 0.5f - chevronInset;
            float stripWide = alongX
                ? size.z * 0.5f - chevronInset : 0.5f;
            Material trim = ArtLib.Solid(ArtLib.Gold, 0.3f);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pos;
                if (alongX)
                    pos = new Vector3(s * (size.x * 0.5f - 0.5f),
                        size.y * 0.5f + 0.02f, 0f);
                else
                    pos = new Vector3(0f, size.y * 0.5f + 0.02f,
                        s * (size.z * 0.5f - 0.5f));
                ArtLib.DecorCube(go.transform, pos,
                    alongX
                        ? new Vector3(0.25f, 0.04f, stripWide)
                        : new Vector3(stripWide, 0.04f, 0.25f),
                    Quaternion.identity, trim);
            }

            MovingPlatform mover = go.AddComponent<MovingPlatform>();
            mover.moveOffset = offset;
            mover.period = period;
        }
    }
}
