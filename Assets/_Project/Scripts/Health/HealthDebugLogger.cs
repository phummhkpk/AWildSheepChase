using UnityEngine;

namespace AWildSheepChase.Vitals
{
    /// <summary>ตัวช่วยชั่วคราว: พิมพ์ HP ลง Console ลบทิ้งได้เมื่อมี HP bar ใน Milestone 5</summary>
    [RequireComponent(typeof(Health))]
    public class HealthDebugLogger : MonoBehaviour
    {
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            Debug.Log($"{name} HP: {current}/{max}");
        }
    }
}