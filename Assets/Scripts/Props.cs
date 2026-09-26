using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Build-time decorations for platform tops: grass tufts, flowers, rocks
    /// and trees. Everything is deterministic per seed (System.Random — never
    /// UnityEngine.Random) and purely static: no update loop. All props are
    /// collider-free children of the platform transform. (Some flowers carry
    /// one dormant FlowerPoke component — they only animate when Pip lands
    /// on them.)
    public static class Props
    {
        // Cached materials — created once on the first DressPlatform call.
        static Material grassMatA;
        static Material grassMatB;
        static Material grassMatC;
        static Material stemMat;
        static Material[] pastelMats;

        // Fixed pastel palette for flower heads: pink, yellow, light blue,
        // white, lavender.
        // Flower-head pastels — the shared ArtLib.Pastels array.
        static readonly Color[] PastelPalette = ArtLib.Pastels;

        const float CenterClear = 1.2f;   // gameplay strip kept free of props
        const float EdgeMargin = 0.5f;    // keep props off the platform edges
        const float PanelSoftEdge = 6f;   // anti-alias band for UI panels (px)

        /// Places decorations on one platform. Positions are local: the
        /// platform centre is the origin and its top surface sits at
        /// y = platformSize.y * 0.5. Same seed always yields the same layout.
        /// Spinner arm reach plus canopy margin: props keep this distance
        /// from any guardian, so spinning arms never sweep through trees.
        const float SpinnerClearRadius = 5.2f;

        /// Decorates a platform. keepClearZones are local-xz circles (guardian
        /// positions) that props must stay clear of.
        public static void DressPlatform(Transform platformTransform, Vector3 platformSize,
            int seed, Material trunkMat, Material leafMat, Material rockMat,
            IList<Vector2> keepClearZones = null)
        {
            if (platformTransform == null) return;

            EnsureMaterials();

            System.Random rng = new System.Random(seed);
            float topY = platformSize.y * 0.5f;
            float halfX = platformSize.x * 0.5f - EdgeMargin;
            float halfZ = platformSize.z * 0.5f - EdgeMargin;

            int count = DecideCount(rng, platformSize);
            // Placed spots so far: later props bias toward these, creating
            // natural patches rather than uniform scatter. Props grow in
            // clusters the way real vegetation does.
            var placed = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                Vector3 spot;
                bool placedNearExisting = false;
                // After the first prop, 60% of props try to cluster near
                // a previously placed one (within ~1.5 units), so tufts
                // and flowers read as patches instead of confetti.
                if (placed.Count > 0 && rng.NextDouble() < 0.6)
                {
                    Vector2 anchor = placed[rng.Next(placed.Count)];
                    float sx = ((float)rng.NextDouble() * 2f - 1f) * 1.5f;
                    float sz = ((float)rng.NextDouble() * 2f - 1f) * 1.5f;
                    Vector2 candidate = anchor + new Vector2(sx, sz);
                    // Validate the clustered spot against the same rules.
                    if (Mathf.Abs(candidate.x) < halfX &&
                        Mathf.Abs(candidate.y) < halfZ &&
                        !InKeepClearZone(candidate.x, candidate.y, keepClearZones) &&
                        !(Mathf.Abs(candidate.x) < CenterClear &&
                          Mathf.Abs(candidate.y) < CenterClear))
                    {
                        spot = new Vector3(candidate.x, 0f, candidate.y);
                        PlaceProp(rng, platformTransform, topY, spot, rockMat);
                        placed.Add(candidate);
                        placedNearExisting = true;
                    }
                }
                if (!placedNearExisting)
                {
                    if (!TryPickSpot(rng, halfX, halfZ, keepClearZones, out spot)) continue;
                    PlaceProp(rng, platformTransform, topY, spot, rockMat);
                    placed.Add(new Vector2(spot.x, spot.z));
                }
            }

            // At most one tree, and only on big platforms (~40% chance).
            float minSide = Mathf.Min(platformSize.x, platformSize.z);
            if (minSide >= 8f && rng.NextDouble() < 0.4)
            {
                Vector3 spot;
                if (TryPickSpot(rng, halfX, halfZ, keepClearZones, out spot))
                    Tree(rng, platformTransform, topY, spot, trunkMat, leafMat);
            }
        }

        /// Square RGBA32 texture of a rounded rectangle: fill colour inside,
        /// transparent outside, with a soft 6-pixel anti-aliased edge. Used
        /// by UIManager as the 9-sliced button sprite.
        public static Texture2D PaintRoundedPanel(int size, int cornerRadius, Color fill)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = cornerRadius;
            if (radius < 1f) radius = 1f;
            float half = size * 0.5f;
            if (radius > half) radius = half;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = RoundedAlpha(x, y, half, radius);
                    Color c = fill;
                    c.a = fill.a * a;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ---------- internals ----------

        /// 0-1 decorations on small platforms, 3-6 on large ones, and an
        /// area-scaled amount in between.
        static int DecideCount(System.Random rng, Vector3 size)
        {
            float minSide = Mathf.Min(size.x, size.z);
            if (minSide < 4f) return rng.Next(2);
            if (minSide >= 10f) return 6 + rng.Next(4); // large arenas feel furnished
            if (minSide >= 8f) return 4 + rng.Next(4);
            float area = size.x * size.z;
            float t = Mathf.Clamp01((area - 12f) / 44f);
            return 1 + (int)(t * 3.99f);
        }

        /// Random spot inside the footprint minus the edge margin that also
        /// stays clear of the centre gameplay strip. Returns false when the
        /// platform is too small to host anything outside the strip.
        static bool TryPickSpot(System.Random rng, float halfX, float halfZ,
            IList<Vector2> keepClearZones, out Vector3 spot)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * halfX;
                float z = ((float)rng.NextDouble() * 2f - 1f) * halfZ;
                if (Mathf.Abs(x) < CenterClear && Mathf.Abs(z) < CenterClear) continue;
                if (InKeepClearZone(x, z, keepClearZones)) continue;
                spot = new Vector3(x, 0f, z);
                return true;
            }

            // Deterministic fallback: the inset corner farthest from centre.
            // Still respects the zones; if the corner is inside one, the
            // platform simply goes without this prop.
            if (halfX >= CenterClear || halfZ >= CenterClear)
            {
                Vector3 corner = new Vector3(halfX, 0f, halfZ);
                if (!InKeepClearZone(corner.x, corner.z, keepClearZones))
                {
                    spot = corner;
                    return true;
                }
            }
            spot = Vector3.zero;
            return false;
        }

        /// True when a local-xz point sits inside any guardian keep-clear
        /// circle (arm reach + canopy margin).
        static bool InKeepClearZone(float x, float z, IList<Vector2> zones)
        {
            if (zones == null) return false;
            for (int i = 0; i < zones.Count; i++)
            {
                if ((new Vector2(x, z) - zones[i]).sqrMagnitude <
                    SpinnerClearRadius * SpinnerClearRadius)
                    return true;
            }
            return false;
        }

        static void PlaceProp(System.Random rng, Transform parent, float topY,
            Vector3 spot, Material rockMat)
        {
            double roll = rng.NextDouble();
            if (roll < 0.45) GrassTuft(rng, parent, topY, spot);
            else if (roll < 0.75) Flower(rng, parent, topY, spot);
            else Rock(rng, parent, topY, spot, rockMat);
        }

        /// 2-3 thin, slightly tilted blade cubes in 2-3 cached green shades.
        static void GrassTuft(System.Random rng, Transform parent, float topY, Vector3 spot)
        {
            int blades = 2 + rng.Next(2);
            for (int i = 0; i < blades; i++)
            {
                Material mat = grassMatA;
                int shade = rng.Next(3);
                if (shade == 1) mat = grassMatB;
                else if (shade == 2) mat = grassMatC;

                float ox = ((float)rng.NextDouble() * 2f - 1f) * 0.12f;
                float oz = ((float)rng.NextDouble() * 2f - 1f) * 0.12f;
                float tilt = (float)rng.NextDouble() * 14f - 7f;
                float yaw = (float)rng.NextDouble() * 360f;

                Vector3 pos = new Vector3(spot.x + ox, topY + 0.11f, spot.z + oz);
                Quaternion rot = Quaternion.Euler(tilt, yaw, tilt * 0.8f);
                ArtLib.DecorCube(parent, pos, new Vector3(0.06f, 0.25f, 0.06f), rot, mat);
            }
        }

        /// Thin cylinder stem with a small pastel sphere head on top,
        /// grouped under one root so a poke can bounce the whole flower.
        /// Some flowers also become poke reactors — the per-level budget
        /// lives in FlowerPoke, so selection stays deterministic: the same
        /// build order always registers the same flowers.
        static void Flower(System.Random rng, Transform parent, float topY,
            Vector3 spot)
        {
            Material petal = pastelMats[rng.Next(pastelMats.Length)];

            GameObject root = new GameObject("Flower");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(spot.x, topY, spot.z);

            // Cylinder primitive is 2 units tall: scale y 0.15 = 0.3 stem.
            DecorPrimitive(PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.15f, 0f),
                new Vector3(0.04f, 0.15f, 0.04f), Quaternion.identity, stemMat);

            DecorPrimitive(PrimitiveType.Sphere, root.transform,
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.12f, 0.12f, 0.12f), Quaternion.identity, petal);

            root.AddComponent<FlowerPoke>();
        }

        /// Builds one instant flower at a world position — Gloomfang's
        /// raindrop bloom uses it. Returns the root at zero scale so the
        /// caller can spring it up to full size.
        public static Transform SproutFlower(Transform parent,
            Vector3 basePosition)
        {
            EnsureMaterials();
            GameObject root = new GameObject("BloomedFlower");
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;

            // Same proportions as the dressed flowers: 0.3 stem, 0.12 head.
            DecorPrimitive(PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.15f, 0f),
                new Vector3(0.04f, 0.15f, 0.04f), Quaternion.identity, stemMat);
            Material petal =
                pastelMats[StableHash(basePosition) % pastelMats.Length];
            DecorPrimitive(PrimitiveType.Sphere, root.transform,
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.12f, 0.12f, 0.12f), Quaternion.identity, petal);
            return root.transform;
        }

        /// Stable integer hash of a world position: the same spot always
        /// hashes the same, so a flower's chime note never changes between
        /// visits to the same level.
        public static int StableHash(Vector3 v)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + Mathf.RoundToInt(v.x * 97f);
                h = h * 31 + Mathf.RoundToInt(v.y * 97f);
                h = h * 31 + Mathf.RoundToInt(v.z * 97f);
                return h < 0 ? -h : h;
            }
        }

        /// Unevenly scaled, randomly rotated rock sunk partway into the turf.
        /// Carries a small collider so Pip can't walk through it — rocks
        /// are solid objects.
        static void Rock(System.Random rng, Transform parent, float topY,
            Vector3 spot, Material rockMat)
        {
            float w = 0.5f * (0.8f + (float)rng.NextDouble() * 0.5f);
            float h = 0.35f * (0.8f + (float)rng.NextDouble() * 0.5f);
            float d = 0.45f * (0.8f + (float)rng.NextDouble() * 0.5f);
            float tiltX = (float)rng.NextDouble() * 30f - 15f;
            float yaw = (float)rng.NextDouble() * 360f;
            float tiltZ = (float)rng.NextDouble() * 30f - 15f;

            // Sink ~30% of the height below the surface.
            float y = topY + h * 0.35f;
            Quaternion rot = Quaternion.Euler(tiltX, yaw, tiltZ);
            var rock = ArtLib.DecorCube(parent,
                new Vector3(spot.x, y, spot.z),
                new Vector3(w, h, d), rot, rockMat);
            rock.name = "Rock";
            // Solid: a small box collider so Pip bumps into the rock.
            // Smaller than the visual mesh (70%) so it doesn't catch on
            // the corners, and keeps Pip from getting stuck on a tilt.
            var col = rock.AddComponent<BoxCollider>();
            col.size = new Vector3(w * 0.7f, h, d * 0.7f);
        }

        /// Cylinder trunk plus 2-3 stacked spheres for the canopy; the whole
        /// tree stands roughly 3 units tall. The trunk carries a capsule
        /// collider so Pip can't walk through the tree — the canopy stays
        /// pass-through so Pip can jump over without hitting leaves.
        static void Tree(System.Random rng, Transform parent, float topY,
            Vector3 spot, Material trunkMat, Material leafMat)
        {
            // Cylinder primitive is 2 units tall: scale y 0.8 = 1.6 trunk.
            var trunk = DecorPrimitive(PrimitiveType.Cylinder, parent,
                new Vector3(spot.x, topY + 0.8f, spot.z),
                new Vector3(0.35f, 0.8f, 0.35f), Quaternion.identity, trunkMat);
            // Solid trunk: a capsule collider matching the visible cylinder.
            // The canopy spheres above stay pass-through (no collider) so
            // Pip can jump over the tree without hitting leaves.
            trunk.AddComponent<CapsuleCollider>();

            float yaw = (float)rng.NextDouble() * 360f;
            float dx = ((float)rng.NextDouble() * 2f - 1f) * 0.12f;
            float dz = ((float)rng.NextDouble() * 2f - 1f) * 0.12f;

            DecorPrimitive(PrimitiveType.Sphere, parent,
                new Vector3(spot.x + dx, topY + 1.85f, spot.z + dz),
                new Vector3(1.35f, 1.3f, 1.35f), Quaternion.Euler(0f, yaw, 0f), leafMat);
            DecorPrimitive(PrimitiveType.Sphere, parent,
                new Vector3(spot.x - dx, topY + 2.45f, spot.z - dz),
                new Vector3(0.95f, 0.9f, 0.95f), Quaternion.Euler(0f, yaw * 0.5f, 0f), leafMat);
            if (rng.Next(2) == 0)
            {
                DecorPrimitive(PrimitiveType.Sphere, parent,
                    new Vector3(spot.x + dz, topY + 2.95f, spot.z - dx),
                    new Vector3(0.7f, 0.65f, 0.7f),
                    Quaternion.Euler(0f, yaw + 45f, 0f), leafMat);
            }
        }

        /// Collider-free primitive child, like ArtLib.DecorCube but for any
        /// primitive type (cylinders, spheres).
        static GameObject DecorPrimitive(PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);

            // Destroy the collider by its concrete type: naming them here
            // keeps the classes alive under IL2CPP stripping. Without this,
            // CreatePrimitive(Sphere) fails on device ("class 'SphereCollider'
            // doesn't exist") — breaking every flower, tree and Pip's eyes.
            Collider col = null;
            switch (type)
            {
                case PrimitiveType.Sphere:
                    col = go.GetComponent<SphereCollider>(); break;
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    col = go.GetComponent<CapsuleCollider>(); break;
                case PrimitiveType.Cube:
                case PrimitiveType.Plane:
                    col = go.GetComponent<BoxCollider>(); break;
            }
            if (col != null) Object.Destroy(col);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = rotation;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// Creates every cached material exactly once (null-checked first).
        static void EnsureMaterials()
        {
            if (grassMatA != null) return;

            Color g = ArtLib.Grass;
            grassMatA = ArtLib.Solid(g, 0f);
            grassMatB = ArtLib.Solid(g * 0.72f, 0f);
            grassMatC = ArtLib.Solid(new Color(g.r + 0.10f, g.g * 1.05f, g.b * 0.75f), 0f);
            stemMat = ArtLib.Solid(g * 0.9f, 0f);

            pastelMats = new Material[PastelPalette.Length];
            for (int i = 0; i < PastelPalette.Length; i++)
                pastelMats[i] = ArtLib.Solid(PastelPalette[i], 0f);
        }

        /// Coverage of the rounded-rectangle interior at one pixel: 1 fully
        /// inside, 0 outside, fading across the 6-pixel soft edge band.
        static float RoundedAlpha(int px, int py, float half, float radius)
        {
            float x = Mathf.Abs(px + 0.5f - half);
            float y = Mathf.Abs(py + 0.5f - half);
            float qx = x - half + radius;
            float qy = y - half + radius;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            float dist = Mathf.Sqrt(ox * ox + oy * oy)
                + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return Mathf.Clamp01(0.5f - dist / PanelSoftEdge);
        }
    }
}
