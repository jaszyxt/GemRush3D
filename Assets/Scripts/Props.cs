using UnityEngine;

namespace GemRush
{
    /// Build-time decorations for platform tops: grass tufts, flowers, rocks
    /// and trees. Everything is deterministic per seed (System.Random — never
    /// UnityEngine.Random) and purely static: no MonoBehaviour, no update loop.
    /// All props are collider-free children of the platform transform.
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
        static readonly Color[] PastelPalette = new Color[]
        {
            new Color(1.00f, 0.70f, 0.80f),
            new Color(1.00f, 0.92f, 0.55f),
            new Color(0.65f, 0.82f, 1.00f),
            new Color(0.98f, 0.98f, 0.94f),
            new Color(0.80f, 0.70f, 0.95f)
        };

        const float CenterClear = 1.2f;   // gameplay strip kept free of props
        const float EdgeMargin = 0.5f;    // keep props off the platform edges
        const float PanelSoftEdge = 6f;   // anti-alias band for UI panels (px)

        /// Places decorations on one platform. Positions are local: the
        /// platform centre is the origin and its top surface sits at
        /// y = platformSize.y * 0.5. Same seed always yields the same layout.
        public static void DressPlatform(Transform platformTransform, Vector3 platformSize,
            int seed, Material trunkMat, Material leafMat, Material rockMat)
        {
            if (platformTransform == null) return;

            EnsureMaterials();

            System.Random rng = new System.Random(seed);
            float topY = platformSize.y * 0.5f;
            float halfX = platformSize.x * 0.5f - EdgeMargin;
            float halfZ = platformSize.z * 0.5f - EdgeMargin;

            int count = DecideCount(rng, platformSize);
            for (int i = 0; i < count; i++)
            {
                Vector3 spot;
                if (!TryPickSpot(rng, halfX, halfZ, out spot)) continue;
                PlaceProp(rng, platformTransform, topY, spot, rockMat);
            }

            // At most one tree, and only on big platforms (~40% chance).
            float minSide = Mathf.Min(platformSize.x, platformSize.z);
            if (minSide >= 8f && rng.NextDouble() < 0.4)
            {
                Vector3 spot;
                if (TryPickSpot(rng, halfX, halfZ, out spot))
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
            if (minSide >= 8f) return 3 + rng.Next(4);
            float area = size.x * size.z;
            float t = Mathf.Clamp01((area - 12f) / 44f);
            return 1 + (int)(t * 3.99f);
        }

        /// Random spot inside the footprint minus the edge margin that also
        /// stays clear of the centre gameplay strip. Returns false when the
        /// platform is too small to host anything outside the strip.
        static bool TryPickSpot(System.Random rng, float halfX, float halfZ, out Vector3 spot)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * halfX;
                float z = ((float)rng.NextDouble() * 2f - 1f) * halfZ;
                if (Mathf.Abs(x) < CenterClear && Mathf.Abs(z) < CenterClear) continue;
                spot = new Vector3(x, 0f, z);
                return true;
            }

            // Deterministic fallback: the inset corner farthest from centre.
            if (halfX >= CenterClear || halfZ >= CenterClear)
            {
                spot = new Vector3(halfX, 0f, halfZ);
                return true;
            }
            spot = Vector3.zero;
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

        /// Thin cylinder stem with a small pastel sphere head on top.
        static void Flower(System.Random rng, Transform parent, float topY, Vector3 spot)
        {
            Material petal = pastelMats[rng.Next(pastelMats.Length)];

            // Cylinder primitive is 2 units tall: scale y 0.15 = 0.3 stem.
            DecorPrimitive(PrimitiveType.Cylinder, parent,
                new Vector3(spot.x, topY + 0.15f, spot.z),
                new Vector3(0.04f, 0.15f, 0.04f), Quaternion.identity, stemMat);

            DecorPrimitive(PrimitiveType.Sphere, parent,
                new Vector3(spot.x, topY + 0.34f, spot.z),
                new Vector3(0.12f, 0.12f, 0.12f), Quaternion.identity, petal);
        }

        /// Unevenly scaled, randomly rotated rock sunk partway into the turf.
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
            ArtLib.DecorCube(parent, new Vector3(spot.x, y, spot.z),
                new Vector3(w, h, d), rot, rockMat);
        }

        /// Cylinder trunk plus 2-3 stacked spheres for the canopy; the whole
        /// tree stands roughly 3 units tall.
        static void Tree(System.Random rng, Transform parent, float topY,
            Vector3 spot, Material trunkMat, Material leafMat)
        {
            // Cylinder primitive is 2 units tall: scale y 0.8 = 1.6 trunk.
            DecorPrimitive(PrimitiveType.Cylinder, parent,
                new Vector3(spot.x, topY + 0.8f, spot.z),
                new Vector3(0.35f, 0.8f, 0.35f), Quaternion.identity, trunkMat);

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
