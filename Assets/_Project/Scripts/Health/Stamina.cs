using System;
using UnityEngine;

namespace AWildSheepChase.Vitals
{
    public class Stamina : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float regenPerSecond = 35f;
        [Tooltip("หลังใช้ stamina ต้องรอเท่านี้ก่อนเริ่มฟื้น")]
        [SerializeField, Min(0f)] private float regenDelay = 0.8f;

        public float Max => maxStamina;
        public float Current { get; private set; }

        /// <summary>(current, max)</summary>
        public event Action<float, float> StaminaChanged;
        /// <summary>ยิงเมื่อพยายามใช้ stamina แต่ไม่พอ (ไว้ให้ UI กะพริบเตือน)</summary>
        public event Action SpendFailed;

        private float regenStartTime;

        private void Awake()
        {
            Current = maxStamina;
        }

        public bool HasEnough(float amount) => Current >= amount;

        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;

            if (Current < amount)
            {
                SpendFailed?.Invoke();
                return false;
            }

            Current -= amount;
            regenStartTime = Time.time + regenDelay;
            StaminaChanged?.Invoke(Current, maxStamina);
            return true;
        }

        public void Restore(float amount)
        {
            if (amount <= 0f) return;

            Current = Mathf.Min(maxStamina, Current + amount);
            StaminaChanged?.Invoke(Current, maxStamina);
        }

        private void Update()
        {
            if (Current >= maxStamina || Time.time < regenStartTime) return;

            Current = Mathf.Min(maxStamina, Current + regenPerSecond * Time.deltaTime);
            StaminaChanged?.Invoke(Current, maxStamina);
        }
    }
}