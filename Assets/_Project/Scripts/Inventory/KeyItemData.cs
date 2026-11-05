using UnityEngine;

namespace AWildSheepChase.Items
{
    /// <summary>
    /// กุญแจ: ไม่มีค่าเพิ่มเติม ประตูใน Milestone 7 จะอ้างอิง asset กุญแจดอกนี้ตรง ๆ
    /// กุญแจแต่ละดอก (ห้อง/ประตูคนละบาน) ให้สร้างเป็น asset คนละไฟล์
    /// </summary>
    [CreateAssetMenu(fileName = "Key", menuName = "AWildSheepChase/Items/Key")]
    public class KeyItemData : ItemData
    {
    }
}