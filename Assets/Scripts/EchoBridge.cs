using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Per-level registry of echo bridges, grouped by bell index. A ringing
    /// bell extends the "solid until" time of every bridge in its group;
    /// the bridges fade out when the last echo expires.
    public class BellRig : MonoBehaviour
    {
        static BellRig instance;

        readonly Dictionary<int, List<EchoBridge>> groups =
            new Dictionary<int, List<EchoBridge>>();
        readonly Dictionary<int, float> solidUntil =
            new Dictionary<int, float>();

        /// Resolved by scene lookup, never cached in a static: worlds are
        /// rebuilt constantly, and a static would go stale on the old one.
        public static BellRig Instance
        {
            get { return UnityEngine.Object.FindFirstObjectByType<BellRig>(); }
        }

        public static void Create(Transform parent)
        {
            if (Instance != null) return;
            GameObject go = new GameObject("BellRig");
            go.transform.SetParent(parent, false);
            go.AddComponent<BellRig>();
        }

        public void Register(EchoBridge bridge)
        {
            if (!groups.TryGetValue(bridge.BellIndex, out List<EchoBridge> list))
            {
                list = new List<EchoBridge>();
                groups[bridge.BellIndex] = list;
            }
            list.Add(bridge);
        }

        /// A bell was rung: its bridges become solid until the echo ends.
        public void Ring(int bellIndex, float toneSeconds)
        {
            solidUntil[bellIndex] = Time.time + toneSeconds;
        }

        /// True while any bell linked to this bridge's group is echoing.
        public bool IsSolid(int bellIndex)
        {
            return solidUntil.TryGetValue(bellIndex, out float until) &&
                Time.time < until;
        }
    }

    /// A hidden bridge that exists only while its bell's echo rings.
    /// Fades in with its collider enabled; fades out and disables it after,
    /// so an unechoed bridge can never be stood on.
    public class EchoBridge : MonoBehaviour
    {
        public int BellIndex;

        Material mat;
        Collider col;
        bool solid;

        public static EchoBridge Create(Transform parent, EchoBridgeSpec spec,
            Material material)
        {
            GameObject go = new GameObject("EchoBridge");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = spec.Center;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = spec.Size;
            col.enabled = false; // not solid until rung

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            // The builder passes a fresh URP/Lit material per bridge (each
            // fades on its own clock); fade it to a whisper of a hint even
            // while off. This used to build a Built-in "Standard" material
            // instead — broken under URP, and stripped from builds, where
            // Shader.Find returned null and the level crashed on load.
            Material mat = material;
            ArtLib.SetFade(mat, 0.06f);
            renderer.sharedMaterial = mat;

            EchoBridge bridge = go.AddComponent<EchoBridge>();
            bridge.BellIndex = spec.BellIndex;
            bridge.mat = mat;
            bridge.col = col;
            return bridge;
        }

        void Start()
        {
            if (BellRig.Instance != null)
                BellRig.Instance.Register(this);
        }

        void Update()
        {
            bool shouldBeSolid = BellRig.Instance != null &&
                BellRig.Instance.IsSolid(BellIndex);
            if (shouldBeSolid == solid) return;

            solid = shouldBeSolid;
            col.enabled = solid;
            Color c = mat.color;
            c.a = solid ? 0.8f : 0.06f;
            mat.color = c;

            // The bridge answers the bell: rising chimes as it takes shape,
            // a softer falling answer as the echo lets it go.
            AudioManager.Instance.PlayBridge(solid);
        }
    }
}
