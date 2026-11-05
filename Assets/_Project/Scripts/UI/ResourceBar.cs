using UnityEngine;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    /// <summary>
    /// แถบ HP/Stamina: ปรับความกว้างด้วย anchorMax.x (ไม่ต้องใช้ Sprite)
    /// ลูกทั้งสองต้องตั้ง Anchor แบบ stretch (0,0)-(1,1) และ Offset ทุกด้าน = 0
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [Tooltip("แถบเงาที่ค่อย ๆ ลดตามหลัง (ไม่บังคับ)")]
        [SerializeField] private RectTransform delayedFill;
        [Tooltip("ใช้กะพริบเตือน (ไม่บังคับ)")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.15f;
        [Tooltip("ความเร็วที่แถบเงาลด (สัดส่วนต่อวินาที)")]
        [SerializeField, Min(0.01f)] private float delayedSpeed = 0.6f;

        private float targetRatio = 1f;
        private float delayedRatio = 1f;
        private float flashEndTime;
        private Color baseColor = Color.white;

        private void Awake()
        {
            if (fillImage != null)
            {
                baseColor = fillImage.color;
            }

            Apply();
        }

        public void SetValue(float current, float max, bool instant = false)
        {
            targetRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (instant)
            {
                delayedRatio = targetRatio;
            }

            Apply();
        }

        public void Flash()
        {
            flashEndTime = Time.unscaledTime + flashDuration;
        }

        private void Update()
        {
            if (delayedRatio > targetRatio)
            {
                delayedRatio = Mathf.MoveTowards(delayedRatio, targetRatio, delayedSpeed * Time.unscaledDeltaTime);
            }
            else
            {
                delayedRatio = targetRatio;
            }

            Apply();

            if (fillImage != null)
            {
                fillImage.color = Time.unscaledTime < flashEndTime ? flashColor : baseColor;
            }
        }

        private void Apply()
        {
            SetRatio(fill, targetRatio);
            SetRatio(delayedFill, delayedRatio);
        }

        private static void SetRatio(RectTransform rect, float ratio)
        {
            if (rect == null) return;

            Vector2 max = rect.anchorMax;
            max.x = ratio;
            rect.anchorMax = max;
        }
    }
}