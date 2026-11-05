using AWildSheepChase.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace AWildSheepChase.Combat
{
    public class Projectile : MonoBehaviour
    {
        private IObjectPool<Projectile> pool;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float knockback;
        private float radius;
        private float hitStopDuration;
        private float despawnTime;
        private LayerMask hitMask;
        private GameObject source;
        private bool released = true;

        public void SetPool(IObjectPool<Projectile> ownerPool)
        {
            pool = ownerPool;
        }

        public void Launch(Vector3 position, Vector3 launchDirection, RangedWeaponData weapon,
            LayerMask mask, GameObject owner)
        {
            direction = launchDirection.normalized;
            speed = weapon.ProjectileSpeed;
            damage = weapon.Damage;
            knockback = weapon.Knockback;
            radius = weapon.ProjectileRadius;
            hitStopDuration = weapon.HitStopDuration;
            despawnTime = Time.time + weapon.Lifetime;
            hitMask = mask;
            source = owner;
            released = false;

            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
        }

        private void Update()
        {
            if (released) return;

            if (Time.time >= despawnTime)
            {
                Release();
                return;
            }

            float distance = speed * Time.deltaTime;

            // SphereCast กันกระสุนทะลุสิ่งกีดขวางบาง ๆ เมื่อวิ่งเร็ว
            if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit,
                    distance, hitMask, QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit);
                return;
            }

            transform.position += direction * distance;
        }

        private void HandleHit(RaycastHit hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(new DamageInfo(damage, direction, knockback, source));
                GameEvents.RequestHitStop(hitStopDuration);
            }

            Release();
        }

        private void Release()
        {
            if (released) return;
            released = true;

            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}