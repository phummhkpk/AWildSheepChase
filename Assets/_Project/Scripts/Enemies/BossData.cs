using System;
using AWildSheepChase.CameraSystem;
using UnityEngine;

namespace AWildSheepChase.Enemies
{
    public enum BossAttackType { Swipe, Charge, ProjectileFan, GroundSlam }

    [Serializable]
    public class BossPhase
    {
        [SerializeField] private string phaseName = "Phase";
        [Tooltip("phase นี้เริ่มเมื่อ HP ลดลงถึงสัดส่วนนี้ (1 = เริ่มเกม, 0.66 = เหลือ 66%)")]
        [SerializeField, Range(0f, 1f)] private float startsAtHealthFraction = 1f;
        [SerializeField, Min(0f)] private float moveSpeed = 2.8f;
        [Tooltip("พักหลังจบท่าก่อนเลือกท่าใหม่")]
        [SerializeField, Min(0f)] private float restTime = 1.4f;
        [Tooltip("คูณเวลา windup ทุกท่า (น้อยกว่า 1 = เร็วขึ้น หลบยากขึ้น)")]
        [SerializeField, Range(0.3f, 1.5f)] private float windupScale = 1f;
        [Tooltip("จำนวนรอบยิงกระสุนที่เพิ่มจากค่าพื้นฐาน")]
        [SerializeField, Min(0)] private int extraFanWaves;
        [SerializeField] private BossAttackType[] attacks = { BossAttackType.Swipe };

        public string PhaseName => phaseName;
        public float StartsAtHealthFraction => startsAtHealthFraction;
        public float MoveSpeed => moveSpeed;
        public float RestTime => restTime;
        public float WindupScale => windupScale;
        public int ExtraFanWaves => extraFanWaves;
        public BossAttackType[] Attacks => attacks;
    }

    /// <summary>ค่าที่ทุกท่าต้องมี: ช่วงระยะที่เลือกท่านี้ได้, เวลา windup, เวลา recovery</summary>
    [Serializable]
    public abstract class BossAttackParams
    {
        [Tooltip("ใช้ท่านี้ได้เมื่อห่างจากผู้เล่นอย่างน้อยเท่านี้")]
        [SerializeField, Min(0f)] private float engageMin;
        [Tooltip("ใช้ท่านี้ได้เมื่อห่างจากผู้เล่นไม่เกินเท่านี้")]
        [SerializeField, Min(0f)] private float engageMax;
        [SerializeField, Min(0.05f)] private float windup;
        [SerializeField, Min(0f)] private float recovery;

        public float EngageMin => engageMin;
        public float EngageMax => engageMax;
        public float Windup => windup;
        public float Recovery => recovery;

        protected BossAttackParams(float engageMin, float engageMax, float windup, float recovery)
        {
            this.engageMin = engageMin;
            this.engageMax = engageMax;
            this.windup = windup;
            this.recovery = recovery;
        }
    }

    [Serializable]
    public class SwipeParams : BossAttackParams
    {
        [SerializeField, Min(0.1f)] private float range = 3.2f;
        [SerializeField, Range(10f, 360f)] private float arcAngle = 160f;
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0f)] private float knockback = 12f;

        public float Range => range;
        public float ArcAngle => arcAngle;
        public float Damage => damage;
        public float Knockback => knockback;

        public SwipeParams() : base(0f, 3.6f, 0.8f, 0.9f) { }
    }

    [Serializable]
    public class ChargeParams : BossAttackParams
    {
        [SerializeField, Min(0.1f)] private float speed = 14f;
        [Tooltip("พุ่งนานสูงสุดเท่านี้ (ระยะสูงสุด = Speed x Duration)")]
        [SerializeField, Min(0.1f)] private float duration = 0.9f;
        [SerializeField, Min(0.1f)] private float hitRadius = 1.3f;
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0f)] private float knockback = 14f;
        [Tooltip("ถ้าชนกำแพง บอสมึนนานเท่านี้ (ช่วงให้ผู้เล่นตีโต้)")]
        [SerializeField, Min(0f)] private float wallStunTime = 1.8f;

        public float Speed => speed;
        public float Duration => duration;
        public float HitRadius => hitRadius;
        public float Damage => damage;
        public float Knockback => knockback;
        public float WallStunTime => wallStunTime;

        public ChargeParams() : base(4f, 14f, 1f, 0.8f) { }
    }

    [Serializable]
    public class FanParams : BossAttackParams
    {
        [SerializeField, Min(1)] private int projectileCount = 5;
        [Tooltip("องศารวมที่กระสุนกระจายออก")]
        [SerializeField, Range(0f, 180f)] private float spreadAngle = 60f;
        [SerializeField, Min(1)] private int waves = 1;
        [SerializeField, Min(0f)] private float waveInterval = 0.45f;

        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public int Waves => waves;
        public float WaveInterval => waveInterval;

        public FanParams() : base(3f, 18f, 0.8f, 0.9f) { }
    }

    [Serializable]
    public class SlamParams : BossAttackParams
    {
        [SerializeField, Min(0.5f)] private float radius = 4.5f;
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0f)] private float knockback = 14f;
        [Tooltip("ผู้เล่นที่เท้าลอยสูงกว่าพื้นเท่านี้ตอนกระแทก จะไม่โดน (กระโดดหลบได้)")]
        [SerializeField, Min(0f)] private float jumpClearHeight = 0.6f;

        public float Radius => radius;
        public float Damage => damage;
        public float Knockback => knockback;
        public float JumpClearHeight => jumpClearHeight;

        public SlamParams() : base(0f, 3.8f, 1.1f, 1.1f) { }
    }

    [CreateAssetMenu(fileName = "BossData", menuName = "AWildSheepChase/Enemies/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private string bossName = "Elder Ram";
        [SerializeField, Min(0f)] private float detectionRange = 14f;
        [Tooltip("ระยะที่บอสหยุดเดินเข้าหา")]
        [SerializeField, Min(0f)] private float stoppingDistance = 2.2f;
        [Tooltip("องศาต่อวินาที")]
        [SerializeField, Min(0f)] private float turnSpeed = 240f;
        [Tooltip("สัดส่วนช่วงแรกของ windup ที่บอสยังหันตามผู้เล่น (ที่เหลือล็อกทิศ)")]
        [SerializeField, Range(0f, 1f)] private float windupTrackFraction = 0.6f;
        [SerializeField, Min(0.1f)] private float transitionDuration = 1.6f;

        [Header("Phases (เรียงจากต้นไปท้าย)")]
        [SerializeField] private BossPhase[] phases = new BossPhase[3];

        [Header("Attacks")]
        [SerializeField] private SwipeParams swipe = new SwipeParams();
        [SerializeField] private ChargeParams charge = new ChargeParams();
        [SerializeField] private FanParams fan = new FanParams();
        [SerializeField] private SlamParams slam = new SlamParams();

        [Header("Feedback")]
        [SerializeField] private CameraShakeProfile slamShake;
        [SerializeField] private CameraShakeProfile crashShake;
        [SerializeField] private CameraShakeProfile phaseShake;
        [SerializeField] private CameraShakeProfile deathShake;

        public string BossName => bossName;
        public float DetectionRange => detectionRange;
        public float StoppingDistance => stoppingDistance;
        public float TurnSpeed => turnSpeed;
        public float WindupTrackFraction => windupTrackFraction;
        public float TransitionDuration => transitionDuration;

        public int PhaseCount => phases != null ? phases.Length : 0;
        public BossPhase GetPhase(int index) => phases[Mathf.Clamp(index, 0, phases.Length - 1)];

        public SwipeParams Swipe => swipe;
        public ChargeParams Charge => charge;
        public FanParams Fan => fan;
        public SlamParams Slam => slam;

        public CameraShakeProfile SlamShake => slamShake;
        public CameraShakeProfile CrashShake => crashShake;
        public CameraShakeProfile PhaseShake => phaseShake;
        public CameraShakeProfile DeathShake => deathShake;

        public BossAttackParams GetParams(BossAttackType type)
        {
            switch (type)
            {
                case BossAttackType.Swipe: return swipe;
                case BossAttackType.Charge: return charge;
                case BossAttackType.ProjectileFan: return fan;
                default: return slam;
            }
        }
    }
}