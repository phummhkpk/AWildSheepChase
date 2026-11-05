using System;
using AWildSheepChase.CameraSystem;
using UnityEngine;

namespace AWildSheepChase.Combat
{
    [Serializable]
    public class ComboStep
    {
        [Header("Damage")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float knockback = 5f;

        [Header("Hitbox (รูปพัด)")]
        [SerializeField, Min(0.1f)] private float range = 2f;
        [SerializeField, Range(10f, 360f)] private float arcAngle = 100f;

        [Header("Timing (วินาที)")]
        [SerializeField, Min(0f)] private float windupTime = 0.06f;
        [SerializeField, Min(0.02f)] private float activeTime = 0.1f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.22f;

        [Header("Lunge (พุ่งไปข้างหน้าตอนฟัน)")]
        [SerializeField, Min(0f)] private float lungeDistance = 0.6f;
        [SerializeField, Min(0f)] private float lungeDuration = 0.12f;

        [Header("Stamina")]
        [SerializeField, Min(0f)] private float staminaCost = 10f;

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float hitStopDuration = 0.04f;
        [SerializeField] private CameraShakeProfile hitShake;

        public float Damage => damage;
        public float Knockback => knockback;
        public float Range => range;
        public float ArcAngle => arcAngle;
        public float WindupTime => windupTime;
        public float ActiveTime => activeTime;
        public float RecoveryTime => recoveryTime;
        public float LungeDistance => lungeDistance;
        public float LungeDuration => lungeDuration;
        public float StaminaCost => staminaCost;
        public float HitStopDuration => hitStopDuration;
        public CameraShakeProfile HitShake => hitShake;
    }

    [CreateAssetMenu(fileName = "MeleeWeaponData", menuName = "AWildSheepChase/Combat/Melee Weapon Data")]
    public class MeleeWeaponData : ScriptableObject
    {
        [SerializeField] private ComboStep[] comboSteps = new ComboStep[3];
        [Tooltip("กดโจมตีล่วงหน้าได้นานเท่านี้ก่อนถึงจังหวะต่อท่า")]
        [SerializeField, Min(0f)] private float inputBufferTime = 0.3f;
        [Tooltip("ถ้าไม่โจมตีต่อภายในเวลานี้หลังจบท่า combo จะกลับไปเริ่มท่าที่ 1")]
        [SerializeField, Min(0f)] private float comboResetTime = 0.6f;

        public int StepCount => comboSteps != null ? comboSteps.Length : 0;
        public float InputBufferTime => inputBufferTime;
        public float ComboResetTime => comboResetTime;

        public ComboStep GetStep(int index) => comboSteps[index];
    }
}