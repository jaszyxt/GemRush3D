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

            // The ring zone covers the whole pedestal: visiting the bell
            // at all IS ringing it — no aiming, no jump timing. (The old
            // 2.2-wide box could be jumped past or landed beside, which
            // read as "the bell just didn't ring".)
            BoxCollider trigger = bell.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.6f, 3.2f, 3.6f);
            trigger.center = new Vector3(0f, 1.6f, 0f);

            Bell b = bell.AddComponent<Bell>();
            b.bellIndex = spec.Index;
            b.toneSeconds = spec.ToneSeconds;
            b.clapper = clapperGo.transform;
        }

        // The teach-me hint: lingering near an un-rung bell shows one
        // quiet toast (cooldown-limited, never while the echo is live) —
        // the same gentle pattern the lantern shrine uses. The mechanic
        // should be guessable, but a first-time player who can't connect
        // bell to bridge deserves the sentence.
        const float HintRadius = 4.5f;
        const float HintCooldown = 10f;
        float hintCooldown;

        void Update()
        {
            HintPulse();
            if (swingTimer <= 0f || clapper == null) return;
            swingTimer -= Time.deltaTime;
            swing += Time.deltaTime * 9f;
            clapper.localRotation = Quaternion.Euler(
                Mathf.Sin(swing * 3f) * 18f, 0f, Mathf.Sin(swing * 2f) * 12f);
        }

        void HintPulse()
        {
            if (hintCooldown > 0f)
            {
                hintCooldown -= Time.deltaTime;
                return;
            }
            GameManager gm = GameManager.Instance;
            PlayerController player = GameBootstrap.Player;
            if (gm == null || gm.State != GameState.Playing || player == null)
                return;
            if (BellRig.Instance != null &&
                BellRig.Instance.IsSolid(bellIndex))
                return; // the echo is doing the teaching right now
            Vector3 flat = player.transform.position - transform.position;
            flat.y = 0f;
            if (flat.magnitude > HintRadius) return;
            hintCooldown = HintCooldown;
            if (UIManager.Instance != null)
                UIManager.Instance.ShowStoryToast(Strings.BellHint);
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
