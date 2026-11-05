using UnityEngine;

namespace AWildSheepChase.Enemies
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "AWildSheepChase/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Stats")]
        [SerializeField, Min(1f)] private float maxHealth = 30f;
        [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0f)] private float acceleration = 14f;
        [Tooltip("องศาต่อวินาที")]
        [SerializeField, Min(0f)] private float angularSpeed = 540f;

        [Header("Perception")]
        [SerializeField, Min(0f)] private float detectionRange = 10f;
        [Tooltip("ห่างเกินนี้ศัตรูจะเลิกไล่และกลับไป Idle")]
        [SerializeField, Min(0f)] private float loseTargetRange = 16f;

        [Header("Attack")]
        [Tooltip("เริ่มโจมตีได้เมื่อห่างจากผู้เล่นไม่เกินนี้ (ประชิด = ระยะฟัน, ไกล = ระยะยิง)")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
        [Tooltip("ระยะที่ศัตรูหยุดเดินเข้าหา ควรน้อยกว่า Attack Range")]
        [SerializeField, Min(0f)] private float stoppingDistance = 1.3f;
        [Tooltip("เวลาเตรียมโจมตี (ตัวแดง) ผู้เล่นหลบได้ในช่วงนี้")]
        [SerializeField, Min(0f)] private float attackWindup = 0.5f;
        [SerializeField, Min(0f)] private float attackRecovery = 0.6f;
        [Tooltip("เวลาพักหลังจบ recovery ก่อนโจมตีรอบใหม่ได้")]
        [SerializeField, Min(0f)] private float attackCooldown = 0.8f;

        [Header("Melee (ใช้เฉพาะศัตรูประชิด)")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float knockback = 7f;
        [SerializeField, Range(10f, 360f)] private float meleeArcAngle = 120f;

        [Header("Reaction")]
        [Tooltip("ปิดไว้ = โดนตีแล้วไม่สะดุด ไม่กระเด็น (เหมาะกับบอส)")]
        [SerializeField] private bool canBeStaggered = true;
        [SerializeField, Min(0.05f)] private float hurtDuration = 0.3f;
        [Tooltip("0 = กระเด็นเต็มที่, 1 = ไม่กระเด็นเลย")]
        [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;

        [Header("Death")]
        [SerializeField, Min(0f)] private float despawnDelay = 1f;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float AngularSpeed => angularSpeed;

        public float DetectionRange => detectionRange;
        public float LoseTargetRange => loseTargetRange;

        public float AttackRange => attackRange;
        public float StoppingDistance => stoppingDistance;
        public float AttackWindup => attackWindup;
        public float AttackRecovery => attackRecovery;
        public float AttackCooldown => attackCooldown;

        public float Damage => damage;
        public float Knockback => knockback;
        public float MeleeArcAngle => meleeArcAngle;

        public bool CanBeStaggered => canBeStaggered;
        public float HurtDuration => hurtDuration;
        public float KnockbackResistance => knockbackResistance;

        public float DespawnDelay => despawnDelay;
    }
}