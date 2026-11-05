using System;
using AWildSheepChase.Core;
using AWildSheepChase.Enemies;
using UnityEngine;

namespace AWildSheepChase.World
{
    /// <summary>
    /// โซนของห้อง: Collider ต้องเป็น Is Trigger และครอบพื้นที่ห้อง
    /// ลากศัตรูที่อยู่ในห้องใส่ช่อง Enemies เมื่อตายหมดจะยิง event Cleared
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Room : MonoBehaviour
    {
        [Tooltip("รหัสห้อง ใช้ตอน Save/Load ห้ามซ้ำกัน")]
        [SerializeField] private string roomId = "room";
        [SerializeField] private string displayName = "Room";
        [SerializeField] private bool announceOnFirstEnter = true;
        [SerializeField] private bool announceWhenCleared = true;
        [SerializeField] private EnemyController[] enemies = new EnemyController[0];

        public event Action<Room> PlayerEntered;
        public event Action<Room> Cleared;

        public string Id => roomId;
        public string DisplayName => displayName;
        public bool Visited { get; private set; }
        /// <summary>ห้องที่ไม่มีศัตรูเลย ถือว่า Cleared ตั้งแต่เริ่ม</summary>
        public bool IsCleared { get; private set; }
        public int RemainingEnemies => remaining;

        private int remaining;

        private void Awake()
        {
            remaining = 0;
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null) remaining++;
            }

            IsCleared = remaining == 0;
        }

        private void OnEnable()
        {
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null) enemy.Died += OnEnemyDied;
            }
        }

        private void OnDisable()
        {
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null) enemy.Died -= OnEnemyDied;
            }
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            if (IsCleared) return;

            remaining = Mathf.Max(0, remaining - 1);
            if (remaining > 0) return;

            IsCleared = true;
            if (announceWhenCleared)
            {
                GameEvents.RequestMessage(displayName + " cleared");
            }

            Cleared?.Invoke(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (!Visited)
            {
                Visited = true;
                if (announceOnFirstEnter)
                {
                    GameEvents.RequestMessage(displayName);
                }
            }

            PlayerEntered?.Invoke(this);
        }
    }
}