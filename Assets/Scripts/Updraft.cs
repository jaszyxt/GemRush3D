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

            Material windMat = ArtLib.Solid(new Color(0.65f, 0.92f, 1f), 0f);
            ArtLib.SetFade(windMat, 0.3f);

            Updraft up = col.AddComponent<Updraft>();
            up.strength = strength;
            up.wispHeight = size.y;

            int count = Mathf.Max(3, (int)(size.y * 0.8f));
            up.wisps = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(wisp.GetComponent<BoxCollider>());
                wisp.transform.SetParent(col.transform, false);
                wisp.transform.localScale = new Vector3(
                    size.x * (0.5f + 0.3f * ((i * 7) % 3)),
                    0.12f,
                    size.z * (0.5f + 0.3f * ((i * 5) % 3)));
                wisp.transform.localPosition = new Vector3(
                    (((i * 11) % 5) - 2f) * 0.12f,
                    size.y * (i + 0.5f) / count,
                    (((i * 13) % 5) - 2f) * 0.12f);
                wisp.GetComponent<MeshRenderer>().sharedMaterial = windMat;
                up.wisps[i] = wisp.transform;
            }
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
            player.SetWindLift(strength);
        }
    }
}
