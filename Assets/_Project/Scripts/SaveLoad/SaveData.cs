using System;
using System.Collections.Generic;

namespace AWildSheepChase.SaveLoad
{
    // ใช้ public field เพราะ JsonUtility ต้องการ (คลาสเหล่านี้เป็นแค่ถุงข้อมูล)

    [Serializable]
    public class ItemSaveEntry
    {
        public string id;
        public int amount;

        public ItemSaveEntry()
        {
        }

        public ItemSaveEntry(string id, int amount)
        {
            this.id = id;
            this.amount = amount;
        }
    }

    [Serializable]
    public class RoomSaveEntry
    {
        public string id;
        public bool visited;
        /// <summary>ลำดับ (index) ในช่อง Enemies ของห้อง ที่ตายแล้ว</summary>
        public List<int> deadEnemyIndices = new List<int>();
    }

    [Serializable]
    public class SaveData
    {
        // ยังเป็น 1: ฟิลด์ที่เพิ่มใหม่ (collectedPickupIds) ถ้าไม่มีในไฟล์เก่าจะเป็นลิสต์ว่าง จึงใช้เซฟเก่าต่อได้
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAtUtc;
        public string checkpointId;
        public float playerHealth;
        public List<ItemSaveEntry> items = new List<ItemSaveEntry>();
        public List<string> openDoorIds = new List<string>();
        public List<string> reachedCheckpointIds = new List<string>();
        public List<RoomSaveEntry> rooms = new List<RoomSaveEntry>();
        public List<string> collectedPickupIds = new List<string>();
        public List<string> talkedNpcIds = new List<string>();
    }
}