using AWildSheepChase.Items;
using UnityEngine;

namespace AWildSheepChase.Player
{
    /// <summary>กดปุ่ม UseItem แล้วใช้ไอเทมด่วน (ยาฟื้นพลัง)</summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Inventory))]
    public class PlayerItemUser : MonoBehaviour
    {
        [SerializeField] private ItemData quickUseItem;
        [SerializeField, Min(0f)] private float useCooldown = 0.5f;

        private PlayerInputHandler input;
        private PlayerController controller;
        private Inventory inventory;
        private float nextUseTime;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            controller = GetComponent<PlayerController>();
            inventory = GetComponent<Inventory>();
        }

        private void OnEnable()
        {
            input.UseItemPressed += OnUseItemPressed;
        }

        private void OnDisable()
        {
            input.UseItemPressed -= OnUseItemPressed;
        }

        private void OnUseItemPressed()
        {
            if (controller.IsDead || quickUseItem == null) return;
            if (Time.time < nextUseTime) return;

            if (inventory.TryUse(quickUseItem, gameObject))
            {
                nextUseTime = Time.time + useCooldown;
            }
        }
    }
}