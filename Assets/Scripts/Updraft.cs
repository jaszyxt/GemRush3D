using UnityEngine;

namespace GemRush
{
    /// A column of rising wind. While Pip is inside he floats upward at the
    /// column's strength — ride it to high ledges, hop out to glide. Named
    /// Updraft (Unity already owns WindZone).
    public class Updraft : MonoBehaviour
    {
        float strength = 11f;
        Transform[] wisps;
        GameObject tipGem;
        float wispHeight;
        float drift;

        public static void Create(Transform parent, Vector3 baseCenter,
            Vector3 size, float strength)
        {
            GameObject col = new GameObject("Updraft");
            col.transform.SetParent(parent, false);
            col.transform.localPosition = baseCenter;

            BoxCollider volume = col.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = size;
            volume.center = new Vector3(0f, size.y * 0.5f, 0f);

            Material windMat = ArtLib.Solid(ArtLib.Air, 0f);
            ArtLib.SetFade(windMat, 0.3f);

            Updraft up = col.AddComponent<Updraft>();
            up.strength = strength;
            up.wispHeight = size.y;

            // Rim beacon: a glowing cap at the column's top marking where
            // you pop out — the exit reads before you even enter.
            Material rimMat = ArtLib.Solid(ArtLib.Air, 1.4f);
            ArtLib.SetFade(rimMat, 0.5f);
            ArtLib.DecorCube(col.transform,
                new Vector3(0f, size.y + 0.1f, 0f),
                new Vector3(size.x * 1.1f, 0.15f, size.z * 1.1f),
                Quaternion.identity, rimMat);

            int count = Mathf.Max(3, (int)(size.y * 0.8f));
            up.wisps = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(wisp.GetComponent<BoxCollider>());
                wisp.transform.SetParent(col.transform, false);
                // Vertical streaks (tall thin cubes), not horizontal
                // shelves — the geometry itself carries the direction of
                // motion, so "air rising here" reads in a still frame.
                wisp.transform.localScale = new Vector3(
                    0.15f + 0.1f * ((i * 7) % 3),
                    size.y / 4f,
                    0.15f + 0.1f * ((i * 5) % 3));
                wisp.transform.localPosition = new Vector3(
                    (((i * 11) % 5) - 2f) * 0.35f,
                    size.y * (i + 0.5f) / count,
                    (((i * 13) % 5) - 2f) * 0.35f);
                wisp.GetComponent<MeshRenderer>().sharedMaterial = windMat;
                up.wisps[i] = wisp.transform;
            }

            // A bright gem riding the very top of the column: the reward
            // mark that says "this goes somewhere worth going".
            Material tipMat = ArtLib.Solid(ArtLib.GemPink, 1.6f);
            up.tipGem = ArtLib.DecorSphere(col.transform,
                new Vector3(0f, size.y + 0.6f, 0f),
                new Vector3(0.4f, 0.4f, 0.4f), tipMat);
        }

        void Update()
        {
            // Wisps drift upward and wrap, so the column reads as moving air.
            drift += Time.deltaTime;
            if (wisps == null) return;
            for (int i = 0; i < wisps.Length; i++)
            {
                Vector3 lp = wisps[i].localPosition;
                lp.y += Time.deltaTime * (1.2f + 0.3f * (i % 3));
                if (lp.y > wispHeight) lp.y -= wispHeight;
                wisps[i].localPosition = lp;
            }
        }

        void OnTriggerStay(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            // A wind column is a fountain: constant full lift while inside,
            // so Pip pops out of the top with upward momentum and arcs
            // ballistically onto the ledge the level points him at. Descend
            // by steering out the side — never by sinking through the wind.
            player.SetWindLift(strength);
            // The wind bed is a heartbeat: each frame inside re-asserts the
            // pulse, and it decays back to the mood bed when Pip hops out.
            AudioManager.Instance.PulseWind();
        }
    }
}
