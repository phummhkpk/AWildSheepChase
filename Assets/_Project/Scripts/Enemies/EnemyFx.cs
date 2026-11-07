using AWildSheepChase.Core;
using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Enemies
{
    /// <summary>แปลงเหตุการณ์ของศัตรู/บอสเป็นเอฟเฟกต์เสียง/ภาพ ใส่ที่ prefab ศัตรูทุกตัว</summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyController))]
    public class EnemyFx : MonoBehaviour
    {
        [Tooltip("ความสูงจากเท้าที่เอฟเฟกต์จะเกิด (บอสควรสูงกว่า)")]
        [SerializeField, Min(0f)] private float effectHeight = 1f;

        private Health health;
        private EnemyController enemy;
        private BossController boss; // ไม่บังคับ

        private void Awake()
        {
            health = GetComponent<Health>();
            enemy = GetComponent<EnemyController>();
            TryGetComponent(out boss);
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            enemy.StateChanged += OnStateChanged;

            if (boss != null)
            {
                boss.PhaseChanged += OnPhaseChanged;
                boss.Defeated += OnDefeated;
            }
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            enemy.StateChanged -= OnStateChanged;

            if (boss != null)
            {
                boss.PhaseChanged -= OnPhaseChanged;
                boss.Defeated -= OnDefeated;
            }
        }

        private Vector3 Center => transform.position + Vector3.up * effectHeight;

        private void OnDamaged(DamageInfo info) => GameEvents.RequestEffect(GameEffect.EnemyHit, Center);

        private void OnDied(DamageInfo info) => GameEvents.RequestEffect(GameEffect.EnemyDeath, Center);

        private void OnStateChanged(EnemyState previous, EnemyState next)
        {
            if (next == EnemyState.Attack)
            {
                GameEvents.RequestEffect(GameEffect.EnemyWindup, Center);
            }
        }

        private void OnPhaseChanged(int index, BossPhase phase) => GameEvents.RequestEffect(GameEffect.BossRoar, Center);

        private void OnDefeated() => GameEvents.RequestEffect(GameEffect.BossDefeated, Center);
    }
}