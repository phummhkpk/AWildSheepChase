using AWildSheepChase.CameraSystem;
using AWildSheepChase.Core;
using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Player
{
    /// <summary>เชื่อม Health กับ PlayerController: สะดุด, ตาย, i-frame ตอน Dash, shake และ hit-stop</summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDamageReceiver : MonoBehaviour
    {
        [SerializeField] private CameraShakeProfile hurtShake;
        [SerializeField] private CameraShakeProfile deathShake;
        [SerializeField, Min(0f)] private float hurtHitStop = 0.06f;

        private Health health;
        private PlayerController controller;

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();

            // Dash เป็น i-frame: Health จะเรียกฟังก์ชันนี้ก่อนรับดาเมจทุกครั้ง
            health.InvincibilityCheck = () => controller.IsInvincible;
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo info)
        {
            // ถ้าตาย ให้ OnDied จัดการแทน
            if (!health.IsAlive) return;

            controller.ApplyHurt(info.Direction, info.KnockbackForce);
            GameEvents.RequestCameraShake(hurtShake);
            GameEvents.RequestHitStop(hurtHitStop);
        }

        private void OnDied(DamageInfo info)
        {
            controller.Die();
            GameEvents.RequestCameraShake(deathShake);
            Debug.Log("Player ตายแล้ว (หน้า Game Over จะทำใน Milestone 9)");
        }
    }
}