using UnityEngine;

namespace AWildSheepChase.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "AWildSheepChase/Player/Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Move")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float acceleration = 50f;
        [SerializeField, Min(0f)] private float deceleration = 60f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField, Range(0f, 1f)] private float stickAimDeadzone = 0.3f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
        [SerializeField, Min(1f)] private float gravityMultiplier = 2f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashDistance = 5f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.25f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.8f;
        [SerializeField] private bool dashInvincible = true;
        [SerializeField, Min(0f)] private float dashStaminaCost = 20f;

        [Header("Hurt")]
        [Tooltip("เวลาที่ควบคุมไม่ได้หลังโดนตี")]
        [SerializeField, Min(0.05f)] private float hurtDuration = 0.25f;
        [Tooltip("คูณกับ Knockback ของผู้โจมตี")]
        [SerializeField, Min(0f)] private float hurtKnockbackScale = 1f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckOffset = 0.1f;
        [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.25f;

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float RotationSpeed => rotationSpeed;
        public float StickAimDeadzone => stickAimDeadzone;

        public float JumpHeight => jumpHeight;
        public float GravityMultiplier => gravityMultiplier;
        public float JumpBufferTime => jumpBufferTime;

        public float DashDistance => dashDistance;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
        public bool DashInvincible => dashInvincible;
        public float DashStaminaCost => dashStaminaCost;

        public float HurtDuration => hurtDuration;
        public float HurtKnockbackScale => hurtKnockbackScale;

        public float GroundCheckOffset => groundCheckOffset;
        public float GroundCheckRadius => groundCheckRadius;
    }
}