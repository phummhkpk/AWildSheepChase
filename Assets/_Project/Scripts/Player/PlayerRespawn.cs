using System;
using System.Collections;
using AWildSheepChase.Vitals;
using AWildSheepChase.World;
using UnityEngine;

namespace AWildSheepChase.Player
{
    /// <summary>จำจุดเกิดล่าสุด (ตำแหน่งเริ่มเกม หรือ Checkpoint) และชุบผู้เล่นเมื่อตาย</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Health))]
    public class PlayerRespawn : MonoBehaviour
    {
        [Tooltip("เกิดใหม่เองหลังตาย (Milestone 9 จะปิดอันนี้แล้วใช้หน้า Game Over แทน)")]
        [SerializeField] private bool autoRespawn = true;
        [SerializeField, Min(0f)] private float autoRespawnDelay = 2.5f;
        [SerializeField, Range(0.1f, 1f)] private float respawnHealthFraction = 1f;
        [SerializeField, Min(0f)] private float respawnInvincibility = 1.5f;

        public event Action Respawned;

        public Vector3 SpawnPosition { get; private set; }
        public Quaternion SpawnRotation { get; private set; }

        private PlayerController controller;
        private Health health;
        private Stamina stamina; // ไม่บังคับ

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            TryGetComponent(out stamina);

            SpawnPosition = transform.position;
            SpawnRotation = transform.rotation;
        }

        private void OnEnable()
        {
            controller.Died += OnPlayerDied;
            Checkpoint.Activated += OnCheckpointActivated;
        }

        private void OnDisable()
        {
            controller.Died -= OnPlayerDied;
            Checkpoint.Activated -= OnCheckpointActivated;
        }

        public void SetSpawn(Vector3 position, Quaternion rotation)
        {
            SpawnPosition = position;
            SpawnRotation = rotation;
        }

        public void Respawn()
        {
            health.Revive(respawnHealthFraction, respawnInvincibility);

            if (stamina != null)
            {
                stamina.Restore(stamina.Max);
            }

            controller.Respawn(SpawnPosition, SpawnRotation);
            Respawned?.Invoke();
        }

        private void OnPlayerDied()
        {
            if (autoRespawn)
            {
                StartCoroutine(RespawnAfterDelay());
            }
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSecondsRealtime(autoRespawnDelay);

            if (controller.IsDead)
            {
                Respawn();
            }
        }

        private void OnCheckpointActivated(Checkpoint checkpoint)
        {
            SetSpawn(checkpoint.RespawnPosition, checkpoint.RespawnRotation);
        }
    }
}