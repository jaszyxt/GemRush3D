using UnityEngine;

namespace GemRush
{
    /// A floating Sunstone heart that grants one extra life. Placed before
    /// the game's meanest stretches so a wipe never costs the whole level.
    public class HeartPickup : MonoBehaviour
    {
        bool collected;

        public static void Create(Transform parent, Vector3 position)
        {
            GameObject heart = new GameObject("Heart");
            heart.transform.SetParent(parent, false);
            heart.transform.localPosition = position;

            Material mat = ArtLib.Solid(ArtLib.Gold, 0.6f);

            // Two spheres for the lobes, a rotated cube for the point.
            GameObject lobeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(lobeL.GetComponent<SphereCollider>());
            lobeL.transform.SetParent(heart.transform, false);
            lobeL.transform.localPosition = new Vector3(-0.14f, 0.12f, 0f);
            lobeL.transform.localScale = new Vector3(0.34f, 0.34f, 0.26f);
            lobeL.GetComponent<MeshRenderer>().sharedMaterial = mat;

            GameObject lobeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(lobeR.GetComponent<SphereCollider>());
            lobeR.transform.SetParent(heart.transform, false);
            lobeR.transform.localPosition = new Vector3(0.14f, 0.12f, 0f);
            lobeR.transform.localScale = new Vector3(0.34f, 0.34f, 0.26f);
            lobeR.GetComponent<MeshRenderer>().sharedMaterial = mat;

            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(point.GetComponent<BoxCollider>());
            point.transform.SetParent(heart.transform, false);
            point.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            point.transform.localScale = new Vector3(0.34f, 0.34f, 0.24f);
            point.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            point.GetComponent<MeshRenderer>().sharedMaterial = mat;

            BoxCollider trigger = heart.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.2f, 1.2f, 1.2f);

            heart.AddComponent<HeartPickup>();
        }

        void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);
            transform.localPosition += Vector3.up *
                (Mathf.Sin(Time.time * ArtLib.HoverBobRate) * 0.0015f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            collected = true;

            GameManager.Instance.OnHeartCollected();
            AudioManager.Instance.PlayHeart();
            Haptics.Medium(); // a spare life is a real gift
            Fx.Burst(transform.localPosition, ArtLib.Gold * 1.6f, 18);

            // Shrink out politely.
            transform.localScale = Vector3.one * 0.01f;
            enabled = false;
            Destroy(gameObject, 0.1f);
        }
    }
}
