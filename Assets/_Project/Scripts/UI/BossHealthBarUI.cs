using AWildSheepChase.Enemies;
using AWildSheepChase.Vitals;
using TMPro;
using UnityEngine;

namespace AWildSheepChase.UI
{
    /// <summary>แถบ HP ใหญ่ของบอสกลางบนจอ: โผล่เมื่อบอสตื่น ซ่อนเมื่อบอสตายหรือผู้เล่นตาย</summary>
    [RequireComponent(typeof(ResourceBar))]
    [RequireComponent(typeof(CanvasGroup))]
    public class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BossController boss;
        [SerializeField] private TMP_Text nameText;
        [SerializeField, Min(0.1f)] private float fadeSpeed = 4f;

        private ResourceBar bar;
        private CanvasGroup group;
        private Health health;
        private float targetAlpha;

        private void Awake()
        {
            bar = GetComponent<ResourceBar>();
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;

            if (boss == null)
            {
                Debug.LogWarning("BossHealthBarUI: ยังไม่ได้ลาก Enemy_Boss ใส่ช่อง Boss", this);
                return;
            }

            health = boss.GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (boss == null || health == null) return;

            health.HealthChanged += OnHealthChanged;
            boss.Engaged += OnEngaged;
            boss.Disengaged += OnHidden;
            boss.Defeated += OnHidden;
            boss.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            // ถ้าบอสถูกทำลายไปแล้ว ไม่ต้องถอด event (object หายไปพร้อมกัน)
            if (boss == null || health == null) return;

            health.HealthChanged -= OnHealthChanged;
            boss.Engaged -= OnEngaged;
            boss.Disengaged -= OnHidden;
            boss.Defeated -= OnHidden;
            boss.PhaseChanged -= OnPhaseChanged;
        }

        private void Start()
        {
            if (nameText != null && boss != null)
            {
                nameText.text = boss.BossName;
            }

            if (health != null)
            {
                bar.SetValue(health.Current, health.MaxHealth, true);
            }
        }

        private void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        }

        private void OnHealthChanged(float current, float max) => bar.SetValue(current, max);

        private void OnEngaged() => targetAlpha = 1f;

        private void OnHidden() => targetAlpha = 0f;

        private void OnPhaseChanged(int index, BossPhase phase) => bar.Flash();
    }
}