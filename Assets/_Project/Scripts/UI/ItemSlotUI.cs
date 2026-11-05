using AWildSheepChase.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    /// <summary>ช่อง HUD แสดงไอเทม 1 ชนิด: ไอคอน + จำนวน (จาง ๆ เมื่อไม่มีของ)</summary>
    public class ItemSlotUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private ItemData item;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;
        [SerializeField, Range(0f, 1f)] private float emptyAlpha = 0.35f;

        private bool started;

        private void Awake()
        {
            if (inventory == null || item == null)
            {
                Debug.LogWarning("ItemSlotUI: ยังไม่ได้ลาก Inventory (Player) หรือ Item ใส่ช่อง", this);
            }
        }

        private void OnEnable()
        {
            if (inventory == null) return;

            inventory.Changed += OnChanged;
            if (started) Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= OnChanged;
        }

        private void Start()
        {
            started = true;
            Refresh();
        }

        private void OnChanged(ItemData changed, int count)
        {
            if (changed == item) Refresh();
        }

        private void Refresh()
        {
            if (inventory == null || item == null) return;

            int count = inventory.Count(item);

            if (countText != null)
            {
                countText.text = "x" + count;
            }

            if (icon != null)
            {
                if (item.Icon != null) icon.sprite = item.Icon;

                Color color = item.WorldColor;
                color.a = count > 0 ? 1f : emptyAlpha;
                icon.color = color;
            }
        }
    }
}