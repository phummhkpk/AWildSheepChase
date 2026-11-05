using UnityEngine;

namespace AWildSheepChase.Combat
{
    [CreateAssetMenu(fileName = "RangedWeaponData", menuName = "AWildSheepChase/Combat/Ranged Weapon Data")]
    public class RangedWeaponData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float damage = 8f;
        [SerializeField, Min(0f)] private float knockback = 3f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 16f;
        [SerializeField, Min(0.1f)] private float lifetime = 1.5f;
        [SerializeField, Min(0.05f)] private float projectileRadius = 0.2f;
        [SerializeField, Min(0f)] private float cooldown = 0.5f;
        [SerializeField, Min(0f)] private float hitStopDuration = 0.03f;

        public float Damage => damage;
        public float Knockback => knockback;
        public float ProjectileSpeed => projectileSpeed;
        public float Lifetime => lifetime;
        public float ProjectileRadius => projectileRadius;
        public float Cooldown => cooldown;
        public float HitStopDuration => hitStopDuration;
    }
}