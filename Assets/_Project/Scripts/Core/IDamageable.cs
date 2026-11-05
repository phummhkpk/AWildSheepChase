using UnityEngine;

namespace AWildSheepChase.Core
{
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        /// <summary>ทิศทางที่แรงกระทำไป (แนวราบ) ใช้ทำ knockback</summary>
        public Vector3 Direction { get; }
        public float KnockbackForce { get; }
        public GameObject Source { get; }

        public DamageInfo(float amount, Vector3 direction, float knockbackForce, GameObject source)
        {
            Amount = amount;
            Direction = direction;
            KnockbackForce = knockbackForce;
            Source = source;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(in DamageInfo info);
    }
}