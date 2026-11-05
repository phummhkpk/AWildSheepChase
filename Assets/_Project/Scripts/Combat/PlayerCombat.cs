using System;
using System.Collections.Generic;
using AWildSheepChase.Core;
using AWildSheepChase.Player;
using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Combat
{
    public enum AttackPhase { None, Windup, Active, Recovery }

    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(ProjectileLauncher))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private MeleeWeaponData meleeWeapon;
        [SerializeField] private LayerMask targetLayers;
        [Tooltip("ความสูงจากเท้าถึงจุดกำเนิด hitbox (ระดับอก)")]
        [SerializeField, Min(0f)] private float hitOriginHeight = 1f;

        public event Action<int> MeleeStarted;
        public event Action<int> MeleeHitboxActivated;
        public event Action<int> HitLanded;
        public event Action MeleeEnded;
        public event Action RangedFired;

        public AttackPhase Phase { get; private set; } = AttackPhase.None;

        private PlayerController controller;
        private PlayerInputHandler input;
        private ProjectileLauncher launcher;
        private Stamina stamina; // ไม่บังคับ

        private readonly Collider[] overlapBuffer = new Collider[16];
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        private int currentStepIndex;
        private int nextComboIndex;
        private float phaseEndTime;
        private float bufferedAttackUntil = -1f;
        private float comboExpireTime;
        private Vector3 attackFacing = Vector3.forward;

        private bool HasBufferedAttack => Time.time <= bufferedAttackUntil;
        private bool HasWeapon => meleeWeapon != null && meleeWeapon.StepCount > 0;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputHandler>();
            launcher = GetComponent<ProjectileLauncher>();
            TryGetComponent(out stamina);

            if (!HasWeapon)
            {
                Debug.LogError("PlayerCombat: ยังไม่ได้ใส่ Melee Weapon หรือ Combo Steps ว่างเปล่า", this);
            }
        }

        private void OnEnable()
        {
            input.AttackPressed += OnAttackPressed;
            input.RangedPressed += OnRangedPressed;
            controller.StateChanged += OnPlayerStateChanged;
        }

        private void OnDisable()
        {
            input.AttackPressed -= OnAttackPressed;
            input.RangedPressed -= OnRangedPressed;
            controller.StateChanged -= OnPlayerStateChanged;
        }

        private void OnPlayerStateChanged(PlayerState previous, PlayerState next)
        {
            // Dash / โดนตี / ตาย ยกเลิกท่าโจมตีที่ค้างอยู่
            if (next == PlayerState.Dash || next == PlayerState.Hurt || next == PlayerState.Dead)
            {
                CancelAttack();
            }
        }

        private void OnAttackPressed()
        {
            if (!HasWeapon) return;
            bufferedAttackUntil = Time.time + meleeWeapon.InputBufferTime;
        }

        private void OnRangedPressed()
        {
            if (!controller.CanStartAction) return;

            if (launcher.TryFire(controller.GetAimDirection(), gameObject))
            {
                RangedFired?.Invoke();
            }
        }

        private void FixedUpdate()
        {
            if (!HasWeapon) return;

            switch (Phase)
            {
                case AttackPhase.None:
                    UpdateNone();
                    break;
                case AttackPhase.Windup:
                    if (Time.time >= phaseEndTime) EnterActive();
                    break;
                case AttackPhase.Active:
                    PerformHitCheck();
                    if (Time.time >= phaseEndTime) EnterRecovery();
                    break;
                case AttackPhase.Recovery:
                    UpdateRecovery();
                    break;
            }
        }

        private void UpdateNone()
        {
            if (nextComboIndex > 0 && Time.time > comboExpireTime)
            {
                nextComboIndex = 0;
            }

            if (HasBufferedAttack && controller.CanStartAction)
            {
                StartStep(nextComboIndex);
            }
        }

        private void StartStep(int index)
        {
            if (!controller.CanAttack) return;

            ComboStep step = meleeWeapon.GetStep(index);

            if (stamina != null && !stamina.TrySpend(step.StaminaCost))
            {
                // stamina ไม่พอ: ทิ้ง input ที่กดไว้ เพื่อไม่ให้โจมตีเองตอน stamina ฟื้น
                bufferedAttackUntil = -1f;
                return;
            }

            Vector3 facing = controller.GetAimDirection();
            float lungeSpeed = step.LungeDuration > 0.001f
                ? 2f * step.LungeDistance / step.LungeDuration
                : 0f;

            if (!controller.BeginAttackStep(facing, lungeSpeed, step.LungeDuration)) return;

            currentStepIndex = index;
            attackFacing = facing;
            bufferedAttackUntil = -1f;
            hitTargets.Clear();

            Phase = AttackPhase.Windup;
            phaseEndTime = Time.time + step.WindupTime;
            MeleeStarted?.Invoke(index);
        }

        private void EnterActive()
        {
            Phase = AttackPhase.Active;
            phaseEndTime = Time.time + meleeWeapon.GetStep(currentStepIndex).ActiveTime;
            MeleeHitboxActivated?.Invoke(currentStepIndex);
        }

        private void EnterRecovery()
        {
            Phase = AttackPhase.Recovery;
            phaseEndTime = Time.time + meleeWeapon.GetStep(currentStepIndex).RecoveryTime;

            // ท่าสุดท้ายไม่มีท่าต่อ (nextComboIndex = 0)
            bool hasNext = currentStepIndex + 1 < meleeWeapon.StepCount;
            nextComboIndex = hasNext ? currentStepIndex + 1 : 0;
        }

        private void UpdateRecovery()
        {
            if (HasBufferedAttack && nextComboIndex > 0)
            {
                StartStep(nextComboIndex);
                return;
            }

            if (Time.time >= phaseEndTime)
            {
                FinishAttack();
            }
        }

        private void FinishAttack()
        {
            Phase = AttackPhase.None;
            comboExpireTime = Time.time + meleeWeapon.ComboResetTime;
            controller.EndAttack();
            MeleeEnded?.Invoke();
        }

        private void CancelAttack()
        {
            if (Phase == AttackPhase.None) return;

            Phase = AttackPhase.None;
            nextComboIndex = 0;
            bufferedAttackUntil = -1f;
            hitTargets.Clear();
            MeleeEnded?.Invoke();
        }

        private void PerformHitCheck()
        {
            ComboStep step = meleeWeapon.GetStep(currentStepIndex);
            Vector3 origin = transform.position + Vector3.up * hitOriginHeight;

            int count = Physics.OverlapSphereNonAlloc(
                origin, step.Range, overlapBuffer, targetLayers, QueryTriggerInteraction.Ignore);

            float halfArc = step.ArcAngle * 0.5f;
            int hits = 0;

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || hitTargets.Contains(damageable)) continue;

                // ใช้จุดที่ใกล้ที่สุดบน collider เพื่อให้ศัตรูตัวใหญ่ถูกตีได้ถูกต้อง
                Vector3 toTarget = col.ClosestPoint(origin) - origin;
                toTarget.y = 0f;
                bool hasDirection = toTarget.sqrMagnitude > 0.0001f;

                if (hasDirection && Vector3.Angle(attackFacing, toTarget) > halfArc) continue;

                Vector3 hitDirection = hasDirection ? toTarget.normalized : attackFacing;
                hitTargets.Add(damageable);
                damageable.TakeDamage(new DamageInfo(step.Damage, hitDirection, step.Knockback, gameObject));
                hits++;
            }

            if (hits > 0)
            {
                GameEvents.RequestHitStop(step.HitStopDuration);
                GameEvents.RequestCameraShake(step.HitShake);
                HitLanded?.Invoke(hits);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (meleeWeapon == null || meleeWeapon.StepCount == 0) return;

            ComboStep step = meleeWeapon.GetStep(0);
            Vector3 origin = transform.position + Vector3.up * hitOriginHeight;
            Vector3 facing = Application.isPlaying && Phase != AttackPhase.None ? attackFacing : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) return;
            facing.Normalize();

            float half = step.ArcAngle * 0.5f;
            Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * facing * step.Range;
            Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * facing * step.Range;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + left);
            Gizmos.DrawLine(origin, origin + right);
            Gizmos.DrawLine(origin + left, origin + right);
        }
    }
}