using UnityEngine;

namespace GemRush
{
    /// The playable character: a physics-driven capsule with camera-relative
    /// movement, coyote time, jump buffering, moving-platform carrying,
    /// hazard/fall death and respawn.
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 8f;
        public float airControl = 0.55f;
        public float acceleration = 14f;
        public float jumpVelocity = 9.5f;
        public float coyoteTime = 0.12f;
        public float jumpBuffer = 0.15f;

        Rigidbody rb;
        CapsuleCollider capsule;
        Transform tr;
        Transform bodyVisual;
        Transform pupilL;
        Transform pupilR;
        bool grounded;
        bool wasGroundedLastFrame = true;
        float squash; // 0 = neutral; positive = stretched, negative = squashed
        float squashVel; // spring velocity for the squash overshoot
        bool jumpCutApplied; // variable jump height: cut only once per jump
        float lastGroundedTime = -99f;
        float lastJumpPressedTime = -99f;
        Vector3 platformVelocity;
        float windLift; // set every frame by any Updraft the player is inside
        Vector3 gustPush; // set every frame by any active GustZone
        float gustLift;   // vertical sustain while inside a gust

        /// Bonus-flight mode (playing as Gloomfang): no gravity, hold jump
        /// to rise, gentle sink otherwise, fall deaths replaced by a clamp.
        public bool flyMode;

        public static PlayerController Create(Transform parent, Vector3 position,
            bool fly = false)
        {
            GameObject go = new GameObject("Player");
            go.transform.SetParent(parent);
            go.transform.position = position;
            PlayerController pc = go.AddComponent<PlayerController>();
            pc.flyMode = fly;
            return pc;
        }

        void Awake()
        {
            tr = transform;

            capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.zero;

            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = 1f;
            rb.linearDamping = 0f;
            // Never sleep: a sleeping body generates no trigger callbacks
            // (gems, gusts, hearts) — and a gust must grab Pip even when he
            // stands perfectly still waiting for it.
            rb.sleepThreshold = 0f;
        }

        void Start()
        {
            // Visuals are built in Start so flyMode (set right after
            // AddComponent) can pick the body: capsule Pip or storm cloud.
            if (flyMode)
            {
                rb.useGravity = false;
                rb.linearDamping = 2f;
                BuildGloomfangVisuals();
            }
            else
            {
                BuildVisuals();
            }
        }

        void BuildVisuals()
        {
            Material skin = ArtLib.Solid(ArtLib.PlayerSkin, 0f);
            Material nose = ArtLib.Solid(ArtLib.PlayerNose, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(tr, false);
            body.GetComponent<MeshRenderer>().sharedMaterial = skin;
            bodyVisual = body.transform;

            // Eyes: white spheres with pupils that track the movement direction.
            Material eyeWhite = ArtLib.Solid(new Color(0.97f, 0.97f, 1f), 0f);
            Material pupil = ArtLib.Solid(new Color(0.1f, 0.1f, 0.12f), 0f);
            pupilL = BuildEye(body.transform, eyeWhite, pupil,
                new Vector3(-0.16f, 0.3f, 0.4f));
            pupilR = BuildEye(body.transform, eyeWhite, pupil,
                new Vector3(0.16f, 0.3f, 0.4f));

            ArtLib.DecorCube(tr, new Vector3(0f, 0.35f, 0.5f),
                new Vector3(0.22f, 0.22f, 0.34f), Quaternion.identity, nose);
        }

        /// Gloomfang's playable body: the shared real-cloud look, scaled to
        /// player size, so the follower and the playable storm are twins.
        void BuildGloomfangVisuals()
        {
            Gloomfang.BuildBody(tr, 0.62f, out bodyVisual,
                out pupilL, out pupilR);
        }

        static Transform BuildEye(Transform body, Material white, Material pupilMat,
            Vector3 position)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(body, false);
            eye.transform.localPosition = position;
            eye.transform.localScale = new Vector3(0.15f, 0.17f, 0.1f);
            eye.GetComponent<MeshRenderer>().sharedMaterial = white;

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(dot.GetComponent<Collider>());
            dot.transform.SetParent(eye.transform, false);
            dot.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            dot.transform.localScale = new Vector3(0.55f, 0.6f, 0.5f);
            dot.GetComponent<MeshRenderer>().sharedMaterial = pupilMat;
            return dot.transform;
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            bool playing = gm != null && gm.State == GameState.Playing;

            if (playing && Input.GetButtonDown("Jump"))
            {
                lastJumpPressedTime = Time.time;
                GamepadInput.MarkOther();
            }
            // Gamepad jump rides the same buffered queue as keyboard and
            // touch; hold state feeds variable height / fly mode below.
            if (playing && GamepadInput.JumpPressed)
            {
                lastJumpPressedTime = Time.time;
                GamepadInput.MarkGamepad();
            }

            if (TouchControls.JumpQueued)
            {
                TouchControls.JumpQueued = false;
                if (playing) lastJumpPressedTime = Time.time;
            }

            bool wantJump = Time.time - lastJumpPressedTime <= jumpBuffer;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime &&
                           rb.linearVelocity.y < 4f;
            if (wantJump && canJump)
            {
                lastJumpPressedTime = -99f;
                lastGroundedTime = -99f;
                platformVelocity = Vector3.zero;
                jumpCutApplied = false;
                Vector3 vel = rb.linearVelocity;
                vel.y = jumpVelocity;
                rb.linearVelocity = vel;
                squash = 0.28f;
                AudioManager.Instance.PlayJump();
                Vector3 feet = tr.position + Vector3.down * 0.9f;
                Fx.Burst(feet, new Color(0.9f, 0.9f, 0.9f), 8);
            }

            // Variable jump height (release-to-cut): a short tap makes a
            // short hop. One mild cut per jump, never while wind carries the
            // player, and never in flight (hold-to-rise IS the fly control).
            bool jumpHeld = Input.GetButton("Jump") || TouchControls.JumpHeld ||
                GamepadInput.JumpHeld;
            if (!flyMode && !jumpHeld && !jumpCutApplied && grounded == false &&
                Time.time - lastGroundedTime > coyoteTime &&
                rb.linearVelocity.y > jumpVelocity * 0.35f &&
                gustPush == Vector3.zero && windLift <= 0f)
            {
                jumpCutApplied = true;
                Vector3 vel = rb.linearVelocity;
                vel.y *= 0.5f;
                rb.linearVelocity = vel;
            }

            // gm can be null while a half-torn-down world is still ticking
            // (editor domain reload mid-play) — treat that as kill-line-off.
            if (gm != null &&
                tr.position.y < (gm.CurrentLevelDefinition != null
                    ? gm.CurrentLevelDefinition.KillY : -12f))
            {
                if (flyMode)
                {
                    // No fall deaths in flight: softly bounce off the floor.
                    Vector3 v = rb.linearVelocity;
                    if (rb.position.y < 0.5f) v.y = Mathf.Max(v.y, 1.5f);
                    rb.linearVelocity = v;
                    if (rb.position.y < -1f)
                        TeleportTo(new Vector3(rb.position.x, 0.5f, rb.position.z));
                }
                else gm.OnPlayerDied(true);
            }

            if (flyMode) return; // flight skips squash (no landing to sell)
            UpdateSquash();
        }

        /// Classic platformer juice: stretch while rising, squash on landing,
        /// lean into motion, breathe when idle.
        void UpdateSquash()
        {
            if (bodyVisual == null) return;

            Vector3 localVel = tr.InverseTransformDirection(rb.linearVelocity);
            float speed = localVel.magnitude;

            float target = 0f;
            if (grounded && speed < 0.6f)
                target = Mathf.Sin(Time.time * 2.2f) * 0.045f; // idle breathing
            // Underdamped spring instead of a plain decay: the squash passes
            // slightly past neutral on recovery — classic follow-through, so
            // landings read as bouncy rather than damped.
            squashVel += (-90f * (squash - target) - 12f * squashVel)
                * Time.deltaTime;
            squash += squashVel * Time.deltaTime;
            if (!grounded && rb.linearVelocity.y > 2f && squash < 0.25f)
            {
                squash = Mathf.Lerp(squash, 0.25f, 0.5f);
                squashVel = 0f;
            }

            bodyVisual.localScale = new Vector3(
                1f - squash * 0.6f, 1f + squash, 1f - squash * 0.6f);

            // Lean into horizontal motion.
            float pitch = Mathf.Clamp(localVel.z * 1.6f, -12f, 12f);
            float roll = Mathf.Clamp(-localVel.x * 1.6f, -12f, 12f);
            bodyVisual.localRotation = Quaternion.Euler(pitch, 0f, roll);

            // Pupils glance toward the movement direction.
            if (pupilL != null && pupilR != null)
            {
                float px = Mathf.Clamp(localVel.x * 0.025f, -0.05f, 0.05f);
                float py = Mathf.Clamp(rb.linearVelocity.y * 0.012f, -0.04f, 0.04f);
                Vector3 gaze = new Vector3(px, py, 0f);
                pupilL.localPosition = gaze;
                pupilR.localPosition = gaze;
            }
        }

        void OnLand(float impactSpeed)
        {
            squash = Mathf.Clamp(-impactSpeed * 0.06f, -0.3f, 0f);
            AudioManager.Instance.PlayLand(impactSpeed);
            if (impactSpeed > 5f)
                Fx.Burst(tr.position + Vector3.down * 0.9f,
                    new Color(0.9f, 0.9f, 0.9f), 10);
        }

        void FixedUpdate()
        {
            grounded = CheckGrounded();
            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGroundedLastFrame)
                {
                    OnLand(Mathf.Abs(rb.linearVelocity.y));
                    platformVelocity = Vector3.zero;
                }
            }
            wasGroundedLastFrame = grounded;

            GameManager gm = GameManager.Instance;
            bool playing = gm != null && gm.State == GameState.Playing;

            Vector2 input = Vector2.zero;
            if (playing)
            {
                input.x = Input.GetAxisRaw("Horizontal");
                input.y = Input.GetAxisRaw("Vertical");
                if (input.sqrMagnitude > 0.01f) GamepadInput.MarkOther();
                // Merge every live source by max magnitude: keyboard
                // (full-throttle, up to √2 on diagonals), the gamepad stick
                // (deadzone-rescaled analog) and the touch joystick.
                Vector2 stick = GamepadInput.LeftStick;
                if (stick.sqrMagnitude > input.sqrMagnitude)
                {
                    input = stick;
                    GamepadInput.MarkGamepad();
                }
                if (TouchControls.Instance != null &&
                    TouchControls.Instance.MoveVector.sqrMagnitude > 0.01f &&
                    TouchControls.Instance.MoveVector.sqrMagnitude > input.sqrMagnitude)
                {
                    input = TouchControls.Instance.MoveVector;
                    GamepadInput.MarkOther();
                }
            }

            Vector3 wishDir = Vector3.zero;
            if (input.sqrMagnitude > 0.01f)
            {
                Camera cam = Camera.main;
                Transform camTr = cam != null ? cam.transform : tr;
                Vector3 fwd = camTr.forward; fwd.y = 0f;
                Vector3 right = camTr.right; right.y = 0f;
                wishDir = right * input.x + fwd * input.y;
                float mag = wishDir.magnitude;
                if (mag > 0.01f)
                {
                    // Keyboard input is full-throttle; the touch joystick is
                    // analog, so scale by how far the stick is pushed.
                    float throttle = Mathf.Clamp01(input.magnitude);
                    wishDir = (wishDir / mag) * throttle;
                }
                else wishDir = Vector3.zero;
            }

            float control = grounded ? 1f : (flyMode ? 1f : airControl);
            // Gust push rides inside the target velocity, like platform
            // carrying: the control blend then works WITH the gust instead
            // of cancelling it the frame after.
            Vector3 targetVel = wishDir * moveSpeed * (flyMode ? 0.85f : 1f)
                + platformVelocity + gustPush;
            Vector3 vel = rb.linearVelocity;
            float blend = 1f - Mathf.Exp(-acceleration * control * Time.fixedDeltaTime);
            vel.x = Mathf.Lerp(vel.x, targetVel.x, blend);
            vel.z = Mathf.Lerp(vel.z, targetVel.z, blend);
            // Terminal fall speed: long drops used to build unbounded speed,
            // which made landing squash/dust thresholds and timing reads
            // inconsistent between a 3-unit hop and a 30-unit plunge.
            if (!flyMode) vel.y = Mathf.Max(vel.y, -28f);
            if (flyMode)
            {
                // Hold jump to rise; let go and Gloomfang gently sinks.
                // (Touch holds report through JumpHeld — onClick-only jump
                // used to make flight rise for just the buffer window.)
                bool rising = Input.GetButton("Jump") || TouchControls.JumpHeld ||
                    GamepadInput.JumpHeld ||
                    Time.time - lastJumpPressedTime <= jumpBuffer;
                float targetY = rising ? 5.5f : -1.4f;
                vel.y = Mathf.Lerp(vel.y, targetY,
                    1f - Mathf.Exp(-4f * Time.fixedDeltaTime));
            }
            else if (gustPush != Vector3.zero)
            {
                // Inside a gust: gustLift is the vertical target (0 = hold
                // your entry height), so the crossing stays flat.
                vel.y = Mathf.Lerp(vel.y, gustLift,
                    1f - Mathf.Exp(-4f * Time.fixedDeltaTime));
            }
            else if (windLift > 0f)
            {
                // Wind columns: constant lift while inside (the fountain),
                // but pressing DOWN sinks through the wind at a gentle
                // pace — the explicit "let me off here" control. The gate
                // below also means wind never dampens an upward jump.
                float lift = input.y < -0.5f ? -3f : windLift;
                if (vel.y < lift)
                {
                    vel.y = Mathf.Lerp(vel.y, lift,
                        1f - Mathf.Exp(-5f * Time.fixedDeltaTime));
                }
            }
            rb.linearVelocity = vel;
            gustPush = Vector3.zero;
            gustLift = 0f;
            windLift = 0f;
        }

        bool CheckGrounded()
        {
            Vector3 origin = tr.position + Vector3.down * (capsule.height * 0.5f - 0.04f);
            Collider[] hits = Physics.OverlapSphere(origin, 0.3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == capsule) continue;
                return true;
            }
            return false;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<HazardMarker>() != null)
            {
                GameManager.Instance.OnPlayerDied();
                return;
            }
            TryRide(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            TryRide(collision);
        }

        void TryRide(Collision collision)
        {
            if (!grounded)
            {
                platformVelocity = Vector3.zero;
                return;
            }
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                if (collision.contacts[i].normal.y > 0.5f)
                {
                    MovingPlatform mover = collision.gameObject.GetComponent<MovingPlatform>();
                    platformVelocity = mover != null ? mover.Velocity : Vector3.zero;
                    return;
                }
            }
        }

        /// Called by bounce pads: an external launch, stronger than a jump.
        /// Keeps the squash-and-stretch feel consistent with jumping.
        public void Launch(float verticalVelocity)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = Mathf.Max(vel.y, 0f);
            vel.y = verticalVelocity;
            rb.linearVelocity = vel;
            squash = 0.34f;
            lastJumpPressedTime = -99f;
            lastGroundedTime = -99f;
            platformVelocity = Vector3.zero;
            jumpCutApplied = true; // a pad launch is never cut short
        }

        /// Called by Updraft columns while Pip is inside: blend vertical
        /// velocity toward the column's lift, then clear for this frame.
        public void SetWindLift(float lift)
        {
            windLift = lift;
        }

        /// Called by GustZones while their blow is live: a horizontal carry
        /// plus vertical sustain, so gaps are crossed on the wind.
        public void SetGustPush(Vector3 push, float lift)
        {
            gustPush = push;
            gustLift = lift;
        }

        public void TeleportTo(Vector3 position)
        {
            // Move the rigidbody, not just the transform: with
            // autoSyncTransforms off, a bare transform write never reaches
            // the physics body — respawn would leave it below KillY and the
            // player would die again instantly.
            rb.position = position;
            tr.position = position;
            rb.linearVelocity = Vector3.zero;
            platformVelocity = Vector3.zero;
            lastJumpPressedTime = -99f;
            lastGroundedTime = Time.time;
        }
    }
}
