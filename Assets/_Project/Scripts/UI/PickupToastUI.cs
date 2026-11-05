using AWildSheepChase.Core;
using AWildSheepChase.Items;
using TMPro;
using UnityEngine;

namespace AWildSheepChase.UI
{
    /// <summary>ข้อความสั้น ๆ กลางล่างจอ: ตอนเก็บของ ("+1 Health Potion") และข้อความระบบอื่น ๆ แล้วค่อย ๆ จางหาย</summary>
    public class PickupToastUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private TMP_Text text;
        [SerializeField, Min(0.1f)] private float duration = 1.5f;
        [SerializeField, Min(0.05f)] private float fadeTime = 0.4f;

        private Color baseColor = Color.white;
        private float hideTime;

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            if (text != null)
            {
                baseColor = text.color;
                SetAlpha(0f);
            }

            if (inventory == null)
            {
                Debug.LogWarning("PickupToastUI: ยังไม่ได้ลาก Inventory (Player) ใส่ช่อง (ข้อความจากประตู/Checkpoint ยังแสดงได้ แต่ข้อความเก็บของจะไม่ขึ้น)", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.MessageRequested += ShowMessage;
            if (inventory != null) inventory.ItemAdded += OnItemAdded;
        }

        private void OnDisable()
        {
            GameEvents.MessageRequested -= ShowMessage;
            if (inventory != null) inventory.ItemAdded -= OnItemAdded;
        }

        private void OnItemAdded(ItemData item, int amount)
        {
            ShowMessage("+" + amount + " " + item.DisplayName);
        }

        private void ShowMessage(string message)
        {
            if (text == null) return;

            text.text = message;
            hideTime = Time.unscaledTime + duration;
            SetAlpha(1f);
        }

        private void Update()
        {
            if (text == null) return;

            float remaining = hideTime - Time.unscaledTime;
            SetAlpha(Mathf.Clamp01(remaining / fadeTime));
        }

        private void SetAlpha(float alpha)
        {
            Color color = baseColor;
            color.a = alpha;
            text.color = color;
        }
    }
}