using System;
using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Player
{
    public enum PlayerState { Idle, Move, Dash, Attack, Hurt, Dead }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("ปล่อยว่างไว้ = ใช้ Camera.main")]
        [SerializeField] private Camera viewCamera;

        public event Action<PlayerState, PlayerState> StateChanged;
        public event Action Jumped;
        public event Action DashStarted;
        public event Action DashEnded;
        public event Action Hurt;
        public event Action Died;

        public PlayerState State { get; private set; } = PlayerState.Idle;
        public bool IsGrounded { get; private set; }
        public bool IsDead => State == PlayerState.Dead;
        public bool IsInvincible => State == PlayerState.Dash && config.DashInvincible;
        public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);

        /// <summary>true เมื่อเริ่มท่าใหม่ได้ (ไม่ได้ติด Dash/Attack/Hurt/Dead)</summary>
        public bool CanStartAction => State == PlayerState.Idle || State == PlayerState.Move;

        /// <summary>true เมื่อเริ่มท่าโจมตีได้ (รวมการต่อ combo ขณะอยู่ใน Attack)</summary>
        public bool CanAttack => CanStartAction || State == PlayerState.Attack;

        private Rigidbody rb;
        private PlayerInputHandler input;
        private Stamina stamina; // ไม่บังคับ: ถ้าไม่มี = ไม่เสีย stamina

        private float jumpBufferedUntil = -1f;
        private float nextDashTime;
        private float dashEndTime;
        private Vector3 dashDirection;

        private Vector3 attackVelocity;
        private float attackDeceleration;

        private Vector3 hurtVelocity;
        private float hurtDeceleration;
        private float hurtEndTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<PlayerInputHandler>();
            TryGetComponent(out stamina);

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera == null)
            {
                Debug.LogWarning("PlayerController: ไม่พบกล้อง (ตรวจว่า Main Camera มี Tag 'MainCamera')", this);
            }
        }

        private void OnEnable()
        {
            input.JumpPressed += OnJumpPressed;
            input.DashPressed += OnDashPressed;
        }

        private void OnDisable()
        {
            input.JumpPressed -= OnJumpPressed;
            input.DashPressed -= OnDashPressed;
        }

        private void OnJumpPressed()
        {
            jumpBufferedUntil = Time.time + config.JumpBufferTime;
        }

        private void OnDashPressed()
        {
            TryStartDash();
        }

        private void FixedUpdate()
        {
            CheckGround();
            ApplyExtraGravity();

            switch (State)
            {
                case PlayerState.Idle:
                case PlayerState.Move:
                    HandleLocomotion();
                    break;
                case PlayerState.Dash:
                    HandleDash();
                    break;
                case PlayerState.Attack:
                    HandleAttack();
                    break;
                case PlayerState.Hurt:
                    HandleHurt();
                    break;
                case PlayerState.Dead:
                    HandleDead();
                    break;
            }

            rb.angularVelocity = Vector3.zero;
        }

        // ---------- API สำหรับระบบอื่น ----------

        /// <summary>
        /// เริ่มท่าโจมตี 1 ท่า: หันไปทางที่เล็งทันที และพุ่ง (lunge) ไปข้างหน้าเล็กน้อย
        /// ความเร็วเริ่มต้นจะลดลงเป็น 0 ตลอดช่วง lungeDuration
        /// </summary>
        public bool BeginAttackStep(Vector3 facing, float lungeSpeed, float lungeDuration)
        {
            if (!CanAttack) return false;

            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                facing.Normalize();
                rb.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }

            attackVelocity = facing * lungeSpeed;
            attackDeceleration = lungeDuration > 0.001f ? lungeSpeed / lungeDuration : 1000f;
            SetState(PlayerState.Attack);
            return true;
        }

        public void EndAttack()
        {
            if (State == PlayerState.Attack)
            {
                SetState(PlayerState.Idle);
            }
        }

        /// <summary>โดนตี: สะดุดและกระเด็นตามทิศ (เรียกจาก PlayerDamageReceiver)</summary>
        public void ApplyHurt(Vector3 direction, float knockbackForce)
        {
            if (State == PlayerState.Dead) return;

            if (State == PlayerState.Dash)
            {
                // กรณีปิด Dash Invincible แล้วโดนตีกลาง Dash
                nextDashTime = Time.time + config.DashCooldown;
                DashEnded?.Invoke();
            }

            direction.y = 0f;
            float speed = knockbackForce * config.HurtKnockbackScale;

            hurtVelocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector3.zero;
            hurtDeceleration = config.HurtDuration > 0.001f ? speed / config.HurtDuration : 1000f;
            hurtEndTime = Time.time + config.HurtDuration;
            jumpBufferedUntil = -1f;

            SetState(PlayerState.Hurt);
            Hurt?.Invoke();
        }

        public void Die()
        {
            if (State == PlayerState.Dead) return;

            attackVelocity = Vector3.zero;
            hurtVelocity = Vector3.zero;
            SetState(PlayerState.Dead);
            Died?.Invoke();
        }

        /// <summary>ย้ายผู้เล่นไปจุดใหม่และกลับสู่สถานะปกติ (ใช้ตอน respawn และโหลดเซฟ)</summary>
        public void Respawn(Vector3 position, Quaternion rotation)
        {
            attackVelocity = Vector3.zero;
            hurtVelocity = Vector3.zero;
            jumpBufferedUntil = -1f;

            rb.linearVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);

            SetState(PlayerState.Idle);
        }

        /// <summary>ทิศที่ผู้เล่นเล็ง (แนวราบ) ใช้ทั้งโจมตีและยิง</summary>
        public Vector3 GetAimDirection()
        {
            if (TryGetAimDirection(out Vector3 aim))
            {
                return aim;
            }

            Vector3 move = GetMoveDirection();
            if (input.UsingGamepad && move.sqrMagnitude > 0.0001f)
            {
                return move.normalized;
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        // ---------- Movement ----------

        private void CheckGround()
        {
            Vector3 origin = rb.position + Vector3.up * config.GroundCheckOffset;
            IsGrounded = Physics.CheckSphere(origin, config.GroundCheckRadius, groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void ApplyExtraGravity()
        {
            // แรงโน้มถ่วงรวม = Physics.gravity * GravityMultiplier ทำให้กระโดดกระชับขึ้น
            Vector3 extra = Physics.gravity * (config.GravityMultiplier - 1f);
            rb.AddForce(extra, ForceMode.Acceleration);
        }

        private void HandleLocomotion()
        {
            Vector3 moveDir = GetMoveDirection();
            bool hasInput = moveDir.sqrMagnitude > 0.0001f;

            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 targetHorizontal = moveDir * config.MoveSpeed;
            float rate = hasInput ? config.Acceleration : config.Deceleration;
            horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, rate * Time.fixedDeltaTime);

            float y = velocity.y;
            if (IsGrounded && jumpBufferedUntil >= Time.time)
            {
                float effectiveGravity = Mathf.Abs(Physics.gravity.y) * config.GravityMultiplier;
                y = Mathf.Sqrt(2f * effectiveGravity * config.JumpHeight);
                jumpBufferedUntil = -1f;
                Jumped?.Invoke();
            }

            rb.linearVelocity = new Vector3(horizontal.x, y, horizontal.z);

            RotateTowardsAim(moveDir);
            SetState(hasInput ? PlayerState.Move : PlayerState.Idle);
        }

        private void HandleAttack()
        {
            attackVelocity = Vector3.MoveTowards(
                attackVelocity, Vector3.zero, attackDeceleration * Time.fixedDeltaTime);

            Vector3 velocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(attackVelocity.x, velocity.y, attackVelocity.z);
        }

        private void HandleHurt()
        {
            hurtVelocity = Vector3.MoveTowards(
                hurtVelocity, Vector3.zero, hurtDeceleration * Time.fixedDeltaTime);

            Vector3 velocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(hurtVelocity.x, velocity.y, hurtVelocity.z);

            if (Time.time >= hurtEndTime)
            {
                SetState(PlayerState.Idle);
            }
        }

        private void HandleDead()
        {
            Vector3 velocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }

        private void TryStartDash()
        {
            // Dash ยกเลิกท่าโจมตีได้ (Attack) เพื่อให้หลบได้ทันใจ
            if (!CanAttack) return;
            if (Time.time < nextDashTime) return;
            if (stamina != null && !stamina.TrySpend(config.DashStaminaCost)) return;

            Vector3 dir = GetMoveDirection();
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
                dir.y = 0f;
            }

            dashDirection = dir.normalized;
            dashEndTime = Time.time + config.DashDuration;
            SetState(PlayerState.Dash);
            DashStarted?.Invoke();
        }

        private void HandleDash()
        {
            float speed = config.DashDistance / config.DashDuration;
            rb.linearVelocity = new Vector3(dashDirection.x * speed, rb.linearVelocity.y, dashDirection.z * speed);

            if (Time.time >= dashEndTime)
            {
                nextDashTime = Time.time + config.DashCooldown;
                SetState(PlayerState.Idle);
                DashEnded?.Invoke();
            }
        }

        private Vector3 GetMoveDirection()
        {
            Vector2 move = input.Move;
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (viewCamera != null)
            {
                forward = viewCamera.transform.forward;
                right = viewCamera.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            return Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);
        }

        private void RotateTowardsAim(Vector3 moveDir)
        {
            Vector3 targetDir;

            if (!TryGetAimDirection(out targetDir))
            {
                // gamepad ที่ไม่ได้แตะ stick ขวา → หันตามทิศที่เดิน
                if (input.UsingGamepad && moveDir.sqrMagnitude > 0.0001f)
                {
                    targetDir = moveDir;
                }
                else
                {
                    return;
                }
            }

            Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
            Quaternion newRotation = Quaternion.RotateTowards(
                rb.rotation, targetRotation, config.RotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }

        private bool TryGetAimDirection(out Vector3 direction)
        {
            direction = Vector3.zero;

            if (input.UsingGamepad)
            {
                Vector2 stick = input.LookStick;
                if (stick.magnitude < config.StickAimDeadzone) return false;

                Vector3 forward = Vector3.forward;
                Vector3 right = Vector3.right;
                if (viewCamera != null)
                {
                    forward = viewCamera.transform.forward;
                    right = viewCamera.transform.right;
                    forward.y = 0f;
                    right.y = 0f;
                    forward.Normalize();
                    right.Normalize();
                }

                direction = (forward * stick.y + right * stick.x).normalized;
                return true;
            }

            if (viewCamera == null) return false;

            Ray ray = viewCamera.ScreenPointToRay(input.PointerPosition);
            Plane plane = new Plane(Vector3.up, rb.position);
            if (!plane.Raycast(ray, out float distance)) return false;

            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 toPoint = hitPoint - rb.position;
            toPoint.y = 0f;
            if (toPoint.sqrMagnitude < 0.01f) return false;

            direction = toPoint.normalized;
            return true;
        }

        private void SetState(PlayerState next)
        {
            if (State == next) return;
            PlayerState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                transform.position + Vector3.up * config.GroundCheckOffset, config.GroundCheckRadius);
        }
    }
}