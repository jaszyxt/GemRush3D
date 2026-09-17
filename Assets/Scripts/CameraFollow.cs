using UnityEngine;

namespace GemRush
{
    /// Smooth third-person follow camera with a fixed offset.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 7f, -9f);
        public float positionSmooth = 5f;
        public float lookSmooth = 9f;

        Vector3 lookPoint;
        float shakeTimer;
        float shakeMagnitude;

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            lookPoint = target.position;
        }

        /// Brief position jitter, used for death feedback.
        public void Shake(float magnitude, float duration)
        {
            shakeMagnitude = magnitude;
            shakeTimer = duration;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float posBlend = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            float lookBlend = 1f - Mathf.Exp(-lookSmooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position,
                target.position + offset, posBlend);
            lookPoint = Vector3.Lerp(lookPoint, target.position, lookBlend);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float falloff = Mathf.Clamp01(shakeTimer / 0.25f) * shakeMagnitude;
                Vector3 jitter = new Vector3(
                    (Random.value * 2f - 1f) * falloff,
                    (Random.value * 2f - 1f) * falloff,
                    0f);
                transform.position += jitter;
            }

            transform.LookAt(lookPoint);
        }
    }
}
