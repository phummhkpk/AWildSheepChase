using System.Collections.Generic;
using UnityEngine;

namespace AWildSheepChase.Items
{
    /// <summary>รายชื่อไอเทมทั้งหมดในเกม ใช้แปลง Id ในไฟล์เซฟกลับเป็น asset (ไอเทมใหม่ทุกชิ้นต้องเพิ่มที่นี่)</summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "AWildSheepChase/Items/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemData[] items = new ItemData[0];

        private Dictionary<string, ItemData> lookup;

        private void OnEnable()
        {
            lookup = null;
        }

        public ItemData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (lookup == null) Build();
            return lookup.TryGetValue(id, out ItemData item) ? item : null;
        }

        private void Build()
        {
            lookup = new Dictionary<string, ItemData>();

            foreach (ItemData item in items)
            {
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.Id))
                {
                    Debug.LogWarning("ItemDatabase: ไอเทม '" + item.name + "' ไม่มี Id", this);
                    continue;
                }

                if (!lookup.TryAdd(item.Id, item))
                {
                    Debug.LogWarning("ItemDatabase: Id ซ้ำกัน '" + item.Id + "'", this);
                }
            }
        }
    }
}