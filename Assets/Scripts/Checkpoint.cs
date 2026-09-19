using UnityEngine;

namespace GemRush
{
    /// A glowing pad; touching it moves the respawn point here and lights it up.
    public class Checkpoint : MonoBehaviour
    {
        GameObject ring;
        Material ringMaterial;
        bool activated;
        string storyLine;

        public static void Create(Transform parent, Vector3 platformTopCenter,
            string storyLine = null)
        {
            GameObject pad = new GameObject("Checkpoint");
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = platformTopCenter;

            // Visual: glowing disc + small beacon post.
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Pad";
            Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.SetParent(pad.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            disc.transform.localScale = new Vector3(2.4f, 0.06f, 2.4f);

            Material mat = ArtLib.Solid(ArtLib.CheckpointOff, 0.8f);

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Beacon";
            Object.Destroy(post.GetComponent<Collider>());
            post.transform.SetParent(pad.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            post.transform.localScale = new Vector3(0.18f, 1.0f, 0.18f);
            post.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Trigger volume.
            BoxCollider trigger = pad.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 2.5f, 3f);
            trigger.center = new Vector3(0f, 1.25f, 0f);

            Checkpoint cp = pad.AddComponent<Checkpoint>();
            cp.ring = disc;
            cp.ringMaterial = mat;
            cp.storyLine = storyLine;
        }

        void OnTriggerEnter(Collider other)
        {
            if (activated) return;
            PlayerController pip = other.GetComponentInParent<PlayerController>();
            if (pip == null) return;
            activated = true;

            Vector3 top = transform.localPosition;
            GameManager.Instance.SetSpawn(top + new Vector3(0f, 1.2f, 0f));
            AudioManager.Instance.PlayCheckpoint();
            // Checkpoint cadence: the pad steps to the dominant chord, so
            // it resolves home to the tonic under the chime.
            AudioManager.Instance.RestartMusicAtDominant();
            Haptics.Light();

            if (ringMaterial != null)
            {
                ringMaterial.color = ArtLib.CheckpointOn;
                ringMaterial.SetColor("_EmissionColor", ArtLib.CheckpointOn * 1.6f);
            }
            if (ring != null)
                ring.transform.localScale = new Vector3(2.8f, 0.08f, 2.8f);

            Fx.Burst(top + new Vector3(0f, 0.5f, 0f), ArtLib.CheckpointOn * 1.5f, 20);

            // Pip's own reaction, layered over the pad's fanfare: a quick
            // joyful twirl. Purely visual on his side.
            pip.Twirl();

            if (!string.IsNullOrEmpty(storyLine) && UIManager.Instance != null)
                UIManager.Instance.ShowStoryToast(storyLine);
        }
    }
}
