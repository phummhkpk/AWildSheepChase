using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWildSheepChase.Items
{
    public class Inventory : MonoBehaviour
    {
        [Tooltip("ของที่มีตอนเริ่มเกม (ไว้ทดสอบ)")]
        [SerializeField] private List<ItemStack> startingItems = new List<ItemStack>();

        /// <summary>(ไอเทม, จำนวนใหม่ทั้งหมด) ยิงทุกครั้งที่จำนวนเปลี่ยน</summary>
        public event Action<ItemData, int> Changed;
        /// <summary>(ไอเทม, จำนวนที่เพิ่มเข้ามา) ไว้ทำ toast / เสียงเก็บของ</summary>
        public event Action<ItemData, int> ItemAdded;
        public event Action<ItemData> ItemUsed;

        /// <summary>ใช้อ่านเฉย ๆ ห้ามแก้ไข (Save/Load ใช้)</summary>
        public IReadOnlyDictionary<ItemData, int> Items => counts;

        private readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();

        private void Awake()
        {
            foreach (ItemStack stack in startingItems)
            {
                if (stack != null && stack.Item != null)
                {
                    Add(stack.Item, stack.Amount);
                }
            }
        }

        public int Count(ItemData item)
        {
            if (item == null) return 0;
            return counts.TryGetValue(item, out int value) ? value : 0;
        }

        public bool Has(ItemData item, int amount = 1) => Count(item) >= amount;

        /// <summary>เพิ่มไอเทม คืนจำนวนที่ใส่ได้จริง (กระเป๋าเต็มแล้วจะได้ 0 หรือน้อยกว่าที่ขอ)</summary>
        public int Add(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

            int current = Count(item);
            int space = Mathf.Max(0, item.MaxStack - current);
            int added = Mathf.Min(amount, space);
            if (added <= 0) return 0;

            counts[item] = current + added;
            Changed?.Invoke(item, current + added);
            ItemAdded?.Invoke(item, added);
            return added;
        }

        public bool Remove(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0 || !Has(item, amount)) return false;

            int remaining = Count(item) - amount;
            if (remaining > 0)
            {
                counts[item] = remaining;
            }
            else
            {
                counts.Remove(item);
            }

            Changed?.Invoke(item, remaining);
            return true;
        }

        /// <summary>กำหนดจำนวนตรง ๆ (ใช้ตอนโหลดเซฟ) ไม่ยิง ItemAdded จึงไม่มีข้อความเก็บของเด้ง</summary>
        public void SetCount(ItemData item, int amount)
        {
            if (item == null) return;

            amount = Mathf.Clamp(amount, 0, item.MaxStack);
            if (amount == Count(item)) return;

            if (amount > 0)
            {
                counts[item] = amount;
            }
            else
            {
                counts.Remove(item);
            }

            Changed?.Invoke(item, amount);
        }

        /// <summary>ล้างกระเป๋าทั้งหมด (ใช้ตอนโหลดเซฟ)</summary>
        public void ClearAll()
        {
            List<ItemData> keys = new List<ItemData>(counts.Keys);
            foreach (ItemData key in keys)
            {
                counts.Remove(key);
                Changed?.Invoke(key, 0);
            }
        }

        /// <summary>ใช้ไอเทม 1 ชิ้น ถ้าไอเทมบอกว่าใช้สำเร็จ ค่อยหักจำนวน</summary>
        public bool TryUse(ItemData item, GameObject user)
        {
            if (item == null || !Has(item)) return false;
            if (!item.Use(user)) return false;

            Remove(item);
            ItemUsed?.Invoke(item);
            return true;
        }
    }
}