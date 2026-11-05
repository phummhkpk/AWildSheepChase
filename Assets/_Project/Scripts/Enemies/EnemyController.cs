using System;
using AWildSheepChase.Core;
using AWildSheepChase.Vitals;
using UnityEngine;
using UnityEngine.AI;

namespace AWildSheepChase.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Hurt, Dead }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        private enum AttackStage { Windup, Recovery }

        [SerializeField] private EnemyData data;
        [Tooltip("Layer ที่บังสายตา เช่น Ground (ผนัง)")]
        [SerializeField] private LayerMask obstacleLayers;
        [Tooltip("ความสูงจากเท้าถึงระดับสายตา (ใช้เช็คสายตา)")]
        [SerializeField, Min(0f)] private float eyeHeight = 1f;
        [SerializeField, Min(0.05f)] private float perceptionInterval = 0.15f;
        [SerializeField, Min(0.05f)] private float pathRefreshInterval = 0.2f;
        [Tooltip("หน่วงก่อนโจมตีครั้งแรกหลังเจอผู้เล่น")]
        [SerializeField, Min(0f)] private float aggroAttackDelay = 0.4f;

        public event Action<EnemyState, EnemyState> StateChanged;
        public event Action<EnemyController> Died;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsWindingUp => ExternalTelegraph
            || (State == EnemyState.Attack && attackStage == AttackStage.Windup);
        public float DespawnDelay => data != null ? data.DespawnDelay : 1f;

        /// <summary>true = ระบบอื่น (BossController) คุม AI แทน ตัวนี้เหลือแค่จัดการความตาย</summary>
        public bool ExternalControl { get; private set; }
        /// <summary>ระบบอื่นตั้งเป็น true เพื่อให้ตัวแดง (telegraph) ผ่าน EnemyFeedback</summary>
        public bool ExternalTelegraph { get; set; }

        public void SetExternalControl(bool value)
        {
            ExternalControl = value;
            if (value)
            {
                StopAgent();
            }
        }

        private NavMeshAgent agent;
        private Health health;
        private EnemyAttack attack;
        private Collider[] colliders;

        private Transform target;
        private IDamageable targetDamageable;

        private bool canSeeTarget;
        private float nextPerceptionTime;
        private float nextPathTime;
        private float nextAttackTime;

        private AttackStage attackStage;
        private float attackStageEndTime;
        private Vector3 attackFacing = Vector3.forward;

        private Vector3 knockbackVelocity;
        private float knockbackDeceleration;
        private float hurtEndTime;

        private bool HasTarget => target != null && targetDamageable != null && targetDamageable.IsAlive;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            attack = GetComponent<EnemyAttack>();
            colliders = GetComponentsInChildren<Collider>();

            if (data == null)
            {
                Debug.LogError("EnemyController: ยังไม่ได้ใส่ Enemy Data", this);
                enabled = false;
                return;
            }

            if (attack == null)
            {
                Debug.LogError("EnemyController: ไม่พบ Enemy Attack (ใส่ Enemy Melee Attack หรือ Enemy Ranged Attack)", this);
                enabled = false;
                return;
            }

            attack.Initialize(data);
            health.SetMaxHealth(data.MaxHealth);

            agent.speed = data.MoveSpeed;
            agent.acceleration = data.Acceleration;
            agent.angularSpeed = data.AngularSpeed;
            agent.stoppingDistance = data.StoppingDistance;
            agent.updateRotation = false; // เราหมุนเองเพื่อควบคุมตอนโจมตี
        }

        private void OnEnable()
        {
            health.Damaged += OnHealthDamaged;
            health.Died += OnHealthDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnHealthDamaged;
            health.Died -= OnHealthDied;
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("EnemyController: ไม่พบ GameObject ที่มี Tag 'Player'", this);
            }
            else
            {
                target = playerObject.transform;
                targetDamageable = playerObject.GetComponentInParent<IDamageable>();
                if (targetDamageable == null)
                {
                    Debug.LogWarning("EnemyController: Player ไม่มี Health จึงทำดาเมจไม่ได้", this);
                }
            }

            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("EnemyController: ศัตรูไม่ได้อยู่บน NavMesh (ลืม Bake หรือวางสูง/ต่ำเกินไป)", this);
            }
        }

        private void Update()
        {
            if (State == EnemyState.Dead || ExternalControl) return;

            UpdatePerception();

            switch (State)
            {
                case EnemyState.Idle: UpdateIdle(); break;
                case EnemyState.Chase: UpdateChase(); break;
                case EnemyState.Attack: UpdateAttack(); break;
                case EnemyState.Hurt: UpdateHurt(); break;
            }
        }

        // ---------- Perception ----------

        private void UpdatePerception()
        {
            if (Time.time < nextPerceptionTime) return;
            nextPerceptionTime = Time.time + perceptionInterval;
            canSeeTarget = target != null && HasLineOfSight();
        }

        private bool HasLineOfSight()
        {
            Vector3 from = transform.position + Vector3.up * eyeHeight;
            Vector3 to = target.position + Vector3.up * eyeHeight;
            return !Physics.Linecast(from, to, obstacleLayers, QueryTriggerInteraction.Ignore);
        }

        private float DistanceToTarget()
        {
            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        // ---------- States ----------

        private void UpdateIdle()
        {
            if (!HasTarget) return;

            if (canSeeTarget && DistanceToTarget() <= data.DetectionRange)
            {
                SetState(EnemyState.Chase);
            }
        }

        private void UpdateChase()
        {
            if (!HasTarget)
            {
                GoIdle();
                return;
            }

            float distance = DistanceToTarget();
            if (distance > data.LoseTargetRange)
            {
                GoIdle();
                return;
            }

            if (distance <= data.AttackRange && canSeeTarget && Time.time >= nextAttackTime)
            {
                StartAttack();
                return;
            }

            if (Time.time >= nextPathTime && agent.isOnNavMesh)
            {
                nextPathTime = Time.time + pathRefreshInterval;
                agent.SetDestination(target.position);
            }

            Vector3 desired = agent.desiredVelocity;
            desired.y = 0f;
            Vector3 toTarget = target.position - transform.position;
            Face(desired.sqrMagnitude > 0.05f ? desired : toTarget);
        }

        private void StartAttack()
        {
            StopAgent();

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            attackFacing = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

            attackStage = AttackStage.Windup;
            attackStageEndTime = Time.time + data.AttackWindup;
            SetState(EnemyState.Attack);
        }

        private void UpdateAttack()
        {
            if (attackStage == AttackStage.Windup)
            {
                Face(attackFacing);

                if (Time.time >= attackStageEndTime)
                {
                    attack.Execute(target, attackFacing);
                    attackStage = AttackStage.Recovery;
                    attackStageEndTime = Time.time + data.AttackRecovery;
                }
            }
            else if (Time.time >= attackStageEndTime)
            {
                nextAttackTime = Time.time + data.AttackCooldown;
                SetState(HasTarget ? EnemyState.Chase : EnemyState.Idle);
            }
        }

        private void UpdateHurt()
        {
            knockbackVelocity = Vector3.MoveTowards(
                knockbackVelocity, Vector3.zero, knockbackDeceleration * Time.deltaTime);

            if (agent.enabled && agent.isOnNavMesh && knockbackVelocity.sqrMagnitude > 0.0001f)
            {
                agent.Move(knockbackVelocity * Time.deltaTime);
            }

            if (Time.time >= hurtEndTime)
            {
                SetState(HasTarget ? EnemyState.Chase : EnemyState.Idle);
            }
        }

        private void GoIdle()
        {
            StopAgent();
            SetState(EnemyState.Idle);
        }

        // ---------- Health events ----------

        private void OnHealthDamaged(DamageInfo info)
        {
            if (!health.IsAlive || State == EnemyState.Dead) return;

            if (ExternalControl) return;

            if (!data.CanBeStaggered)
            {
                // ถูกตีแล้วหันมาไล่ผู้เล่นทันที แต่ไม่สะดุด
                if (State == EnemyState.Idle) SetState(EnemyState.Chase);
                return;
            }

            Vector3 direction = info.Direction;
            direction.y = 0f;
            float speed = info.KnockbackForce * (1f - data.KnockbackResistance);

            knockbackVelocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector3.zero;
            knockbackDeceleration = data.HurtDuration > 0.001f ? speed / data.HurtDuration : 1000f;
            hurtEndTime = Time.time + data.HurtDuration;

            StopAgent();
            SetState(EnemyState.Hurt);
        }

        private void OnHealthDied(DamageInfo info)
        {
            StopAgent();
            SetState(EnemyState.Dead);

            agent.enabled = false;
            foreach (Collider col in colliders)
            {
                if (col != null) col.enabled = false;
            }

            Died?.Invoke(this);
            Destroy(gameObject, data.DespawnDelay);
        }

        // ---------- Helpers ----------

        private void StopAgent()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, data.AngularSpeed * Time.deltaTime);
        }

        private void SetState(EnemyState next)
        {
            if (State == next) return;

            EnemyState previous = State;
            if (previous == EnemyState.Idle && next == EnemyState.Chase)
            {
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + aggroAttackDelay);
            }

            State = next;
            StateChanged?.Invoke(previous, next);
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.DetectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.AttackRange);
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, data.LoseTargetRange);
        }
    }
}