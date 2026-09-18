using UnityEngine;

namespace GemRush
{
    /// Gloomfang, weather support (probationary). After the befriending he
    /// tags along on Pip's post-story adventures.
    ///
    /// View contract (player-reported, non-negotiable): he is anchored to
    /// the CAMERA's right edge, never between the camera and Pip, and his
    /// body is vapor-translucent — even at the worst angle he cannot block
    /// the view of the player.
    public class Gloomfang : MonoBehaviour
    {
        Transform target;
        Transform bodyVisual;
        Transform pupilL;
        Transform pupilR;
        Transform spark;
        Material sparkMaterial;
        Vector3 velocity;
        float bob;
        bool mirror; // mirror twin: haunts the opposite side of the sky
        float sparkTimer;

        /// Shared body builder: soft overlapping spheres at vapor opacity.
        /// Used by the follower AND the playable Gloomfang so they always
        /// look identical. Eyes and badge stay fully opaque for expression.
        public static void BuildBody(Transform parent, float scale,
            out Transform body, out Transform pupilL, out Transform pupilR)
        {
            body = new GameObject("Body").transform;
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * scale;

            Material vapor = ArtLib.Solid(new Color(0.72f, 0.76f, 0.88f), 0f);
            ArtLib.SetFade(vapor, 0.78f);
            Material vaporLight = ArtLib.Solid(new Color(0.80f, 0.84f, 0.94f), 0f);
            ArtLib.SetFade(vaporLight, 0.78f);
            Material belly = ArtLib.Solid(new Color(0.60f, 0.64f, 0.80f), 0f);
            ArtLib.SetFade(belly, 0.82f);

            ArtLib.DecorSphere(body.transform, Vector3.zero,
                new Vector3(2.6f, 1.9f, 1.9f), vapor);
            ArtLib.DecorSphere(body.transform, new Vector3(-0.55f, 0.5f, 0f),
                new Vector3(1.5f, 1.2f, 1.4f), vaporLight);
            ArtLib.DecorSphere(body.transform, new Vector3(0.65f, 0.45f, 0f),
                new Vector3(1.35f, 1.1f, 1.3f), vaporLight);
            ArtLib.DecorSphere(body.transform, new Vector3(0.05f, -0.4f, 0f),
                new Vector3(2.0f, 1.1f, 1.6f), belly);

            // Eyes and badge stay opaque — the character must stay readable.
            Material eyeWhite = ArtLib.Solid(new Color(0.97f, 0.97f, 1f), 0f);
            Material pupil = ArtLib.Solid(new Color(0.1f, 0.1f, 0.12f), 0f);
            pupilL = BuildEye(body.transform, eyeWhite, pupil,
                new Vector3(-0.38f, 0.14f, 0.72f));
            pupilR = BuildEye(body.transform, eyeWhite, pupil,
                new Vector3(0.38f, 0.14f, 0.72f));

            ArtLib.DecorCube(body.transform, new Vector3(-1.0f, -0.15f, 0.45f),
                new Vector3(0.28f, 0.28f, 0.08f), Quaternion.identity,
                ArtLib.Solid(ArtLib.Gold, 0.4f));
        }

        static Transform BuildEye(Transform body, Material white, Material pupilMat,
            Vector3 position)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(eye.GetComponent<SphereCollider>());
            eye.transform.SetParent(body, false);
            eye.transform.localPosition = position;
            eye.transform.localScale = new Vector3(0.42f, 0.34f, 0.16f);
            eye.GetComponent<MeshRenderer>().sharedMaterial = white;

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(dot.GetComponent<SphereCollider>());
            dot.transform.SetParent(eye.transform, false);
            dot.transform.localPosition = new Vector3(0f, -0.08f, 0.5f);
            dot.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
            dot.GetComponent<MeshRenderer>().sharedMaterial = pupilMat;
            return dot.transform;
        }

        public static void Create(Transform parent, Transform followTarget,
            bool mirror = false)
        {
            GameObject go = new GameObject(mirror ? "MirrorGloomfang" : "Gloomfang");
            go.transform.SetParent(parent, false);
            Gloomfang g = go.AddComponent<Gloomfang>();
            g.target = followTarget;
            g.mirror = mirror;
            BuildBody(go.transform, 1f, out g.bodyVisual, out g.pupilL, out g.pupilR);

            if (mirror)
            {
                // The mirror twin is a reflection: same shape, ghostlier.
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    Color c = r.sharedMaterial.color;
                    c.a = 0.55f;
                    Material m = new Material(r.sharedMaterial);
                    ArtLib.SetFade(m, 0.55f);
                    if (c.r < 0.2f && c.b > 0.05f && c.b < 0.2f) { } // pupils stay solid
                    r.sharedMaterial = m;
                    r.sharedMaterial.color = c;
                }
            }

            // The occasional tiny spark flicker under his belly.
            g.sparkMaterial = ArtLib.Solid(ArtLib.Air, 2f);
            g.spark = ArtLib.DecorCube(g.bodyVisual,
                new Vector3(0.35f, -0.85f, 0.3f),
                new Vector3(0.3f, 0.45f, 0.1f), Quaternion.identity,
                g.sparkMaterial).transform;
            g.spark.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (target == null) return;
            bob += Time.deltaTime;

            // View contract: anchor to the CAMERA's right edge. He lives in
            // the right quarter of the screen by construction and can never
            // end up between the camera and Pip.
            Vector3 right = Vector3.right;
            Vector3 fwd = Vector3.forward;
            Camera cam = Camera.main;
            if (cam != null)
            {
                right = cam.transform.right;
                fwd = cam.transform.forward;
            }
            right.y = 0f; fwd.y = 0f;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            right.Normalize(); fwd.Normalize();

            Vector3 anchor = mirror
                ? new Vector3(-target.position.x, 0f, target.position.z)
                : target.position;
            Vector3 station = anchor
                + right * 3.2f
                + Vector3.up * (1.5f + Mathf.Sin(bob * 1.3f) * 0.25f)
                - fwd * 2.2f;

            // Critically-damped chase: never snaps, never overshoots.
            Vector3 toStation = station - transform.position;
            velocity = Vector3.Lerp(velocity,
                toStation * 2.2f, 1f - Mathf.Exp(-4f * Time.deltaTime));
            transform.position += velocity * Time.deltaTime;

            // Body leans into his own drift.
            if (bodyVisual != null)
                bodyVisual.localRotation = Quaternion.Euler(
                    Mathf.Clamp(-velocity.y * 0.8f, -8f, 8f),
                    0f,
                    Mathf.Clamp(velocity.x * 0.8f, -8f, 8f));

            // Pupils drift toward Pip, because what else is there to look at.
            if (pupilL != null && pupilR != null)
            {
                Vector3 gaze = new Vector3(
                    Mathf.Clamp(velocity.x * 0.01f, -0.06f, 0.06f),
                    Mathf.Clamp(velocity.y * 0.008f, -0.05f, 0.05f), 0f);
                pupilL.localPosition = gaze;
                pupilR.localPosition = gaze;
            }

            // The occasional tiny spark: blink on briefly, then shy away.
            sparkTimer -= Time.deltaTime;
            if (sparkTimer <= 0f)
            {
                if (spark != null && spark.gameObject.activeSelf)
                {
                    spark.gameObject.SetActive(false);
                    sparkTimer = Random.Range(2.5f, 6f);
                }
                else
                {
                    if (spark != null) spark.gameObject.SetActive(true);
                    sparkTimer = Random.Range(0.15f, 0.35f);
                }
            }
        }
    }
}
