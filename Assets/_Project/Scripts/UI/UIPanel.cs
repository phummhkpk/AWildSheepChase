using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    /// <summary>หน้าต่าง UI ที่เปิด/ปิดได้ เปิดแล้ว fade เข้าด้วยเวลาจริง (ใช้ได้แม้เกมหยุดอยู่) และเลือกปุ่มแรกให้ gamepad</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        [Tooltip("ปุ่มที่ถูกเลือกเมื่อเปิดหน้าต่าง (ให้ gamepad/คีย์บอร์ดเลื่อนเมนูได้)")]
        [SerializeField] private Selectable firstSelected;
        [SerializeField, Min(0.1f)] private float fadeSpeed = 6f;

        public bool IsVisible { get; private set; }

        private CanvasGroup group;

        private CanvasGroup Group
        {
            get
            {
                if (group == null) group = GetComponent<CanvasGroup>();
                return group;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;

            Group.alpha = 0f;
            Group.interactable = true;
            Group.blocksRaycasts = true;

            if (firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }

        public void Hide()
        {
            IsVisible = false;

            Group.interactable = false;
            Group.blocksRaycasts = false;
            Group.alpha = 0f;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsVisible && Group.alpha < 1f)
            {
                Group.alpha = Mathf.MoveTowards(Group.alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}