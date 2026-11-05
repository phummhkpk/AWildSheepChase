using System.Collections.Generic;
using AWildSheepChase.Core;
using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Items
{
    /// <summary>ติดกับศัตรู: ตายแล้วสุ่มตาม Loot Table และสร้าง ItemPickup บนพื้นรอบ ๆ ตัว</summary>
    [RequireComponent(typeof(Health))]
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private LootTableData lootTable;
        [SerializeField] private ItemPickup pickupPrefab;
        [Tooltip("ใช้หาความสูงพื้นให้ของดรอปวางบนพื้นพอดี")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0f)] private float scatterRadius = 1f;

        private Health health;
        private readonly List<ItemStack> results = new List<ItemStack>();

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
        }

        private void OnDied(DamageInfo info)
        {
            if (lootTable == null || pickupPrefab == null) return;

            lootTable.Roll(results);

            foreach (ItemStack stack in results)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
                position = SnapToGround(position);

                ItemPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                pickup.Setup(stack.Item, stack.Amount);
            }
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * 2f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 6f, groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y;
            }

            return position;
        }
    }
}