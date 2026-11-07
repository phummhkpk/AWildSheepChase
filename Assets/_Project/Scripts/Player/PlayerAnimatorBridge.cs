using AWildSheepChase.Combat;
using UnityEngine;

namespace AWildSheepChase.Player
{
    /// <summary>
    /// ส่งสถานะผู้เล่นเข้า Animator (ไม่บังคับ) ถ้าไม่มี Animator ใน Player หรือลูก สคริปต์นี้จะปิดตัวเอง
    /// Parameter ที่ต้องสร้างใน Controller: Speed (Float), Grounded (Bool), Combo (Int),
    /// Attack / Dash / Hurt / Die / Revive (Trigger)
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int ComboId = Animator.StringToHash("Combo");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DashId = Animator.StringToHash("Dash");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int ReviveId = Animator.StringToHash("Revive");

        [Tooltip("ปล่อยว่าง = หาจากลูกของ Player")]
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float speedDamp = 0.08f;

        private PlayerController controller;
        private PlayerCombat combat;
        private Rigidbody rb;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody>();
            TryGetComponent(out combat);

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            controller.StateChanged += OnStateChanged;
            if (combat != null) combat.MeleeStarted += OnMeleeStarted;
        }

        private void OnDisable()
        {
            controller.StateChanged -= OnStateChanged;
            if (combat != null) combat.MeleeStarted -= OnMeleeStarted;
        }

        private void Update()
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;

            animator.SetFloat(SpeedId, velocity.magnitude, speedDamp, Time.deltaTime);
            animator.SetBool(GroundedId, controller.IsGrounded);
        }

        private void OnStateChanged(PlayerState previous, PlayerState next)
        {
            if (previous == PlayerState.Dead)
            {
                animator.SetTrigger(ReviveId);
            }

            switch (next)
            {
                case PlayerState.Dash: animator.SetTrigger(DashId); break;
                case PlayerState.Hurt: animator.SetTrigger(HurtId); break;
                case PlayerState.Dead: animator.SetTrigger(DieId); break;
            }
        }

        private void OnMeleeStarted(int stepIndex)
        {
            animator.SetInteger(ComboId, stepIndex);
            animator.SetTrigger(AttackId);
        }
    }
}