using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// The Sky Garden's signature moment: on victory, every sleepy bud on
    /// the course blooms into a flower, sweeping outward from the portal in
    /// a wave. Pure celebration — one component per bud, one static call.
    public class BloomFlower : MonoBehaviour
    {
        float delay;
        float elapsed;
        Material mat;
        Color target;

        /// Finds every bud in the world and wakes them in a wave outward
        /// from the portal.
        public static void Trigger(Transform world, Vector3 portalPosition)
        {
            if (world == null) return;
            List<Transform> buds = new List<Transform>();
            Collect(world, buds);
            foreach (Transform bud in buds)
            {
                BloomFlower bloom = bud.gameObject.AddComponent<BloomFlower>();
                bloom.delay = Vector3.Distance(bud.position, portalPosition) * 0.06f;
                bloom.mat = bud.GetComponent<MeshRenderer>().material;
                bloom.target = ArtLib.Pastels[Random.Range(0, ArtLib.Pastels.Length)];
            }
        }

        static void Collect(Transform root, List<Transform> buds)
        {
            if (root.name == "Bud") { buds.Add(root); return; }
            foreach (Transform child in root) Collect(child, buds);
        }

        void Update()
        {
            if (delay > 0f)
            {
                delay -= Time.deltaTime;
                return;
            }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.7f);
            float pop = Mathf.Sin(t * Mathf.PI) * 0.35f; // a little bounce
            transform.localScale = Vector3.one *
                Mathf.Lerp(0.22f, 0.55f, t) * (1f + pop);
            mat.color = Color.Lerp(ArtLib.Bud, target, t);
            if (t >= 1f) enabled = false;
        }
    }
}
