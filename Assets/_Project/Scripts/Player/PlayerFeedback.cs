using AWildSheepChase.Combat;
using AWildSheepChase.Core;
using AWildSheepChase.Items;
using UnityEngine;

namespace AWildSheepChase.Player
{
    /// <summary>แปลงเหตุการณ์ของผู้เล่นเป็นเอฟเฟกต์เสียง/ภาพ (ไม่มี logic เกม) ใส่ที่ Player</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerFeedback : MonoBehaviour
    {
        private PlayerController controller;
        private PlayerCombat combat;
        private Inventory inventory;
        private bool started;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            TryGetComponent(out combat);
            TryGetComponent(out inventory);
        }

        private void OnEnable()
        {
            controller.Jumped += OnJumped;
            controller.DashStarted += OnDashStarted;
            controller.Hurt += OnHurt;
            controller.Died += OnDied;

            if (combat != null)
            {
                combat.MeleeHitboxActivated += OnSwing;
                combat.HitLanded += OnHitLanded;
                combat.RangedFired += OnRangedFired;
            }

            if (inventory != null)
            {
                inventory.ItemAdded += OnItemAdded;
                inventory.ItemUsed += OnItemUsed;
            }
        }

        private void OnDisable()
        {
            controller.Jumped -= OnJumped;
            controller.DashStarted -= OnDashStarted;
            controller.Hurt -= OnHurt;
            controller.Died -= OnDied;

            if (combat != null)
            {
                combat.MeleeHitboxActivated -= OnSwing;
                combat.HitLanded -= OnHitLanded;
                combat.RangedFired -= OnRangedFired;
            }

            if (inventory != null)
            {
                inventory.ItemAdded -= OnItemAdded;
                inventory.ItemUsed -= OnItemUsed;
            }
        }

        private void Start()
        {
            // ข้ามเหตุการณ์ตอนเริ่มเกม (เช่น ของเริ่มต้นใน Inventory ถูก Add ใน Awake)
            started = true;
        }

        private Vector3 Chest => transform.position + Vector3.up;

        private void Play(GameEffect effect, Vector3 position)
        {
            if (started) GameEvents.RequestEffect(effect, position);
        }

        private void OnJumped() => Play(GameEffect.PlayerJump, transform.position);

        private void OnDashStarted() => Play(GameEffect.PlayerDash, Chest);

        private void OnHurt() => Play(GameEffect.PlayerHurt, Chest);

        private void OnDied() => Play(GameEffect.PlayerDeath, Chest);

        private void OnSwing(int stepIndex) => Play(GameEffect.SwordSwing, Chest + transform.forward);

        private void OnHitLanded(int hits) => Play(GameEffect.SwordHit, Chest + transform.forward);

        private void OnRangedFired() => Play(GameEffect.RangedShot, Chest + transform.forward);

        private void OnItemAdded(ItemData item, int amount) => Play(GameEffect.PickupCollect, Chest);

        private void OnItemUsed(ItemData item) => Play(GameEffect.PotionUse, Chest);
    }
}