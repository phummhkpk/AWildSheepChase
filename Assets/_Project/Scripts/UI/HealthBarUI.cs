using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.UI
{
    [RequireComponent(typeof(ResourceBar))]
    public class HealthBarUI : MonoBehaviour
    {
        [Tooltip("ปล่อยว่าง = หาจาก parent (ใช้กับแถบเหนือหัวศัตรู)")]
        [SerializeField] private Health health;
        [Tooltip("ใส่ถ้าต้องการซ่อนแถบ (ใช้กับแถบเหนือหัวศัตรู)")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool hideWhenFull;
        [SerializeField] private bool hideWhenEmpty;

        private ResourceBar bar;
        private bool started;

        private void Awake()
        {
            bar = GetComponent<ResourceBar>();

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (health == null)
            {
                Debug.LogWarning("HealthBarUI: ไม่พบ Health (ลากใส่ช่อง Health หรือวางเป็นลูกของ object ที่มี Health)", this);
            }
        }

        private void OnEnable()
        {
            if (health == null) return;

            health.HealthChanged += OnHealthChanged;
            if (started) Refresh(true);
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= OnHealthChanged;
        }

        private void Start()
        {
            started = true;
            Refresh(true);
        }

        private void OnHealthChanged(float current, float max)
        {
            bar.SetValue(current, max);
            UpdateVisibility(current, max);
        }

        private void Refresh(bool instant)
        {
            if (health == null) return;

            bar.SetValue(health.Current, health.MaxHealth, instant);
            UpdateVisibility(health.Current, health.MaxHealth);
        }

        private void UpdateVisibility(float current, float max)
        {
            if (canvasGroup == null) return;

            bool hide = (hideWhenFull && current >= max) || (hideWhenEmpty && current <= 0f);
            canvasGroup.alpha = hide ? 0f : 1f;
        }
    }
}