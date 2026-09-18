using UnityEngine;

namespace GemRush
{
    /// Smooth third-person follow camera. The framing adapts to the screen's
    /// aspect ratio: the tuned offset was designed for a wide phone, so on
    /// narrower screens (tablets, 4:3) the camera dollies back to keep the
    /// same course visibility instead of feeling cramped.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 baseOffset = new Vector3(0f, 7f, -9f);
        public float positionSmooth = 5f;
        public float lookSmooth = 9f;

        /// The aspect the framing was tuned on. Wider than this = phone as
        /// designed (scale 1); narrower = camera scales back, up to +50%.
        const float ReferenceAspect = 2.1f;

        Vector3 offset;
        Vector3 lookPoint;
        float followY; // soft-zone height the camera actually holds
        float shakeTimer;
        float shakeMagnitude;

        void Start()
        {
            RecalculateFraming();
        }

        /// Distance scale from the screen aspect: 1.0 on wide phones, up to
        /// 1.5 on 4:3 tablets.
        void RecalculateFraming()
        {
            float aspect = (float)Screen.width / Screen.height;
            float scale = Mathf.Clamp(ReferenceAspect / aspect, 1f, 1.5f);
            offset = baseOffset * scale;
        }

        public void SnapToTarget()
        {
            RecalculateFraming();
            if (target == null) return;
            transform.position = target.position + offset;
            lookPoint = target.position;
            followY = target.position.y;
        }

        /// Brief position jitter, used for death feedback. Respects the
        /// motion-comfort setting: players who turn shake off keep every
        /// other piece of death feedback (burst, sound, haptic).
        public void Shake(float magnitude, float duration)
        {
            if (!SaveSystem.ShakeOn) return;
            shakeMagnitude = magnitude;
            shakeTimer = duration;
        }

        void LateUpdate()
        {
            RecalculateFraming();
            if (target == null) return;
            float posBlend = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            float lookBlend = 1f - Mathf.Exp(-lookSmooth * Time.deltaTime);

            // Vertical soft zone: the camera holds its height while Pip hops
            // (small target deltas settle slowly) and only chases when he
            // leaves the window — climbs and falls. Riding every jump arc
            // made landing heights hard to read; a held horizon reads true.
            const float YWindow = 2.5f;
            float dy = target.position.y - followY;
            float yBlend = Mathf.Abs(dy) > YWindow ? posBlend : posBlend * 0.25f;
            followY = Mathf.Lerp(followY, target.position.y, yBlend);

            Vector3 goal = target.position + offset;
            goal.y = followY + offset.y;
            transform.position = Vector3.Lerp(transform.position, goal, posBlend);
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
