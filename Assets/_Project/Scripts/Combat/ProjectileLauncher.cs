using UnityEngine;
using UnityEngine.Pool;

namespace AWildSheepChase.Combat
{
    public class ProjectileLauncher : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private RangedWeaponData weapon;
        [Tooltip("ตำแหน่งปากกระบอก เทียบกับตัวละคร (x ขวา, y ขึ้น, z หน้า) ในทิศที่ยิง")]
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 1f, 0.8f);
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask obstacleLayers;

        public float CooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);

        private ObjectPool<Projectile> pool;
        private Transform poolRoot;
        private float nextFireTime;

        private void Awake()
        {
            poolRoot = new GameObject("ProjectilePool").transform;
            pool = new ObjectPool<Projectile>(
                CreateProjectile, OnGetProjectile, OnReleaseProjectile, OnDestroyProjectile,
                true, 10, 50);
        }

        private void OnDestroy()
        {
            pool?.Dispose();
            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
            }
        }

        public bool TryFire(Vector3 direction, GameObject source)
        {
            if (projectilePrefab == null || weapon == null) return false;
            if (Time.time < nextFireTime) return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
            }
            direction.Normalize();

            Vector3 position = transform.position + Quaternion.LookRotation(direction, Vector3.up) * muzzleOffset;

            Projectile projectile = pool.Get();
            projectile.Launch(position, direction, weapon, targetLayers | obstacleLayers, source);

            nextFireTime = Time.time + weapon.Cooldown;
            return true;
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(projectilePrefab, poolRoot);
            projectile.SetPool(pool);
            return projectile;
        }

        private static void OnGetProjectile(Projectile projectile) => projectile.gameObject.SetActive(true);

        private static void OnReleaseProjectile(Projectile projectile) => projectile.gameObject.SetActive(false);

        private static void OnDestroyProjectile(Projectile projectile)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
    }
}