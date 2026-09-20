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
            // Rebuild safety: Object.Destroy is deferred, so on a replay
            // the OLD world's rig is still findable this frame — a plain
            // "skip if one exists" guard would settle for that doomed rig,
            // skip building a new one, and the new world would end up
            // with none at all: bells went permanently silent for the
            // rest of the run after a game-over replay. Retire the old
            // rig and always build a fresh one for this world.
            BellRig existing = Instance;
            if (existing != null) Object.Destroy(existing.gameObject);
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
        Renderer slabRenderer;
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

            // The visible slab: a real mesh child (the renderer used to sit
            // on a bare GameObject with no MeshFilter, so bridges rendered
            // nothing at all — the crossing read as an impossible gap).
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(slab.GetComponent<BoxCollider>());
            slab.transform.SetParent(go.transform, false);
            slab.transform.localPosition = Vector3.zero;
            slab.transform.localScale = spec.Size;

            MeshRenderer renderer = slab.GetComponent<MeshRenderer>();
            // The builder passes a fresh URP/Lit material per bridge (each
            // fades on its own clock). The bridge is HONEST about its
            // state: invisible while intangible, bright while solid — the
            // old faint ghost read as "walkable" and dropped players to
            // their death. This used to build a Built-in "Standard"
            // material instead — broken under URP, and stripped from
            // builds, where Shader.Find returned null and the level
            // crashed on load.
            Material mat = material;
            ArtLib.SetFade(mat, 0f);
            renderer.sharedMaterial = mat;
            // Belt and braces: while off, the mesh itself is switched off
            // — no render, no shadow — so the bridge cannot show up
            // through any material, shader or scaling edge case.
            renderer.enabled = false;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            EchoBridge bridge = go.AddComponent<EchoBridge>();
            bridge.BellIndex = spec.BellIndex;
            bridge.mat = mat;
            bridge.col = col;
            bridge.slabRenderer = renderer;
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
            // Honesty rule, enforced at the mesh level: no echo = the
            // bridge is not in the world at all; echo = bright and solid.
            if (slabRenderer != null) slabRenderer.enabled = solid;
            Color c = mat.color;
            c.a = solid ? 0.8f : 0f;
            mat.color = c;

            // The bridge answers the bell: rising chimes as it takes shape,
            // a softer falling answer as the echo lets it go.
            AudioManager.Instance.PlayBridge(solid);
        }
    }
}
