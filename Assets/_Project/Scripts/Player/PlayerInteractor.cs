using System;
using AWildSheepChase.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AWildSheepChase.Player
{
    /// <summary>สแกนหา IInteractable ที่ใกล้ที่สุดรอบตัว และโต้ตอบเมื่อกดปุ่ม (E / LB)</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask interactLayers;
        [SerializeField, Min(0.5f)] private float interactRadius = 2f;
        [Tooltip("ความสูงจากเท้าถึงจุดกำเนิดการสแกน")]
        [SerializeField, Min(0f)] private float originHeight = 1f;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.1f;

        /// <summary>ยิงเมื่อสิ่งที่โฟกัสอยู่เปลี่ยน (null = ไม่มี)</summary>
        public event Action<IInteractable> FocusChanged;

        public IInteractable Focus { get; private set; }

        private PlayerController controller;
        private PlayerInputHandler input;
        private InputAction interactAction;
        private readonly Collider[] buffer = new Collider[8];
        private float nextScanTime;

        // input ถูกปิดตอน Pause / Game Over / Victory / คุยกับ NPC จึงใช้เป็นตัวตัดสินว่าโต้ตอบได้หรือไม่
        private bool CanOperate => input.enabled && !controller.IsDead;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputHandler>();

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/leftShoulder");
        }

        private void OnEnable()
        {
            interactAction.performed += OnInteractPerformed;
            interactAction.Enable();
        }

        private void OnDisable()
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.Disable();
            SetFocus(null);
        }

        private void OnDestroy()
        {
            interactAction?.Dispose();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanTime) return;
            nextScanTime = Time.unscaledTime + scanInterval;

            RefreshFocus();
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (!CanOperate || !controller.CanStartAction) return;

            RefreshFocus();
            if (Focus != null)
            {
                Focus.Interact(gameObject);
            }
        }

        private void RefreshFocus()
        {
            SetFocus(CanOperate ? FindBest() : null);
        }

        private IInteractable FindBest()
        {
            Vector3 origin = transform.position + Vector3.up * originHeight;
            int count = Physics.OverlapSphereNonAlloc(
                origin, interactRadius, buffer, interactLayers, QueryTriggerInteraction.Ignore);

            IInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = buffer[i];
                IInteractable candidate = col.GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;

                float distance = (col.ClosestPoint(origin) - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private void SetFocus(IInteractable next)
        {
            if (ReferenceEquals(Focus, next)) return;

            Focus = next;
            FocusChanged?.Invoke(next);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * originHeight, interactRadius);
        }
    }
}