using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Enemies
{
    /// <summary>เอฟเฟกต์ชั่วคราวผ่านสีของ material: กะพริบขาวตอนโดนตี, แดงตอนเตรียมโจมตี, ย่อตัวตอนตาย</summary>
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(Health))]
    public class EnemyFeedback : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color telegraphColor = Color.red;
        [SerializeField, Min(0f)] private float flashDuration = 0.1f;

        private EnemyController enemy;
        private Health health;
        private Material material;
        private Color baseColor;
        private Vector3 baseScale;
        private float flashEndTime;
        private float deadTime;
        private bool isDead;

        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            health = GetComponent<Health>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            material = targetRenderer.material;
            baseColor = material.color;
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            enemy.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            enemy.StateChanged -= OnStateChanged;
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void OnDamaged(AWildSheepChase.Core.DamageInfo info)
        {
            flashEndTime = Time.unscaledTime + flashDuration;
        }

        private void OnStateChanged(EnemyState previous, EnemyState next)
        {
            if (next == EnemyState.Dead)
            {
                isDead = true;
                deadTime = Time.time;
            }
        }

        private void Update()
        {
            if (isDead)
            {
                float t = Mathf.Clamp01((Time.time - deadTime) / Mathf.Max(0.01f, enemy.DespawnDelay));
                transform.localScale = baseScale * (1f - t);
            }

            Color color = baseColor;
            if (enemy.IsWindingUp) color = telegraphColor;
            if (Time.unscaledTime < flashEndTime) color = hitFlashColor;
            material.color = color;
        }
    }
}