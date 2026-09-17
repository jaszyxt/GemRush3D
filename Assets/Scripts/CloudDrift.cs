using UnityEngine;

namespace GemRush
{
    /// Gives cloud layers a slow, lazy drift: sideways sway and a gentle bob,
    /// each cloud on its own phase. The high layer drifts the other way, so
    /// the sky has visible parallax against itself.
    public class CloudDrift : MonoBehaviour
    {
        Transform layerA;
        Transform layerB;
        Vector3[] basesA;
        Vector3[] basesB;
        float t;

        public static void Create(Transform layerA, Transform layerB)
        {
            GameObject go = new GameObject("CloudDrift");
            CloudDrift drift = go.AddComponent<CloudDrift>();
            drift.layerA = layerA;
            drift.layerB = layerB;
        }

        void Start()
        {
            basesA = Capture(layerA);
            basesB = Capture(layerB);
        }

        static Vector3[] Capture(Transform layer)
        {
            if (layer == null) return new Vector3[0];
            Vector3[] bases = new Vector3[layer.childCount];
            for (int i = 0; i < layer.childCount; i++)
                bases[i] = layer.GetChild(i).localPosition;
            return bases;
        }

        void Update()
        {
            t += Time.deltaTime;
            Apply(layerA, basesA, t, 0.35f, 1f);
            Apply(layerB, basesB, t, -0.25f, 1.4f);
        }

        static void Apply(Transform layer, Vector3[] bases, float time,
            float swaySpeed, float bobSpeed)
        {
            if (layer == null || bases == null) return;
            for (int i = 0; i < layer.childCount && i < bases.Length; i++)
            {
                float phase = i * 1.7f;
                Vector3 p = bases[i];
                p.x += Mathf.Sin(time * swaySpeed + phase) * 1.4f;
                p.y += Mathf.Sin(time * 0.22f * bobSpeed + phase * 1.3f) * 0.45f;
                layer.GetChild(i).localPosition = p;
            }
        }
    }
}
