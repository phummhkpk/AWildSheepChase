using System;
using AWildSheepChase.Core;
using UnityEngine;

namespace AWildSheepChase.Vitals
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [Tooltip("อมตะนานเท่านี้หลังโดนตี (0 = ไม่มี i-frame) ผู้เล่นควรใช้ประมาณ 0.6")]
        [SerializeField, Min(0f)] private float invincibilityDuration = 0f;
        [SerializeField] private bool showDamageNumbers = true;
        [Tooltip("ความสูงจากเท้าที่ตัวเลขดาเมจจะเริ่มลอย")]
        [SerializeField, Min(0f)] private float damageNumberHeight = 2f;

        public float MaxHealth => maxHealth;
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        /// <summary>อยู่ในช่วง i-frame หลังโดนตี (นับเฉพาะแบบจับเวลา ไม่รวม Dash)</summary>
        public bool IsInIFrames => Time.time < invincibleUntil;

        /// <summary>ระบบอื่นตั้งฟังก์ชันนี้เพื่อบอกว่า "ตอนนี้อมตะ" เช่น ตอน Dash</summary>
        public Func<bool> InvincibilityCheck { get; set; }

        public bool IsInvincible => IsInIFrames || (InvincibilityCheck != null && InvincibilityCheck());

        /// <summary>ยิงทุกครั้งที่โดนดาเมจ (รวมครั้งที่ทำให้ตายด้วย)</summary>
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        /// <summary>(current, max)</summary>
        public event Action<float, float> HealthChanged;

        private float invincibleUntil = -1f;
        private bool isPlayer;

        private void Awake()
        {
            Current = maxHealth;
            isPlayer = CompareTag("Player");
        }

        public void SetMaxHealth(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, value);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            HealthChanged?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || IsInvincible) return;

            Current = Mathf.Max(0f, Current - info.Amount);

            if (invincibilityDuration > 0f)
            {
                invincibleUntil = Time.time + invincibilityDuration;
            }

            HealthChanged?.Invoke(Current, maxHealth);

            if (showDamageNumbers)
            {
                GameEvents.RaiseDamageTaken(
                    transform.position + Vector3.up * damageNumberHeight, info.Amount, isPlayer);
            }

            Damaged?.Invoke(info);

            if (Current <= 0f)
            {
                Died?.Invoke(info);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            HealthChanged?.Invoke(Current, maxHealth);
        }

        /// <summary>ชุบชีวิต: ตั้ง HP ตามสัดส่วนของค่าสูงสุด (อย่างน้อย 1) และอมตะชั่วคราว</summary>
        public void Revive(float healthFraction = 1f, float invincibleSeconds = 0f)
        {
            Current = Mathf.Clamp(maxHealth * Mathf.Clamp01(healthFraction), 1f, maxHealth);
            invincibleUntil = invincibleSeconds > 0f ? Time.time + invincibleSeconds : -1f;
            HealthChanged?.Invoke(Current, maxHealth);
        }
    }
}