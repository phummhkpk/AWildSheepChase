using UnityEngine;

namespace AWildSheepChase.Items
{
    /// <summary>ไอเทมในโลก: ต้องมี Collider แบบ Is Trigger ผู้เล่นเดินชนแล้วเก็บเข้า Inventory</summary>
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour
    {
        /// <summary>ยิงเมื่อเก็บครบทั้งกอง (SaveManager ฟังเพื่อจดว่า Pickup ที่มี Id ถูกเก็บไปแล้ว)</summary>
        public static event System.Action<ItemPickup> Collected;

        [Tooltip("ของที่วางใน Scene ต้องตั้ง Id ไม่ซ้ำกัน (เซฟจะจำว่าเก็บแล้ว) ของดรอปจากศัตรูปล่อยว่างไว้")]
        [SerializeField] private string pickupId;
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int amount = 1;
        [Tooltip("ส่วนภาพที่ลอยและหมุน (ไม่บังคับ)")]
        [SerializeField] private Transform visual;
        [Tooltip("Renderer ที่จะถูกย้อมสีตาม World Color ของไอเทม (ไม่บังคับ)")]
        [SerializeField] private Renderer tintRenderer;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [SerializeField, Min(0f)] private float bobSpeed = 2.5f;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField, Min(0.01f)] private float popDuration = 0.25f;

        [Tooltip("รอเท่านี้หลังเกิดก่อนเก็บได้ (กันของดรอปถูกเก็บทันที)")]
        [SerializeField, Min(0f)] private float pickupDelay = 0.4f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string Id => pickupId;

        private MaterialPropertyBlock block;
        private Inventory touching;
        private Vector3 visualBasePosition;
        private Vector3 visualBaseScale = Vector3.one;
        private float spawnTime;
        private float bobPhase;
        private bool collected;

        private void Awake()
        {
            spawnTime = Time.time;
            bobPhase = Random.value * Mathf.PI * 2f;

            if (visual != null)
            {
                visualBasePosition = visual.localPosition;
                visualBaseScale = visual.localScale;
            }

            ApplyTint();
        }

        /// <summary>ใช้ตอนสร้างจาก LootDropper</summary>
        public void Setup(ItemData newItem, int newAmount)
        {
            item = newItem;
            amount = Mathf.Max(1, newAmount);
            ApplyTint();
        }

        private void ApplyTint()
        {
            if (item == null || tintRenderer == null) return;

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            tintRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, item.WorldColor); // URP Lit
            block.SetColor(ColorId, item.WorldColor);     // Built-in / Unlit บางตัว
            tintRenderer.SetPropertyBlock(block);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            Inventory inventory = other.GetComponentInParent<Inventory>();
            if (inventory != null)
            {
                touching = inventory;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (touching != null && other.GetComponentInParent<Inventory>() == touching)
            {
                touching = null;
            }
        }

        private void Update()
        {
            if (collected) return;

            float age = Time.time - spawnTime;
            AnimateVisual(age);

            if (touching != null && age >= pickupDelay)
            {
                TryCollect();
            }
        }

        private void AnimateVisual(float age)
        {
            if (visual == null) return;

            // เด้งขยายเข้ามาตอนเกิด (ease out)
            float pop = Mathf.Clamp01(age / popDuration);
            float scale = 1f - Mathf.Pow(1f - pop, 3f);
            visual.localScale = visualBaseScale * scale;

            float bob = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
            visual.localPosition = visualBasePosition + Vector3.up * bob;
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        private void TryCollect()
        {
            if (item == null) return;

            int added = touching.Add(item, amount);
            if (added <= 0) return; // กระเป๋าเต็ม ทิ้งของไว้ก่อน

            amount -= added;
            if (amount <= 0)
            {
                collected = true;
                Collected?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}