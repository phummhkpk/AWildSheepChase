using System.Collections.Generic;
using AWildSheepChase.Core;
using UnityEngine;

namespace AWildSheepChase.Enemies
{
    public class EnemyMeleeAttack : EnemyAttack
    {
        [SerializeField] private LayerMask targetLayers;
        [Tooltip("ความสูงจากเท้าถึงจุดกำเนิด hitbox")]
        [SerializeField, Min(0f)] private float hitOriginHeight = 1f;

        private readonly Collider[] overlapBuffer = new Collider[8];
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        public override void Execute(Transform target, Vector3 facing)
        {
            Vector3 origin = transform.position + Vector3.up * hitOriginHeight;

            int count = Physics.OverlapSphereNonAlloc(
                origin, Data.AttackRange, overlapBuffer, targetLayers, QueryTriggerInteraction.Ignore);

            float halfArc = Data.MeleeArcAngle * 0.5f;
            hitTargets.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || hitTargets.Contains(damageable)) continue;

                Vector3 toTarget = col.ClosestPoint(origin) - origin;
                toTarget.y = 0f;
                bool hasDirection = toTarget.sqrMagnitude > 0.0001f;

                if (hasDirection && Vector3.Angle(facing, toTarget) > halfArc) continue;

                Vector3 hitDirection = hasDirection ? toTarget.normalized : facing;
                hitTargets.Add(damageable);
                damageable.TakeDamage(new DamageInfo(Data.Damage, hitDirection, Data.Knockback, gameObject));
            }
        }
    }
}