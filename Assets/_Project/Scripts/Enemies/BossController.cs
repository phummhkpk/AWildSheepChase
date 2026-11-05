using System;
using System.Collections;
using System.Collections.Generic;
using AWildSheepChase.Combat;
using AWildSheepChase.Core;
using AWildSheepChase.Vitals;
using UnityEngine;
using UnityEngine.AI;

namespace AWildSheepChase.Enemies
{
    public enum BossState { Idle, Chase, Attack, Transition, Dead }

    /// <summary>
    /// บอสหลาย phase: ใช้คู่กับ EnemyController (ซึ่งจะถูกสั่งปิด AI ของตัวเอง เหลือแค่ระบบตาย)
    /// ตัว root ของบอสต้องมี Scale = (1,1,1)
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ProjectileLauncher))]
    public class BossController : MonoBehaviour
    {
        [SerializeField] private BossData data;
        [Tooltip("Layer ของผู้เล่น (ใช้ตรวจว่าท่าโจมตีโดนไหม)")]
        [SerializeField] private LayerMask targetLayers;
        [Tooltip("Layer ที่บังสายตา เช่น Ground")]
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField, Min(0f)] private float eyeHeight = 1.5f;
        [Tooltip("ความสูงจากเท้าถึงจุดกำเนิด hitbox ของท่าฟันและท่าพุ่ง")]
        [SerializeField, Min(0f)] private float hitOriginHeight = 1.5f;
        [Tooltip("วงสีแดงบนพื้นของ Ground Slam (ลูกของบอส ไม่มี Collider)")]
        [SerializeField] private Transform slamIndicator;
        [SerializeField, Min(0.05f)] private float pathRefreshInterval = 0.2f;
        [Tooltip("หน่วงก่อนโจมตีครั้งแรกหลังตื่น")]
        [SerializeField, Min(0f)] private float wakeAttackDelay = 0.8f;

        public event Action Engaged;
        public event Action Disengaged;
        public event Action Defeated;
        public event Action<int, BossPhase> PhaseChanged;
        public event Action<BossState, BossState> StateChanged;

        public BossState State { get; private set; } = BossState.Idle;
        public int PhaseIndex { get; private set; }
        public string BossName => data != null ? data.BossName : "Boss";

        private EnemyController enemy;
        private NavMeshAgent agent;
        private Health health;
        private ProjectileLauncher launcher;

        private Transform target;
        private IDamageable targetDamageable;
        private Coroutine activeRoutine;

        private readonly Collider[] overlapBuffer = new Collider[8];
        private readonly HashSet<IDamageable> hitSet = new HashSet<IDamageable>();
        private readonly List<BossAttackType> candidates = new List<BossAttackType>();

        private bool inTransition;
        private bool aggro;
        private bool isEngaged;
        private bool canSeeTarget;
        private bool hasLastAttack;
        private BossAttackType lastAttack;
        private float nextPerceptionTime;
        private float nextPathTime;
        private float nextAttackTime;

        private BossPhase CurrentPhase => data.GetPhase(PhaseIndex);
        private bool HasTarget => target != null && targetDamageable != null && targetDamageable.IsAlive;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            launcher = GetComponent<ProjectileLauncher>();

            if (data == null || data.PhaseCount == 0)
            {
                Debug.LogError("BossController: ยังไม่ได้ใส่ Boss Data หรือ Phases ว่างเปล่า (บอสจะใช้ AI ธรรมดาแทน)", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnHealthDamaged;
            health.Died += OnHealthDied;
        }

        private void OnDisable()
        {
            if (health == null) return;

            health.Damaged -= OnHealthDamaged;
            health.Died -= OnHealthDied;
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("BossController: ไม่พบ GameObject ที่มี Tag 'Player'", this);
            }
            else
            {
                target = playerObject.transform;
                targetDamageable = playerObject.GetComponentInParent<IDamageable>();
            }

            enemy.SetExternalControl(true);
            health.InvincibilityCheck = () => inTransition;

            agent.stoppingDistance = data.StoppingDistance;
            agent.speed = CurrentPhase.MoveSpeed;

            HideSlam();

            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("BossController: บอสไม่ได้อยู่บน NavMesh (ลืม Bake หรือวางสูง/ต่ำเกินไป)", this);
            }
        }

        private void Update()
        {
            if (State == BossState.Dead) return;

            switch (State)
            {
                case BossState.Idle: UpdateIdle(); break;
                case BossState.Chase: UpdateChase(); break;
                    // Attack และ Transition ทำงานผ่าน coroutine
            }
        }

        // ---------- States ----------

        private void UpdateIdle()
        {
            if (!HasTarget) return;

            if (Time.time >= nextPerceptionTime)
            {
                nextPerceptionTime = Time.time + 0.15f;
                canSeeTarget = HasLineOfSight();
            }

            bool detected = canSeeTarget && DistanceToTarget() <= data.DetectionRange;
            if (detected || aggro)
            {
                aggro = false;
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + wakeAttackDelay);
                SetState(BossState.Chase);

                if (!isEngaged)
                {
                    isEngaged = true;
                    Engaged?.Invoke();
                }
            }
        }

        private void UpdateChase()
        {
            if (!HasTarget)
            {
                Disengage();
                return;
            }

            int targetPhase = GetTargetPhase();
            if (targetPhase > PhaseIndex)
            {
                BeginRoutine(TransitionRoutine(targetPhase));
                return;
            }

            if (Time.time >= nextAttackTime && TryPickAttack(DistanceToTarget(), out BossAttackType type))
            {
                lastAttack = type;
                hasLastAttack = true;
                StopAgent();
                SetState(BossState.Attack);
                BeginRoutine(RunAttack(type));
                return;
            }

            MoveTowardsTarget();
        }

        private void Disengage()
        {
            StopAgent();
            SetState(BossState.Idle);

            if (isEngaged)
            {
                isEngaged = false;
                Disengaged?.Invoke();
            }
        }

        // ---------- Phase ----------

        private int GetTargetPhase()
        {
            float fraction = health.MaxHealth > 0f ? health.Current / health.MaxHealth : 0f;

            for (int i = data.PhaseCount - 1; i > 0; i--)
            {
                if (fraction <= data.GetPhase(i).StartsAtHealthFraction)
                {
                    return i;
                }
            }

            return 0;
        }

        private IEnumerator TransitionRoutine(int newPhase)
        {
            StopAgent();
            SetState(BossState.Transition);

            PhaseIndex = newPhase;
            BossPhase phase = CurrentPhase;
            inTransition = true;
            enemy.ExternalTelegraph = true;

            GameEvents.RequestCameraShake(data.PhaseShake);
            GameEvents.RequestMessage(data.BossName + ": " + phase.PhaseName);

            float elapsed = 0f;
            while (elapsed < data.TransitionDuration)
            {
                if (target != null) FaceTarget();
                elapsed += Time.deltaTime;
                yield return null;
            }

            inTransition = false;
            enemy.ExternalTelegraph = false;
            agent.speed = phase.MoveSpeed;
            nextAttackTime = Time.time + phase.RestTime;
            PhaseChanged?.Invoke(PhaseIndex, phase);

            FinishRoutine();
        }

        // ---------- Attack selection ----------

        private bool TryPickAttack(float distance, out BossAttackType chosen)
        {
            chosen = BossAttackType.Swipe;
            candidates.Clear();

            foreach (BossAttackType type in CurrentPhase.Attacks)
            {
                BossAttackParams p = data.GetParams(type);
                if (distance >= p.EngageMin && distance <= p.EngageMax)
                {
                    candidates.Add(type);
                }
            }

            if (candidates.Count == 0) return false;

            // ถ้ามีทางเลือกอื่น หลีกเลี่ยงท่าเดิมซ้ำ
            if (candidates.Count > 1 && hasLastAttack)
            {
                candidates.Remove(lastAttack);
            }

            chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }

        private IEnumerator RunAttack(BossAttackType type)
        {
            switch (type)
            {
                case BossAttackType.Swipe: yield return SwipeRoutine(); break;
                case BossAttackType.Charge: yield return ChargeRoutine(); break;
                case BossAttackType.ProjectileFan: yield return FanRoutine(); break;
                case BossAttackType.GroundSlam: yield return SlamRoutine(); break;
            }

            nextAttackTime = Time.time + CurrentPhase.RestTime;
            FinishRoutine();
        }

        // ---------- Attacks ----------

        private IEnumerator SwipeRoutine()
        {
            SwipeParams p = data.Swipe;

            yield return Windup(p.Windup, true);

            ApplyArea(transform.forward, p.Range, p.ArcAngle, p.Damage, p.Knockback, hitOriginHeight, false);

            yield return new WaitForSeconds(p.Recovery);
        }

        private IEnumerator ChargeRoutine()
        {
            ChargeParams p = data.Charge;

            yield return Windup(p.Windup, true);

            Vector3 direction = FlatForward();
            float elapsed = 0f;
            bool hitPlayer = false;
            bool crashed = false;

            while (elapsed < p.Duration)
            {
                float step = p.Speed * Time.deltaTime;
                Vector3 before = transform.position;

                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.Move(direction * step);
                }

                if (!hitPlayer)
                {
                    hitPlayer = ApplyArea(direction, p.HitRadius, 360f, p.Damage, p.Knockback,
                        hitOriginHeight, false) > 0;
                }

                // ขยับได้น้อยมากทั้งที่สั่งพุ่ง = ชนขอบ NavMesh / กำแพง
                float moved = Vector3.Distance(before, transform.position);
                if (elapsed > 0.1f && moved < step * 0.25f)
                {
                    crashed = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (crashed)
            {
                GameEvents.RequestCameraShake(data.CrashShake);
                yield return new WaitForSeconds(p.WallStunTime);
            }
            else
            {
                yield return new WaitForSeconds(p.Recovery);
            }
        }

        private IEnumerator FanRoutine()
        {
            FanParams p = data.Fan;

            yield return Windup(p.Windup, true);

            int waves = p.Waves + CurrentPhase.ExtraFanWaves;
            int count = Mathf.Max(1, p.ProjectileCount);

            for (int wave = 0; wave < waves; wave++)
            {
                Vector3 aim = wave == 0 ? FlatForward() : DirectionToTarget();

                for (int i = 0; i < count; i++)
                {
                    float angle = count > 1
                        ? Mathf.Lerp(-p.SpreadAngle * 0.5f, p.SpreadAngle * 0.5f, i / (float)(count - 1))
                        : 0f;

                    Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * aim;
                    launcher.TryFire(direction, gameObject);
                }

                if (wave < waves - 1)
                {
                    yield return WaitFacingTarget(p.WaveInterval);
                }
            }

            yield return new WaitForSeconds(p.Recovery);
        }

        private IEnumerator SlamRoutine()
        {
            SlamParams p = data.Slam;

            ShowSlam(p.Radius);
            yield return Windup(p.Windup, false);

            // จุดกำเนิดต่ำที่ระดับเอวผู้เล่น วงกระแทกจึงตรงกับวงสีแดงบนพื้น
            ApplyArea(transform.forward, p.Radius, 360f, p.Damage, p.Knockback, 0.5f, true);
            GameEvents.RequestCameraShake(data.SlamShake);
            HideSlam();

            yield return new WaitForSeconds(p.Recovery);
        }

        // ---------- Attack helpers ----------

        /// <summary>เตรียมโจมตี (ตัวแดง) ช่วงแรกหันตามผู้เล่น ช่วงท้ายล็อกทิศ</summary>
        private IEnumerator Windup(float baseDuration, bool track)
        {
            float duration = baseDuration * CurrentPhase.WindupScale;
            float trackUntil = duration * data.WindupTrackFraction;
            float elapsed = 0f;

            enemy.ExternalTelegraph = true;

            while (elapsed < duration)
            {
                if (track && elapsed < trackUntil && target != null)
                {
                    FaceTarget();
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            enemy.ExternalTelegraph = false;
        }

        private IEnumerator WaitFacingTarget(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (target != null) FaceTarget();
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>ทำดาเมจเป็นพัด/วงกลมรอบบอส คืนจำนวนเป้าหมายที่โดน</summary>
        private int ApplyArea(Vector3 facing, float range, float arcAngle, float damage, float knockback,
            float originHeight, bool jumpable)
        {
            Vector3 origin = transform.position + Vector3.up * originHeight;
            int count = Physics.OverlapSphereNonAlloc(
                origin, range, overlapBuffer, targetLayers, QueryTriggerInteraction.Ignore);

            float halfArc = arcAngle * 0.5f;
            int hits = 0;
            hitSet.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];

                if (jumpable && col.bounds.min.y > transform.position.y + data.Slam.JumpClearHeight) continue;

                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || hitSet.Contains(damageable)) continue;

                Vector3 toTarget = col.ClosestPoint(origin) - origin;
                toTarget.y = 0f;
                bool hasDirection = toTarget.sqrMagnitude > 0.0001f;

                if (arcAngle < 359f && hasDirection && Vector3.Angle(facing, toTarget) > halfArc) continue;

                Vector3 hitDirection = hasDirection ? toTarget.normalized : facing;
                hitSet.Add(damageable);
                damageable.TakeDamage(new DamageInfo(damage, hitDirection, knockback, gameObject));
                hits++;
            }

            return hits;
        }

        private void ShowSlam(float radius)
        {
            if (slamIndicator == null) return;

            float diameter = radius * 2f;
            Vector3 scale = slamIndicator.localScale;
            slamIndicator.localScale = new Vector3(diameter, scale.y, diameter);
            slamIndicator.gameObject.SetActive(true);
        }

        private void HideSlam()
        {
            if (slamIndicator != null) slamIndicator.gameObject.SetActive(false);
        }

        // ---------- Routine / state helpers ----------

        private void BeginRoutine(IEnumerator routine)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(routine);
        }

        /// <summary>จบท่าโจมตี/การเปลี่ยน phase แล้วกลับไปไล่ตาม (หรือ Idle ถ้าผู้เล่นตายแล้ว)</summary>
        private void FinishRoutine()
        {
            activeRoutine = null;
            enemy.ExternalTelegraph = false;
            HideSlam();

            if (HasTarget)
            {
                SetState(BossState.Chase);
            }
            else
            {
                Disengage();
            }
        }

        private void SetState(BossState next)
        {
            if (State == next) return;

            BossState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }

        // ---------- Movement / perception ----------

        private void MoveTowardsTarget()
        {
            if (Time.time >= nextPathTime && agent.enabled && agent.isOnNavMesh)
            {
                nextPathTime = Time.time + pathRefreshInterval;
                agent.SetDestination(target.position);
            }

            Vector3 desired = agent.desiredVelocity;
            desired.y = 0f;
            Vector3 toTarget = target.position - transform.position;
            RotateToward(desired.sqrMagnitude > 0.05f ? desired : toTarget);
        }

        private void StopAgent()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;

            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        private void FaceTarget()
        {
            RotateToward(target.position - transform.position);
        }

        private void RotateToward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, data.TurnSpeed * Time.deltaTime);
        }

        private Vector3 FlatForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private Vector3 DirectionToTarget()
        {
            if (target == null) return FlatForward();

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : FlatForward();
        }

        private float DistanceToTarget()
        {
            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private bool HasLineOfSight()
        {
            Vector3 from = transform.position + Vector3.up * eyeHeight;
            Vector3 to = target.position + Vector3.up;
            return !Physics.Linecast(from, to, obstacleLayers, QueryTriggerInteraction.Ignore);
        }

        // ---------- Health events ----------

        private void OnHealthDamaged(DamageInfo info)
        {
            // โดนตีจากไกล ๆ ก็ตื่น
            aggro = true;
        }

        private void OnHealthDied(DamageInfo info)
        {
            if (State == BossState.Dead) return;

            StopAllCoroutines();
            activeRoutine = null;
            inTransition = false;
            enemy.ExternalTelegraph = false;
            HideSlam();

            SetState(BossState.Dead);
            Defeated?.Invoke();

            GameEvents.RequestCameraShake(data.DeathShake);
            GameEvents.RequestHitStop(0.15f);
            GameEvents.RequestMessage(data.BossName + " defeated!");
        }

        // ---------- Debug ----------

        [ContextMenu("Debug Damage 35 Percent")]
        private void DebugDamage35Percent()
        {
            if (!Application.isPlaying) return;

            health.TakeDamage(new DamageInfo(health.MaxHealth * 0.35f, Vector3.zero, 0f, gameObject));
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.DetectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.Slam.Radius);
        }
    }
}