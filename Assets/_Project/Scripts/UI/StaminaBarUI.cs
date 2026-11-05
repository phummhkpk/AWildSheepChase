using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.UI
{
    [RequireComponent(typeof(ResourceBar))]
    public class StaminaBarUI : MonoBehaviour
    {
        [SerializeField] private Stamina stamina;

        private ResourceBar bar;
        private bool started;

        private void Awake()
        {
            bar = GetComponent<ResourceBar>();

            if (stamina == null)
            {
                Debug.LogWarning("StaminaBarUI: ยังไม่ได้ลาก Player ใส่ช่อง Stamina", this);
            }
        }

        private void OnEnable()
        {
            if (stamina == null) return;

            stamina.StaminaChanged += OnStaminaChanged;
            stamina.SpendFailed += OnSpendFailed;
            if (started) bar.SetValue(stamina.Current, stamina.Max, true);
        }

        private void OnDisable()
        {
            if (stamina == null) return;

            stamina.StaminaChanged -= OnStaminaChanged;
            stamina.SpendFailed -= OnSpendFailed;
        }

        private void Start()
        {
            started = true;
            if (stamina != null) bar.SetValue(stamina.Current, stamina.Max, true);
        }

        private void OnStaminaChanged(float current, float max) => bar.SetValue(current, max);

        private void OnSpendFailed() => bar.Flash();
    }
}