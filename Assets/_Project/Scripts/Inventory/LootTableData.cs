using System.Collections.Generic;
using UnityEngine;

namespace AWildSheepChase.Items
{
    [System.Serializable]
    public class LootEntry
    {
        [SerializeField] private ItemData item;
        [Tooltip("โอกาสดรอป 0 = ไม่เลย, 1 = ดรอปแน่นอน (สุ่มแยกกันทุกแถว)")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.5f;
        [SerializeField, Min(1)] private int minAmount = 1;
        [SerializeField, Min(1)] private int maxAmount = 1;

        public ItemData Item => item;
        public float Chance => chance;

        public int RollAmount()
        {
            int max = Mathf.Max(minAmount, maxAmount);
            return Random.Range(minAmount, max + 1);
        }
    }

    [CreateAssetMenu(fileName = "LootTable", menuName = "AWildSheepChase/Items/Loot Table")]
    public class LootTableData : ScriptableObject
    {
        [SerializeField] private LootEntry[] entries = new LootEntry[0];

        /// <summary>สุ่มทุกแถวแยกกัน ผลลัพธ์ใส่ใน results (ล้างของเก่าก่อน)</summary>
        public void Roll(List<ItemStack> results)
        {
            results.Clear();

            foreach (LootEntry entry in entries)
            {
                if (entry == null || entry.Item == null) continue;
                if (Random.value > entry.Chance) continue;

                results.Add(new ItemStack(entry.Item, entry.RollAmount()));
            }
        }
    }
}