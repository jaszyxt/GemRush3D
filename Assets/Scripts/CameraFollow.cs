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

        // Lens feel, gated by the same comfort setting as shake (FOV motion
        // IS camera motion): a one-shot kick (bounce pads) plus a sustained
        // hold (wind rides), both easing back to the fixed baseline the
        // framing is tuned on.
        const float BaseFov = 60f;
        const float HoldTimeout = 0.15f; // unscaled TTL, refreshed per wind frame
        const float MaxTotalKick = 10f;  // stays inside the backdrop quad's margins
        Camera cam;
        float holdExtra;
        float holdUntil; // unscaled time the hold lapses; 0 = off
        float holdCurrent;
        float kickExtra;
        float kickAge;
        float kickAttack;
        float kickRelease;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam != null) cam.fieldOfView = BaseFov;
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
            // A respawn or level change is a hard cut: any lens motion that
            // was mid-flight belongs to the moment we just left.
            ResetLens();
        }

        /// Returns the lens to its designed baseline, cancelling any kick
        /// or wind hold in flight. The FOV system animates on the unscaled
        /// clock (so it reads through hit-stop), which means it would keep
        /// easing while a level teardown, a respawn or photo mode froze the
        /// world around it — leaving the camera parked at BaseFov + 5 from a
        /// ride that ended, or settling visibly when the follow resumed.
        /// Called on every hard cut, so the next frame starts neutral.
        public void ResetLens()
        {
            kickExtra = 0f;
            kickAge = 0f;
            holdExtra = 0f;
            holdCurrent = 0f;
            holdUntil = 0f;
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) cam.fieldOfView = BaseFov;
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

        /// One-shot FOV punch (bounce-pad launches): fast attack, eased
        /// release. No-op under the motion-comfort setting, like Shake.
        public void FovKick(float extraFov, float attackSeconds, float releaseSeconds)
        {
            if (!SaveSystem.ShakeOn) return;
            kickExtra = extraFov;
            kickAttack = Mathf.Max(0.01f, attackSeconds);
            kickRelease = Mathf.Max(0.01f, releaseSeconds);
            kickAge = 0f;
        }

        /// Sustained FOV while Pip rides wind. Refreshed every frame he is
        /// inside a zone; lapses on its own shortly after the last refresh,
        /// so overlapping zones need no bookkeeping and a zone destroyed
        /// mid-ride can never leave the hold stuck on.
        public void SetFovHold(float extraFov)
        {
            if (!SaveSystem.ShakeOn) return;
            holdExtra = extraFov;
            holdUntil = Time.unscaledTime + HoldTimeout;
        }

        /// Ends a wind hold immediately — the ride is definitively over.
        public void ClearFovHold()
        {
            holdUntil = 0f;
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

            // Kick envelope runs on the unscaled clock (it must read right
            // through a hit-stop), the hold eases like the position blend.
            float extraFov = 0f;
            if (kickAge < kickAttack + kickRelease)
            {
                kickAge += Time.unscaledDeltaTime;
                extraFov = kickAge < kickAttack
                    ? kickExtra * (kickAge / kickAttack)
                    : kickExtra * (1f - (kickAge - kickAttack) / kickRelease);
            }
            holdCurrent = Mathf.Lerp(holdCurrent,
                Time.unscaledTime < holdUntil ? holdExtra : 0f,
                1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
            if (cam != null)
                cam.fieldOfView = BaseFov + Mathf.Min(extraFov + holdCurrent,
                    MaxTotalKick);

            transform.LookAt(lookPoint);
        }
    }
}
