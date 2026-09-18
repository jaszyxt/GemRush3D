using UnityEngine;

namespace GemRush
{
    /// An echo bell: touch it and it rings a long, low tone — and every
    /// echo bridge linked to its index turns solid while the tone sings.
    /// Touch again any time to refresh the echo.
    public class Bell : MonoBehaviour
    {
        public int bellIndex;
        public float toneSeconds;

        Transform clapper;
        float swingTimer;
        float swing;

        public static void Create(Transform parent, BellSpec spec,
            Material goldMat)
        {
            GameObject bell = new GameObject("Bell");
            bell.transform.SetParent(parent, false);
            bell.transform.localPosition = spec.PlatformTop;

            // Post + crossbar to hang the bell from.
            Material stone = ArtLib.Solid(ArtLib.Stone, 0f);
            ArtLib.DecorCube(bell.transform, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.22f, 1.8f, 0.22f), Quaternion.identity, stone);
            ArtLib.DecorCube(bell.transform, new Vector3(0f, 1.8f, 0f),
                new Vector3(1.1f, 0.2f, 0.3f), Quaternion.identity, stone);

            // The bell: a golden cup hanging under the crossbar.
            GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(cup.GetComponent<Collider>());
            cup.transform.SetParent(bell.transform, false);
            cup.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            cup.transform.localScale = new Vector3(0.55f, 0.4f, 0.55f);
            cup.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            // The clapper swings only while the echo is fresh.
            GameObject clapperGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(clapperGo.GetComponent<Collider>());
            clapperGo.transform.SetParent(cup.transform, false);
            clapperGo.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            clapperGo.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            clapperGo.GetComponent<MeshRenderer>().sharedMaterial = stone;

            BoxCollider trigger = bell.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.2f, 2.4f, 2.2f);
            trigger.center = new Vector3(0f, 1.2f, 0f);

            Bell b = bell.AddComponent<Bell>();
            b.bellIndex = spec.Index;
            b.toneSeconds = spec.ToneSeconds;
            b.clapper = clapperGo.transform;
        }

        void Update()
        {
            if (swingTimer <= 0f || clapper == null) return;
            swingTimer -= Time.deltaTime;
            swing += Time.deltaTime * 9f;
            clapper.localRotation = Quaternion.Euler(
                Mathf.Sin(swing * 3f) * 18f, 0f, Mathf.Sin(swing * 2f) * 12f);
        }

        void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;
            if (BellRig.Instance == null) return;
            BellRig.Instance.Ring(bellIndex, toneSeconds);
            AudioManager.Instance.PlayBellTone(bellIndex, toneSeconds);
            Fx.Burst(transform.localPosition + new Vector3(0f, 1.4f, 0f),
                ArtLib.Gold * 1.6f, 22);
            swingTimer = 1.4f;
        }
    }
}
