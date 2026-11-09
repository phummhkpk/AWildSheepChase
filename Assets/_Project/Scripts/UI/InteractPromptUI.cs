using AWildSheepChase.Core;
using AWildSheepChase.Player;
using TMPro;
using UnityEngine;

namespace AWildSheepChase.UI
{
    /// <summary>แสดง "[E] Talk" เมื่อยืนใกล้สิ่งที่โต้ตอบได้ ต้องวางบน object ที่เปิดอยู่เสมอ (เช่น HUD)</summary>
    public class InteractPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [Tooltip("ใช้ดูว่าผู้เล่นใช้ gamepad อยู่หรือไม่ เพื่อเปลี่ยนชื่อปุ่ม (ไม่บังคับ)")]
        [SerializeField] private PlayerInputHandler playerInput;
        [Tooltip("ลูกที่เป็นข้อความ (ถูกเปิด/ปิดตามสถานะ)")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private string keyboardKey = "E";
        [SerializeField] private string gamepadKey = "LB";

        private bool lastGamepad;

        private void Awake()
        {
            if (interactor == null || label == null)
            {
                Debug.LogWarning("InteractPromptUI: ยังไม่ได้ลาก Player ใส่ช่อง Interactor หรือใส่ Label", this);
            }
        }

        private void OnEnable()
        {
            if (interactor != null) interactor.FocusChanged += OnFocusChanged;
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.FocusChanged -= OnFocusChanged;
        }

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            if (interactor == null || interactor.Focus == null || playerInput == null) return;

            if (playerInput.UsingGamepad != lastGamepad)
            {
                Refresh();
            }
        }

        private void OnFocusChanged(IInteractable focus)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;

            IInteractable focus = interactor != null ? interactor.Focus : null;
            if (focus == null)
            {
                label.gameObject.SetActive(false);
                return;
            }

            lastGamepad = playerInput != null && playerInput.UsingGamepad;
            label.text = "[" + (lastGamepad ? gamepadKey : keyboardKey) + "] " + focus.PromptText;
            label.gameObject.SetActive(true);
        }
    }
}