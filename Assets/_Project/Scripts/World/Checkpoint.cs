using System;
using AWildSheepChase.Core;
using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.World
{
    /// <summary>จุดเซฟ: Collider ต้องเป็น Is Trigger ผู้เล่นเดินผ่านแล้วกลายเป็นจุดเกิดใหม่</summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        /// <summary>ยิงเมื่อมีการเปิดใช้ Checkpoint ใหม่ (PlayerRespawn ฟังอันนี้)</summary>
        public static event Action<Checkpoint> Activated;

        [Tooltip("รหัส Checkpoint ใช้ตอน Save/Load ห้ามซ้ำกัน")]
        [SerializeField] private string checkpointId = "checkpoint";
        [Tooltip("ไม่บังคับ: จุดที่ผู้เล่นจะเกิด ถ้าว่างใช้ตำแหน่งของ object นี้")]
        [SerializeField] private Transform respawnPoint;
        [Tooltip("Renderer ที่จะเปลี่ยนสีเมื่อเปิดใช้ (ไม่บังคับ)")]
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 1f);
        [Tooltip("ฟื้น HP/Stamina เต็มเฉพาะครั้งแรกที่ผ่าน")]
        [SerializeField] private bool restoreOnFirstActivation = true;
        [SerializeField] private string message = "Checkpoint";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public string Id => checkpointId;
        public bool IsActive { get; private set; }
        public bool HasBeenReached { get; private set; }

        public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;
        public Quaternion RespawnRotation => respawnPoint != null ? respawnPoint.rotation : transform.rotation;

        private MaterialPropertyBlock block;

        private void Awake()
        {
            ApplyColor(inactiveColor);
        }

        private void OnEnable()
        {
            Activated += OnAnyCheckpointActivated;
        }

        private void OnDisable()
        {
            Activated -= OnAnyCheckpointActivated;
        }

        private void OnAnyCheckpointActivated(Checkpoint other)
        {
            if (other == this) return;

            IsActive = false;
            ApplyColor(inactiveColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsActive || !other.CompareTag("Player")) return;

            Activate(other);
        }

        private void Activate(Collider player)
        {
            IsActive = true;
            ApplyColor(activeColor);

            if (!HasBeenReached)
            {
                HasBeenReached = true;

                if (restoreOnFirstActivation)
                {
                    Health health = player.GetComponentInParent<Health>();
                    if (health != null) health.Heal(health.MaxHealth);

                    Stamina stamina = player.GetComponentInParent<Stamina>();
                    if (stamina != null) stamina.Restore(stamina.Max);
                }
            }

            GameEvents.RequestMessage(message);
            Activated?.Invoke(this);
        }

        private void ApplyColor(Color color)
        {
            if (indicator == null) return;

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            indicator.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color); // URP Lit
            block.SetColor(ColorId, color);
            indicator.SetPropertyBlock(block);
        }
    }
}