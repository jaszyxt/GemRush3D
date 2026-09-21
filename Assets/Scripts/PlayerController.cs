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
        WindStreaks windStreaks; // speed-line rig while wind carries Pip
        float lastSkidTime = -99f;

        // ---- Idle life: the glance -> wave -> sit -> nap ladder ----
        // Acted, never explained: every rung is pure body language.
        enum IdleStage { Neutral, Glance, Wave, Sit, Sleep }
        IdleStage idleStage = IdleStage.Neutral;
        float idleTime;      // scaled seconds of eligible stillness
        float stageTime;     // scaled seconds since the current rung began
        float spawnGrace = IdleSpawnGrace;
        float moteTimer;     // sleepy "z" motes while sitting / napping
        float gazeWeight;    // glance at the camera: pupils + shy body turn
        float sitWeight;     // sit/nap pose blend
        float breathDepth = 0.045f;
        float breathSpeed = 2.2f;
        float pupilSparkle;  // the wave: two bright pupil pulses
        bool waveSecondPop;
        Vector2 lastMoveInput;
        Vector3 pupilBaseScale;

        // ---- Checkpoint twirl (visual only) ----
        float twirlTime = -1f;
        float twirlYaw;

        // A reversal only counts at speed, and skid puffs are rate-limited
        // so a jittery stick can't machine-gun dust.
        const float SkidMinSpeed = 6f;
        const float SkidCooldown = 0.25f;
        // Wind rides hold a touch of extra lens width (CameraFollow).
        const float WindFovHold = 5f;
        // Jump, land and skid puffs share one soft near-white.
        static readonly Color DustColor = ArtLib.Dust;

        // Idle ladder rungs, in seconds of stillness. The spawn grace is
        // the intro-card window: no rung may trigger inside it.
        const float IdleGlanceAt = 6f;
        const float IdleWaveAt = 12f;
        const float IdleSitAt = 20f;
        const float IdleSleepAt = 35f;
        const float IdleSpawnGrace = 3.5f;
        const float GlanceSeconds = 1.5f;
        const float WaveBeatSeconds = 1.5f;
        const float YawnSeconds = 2.4f;
        const float TwirlSeconds = 0.35f;

        /// Fired once per landing (Pip's position, impact speed). The
        /// reactive world — the pokeable flowers — listens to this instead
        /// of polling anything per frame.
        public static event System.Action<Vector3, float> Landed;

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

            if (pupilL != null) pupilBaseScale = pupilL.localScale;
        }

        /// Gloomfang's playable body: the shared real-cloud look, scaled to
        /// player size, so the follower and the playable storm are twins.
        void BuildGloomfangVisuals()
        {
            Gloomfang.BuildBody(tr, 0.62f, out bodyVisual,
                out pupilL, out pupilR);
            if (pupilL != null) pupilBaseScale = pupilL.localScale;
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
                EndReverie();
            }
            // Gamepad jump rides the same buffered queue as keyboard and
            // touch; hold state feeds variable height / fly mode below.
            if (playing && GamepadInput.JumpPressed)
            {
                lastJumpPressedTime = Time.time;
                GamepadInput.MarkGamepad();
                EndReverie();
            }

            if (TouchControls.JumpQueued)
            {
                TouchControls.JumpQueued = false;
                if (playing)
                {
                    lastJumpPressedTime = Time.time;
                    EndReverie();
                }
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
                Haptics.Light();
                if (!flyMode) Gloomfang.OnPipJumped(tr.position);
                Vector3 feet = tr.position + Vector3.down * 0.9f;
                Fx.Burst(feet, DustColor, 8);
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
        /// lean into motion, breathe when idle — and the idle ladder (glance
        /// -> wave -> sit -> nap) when Pip is left alone a while.
        void UpdateSquash()
        {
            if (bodyVisual == null) return;

            UpdateIdleLife(GameManager.Instance != null &&
                GameManager.Instance.State == GameState.Playing);

            Vector3 localVel = tr.InverseTransformDirection(rb.linearVelocity);
            float speed = localVel.magnitude;

            float target;
            if (sitWeight > 0.001f)
            {
                // Sitting/napping: a low pose with its own breathing sine,
                // blended in over the plain idle breathe.
                float sitPose = -0.18f +
                    Mathf.Sin(Time.time * breathSpeed) * breathDepth;
                float neutralPose = grounded && speed < 0.6f
                    ? Mathf.Sin(Time.time * 2.2f) * 0.045f : 0f;
                target = Mathf.Lerp(neutralPose, sitPose, sitWeight);
            }
            else if (grounded && speed < 0.6f)
                target = Mathf.Sin(Time.time * 2.2f) * 0.045f; // idle breathing
            else target = 0f;
            // Underdamped spring (shared with the flowers): the squash
            // passes slightly past neutral on recovery — classic
            // follow-through, so landings read as bouncy rather than damped.
            Tweener.StepSpring(ref squash, ref squashVel, target,
                Time.deltaTime);
            if (!grounded && rb.linearVelocity.y > 2f && squash < 0.25f)
            {
                squash = Mathf.Lerp(squash, 0.25f, 0.5f);
                squashVel = 0f;
            }

            bodyVisual.localScale = new Vector3(
                1f - squash * 0.6f, 1f + squash, 1f - squash * 0.6f);

            // Lean into horizontal motion; a glance adds a shy turn toward
            // the camera; the checkpoint twirl rides on the same yaw.
            float glanceYaw = 0f;
            float glanceGazeX = 0f;
            if (gazeWeight > 0.001f)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 local = tr.InverseTransformDirection(
                        cam.transform.position - tr.position);
                    glanceYaw = Mathf.Clamp(local.x * 1.4f, -26f, 26f)
                        * gazeWeight;
                    glanceGazeX = Mathf.Clamp(local.x * 0.02f, -0.035f, 0.035f);
                }
            }
            float pitch = Mathf.Clamp(localVel.z * 1.6f, -12f, 12f);
            float roll = Mathf.Clamp(-localVel.x * 1.6f, -12f, 12f);
            bodyVisual.localRotation = Quaternion.Euler(pitch,
                glanceYaw + twirlYaw, roll);

            // Pupils glance toward the movement direction, drift up toward
            // the camera mid-glance, and sparkle twice for the wave.
            if (pupilL != null && pupilR != null)
            {
                float px = Mathf.Clamp(localVel.x * 0.025f, -0.05f, 0.05f);
                float py = Mathf.Clamp(rb.linearVelocity.y * 0.012f, -0.04f, 0.04f);
                Vector3 gaze = new Vector3(px, py, 0f);
                if (gazeWeight > 0.001f)
                    gaze += new Vector3(glanceGazeX * gazeWeight,
                        0.03f * gazeWeight, 0f);
                pupilL.localPosition = gaze;
                pupilR.localPosition = gaze;
                float sparkleScale = 1f + pupilSparkle;
                pupilL.localScale = pupilBaseScale * sparkleScale;
                pupilR.localScale = pupilBaseScale * sparkleScale;
            }
        }

        // ------------------------------------------------------------------
        // The idle ladder. A tiny state machine on stillness: glance at the
        // camera, a happy double-bounce with sparkling pupils, a sit with a
        // slow yawn, then full sleep under Gloomfang's shade. Zero words.
        // ------------------------------------------------------------------

        void UpdateIdleLife(bool playing)
        {
            float dt = Time.deltaTime;
            // Everything below advances on the paused-safe clock: a checkpoint
            // twirl used to keep spinning behind the pause menu (it consumed
            // raw dt before the eligibility gate) and then resumed from a
            // stale yaw. Freezing it here covers the twirl and the motes in
            // one place, matching UpdateExpression's existing treatment.
            float liveDt = Time.timeScale > 0f ? dt : 0f;

            // The checkpoint twirl is pure visual and runs through anything
            // except a pause.
            if (twirlTime >= 0f)
            {
                twirlTime += liveDt;
                float k = Mathf.Clamp01(twirlTime / TwirlSeconds);
                twirlYaw = 360f * (1f - (1f - k) * (1f - k) * (1f - k));
                if (k >= 1f) { twirlTime = -1f; twirlYaw = 0f; }
            }

            if (playing && spawnGrace > 0f) spawnGrace -= dt;

            bool eligible = playing && Time.timeScale > 0f && grounded &&
                spawnGrace <= 0f &&
                lastMoveInput.sqrMagnitude < 0.01f &&
                rb.linearVelocity.sqrMagnitude < 1f;

            if (eligible)
            {
                idleTime += dt;
                stageTime += dt;
                AdvanceIdleStage();
                if (idleStage == IdleStage.Wave && !waveSecondPop &&
                    stageTime >= 0.45f)
                {
                    // the second hop of the happy double-bounce
                    waveSecondPop = true;
                    squashVel = 1.8f;
                }
                if (idleStage >= IdleStage.Sit) TickSleepMotes(liveDt);
            }
            else if (playing && !grounded && idleStage >= IdleStage.Sit)
            {
                CancelIdleLife(); // the sit pose makes no sense mid-air
            }

            UpdateExpression(Time.timeScale > 0f ? dt : 0f);
        }

        void AdvanceIdleStage()
        {
            switch (idleStage)
            {
                case IdleStage.Neutral:
                    if (idleTime >= IdleGlanceAt)
                        EnterIdleStage(IdleStage.Glance);
                    break;
                case IdleStage.Glance:
                    if (idleTime >= IdleWaveAt)
                        EnterIdleStage(IdleStage.Wave);
                    break;
                case IdleStage.Wave:
                    if (idleTime >= IdleSitAt)
                        EnterIdleStage(IdleStage.Sit);
                    break;
                case IdleStage.Sit:
                    if (idleTime >= IdleSleepAt)
                        EnterIdleStage(IdleStage.Sleep);
                    break;
            }
        }

        void EnterIdleStage(IdleStage stage)
        {
            idleStage = stage;
            stageTime = 0f;
            if (stage == IdleStage.Wave)
            {
                waveSecondPop = false;
                squash = -0.08f; // first hop of the double-bounce
                squashVel = 1.6f;
            }
            else if (stage == IdleStage.Sleep)
            {
                // Eyes close; if Gloomfang is along, he drifts over to
                // hover above the nap as a shade.
                if (pupilL != null) pupilL.gameObject.SetActive(false);
                if (pupilR != null) pupilR.gameObject.SetActive(false);
                if (Gloomfang.Companion != null)
                    Gloomfang.Companion.SetShade(tr);
            }
        }

        void TickSleepMotes(float dt)
        {
            moteTimer -= dt;
            if (moteTimer > 0f) return;
            moteTimer = idleStage == IdleStage.Sleep ? 1.2f : 2.4f;
            Fx.SleepMote(tr.position + Vector3.up * 1.15f +
                new Vector3(Random.Range(-0.2f, 0.2f), 0f,
                    Random.Range(-0.2f, 0.2f)));
        }

        /// Expression targets for the current rung; the weights ease at
        /// real-frame rate so cancelling fades the pose instead of snapping.
        void UpdateExpression(float dt)
        {
            float gazeTarget = 0f;
            float sitTarget = 0f;
            pupilSparkle = 0f;
            breathDepth = 0.045f;
            breathSpeed = 2.2f;

            if (idleStage == IdleStage.Glance)
            {
                // Rise, hold, return — one full glance across ~1.5 s.
                float k = stageTime / GlanceSeconds;
                gazeTarget =
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.25f)) *
                    Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((k - 0.75f) / 0.25f));
            }
            else if (idleStage == IdleStage.Wave)
            {
                if (stageTime < WaveBeatSeconds)
                    pupilSparkle = (Mathf.Sin(stageTime / WaveBeatSeconds *
                        Mathf.PI * 4f) * 0.5f + 0.5f) * 0.3f;
            }
            else if (idleStage == IdleStage.Sit)
            {
                // The yawn is one big slow breath on the way into the sit.
                float yawn = Mathf.Sin(Mathf.PI *
                    Mathf.Clamp01(stageTime / YawnSeconds));
                breathDepth = 0.02f + yawn * 0.09f;
                breathSpeed = 1.6f;
                sitTarget = 1f;
            }
            else if (idleStage == IdleStage.Sleep)
            {
                breathDepth = 0.07f;  // deeper, slower napping breath
                breathSpeed = 1.1f;
                sitTarget = 1f;
            }

            sitWeight = Mathf.MoveTowards(sitWeight, sitTarget, dt * 2.5f);
            gazeWeight = Mathf.MoveTowards(gazeWeight, gazeTarget, dt * 5f);
        }

        /// Any input or level event tears the ladder down; waking from an
        /// actual pose (eyes shut) gets one springy squash-pop.
        void CancelIdleLife()
        {
            idleStage = IdleStage.Neutral;
            idleTime = 0f;
            stageTime = 0f;
            moteTimer = 0f;
            waveSecondPop = false;
            if ((pupilL != null && !pupilL.gameObject.activeSelf) ||
                (pupilR != null && !pupilR.gameObject.activeSelf))
            {
                squash = -0.12f;
                squashVel = 1.4f;
                if (pupilL != null) pupilL.gameObject.SetActive(true);
                if (pupilR != null) pupilR.gameObject.SetActive(true);
                if (Gloomfang.Companion != null)
                    Gloomfang.Companion.SetShade(null);
            }
        }

        /// Input happened: the idle ladder resets and any checkpoint twirl
        /// is cut short.
        void EndReverie()
        {
            CancelIdleLife();
            if (twirlTime >= 0f) { twirlTime = -1f; twirlYaw = 0f; }
        }

        /// The checkpoint celebration: a quick 360° yaw twirl plus a
        /// hop-squash, both purely visual — the movement state is never
        /// touched, so this layers over the pad's ring, burst and toast.
        /// Back-to-back checkpoints each get their own twirl.
        public void Twirl()
        {
            CancelIdleLife();
            twirlTime = 0f;
            twirlYaw = 0f;
            squash = -0.14f; // the hop is sold entirely in the squash spring
            squashVel = 2.4f;
        }

        void OnLand(float impactSpeed)
        {
            squash = Mathf.Clamp(-impactSpeed * 0.06f, -0.3f, 0f);
            AudioManager.Instance.PlayLand(impactSpeed);
            // Hard landings land in the hand: the same impact scale the
            // sound and dust already use, so all three agree.
            if (impactSpeed > 8f) Haptics.Medium();
            else if (impactSpeed > 3f) Haptics.Light();
            if (impactSpeed > 5f)
                Fx.Burst(tr.position + Vector3.down * 0.9f, DustColor, 10);
            if (!flyMode && Landed != null) Landed(tr.position, impactSpeed);
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
            lastMoveInput = input;
            if (playing && input.sqrMagnitude > 0.01f) EndReverie();

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

            UpdateSkid(vel, wishDir);
            UpdateWindRide(vel);

            gustPush = Vector3.zero;
            gustLift = 0f;
            windLift = 0f;
        }

        /// A sharp reversal at speed kicks up a small dust scrape, so the
        /// turn-around reads physical instead of a direction snap. The dot
        /// test is the multi-axis sign flip: the input now points against
        /// the motion.
        void UpdateSkid(Vector3 vel, Vector3 wishDir)
        {
            if (!grounded || Time.time - lastSkidTime < SkidCooldown) return;
            Vector3 flat = new Vector3(vel.x, 0f, vel.z);
            float speed = flat.magnitude;
            if (speed < SkidMinSpeed || wishDir.sqrMagnitude < 0.01f) return;
            if (Vector3.Dot(flat / speed, wishDir) > -0.6f) return;
            lastSkidTime = Time.time;
            Fx.Burst(tr.position + Vector3.down * 0.9f, DustColor, 6);
            // The scrape that belongs with the dust: cooldown and speed gate
            // are already handled above, so this cannot machine-gun.
            Haptics.Light();
            AudioManager.Instance.PlaySkid(
                Mathf.InverseLerp(SkidMinSpeed, moveSpeed, speed));
        }

        /// While any wind carries Pip this physics frame (gust push or
        /// updraft lift — the zones re-set those fields every frame), the
        /// ride is on: speed streaks, the gust haptic texture and a small
        /// FOV hold. One wind-free frame ends the ride, so there is no
        /// per-zone enter/exit bookkeeping to leak.
        void UpdateWindRide(Vector3 vel)
        {
            bool riding = gustPush != Vector3.zero || windLift > 0f;
            CameraFollow rig = GameBootstrap.CameraRig;
            if (!riding)
            {
                if (windStreaks == null) return;
                windStreaks.End();
                windStreaks = null;
                if (rig != null) rig.ClearFovHold();
                return;
            }
            if (windStreaks == null) windStreaks = WindStreaks.Attach(tr);
            Vector3 flow = vel;
            if (flow.sqrMagnitude < 1f) flow = gustPush; // slow entry: aim by wind
            windStreaks.Refresh(flow);
            if (rig != null) rig.SetFovHold(WindFovHold);
            Haptics.StartRideTexture();
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
            EndReverie(); // a pad launch is a level event: the ladder resets
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
            // Respawn (checkpoint or death) restarts the idle ladder and
            // the twirl from scratch.
            CancelIdleLife();
            twirlTime = -1f;
            twirlYaw = 0f;
        }
    }
}
