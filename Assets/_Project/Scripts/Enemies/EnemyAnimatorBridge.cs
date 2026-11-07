using UnityEngine;
using UnityEngine.AI;

namespace AWildSheepChase.Enemies
{
    /// <summary>
    /// ส่งสถานะศัตรู/บอสเข้า Animator (ไม่บังคับ) ถ้าไม่มี Animator สคริปต์นี้จะปิดตัวเอง
    /// Parameter ที่ต้องสร้างใน Controller: Speed (Float), Attack / Hurt / Die (Trigger)
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DieId = Animator.StringToHash("Die");

        [Tooltip("ปล่อยว่าง = หาจากลูกของศัตรู")]
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float speedDamp = 0.08f;

        private EnemyController enemy;
        private NavMeshAgent agent;
        private BossController boss; // ไม่บังคับ
        private bool dead;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            agent = GetComponent<NavMeshAgent>();
            TryGetComponent(out boss);

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
            enemy.StateChanged += OnEnemyStateChanged;
            if (boss != null) boss.StateChanged += OnBossStateChanged;
        }

        private void OnDisable()
        {
            enemy.StateChanged -= OnEnemyStateChanged;
            if (boss != null) boss.StateChanged -= OnBossStateChanged;
        }

        private void Update()
        {
            float speed = 0f;
            if (!dead && agent.enabled && agent.isOnNavMesh)
            {
                speed = agent.velocity.magnitude;
            }

            animator.SetFloat(SpeedId, speed, speedDamp, Time.deltaTime);
        }

        private void OnEnemyStateChanged(EnemyState previous, EnemyState next)
        {
            switch (next)
            {
                case EnemyState.Attack: animator.SetTrigger(AttackId); break;
                case EnemyState.Hurt: animator.SetTrigger(HurtId); break;
                case EnemyState.Dead:
                    dead = true;
                    animator.SetTrigger(DieId);
                    break;
            }
        }

        // บอสถูกคุมด้วย BossController (EnemyController ไม่ยิง state Attack) จึงฟังจากฝั่งบอสด้วย
        private void OnBossStateChanged(BossState previous, BossState next)
        {
            if (next == BossState.Attack)
            {
                animator.SetTrigger(AttackId);
            }
        }
    }
}